const fs=require('fs'),path=require('path'),assert=require('assert');
const root=__dirname;
const c=JSON.parse(fs.readFileSync(path.join(root,'EnergyMeneger.postman_collection.json')));
const e=JSON.parse(fs.readFileSync(path.join(root,'EnergyMeneger.postman_environment.json')));
const keys=new Set(e.values.map(v=>v.key));
const requests=c.item.flatMap(f=>f.item);
for(const item of [...c.item,...requests])for(const event of item.event||[])new Function('pm',event.script.exec.join('\n'));
for(const item of requests){
 for(const [,key] of JSON.stringify(item.request).matchAll(/\{\{([^{}]+)\}\}/g))assert(keys.has(key),'Undefined variable '+key);
 if(item.request.body){const raw=item.request.body.raw.replace(/\{\{([^}]+)\}\}/g,(_,k)=>['testWorkshopId','seedWorkshop1Id','stationTestLimit'].includes(k)?'123':'value');JSON.parse(raw);}
}
const routes=new Set(requests.map(i=>i.request.method+' '+i.request.url.replace('{{baseUrl}}','').split('?')[0].replace(/\{\{[^}]+\}\}|2147483647/g,'{id}')));
let expected=0;
for(const file of fs.readdirSync(path.join(root,'..','Controllers'))){
 if(!file.endsWith('.cs'))continue;
 const text=fs.readFileSync(path.join(root,'..','Controllers',file),'utf8');
 const base=text.match(/\[Route\("([^"]+)"\)\]/)[1];
 for(const m of text.matchAll(/\[Http(Get|Post|Put|Delete)(?:\("([^"]+)"\))?\]/g)){
  const route=m[1].toUpperCase()+' /'+base+(m[2]?'/'+m[2].replace('{id:int}','{id}'):'');
  assert(routes.has(route),'Missing route '+route);expected++;
 }
}
assert.equal(expected,22);assert.equal(requests.length,82);
assert.equal(e.values.find(v=>v.key==='ownerPassword').value,'');
assert.equal(e.values.find(v=>v.key==='ownerLogin').value,'');
assert.equal(e.values.find(v=>v.key==='runWriteTests').value,'false');
// Ensure special characters in a real user's password remain valid JSON.
const login=requests[0],state=Object.fromEntries(e.values.map(v=>[v.key,v.value]));
state.ownerLogin='owner';state.ownerPassword='A"quote\\and\nnewline123';let body;
const pm={environment:{get:k=>state[k],set:(k,v)=>state[k]=v,unset:k=>delete state[k]},variables:{replaceIn:s=>s.replace(/\{\{([^}]+)\}\}/g,(_,k)=>state[k]||'random')},execution:{skipRequest:()=>{}},request:{body:{update:s=>body=s}}};
new Function('pm',login.event.find(e=>e.listen==='prerequest').script.exec.join('\n'))(pm);
assert.equal(JSON.parse(body).password,state.ownerPassword);
console.log(`PASS: ${expected} controller routes, ${requests.length} requests, script syntax, JSON bodies, variables, empty owner credentials, password escaping.`);
