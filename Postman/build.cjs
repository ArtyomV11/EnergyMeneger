const fs=require('fs'), path=require('path');
const root=__dirname;
const values={baseUrl:'https://localhost:7080',ownerLogin:'owner',ownerPassword:'',foremanLogin:'foreman3',foremanPassword:'',workerLogin:'worker3',workerPassword:'',runWriteTests:'false',runEquipmentTests:'false',ownerToken:'',foremanToken:'',workerToken:'',ownerId:'',foremanId:'',workerId:'',factoryId:'',substationId:'',workshop1Id:'',workshop2Id:'',workshop3Id:'',equipmentId:'',from:'',to:'',page:'1',size:'100',stationSnapshot:'',workshopSnapshot:'',equipmentSnapshot:'',foremanSnapshot:''};
const c={info:{_postman_id:'5d53d237-2d33-4bc9-9496-56b57b074bbc',name:'EnergyMeneger — 3 workshops, 300/100 kW',schema:'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',description:'Current repository: fixed 3 workshops at 100 kW each, substation 300 kW. All 7 controllers / 22 method-route combinations. No new objects or blocked/dismissed users. Fill all three passwords. Run folders in order. runWriteTests enables rejected writes and same-value updates (logs/history grow). runEquipmentTests additionally enables/disables the existing lathe, only when all equipment is initially off. Neither flag creates objects. Tests never change employee roles/status. One run uses 4 login attempts; allow 1 minute between repeated runs (limit 10/minute/IP).'},auth:{type:'bearer',bearer:[{key:'token',value:'{{ownerToken}}',type:'string'}]},item:[]};
const event=(listen,code)=>({listen,script:{type:'text/javascript',exec:code.split('\n')}});
let folder;
function group(name,flag){folder={name,item:[]};if(flag)folder.event=[event('prerequest',`if(pm.environment.get('${flag}')!=='true')pm.execution.skipRequest();`)];c.item.push(folder);}
const need=(...keys)=>keys.map(k=>`if(!pm.environment.get('${k}')){pm.execution.setNextRequest(null);pm.execution.skipRequest();throw new Error('Fill environment / run previous folders: ${k}');}`).join('\n');
function req(name,method,url,body,status=200,tests='',pre='',token='ownerToken'){
 const request={method,header:[],url:'{{baseUrl}}'+url,description:name};
 if(token===null)request.auth={type:'noauth'};else if(token!=='ownerToken')request.auth={type:'bearer',bearer:[{key:'token',value:'{{'+token+'}}',type:'string'}]};
 if(token!==null)pre=need(token)+'\n'+pre;
 if(body!==undefined){request.header=[{key:'Content-Type',value:'application/json'}];request.body={mode:'raw',raw:JSON.stringify(body,null,2).replace(/"({{workshop[123]Id}})"/g,'$1'),options:{raw:{language:'json'}}};pre+=`\nconst body=${JSON.stringify(body)};for(const [k,v] of Object.entries(body))if(typeof v==='string'){const resolved=pm.variables.replaceIn(v);body[k]=k==='workshopId'?Number(resolved):resolved;}pm.request.body.update(JSON.stringify(body));`;}
 const item={name,request,event:[event('test',`const expected=${status};pm.test('HTTP '+expected,()=>pm.expect(pm.response.code).eq(expected));if(pm.response.code!==expected)pm.execution.setNextRequest(null);\n`+(status>=400?"pm.test('Error message',()=>pm.expect(pm.response.json().message).to.be.a('string'));\n":'')+tests)]};
 if(pre)item.event.unshift(event('prerequest',pre));folder.item.push(item);return item;
}
const save=(key,field='id')=>`if(pm.response.code===200)pm.environment.set('${key}',pm.response.json().${field});`;
group('01 — Login existing Owner, Foreman, Worker');
for(const role of ['owner','foreman','worker']){
 let pre=need(role+'Login',role+'Password')+`\npm.environment.unset('${role}Token');`;
 if(role==='owner')pre=need('ownerLogin','ownerPassword','foremanLogin','foremanPassword','workerLogin','workerPassword')+`\nfor(const k of ${JSON.stringify(Object.keys(values).filter(k=>/Token$|Id$|Snapshot$/.test(k)))})pm.environment.unset(k);pm.environment.set('from',new Date(Date.now()-86400000).toISOString());pm.environment.set('to',new Date(Date.now()+60000).toISOString());`;
 req('Login '+role,'POST','/api/auth/login',{login:'{{'+role+'Login}}',password:'{{'+role+'Password}}'},200,`if(pm.response.code===200){const b=pm.response.json();pm.test('Role',()=>pm.expect(b.user.role).eq('${{owner:'Owner',foreman:'Foreman',worker:'Worker'}[role]}'));pm.environment.set('${role}Token',b.accessToken);pm.environment.set('${role}Id',b.user.id);pm.environment.set('${role}ExpiresAt',b.expiresAt);}`,pre,null);
}
group('02 — Discover real IDs and check clean topology');
req('Substation 300 kW','GET','/api/substation',undefined,200,save('substationId')+`if(pm.response.code===200){const b=pm.response.json();pm.environment.set('stationSnapshot',JSON.stringify({name:b.name,powerLimitKw:b.powerLimitKw}));pm.test('300 kW',()=>pm.expect(b.powerLimitKw).eq(300));}`);
req('Three workshops at 100 kW','GET','/api/workshops',undefined,200,`if(pm.response.code===200){const rows=pm.response.json();pm.test('Exactly 3 workshops',()=>pm.expect(rows).lengthOf(3));for(let i=1;i<=3;i++){const w=rows.find(w=>w.name==='Цех №'+i);pm.test('Workshop '+i+' at 100 kW',()=>{pm.expect(w).to.exist;pm.expect(w.powerLimitKw).eq(100);});if(w)pm.environment.set('workshop'+i+'Id',w.id);}}`);
req('Get workshop and snapshot','GET','/api/workshops/{{workshop3Id}}',undefined,200,`if(pm.response.code===200){const {name,powerLimitKw,simulationType,minPowerKw,maxPowerKw}=pm.response.json();pm.environment.set('workshopSnapshot',JSON.stringify({name,powerLimitKw,simulationType,minPowerKw,maxPowerKw}));}`,need('workshop3Id'));
req('Six original devices and snapshot','GET','/api/equipment',undefined,200,`if(pm.response.code===200){const rows=pm.response.json();pm.test('Exactly 6 Bootstrap devices',()=>{pm.expect(rows).lengthOf(6);pm.expect(rows.map(e=>e.name)).to.have.members(['Токарный станок','Фрезерный станок','Сверлильный станок','Компрессор','Вентиляция','Освещение']);});const e=rows.find(e=>e.name==='Токарный станок'&&e.workshopId===Number(pm.environment.get('workshop3Id')));pm.test('Lathe exists',()=>pm.expect(e).to.exist);if(e){pm.environment.set('equipmentId',e.id);const {workshopId,name,minPowerKw,maxPowerKw,priority,allowAutoShutdown}=e;pm.environment.set('equipmentSnapshot',JSON.stringify({workshopId,name,minPowerKw,maxPowerKw,priority,allowAutoShutdown}));}}`);
req('Get original device','GET','/api/equipment/{{equipmentId}}',undefined,200,'',need('equipmentId'));
req('Equipment filtered by workshop and enabled','GET','/api/equipment?workshopId={{workshop3Id}}&enabled=false',undefined,200,`if(pm.response.code===200)pm.test('Filters',()=>pm.response.json().forEach(e=>{pm.expect(e.workshopId).eq(Number(pm.environment.get('workshop3Id')));pm.expect(e.isEnabled).eq(false);}));`);
req('Three active users and assigned workshops','GET','/api/users',undefined,200,`if(pm.response.code===200){const rows=pm.response.json();pm.test('Three active roles',()=>{pm.expect(rows).lengthOf(3);pm.expect(rows.map(u=>u.role)).to.have.members(['Owner','Foreman','Worker']);rows.forEach(u=>{pm.expect(u.status).eq('Active');if(u.role!=='Owner')pm.expect(u.workshopId).eq(Number(pm.environment.get('workshop3Id')));});});const u=rows.find(u=>u.id===Number(pm.environment.get('foremanId')));if(u)pm.environment.set('foremanSnapshot',JSON.stringify({role:u.role,workshopId:u.workshopId,status:u.status}));}`);
req('Summary and factory ID','GET','/api/reports/summary',undefined,200,save('factoryId','factoryId'));
group('03 — Roles and cross-workshop access');
req('Anonymous denied','GET','/api/workshops',undefined,401,'','',null);
req('Invalid login denied','POST','/api/auth/login',{login:'missing_{{$guid}}',password:'InvalidPassword123!'},401,'','',null);
for(const role of ['foreman','worker']){
 req(role+' sees own workshop only','GET','/api/workshops',undefined,200,`if(pm.response.code===200)pm.test('Only assigned workshop',()=>{const rows=pm.response.json();pm.expect(rows).lengthOf(1);pm.expect(rows[0].id).eq(Number(pm.environment.get('workshop3Id')));});`,'',role+'Token');
 req(role+' reads own device','GET','/api/equipment/{{equipmentId}}',undefined,200,'','',role+'Token');
 req(role+' foreign workshop denied','GET','/api/workshops/{{workshop1Id}}',undefined,403,'','',role+'Token');
 req(role+' foreign equipment filter denied','GET','/api/equipment?workshopId={{workshop1Id}}',undefined,403,'','',role+'Token');
 for(const url of ['/api/users','/api/substation','/api/reports/summary','/api/logs'])req(role+' forbidden '+url,'GET',url,undefined,403,'','',role+'Token');
}
group('04 — History, statistics, logs and invalid input');
for(const scope of ['equipment','workshop','factory']){
 const id={equipment:'equipmentId',workshop:'workshop3Id',factory:'factoryId'}[scope];
 if(scope!=='factory')req(scope+' history','GET',`/api/reports/history?scope=${scope}&id={{${id}}}&from={{from}}&to={{to}}&page={{page}}&size={{size}}`,undefined,200,`if(pm.response.code===200)pm.test('HistoryPointDto[]',()=>{const rows=pm.response.json();pm.expect(rows).to.be.an('array');rows.forEach(p=>{pm.expect(p.objectId).eq(Number(pm.environment.get('${id}')));pm.expect(p.powerKw).at.least(0);});});`,need(id));
 req(scope+' statistics','GET',`/api/reports/statistics?scope=${scope}&id={{${id}}}&from={{from}}&to={{to}}`,undefined,200,`if(pm.response.code===200)pm.test('StatisticsDto',()=>{const b=pm.response.json();pm.expect(b.scope).eq('${scope}');pm.expect(b.objectId).eq(Number(pm.environment.get('${id}')));pm.expect(b.energyKwh).at.least(0);pm.expect(b.coveredSeconds).at.least(0);pm.expect(b.missingSeconds).at.least(0);});`,need(id));
}
req('Paged action logs','GET','/api/logs?page={{page}}&size={{size}}',undefined,200,`if(pm.response.code===200)pm.test('LogDto[]',()=>pm.expect(pm.response.json()).to.be.an('array'));`);
req('Invalid page','GET','/api/logs?page=0&size=100',undefined,400);
req('Invalid size','GET','/api/logs?page=1&size=1001',undefined,400);
req('History does not support factory scope','GET','/api/reports/history?scope=factory&id={{factoryId}}&from={{from}}&to={{to}}',undefined,400);
req('Reversed period','GET','/api/reports/statistics?scope=factory&id={{factoryId}}&from={{to}}&to={{from}}',undefined,400);
req('Missing workshop','GET','/api/workshops/2147483647',undefined,404);
req('Missing equipment','GET','/api/equipment/2147483647',undefined,404);
group('05 — Rejected writes and same-value updates','runWriteTests');
const workshop={name:'Цех №3',powerLimitKw:100,simulationType:'EquipmentBased',minPowerKw:null,maxPowerKw:null};
const equipment={workshopId:'{{workshop3Id}}',name:'Must not be created',minPowerKw:5,maxPowerKw:5,priority:'Critical',allowAutoShutdown:true};
req('Fourth workshop rejected','POST','/api/workshops',workshop,409);
req('Workshop deletion rejected','DELETE','/api/workshops/{{workshop3Id}}',undefined,409);
for(const limit of [99,101])req('Workshop limit '+limit+' rejected','PUT','/api/workshops/{{workshop3Id}}',{...workshop,powerLimitKw:limit},409);
for(const limit of [299,360])req('Station limit '+limit+' rejected','PUT','/api/substation',{name:'Главная подстанция',powerLimitKw:limit},409);
req('Workshop type cannot change','PUT','/api/workshops/{{workshop3Id}}',{...workshop,simulationType:'Virtual',minPowerKw:0,maxPowerKw:1},400);
req('Critical equipment cannot allow auto shutdown','POST','/api/equipment',equipment,400);
req('Virtual workshop rejects equipment','POST','/api/equipment',{...equipment,workshopId:'{{workshop1Id}}',priority:'Secondary'},400);
req('Missing equipment deletion','DELETE','/api/equipment/2147483647',undefined,404);
req('Cannot create another Owner','POST','/api/users',{login:'unused',password:'UnusedPassword123!',role:'Owner',workshopId:'{{workshop3Id}}'},400);
req('Cannot modify Owner','PUT','/api/users/{{ownerId}}',{role:'Worker',status:'Blocked',workshopId:'{{workshop3Id}}'},403);
for(const role of ['foreman','worker']){
 req(role+' cannot create equipment','POST','/api/equipment',{...equipment,priority:'Secondary'},403,'','',role+'Token');
 req(role+' cannot create employee','POST','/api/users',{login:'unused',password:'UnusedPassword123!',role:'Worker',workshopId:'{{workshop3Id}}'},403,'','',role+'Token');
}
req('Worker cannot enable','POST','/api/equipment/{{equipmentId}}/enable',undefined,403,'','','workerToken');
req('Worker cannot disable','POST','/api/equipment/{{equipmentId}}/disable',undefined,403,'','','workerToken');
// These requests send precisely the stored configuration, not a guessed/default value.
for(const [label,url,snapshot,body] of [
 ['Station','/api/substation','stationSnapshot',{name:'snapshot',powerLimitKw:300}],
 ['Workshop','/api/workshops/{{workshop3Id}}','workshopSnapshot',workshop],
 ['Equipment','/api/equipment/{{equipmentId}}','equipmentSnapshot',equipment],
 ['Foreman','/api/users/{{foremanId}}','foremanSnapshot',{role:'Foreman',workshopId:'{{workshop3Id}}',status:'Active'}]
]){
 const i=req(label+' same-value update','PUT',url,body,200,`if(pm.response.code===200)pm.test('Configuration preserved',()=>{const expected=JSON.parse(pm.environment.get('${snapshot}'));const actual=pm.response.json();for(const [k,v]of Object.entries(expected))pm.expect(actual[k]).deep.eq(v);});`,need(snapshot));
 i.event.find(e=>e.listen==='prerequest').script.exec.push(`pm.request.body.update(pm.environment.get('${snapshot}'));`);
}
group('06 — Optional enable/disable existing lathe','runEquipmentTests');
req('Preflight: every device must be off','GET','/api/equipment',undefined,200,`if(pm.response.code===200){const rows=pm.response.json();const ok=rows.length===6&&rows.every(e=>!e.isEnabled&&e.currentPowerKw===0);pm.test('All six devices initially off',()=>pm.expect(ok).eq(true));if(!ok){pm.execution.setNextRequest(null);throw new Error('Equipment tests require all six devices initially off; no switch was sent.');}}`);
req('Foreman enables lathe','POST','/api/equipment/{{equipmentId}}/enable',undefined,200,`if(pm.response.code===200)pm.test('SwitchResultDto enabled',()=>{const b=pm.response.json();pm.expect(b.success).eq(true);pm.expect(b.equipment.isEnabled).eq(true);pm.expect(b.automaticallyDisabledIds).deep.eq([]);});`,'','foremanToken');
req('Owner enables again (idempotent)','POST','/api/equipment/{{equipmentId}}/enable',undefined,200,`if(pm.response.code===200)pm.test('Still enabled',()=>pm.expect(pm.response.json().equipment.isEnabled).eq(true));`);
req('Foreman disables lathe','POST','/api/equipment/{{equipmentId}}/disable',undefined,200,`if(pm.response.code===200)pm.test('Restored off state',()=>{const e=pm.response.json().equipment;pm.expect(e.isEnabled).eq(false);pm.expect(e.currentPowerKw).eq(0);});`,'','foremanToken');
req('Owner disables again (idempotent)','POST','/api/equipment/{{equipmentId}}/disable',undefined,200);
group('07 — Final topology');
req('Still three workshops at 100 kW','GET','/api/workshops',undefined,200,`if(pm.response.code===200)pm.test('Three at 100',()=>{const rows=pm.response.json();pm.expect(rows).lengthOf(3);rows.forEach(w=>pm.expect(w.powerLimitKw).eq(100));});`);
req('Still three active roles','GET','/api/users',undefined,200,`if(pm.response.code===200)pm.test('No extra/blocked users',()=>{const rows=pm.response.json();pm.expect(rows).lengthOf(3);pm.expect(rows.map(u=>u.role)).to.have.members(['Owner','Foreman','Worker']);rows.forEach(u=>pm.expect(u.status).eq('Active'));});`);
req('Still six devices','GET','/api/equipment',undefined,200,`if(pm.response.code===200)pm.test('No new devices',()=>pm.expect(pm.response.json()).lengthOf(6));`);
req('Still 300 kW','GET','/api/substation',undefined,200,`if(pm.response.code===200)pm.test('300 kW',()=>pm.expect(pm.response.json().powerLimitKw).eq(300));`);
const env={id:'63dc6e50-2d56-47b2-874e-e2ba811ab7fc',name:'EnergyMeneger localhost 7080 — fixed layout',values:Object.entries(values).map(([key,value])=>({key,value,type:/Password|Token/.test(key)?'secret':'default',enabled:true})),_postman_variable_scope:'environment',_postman_exported_at:new Date().toISOString()};
for(const [file,obj]of [['EnergyMeneger.postman_collection.json',c],['EnergyMeneger.postman_environment.json',env]])fs.writeFileSync(path.join(root,file),JSON.stringify(obj,null,2)+'\n');
const seed={source:'Current local repository — fixed layout',factory:{name:'Учебный завод'},substation:{name:'Главная подстанция',powerLimitKw:300},workshops:[{name:'Цех №1',powerLimitKw:100,simulationType:'Virtual',minPowerKw:30,maxPowerKw:70},{name:'Цех №2',powerLimitKw:100,simulationType:'Virtual',minPowerKw:40,maxPowerKw:90},{name:'Цех №3',powerLimitKw:100,simulationType:'EquipmentBased',minPowerKw:null,maxPowerKw:null}],equipment:[['Токарный станок',20,45,'Important',true],['Фрезерный станок',25,55,'Important',true],['Сверлильный станок',5,15,'Secondary',true],['Компрессор',15,35,'Secondary',true],['Вентиляция',5,12,'Critical',false],['Освещение',3,8,'Critical',false]].map(([name,minPowerKw,maxPowerKw,priority,allowAutoShutdown])=>({name,minPowerKw,maxPowerKw,priority,allowAutoShutdown,workshopName:'Цех №3',isEnabled:false,currentPowerKw:0})),users:[{role:'Owner',loginSource:'Bootstrap:Login',passwordSource:'Bootstrap:Password'},{role:'Foreman',defaultLogin:'foreman3',loginSource:'Bootstrap:ForemanLogin',passwordSource:'Bootstrap:ForemanPassword',workshopName:'Цех №3'},{role:'Worker',defaultLogin:'worker3',loginSource:'Bootstrap:WorkerLogin',passwordSource:'Bootstrap:WorkerPassword',workshopName:'Цех №3'}]};
fs.writeFileSync(path.join(root,'Bootstrap.reference.json'),JSON.stringify(seed,null,2)+'\n');
let doc='# Реальные DTO текущего репозитория\n\nJSON: camelCase, enum — строки. Пароли берутся из ваших настроек Bootstrap.\n';
for(const f of fs.readdirSync(path.join(root,'..','DTos')).filter(f=>f.endsWith('.cs'))){const s=fs.readFileSync(path.join(root,'..','DTos',f),'utf8');doc+='\n## '+f+'\n\n```csharp\n'+s.slice(s.indexOf('public sealed record')).trim()+'\n```\n';}
fs.writeFileSync(path.join(root,'DTO-reference.md'),doc);
console.log('Generated',c.item.reduce((n,f)=>n+f.item.length,0),'requests');
