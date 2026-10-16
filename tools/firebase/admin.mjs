// Official CLI session only. Never read/log access tokens. Target is deliberately not configurable.
import {spawnSync} from 'node:child_process';
import {existsSync, readFileSync, writeFileSync, mkdirSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {resolve, dirname} from 'node:path';
import {tmpdir} from 'node:os';
const project='haychoriypaty', packageName='com.haychoriypaty.game';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'../..');
const localEntry=resolve(root,'tools/firebase/node_modules/firebase-tools/lib/bin/firebase.js');
const entry=process.env.FIREBASE_CLI_ENTRY || (existsSync(localEntry)?localEntry:resolve(tmpdir(),'hcyp-firebase-tools/node_modules/firebase-tools/lib/bin/firebase.js'));
function cli(args) {
  const r=spawnSync(existsSync(entry)?process.execPath:'firebase', [...(existsSync(entry)?[entry]:[]),...args,'--project',project,'--non-interactive','--json'],{cwd:root,encoding:'utf8',maxBuffer:8*1024*1024});
  if(r.error) throw r.error;
  let data; try {data=JSON.parse(r.stdout);} catch {throw Error('Official Firebase CLI login is required. No changes made.');}
  if(r.status!==0 || data.status!=='success') throw Error(data.error || 'Firebase CLI failed');
  return data.result;
}
function config(appId) {return JSON.parse(cli(['apps:sdkconfig','ANDROID',appId]).fileContents);}
function client(data) {
  if(data.project_info?.project_id!==project) throw Error('Refusing config from another Firebase project.');
  return (data.client||[]).find(c=>c.client_info?.android_client_info?.package_name===packageName);
}
try {
  const projects=cli(['projects:list']);
  if(!(Array.isArray(projects)?projects:projects.projects||[]).some(p=>p.projectId===project)) throw Error('This CLI account cannot access haychoriypaty. No changes made.');
  const command=process.argv[2]||'preflight';
  if(command==='preflight') {
    console.log(JSON.stringify({project,androidApps:cli(['apps:list','ANDROID']),firestore:cli(['firestore:databases:list'])},null,2));
  } else if(command==='android-config') {
    const matching=cli(['apps:list','ANDROID']).map(app=>({app,config:config(app.appId)})).filter(x=>client(x.config));
    if(matching.length>1) throw Error('Duplicate package registrations already exist; refusing to choose/create another.');
    let selected=matching[0];
    if(!selected) {const app=cli(['apps:create','ANDROID','Hay Chori y Paty','--package-name',packageName]);selected={app,config:config(app.appId)};}
    const c=client(selected.config), webClientId=(c.oauth_client||[]).find(o=>o.client_type===3)?.client_id;
    if(!webClientId) throw Error('Missing Google/Web OAuth client. Enable Google in Authentication > Sign-in method of haychoriypaty, then rerun.');
    const target=resolve(root,'Assets/google-services.json');
    if(existsSync(target) && !client(JSON.parse(readFileSync(target,'utf8')))) throw Error('Refusing overwrite of another Android app config.');
    mkdirSync(resolve(root,'Assets/Resources'),{recursive:true});
    writeFileSync(target,JSON.stringify(selected.config,null,2)+'\n');
    writeFileSync(resolve(root,'Assets/Resources/player-firebase-config.json'),JSON.stringify({projectId:project,appId:c.client_info.mobilesdk_app_id,apiKey:c.api_key[0].current_key,webClientId},null,2)+'\n');
    console.log(JSON.stringify({project,packageName,appId:c.client_info.mobilesdk_app_id,configurationWritten:true}));
  } else throw Error('Use preflight or android-config. Review existing production rules before any deployment.');
} catch(error) {console.error(error.message);process.exitCode=1;}
