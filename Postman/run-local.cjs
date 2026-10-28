// Optional local verification. Credentials stay in memory and are never exported.
const fs=require('fs'),path=require('path'),https=require('https');
const {runtime,script,collection,environment}=require('./validate.cjs');
async function main(){
 const state=Object.fromEntries(environment.values.map(v=>[v.key,v.value]));
 state.runWriteTests=String(process.argv.includes('--writes'));
 state.runEquipmentTests=String(process.argv.includes('--equipment'));
 if(process.argv.includes('--user-secrets')){
  const file=path.join(process.env.APPDATA,'Microsoft','UserSecrets','EnergyMeneger-Development','secrets.json');
  const settings=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
  const get=k=>settings[k]??k.split(':').reduce((o,p)=>o?.[p],settings);
  for(const [role,login,password]of [['owner','Login','Password'],['foreman','ForemanLogin','ForemanPassword'],['worker','WorkerLogin','WorkerPassword']]){
   state[role+'Login']=get('Bootstrap:'+login)||state[role+'Login'];state[role+'Password']=get('Bootstrap:'+password)||'';
  }
 }else for(const role of ['owner','foreman','worker']){
  state[role+'Login']=process.env['ENERGY_'+role.toUpperCase()+'_LOGIN']||state[role+'Login'];state[role+'Password']=process.env['ENERGY_'+role.toUpperCase()+'_PASSWORD']||'';
 }
 let count=0,checks=0,skipped=0;
 for(const folder of collection.item)for(const item of folder.item){
  const {pm,result}=runtime(state);script(pm,folder,'prerequest');if(result.skipped){skipped++;continue;}script(pm,item,'prerequest');if(result.skipped){skipped++;continue;}
  const url=new URL(pm.variables.replaceIn(item.request.url));
  if(url.origin!=='https://localhost:7080')throw new Error('Only localhost:7080 allowed');
  const headers=Object.fromEntries(item.request.header.map(h=>[h.key,h.value]));
  const auth=item.request.auth===undefined?collection.auth:item.request.auth;
  if(auth?.type==='bearer')headers.Authorization='Bearer '+pm.variables.replaceIn(auth.bearer.find(v=>v.key==='token').value);
  const response=await new Promise((resolve,reject)=>{
   const req=https.request(url,{method:item.request.method,headers,rejectUnauthorized:false},res=>{let text='';res.on('data',s=>text+=s);res.on('end',()=>resolve({code:res.statusCode,text}));});
   req.on('error',reject);req.setTimeout(10000,()=>req.destroy(new Error('Local API timed out')));req.end(result.body||undefined);
  });
  pm.response.code=response.code;try{result.json=JSON.parse(response.text);}catch{result.json={};}
  script(pm,item,'test');count++;checks+=result.checks;
  if(result.failures.length)throw new Error(item.name+': '+result.failures.join('; '));
  if(result.stop)throw new Error('Stopped at '+item.name);
 }
 console.log(`LIVE PASS: ${count} requests, ${checks} assertions; ${skipped} optional requests skipped. Writes=${state.runWriteTests}; equipment=${state.runEquipmentTests}.`);
}
main().catch(e=>{console.error('LIVE CHECK FAILED:',e.message||e.code||e.errors?.map(x=>x.code).join(', ')||e.name);process.exitCode=1;});
