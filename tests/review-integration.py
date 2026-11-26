"""HTTP access and review checks. Uses a fresh SQLite DB and a local SMTP sink only.
Run after dotnet build api/Ucall.Api.csproj: python tests/review-integration.py
"""
from pathlib import Path
import email, json, os, re, secrets, socket, socketserver, subprocess, tempfile, threading, time, uuid
from http.cookiejar import CookieJar
from urllib.request import build_opener, HTTPCookieProcessor, ProxyHandler, Request
from urllib.error import HTTPError, URLError
from email import policy

messages = {}
class Sink(socketserver.StreamRequestHandler):
    def handle(self):
        def reply(v): self.wfile.write((v+'\r\n').encode())
        reply('220 test.local'); recipient=''
        while line:=self.rfile.readline():
            s=line.decode(errors='replace').strip()
            if s.startswith(('EHLO','HELO')): reply('250 test.local')
            elif s.startswith('RCPT TO:'): recipient=s.split(':',1)[1].strip('<> ');reply('250 OK')
            elif s=='DATA':
                reply('354 mail'); parts=[]
                while (part:=self.rfile.readline()) not in (b'.\r\n',b''): parts.append(part)
                m=email.message_from_bytes(b''.join(parts),policy=policy.default)
                messages.setdefault(recipient,[]).append(m.get_body().get_content());reply('250 OK')
            elif s=='QUIT': reply('221 OK');return
            else: reply('250 OK')

class Client:
    def __init__(self): self.http=build_opener(ProxyHandler({}),HTTPCookieProcessor(CookieJar()))
    def call(self,route,method='GET',body=None,want=200):
        headers={}
        if method!='GET':headers={'X-CSRF-TOKEN':self.call('/csrf')['token'],'Content-Type':'application/json'}
        req=Request(base+'/api'+route, data=json.dumps(body).encode() if body is not None else None,headers=headers,method=method)
        try: resp=self.http.open(req,timeout=12); status=resp.status;raw=resp.read()
        except HTTPError as e:status=e.code;raw=e.read()
        assert status==want,(route,status,want,raw[:350]);return json.loads(raw) if raw else None

def req(owner,area,title):
    return dict(submissionKey=str(uuid.uuid4()),title=title,area=area,ownerName='Pessoa Teste',ownerEmail=owner,processName=title,description='Descrição suficiente.',activities=[dict(name='Preparar dados',area=area,sla='')],involvedAreas=[area],inputs='Dados',outputs='Lista',tools=['CRM'],people=8,hoursPerPerson=5,frequency='daily',executionsPerMonth=20,costs=[],proposal='Optimizar o processo.',expectedBenefit='Libertar mais tempo.',savedHoursPerMonth=20,purpose='internal')
def login(emailaddr):
    c=Client();ticket=c.call('/review/code','POST',{'email':emailaddr});code=re.search(r'\b(\d{6})\b',messages[emailaddr][-1]).group(1)
    v={'email':emailaddr,'challenge':ticket['challenge'],'code':code};c.call('/review/verify','POST',v);return c,v
def scenario(people,hours):
    return dict(executions=20,levels=[dict(people=people if i==6 else 0,hours=hours if i==6 else 0) for i in range(7)])
def run():
    anon=Client();tt=Client();owner='ana@example.invalid';other='pedro@example.invalid';director='dir@example.invalid'
    tt.call('/setup','POST',dict(token=token,email='leader@example.invalid',password='TestPass-Only!234'))
    tt.call('/settings','PUT',{'values':{'smtp.host':'127.0.0.1','smtp.port':str(smtp.server_address[1]),'smtp.tls':'false','smtp.from':'test@example.invalid'}})
    ids=[]
    for mail,area,title,key in [(owner,'DFC','Cobrança regular após vencimento das faturas','13'),(other,'DFC','Pagamentos a fornecedores ↔ faturas','14'),(other,'DP','Automatização do processo de Recrutamento','DP-RECRUTAMENTO')]:
        r=anon.call('/public/development-requests','POST',req(mail,area,title));team=tt.call('/review-team/');row=next(x for x in team['rows'] if x['reference']==r['reference']);ids.append(row['id'])
        tt.call('/review-team/requests/'+row['id']+'/assignment','PUT',dict(contact=mail,catalogueKey=key))
    tt.call('/review-team/delegates','POST',dict(email=director,area='DFC'))
    ana,v=login(owner);ana.call('/review/verify','POST',v,want=401)
    assert [r['id'] for r in ana.call('/review/requests')['rows']]==ids[:1]
    ana.call('/review/requests/'+ids[1],want=404)
    ana.call('/review-team/',want=401)
    stage=dict(today=scenario(8,4.5),after=scenario(5,3),assumptions='Estimativa.',development='active',mode='support',start='2026-11-16',end=None)
    payload=dict(form=stage,version=None,submit=False);ana.call('/review/requests/'+ids[1],'PUT',payload,want=404)
    lead,_=login(director);assert {r['id'] for r in lead.call('/review/requests')['rows']}==set(ids[:2])
    lead.call('/review/requests/'+ids[2],want=404)
    before=ana.call('/review/requests/'+ids[0]);assert before['baseline']==800 and before['original']['savedHoursPerMonth']==20
    draft=ana.call('/review/requests/'+ids[0],'PUT',payload);assert draft['state']=='draft'
    assert next(x for x in tt.call('/review-team/')['rows'] if x['id']==ids[0])['gain'] is None
    payload['submit']=True;lead.call('/review/requests/'+ids[0],'PUT',payload,want=409)
    payload['version']=draft['version'];saved=ana.call('/review/requests/'+ids[0],'PUT',payload);assert saved['original']==before['original']
    row=next(x for x in tt.call('/review-team/')['rows'] if x['id']==ids[0]);assert row['state']=='submitted' and sum(row['gain'])==420 and row['triage']['integratedInto']==12
    tt.call('/review-team/delegates','DELETE',dict(email=director,area='DFC'));lead.call('/review/requests/'+ids[0],want=404)
    tt.call('/review-team/requests/'+ids[0]+'/assignment','PUT',dict(contact=None,catalogueKey='13'));ana.call('/review/requests/'+ids[0],want=404)
    ana.call('/review/logout','POST');ana.call('/review/requests',want=401)
    print('PASS: mail OTP, replay denied, owner/same-area isolation, directors and revocation, TT, draft, optimistic conflict, immutable original, submitted gain and integrated priority.')
root=Path(__file__).resolve().parents[1]
dll=root/'api/bin/Debug/net8.0/Ucall.Api.dll'
with tempfile.TemporaryDirectory() as temp,socketserver.ThreadingTCPServer(('127.0.0.1',0),Sink) as smtp:
    threading.Thread(target=smtp.serve_forever,daemon=True).start()
    with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    base='http://127.0.0.1:'+str(port);token=secrets.token_urlsafe(32)
    env=dict(os.environ,ASPNETCORE_ENVIRONMENT='Development',ASPNETCORE_URLS=base,Storage__Root=temp,Database__Provider='Sqlite',Database__Initialize='true',Bootstrap__Token=token,Alerts__Disabled='true',App__PublicUrl='https://example.invalid',Logging__LogLevel__Default='Warning')
    with open(Path(temp)/'server.log','w',encoding='utf8') as log:
        app=subprocess.Popen(['dotnet',str(dll)],cwd=dll.parent,env=env,stdout=log,stderr=log,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        try:
            for _ in range(100):
                if app.poll() is not None: raise RuntimeError('Application closed: '+(Path(temp)/'server.log').read_text(errors='replace')[-2300:])
                try:Client().call('/setup');break
                except (URLError,ConnectionError):time.sleep(.1)
            run()
        finally:app.terminate();app.wait();smtp.shutdown()
