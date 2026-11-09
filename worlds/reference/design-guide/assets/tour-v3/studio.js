'use strict';
const directions={right:{row:0,mirror:false,label:'Rechts'},back:{row:1,mirror:false,label:'Hinten / aufwärts'},front:{row:2,mirror:false,label:'Vorne / abwärts'},left:{row:0,mirror:true,label:'Links · rechts gespiegelt'}};
const phaseNames=['Kontakt / Stand','Durchgang','Gegenkontakt','Zweiter Durchgang'];
const state={direction:'right',phase:0,running:false,timer:null,images:new Map()};
const directionButtons=Array.from(document.querySelectorAll('[data-direction]'));
const phaseButtons=Array.from(document.querySelectorAll('[data-phase]'));
const playButton=document.getElementById('play');
const status=document.getElementById('load-status');
function paint(){
 const direction=directions[state.direction];
 for(const id of ['mara','bruno']){
  const sheet=state.images.get(id);if(!sheet)continue;
  const small=document.querySelector(`canvas[data-actor="${id}"][data-size="32"]`),large=document.querySelector(`canvas[data-actor="${id}"][data-size="256"]`);
  const ctx=small.getContext('2d');ctx.save();ctx.clearRect(0,0,32,32);ctx.imageSmoothingEnabled=false;
  if(direction.mirror){ctx.translate(32,0);ctx.scale(-1,1);}
  ctx.drawImage(sheet,state.phase*128,direction.row*128,128,128,0,0,32,32);ctx.restore();
  const zoom=large.getContext('2d');zoom.imageSmoothingEnabled=false;zoom.clearRect(0,0,256,256);zoom.drawImage(small,0,0,256,256);
 }
 for(const button of directionButtons)button.setAttribute('aria-pressed',String(button.dataset.direction===state.direction));
 for(const button of phaseButtons)button.setAttribute('aria-pressed',String(Number(button.dataset.phase)===state.phase));
 document.getElementById('current-phase').textContent=`${directions[state.direction].label} · Phase ${state.phase}: ${phaseNames[state.phase]}`;
 playButton.textContent=state.running?'Anhalten':'Lauf starten';playButton.setAttribute('aria-pressed',String(state.running));
}
function stop(){if(state.timer!==null)clearInterval(state.timer);state.timer=null;state.running=false;}
for(const button of directionButtons)button.addEventListener('click',()=>{state.direction=button.dataset.direction;state.phase=0;paint();});
for(const button of phaseButtons)button.addEventListener('click',()=>{stop();state.phase=Number(button.dataset.phase);paint();});
document.getElementById('stand').addEventListener('click',()=>{stop();state.phase=0;paint();});
playButton.addEventListener('click',()=>{if(state.running)stop();else{state.running=true;state.timer=setInterval(()=>{state.phase=(state.phase+1)%4;paint();},160);}paint();});
for(const radio of document.querySelectorAll('input[name="ground"]'))radio.addEventListener('change',()=>{if(radio.checked)document.getElementById('players').classList.toggle('light',radio.value==='light');});
document.addEventListener('visibilitychange',()=>{if(document.hidden&&state.running){stop();paint();}});
const motion=window.matchMedia('(prefers-reduced-motion: reduce)');
motion.addEventListener('change',()=>{if(motion.matches&&state.running){stop();paint();}});
Promise.all(['mara','bruno'].map(id=>new Promise((resolve,reject)=>{
 const img=new Image();img.onload=()=>{state.images.set(id,img);resolve();};img.onerror=()=>reject(new Error('Die Sheet-Datei von '+id+' konnte nicht geladen werden. Bitte den direkten Dateilink öffnen.'));img.src=`characters/${id}-top-down.png`;
}))).then(()=>{status.textContent='Beide tatsächlichen Sheet-Dateien geladen. Die Animation startet erst auf Knopfdruck.';for(const control of document.querySelectorAll('[data-animation-control]'))control.disabled=false;paint();}).catch(error=>{status.textContent=error.message;status.classList.add('error');});
paint();
