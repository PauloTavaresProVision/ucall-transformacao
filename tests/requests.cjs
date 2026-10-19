const assert=require('node:assert/strict');
const base=process.env.TEST_URL||'http://127.0.0.1:5092';
class Client{cookies={};token='';async call(path,method='GET',body){const r=await fetch(base+'/api'+path,{method,headers:{'Content-Type':'application/json',Cookie:Object.entries(this.cookies).map(([k,v])=>k+'='+v).join('; '),...(this.token?{'X-CSRF-TOKEN':this.token}:{})},body:body===undefined?undefined:JSON.stringify(body)});for(const c of r.headers.getSetCookie()){const [k,v]=c.split(';')[0].split('=');this.cookies[k]=v;}const t=await r.text();return {status:r.status,data:t?JSON.parse(t):null}}async csrf(){this.token=(await this.call('/csrf')).data.token}}
(async()=>{const c=new Client();await c.csrf();const data={submissionKey:crypto.randomUUID(),title:'Simplificar processamento salarial',area:'DP',ownerName:'Responsável de teste',ownerEmail:'teste@example.invalid',processName:'Processamento salarial',description:'Consolidar e validar os dados recebidos.',activities:[{name:'Validar dados',area:'DP',sla:'2 dias'}],involvedAreas:['DP'],inputs:'Dados de assiduidade',outputs:'Processamento validado',tools:['Primavera'],people:3,hoursPerPerson:4,frequency:'daily',executionsPerMonth:20,costs:[{item:'Licença',amount:12000,period:'annual'},{item:'Configuração pontual',amount:9000,period:'once'}],proposal:'Automatizar a validação da informação recebida.',expectedBenefit:'Reduzir o tempo de processamento e os erros.',savedHoursPerMonth:160,financialBenefitPerMonth:null,purpose:'dual',billedOperation:true,usesAi:false,personalData:true,status:'Aprovado'};
assert.equal((await c.call('/development-requests')).status,401);
assert.equal((await c.call('/public/development-requests','POST',{...data,people:0})).status,400);
assert.equal((await c.call('/public/development-requests','POST',{...data,savedHoursPerMonth:241})).status,400);
assert.equal((await c.call('/public/development-requests','POST',{...data,area:'inventada'})).status,400);
const result=await c.call('/public/development-requests','POST',data);assert.equal(result.status,200);assert.equal(result.data.status,'Submetido');assert.match(result.data.reference,/^PD-/);
const repeat=await c.call('/public/development-requests','POST',data);assert.deepEqual(repeat.data,result.data);
assert.equal((await c.call('/public/development-requests','POST',{...data,title:'Pedido alterado depois do envio'})).status,409);
const noCsrf=new Client();assert.equal((await noCsrf.call('/public/development-requests','POST',{...data,submissionKey:crypto.randomUUID()})).status,400);
await c.call('/setup','POST',{email:'requests-admin@example.invalid',password:'Requests-Testing-2026!',token:'requests-test-only-2026'});await c.csrf();
const list=await c.call('/development-requests');assert.equal(list.status,200);const row=list.data.items.find(r=>r.reference===result.data.reference);assert.ok(row);
const detail=await c.call('/development-requests/'+row.id);assert.equal(detail.data.effortHoursPerMonth,240);assert.equal(detail.data.directCostPerMonth,1000);assert.equal(detail.data.data.ownerEmail,data.ownerEmail);assert.equal(detail.data.status,'Submetido');
assert.equal((await fetch(base+'/pedidos/novo')).status,200);
console.log('PASS: submission, validation, deduplication, CSRF, private inbox, stored detail, effort and cost calculations, direct URL.');
})().catch(e=>{console.error(e);process.exit(1)});

