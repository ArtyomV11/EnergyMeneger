const fs=require('fs'),path=require('path'),assert=require('assert');
const root=__dirname;
const collection=JSON.parse(fs.readFileSync(path.join(root,'EnergyMeneger.postman_collection.json')));
const environment=JSON.parse(fs.readFileSync(path.join(root,'EnergyMeneger.postman_environment.json')));
const requests=collection.item.flatMap(f=>f.item),keys=new Set(environment.values.map(v=>v.key));
for(const item of [...collection.item,...requests])for(const e of item.event||[])new Function('pm',e.script.exec.join('\n'));
for(const item of requests){
 for(const [,key]of JSON.stringify(item.request).matchAll(/\{\{([^{}]+)\}\}/g))assert(key.startsWith('$')||keys.has(key),'Undefined variable '+key);
 if(item.request.body)JSON.parse(item.request.body.raw.replace(/\{\{workshop[123]Id\}\}/g,'3').replace(/\{\{[^{}]+\}\}/g,'test'));
}
const routes=new Set(requests.map(i=>i.request.method+' '+i.request.url.replace('{{baseUrl}}','').split('?')[0].replace(/\{\{[^}]+\}\}|2147483647/g,'{id}')));
let routeCount=0;
for(const file of fs.readdirSync(path.join(root,'..','Controllers')).filter(f=>f.endsWith('.cs'))){
 const src=fs.readFileSync(path.join(root,'..','Controllers',file),'utf8'),base=src.match(/\[Route\("([^"]+)"\)\]/)[1];
 for(const m of src.matchAll(/\[Http(Get|Post|Put|Delete)(?:\("([^"]+)"\))?\]/g)){
  const route=m[1].toUpperCase()+' /'+base+(m[2]?'/'+m[2].replace('{id:int}','{id}'):'');assert(routes.has(route),'Missing '+route);routeCount++;
 }
}
for(const role of ['owner','foreman','worker'])assert.equal(environment.values.find(v=>v.key===role+'Password').value,'');
assert.equal(environment.values.find(v=>v.key==='runWriteTests').value,'false');
assert.equal(environment.values.find(v=>v.key==='runEquipmentTests').value,'false');
function expect(actual){
 let deep=false;const chain={};
 for(const name of ['to','be','have','at'])Object.defineProperty(chain,name,{get:()=>chain});
 Object.defineProperty(chain,'deep',{get:()=>{deep=true;return chain;}});
 Object.defineProperty(chain,'exist',{get:()=>{assert(actual!==null&&actual!==undefined,'Expected value to exist');return chain;}});
 chain.eq=v=>{deep?assert.deepStrictEqual(actual,v):assert.strictEqual(actual,v);return chain;};
 chain.lengthOf=n=>{assert.equal(actual.length,n);return chain;};
 chain.members=a=>{assert.deepStrictEqual([...actual].sort(),[...a].sort());return chain;};
 chain.least=n=>{assert(actual>=n,`${actual} < ${n}`);return chain;};
 chain.a=chain.an=t=>{assert(t==='array'?Array.isArray(actual):typeof actual===t);return chain;};return chain;
}
function runtime(state){
 const result={skipped:false,stop:false,checks:0,failures:[]};
 const pm={environment:{get:k=>state[k],set:(k,v)=>state[k]=v,unset:k=>delete state[k]},variables:{replaceIn:s=>s.replace(/\{\{([^}]+)\}\}/g,(_,k)=>k.startsWith('$')?'test-guid':String(state[k]??''))},execution:{skipRequest:()=>result.skipped=true,setNextRequest:v=>{if(v===null)result.stop=true;}},request:{body:{update:s=>result.body=s}},response:{code:200,json:()=>result.json},expect,test:(name,fn)=>{result.checks++;try{fn();}catch(e){result.failures.push(name+': '+e.message);}}};
 return {pm,result};
}
function script(pm,item,listen){for(const e of item.event||[])if(e.listen===listen)new Function('pm',e.script.exec.join('\n'))(pm);}
// Golden responses: execute every pre/post script in order, including both optional folders.
const state=Object.fromEntries(environment.values.map(v=>[v.key,v.value]));
Object.assign(state,{runWriteTests:'true',runEquipmentTests:'true',ownerPassword:'Owner"with\\escapes123',foremanPassword:'ForemanPassword123!',workerPassword:'WorkerPassword123!'});
const workshops=[1,2,3].map(id=>({id,name:'Цех №'+id,powerLimitKw:100,simulationType:id===3?'EquipmentBased':'Virtual',minPowerKw:id===1?30:id===2?40:null,maxPowerKw:id===1?70:id===2?90:null,currentPowerKw:0}));
const equipment=JSON.parse(fs.readFileSync(path.join(root,'Bootstrap.reference.json'))).equipment.map((e,i)=>({...e,id:i+1,workshopId:3}));
const users=['Owner','Foreman','Worker'].map((role,i)=>({id:i+1,role,status:'Active',workshopId:i?3:null}));
let checks=0;
for(const folder of collection.item)for(const item of folder.item){
 const {pm,result}=runtime(state);script(pm,folder,'prerequest');script(pm,item,'prerequest');assert(!result.skipped,'Unexpected skip '+item.name);
 const url=new URL(pm.variables.replaceIn(item.request.url)),route=url.pathname;
 const status=Number(item.event.find(e=>e.listen==='test').script.exec.join('\n').match(/const expected=(\d+)/)[1]);
 pm.response.code=status;
 if(status>=400)result.json={message:'Expected error'};
 else if(route==='/api/auth/login'){
  const body=JSON.parse(result.body),role=['owner','foreman','worker'].find(k=>state[k+'Login']===body.login);
  assert(role);assert.equal(body.password,state[role+'Password']);const user=users[{owner:0,foreman:1,worker:2}[role]];
  result.json={accessToken:role+'-token',expiresAt:new Date(Date.now()+7200000).toISOString(),user};
 }else if(item.request.method==='PUT')result.json={id:3,...JSON.parse(result.body)};
 else if(/\/(enable|disable)$/.test(route))result.json={success:true,message:'OK',equipment:{...equipment[0],isEnabled:route.endsWith('/enable'),currentPowerKw:route.endsWith('/enable')?30:0},automaticallyDisabledIds:[]};
 else if(route==='/api/substation')result.json={id:1,name:'Главная подстанция',powerLimitKw:300};
 else if(route==='/api/workshops')result.json=JSON.parse(JSON.stringify(item.request.auth?workshops.filter(w=>w.id===3):workshops));
 else if(route.startsWith('/api/workshops/'))result.json=workshops[2];
 else if(route==='/api/equipment')result.json=equipment;
 else if(route.startsWith('/api/equipment/'))result.json=equipment[0];
 else if(route==='/api/users')result.json=users;
 else if(route==='/api/reports/summary')result.json={factoryId:1,workshops,powerLimitKw:300,currentPowerKw:0};
 else if(route==='/api/reports/statistics')result.json={scope:url.searchParams.get('scope'),objectId:Number(url.searchParams.get('id')),energyKwh:0,coveredSeconds:0,missingSeconds:86400};
 else result.json=[];
 script(pm,item,'test');assert.deepStrictEqual(result.failures,[],item.name);assert(!result.stop,item.name);checks+=result.checks;
}
// Both optional groups really skip with default settings; failures stop request chaining.
for(const f of collection.item.filter(f=>f.event)){const {pm,result}=runtime({});script(pm,f,'prerequest');assert(result.skipped);}
const bad=runtime(state);bad.pm.response.code=500;bad.result.json={message:'failure'};script(bad.pm,requests[0],'test');assert(bad.result.stop&&bad.result.failures.length);
// No destructive or additive success paths can slip into the collection unnoticed.
for(const item of requests){const code=Number(item.event.find(e=>e.listen==='test').script.exec.join('\n').match(/const expected=(\d+)/)[1]);if(['POST','DELETE'].includes(item.request.method)&&!item.request.url.includes('/auth/login')&&!/\/(enable|disable)$/.test(item.request.url))assert(code>=400,item.name);}
console.log(`PASS: ${routeCount} routes, ${requests.length} requests, ${checks} golden assertions, scripts/JSON/variables, empty passwords, guards and stop-on-error.`);
module.exports={runtime,script,collection,environment};
