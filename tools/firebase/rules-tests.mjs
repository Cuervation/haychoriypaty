import {createRequire} from 'node:module';
import {readFileSync} from 'node:fs';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import assert from 'node:assert/strict';
const base=dirname(fileURLToPath(import.meta.url));
const require=createRequire(process.env.FIREBASE_TEST_PACKAGE || resolve(base,'package.json'));
const {initializeTestEnvironment,assertSucceeds,assertFails}=require('@firebase/rules-unit-testing');
const {doc,getDoc,setDoc,deleteDoc,writeBatch,serverTimestamp}=require('firebase/firestore');
if(process.env.FIRESTORE_EMULATOR_HOST!=='127.0.0.1:8085') throw Error('Refusing tests without the isolated localhost Firestore emulator.');
const env=await initializeTestEnvironment({projectId:'haychoriypaty',firestore:{host:'127.0.0.1',port:8085,rules:readFileSync(resolve(base,'../../firebase/firestore.rules'),'utf8')}});
const alice=env.authenticatedContext('alice',{firebase:{sign_in_provider:'google.com'}}).firestore();
const bob=env.authenticatedContext('bob',{firebase:{sign_in_provider:'google.com'}}).firestore();
const anonymous=env.authenticatedContext('alice',{firebase:{sign_in_provider:'anonymous'}}).firestore();
const unauth=env.unauthenticatedContext().firestore();
const current=db=>doc(db,'players/alice/state/current');
function commit(db,revision,id,options={}) {
  const batch=writeBatch(db), hash='f'.repeat(64), matchId=options.matchId||'';
  const state={schemaVersion:1,uid:'alice',revision,operationId:id,payloadHash:hash,snapshot:'{"version":4}',unlockedLevel:2,balanceCents:250,stockUnits:Array(10).fill(0),updatedAt:serverTimestamp(),...(options.state||{})};
  for(const key of options.omitState||[]) delete state[key];
  if(!options.skipState) batch.set(current(db),state);
  if(!options.skipReceipt) batch.set(doc(db,'players/alice/operations/'+id),{schemaVersion:1,uid:'alice',operationId:id,revision,previousRevision:revision-1,payloadHash:hash,matchId,createdAt:serverTimestamp(),...(options.receipt||{})});
  if(matchId && !options.skipMatch) { const matchRef=doc(db,'players/alice/matches/'+matchId); const match={schemaVersion:1,uid:'alice',id:matchId,attemptId:matchId,operationId:id,revision,level:2,grossSalesCents:500,rewardCents:250,salesByProduct:Array(7).fill(0),hiresByRole:Array(5).fill(0),stockConsumed:Array(10).fill(0),stockRemaining:Array(10).fill(0),committedAt:serverTimestamp(),utc:'2026-10-16T00:00:00Z',club:'Test club',result:'won',won:true,abandoned:false,elapsedSeconds:50,hireSpendCents:0,speedSpendCents:0,initialCoinsCents:100,leftoverCents:0,halfSalesRewardCents:250,leftoverBonusCents:0,progressionBonusCents:0,customersAttended:0,customersLost:0,ordersCompleted:0,recordsReached:0,...(options.match||{})}; for(const key of options.omitMatch||[]) delete match[key]; batch.set(matchRef,match); }
  return batch.commit();
}
let passed=0; const names=new Set();
async function check(name,action){assert(!names.has(name),'duplicate check name: '+name);names.add(name);await action();passed++;console.log('PASS '+name);}
try {
  await check('anonymous/no session cannot read',()=>assertFails(getDoc(current(unauth))));
  await check('owner atomic create with receipt',()=>assertSucceeds(commit(alice,1,'a'.repeat(32))));
  await check('owner can read',()=>assertSucceeds(getDoc(current(alice))));
  await check('different UID cannot read',()=>assertFails(getDoc(current(bob))));
  await check('anonymous provider cannot reuse an owner UID',()=>assertFails(getDoc(current(anonymous))));
  await check('different UID cannot write',()=>assertFails(commit(bob,2,'b'.repeat(32))));
  await check('state-only overwrite denied',()=>assertFails(setDoc(current(alice),{balanceCents:999},{merge:true})));
  await check('result/state/receipt atomic update',()=>assertSucceeds(commit(alice,2,'c'.repeat(32),{matchId:'d'.repeat(32)})));
  await check('receipt cannot be rewritten',()=>assertFails(setDoc(doc(alice,'players/alice/operations/'+'c'.repeat(32)),{revision:99},{merge:true})));
  await check('result cannot be deleted',()=>assertFails(deleteDoc(doc(alice,'players/alice/matches/'+'d'.repeat(32)))));
  await check('out of order revision denied',()=>assertFails(commit(alice,4,'e'.repeat(32))));
  await check('stale offline branch denied',()=>assertFails(commit(alice,2,'f'.repeat(32))));
  await check('negative balance denied',()=>assertFails(commit(alice,3,'1'.repeat(32),{state:{balanceCents:-1}})));
  await check('negative ingredient units denied',()=>assertFails(commit(alice,3,'2'.repeat(32),{state:{stockUnits:[-1,0,0,0,0,0,0,0,0,0]}})));
  await check('unapproved fields denied',()=>assertFails(commit(alice,3,'3'.repeat(32),{state:{publicProfile:true}})));
  await check('arbitrary document denied',()=>assertFails(setDoc(doc(alice,'players/alice/state/anything'),{balance:2})));
  await check('concurrent devices: exactly one revision wins',async()=>{
    const result=await Promise.allSettled([commit(alice,3,'4'.repeat(32)),commit(alice,3,'5'.repeat(32))]);
    assert.equal(result.filter(r=>r.status==='fulfilled').length,1);assert.equal((await getDoc(current(alice))).data().revision,3);
  });
  await check('invalid result rolls back state and receipt',async()=>{
    await assertFails(commit(alice,4,'6'.repeat(32),{matchId:'7'.repeat(32),match:{salesByProduct:[]}}));
    assert.equal((await getDoc(current(alice))).data().revision,3);
    assert.equal((await getDoc(doc(alice,'players/alice/operations/'+'6'.repeat(32)))).exists(),false);
  });
  await check('negative history inventory denied',()=>assertFails(commit(alice,4,'8'.repeat(32),{matchId:'9'.repeat(32),match:{stockConsumed:[-1,0,0,0,0,0,0,0,0,0]}})));
  await check('unexpected history fields denied',()=>assertFails(commit(alice,4,'8'.repeat(32),{matchId:'9'.repeat(32),match:{admin:true}})));
  await check('inconsistent reward components denied',()=>assertFails(commit(alice,4,'8'.repeat(32),{matchId:'9'.repeat(32),match:{rewardCents:999}})));
  // All three outcomes use the complete production DTO and nonzero list entries.
  const sales=[1,2,3,4,5,6,7], hires=[1,2,3,4,5], inventory=[10,11,12,13,14,15,16,17,18,19];
  let serial=100;
  const unique=()=> (++serial).toString(16).padStart(32,'0');
  for(const [revision,result] of [[4,'won'],[5,'lost'],[6,'abandoned']]) {
    const match={result,won:result==='won',abandoned:result==='abandoned',salesByProduct:sales,hiresByRole:hires,
      stockConsumed:Array(10).fill(1),stockRemaining:inventory,customersAttended:5,customersLost:2,ordersCompleted:3,
      hireSpendCents:100,speedSpendCents:200,elapsedSeconds:12.5,recordsReached:result==='abandoned'?0:3,
      rewardCents:result==='abandoned'?0:250,halfSalesRewardCents:result==='abandoned'?0:250};
    await check('full DTO '+result+' atomic commit',()=>assertSucceeds(commit(alice,revision,unique(),{matchId:unique(),state:{stockUnits:inventory},match})));
  }
  const deny=async(name,options)=>check(name,()=>assertFails(commit(alice,7,unique(),{matchId:unique(),state:{stockUnits:inventory},match:{stockRemaining:inventory},...options})));
  // Independent invalid field cases are evaluated from the same valid base revision.
  for(const [field,value] of Object.entries({schemaVersion:2,uid:'bob',id:'0'.repeat(32),attemptId:'0'.repeat(32),operationId:'bad',revision:1.5,
    level:11,grossSalesCents:-1,rewardCents:-1,committedAt:new Date(0),utc:3,club:5,elapsedSeconds:-1,won:1,abandoned:1,
    result:'unknown',hireSpendCents:-1,speedSpendCents:-1,initialCoinsCents:-1,leftoverCents:-1,halfSalesRewardCents:-1,
    leftoverBonusCents:-1,progressionBonusCents:-1,customersAttended:-1,customersLost:-1,ordersCompleted:-1,recordsReached:4})) {
    await deny('match invalid '+field,{match:{stockRemaining:inventory,[field]:value}});
  }
  for(const field of ['grossSalesCents','rewardCents','hireSpendCents','speedSpendCents','initialCoinsCents','leftoverCents',
    'halfSalesRewardCents','leftoverBonusCents','progressionBonusCents','customersAttended','customersLost','ordersCompleted','recordsReached'])
    await deny('match noninteger '+field,{match:{stockRemaining:inventory,[field]:0.5}});
  await deny('match utc oversized',{match:{stockRemaining:inventory,utc:'x'.repeat(65)}});
  await deny('match club oversized',{match:{stockRemaining:inventory,club:'x'.repeat(129)}});
  await deny('match won flags inconsistent',{match:{stockRemaining:inventory,won:false}});
  await deny('match lost flags inconsistent',{match:{stockRemaining:inventory,result:'lost',abandoned:true}});
  await deny('match abandoned reward forbidden',{match:{stockRemaining:inventory,result:'abandoned',won:false,abandoned:true}});
  await deny('match remaining stock differs from committed inventory',{match:{stockRemaining:Array(10).fill(0)}});
  for(const [field,length] of [['salesByProduct',7],['hiresByRole',5],['stockConsumed',10]]) {
    await deny(field+' wrong length',{match:{stockRemaining:inventory,[field]:Array(length-1).fill(0)}});
    await deny(field+' wrong type',{match:{stockRemaining:inventory,[field]:'bad'}});
    for(let i=0;i<length;i++) for(const bad of [-1,0.5]) {
      const a=Array(length).fill(0);a[i]=bad;
      await deny(field+' slot '+i+' invalid '+bad,{match:{stockRemaining:inventory,[field]:a}});
    }
  }
  for(const field of ['id','attemptId','uid','operationId','revision','schemaVersion','committedAt','utc','level','club','result','won','abandoned','elapsedSeconds',
    'salesByProduct','grossSalesCents','hireSpendCents','speedSpendCents','initialCoinsCents','leftoverCents','halfSalesRewardCents','leftoverBonusCents',
    'progressionBonusCents','rewardCents','stockConsumed','stockRemaining','hiresByRole','customersAttended','customersLost','ordersCompleted','recordsReached'])
    await deny('match required '+field,{omitMatch:[field]});
  for(const [field,value] of Object.entries({schemaVersion:2,uid:'bob',revision:7.5,operationId:'bad',payloadHash:'bad',snapshot:'',
    unlockedLevel:11,balanceCents:0.5,stockUnits:[],updatedAt:new Date(0)}))
    await deny('state invalid '+field,{state:{stockUnits:inventory,[field]:value}});
  await deny('state snapshot oversized',{state:{stockUnits:inventory,snapshot:'x'.repeat(450001)}});
  for(let i=0;i<10;i++) {const a=[...inventory];a[i]=0.5;await deny('state stock slot noninteger '+i,{state:{stockUnits:a},match:{stockRemaining:a}});}
  for(const [field,value] of Object.entries({schemaVersion:2,uid:'bob',operationId:'bad',revision:8,previousRevision:5,payloadHash:'bad',matchId:'bad',createdAt:new Date(0)}))
    await deny('receipt invalid '+field,{receipt:{[field]:value}});
  await deny('match without new receipt denied',{skipReceipt:true});
  await deny('match receipt points elsewhere denied',{receipt:{matchId:'0'.repeat(32)}});
  await deny('receipt declaring absent match denied',{skipMatch:true});
  await deny('receipt without new state denied',{skipState:true});
  await check('match cannot be rewritten',()=>assertFails(setDoc(doc(alice,'players/alice/matches/'+'d'.repeat(32)),{grossSalesCents:1},{merge:true})));
  await check('receipt cannot be deleted',()=>assertFails(deleteDoc(doc(alice,'players/alice/operations/'+'c'.repeat(32)))));
  await check('current state cannot be deleted',()=>assertFails(deleteDoc(current(alice))));
  assert.equal((await getDoc(current(alice))).data().revision,6,'all malformed atomic batches must leave state untouched');
  console.log(`RULES_RESULT ${passed}/${passed} passed — isolated emulator only, no production reads/writes.`);
} finally {await env.cleanup();}
