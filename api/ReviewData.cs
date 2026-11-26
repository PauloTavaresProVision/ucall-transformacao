using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Ucall;
public record Triage(string Key,int Priority,string Area,string Title,string Note="",int? IntegratedInto=null,string? Reference=null) {
 public string Decision => Priority switch {1=>"A Equipa de Transformação vai desenvolver",2=>"A avaliar com a área",_=>"Desenvolvimento autónomo pela área"};
}
public static class ReviewCatalogue {
 public static readonly Triage[] All=[
 new("12",1,"GDN","CRM comercial e de gestão de clientes","Resolve o pedido 13. O 2 poderá integrar o CRM mais tarde.",null,"PD-20261030-94D8F5186E"),
 new("13",1,"DFC","Cobrança regular após vencimento das faturas","Integrado no pedido 12.",12),
 new("15",1,"DFC","Lançamentos de faturas de fornecedores a partir das digitalizações"),
 new("16",1,"DP","Indicadores e bases de dados do R&S"),
 new("27",1,"DLP","Manutenção Inovadora","Inclui o pedido 31."),
 new("31",1,"DLP","Frota Digital","Integrado no pedido 27.",27),
 new("33",1,"DSI","Linhas da Ucall altamente tecnológicas","Em curso."),
 new("22",2,"Academia","Automatização do Plano Macro","Output nos drivers de custo."),
 new("23",2,"Academia","Automatização do processo de formação Mycall"),
 new("20",2,"GCGR","Knowledge Controlo de Gestão"),
 new("28",2,"GGP","Projetos Ucall numa BD com indicadores de reporting","Bitrix: manter ou substituir."),
 new("29",2,"GGP","Gestão das reuniões de Gestão","Aprimorar o Meetsense."),
 new("24",3,"Academia","Automatização dos processos U-know"),
 new("5",3,"DC","Motivos de chamadas dia a dia, com comparativo"),
 new("6",3,"DC","Extração das vendas na GoContact"),
 new("7",3,"DC","Carregamento de BDs para outbounds"),
 new("8",3,"DC","Processos de automatização DC (monitorias)"),
 new("9",3,"DC","Estado das bases de dados de novas entradas","Avança a seguir ao Recrutamento (DP)."),
 new("10",3,"DC","Plataforma de gestão de férias","Fica o registo das 08:29.",null,"PD-20261030-F0B0345FE9"),
 new("DC-TICKETS",3,"DC","Relatório de desempenho nas filas de Tickets","Fora do PdS; autónomo."),
 new("11",3,"DC","Controlo do dimensionamento","PdS: igual ao 1 (GCGR)."),
 new("DP-RECRUTAMENTO",3,"DP","Automatização do processo de Recrutamento","Fora do PdS; avança com o pedido 9."),
 new("14",3,"DFC","Pagamentos a fornecedores ↔ faturas"),
 new("2",3,"DMKT","Parabéns a clientes e alerta de aniversários","A DMKT avança já. Poderá integrar o CRM (12)."),
 new("3",3,"DMKT","Apresentações com os templates da empresa","Ferramenta já criada."),
 new("21",3,"DMKT","Produção editorial e gráfica da revista Logados"),
 new("17",3,"DMC","Bónus de Qualidade ACC + Bonificação TG Qualidade"),
 new("25",3,"DMC","Relatórios mensais de Qualidade","Em desenvolvimento."),
 new("32",3,"DMC","Estudos de Experiência de Cliente","Já concluído."),
 new("1",3,"GCGR","Alertas de erros nos dados do ELO (assiduidades)"),
 new("4",3,"GCGR","Exportações de CSVs das plataformas"),
 new("18",3,"GCGR","Budget / Forecast"),
 new("19",3,"GCGR","Apuramento de rentabilidades"),
 new("26",3,"GGP","Objetivos de Direção anuais e KPIs"),
 new("30",3,"DLP","Stock Digital","Só reporting.")];
 public const string Test="PD-20261020-3956FEC098",Duplicate="PD-20261030-DA69FF3C75",Survivor="PD-20261030-F0B0345FE9";
 public static string Normal(string s)=>new(s.Normalize(NormalizationForm.FormD).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
public class ReviewBinding {
 public string RequestId{get;set;}="";public string? CatalogueKey{get;set;} public string? Contact{get;set;}
}
public class AreaDelegate {public string Email{get;set;}=""; public string Area{get;set;}="";}
public class ReviewRecord {
 public string RequestId{get;set;}="";public string Payload{get;set;}="";public string State{get;set;}=""; public string Version{get;set;}=""; public string Actor{get;set;}="";public string At{get;set;}="";
}
public class ReviewEvent {public string Id{get;set;}=Guid.NewGuid().ToString("N");public string RequestId{get;set;}="";public string Actor{get;set;}="";public string At{get;set;}="";public string Action{get;set;}="";public string Data{get;set;}="";}
public class ReviewCode {public string Email{get;set;}="";public string Challenge{get;set;}="";public string Secret{get;set;}="";public long Issued{get;set;} public int Attempts{get;set;}}
public class ReviewPing {public string Email{get;set;}="";public long Sent{get;set;}}
public partial class RequestsDb {
 public DbSet<ReviewBinding> Bindings=>Set<ReviewBinding>();public DbSet<AreaDelegate> Delegates=>Set<AreaDelegate>();public DbSet<ReviewRecord> Reviews=>Set<ReviewRecord>();public DbSet<ReviewEvent> ReviewEvents=>Set<ReviewEvent>();public DbSet<ReviewCode> Codes=>Set<ReviewCode>();public DbSet<ReviewPing> Pings=>Set<ReviewPing>();
 static void ReviewModel(ModelBuilder b){
  b.Entity<ReviewBinding>().ToTable("ReviewBindings").HasKey(x=>x.RequestId);b.Entity<ReviewBinding>().HasIndex(x=>x.CatalogueKey).IsUnique();
  b.Entity<ReviewRecord>().ToTable("ReviewRecords").HasKey(x=>x.RequestId);b.Entity<ReviewRecord>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<AreaDelegate>().ToTable("ReviewDelegates").HasKey(x=>new{x.Email,x.Area});
  b.Entity<ReviewEvent>().ToTable("ReviewEvents").HasKey(x=>x.Id);b.Entity<ReviewCode>().ToTable("ReviewCodes").HasKey(x=>x.Email);b.Entity<ReviewPing>().ToTable("ReviewPings").HasKey(x=>x.Email);
 }
 public Task ReviewSchema()=>Database.ExecuteSqlRawAsync("""
 CREATE TABLE IF NOT EXISTS "ReviewBindings" ("RequestId" TEXT PRIMARY KEY REFERENCES "DevelopmentRequests"("Id") ON DELETE CASCADE,"CatalogueKey" TEXT NULL UNIQUE,"Contact" TEXT NULL);
 CREATE TABLE IF NOT EXISTS "ReviewDelegates" ("Email" TEXT NOT NULL,"Area" TEXT NOT NULL,PRIMARY KEY ("Email","Area"));
 CREATE TABLE IF NOT EXISTS "ReviewRecords" ("RequestId" TEXT PRIMARY KEY REFERENCES "DevelopmentRequests"("Id") ON DELETE CASCADE,"Payload" TEXT NOT NULL,"State" TEXT NOT NULL,"Actor" TEXT NOT NULL,"At" TEXT NOT NULL,"Version" TEXT NOT NULL);
 CREATE TABLE IF NOT EXISTS "ReviewEvents" ("Id" TEXT PRIMARY KEY,"RequestId" TEXT NOT NULL,"Actor" TEXT NOT NULL,"At" TEXT NOT NULL,"Action" TEXT NOT NULL,"Data" TEXT NOT NULL);
 CREATE TABLE IF NOT EXISTS "ReviewCodes" ("Email" TEXT PRIMARY KEY,"Challenge" TEXT NOT NULL,"Secret" TEXT NOT NULL,"Issued" BIGINT NOT NULL,"Attempts" INTEGER NOT NULL);
 CREATE TABLE IF NOT EXISTS "ReviewPings" ("Email" TEXT PRIMARY KEY,"Sent" BIGINT NOT NULL);
 """);
}
public record LevelHours(int? People,decimal? Hours);
public record Scenario(decimal? Executions,LevelHours[] Levels);
public record HoursForm(Scenario Today,Scenario After,string? Assumptions,string? Development,string? Mode,DateOnly? Start,DateOnly? End);
public record SaveHours(HoursForm Form,string? Version,bool Submit);
public static class Hours {
 public static readonly string[] Levels=["Direcção (inclui subdirectores)","Gestão","Coordenação","Supervisão","Técnico Especialista","Técnico Sénior","Técnico"];
 public static string? Validate(HoursForm? f,bool full){
  if(f==null||f.Assumptions?.Length>4000)return "Revê os pressupostos (até 4 000 caracteres).";
  foreach(var s in new[]{f.Today,f.After}){
   if(s?.Levels==null||s.Levels.Length!=Levels.Length||s.Executions is <0 or >10000||full&&s.Executions is null)return "Indica as execuções mensais para os dois cenários.";
   foreach(var l in s.Levels)if(l==null||l.People is <0 or >10000||l.Hours is <0 or >744||full&&(l.People==null||l.Hours==null||l.People>0&&l.Hours==0||l.People==0&&l.Hours>0))return "Preenche os sete níveis. Usa zero quando o nível não intervém.";
  }
  if(full&&(f.Today.Executions<=0||!f.Today.Levels.Any(x=>x.People>0)))return "O cenário de hoje tem de incluir pelo menos uma pessoa e uma execução.";
  if(f.Development!=null&&f.Development is not ("pending" or "active" or "done")||full&&f.Development==null)return "Selecciona o estado do desenvolvimento.";
  if(f.Mode!=null&&f.Mode is not ("area" or "support"))return "Selecciona um modo válido.";
  if(f.Start>f.End||f.Development=="pending"&&(f.Mode!=null||f.Start!=null||f.End!=null)||f.Development=="active"&&f.End!=null)return "Revê o estado e as datas de desenvolvimento.";
  if(full&&(f.Development is "active" or "done")&&(f.Mode==null||f.Start==null||f.Development=="done"&&f.End==null))return "Indica o modo, a data de início e, se concluído, a data de conclusão.";
  return null;
 }
 public static decimal[] Values(Scenario s)=>s.Levels.Select(l=>(l.People??0)*(l.Hours??0)*(s.Executions??0)).ToArray();
 public static decimal[] Gains(HoursForm f)=>Values(f.Today).Zip(Values(f.After),(a,b)=>a-b).ToArray();
}
