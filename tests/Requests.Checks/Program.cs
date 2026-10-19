using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Ucall;
using Microsoft.EntityFrameworkCore;
var i=new DevelopmentRequestInput{SubmissionKey=Guid.NewGuid().ToString(),Title="Processamento salarial",Area="DP",OwnerName="Teste de validação",OwnerEmail="test@example.invalid",ProcessName="Processamento salarial",Description="Consolidar dados de assiduidade",Activities=[new("Validar dados","DP","")],InvolvedAreas=["DP"],Inputs="Assiduidade",Outputs="Salários",Tools=["Primavera"],People=3,HoursPerPerson=4,Frequency="daily",ExecutionsPerMonth=20,Costs=[new("Licença",12000,"annual"),new("Pontual",9000,"once")],Proposal="Automatizar validações de informação",ExpectedBenefit="Reduzir erros e tempo de processamento",SavedHoursPerMonth=160};
var validator=typeof(DevelopmentRequests).GetMethod("Validate",BindingFlags.NonPublic|BindingFlags.Static)!;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
string? Validate()=>validator.Invoke(null,[i]) as string;
Check(typeof(DevelopmentRequestInput).GetProperty("BilledOperation")==null&&typeof(DevelopmentRequestInput).GetProperty("UsesAi")==null&&typeof(DevelopmentRequestInput).GetProperty("PersonalData")==null,"Public input still accepts internal fields");
Check(Validate()==null,"Valid request rejected");Check(DevelopmentRequests.Effort(i)==240,"Effort");Check(DevelopmentRequests.Cost(i)==1000,"Annual cost and one-off exclusion");
i.InvolvedAreas=["DP","DSI"];Check(Validate()?.Contains("DSI")==true,"Missing DSI intervention accepted");i.Activities.Add(new("Dar apoio técnico","DSI",""));Check(Validate()==null,"Described DSI rejected");i.InvolvedAreas=["DP"];Check(Validate()!=null,"Unselected activity area accepted");i.Activities.RemoveAt(1);i.SavedHoursPerMonth=10;i.Frequency="weekly";i.ExecutionsPerMonth=4;Check(DevelopmentRequests.Effort(i)==48&&Validate()==null,"Weekly four executions");i.ExecutionsPerMonth=5;Check(DevelopmentRequests.Effort(i)==60,"Weekly five executions");i.ExecutionsPerMonth=4.33m;Check(DevelopmentRequests.Effort(i)==51.96m,"Weekly annual average");i.Frequency="daily";i.ExecutionsPerMonth=20;i.SavedHoursPerMonth=241;Check(Validate()!=null,"Savings over baseline accepted");i.SavedHoursPerMonth=160;i.Area="INVALID";Check(Validate()!=null,"Invalid area accepted");i.Area="DP";i.People=0;Check(Validate()!=null,"Zero people accepted");i.People=3;i.InvolvedAreas=null!;Check(Validate()!=null,"Null areas accepted");i.InvolvedAreas=["DP"];i.Costs=[new("Invalid",-1,"monthly")];Check(Validate()!=null,"Negative cost accepted");
var path=Path.GetFullPath(".verification/requests-domain.db");
await using(var db=new RequestsDb(new DbContextOptionsBuilder<RequestsDb>().UseSqlite("Data Source="+path).Options)){await db.Upgrade();await db.Upgrade();var key=Guid.NewGuid().ToString();db.Requests.Add(new(){SubmissionKey=key,Reference="PD-"+key,Payload="{}",SubmittedAt=DateTimeOffset.UtcNow.ToString("O")});await db.SaveChangesAsync();Check(await db.Requests.AnyAsync(x=>x.SubmissionKey==key&&x.Status=="Submetido"),"Persistence or initial status");var row=await db.Requests.SingleAsync(x=>x.SubmissionKey==key);
ClaimsPrincipal User(string role)=>new(new ClaimsIdentity([new(ClaimTypes.Role,role),new(ClaimTypes.NameIdentifier,"test-"+role)],"test"));
Check(!DevelopmentRequests.CanAssess(User("ce"))&&!DevelopmentRequests.CanAssess(User("guest"))&&DevelopmentRequests.CanAssess(User("officer")),"Assessment permissions");
Check(!await db.Assessments.AnyAsync(x=>x.RequestId==row.Id),"New request falsely marked assessed");
var denied=await DevelopmentRequests.SaveAssessment(row.Id,new(true,true,true,null),User("ce"),db,default);Check(denied is Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult,"CE can modify assessment");
Check(!await db.Assessments.AnyAsync(x=>x.RequestId==row.Id),"Forbidden assessment persisted");
var saved=await DevelopmentRequests.SaveAssessment(row.Id,new(true,null,false,null),User("officer"),db,default);Check((saved as IStatusCodeHttpResult)?.StatusCode==200,"Officer assessment failed");
var first=await db.Assessments.AsNoTracking().SingleAsync(x=>x.RequestId==row.Id);var assessed=JsonSerializer.Deserialize<RequestAssessmentInput>(first.Payload,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
Check(assessed.BilledOperation==true&&assessed.UsesAi==null&&assessed.PersonalData==false&&first.UpdatedBy=="test-officer","Assessment values or audit incorrect");
var conflict=await DevelopmentRequests.SaveAssessment(row.Id,new(false,false,false,null),User("leader"),db,default);Check((conflict as IStatusCodeHttpResult)?.StatusCode==409,"Stale assessment overwritten");
var revised=await DevelopmentRequests.SaveAssessment(row.Id,new(false,true,true,first.Version),User("leader"),db,default);Check((revised as IStatusCodeHttpResult)?.StatusCode==200,"Leader update failed");
var current=await db.Assessments.AsNoTracking().SingleAsync(x=>x.RequestId==row.Id);Check(current.Version!=first.Version&&current.UpdatedBy=="test-leader","Version or audit did not advance");
Check((await db.Requests.SingleAsync(x=>x.Id==row.Id)).Payload=="{}","Assessment changed applicant payload");
db.Requests.Add(new(){SubmissionKey=key,Reference="PD-DUP-"+key});try{await db.SaveChangesAsync();throw new Exception("Duplicate key allowed");}catch(DbUpdateException){}}
Console.WriteLine("PASS: server validation, effort, recurring costs, baseline bounds, schema upgrade twice, persistence, initial status, unique submission key.");

