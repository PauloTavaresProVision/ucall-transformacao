using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ucall;

var temp=Directory.CreateTempSubdirectory("ucall-review-");var server=new MemoryServer();var mail=new Mailbox();
var app=await UcallHost.Build(["--environment=Development","--Storage:Root="+temp.FullName,"--Database:Provider=Sqlite","--Database:Initialize=true","--Alerts:Disabled=true","--Bootstrap:Token=only-for-test","--App:PublicUrl=https://ucall.example.invalid","--Logging:LogLevel:Default=Warning"],b=>{b.Services.AddSingleton<IServer>(server);b.Services.AddSingleton<IReviewMail>(mail);})??throw new Exception();
try {
 await app.StartAsync();var tt=new Client(server,1);var anon=new Client(server,2);var ana=new Client(server,3);var director=new Client(server,4);
 await tt.Post("/setup",new{token="only-for-test",email="lead@example.invalid",password="Test-Account!2026"});
 var cat=ReviewCatalogue.All;Assert(cat.Count(x=>x.Priority==1)==7&&cat.Count(x=>x.Priority==2)==5&&cat.Count(x=>x.Priority==3)==23&&cat.Select(x=>x.Key).Distinct().Count()==35,"catalogue");
 async Task<string> Create(string key,string email){
  var t=cat.Single(x=>x.Key==key);
  var o=new DevelopmentRequestInput{Title=t.Title,Area=t.Area,OwnerName="Responsável",OwnerEmail=email,SubmissionKey=Guid.NewGuid().ToString(),ProcessName=t.Title,Description="Descrição do processo.",Activities=[new("Actividade",t.Area,"")],InvolvedAreas=[t.Area],Inputs="Dados",Outputs="Lista",Tools=["CRM"],People=8,HoursPerPerson=5,Frequency="daily",ExecutionsPerMonth=20,Proposal="Optimização proposta.",ExpectedBenefit="Libertar mais tempo.",SavedHoursPerMonth=20};
  var receipt=await anon.Post("/public/development-requests",o);
  var id=(await tt.Get("/review-team/"))["rows"]!.AsArray().Single(r=>r!["reference"]!.ToString()==receipt["reference"]!.ToString())!["id"]!.ToString();
  await tt.Call("/review-team/requests/"+id+"/assignment","PUT",new{contact=email,catalogueKey=key});return id;
 }
 const string a="ana@example.invalid",b="pedro@example.invalid",d="direccao@example.invalid";var one=await Create("13",a);var two=await Create("14",b);var three=await Create("DP-RECRUTAMENTO",b);
 await tt.Post("/review-team/delegates",new{email=d,area="DFC"});
 async Task Login(Client client,string address){
  var n=mail.Sent.Count;await client.Post("/review/code",new{email=address});var reply=await client.Post("/review/code",new{email=address});
  Assert(mail.Sent.Count==n+1,"email cooldown");var code=Regex.Match(mail.Sent.Last().Body,@"\b\d{6}\b").Value;
  var verify=new{email=address,code,challenge=(string?)reply["challenge"]};await client.Post("/review/verify",verify);await client.Call("/review/verify","POST",verify,401);
 }
 await Login(ana,a);await Login(director,d);
 await ana.Get("/review-team/",401);await anon.Get("/review/requests",401);
 Assert((await ana.Get("/review/requests"))["rows"]!.AsArray().Count==1,"owner list");
 Assert((await director.Get("/review/requests"))["rows"]!.AsArray().Count==2,"director list");
 await ana.Get("/review/requests/"+two,404);await director.Get("/review/requests/"+three,404);
 var original=await ana.Get("/review/requests/"+one);
 Scenario S(int ppl,decimal hours)=>new(20,Enumerable.Range(0,7).Select(i=>i==6?new LevelHours(ppl,hours):new LevelHours(0,0)).ToArray());
 var form=new HoursForm(S(8,4.5m),S(5,3),"Teste.","active","support",new(2026,11,16),null);
 var payload=new SaveHours(form,null,false);
 await ana.Call("/review/requests/"+two,"PUT",payload,404);
 var draft=await ana.Call("/review/requests/"+one,"PUT",payload);Assert((string?)draft["state"]=="draft","draft");
 var before=(await tt.Get("/review-team/"))["rows"]!.AsArray().Single(x=>(string?)x!["id"]==one)!;Assert(before["gain"]==null,"draft excluded");
 await director.Call("/review/requests/"+one,"PUT",payload,409);
 var complete=payload with{Version=(string?)draft["version"],Submit=true};
 await ana.Call("/review/requests/"+one,"PUT",complete with{Form=form with{Start=null}},400);
 var done=await ana.Call("/review/requests/"+one,"PUT",complete);Assert(JsonNode.DeepEquals(done["original"],original["original"]),"immutable original");
 var row=(await tt.Get("/review-team/"))["rows"]!.AsArray().Single(x=>(string?)x!["id"]==one)!;
 Assert((decimal?)row["gain"]![6]==420&&(decimal?)row["baseline"]==800&&(decimal?)row["impact"]==20&&(int?)row["triage"]!["integratedInto"]==12,"reference gain");
 var r1=await tt.Post("/review-team/reminders",null);Assert((int?)r1["sent"]==2&&!mail.Sent.TakeLast(2).Any(x=>x.Recipient==a),"pending reminders only");
 var r2=await tt.Post("/review-team/reminders",null);Assert((int?)r2["sent"]==0&&(int?)r2["skipped"]==2,"one reminder per day");
 await tt.Call("/review-team/delegates","DELETE",new{email=d,area="DFC"});await director.Get("/review/requests/"+one,404);
 await tt.Call("/review-team/requests/"+one+"/assignment","PUT",new{contact="outra@example.invalid",catalogueKey="13"});await ana.Get("/review/requests/"+one,404);
 // Without an assignment, the email of the original submission has access.
 await tt.Call("/review-team/requests/"+one+"/assignment","PUT",new{contact=(string?)null,catalogueKey="13"});await ana.Get("/review/requests/"+one);
 await ana.Post("/review/logout",null);await ana.Get("/review/requests",401);
 using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<RequestsDb>();Assert(await db.ReviewEvents.CountAsync(x=>x.RequestId==one&&(x.Action=="draft"||x.Action=="submitted"))==2,"audit");await db.ReviewSchema();await ReviewApi.AutoBind(db);Assert(await db.Reviews.CountAsync()==1,"upgrade idempotent");}
 Console.WriteLine("PASS: real ASP.NET middleware/SQLite; OTP, replay, cooldown, access and revocation, draft, conflicts, audit, original, gain, priority, reminders.");
}finally{await app.StopAsync();await app.DisposeAsync();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();temp.Delete(true);}
static void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
class Mailbox:IReviewMail {public List<(string Recipient,string Body)> Sent=[];public Task Send(string recipient,string subject,string body,CancellationToken ct){Sent.Add((recipient,body));return Task.CompletedTask;}}
class BodyDetection(bool value):IHttpRequestBodyDetectionFeature{public bool CanHaveBody=>value;}
class MemoryServer:IServer{
 public IFeatureCollection Features{get;}=new FeatureCollection();public Func<IFeatureCollection,Task> Dispatch=null!;
 public MemoryServer()=>Features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
 public Task StartAsync<T>(IHttpApplication<T> app,CancellationToken ct) where T:notnull{Dispatch=async f=>{var context=app.CreateContext(f);try{await app.ProcessRequestAsync(context);}finally{app.DisposeContext(context,null);}};return Task.CompletedTask;}
 public void Dispose(){}public Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
class Client(MemoryServer server,int address){
 readonly Dictionary<string,string> cookies=new();static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
 public Task<JsonNode> Get(string path,int expected=200)=>Call(path,"GET",null,expected);
 public Task<JsonNode> Post(string path,object? data)=>Call(path,"POST",data);
 public async Task<JsonNode> Call(string path,string method,object? data,int expected=200){
  var request=new HttpRequestFeature{Method=method,Scheme="https",Protocol="HTTP/1.1",Path="/api"+path};request.Headers.Host="test.invalid";
  if(method!="GET"){request.Headers["X-CSRF-TOKEN"]=(string?)(await Get("/csrf"))["token"];if(data!=null){var bytes=JsonSerializer.SerializeToUtf8Bytes(data,Json);request.Body=new MemoryStream(bytes);request.Headers.ContentType="application/json";request.Headers.ContentLength=bytes.Length;}}
  request.Headers.Cookie=string.Join("; ",cookies.Select(x=>x.Key+"="+x.Value));var f=new FeatureCollection();var response=new HttpResponseFeature();var output=new MemoryStream();
  f.Set<IHttpRequestFeature>(request);f.Set<IHttpRequestBodyDetectionFeature>(new BodyDetection(data!=null));f.Set<IHttpResponseFeature>(response);f.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(output));f.Set<IHttpConnectionFeature>(new HttpConnectionFeature{RemoteIpAddress=IPAddress.Parse("127.0.0."+address)});
  await server.Dispatch(f);foreach(var v in response.Headers.SetCookie){var pair=v!.Split(';')[0].Split('=',2);if(pair[1].Length==0)cookies.Remove(pair[0]);else cookies[pair[0]]=pair[1];}
  var text=Encoding.UTF8.GetString(output.ToArray());if(response.StatusCode!=expected)throw new Exception($"{method} {path}: {response.StatusCode}, expected {expected}. {text}");return text.Length==0?new JsonObject():JsonNode.Parse(text)!;
 }
}
