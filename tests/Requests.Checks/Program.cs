using System.Reflection;
using Ucall;
using Microsoft.EntityFrameworkCore;
var i=new DevelopmentRequestInput{SubmissionKey=Guid.NewGuid().ToString(),Title="Processamento salarial",Area="DP",OwnerName="Teste de validação",OwnerEmail="test@example.invalid",ProcessName="Processamento salarial",Description="Consolidar dados de assiduidade",Activities=[new("Validar dados","DP","")],InvolvedAreas=["DP"],Inputs="Assiduidade",Outputs="Salários",Tools=["Primavera"],People=3,HoursPerPerson=4,Frequency="daily",ExecutionsPerMonth=20,Costs=[new("Licença",12000,"annual"),new("Pontual",9000,"once")],Proposal="Automatizar validações de informação",ExpectedBenefit="Reduzir erros e tempo de processamento",SavedHoursPerMonth=160};
var validator=typeof(DevelopmentRequests).GetMethod("Validate",BindingFlags.NonPublic|BindingFlags.Static)!;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
string? Validate()=>validator.Invoke(null,[i]) as string;
Check(Validate()==null,"Valid request rejected");Check(DevelopmentRequests.Effort(i)==240,"Effort");Check(DevelopmentRequests.Cost(i)==1000,"Annual cost and one-off exclusion");
i.SavedHoursPerMonth=241;Check(Validate()!=null,"Savings over baseline accepted");i.SavedHoursPerMonth=160;i.Area="INVALID";Check(Validate()!=null,"Invalid area accepted");i.Area="DP";i.People=0;Check(Validate()!=null,"Zero people accepted");i.People=3;i.InvolvedAreas=null!;Check(Validate()!=null,"Null areas accepted");i.InvolvedAreas=["DP"];i.Costs=[new("Invalid",-1,"monthly")];Check(Validate()!=null,"Negative cost accepted");
var path=Path.GetFullPath(".verification/requests-domain.db");
await using(var db=new RequestsDb(new DbContextOptionsBuilder<RequestsDb>().UseSqlite("Data Source="+path).Options)){await db.Upgrade();await db.Upgrade();var key=Guid.NewGuid().ToString();db.Requests.Add(new(){SubmissionKey=key,Reference="PD-"+key,Payload="{}",SubmittedAt=DateTimeOffset.UtcNow.ToString("O")});await db.SaveChangesAsync();Check(await db.Requests.AnyAsync(x=>x.SubmissionKey==key&&x.Status=="Submetido"),"Persistence or initial status");db.Requests.Add(new(){SubmissionKey=key,Reference="PD-DUP-"+key});try{await db.SaveChangesAsync();throw new Exception("Duplicate key allowed");}catch(DbUpdateException){}}
Console.WriteLine("PASS: server validation, effort, recurring costs, baseline bounds, schema upgrade twice, persistence, initial status, unique submission key.");

