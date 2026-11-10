const SK = {
 rest:{num:"01",module:"屏幕休息",title:["暂时不用，","让屏幕歇一会"],extra:"支持遮黑与调暗，也可临时暂缓",note:"效果示意 · 等待已压缩 · 调暗需设备支持"},
 privacy:{num:"02",module:"隐私保护",title:["想遮的内容，","替你遮好"],extra:"自动保护，也能手动立即保护",note:"效果示意 · 等待已压缩 · 隐私幕不替代锁屏"},
 health:{num:"03",module:"健康提醒",title:["休息这件事，","别总等忙完"],extra:"工作多久、休息多久，按自己的节奏设置",note:"效果示意 · 示例计时 · 不改变真实工作周期"},
 color:{num:"04",module:"色温调节",title:["从白天到夜晚，","跟上你的作息"],extra:"保持原色，或设置自己的工作与休息色温",note:"色调仅作示意 · 需完成屏幕验证后开启"}
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
function Workspace(){return <div className="workspace"><div className="appbar"><span>工作笔记 · 虚构示例</span><span>—　□　×</span></div><div className="work-grid"><aside className="work-nav"><strong>我的桌面</strong><span>今日工作</span><span>项目备忘</span><span>灵感记录</span></aside><main className="work-body"><h3>给今天，留一点从容</h3><div className="work-meta"><span>工作备忘</span><span>待办与想法</span></div><div className="work-line"/><div className="work-line"/><div className="work-line"/><div className="work-line"/><div className="work-line"/><div className="work-card-row"><div className="work-card">整理手头的工作</div><div className="work-card">给休息留一点时间</div></div></main></div></div>}
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
  {kind==="rest"&&<React.Fragment><div className="idle-card" style={{opacity:inWait?1:0}}>示例空闲等待<strong>{Math.max(0,Math.ceil(A-T))} 秒</strong></div><div className="idle-track" style={{opacity:inWait?1:0}}><i style={{width:clamp((T-G)/(A-G),0,1)*100+"%"}}/></div><div className="screen-layer" data-blackout="" style={{background:"#000",opacity:MOTION.enter(T,A,A+.5)*(1-MOTION.enter(T,R+1,R+1.6))}}/></React.Fragment>}
  {kind==="privacy"&&<React.Fragment><div className="idle-card" style={{opacity:inWait?1:0}}>示例保护等待<strong>{Math.max(0,Math.ceil(A-T))} 秒</strong></div><div className="screen-layer curtain" data-curtain="" style={{opacity:closed?1:0}}><img className="forest" src="../assets/forest-mist.png" alt="" style={{opacity:MOTION.enter(T,A+.1,A+1.1)}}/><button ref={targetRef} className="curtain-button" style={{filter:click?"brightness(1.08)":""}}>进入此屏幕</button></div></React.Fragment>}
  {kind==="health"&&<React.Fragment><div className="screen-layer" style={{opacity:resting?1:reminder*.22}}><img className="forest" src="../assets/forest-mist.png" alt=""/></div><div className={"screen-layer "+(resting?"health-resting":"")} style={{opacity:reminder}}><div className="health-card" data-health-state={healthReturned?"ended":resting?"resting":"reminder"}><span className="health-close">×</span><div className="health-label">屏幕良伴 · 健康提醒</div><h3>{resting?"把目光交给远方":"该休息一下了"}</h3><p>{resting?"按自己的节奏，放松片刻。":"专注了这么久，辛苦啦。伸个懒腰吧。"}</p>{resting&&<div className="health-timer">0{Math.floor(seconds/60)}:{String(seconds%60).padStart(2,"0")}</div>}<button ref={targetRef} className="health-primary" style={{filter:click||healthClick2?"brightness(1.08)":""}}>{resting?"提前结束":"开始休息"}</button>{!resting&&<div className="health-secondary"><span>Ⅱ　稍后</span><span>→　跳过本次</span></div>}</div></div></React.Fragment>}
  {kind==="color"&&<React.Fragment><div className="screen-layer"><img className="forest" src="../assets/forest-mist.png" alt=""/><div className="color-overlay" data-warm="" style={{opacity:warm*.58}}/></div><div className="color-pills"><span>{warm>.1?"休息时段 · 暖色示意":"工作时段 · 保持原色"}</span><span>单屏演示</span></div><button className="compare-button" ref={targetRef} style={{background:warm<.1&&T>=R&&T<L?"#b4d6a1":undefined}}>对比原色</button></React.Fragment>}
  <Pointer x={x} y={y} show={show} click={kind==="health"&&T>=R?healthClick2:click}/>
 </React.Fragment>;
}
function Piece(){
 const {T,CUES,duration}=useComposition(),tl=useTimeline(),screenRef=React.useRef(null);
 const kind=window.SCREENKEEPER_DEMO,d=SK[kind],G=CUES.Trigger,A=CUES.Active,R=CUES.Return,L=CUES.Loop;
 const warm=MOTION.enter(T,G,A+1)*(1-MOTION.enter(T,R+1.5,R+1.85));
 const state=kind==="rest"?(T<G||T>=L?"正常工作":T<A?"空闲等待":T<R+1?"屏幕休息中":"检测到活动，恢复显示"):
 kind==="privacy"?(T<G||T>=L?"内容正常显示":T<A?"保护等待":T<R+1.85?"隐私幕保护中":"已明确解除保护"):
 kind==="health"?(T<G||T>=L?"专注工作":T<A+1.7?"休息提醒":T<R+2.2?"休息倒计时":"回到工作"):
 (warm<.1?"保持原色":warm>.95?"自定义暖色":"平滑调整");
 const captions=kind==="rest"?[
 {at:-1,text:"正在用，安心专注。"},
 {at:G,text:"空闲后，按你的规则开始休息。"},
 {at:A,text:"不需要一直亮着，就让它歇一会。"},
 {at:R,text:"检测到所选活动条件，再恢复显示。"},
 {at:L,text:"正在用，安心专注。"}]:
 kind==="privacy"?[
 {at:-1,text:"聊天、资料，不必一直展示。"},
 {at:G,text:"开启规则，等待自动保护。"},
 {at:A,text:"先遮好内容，再换上安静的山林。",until:A+1.8},
 {at:A+1.8,text:"鼠标经过，隐私幕也不会解除。"},
 {at:R,text:"点击“进入此屏幕”，再显示原内容。"},
 {at:L,text:"聊天、资料，不必一直展示。"}]:
 kind==="health"?[
 {at:-1,text:"工作有节奏，休息也应该。"},
 {at:G,text:"到点轻轻提醒，不抢走你的决定权。"},
 {at:A,text:"点击开始休息，才进入休息倒计时。"},
 {at:R,text:"准备好了，也能提前结束。"},
 {at:L,text:"工作有节奏，休息也应该。"}]:[
 {at:-1,text:"白天保持原色，按习惯工作。"},
 {at:G,text:"到了设定时段，逐渐切换暖色。"},
 {at:A,text:"每块屏幕，都能安排自己的作息。"},
 {at:R,text:"对比原色，感受两种氛围。"},
 {at:L,text:"白天保持原色，按习惯工作。"}];
 React.useLayoutEffect(()=>{
  window.__animStage={duration,captureActive:document.documentElement.hasAttribute("data-capture"),setTime(time){document.querySelector("[data-om-exportable-video-with-duration-secs]").dispatchEvent(new CustomEvent("data-om-seek-to-time-frame",{detail:{time,sync:true}}));},setPlaying:tl.setPlaying};
 },[duration,tl.setPlaying]);
 const zoom=1+.018*MOTION.enter(T,A-1,A+1)*(1-MOTION.enter(T,R+2,L));
 return <div className="piece" data-screen-label={d.module+" · "+Math.floor(T)+"秒"} data-demo={kind} data-time={T.toFixed(3)} data-state={state}>
  <div className="brand"><img src="../assets/logo.svg" alt=""/>屏幕良伴 <span>ScreenKeeper</span></div><div className="series">功能演示 {d.num} / 04</div><div className="hairline"/>
  <div className="eyebrow">{d.module}</div><h1 className="headline"><span>{d.title[0]}</span><span>{d.title[1]}</span></h1><div className="promise">单屏多屏，皆由你心定</div>
  <div className="state-line"><span className="state-pill">{state}</span><span className="single-label">{kind==="health"?"单屏多屏均适用 · 独立休息周期":"以单屏为例 · 多屏可独立设置"}</span></div>
  <div className="monitor-group" style={{transform:"scale("+zoom+")"}}><div className="monitor-frame"><div className="screen" ref={screenRef}><DemoScreen kind={kind} T={T} CUES={CUES} screenRef={screenRef}/></div></div><div className="stand"/></div>
  {kind==="color"?<div className="color-track"><span>原色</span><div className="rail"><div className="fill" style={{width:warm*100+"%"}}/><i className="knob" style={{left:warm*100+"%"}}/></div><span>暖色示意</span></div>:<div className="module-extra">{d.extra}</div>}
  <Captions items={captions} style={{bottom:126,left:40,right:40,fontFamily:'"Microsoft YaHei",sans-serif',fontSize:29,color:"#153b30",textShadow:"none",fontWeight:600}}/>
  <div className="footer-line"/><div className="footnote"><span>{d.note}</span><span>screenkeeper.skyrrats.com</span></div>
 </div>;
}
function App(){
 const [t,setTweak]=useTweaks(window.TWEAK_DEFAULTS),[panel,setPanel]=React.useState(false);
 const capture=new URLSearchParams(location.search).has("capture");
 return <div className={t.motionEditor?"":"hide-editor"}>
  <CompositionStage width={900} height={1200} scenes={window.OM_SCENES} playback={window.OM_PLAYBACK} autoplay={!capture&&!matchMedia("(prefers-reduced-motion: reduce)").matches}><Piece/></CompositionStage>
  {!capture&&<React.Fragment><button className="tweaks-open" onClick={()=>setPanel(!panel)}>{panel?"收起设置":"播放设置"}</button>{panel&&<div className="motion-panel"><h3>演示播放设置</h3><TweakToggle label="Motion editor · 时间轴" value={t.motionEditor} onChange={v=>setTweak("motionEditor",v)}/><p>空格播放 / 暂停，方向键逐帧查看。视频仅作效果示意。</p><a href="../index.html">返回四功能预览</a></div>}</React.Fragment>}
 </div>;
}
ReactDOM.createRoot(document.getElementById("root")).render(<App/>);
