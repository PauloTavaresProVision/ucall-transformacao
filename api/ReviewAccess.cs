using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace Ucall;
public record MailInput(string Email);
public record VerifyInput(string Email,string Challenge,string Code);
public record AssignmentInput(string? Contact,string? CatalogueKey);
public record DelegateInput(string Email,string Area);
public record ReviewRow(string Id,string Reference,string SubmittedAt,string Title,string Area,string OwnerName,string? Contact,string? OriginalEmail,Triage? Triage,string State,string? Development,string? Mode,DateOnly? Start,DateOnly? End,string? Assumptions,decimal[]? Today,decimal[]? After,decimal[]? Gain,decimal? Impact,decimal Baseline,string? Actor,string? At);
public static partial class ReviewApi {
 static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
 const string Cookie="ucall.review";
 static long Now=>DateTimeOffset.UtcNow.ToUnixTimeSeconds();
 static string Stamp=>DateTimeOffset.UtcNow.ToString("O");
 static IResult Bad(string error)=>Results.BadRequest(new{error});
 static IResult Denied()=>Results.StatusCode(403);
 static string Clean(string? s)=>(s??"").Trim().ToLowerInvariant();
 static bool Mail(string s)=>s.Length is >3 and <=254 && new EmailAddressAttribute().IsValid(s)&&System.Net.Mail.MailAddress.TryCreate(s,out var m)&&m.Address==s&&!s.Any(char.IsWhiteSpace);
 static DevelopmentRequestInput Input(DevelopmentRequest r)=>JsonSerializer.Deserialize<DevelopmentRequestInput>(r.Payload,Json)!;
 static HoursForm Form(ReviewRecord r)=>JsonSerializer.Deserialize<HoursForm>(r.Payload,Json)!;
 static string Staff(HttpContext c)=>c.User.FindFirstValue(ClaimTypes.NameIdentifier)??"";
 static string? Viewer(HttpContext c,IDataProtectionProvider dp){
  if(!c.Request.Cookies.TryGetValue(Cookie,out var value))return null;
  try{return dp.CreateProtector("UCALL.hours.session.v1").ToTimeLimitedDataProtector().Unprotect(value);}catch(CryptographicException){return null;}
 }
 static CookieOptions Options(HttpContext c)=>new(){HttpOnly=true,Secure=!c.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()||c.Request.IsHttps,SameSite=SameSiteMode.Strict,Path="/api/review",MaxAge=TimeSpan.FromHours(8)};
 public static bool Allowed(string? viewer,string? owner,IEnumerable<string> areas,string area)=>!string.IsNullOrEmpty(viewer)&&(viewer==owner||areas.Contains(area));
 // The leader's assignment wins; otherwise the email written in the original submission.
 static string? Owner(ReviewBinding? b,DevelopmentRequestInput p)=>b?.Contact??(Mail(Clean(p.OwnerEmail))?Clean(p.OwnerEmail):null);
 static async Task<bool> Owns(RequestsDb db,string email,CancellationToken ct){
  var bindings=await db.Bindings.AsNoTracking().ToDictionaryAsync(x=>x.RequestId,ct);
  return (await db.Requests.AsNoTracking().ToListAsync(ct)).Where(Eligible).Any(r=>Owner(bindings.GetValueOrDefault(r.Id),Input(r))==email);
 }
 static Task<List<string>> Areas(RequestsDb db,string mail)=>db.Delegates.Where(x=>x.Email==mail).Select(x=>x.Area).ToListAsync();
 static ReviewEvent Event(string id,string actor,string action,string data)=>new(){RequestId=id,Actor=actor,At=Stamp,Action=action,Data=data};
 static async Task<IResult> Issue(MailInput i,RequestsDb db,IDataProtectionProvider dp,IReviewMail smtp,ILoggerFactory logs,CancellationToken ct){
  var email=Clean(i.Email);var token=Guid.NewGuid().ToString("N");var ticket=dp.CreateProtector("UCALL.hours.challenge",email).ToTimeLimitedDataProtector();IResult Reply(string id)=>Results.Ok(new{challenge=ticket.Protect(id,TimeSpan.FromMinutes(10))});var generic=Reply(token);
  if(!Mail(email)||!await db.Delegates.AnyAsync(x=>x.Email==email,ct)&&!await Owns(db,email,ct))return generic;
  var code=RandomNumberGenerator.GetInt32(1000000).ToString("D6");var secret=dp.CreateProtector("UCALL.hours.code",email,token).Protect(code);var now=Now;
  var changed=await db.Database.ExecuteSqlInterpolatedAsync($"""
   INSERT INTO "ReviewCodes" ("Email","Challenge","Secret","Issued","Attempts") VALUES ({email},{token},{secret},{now},0)
   ON CONFLICT ("Email") DO UPDATE SET "Challenge"=excluded."Challenge","Secret"=excluded."Secret","Issued"=excluded."Issued","Attempts"=0 WHERE "ReviewCodes"."Issued"<={now-60}
   """,ct);
  if(changed==1){try{await smtp.Send(email,"UCALL · Código de acesso aos teus pedidos",$"O teu código é {code}. É válido durante 10 minutos e só pode ser usado uma vez. Se não pediste este acesso, ignora esta mensagem.",ct);}catch(Exception e) when(e is not OperationCanceledException || !ct.IsCancellationRequested){logs.CreateLogger("ReviewMail").LogWarning("Não foi possível entregar um código de revisão por SMTP.");}}
  if(changed==0){var active=await db.Codes.AsNoTracking().Where(x=>x.Email==email).Select(x=>x.Challenge).FirstOrDefaultAsync(ct);return Reply(active??token);}
  return generic;
 }
 static async Task<IResult> Verify(VerifyInput i,RequestsDb db,HttpContext c,IDataProtectionProvider dp,CancellationToken ct){
  var email=Clean(i.Email);var since=Now-600;if(!Mail(email)||string.IsNullOrEmpty(i.Challenge)||i.Challenge.Length>1024||i.Code?.Length!=6)return Results.Unauthorized();
  try{i=i with{Challenge=dp.CreateProtector("UCALL.hours.challenge",email).ToTimeLimitedDataProtector().Unprotect(i.Challenge)};}catch(CryptographicException){return Results.Unauthorized();}
  var q=db.Codes.Where(x=>x.Email==email&&x.Challenge==i.Challenge&&x.Issued>since&&x.Attempts<5);
  if(await q.ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Attempts,x=>x.Attempts+1),ct)!=1)return Results.Unauthorized();
  var challenge=await db.Codes.AsNoTracking().FirstOrDefaultAsync(x=>x.Email==email&&x.Challenge==i.Challenge,ct);
  if(challenge==null)return Results.Unauthorized();
  string expected;try{expected=dp.CreateProtector("UCALL.hours.code",email,i.Challenge).Unprotect(challenge.Secret);}catch(CryptographicException){return Results.Unauthorized();}
  if(!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected),Encoding.UTF8.GetBytes(i.Code)))return Results.Unauthorized();
  if(await db.Codes.Where(x=>x.Email==email&&x.Challenge==i.Challenge&&x.Issued>since).ExecuteDeleteAsync(ct)!=1)return Results.Unauthorized();
  c.Response.Cookies.Append(Cookie,dp.CreateProtector("UCALL.hours.session.v1").ToTimeLimitedDataProtector().Protect(email,TimeSpan.FromHours(8)),Options(c));return Results.Ok();
 }
 static bool Eligible(DevelopmentRequest r)=>r.Reference!=ReviewCatalogue.Test&&r.Reference!=ReviewCatalogue.Duplicate;
 static Triage? Entry(ReviewBinding? b)=>ReviewCatalogue.All.FirstOrDefault(x=>x.Key==b?.CatalogueKey);
 public static async Task AutoBind(RequestsDb db){
  var requests=(await db.Requests.AsNoTracking().ToListAsync()).Where(Eligible).Select(r=>(r,p:Input(r))).ToList();
  var existing=await db.Bindings.ToListAsync();
  foreach(var t in ReviewCatalogue.All){
   if(existing.Any(b=>b.CatalogueKey==t.Key))continue;
   var hits=requests.Where(x=>x.p.Area==t.Area&&(t.Reference!=null?x.r.Reference==t.Reference:ReviewCatalogue.Normal(x.p.Title)==ReviewCatalogue.Normal(t.Title))).ToList();
   if(hits.Count!=1)continue;var id=hits[0].r.Id;var b=existing.FirstOrDefault(x=>x.RequestId==id);
   if(b?.CatalogueKey!=null)continue;
   if(b==null){b=new(){RequestId=id};db.Bindings.Add(b);existing.Add(b);}b.CatalogueKey=t.Key;
  }
  await db.SaveChangesAsync();
 }
 static async Task<List<ReviewRow>> Rows(RequestsDb db,string? viewer,bool team){
  var bindings=await db.Bindings.AsNoTracking().ToDictionaryAsync(x=>x.RequestId);var areas=viewer==null?[]:await Areas(db,viewer);
  var reviews=await db.Reviews.AsNoTracking().ToDictionaryAsync(x=>x.RequestId);var rows=new List<ReviewRow>();
  foreach(var r in (await db.Requests.AsNoTracking().ToListAsync()).Where(Eligible)){
   var p=Input(r);bindings.TryGetValue(r.Id,out var b);if(!team&&!Allowed(viewer,Owner(b,p),areas,p.Area))continue;
   reviews.TryGetValue(r.Id,out var rev);var f=rev==null?null:Form(rev);bool final=rev?.State=="submitted"&&f!=null;
   rows.Add(new(r.Id,r.Reference,r.SubmittedAt,p.Title,p.Area,p.OwnerName,team?b?.Contact:null,team?p.OwnerEmail:null,Entry(b),rev?.State??"pending",f?.Development,f?.Mode,f?.Start,f?.End,team?f?.Assumptions:null,final?Hours.Values(f!.Today):null,final?Hours.Values(f!.After):null,final?Hours.Gains(f!):null,p.SavedHoursPerMonth,DevelopmentRequests.Effort(p),team?rev?.Actor:null,rev?.At));
  }
  return rows.OrderBy(x=>x.Triage?.Priority??4).ThenBy(x=>x.Area).ThenBy(x=>x.Title).ToList();
 }
 static async Task<IResult> Item(string id,HttpContext c,IDataProtectionProvider dp,RequestsDb db,SaveHours? save,bool team=false){
  var viewer=team?Staff(c):Viewer(c,dp);if(viewer==null)return Results.Unauthorized();
  var r=await db.Requests.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(r==null||!Eligible(r))return Results.NotFound();
  var p=Input(r);var b=await db.Bindings.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==id);
  if(!team&&!Allowed(viewer,Owner(b,p),await Areas(db,viewer),p.Area))return Results.NotFound();
  var rec=await db.Reviews.SingleOrDefaultAsync(x=>x.RequestId==id);
  if(save!=null){
   if(rec?.Version!=save.Version||rec?.State=="submitted"&&!save.Submit)return Results.Conflict(new{error="A revisão já foi actualizada. Reabre o pedido antes de guardar."});
   var err=Hours.Validate(save.Form,save.Submit);if(err!=null)return Bad(err);
   if(rec==null){rec=new(){RequestId=id};db.Reviews.Add(rec);}
   rec.Version=Guid.NewGuid().ToString("N");rec.At=Stamp;rec.Actor=viewer;rec.State=save.Submit?"submitted":"draft";rec.Payload=JsonSerializer.Serialize(save.Form,Json);
   db.ReviewEvents.Add(Event(id,viewer,rec.State,rec.Payload));try{await db.SaveChangesAsync();}catch(DbUpdateException){return Results.Conflict(new{error="A revisão mudou. Reabre o pedido."});}
  }
  // Do not disclose the public form's submission/idempotency key.
  return Results.Ok(new{r.Id,r.Reference,r.SubmittedAt,triage=Entry(b),original=new{p.Title,p.Area,p.OwnerName,p.OwnerEmail,p.Purpose,p.ProcessName,p.Description,p.Activities,p.InvolvedAreas,p.Inputs,p.Outputs,p.Tools,p.People,p.HoursPerPerson,p.ExecutionsPerMonth,p.Frequency,p.Costs,p.Proposal,p.ExpectedBenefit,p.SavedHoursPerMonth,p.FinancialBenefitPerMonth},baseline=DevelopmentRequests.Effort(p),directCost=DevelopmentRequests.Cost(p),form=rec==null?null:Form(rec),state=rec?.State??"pending",version=rec?.Version,at=rec?.At});
 }
 public static void Map(WebApplication app){
  var portal=app.MapGroup("/api/review");portal.MapPost("/code",Issue).RequireRateLimiting("login");portal.MapPost("/verify",Verify).RequireRateLimiting("login");
  portal.MapPost("/logout",(HttpContext c)=>{c.Response.Cookies.Delete(Cookie,Options(c));return Results.Ok();});
  portal.MapGet("/requests",async(HttpContext c,IDataProtectionProvider dp,RequestsDb db)=>{var viewer=Viewer(c,dp);return viewer==null?Results.Unauthorized():Results.Ok(new{email=viewer,rows=await Rows(db,viewer,false)});});
  portal.MapGet("/requests/{id}",(string id,HttpContext c,IDataProtectionProvider dp,RequestsDb db)=>Item(id,c,dp,db,null));
  portal.MapPut("/requests/{id}",(string id,SaveHours body,HttpContext c,IDataProtectionProvider dp,RequestsDb db)=>Item(id,c,dp,db,body));
  var tt=app.MapGroup("/api/review-team").RequireAuthorization(p=>p.RequireRole("leader","officer"));
  tt.MapGet("/",async(RequestsDb db,HttpContext c)=>Results.Ok(new{rows=await Rows(db,null,true),catalogue=ReviewCatalogue.All,levels=Hours.Levels,isLeader=c.User.IsInRole("leader"),delegates=c.User.IsInRole("leader")?await db.Delegates.AsNoTracking().OrderBy(x=>x.Area).ToListAsync():[]}));
  tt.MapGet("/requests/{id}",(string id,HttpContext c,IDataProtectionProvider dp,RequestsDb db)=>Item(id,c,dp,db,null,true));
  tt.MapPut("/requests/{id}/assignment",Assign).RequireAuthorization("Leader");
  tt.MapPost("/delegates",SetDelegate).RequireAuthorization("Leader");
  tt.MapDelete("/delegates",([Microsoft.AspNetCore.Mvc.FromBody] DelegateInput i, RequestsDb db,HttpContext c)=>Delegate(i,db,c,false)).RequireAuthorization("Leader");
  tt.MapPost("/reminders",Remind).RequireRateLimiting("login");
 }
 static async Task<IResult> Assign(string id,AssignmentInput i,RequestsDb db,HttpContext c){
  var r=await db.Requests.SingleOrDefaultAsync(x=>x.Id==id);if(r==null||!Eligible(r))return Results.NotFound();
  var contact=Clean(i.Contact);if(contact.Length>0&&!Mail(contact))return Bad("Indica um email individual válido.");
  var t=ReviewCatalogue.All.FirstOrDefault(x=>x.Key==i.CatalogueKey);if(i.CatalogueKey!=null&&(t==null||t.Area!=Input(r).Area||t.Reference!=null&&t.Reference!=r.Reference))return Bad("A linha de triagem não corresponde à área ou referência.");
  var b=await db.Bindings.FindAsync(id);if(b==null){b=new(){RequestId=id};db.Bindings.Add(b);}
  b.CatalogueKey=t?.Key;b.Contact=contact.Length==0?null:contact;db.ReviewEvents.Add(Event(id,Staff(c),"assignment",JsonSerializer.Serialize(i,Json)));
  try{await db.SaveChangesAsync();}catch(DbUpdateException){return Results.Conflict(new{error="Esta linha de triagem já está associada a outro pedido."});}return Results.Ok();
 }
 static Task<IResult> SetDelegate(DelegateInput i,RequestsDb db,HttpContext c)=>Delegate(i,db,c,true);
 static async Task<IResult> Delegate(DelegateInput i,RequestsDb db,HttpContext c,bool grant){
  var email=Clean(i.Email);if(!Mail(email)||!DevelopmentRequests.Areas.Contains(i.Area))return Bad("Indica o email individual e a área.");
  var row=await db.Delegates.FindAsync(email,i.Area);if(row==null&&grant)db.Delegates.Add(new(){Email=email,Area=i.Area});else if(row!=null&&!grant)db.Delegates.Remove(row);
  db.ReviewEvents.Add(Event(i.Area,Staff(c),grant?"grant-area":"revoke-area",email));await db.SaveChangesAsync();return Results.Ok();
 }
 static async Task<IResult> Remind(RequestsDb db,HttpContext c,IReviewMail smtp,ConfigurationStore cfg,CancellationToken ct){
  var pending=(await Rows(db,null,true)).Where(r=>r.State!="submitted").ToList();var grants=await db.Delegates.AsNoTracking().ToListAsync(ct);
  var recipients=pending.SelectMany(r=>grants.Where(d=>d.Area==r.Area).Select(d=>d.Email).Append(r.Contact)).OfType<string>().Distinct().ToList();
  var url=(await cfg.Get("app.publicUrl")).TrimEnd('/');if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https")return Bad("Configura o endereço HTTPS da plataforma em Configurações antes de enviar lembretes.");
  int sent=0,failed=0,skipped=0;
  foreach(var email in recipients){
   long now=Now;var claimed=await db.Database.ExecuteSqlInterpolatedAsync($"""
   INSERT INTO "ReviewPings" ("Email","Sent") VALUES ({email},{now}) ON CONFLICT ("Email") DO UPDATE SET "Sent"=excluded."Sent" WHERE "ReviewPings"."Sent"<={now-86400}
   """,ct);if(claimed==0){skipped++;continue;}
   try{await smtp.Send(email,"UCALL · Revisão de horas por concluir",$"Tens pedidos cuja revisão de horas ainda não foi submetida. Acede a {url}/pedidos/meus para concluir a revisão.",ct);sent++;}
   catch(Exception) when(!ct.IsCancellationRequested){failed++;await db.Pings.Where(x=>x.Email==email&&x.Sent==now).ExecuteDeleteAsync(ct);}
  }
  db.ReviewEvents.Add(Event("TT",Staff(c),"reminders",$"{sent}/{failed}/{skipped}"));await db.SaveChangesAsync(ct);return Results.Ok(new{sent,failed,skipped,withoutContact=pending.Count(r=>r.Contact==null&&!grants.Any(d=>d.Area==r.Area))});
 }
}
