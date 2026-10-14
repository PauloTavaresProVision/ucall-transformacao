using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Ucall;
public static class Seed {
 public static readonly string[] Roles=["leader","officer","guest","ce"];
 public static async Task Run(AppDb db,RoleManager<IdentityRole> roles){foreach(var role in Roles)if(!await roles.RoleExistsAsync(role))await roles.CreateAsync(new(role));if(await db.People.AnyAsync())return;
  db.People.AddRange(new Person{Id="paulo",Name="Paulo Tavares",Area="Chief Transformation Officer"},new Person{Id="milton",Name="Milton Ferreira",Area="Eficiência e finanças"},new Person{Id="ines",Name="Inês Menano",Area="Processos"},new Person{Id="aniceto",Name="Aniceto Tavares",Area="Tecnologia"},new Person{Id="carlos",Name="Carlos Amaral",Area="Oferta"},new Person{Id="allen",Name="Allen Kaputu",Area="Recrutamento"},new Person{Id="sandra",Name="Sandra Gonçalves",Area="Comunicação"},new Person{Id="debora",Name="Débora Brás",Area="Comunicação"},new Person{Id="paula",Name="Paula Oliveira",Area="Sócia / CE"},new Person{Id="ce",Name="Comissão Executiva",Area="Governança"},new Person{Id="seniores",Name="Perfis seniores",Area="A recrutar"});
  (string Title,string Owners,string Date,string Deliverable)[] rows=[
   ("Kick-off: mandato, cadência e tempo protegido","paulo","2026-10-16","Mandato aceite"),
   ("Perfis dos seniores, orçamento e via de recrutamento","paulo,allen,milton","2026-10-31","Perfis aprovados pela CE"),
   ("Inventário das soluções tecnológicas e peso na facturação","milton,aniceto","2026-10-31","Lista e % das vendas"),
   ("Abrir recrutamento dos dois perfis","allen","2026-11-15","Processo aberto"),
   ("Mapa de prioridades de IA por direcção","paulo","2026-11-30","Lista priorizada; CE filtra em 72h"),
   ("Baselines: rentabilidade, processos, automação e QM","milton,ines,aniceto","2026-12-15","Baselines fechadas"),
   ("Plano de automação do back-office com ROI","ines,aniceto","2026-12-15","Lista priorizada com ROI"),
   ("Apresentação do plano às equipas","paulo","2026-12-30","Sessão realizada"),
   ("3 a 5 hipóteses de produto com business case","paulo,milton,aniceto,carlos","2027-01-31","Hipóteses priorizadas"),
   ("Modelo de canibalização por cliente","milton","2027-02-28","Simulação por cliente"),
   ("Pricing v1 por resultados e KPIs","milton,carlos","2027-02-28","Modelo de pricing v1"),
   ("Escolha de clientes-piloto e propostas","paulo,carlos","2027-03-15","Propostas entregues"),
   ("Os dois seniores contratados","allen","2027-03-31","Contratos assinados"),
   ("Primeiros processos de back-office automatizados","aniceto,ines","2027-03-31","Ganhos medidos"),
   ("Checkpoint de liderança dos 6 meses","paula,paulo","2027-03-31","Revisão de prazos e papéis"),
   ("Pacote de produtos e pricing aprovados pela CE","paulo","2027-04-30","Catálogo v1 com preço"),
   ("Pilotos a correr com KPIs medidos","paulo,seniores","2027-08-31","Resultados dos pilotos"),
   ("Back-office: 10% automatizado e 15% optimizado","aniceto,ines","2027-06-30","Indicadores no dashboard"),
   ("Plano de transição da operação por cliente","paulo,milton,carlos","2027-08-31","Plano por cliente"),
   ("Plano de comunicação a clientes e colaboradores","sandra,debora","2027-08-31","Plano aprovado"),
   ("Gate de Lançamento","ce","2027-09-30","Decisão registada"),
   ("Lançamento comercial do pacote e pricing","carlos,paulo","2027-10-31","Oferta no mercado"),
   ("Início da automação facturada por cliente","aniceto,carlos","2027-11-30","Contratos migrados"),
   ("Fecho do ano: metas P3 cumpridas","ines,aniceto","2027-12-31","Metas P3 cumpridas"),
   ("Plano de escala 2028","paulo,sandra","2027-12-15","Plano 2028 aprovado")];
  string[] phases=["Arranque","Diagnóstico","Desenhar a oferta","Pilotar","Lançar"];
  for(int i=0;i<rows.Length;i++){var r=rows[i];var a=new WorkAction{MandateNumber=i+1,Title=r.Title,OwnerId=r.Owners.Split(',')[0],Participants=r.Owners,DueDate=DateOnly.Parse(r.Date),OriginalDueDate=DateOnly.Parse(r.Date),Deliverable=r.Deliverable,Phase=phases[i<3?0:i<8?1:i<16?2:i<21?3:4],Status=i<3?"Em curso":"Por iniciar",Progress=0,Priority=i switch {1 or 3 or 12=>4,9 or 10=>2,5 or 6 or 13 or 17 or 23=>3,18 or 19 or 20=>5,_=>1}};if(i==16)a.StartDate=new(2027,6,1);if(i==21)a.DeadlineLabel="Out 2027 · data operacional por confirmar";if(i==22){a.StartDate=new(2027,11,1);a.DeadlineLabel="A partir de Nov 2027 · prazo operacional provisório";}db.Actions.Add(a);}
  db.Kpis.AddRange(new Kpi{Code="automated",Name="Processos internos automatizados",Target=20,TargetDate=new(2027,12,31),InterimTarget=10,InterimDate=new(2027,6,30),OwnerId="aniceto",Calculated=true},new Kpi{Code="optimized",Name="Processos mapeados optimizados",Target=30,TargetDate=new(2027,12,31),InterimTarget=15,InterimDate=new(2027,6,30),OwnerId="ines",Calculated=true},new Kpi{Code="reduction",Name="Redução média de tempo/custo por processo optimizado",Target=5,TargetDate=new(2027,12,31),OwnerId="ines"},new Kpi{Code="products",Name="Produtos KPO/TX no catálogo com preço",Unit="produtos",Target=3,TargetDate=new(2027,4,30),OwnerId="paulo"},new Kpi{Code="pilots",Name="Pilotos concluídos com KPIs medidos",Unit="pilotos",Target=1,TargetDate=new(2027,8,31),OwnerId="paulo"},new Kpi{Code="revenue",Name="Receita de soluções tecnológicas",Unit="AKZ",TargetDate=new(2027,9,30),OwnerId="milton"},new Kpi{Code="ontime",Name="Acções cumpridas no prazo",Target=90,TargetDate=new(2027,12,31),OwnerId="paulo",Calculated=true});
  string[] criteria=["Catálogo com ≥3 produtos, preço e business case positivo","Pricing por resultados aprovado pela CE","≥1 piloto concluído com KPIs do cliente","Impacto na facturação simulado cliente a cliente","Plano de comunicação pronto","Os dois seniores em funções"];
  for(int i=0;i<6;i++)db.Gate.Add(new(){Number=i+1,Title=criteria[i],OwnerId=i==1||i==3?"milton":i==4?"sandra":i==5?"allen":"paulo"});
  db.Meetings.Add(new(){Title="Primeira reunião da Equipa de Transformação",ScheduledAt=new DateTimeOffset(2026,10,14,14,30,0,TimeSpan.FromHours(1)).ToUniversalTime(),Participants="paulo,milton,ines,aniceto",Agenda="Aceitação do mandato e ajustes\nResponsáveis e tempo protegido\nCadência de reuniões e actualizações\nDecisões e novas acções"});
  foreach(var (kind,label,before) in new[]{("deadline","Prazos próximos","7,3,1"),("overdue","Acções atrasadas",""),("weekly","Actualização semanal em falta",""),("decision","Decisões pendentes",""),("gate","Critérios do Gate pendentes","30,14,7"),("kpi","KPIs fora da trajectória","" )})db.AlertRules.Add(new(){Kind=kind,Label=label,DaysBefore=before});
  db.Settings.AddRange(new Setting{Key="openai.model",Value="gpt-4.1"},new Setting{Key="smtp.port",Value="587"},new Setting{Key="smtp.tls",Value="true"});await db.SaveChangesAsync();
 }
}
