const fs = require('fs');
const path = require('path');
const out = __dirname;
const sha = '26736fc7c5818ec494c610f3d52c6b3e5efad20c';
const vars = {baseUrl:'https://localhost:7080',ownerLogin:'',ownerPassword:'',runWriteTests:'false',runId:'',ownerToken:'',foremanToken:'',workerToken:'',ownerId:'',factoryId:'',substationId:'',seedWorkshop1Id:'',seedWorkshop2Id:'',seedWorkshop3Id:'',seedEquipmentId:'',testWorkshopId:'',testEquipmentId:'',secondaryId:'',importantId:'',criticalId:'',foremanId:'',workerId:'',foremanLogin:'',workerLogin:'',foremanPassword:'',workerPassword:'',stationName:'',stationOriginalLimit:'',stationTestLimit:'',from:'',to:'',page:'1',size:'100'};
const c = {info:{_postman_id:'5d53d237-2d33-4bc9-9496-56b57b074bbc',name:'EnergyMeneger main — all controllers',schema:'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',description:`Source: https://github.com/ArtyomV11/EnergyMeneger/commit/${sha}\n7 controllers; all 22 method/route combinations. Import environment; fill ownerLogin and ownerPassword from Bootstrap configuration. Run folders in order. Default: read-only plus negative authorization tests. Set runWriteTests=true for full isolated write scenario. It permanently adds a 60 kW workshop, raises substation limit by 60 kW and creates two employees; cleanup disables equipment and blocks employees. History prevents removal; original Bootstrap objects are not edited. Use a disposable database for a fully clean reset. Each full run uses new unique names. Login limiter: 10/minute/IP; one run uses fewer than 10 logins. Wait one minute before repeating. No refresh/register/delete-user endpoints exist.`},auth:{type:'bearer',bearer:[{key:'token',value:'{{ownerToken}}',type:'string'}]},item:[]};
const event=(listen,code)=>({listen,script:{type:'text/javascript',exec:code.split('\n')}});
let folder;
function group(name,write=false){folder={name,item:[]};if(write)folder.event=[event('prerequest',`if(pm.environment.get('runWriteTests') !== 'true') pm.execution.skipRequest();`)];c.item.push(folder);}
function req(name,method,url,body,status=200,tests='',pre='',token='ownerToken'){
 const request={method,header:[],url:'{{baseUrl}}'+url,description:name};
 if(body!==undefined){request.header.push({key:'Content-Type',value:'application/json'});request.body={mode:'raw',raw:JSON.stringify(body,null,2),options:{raw:{language:'json'}}};}
 if(token===null) request.auth={type:'noauth'};else if(token!=='ownerToken')request.auth={type:'bearer',bearer:[{key:'token',value:'{{'+token+'}}',type:'string'}]};
 const checks=`pm.test('HTTP ${JSON.stringify(status)}',()=>pm.expect(pm.response.code).to.be.oneOf(${JSON.stringify(Array.isArray(status)?status:[status])}));\nif(pm.response.code>=400) pm.test('ErrorDto',()=>pm.expect(pm.response.json().message).to.be.a('string'));\n`+tests;
 const bodyPre=body===undefined?'':`\nconst template=${JSON.stringify(body)};\nfor(const [key,value] of Object.entries(template)){if(typeof value==='string'){const v=pm.variables.replaceIn(value);template[key]=['workshopId','powerLimitKw'].includes(key)?Number(v):v;}}\npm.request.body.update(JSON.stringify(template));`;
 const item={name,request,event:[event('test',checks)]};if(pre||bodyPre)item.event.unshift(event('prerequest',pre+bodyPre));folder.item.push(item);return item;
}
const save=(key,field='id')=>`if(pm.response.code<300) pm.environment.set('${key}',pm.response.json().${field});`;
const need=(...keys)=>keys.map(k=>`if(!pm.environment.get('${k}')) { pm.execution.skipRequest(); throw new Error('Run previous folders: missing ${k}'); }`).join('\n');
const eq=(name,power,priority='Secondary',auto=true)=>({workshopId:'{{testWorkshopId}}',name:name+' {{runId}}',minPowerKw:power,maxPowerKw:power,priority,allowAutoShutdown:auto});
const workshop={name:'Postman {{runId}}',powerLimitKw:60,simulationType:'EquipmentBased',minPowerKw:null,maxPowerKw:null};
const user=(role,status='Active')=>({role,workshopId:'{{testWorkshopId}}',status});
group('01 — Owner login and run initialization');
req('Login Owner — enter real Bootstrap credentials','POST','/api/auth/login',{login:'{{ownerLogin}}',password:'{{ownerPassword}}'},200,`if(pm.response.code===200){const b=pm.response.json();pm.test('Owner role',()=>pm.expect(b.user.role).eq('Owner'));pm.environment.set('ownerToken',b.accessToken);pm.environment.set('ownerId',b.user.id);pm.environment.set('ownerExpiresAt',b.expiresAt);}`,`if(!pm.environment.get('ownerLogin')||!pm.environment.get('ownerPassword')){pm.execution.skipRequest();throw new Error('Fill ownerLogin and ownerPassword from Bootstrap configuration');}
for(const k of ['ownerToken','foremanToken','workerToken','testWorkshopId','testEquipmentId','secondaryId','importantId','criticalId','foremanId','workerId','seedWorkshop1Id','seedWorkshop2Id','seedWorkshop3Id','seedEquipmentId'])pm.environment.unset(k);
const run=Date.now()+'_'+pm.variables.replaceIn('{{$randomInt}}');pm.environment.set('runId',run);
for(const role of ['foreman','worker']){pm.environment.set(role+'Login','pm_'+role+'_'+run);pm.environment.set(role+'Password',pm.variables.replaceIn('{{$guid}}')+'Aa1!');}
pm.environment.set('from',new Date(Date.now()-86400000).toISOString());pm.environment.set('to',new Date(Date.now()+60000).toISOString());`,null);
group('02 — Bootstrap discovery (IDs are never assumed)');
req('Substation snapshot','GET','/api/substation',undefined,200,`${save('substationId')}\nif(pm.response.code===200){const b=pm.response.json();pm.environment.set('stationName',b.name);pm.environment.set('stationOriginalLimit',b.powerLimitKw);pm.environment.set('stationTestLimit',b.powerLimitKw+60);}`,need('ownerToken'));
req('Find workshops by Bootstrap names','GET','/api/workshops',undefined,200,`if(pm.response.code===200){const rows=pm.response.json();for(let i=1;i<=3;i++){const w=rows.find(x=>x.name==='Цех №'+i);pm.test('Bootstrap workshop '+i,()=>pm.expect(w).to.exist);if(w)pm.environment.set('seedWorkshop'+i+'Id',w.id);}}`);
req('Get Bootstrap workshop','GET','/api/workshops/{{seedWorkshop3Id}}',undefined,200,'',need('seedWorkshop3Id'));
req('All equipment','GET','/api/equipment',undefined,200,`if(pm.response.code===200){const e=pm.response.json().find(x=>x.name==='Токарный станок'&&x.workshopId===Number(pm.environment.get('seedWorkshop3Id')));pm.test('Bootstrap lathe',()=>pm.expect(e).to.exist);if(e)pm.environment.set('seedEquipmentId',e.id);}`);
req('Equipment filter: workshop and enabled','GET','/api/equipment?workshopId={{seedWorkshop3Id}}&enabled=false',undefined,200,`if(pm.response.code===200)pm.test('Filter',()=>pm.response.json().forEach(e=>{pm.expect(e.isEnabled).eq(false);pm.expect(e.workshopId).eq(Number(pm.environment.get('seedWorkshop3Id')));}));`);
req('Get Bootstrap equipment','GET','/api/equipment/{{seedEquipmentId}}',undefined,200,'',need('seedEquipmentId'));
req('List users','GET','/api/users',undefined,200);
req('Factory summary and ID','GET','/api/reports/summary',undefined,200,save('factoryId','factoryId'));
group('03 — Anonymous and validation checks');
req('Anonymous workshops denied','GET','/api/workshops',undefined,401,'','',null);
req('Invalid login denied','POST','/api/auth/login',{login:'nonexistent_{{runId}}',password:'InvalidPassword123!'},401,'','',null);
req('Missing workshop','GET','/api/workshops/2147483647',undefined,404);
req('Missing equipment','GET','/api/equipment/2147483647',undefined,404);
req('Invalid log pagination','GET','/api/logs?page=0&size=100',undefined,400);
req('Invalid history scope','GET','/api/reports/history?scope=factory&id={{factoryId}}&from={{from}}&to={{to}}',undefined,400);
req('Reversed statistics period','GET','/api/reports/statistics?scope=factory&id={{factoryId}}&from={{to}}&to={{from}}',undefined,400);
group('04 — Isolated workshop and users (runWriteTests=true)',true);
req('Increase station budget by 60 kW','PUT','/api/substation',{name:'{{stationName}}',powerLimitKw:'{{stationTestLimit}}'},200,'',need('stationTestLimit'));
req('Create dedicated equipment-based workshop','POST','/api/workshops',workshop,201,save('testWorkshopId'));
req('Update dedicated workshop','PUT','/api/workshops/{{testWorkshopId}}',{...workshop,name:'Postman updated {{runId}}'},200,'',need('testWorkshopId'));
req('Invalid simulation type change','PUT','/api/workshops/{{testWorkshopId}}',{...workshop,simulationType:'Virtual',minPowerKw:1,maxPowerKw:2},400,'',need('testWorkshopId'));
req('Invalid zero workshop limit','POST','/api/workshops',{...workshop,powerLimitKw:0},400);
req('Station cannot be below allocated budgets','PUT','/api/substation',{name:'{{stationName}}',powerLimitKw:1},409);
req('Workshop exceeds station allocation','POST','/api/workshops',{...workshop,powerLimitKw:'{{stationTestLimit}}'},409);
for(const role of ['Foreman','Worker']){const k=role.toLowerCase();req('Create '+role,'POST','/api/users',{login:'{{'+k+'Login}}',password:'{{'+k+'Password}}',role,workshopId:'{{testWorkshopId}}'},201,`if(pm.response.code===201){const b=pm.response.json();pm.environment.set('${k}Id',b.user.id);pm.environment.set('${k}Password',b.initialPassword);}`,need('testWorkshopId'));req('Login '+role,'POST','/api/auth/login',{login:'{{'+k+'Login}}',password:'{{'+k+'Password}}'},200,save(k+'Token','accessToken'),need(k+'Id'),null);}
req('Cannot create another Owner','POST','/api/users',{login:'pm_owner_{{runId}}',password:'PostmanOwnerTest123!',role:'Owner',workshopId:'{{testWorkshopId}}'},400);
req('Cannot modify Owner','PUT','/api/users/{{ownerId}}',user('Worker'),403);
group('05 — Role and workshop access (runWriteTests=true)',true);
for(const k of ['foreman','worker']){
 req(k+': own workshops only','GET','/api/workshops',undefined,200,`if(pm.response.code===200)pm.test('Only assigned workshop',()=>pm.response.json().forEach(w=>pm.expect(w.id).eq(Number(pm.environment.get('testWorkshopId')))));`,need(k+'Token'),k+'Token');
 req(k+': foreign workshop denied','GET','/api/workshops/{{seedWorkshop1Id}}',undefined,403,'','',k+'Token');
 req(k+': foreign equipment denied','GET','/api/equipment/{{seedEquipmentId}}',undefined,403,'','',k+'Token');
 req(k+': own equipment list','GET','/api/equipment?workshopId={{testWorkshopId}}',undefined,200,'','',k+'Token');
 for(const url of ['/api/users','/api/logs','/api/substation','/api/reports/summary'])req(k+': forbidden '+url,'GET',url,undefined,403,'','',k+'Token');
 req(k+': create workshop denied','POST','/api/workshops',workshop,403,'','',k+'Token');
 req(k+': create equipment denied','POST','/api/equipment',eq('Denied',1),403,'','',k+'Token');
}
group('06 — Equipment CRUD and deterministic limits (runWriteTests=true)',true);
req('Create Secondary 40 kW','POST','/api/equipment',eq('Secondary',40),201,save('secondaryId'),need('testWorkshopId'));
req('Update Secondary (same fixed range)','PUT','/api/equipment/{{secondaryId}}',eq('Secondary updated',40),200,'',need('secondaryId'));
req('Create Important 40 kW','POST','/api/equipment',eq('Important',40,'Important'),201,save('importantId'));
req('Create Critical 70 kW (cannot fit)','POST','/api/equipment',eq('Critical',70,'Critical',false),201,save('criticalId'));
req('Critical cannot allow auto shutdown','POST','/api/equipment',eq('Invalid Critical',5,'Critical',true),400);
req('Virtual workshop cannot contain equipment','POST','/api/equipment',{...eq('Invalid virtual',5),workshopId:'{{seedWorkshop1Id}}'},400);
req('Worker cannot enable equipment','POST','/api/equipment/{{secondaryId}}/enable',undefined,403,'',need('secondaryId'),'workerToken');
req('Foreman enables Secondary','POST','/api/equipment/{{secondaryId}}/enable',undefined,200,`if(pm.response.code===200)pm.test('Enabled 40 kW',()=>{const b=pm.response.json();pm.expect(b.success).eq(true);pm.expect(b.equipment.currentPowerKw).eq(40);});`,need('secondaryId'),'foremanToken');
req('Important displaces Secondary','POST','/api/equipment/{{importantId}}/enable',undefined,200,`if(pm.response.code===200)pm.test('Secondary auto disabled',()=>pm.expect(pm.response.json().automaticallyDisabledIds).include(Number(pm.environment.get('secondaryId'))));`,need('importantId'));
req('Critical rejected without changing equipment','POST','/api/equipment/{{criticalId}}/enable',undefined,409,`if(pm.response.code===409)pm.test('Rejected switch result',()=>{const b=pm.response.json();pm.expect(b.success).eq(false);pm.expect(b.automaticallyDisabledIds).deep.eq([]);});`,need('criticalId'));
// SwitchResultDto on 409 has message too and is not an ErrorDto-only response.
req('Foreman disables Important','POST','/api/equipment/{{importantId}}/disable',undefined,200,'',need('importantId'),'foremanToken');
req('Disable is idempotent','POST','/api/equipment/{{importantId}}/disable',undefined,200);
req('History prevents equipment delete','DELETE','/api/equipment/{{secondaryId}}',undefined,409,'',need('secondaryId'));
req('History and users prevent workshop delete','DELETE','/api/workshops/{{testWorkshopId}}',undefined,409,'',need('testWorkshopId'));
group('07 — History, statistics and logs');
for(const scope of ['workshop','equipment','factory']){const id={workshop:'seedWorkshop3Id',equipment:'seedEquipmentId',factory:'factoryId'}[scope];if(scope!=='factory')req(scope+' history','GET',`/api/reports/history?scope=${scope}&id={{${id}}}&from={{from}}&to={{to}}&page={{page}}&size={{size}}`,undefined,200,`if(pm.response.code===200)pm.test('History array',()=>pm.expect(pm.response.json()).to.be.an('array'));`,need(id));req(scope+' statistics','GET',`/api/reports/statistics?scope=${scope}&id={{${id}}}&from={{from}}&to={{to}}`,undefined,200,`if(pm.response.code===200)pm.test('StatisticsDto',()=>{const b=pm.response.json();pm.expect(b.scope).eq('${scope}');pm.expect(b.energyKwh).at.least(0);pm.expect(b.coveredSeconds).at.least(0);pm.expect(b.missingSeconds).at.least(0);});`,need(id));}
req('Action logs (paged)','GET','/api/logs?page={{page}}&size={{size}}',undefined,200,`if(pm.response.code===200)pm.test('LogDto array',()=>pm.expect(pm.response.json()).to.be.an('array'));`);
group('08 — Role changes and cleanup (runWriteTests=true)',true);
req('Demote Foreman to Worker','PUT','/api/users/{{foremanId}}',user('Worker'),200,'',need('foremanId'));
req('Existing token uses updated role from DB','POST','/api/equipment/{{importantId}}/enable',undefined,403,'',need('foremanToken'),'foremanToken');
req('Block Worker','PUT','/api/users/{{workerId}}',user('Worker','Blocked'),200,'',need('workerId'));
req('Blocked token rejected immediately','GET','/api/workshops',undefined,401,'',need('workerToken'),'workerToken');
req('Blocked user cannot login','POST','/api/auth/login',{login:'{{workerLogin}}',password:'{{workerPassword}}'},401,'',need('workerId'),null);
req('Restore Worker Active','PUT','/api/users/{{workerId}}',user('Worker'),200);
req('Dismiss test Worker','PUT','/api/users/{{workerId}}',user('Worker','Dismissed'),200);
req('Dismissed employee cannot be changed','PUT','/api/users/{{workerId}}',user('Worker'),409);
req('Block test Foreman','PUT','/api/users/{{foremanId}}',user('Foreman','Blocked'),200);
for(const k of ['secondaryId','importantId','criticalId'])req('Cleanup disable '+k,'POST',`/api/equipment/{{${k}}}/disable`,undefined,200,'',need(k));
req('Final isolated workshop state','GET','/api/workshops/{{testWorkshopId}}',undefined,200,`if(pm.response.code===200)pm.test('No test consumption',()=>pm.expect(pm.response.json().currentPowerKw).eq(0));`,need('testWorkshopId'));
// Numeric placeholders must be JSON numbers, not strings. JSON serialization above preserves string names safely.
function walk(items){for(const i of items){if(i.item)walk(i.item);else if(i.request.body)i.request.body.raw=i.request.body.raw.replace(/"({{(?:testWorkshopId|seedWorkshop1Id|stationTestLimit)}})"/g,'$1');}}
walk(c.item);
fs.writeFileSync(path.join(out,'EnergyMeneger.postman_collection.json'),JSON.stringify(c,null,2)+'\n');
const env={id:'63dc6e50-2d56-47b2-874e-e2ba811ab7fc',name:'EnergyMeneger localhost 7080',values:Object.entries(vars).map(([key,value])=>({key,value,type:/Password|Token/.test(key)?'secret':'default',enabled:true})),_postman_variable_scope:'environment',_postman_exported_at:new Date().toISOString(),_postman_exported_using:'EnergyMeneger source generator'};
fs.writeFileSync(path.join(out,'EnergyMeneger.postman_environment.json'),JSON.stringify(env,null,2)+'\n');
const seed={sourceCommit:sha,factory:{name:'Учебный завод'},substation:{name:'Главная подстанция',powerLimitKw:300},workshops:[{name:'Цех №1',powerLimitKw:70,simulationType:'Virtual',minPowerKw:30,maxPowerKw:70},{name:'Цех №2',powerLimitKw:90,simulationType:'Virtual',minPowerKw:40,maxPowerKw:90},{name:'Цех №3',powerLimitKw:140,simulationType:'EquipmentBased',minPowerKw:null,maxPowerKw:null}],equipment:[['Токарный станок',20,45,'Important',true],['Фрезерный станок',25,55,'Important',true],['Сверлильный станок',5,15,'Secondary',true],['Компрессор',15,35,'Secondary',true],['Вентиляция',5,12,'Critical',false],['Освещение',3,8,'Critical',false]].map(([name,minPowerKw,maxPowerKw,priority,allowAutoShutdown])=>({workshopName:'Цех №3',name,minPowerKw,maxPowerKw,priority,allowAutoShutdown,isEnabled:false,currentPowerKw:0})),owner:{loginSource:'Bootstrap:Login',passwordSource:'Bootstrap:Password',role:'Owner',status:'Active'},note:'IDs are assigned by the database. Bootstrap creates no Foreman or Worker. Do not replay these records through POST against an initialized database.'};
fs.writeFileSync(path.join(out,'Bootstrap.reference.json'),JSON.stringify(seed,null,2)+'\n');
const dtoFiles=fs.readdirSync(path.join(out,'..','DTos')).filter(f=>f.endsWith('.cs'));
let dtoDoc='# DTO из main\n\nCommit: '+sha+'\n\nJSON: camelCase, enum — строки (Program.cs, JsonStringEnumConverter). Ниже точные C# контракты; nullable поля передаются как null.\n\n';
for(const f of dtoFiles){const src=fs.readFileSync(path.join(out,'..','DTos',f),'utf8');dtoDoc+='## '+f+'\n\n```csharp\n'+src.slice(src.indexOf('public sealed record')).trim()+'\n```\n\n';}
fs.writeFileSync(path.join(out,'DTO-reference.md'),dtoDoc);
console.log('Generated',c.item.reduce((n,f)=>n+f.item.length,0),'requests');
