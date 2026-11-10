const SK = {
 rest:{num:"01",module:"Screen rest",title:["Let your screen","take a breather."],extra:"Black out or dim idle screens. Snooze when you need to.",note:"Illustrative demo · Shortened wait · Dimming needs device support"},
 privacy:{num:"02",module:"Privacy protection",title:["Your screen.","Your private space."],extra:"Scheduled protection, with manual control when you need it.",note:"Illustrative demo · Shortened wait · Not a Windows lock"},
 health:{num:"03",module:"Break reminders",title:["Take a break.","Work can wait."],extra:"Set work and break durations to suit your day.",note:"Illustrative demo · Sample timer · No real work cycle changed"},
 color:{num:"04",module:"Color temperature",title:["From day to night,","at your pace."],extra:"Keep original colors, or set work and rest temperatures.",note:"Illustrative colors · Verify your display before enabling"}
};
const MOTION={
 enter:(t,a,b)=>Easing.easeInOutCubic(clamp((t-a)/(b-a),0,1)),
 draw:(t,a,b,from=0,to=1)=>interpolate([a,b],[from,to],Easing.easeInOutCubic)(t),
 pop:(t,a,b)=>Math.sin(Math.PI*clamp((t-a)/(b-a),0,1))
};
function useTarget(rootRef,targetRef,frameMode){
 const [p,setP]=React.useState({x:380,y:220});
 React.useLayoutEffect(()=>{
  const update=()=>{const r=rootRef.current,b=targetRef.current;if(!r||!b)return;const a=r.getBoundingClientRect(),z=b.getBoundingClientRect();const scale=a.width/r.offsetWidth;setP({x:(z.left-a.left+z.width/2)/scale,y:(z.top-a.top+z.height/2)/scale});};
  update();const observer=new ResizeObserver(update);if(rootRef.current)observer.observe(rootRef.current);window.addEventListener("resize",update);return()=>{observer.disconnect();window.removeEventListener("resize",update);};
 },[frameMode]);return p;
}
function Pointer({x,y,show=1,click=0}){return <div className="pointer" style={{left:x,top:y,opacity:show}}><i className="click-ring" style={{opacity:click,transform:"scale("+(0.7+click*0.6)+")"}}/></div>}
function Workspace(){return <div className="workspace"><div className="appbar"><span>Work notes · Fictional example</span><span>—　□　×</span></div><div className="work-grid"><aside className="work-nav"><strong>My workspace</strong><span>Today</span><span>Projects</span><span>Ideas</span></aside><main className="work-body"><h3>Make room for a calmer day</h3><div className="work-meta"><span>Work notes</span><span>Tasks & ideas</span></div><div className="work-line"/><div className="work-line"/><div className="work-line"/><div className="work-line"/><div className="work-line"/><div className="work-card-row"><div className="work-card">One thing at a time</div><div className="work-card">Make time for a break</div></div></main></div></div>}
function DemoScreen({kind,T,CUES,screenRef}){
 const targetRef=React.useRef(null), p=useTarget(screenRef,targetRef,kind==='health'&&T>=CUES.Active+1.7&&T<CUES.Return+2.2);
 const A=CUES.Active,R=CUES.Return,L=CUES.Loop,G=CUES.Trigger;
 const inWait=T>=G&&T<A, clickAt=kind==="privacy"?R+1.5:kind==="health"?A+1.5:R+1.5;
 const click=MOTION.pop(T,clickAt-.12,clickAt+.3);
 let target={x:p.x,y:p.y};
 let progress=MOTION.enter(T,clickAt-1.6,clickAt-.2);
 let x=target.x+110*(1-progress), y=target.y+105*(1-progress);
 let show=T>=clickAt-1.7&&T<clickAt+.55?1:0;
 const closed=T>=A&&T<R+1.85;
 const reminder=MOTION.enter(T,G,G+.65)*(1-MOTION.enter(T,R+2,R+2.35));
 const resting=T>=A+1.7&&T<R+2.2;
 const seconds=Math.max(0,300-Math.floor(Math.max(0,T-(A+1.7))));
 const warm=MOTION.enter(T,G,A+1)*(1-MOTION.enter(T,R+1.5,R+1.85));
 if(kind==="rest"){x=MOTION.draw(T,R-.5,R+1.4,680,440);y=MOTION.draw(T,R-.5,R+1.4,390,260);show=T>=R-.5&&T<R+2?1:0;}
 if(kind==="color")show=T>=R-.1&&T<L?1:0;
 if(kind==="privacy"&&T>A+1&&T<R-.5){const q=MOTION.enter(T,A+1,A+4);x=80+550*q;y=96;show=1;}
 const healthReturned=T>=R+2.2;
 const healthClick2=MOTION.pop(T,R+1.85,R+2.15);
 const healthEndMove=MOTION.enter(T,R,R+1.85);
 if(kind==="health"&&T>=R){x=p.x+90*(1-healthEndMove);y=p.y+80*(1-healthEndMove);show=T<R+2.3?1:0;}
 return <React.Fragment>
  <Workspace/>
  {kind==="rest"&&<React.Fragment><div className="idle-card" style={{opacity:inWait?1:0}}>Idle delay (demo)<strong>{Math.max(0,Math.ceil(A-T))} s</strong></div><div className="idle-track" style={{opacity:inWait?1:0}}><i style={{width:clamp((T-G)/(A-G),0,1)*100+"%"}}/></div><div className="screen-layer" data-blackout="" style={{background:"#000",opacity:MOTION.enter(T,A,A+.5)*(1-MOTION.enter(T,R+1,R+1.6))}}/></React.Fragment>}
  {kind==="privacy"&&<React.Fragment><div className="idle-card" style={{opacity:inWait?1:0}}>Privacy delay (demo)<strong>{Math.max(0,Math.ceil(A-T))} s</strong></div><div className="screen-layer curtain" data-curtain="" style={{opacity:closed?1:0}}><img className="forest" src="../assets/forest-mist.png" alt="" style={{opacity:MOTION.enter(T,A+.1,A+1.1)}}/><button ref={targetRef} className="curtain-button" style={{filter:click?"brightness(1.08)":""}}>Enter this screen</button></div></React.Fragment>}
  {kind==="health"&&<React.Fragment><div className="screen-layer" style={{opacity:resting?1:reminder*.22}}><img className="forest" src="../assets/forest-mist.png" alt=""/></div><div className={"screen-layer "+(resting?"health-resting":"")} style={{opacity:reminder}}><div className="health-card" data-health-state={healthReturned?"ended":resting?"resting":"reminder"}><span className="health-close">×</span><div className="health-label">ScreenKeeper · Break reminders</div><h3>{resting?"Look into the distance":"Time for a break"}</h3><p>{resting?"Take a moment. Look away from the screen.":"Blink, stretch, take a moment."}</p>{resting&&<div className="health-timer">0{Math.floor(seconds/60)}:{String(seconds%60).padStart(2,"0")}</div>}<button ref={targetRef} className="health-primary" style={{filter:click||healthClick2?"brightness(1.08)":""}}>{resting?"End early":"Start break"}</button>{!resting&&<div className="health-secondary"><span>Ⅱ　Snooze</span><span>→　Skip this time</span></div>}</div></div></React.Fragment>}
  {kind==="color"&&<React.Fragment><div className="screen-layer"><img className="forest" src="../assets/forest-mist.png" alt=""/><div className="color-overlay" data-warm="" style={{opacity:warm*.58}}/></div><div className="color-pills"><span>{warm>.1?"Rest hours · Warm colors":"Work hours · Original colors"}</span><span>Single screen</span></div><button className="compare-button" ref={targetRef} style={{background:warm<.1&&T>=R&&T<L?"#b4d6a1":undefined}}>Compare original</button></React.Fragment>}
  <Pointer x={x} y={y} show={show} click={kind==="health"&&T>=R?healthClick2:click}/>
 </React.Fragment>;
}
function Piece(){
 const {T,CUES,duration}=useComposition(),tl=useTimeline(),screenRef=React.useRef(null);
 const kind=window.SCREENKEEPER_DEMO,d=SK[kind],G=CUES.Trigger,A=CUES.Active,R=CUES.Return,L=CUES.Loop;
 const warm=MOTION.enter(T,G,A+1)*(1-MOTION.enter(T,R+1.5,R+1.85));
 const state=kind==="rest"?(T<G||T>=L?"Screen in use":T<A?"Waiting while idle":T<R+1?"Screen resting":"Activity detected · Restored"):
 kind==="privacy"?(T<G||T>=L?"Your content, on screen":T<A?"Waiting to protect":T<R+1.85?"Privacy curtain active":"Revealed by you"):
 kind==="health"?(T<G||T>=L?"Time to focus":T<A+1.7?"Break reminder":T<R+2.2?"Break countdown":"Back to work"):
 (warm<.1?"Original colors":warm>.95?"Custom warm colors":"Gradual transition");
 const captions=kind==="rest"?[
 {at:-1,text:"Stay focused while your screen is in use."},
 {at:G,text:"When idle, it rests on your terms."},
 {at:A,text:"Nothing to show? Let the screen take a breather."},
 {at:R,text:"Your chosen activity brings the display back."},
 {at:L,text:"Stay focused while your screen is in use."}]:
 kind==="privacy"?[
 {at:-1,text:"Your chats and documents need not stay on display."},
 {at:G,text:"Set your rules. Let protection follow."},
 {at:A,text:"Content is covered first. The forest fades in next.",until:A+1.8},
 {at:A+1.8,text:"Moving the pointer does not reveal your content."},
 {at:R,text:"Choose “Enter this screen” to reveal your content."},
 {at:L,text:"Your chats and documents need not stay on display."}]:
 kind==="health"?[
 {at:-1,text:"A rhythm for work. A little room for rest."},
 {at:G,text:"A gentle reminder. The choice stays yours."},
 {at:A,text:"The countdown starts when you choose Start break."},
 {at:R,text:"Ready to return? You can end the break early."},
 {at:L,text:"A rhythm for work. A little room for rest."}]:[
 {at:-1,text:"Keep original colors during your work hours."},
 {at:G,text:"Ease into warmer colors at your chosen time."},
 {at:A,text:"Set a separate schedule for each display."},
 {at:R,text:"Compare with original colors whenever you need to."},
 {at:L,text:"Keep original colors during your work hours."}];
 React.useLayoutEffect(()=>{
  window.__animStage={duration,captureActive:document.documentElement.hasAttribute("data-capture"),setTime(time){document.querySelector("[data-om-exportable-video-with-duration-secs]").dispatchEvent(new CustomEvent("data-om-seek-to-time-frame",{detail:{time,sync:true}}));},setPlaying:tl.setPlaying};
 },[duration,tl.setPlaying]);
 const zoom=1+.018*MOTION.enter(T,A-1,A+1)*(1-MOTION.enter(T,R+2,L));
 return <div className="piece" data-screen-label={d.module+" · "+Math.floor(T)+"s"} data-demo={kind} data-time={T.toFixed(3)} data-state={state}>
  <div className="brand"><img src="../assets/logo.svg" alt=""/><span>ScreenKeeper</span></div><div className="series">FEATURE DEMO {d.num} / 04</div><div className="hairline"/>
  <div className="eyebrow">{d.module}</div><h1 className="headline"><span>{d.title[0]}</span><span>{d.title[1]}</span></h1><div className="promise">One screen or many. Your way.</div>
  <div className="state-line"><span className="state-pill">{state}</span><span className="single-label">{kind==="health"?"One work cycle across your displays":"Single-screen demo · Per-display settings"}</span></div>
  <div className="monitor-group" style={{transform:"scale("+zoom+")"}}><div className="monitor-frame"><div className="screen" ref={screenRef}><DemoScreen kind={kind} T={T} CUES={CUES} screenRef={screenRef}/></div></div><div className="stand"/></div>
  {kind==="color"?<div className="color-track"><span>Original</span><div className="rail"><div className="fill" style={{width:warm*100+"%"}}/><i className="knob" style={{left:warm*100+"%"}}/></div><span>Warm colors</span></div>:<div className="module-extra">{d.extra}</div>}
  <Captions items={captions} style={{bottom:126,left:40,right:40,fontFamily:'"Segoe UI",sans-serif',fontSize:26,color:"#153b30",textShadow:"none",fontWeight:600}}/>
  <div className="footer-line"/><div className="footnote"><span>{d.note}</span><span>screenkeeper.skyrrats.com</span></div>
 </div>;
}
function App(){
 const [t,setTweak]=useTweaks(window.TWEAK_DEFAULTS),[panel,setPanel]=React.useState(false);
 const capture=new URLSearchParams(location.search).has("capture");
 return <div className={t.motionEditor?"":"hide-editor"}>
  <CompositionStage width={900} height={1200} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} autoplay={!capture&&!matchMedia("(prefers-reduced-motion: reduce)").matches}><Piece/></CompositionStage>
  {!capture&&<React.Fragment><button className="tweaks-open" onClick={()=>setPanel(!panel)}>{panel?"Hide settings":"Playback settings"}</button>{panel&&<div className="motion-panel"><h3>Playback settings</h3><TweakToggle label="Motion editor" value={t.motionEditor} onChange={v=>setTweak("motionEditor",v)}/><p>Space to play or pause. Arrow keys to step through frames. Illustrative demo.</p><a href="../index.html">Back to all four demos</a></div>}</React.Fragment>}
 </div>;
}
ReactDOM.createRoot(document.getElementById("root")).render(<App/>);
