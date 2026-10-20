using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Ucall;

public class DevelopmentRequest {
 public string Id { get; set; } = Guid.NewGuid().ToString("N");
 public string SubmissionKey { get; set; } = "";
 public string Reference { get; set; } = "";
 public string SubmittedAt { get; set; } = "";
 public string Status { get; set; } = "Submetido";
 public string Payload { get; set; } = "";
}
public class RequestAssessment {
 public string RequestId { get; set; }="";
 public string Payload { get; set; }="";
 public string Version { get; set; }=Guid.NewGuid().ToString("N");
 public string UpdatedAt { get; set; }="";
 public string UpdatedBy { get; set; }="";
}
public record RequestAssessmentInput(bool? BilledOperation,bool? UsesAi,bool? PersonalData,string? Version);
public class RequestsDb(DbContextOptions<RequestsDb> options) : DbContext(options) {
 public DbSet<DevelopmentRequest> Requests => Set<DevelopmentRequest>();
 public DbSet<RequestAssessment> Assessments => Set<RequestAssessment>();
 protected override void OnModelCreating(ModelBuilder b) {
  var e=b.Entity<DevelopmentRequest>();e.ToTable("DevelopmentRequests");e.HasKey(x=>x.Id);
  var a=b.Entity<RequestAssessment>();a.ToTable("DevelopmentRequestAssessments");a.HasKey(x=>x.RequestId);a.Property(x=>x.Version).IsConcurrencyToken();
  a.HasOne<DevelopmentRequest>().WithOne().HasForeignKey<RequestAssessment>(x=>x.RequestId).OnDelete(DeleteBehavior.Cascade);
  e.HasIndex(x=>x.SubmissionKey).IsUnique();e.HasIndex(x=>x.Reference).IsUnique();
 }
 public Task Upgrade(CancellationToken ct=default) => Database.ExecuteSqlRawAsync("""
  CREATE TABLE IF NOT EXISTS "DevelopmentRequests" (
   "Id" TEXT NOT NULL PRIMARY KEY, "SubmissionKey" TEXT NOT NULL,
   "Reference" TEXT NOT NULL, "SubmittedAt" TEXT NOT NULL,
   "Status" TEXT NOT NULL, "Payload" TEXT NOT NULL
  );
  CREATE TABLE IF NOT EXISTS "DevelopmentRequestAssessments" (
   "RequestId" TEXT NOT NULL PRIMARY KEY REFERENCES "DevelopmentRequests" ("Id") ON DELETE CASCADE,
   "Payload" TEXT NOT NULL, "Version" TEXT NOT NULL, "UpdatedAt" TEXT NOT NULL, "UpdatedBy" TEXT NOT NULL
  );
  CREATE UNIQUE INDEX IF NOT EXISTS "IX_DevelopmentRequests_SubmissionKey" ON "DevelopmentRequests" ("SubmissionKey");
  CREATE UNIQUE INDEX IF NOT EXISTS "IX_DevelopmentRequests_Reference" ON "DevelopmentRequests" ("Reference");
  """, ct);
}
public record RequestActivity(string Name,string Area,string Sla);
public record RequestCost(string Item,decimal Amount,string Period);
public class DevelopmentRequestInput {
 public string SubmissionKey { get; set; }="";
 public string Title { get; set; }=""; public string Area { get; set; }="";
 public string OwnerName { get; set; }=""; public string OwnerEmail { get; set; }="";
 public string ProcessName { get; set; }=""; public string Description { get; set; }="";
 public List<RequestActivity> Activities { get; set; }=[];
 public List<string> InvolvedAreas { get; set; }=[];
 public string Inputs { get; set; }=""; public string Outputs { get; set; }="";
 public List<string> Tools { get; set; }=[];
 public int People { get; set; } public decimal HoursPerPerson { get; set; }
 public string Frequency { get; set; }=""; public decimal ExecutionsPerMonth { get; set; }
 public List<RequestCost> Costs { get; set; }=[];
 public string Proposal { get; set; }=""; public string ExpectedBenefit { get; set; }="";
 public decimal? SavedHoursPerMonth { get; set; } public decimal? FinancialBenefitPerMonth { get; set; }
 public string Purpose { get; set; }="internal";
}
public static class DevelopmentRequests {
 public static readonly string[] Areas=["DP","DMKT","DMC","GGP","GCGR","DLP","DSI","DFC","GDN","Academia","DC","GIC"];
 static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
 static bool Team(ClaimsPrincipal u)=>u.IsInRole("leader")||u.IsInRole("officer")||u.IsInRole("ce");
 public static decimal Effort(DevelopmentRequestInput i)=>decimal.Round(i.People*i.HoursPerPerson*i.ExecutionsPerMonth,2);
 public static decimal Cost(DevelopmentRequestInput i)=>decimal.Round(i.Costs.Sum(c=>c.Amount*(c.Period switch{"monthly"=>1m,"annual"=>1m/12,"quarterly"=>1m/3,_=>0})),2);
 static string? Validate(DevelopmentRequestInput i) {
  if(!Guid.TryParse(i.SubmissionKey,out _))return "O identificador do pedido não é válido. Recarrega o formulário.";
  bool Text(string? s,int min,int max)=>s!=null&&s.Trim().Length>=min&&s.Length<=max;
  if(!Text(i.Title,5,180)||!Areas.Contains(i.Area)||!Text(i.OwnerName,3,120)||!Text(i.OwnerEmail,3,254)||!new EmailAddressAttribute().IsValid(i.OwnerEmail))return "Revê o título, a área e os contactos do responsável.";
  if(!Text(i.ProcessName,3,180)||!Text(i.Description,10,8000)||!Text(i.Inputs,2,4000)||!Text(i.Outputs,2,4000))return "Descreve o processo, os inputs e os outputs.";
  if(i.Activities==null||i.Activities.Count is <1 or >40||i.Activities.Any(a=>a==null||!Text(a.Name,2,500)||!Areas.Contains(a.Area)||a.Sla==null||a.Sla.Length>200))return "Indica pelo menos uma actividade com a respectiva área responsável.";
  if(i.InvolvedAreas==null||i.InvolvedAreas.Count is <1 or >12||i.InvolvedAreas.Any(a=>!Areas.Contains(a)))return "Selecciona as áreas intervenientes.";
  var missingArea=i.InvolvedAreas.FirstOrDefault(area=>!i.Activities.Any(a=>a.Area==area));
  if(missingArea!=null)return $"Qual é a intervenção da {missingArea} neste processo? Acrescenta uma actividade e selecciona {missingArea} como área responsável.";
  if(i.Activities.Any(a=>!i.InvolvedAreas.Contains(a.Area)))return "Selecciona todas as áreas responsáveis pelas actividades nas áreas intervenientes.";
  if(i.Tools==null||i.Tools.Count is <1 or >30||i.Tools.Any(t=>!Text(t,1,100)))return "Indica as ferramentas de suporte, ou escreve Sem ferramenta.";
  if(i.People is <1 or >10000||i.HoursPerPerson<=0||i.HoursPerPerson>744||i.ExecutionsPerMonth<=0||i.ExecutionsPerMonth>10000||!new[]{"daily","weekly","monthly","quarterly","annual","custom"}.Contains(i.Frequency))return "Revê pessoas, horas por execução e frequência mensal.";
  if(Effort(i)>744m*i.People)return "O esforço mensal ultrapassa as horas disponíveis por pessoa. Revê a duração e a frequência.";
  if(i.Costs==null||i.Costs.Count>30||i.Costs.Any(c=>c==null||!Text(c.Item,2,200)||c.Amount<0||c.Amount>1000000000000m||!new[]{"monthly","annual","quarterly","once"}.Contains(c.Period)))return "Revê os itens de custo, valores e periodicidade.";
  if(!Text(i.Proposal,10,8000)||!Text(i.ExpectedBenefit,10,8000)||!new[]{"internal","dual"}.Contains(i.Purpose))return "Descreve a melhoria e o benefício esperado e escolhe a finalidade.";
  if(i.SavedHoursPerMonth<0||i.FinancialBenefitPerMonth<0||i.FinancialBenefitPerMonth>1000000000000m||!(i.SavedHoursPerMonth>0||i.FinancialBenefitPerMonth>0))return "Quantifica pelo menos um benefício: horas por mês ou AKZ por mês.";
  if(i.SavedHoursPerMonth>Effort(i))return "A redução estimada de horas não pode ultrapassar o esforço actual.";
  return null;
 }
 static object Receipt(DevelopmentRequest r)=>new{r.Reference,r.SubmittedAt,r.Status};
 static object Summary(DevelopmentRequest r) {
  var p=JsonSerializer.Deserialize<DevelopmentRequestInput>(r.Payload,Json)!;
  return new{r.Id,r.Reference,r.SubmittedAt,r.Status,p.Title,p.Area,p.OwnerName,p.Purpose,p.SavedHoursPerMonth,p.FinancialBenefitPerMonth};
 }
 public static bool CanAssess(ClaimsPrincipal u)=>u.IsInRole("leader")||u.IsInRole("officer");
 static object? AssessmentView(RequestAssessment? a) {
  if(a==null)return null;
  var p=JsonSerializer.Deserialize<RequestAssessmentInput>(a.Payload,Json)!;
  return new{p.BilledOperation,p.UsesAi,p.PersonalData,a.Version,a.UpdatedAt,a.UpdatedBy};
 }
 public static async Task<IResult> SaveAssessment(string id,RequestAssessmentInput input,ClaimsPrincipal u,RequestsDb db,CancellationToken ct) {
  if(!CanAssess(u))return Results.Forbid();
  if(!await db.Requests.AnyAsync(x=>x.Id==id,ct))return Results.NotFound();
  var current=await db.Assessments.SingleOrDefaultAsync(x=>x.RequestId==id,ct);
  if(current?.Version!=input.Version)return Results.Conflict(new{error="O enquadramento foi alterado por outra pessoa. Fecha e volta a abrir o pedido antes de guardar."});
  var a=current??new RequestAssessment{RequestId=id};
  a.Payload=JsonSerializer.Serialize(input with{Version=null},Json);
  a.UpdatedAt=DateTimeOffset.UtcNow.ToString("O");a.UpdatedBy=u.FindFirstValue(ClaimTypes.NameIdentifier)??u.Identity?.Name??"equipa";a.Version=Guid.NewGuid().ToString("N");
  if(current==null)db.Assessments.Add(a);
  try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){return Results.Conflict(new{error="O enquadramento foi alterado por outra pessoa. Fecha e volta a abrir o pedido antes de guardar."});}
  return Results.Ok(AssessmentView(a));
 }
 public static void Map(WebApplication app) {
  app.MapPut("/api/development-requests/{id}/assessment",SaveAssessment).RequireAuthorization();
  app.MapGet("/api/public/development-requests/areas",()=>Results.Ok(Areas));
  app.MapPost("/api/public/development-requests",async(DevelopmentRequestInput input,RequestsDb db,HttpRequest http,CancellationToken ct)=>{
   if(http.ContentLength>131072)return Results.BadRequest(new{error="O pedido excede o tamanho permitido."});
   var error=Validate(input);if(error!=null)return Results.BadRequest(new{error});
   input.SubmissionKey=Guid.Parse(input.SubmissionKey).ToString("N");
   var existing=await db.Requests.SingleOrDefaultAsync(x=>x.SubmissionKey==input.SubmissionKey,ct);
   var payload=JsonSerializer.Serialize(input,Json);
   if(existing!=null)return existing.Payload==payload?Results.Ok(Receipt(existing)):Results.Conflict(new{error="Este pedido já foi enviado. Inicia um novo pedido para submeter outra proposta."});
   var r=new DevelopmentRequest{SubmissionKey=input.SubmissionKey,SubmittedAt=DateTimeOffset.UtcNow.ToString("O"),Payload=payload};
   r.Reference="PD-"+DateTime.UtcNow.ToString("yyyyMMdd")+"-"+r.Id[..10].ToUpperInvariant();
   db.Requests.Add(r);
   try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){db.ChangeTracker.Clear();existing=await db.Requests.SingleOrDefaultAsync(x=>x.SubmissionKey==input.SubmissionKey,ct);if(existing!=null&&existing.Payload==payload)return Results.Ok(Receipt(existing));throw;}
   return Results.Ok(Receipt(r));
  }).RequireRateLimiting("request-submit");
  app.MapGet("/api/development-requests",async(ClaimsPrincipal u,RequestsDb db)=>{
   if(!Team(u))return Results.Forbid();
   var rows=await db.Requests.AsNoTracking().OrderByDescending(x=>x.SubmittedAt).Take(200).ToListAsync();
   return Results.Ok(new{total=await db.Requests.CountAsync(),items=rows.Select(Summary)});
  }).RequireAuthorization();
  app.MapGet("/api/development-requests/{id}",async(string id,ClaimsPrincipal u,RequestsDb db)=>{
   if(!Team(u))return Results.Forbid();var r=await db.Requests.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
   var assessment=r==null?null:await db.Assessments.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==id);
   return r==null?Results.NotFound():Results.Ok(new{r.Id,r.Reference,r.SubmittedAt,r.Status,assessment=AssessmentView(assessment),canEditAssessment=CanAssess(u),data=JsonSerializer.Deserialize<DevelopmentRequestInput>(r.Payload,Json),effortHoursPerMonth=Effort(JsonSerializer.Deserialize<DevelopmentRequestInput>(r.Payload,Json)!),directCostPerMonth=Cost(JsonSerializer.Deserialize<DevelopmentRequestInput>(r.Payload,Json)!)});
  }).RequireAuthorization();
 }
}

