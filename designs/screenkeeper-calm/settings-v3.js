'use strict';
(() => {
  const {h,Icon,Button,Toggle,Choices,TimeInput,CurtainPreview,AppearancePanel,CopyDialog,PauseDialog,AppPicker,AppTargetPanel,AppTestDialog,appName,factors,PrivacyPanelV3,PauseV3,DesktopTestV3,ModalShell}=SK;
  const {useState,useRef,useEffect}=React;
  const clone=x=>JSON.parse(JSON.stringify(x));
  const initial={
    1:{managed:false,privacy:'off',rememberedPrivacy:'curtain',privacyTime:'30',privacyUnit:'seconds',pauseTime:'15',pauseUnit:'minutes',targetApp:null,appNotRunning:'curtain',appElsewhere:'curtain',curtainStyle:'solid',images:[],imageFit:'cover',slideTime:'10',slideUnit:'seconds',sleep:'none',sleepTime:'5',unit:'minutes',brightness:20,lower:false,mouse:true,active:false,input:false},
    2:{managed:true,privacy:'curtain',rememberedPrivacy:'curtain',privacyTime:'30',privacyUnit:'seconds',pauseTime:'15',pauseUnit:'minutes',targetApp:null,appNotRunning:'curtain',appElsewhere:'curtain',curtainStyle:'solid',images:[],imageFit:'cover',slideTime:'10',slideUnit:'seconds',sleep:'black',sleepTime:'5',unit:'minutes',brightness:20,lower:false,mouse:true,active:false,input:false}
  };
  initial[3]=clone(initial[1]);
  const screenIds=Object.keys(initial).map(Number);
  function App(){
    const [lang,setLang]=useState('zh');
    const t=(zh,en)=>lang==='zh'?zh:en;
    const [size,setSize]=useState('normal'),[offset,setOffset]=useState({x:0,y:0});
    const [demo,setDemo]=useState(false),[scenario,setScenario]=useState('normal'),[failSave,setFailSave]=useState(false);
    const [selected,setSelected]=useState(2),[tab,setTab]=useState('privacy'),[general,setGeneral]=useState(false);
    const [draft,setDraft]=useState(()=>clone(initial)),[saved,setSaved]=useState(()=>clone(initial));
    const [startup,setStartup]=useState(false),[savedStartup,setSavedStartup]=useState(false);
    const [runtime,setRuntime]=useState({}),[dialog,setDialog]=useState(null),[toast,setToast]=useState(null),[hidden,setHidden]=useState(false);
    const [pauses,setPauses]=useState({}),[readingImages,setReadingImages]=useState(false);
    const imageUrls=useRef([]),imageReadVersion=useRef(0);
    const stageRef=useRef(null),dragRef=useRef(null),toastTimer=useRef(null),refreshTimer=useRef(null);
    const config=draft[selected], old=saved[selected], unsupported=scenario==='unsupported'&&selected===2;
    const dirtyIds=screenIds.filter(id=>JSON.stringify(draft[id])!==JSON.stringify(saved[id]));
    const dirty=dirtyIds.length>0||startup!==savedStartup;
    const anyProtected=screenIds.some(id=>saved[id].managed&&saved[id].privacy!=='off');
    const notify=(text,error=false)=>{clearTimeout(toastTimer.current);setToast({text,error});toastTimer.current=setTimeout(()=>setToast(null),3500);};
    useEffect(()=>()=>{clearTimeout(toastTimer.current);clearTimeout(refreshTimer.current);imageUrls.current.forEach(url=>URL.revokeObjectURL(url));},[]);
    useEffect(()=>{const timer=setInterval(()=>{const expired=Object.keys(pauses).filter(id=>pauses[id].until<=Date.now());if(expired.length){setRuntime(prev=>{const next={...prev};expired.forEach(id=>{if(next[id]==='paused')delete next[id];});return next;});setPauses(prev=>{const next={...prev};expired.forEach(id=>delete next[id]);return next;});}},1000);return()=>clearInterval(timer);},[pauses]);
    useEffect(()=>{document.documentElement.lang=lang==='zh'?'zh-CN':'en';setToast(null);},[lang]);
    useEffect(()=>{const fn=e=>{if(e.key==='Escape')setDialog(current=>current?.kind==='protect'?current:null);};window.addEventListener('keydown',fn);return()=>window.removeEventListener('keydown',fn);},[]);
    const patch=change=>setDraft(current=>({...current,[selected]:{...current[selected],...(change.privacy&&change.privacy!=='off'?{rememberedPrivacy:change.privacy}:{}),...change}}));
    const monitorName=id=>t(`显示器 ${id}`,`Display ${id}`);
    const privacyName=value=>({off:t('未启用','Not enabled'),curtain:t('隐私幕','Privacy curtain'),desktop:t('回到桌面','Show desktop'),app:t('指定应用','Target app')})[value];
    const sleepName=value=>({none:t('不休眠','Stay on'),black:t('遮黑','Black out'),dim:t('调暗','Dim')})[value];
    const unitName=value=>({seconds:t('秒','sec'),minutes:t('分钟','min'),hours:t('小时','hr')})[value];
    const validInteger=(value,max)=>/^\d+$/.test(value)&&Number(value)>=1&&Number(value)<=max;
    function errors(c,id){
      if(!c.managed)return[];
      const e=[];
      if(c.privacy==='app'){
        if(!c.targetApp)e.push(t('请选择一个指定应用。','Choose a target app.'));
        else if(screenIds.some(other=>other!==id&&draft[other].managed&&draft[other].privacy==='app'&&draft[other].targetApp?.id===c.targetApp.id))e.push(t('同一应用窗口不能同时用于多屏，请为此屏重新选择窗口。','The same app window cannot protect multiple displays. Choose another window.'));
      }
      if(c.privacy!=='off'){
        if(!validInteger(c.privacyTime,86400/factors[c.privacyUnit]))e.push(t('隐私等待时间需为正整数，总时长不超过 24 小时。','Enter a positive whole privacy delay, up to 24 hours.'));
        if(!validInteger(c.pauseTime,86400/factors[c.pauseUnit]))e.push(t('暂缓时长需为正整数，总时长不超过 24 小时。','Enter a positive whole pause duration, up to 24 hours.'));
        if(c.privacy==='curtain'&&c.curtainStyle==='images'&&!c.images.length)e.push(t('请选择至少一张隐私幕图片，或使用默认纯色。','Choose at least one curtain image, or use the default solid background.'));
        if(c.privacy==='curtain'&&c.curtainStyle==='images'&&c.images.length>1&&!validInteger(c.slideTime,3600/factors[c.slideUnit]))e.push(t('轮播间隔需为正整数，总时长不超过 1 小时。','Enter a positive whole slideshow interval, up to 1 hour.'));
      }
      if(c.privacy==='off'&&c.sleep==='none')e.push(t('请至少启用隐私保护或一种屏幕休息方式。','Enable privacy protection or a sleep action.'));
      if(c.sleep!=='none'){
        const factor={seconds:1000,minutes:60000,hours:3600000}[c.unit];
        if(!validInteger(c.sleepTime,Math.floor(2147483647/factor)))e.push(t('请填写有效的休息等待时间。','Enter a valid sleep delay.'));
        if(!c.mouse&&!c.active&&!c.input)e.push(t('请至少选择一项保持唤醒的条件。','Select at least one keep-awake condition.'));
        if(c.sleep==='dim'&&id===2&&scenario==='unsupported')e.push(t('此显示器不支持调暗，请选择遮黑或不休眠。','Dimming is unavailable. Choose Black out or Stay on.'));
      }return e;
    }
    const allErrors=screenIds.flatMap(id=>errors(draft[id],id)), currentErrors=errors(config,selected);
    function summary(c){if(!c.managed)return t('尚未启用管理','Not managed');return [c.privacy!=='off'?`${privacyName(c.privacy)}${c.privacy==='app'?' / '+appName(c.targetApp,t):''} · ${c.privacyTime} ${unitName(c.privacyUnit)}`:t('隐私保护关闭','Privacy off'),c.sleep==='none'?sleepName(c.sleep):`${sleepName(c.sleep)} · ${c.sleepTime} ${unitName(c.unit)}`].join(' / ');}
    function runtimeText(){return runtime[selected]==='paused'?t('已暂缓自动保护 · ','Protection paused · ')+`${pauses[selected]?.time||old.pauseTime} ${unitName(pauses[selected]?.unit||old.pauseUnit)}`:runtime[selected]==='protected'?t('隐私幕已开启','Privacy curtain is active'):runtime[selected]==='application'?t('已显示指定应用 · ','Target app shown · ')+appName(old.targetApp,t):runtime[selected]==='desktop'?t('已回到桌面','Desktop is shown'):!old.managed||old.privacy==='off'?t('未启用自动保护','Automatic protection off'):t('已生效 · ','Saved · ')+privacyName(old.privacy)+' · '+old.privacyTime+' '+unitName(old.privacyUnit);}
    function save(){if(readingImages)return false;if(allErrors.length){notify(allErrors[0],true);return false;}if(failSave){notify(t('保存失败。更改已保留，请检查配置目录权限后重试。','Save failed. Your changes are kept. Check folder permissions and retry.'),true);return false;}setPauses(prev=>Object.fromEntries(Object.entries(prev).filter(([id])=>draft[id].managed&&draft[id].privacy!=='off')));setSaved(clone(draft));setSavedStartup(startup);setRuntime(prev=>Object.fromEntries(Object.entries(prev).filter(([id,state])=>state==='paused'&&draft[id].managed&&draft[id].privacy!=='off')));notify(t('所有设置已保存','All settings saved'));return true;}
    function refresh(){if(dirty)return;setScenario('loading');clearTimeout(refreshTimer.current);refreshTimer.current=setTimeout(()=>{setScenario('normal');notify(t('已重新检测到 3 块显示器','3 displays detected'));},700);}
    function protect(){
      const ids=screenIds.filter(id=>saved[id].managed&&saved[id].privacy!=='off');if(!ids.length)return;
      const outcomes={};
      ids.forEach(id=>{const c=saved[id];outcomes[id]=c.privacy==='curtain'?'protected':c.privacy==='desktop'?'desktop':c.targetApp&&(c.targetApp.manual?c.appNotRunning==='launch':c.targetApp.location===id||c.appElsewhere==='move')?'application':'protected';});
      setRuntime(prev=>({...prev,...outcomes}));
      const queue=ids.filter(id=>outcomes[id]==='protected');
      if(queue.length){setToast(null);setDialog({kind:'protect',ids,queue,config:clone(saved[queue[0]])});}
      else notify(t(`已演示 ${ids.length} 块屏幕的已保存保护规则`,`Simulated saved protection rules for ${ids.length} display(s)`));
    }
    function reset(){imageReadVersion.current++;setPauses({});setDraft(clone(initial));setSaved(clone(initial));setSelected(2);setTab('privacy');setGeneral(false);setStartup(false);setSavedStartup(false);setRuntime({});setScenario('normal');setFailSave(false);setDialog(null);setHidden(false);setOffset({x:0,y:0});notify(t('演示已重置','Demo reset'));}
    function changeSize(next){setSize(next);setOffset({x:0,y:0});setHidden(false);}
    function closeWindow(){if(dirty)setDialog({kind:'close'});else setHidden(true);}
    function pointerDown(e){if(e.button!==0||e.target.closest('button,select,input,a'))return;const rect=e.currentTarget.parentElement.getBoundingClientRect();dragRef.current={x:e.clientX,y:e.clientY,base:offset,max:size==='maximized',ratio:(e.clientX-rect.left)/rect.width,moved:false};e.currentTarget.setPointerCapture(e.pointerId);}
    function pointerMove(e){const d=dragRef.current;if(!d)return;const dx=e.clientX-d.x,dy=e.clientY-d.y;if(!d.moved&&Math.abs(dx)+Math.abs(dy)<5)return;d.moved=true;if(d.max){const s=stageRef.current.getBoundingClientRect(),w=Math.min(1040,s.width-56),ht=Math.min(700,s.height-24);d.base={x:e.clientX-s.left-s.width/2+(.5-d.ratio)*w,y:e.clientY-s.top-s.height/2+ht/2-22};d.x=e.clientX;d.y=e.clientY;d.max=false;setSize('normal');setOffset(d.base);}else{const s=stageRef.current.getBoundingClientRect();setOffset({x:Math.max(-s.width/2+160,Math.min(s.width/2-160,d.base.x+dx)),y:Math.max(-100,Math.min(s.height/2-60,d.base.y+dy))});}}
    function errorBlock(){return currentErrors.length?h('div',{className:'invalid',role:'alert'},currentErrors.map((e,i)=>h('p',{key:i},e))):null;}
    function field(label,description,control){return h('div',{className:'field-row'},h('div',null,h('h3',null,label),description&&h('p',null,description)),h('div',{className:'field-control'},control));}
    function privacyPanel(){return h(PrivacyPanelV3,{config,old,patch,t,issues:currentErrors,busy:readingImages,
      runtimeText:runtimeText(),paused:runtime[selected]==='paused',
      onPause:()=>{if(runtime[selected]==='paused'){setRuntime(prev=>({...prev,[selected]:''}));setPauses(prev=>{const next={...prev};delete next[selected];return next;});}else{setToast(null);setDialog({kind:'pause'});}},
      onPreview:()=>{setToast(null);setDialog({kind:'protect',config:clone(config),ids:[],queue:[]});},
      onImages:()=>setDialog({kind:'backgrounds'}),
      onChoose:()=>{setToast(null);setDialog({kind:'appPicker'});},
      onTest:()=>{setToast(null);setDialog({kind:'appTest',config:clone(config)});},
      onDesktopTest:()=>{setToast(null);setDialog({kind:'desktopTest',config:clone(config)});},
      onHelp:()=>setDialog({kind:'help'})});}
    function sleepPanel(){return h('div',{className:'editor-inner'},
      h('div',{className:'section-heading'},h('div',null,h('h3',null,t('选择屏幕的休息方式','Choose how the screen rests')),h('p',null,t('隐私保护先执行，这里的设置负责遮黑或调暗。','Privacy runs first; these settings control blackout and brightness.')))),
      h(Choices,{value:config.sleep,onChange:value=>patch({sleep:value}),label:t('屏幕休息方式','Sleep action'),options:[
        {value:'none',icon:'sun',title:t('不休眠','Stay on'),note:t('只使用隐私保护','Use privacy protection only')},
        {value:'black',icon:'moon',title:t('遮黑','Black out'),note:t('用黑色覆盖屏幕','Cover the screen in black')},
        {value:'dim',icon:'sun',title:t('调暗','Dim'),note:unsupported?t('不支持 DDC/CI','DDC/CI unavailable'):t('降低显示器亮度','Lower display brightness'),disabled:unsupported}
      ]}),
      unsupported&&h('div',{className:'outcome'},h(Icon,{name:'info',size:15}),t('此显示器暂不支持亮度调节，可使用遮黑。','This display does not support brightness control. Blackout remains available.')),
      config.sleep!=='none'?h(React.Fragment,null,
        field(t('空闲多久后休息','Rest after inactivity'),null,h(React.Fragment,null,h('input',{type:'number',min:1,value:config.sleepTime,'aria-label':t('休息等待时间','Sleep delay'),onChange:e=>patch({sleepTime:e.target.value})}),h('select',{value:config.unit,'aria-label':t('时间单位','Time unit'),onChange:e=>patch({unit:e.target.value})},h('option',{value:'seconds'},t('秒','Seconds')),h('option',{value:'minutes'},t('分钟','Minutes')),h('option',{value:'hours'},t('小时','Hours'))))),
        config.sleep==='dim'?field(t('目标亮度','Target brightness'),null,h(React.Fragment,null,h('input',{type:'range',min:0,max:100,value:config.brightness,'aria-label':t('目标亮度','Target brightness'),onChange:e=>patch({brightness:Number(e.target.value)})}),`${config.brightness}%`)):h('label',{className:'check-row',style:{marginTop:12}},h('input',{type:'checkbox',checked:config.lower,disabled:unsupported,onChange:e=>patch({lower:e.target.checked})}),h('span',null,t('同时将显示器亮度降至零','Also reduce display brightness to zero'),h('small',null,t('需要显示器支持 DDC/CI','Requires DDC/CI support')))),
        h('div',{className:'section-heading',style:{marginTop:20,marginBottom:0}},h('div',null,h('h3',null,t('满足任一条件，保持唤醒','Keep awake when any condition applies')),h('p',null,t('这些条件只影响屏幕休息，不影响隐私计时。','These conditions affect sleep only, not the privacy timer.')))),
        h('div',{className:'check-list'},[
          ['mouse',t('鼠标位于此屏幕','The pointer is on this screen'),t('光标停留在这里时，屏幕不休息','Keep awake even while the pointer is stationary')],
          ['active',t('当前活动窗口位于此屏幕','The active window is on this screen'),t('前台窗口所在的屏幕保持唤醒','Keep the foreground window’s display awake')],
          ['input',t('任意屏幕有键盘或鼠标输入','There is keyboard or mouse input anywhere'),t('在其他屏幕操作时，也保持这块屏幕唤醒','Activity on another display also keeps this one awake')]
        ].map(([key,label,description])=>h('label',{className:'check-row',key},h('input',{type:'checkbox',checked:config[key],onChange:e=>patch({[key]:e.target.checked})}),h('span',null,label,h('small',null,description)))))
        ):h('div',{className:'muted-box'},h(Icon,{name:'sun',size:18}),t('屏幕保持当前亮度。隐私保护仍会按自己的等待时间执行。','The display keeps its current brightness. Privacy still follows its own timer.')),
      errorBlock());}
    function generalPanel(){return h('div',{className:'editor-inner general'},
      field(t('界面语言','Interface language'),t('切换后立即生效，不丢失未保存的修改','Applies immediately and keeps unsaved changes'),h('select',{value:lang,'aria-label':t('设置语言','Settings language'),onChange:e=>setLang(e.target.value)},h('option',{value:'zh'},'简体中文'),h('option',{value:'en'},'English'))),
      field(t('随 Windows 启动','Start with Windows'),t('登录后，在托盘中启动多屏管家','Start ScreenKeeper in the tray when you sign in'),h(Toggle,{checked:startup,onChange:setStartup,label:t('随 Windows 启动','Start with Windows')})),
      field(t('关闭主窗口','Closing the main window'),t('继续在后台执行已保存的屏幕规则','Keep saved display rules running in the background'),h('span',null,t('隐藏到托盘','Hide to tray'))),
      h('div',{className:'product-about'},h('img',{src:'settings-v2-icon.svg',alt:''}),h('div',null,h('h3',{style:{fontWeight:500,fontSize:14}},t('多屏管家','ScreenKeeper')),h('p',null,t('让每一块屏幕，各得其所。','A little more calm, on every screen.')))),
      null);}
    const alternate=['empty','loading','error'].includes(scenario);
    function specialState(){const loading=scenario==='loading',empty=scenario==='empty';return h('div',{className:'state-view'},h('div',{className:'state-glyph'},h(Icon,{name:loading?'refresh':empty?'screen':'info',size:28,className:loading?'spin':''})),h('h3',null,loading?t('正在检测显示器','Detecting displays'):empty?t('还没有可管理的显示器','No displays to manage'):t('暂时无法读取显示器','Could not read displays')),h('p',null,loading?t('正在读取连接状态，请稍候。','Reading connections. Please wait.'):empty?t('连接显示器后重新检测，即可为它设置独立规则。','Connect a display, then refresh to set up its own rules.'):t('已有设置不会丢失，请检查显示器连接后重试。','Your settings are kept. Check your display connections and try again.')),!loading&&h(Button,{icon:'refresh',onClick:refresh},t('重新检测','Retry detection')));}
    async function readImages(files){
      if(!files.length||readingImages)return;
      const owner=selected,version=imageReadVersion.current;
      if(files.length+draft[owner].images.length>12){notify(t('每块屏幕最多选择 12 张图片。','Choose up to 12 images per display.'),true);return;}
      setReadingImages(true);
      const loaded=[];let rejected=0;
      for(const file of files){
        if(!['image/png','image/jpeg','image/webp'].includes(file.type)||file.size>10*1024*1024){rejected++;continue;}
        const src=URL.createObjectURL(file);
        const valid=await new Promise(resolve=>{const image=new Image();image.onload=()=>resolve(true);image.onerror=()=>resolve(false);image.src=src;});
        if(valid){imageUrls.current.push(src);loaded.push({name:file.name,src});}else{URL.revokeObjectURL(src);rejected++;}
      }
      if(version===imageReadVersion.current){
        if(loaded.length)setDraft(prev=>({...prev,[owner]:{...prev[owner],images:[...prev[owner].images,...loaded].slice(0,12),curtainStyle:'images'}}));
        if(rejected)notify(t(`${rejected} 张图片无法读取或超出限制，其余有效图片已保留。`,`${rejected} image(s) were unreadable or exceeded limits. Valid images were kept.`),true);
      }
      setReadingImages(false);
    }
    function applyToScreens(ids){
      const source=clone(config);
      setDraft(prev=>{const next={...prev};ids.forEach(id=>{
        next[id]={...clone(source),managed:prev[id].managed};
        if(id===2&&scenario==='unsupported'){if(next[id].sleep==='dim')next[id].sleep='black';next[id].lower=false;}
      });return next;});
      setDialog(null);notify(t(`已应用到 ${ids.length} 块屏幕的待保存设置，请保存后生效。`,`Applied to ${ids.length} display draft(s). Save to activate.`));
    }
    function closeCurtain(){
      const id=dialog.queue?.[0];
      if(id)setRuntime(prev=>{const next={...prev};delete next[id];return next;});
      if(dialog.queue?.length>1){const queue=dialog.queue.slice(1);setDialog({...dialog,queue,config:clone(saved[queue[0]])});}
      else setDialog(null);
    }
    function modal(){if(!dialog)return null;
      if(dialog.kind==='protect')return h(CurtainPreview,{key:dialog.queue?.[0]||'preview',config:dialog.config,t,onClose:closeCurtain});
      if(dialog.kind==='desktopTest')return h(DesktopTestV3,{t,onClose:()=>setDialog(null),onCurtain:()=>setDialog({kind:'protect',config:dialog.config,queue:[],ids:[]})});
      if(dialog.kind==='backgrounds')return h(ModalShell,{label:t('管理隐私幕图片','Manage curtain images'),onClose:()=>setDialog(null),className:'backgrounds-dialog'},
        h(AppearancePanel,{config,patch,t,busy:readingImages,onFiles:readImages,onPreview:()=>setDialog({kind:'protect',config:clone(config),ids:[],queue:[]})}),
        h('div',{className:'dialog-actions'},h(Button,{kind:'primary',onClick:()=>setDialog(null)},t('完成','Done'))));
      if(dialog.kind==='appPicker')return h(AppPicker,{value:config.targetApp,t,onClose:()=>setDialog(null),onSelect:targetApp=>{patch({targetApp});setDialog(null);}});
      if(dialog.kind==='appTest')return h(AppTestDialog,{config:dialog.config,t,onClose:()=>setDialog(null),onCurtain:()=>setDialog({kind:'protect',config:dialog.config,queue:[],ids:[]})});
      if(dialog.kind==='copy')return h(CopyDialog,{source:config,sourceName:monitorName(selected),targets:screenIds.filter(id=>id!==selected).map(id=>({id,name:monitorName(id),managed:draft[id].managed,unsupported:id===2&&scenario==='unsupported'})),dirtyIds,onClose:()=>setDialog(null),onApply:applyToScreens,t,unitName,privacyName,sleepName});
      if(dialog.kind==='pause')return h(PauseV3,{config:old,t,onClose:()=>setDialog(null),onStart:(time,unit)=>{setPauses(prev=>({...prev,[selected]:{time,unit,until:Date.now()+Number(time)*factors[unit]*1000}}));setRuntime(prev=>({...prev,[selected]:'paused'}));setDialog(null);}});
      const closing=dialog.kind==='close';
      return h('div',{className:'overlay',onClick:e=>{if(e.target===e.currentTarget)setDialog(null);}},h('div',{className:'dialog',role:'dialog','aria-modal':true,'aria-label':closing?t('尚未保存的更改','Unsaved changes'):t('隐私保护规则','Privacy rules')},
        h('div',{className:'dialog-icon'},h(Icon,{name:closing?'info':'help',size:22})),
        h('h3',null,closing?t('还有尚未保存的更改','You have unsaved changes'):t('保护内容，也保留你的工作','Hide your content. Keep your work.')),
        h('p',null,closing?t('隐藏窗口前，是否保存本次修改？','Save your changes before hiding the window?'):t('隐私计时只跟随本屏实际操作。普通输入不会解除隐私幕；回到桌面不会自动还原窗口。跨屏或无法最小化时，会保留隐私幕。','Only activity on this screen resets privacy timing. Ordinary input never dismisses the curtain. Show desktop never restores previous windows automatically; failures keep the curtain.')),
        !closing&&h('p',{style:{marginTop:12}},t('如果屏幕休息时间先到，也会先保护内容。隐私保护不替代 Windows 锁屏。','If sleep starts earlier, privacy runs first. Privacy protection does not replace locking Windows.')),
        h('div',{className:'dialog-actions'},closing?h(React.Fragment,null,
          h(Button,{onClick:()=>setDialog(null)},t('取消','Cancel')),
          h(Button,{onClick:()=>{imageReadVersion.current++;setDraft(clone(saved));setStartup(savedStartup);setDialog(null);setHidden(true);}},t('不保存','Discard')),
          h(Button,{kind:'primary',disabled:allErrors.length>0||readingImages,onClick:()=>{if(save()){setDialog(null);setHidden(true);}}},t('保存','Save'))
        ):h(Button,{kind:'primary',onClick:()=>setDialog(null)},t('明白了','Got it')))));
    }
    return h('div',{className:demo?'demo-open':''},
      h('header',{className:'review-bar'},h('div',{className:'review-title'},h('span',{className:'review-mark'},'DESIGN / 03'),h('div',null,h('h1',null,'ScreenKeeper · 隐私设置精简稿'),h('p',null,'启用 → 等待 → 保护方式 · 只展开当前需要的设置'))),h('div',{className:'review-actions'},h('div',{className:'size-group'},[['normal','普通窗口'],['maximized','最大化'],['compact','700 × 500']].map(([value,label])=>h('button',{key:value,className:size===value?'active':'',onClick:()=>changeSize(value),'aria-pressed':size===value},label))),h('button',{className:'demo-toggle',onClick:()=>setDemo(!demo),'aria-expanded':demo},demo?'收起演示面板':'演示面板'))),
      demo&&h('div',{className:'demo-panel'},h('label',null,'页面状态',h('select',{value:scenario,'aria-label':'演示场景',onChange:e=>{clearTimeout(refreshTimer.current);setScenario(e.target.value);}},h('option',{value:'normal'},'正常'),h('option',{value:'unsupported'},'不支持调暗'),h('option',{value:'empty'},'无显示器'),h('option',{value:'loading'},'检测中'),h('option',{value:'error'},'检测失败'))),h('label',null,h('input',{type:'checkbox',checked:failSave,onChange:e=>setFailSave(e.target.checked)}),'模拟保存失败'),h('button',{onClick:reset},'重置演示'),h('a',{href:'settings-v2.html',style:{color:'inherit',fontSize:11}},'对照已确认的 v2')),
      h('main',{className:'stage',ref:stageRef},hidden?h('div',{className:'restore-area'},h(Icon,{name:'moon',size:30}),h('p',null,t('窗口已隐藏，屏幕规则继续运行','Window hidden. Saved rules keep running.')),h('button',{className:'restore-preview',onClick:()=>setHidden(false)},h('img',{src:'settings-v2-icon.svg',width:18,height:18,alt:''}),t('从托盘打开设置（演示）','Open settings from tray (preview)'))):h('section',{className:`window ${size}`,lang:lang==='zh'?'zh-CN':'en',style:{'--dx':`${offset.x}px`,'--dy':`${offset.y}px`},'aria-label':'ScreenKeeper settings prototype','data-screen-label':'隐私设置 v3'},
        h('header',{className:'titlebar',onPointerDown:pointerDown,onPointerMove:pointerMove,onPointerUp:()=>{dragRef.current=null;},onPointerCancel:()=>{dragRef.current=null;},onDoubleClick:e=>{if(!e.target.closest('button,select,input'))changeSize(size==='maximized'?'normal':'maximized');}},h('div',{className:'brand'},h('img',{src:'settings-v2-icon.svg',alt:''}),h('span',null,t('多屏管家','ScreenKeeper')),h('span',null,t('设置','Settings'))),h('div',{className:'window-tools'},h('label',{className:'language'},'Language',h('select',{value:lang,'aria-label':'语言 / Language',onChange:e=>setLang(e.target.value)},h('option',{value:'zh'},'简体中文'),h('option',{value:'en'},'English'))),h('button',{className:'chrome',title:t('最小化','Minimize'),'aria-label':t('最小化','Minimize'),onClick:()=>setHidden(true)},h(Icon,{name:'minimize',size:13})),h('button',{className:'chrome',title:size==='maximized'?t('还原','Restore'):t('最大化','Maximize'),'aria-label':size==='maximized'?t('还原','Restore'):t('最大化','Maximize'),onClick:()=>changeSize(size==='maximized'?'normal':'maximized')},h(Icon,{name:size==='maximized'?'restore':'maximize',size:12})),h('button',{className:'chrome',title:t('隐藏到托盘','Hide to tray'),'aria-label':t('隐藏到托盘','Hide to tray'),onClick:closeWindow},h(Icon,{name:'close',size:14})))),
        h('div',{className:'workspace'},h('aside',{className:'sidebar'},h('div',{className:'nav-label'},t('我的显示器','MY DISPLAYS'),h('span',null,scenario==='empty'?'0':'03')),h('nav',{className:'monitor-list','aria-label':t('选择显示器','Choose a display')},!alternate&&screenIds.map(id=>h('button',{key:id,className:`monitor-item ${!general&&selected===id?'selected':''}`,'aria-pressed':!general&&selected===id,onClick:()=>{setSelected(id);setGeneral(false);}},h('span',{className:'monitor-icon'},h(Icon,{name:'screen'}),h('span',null,id)),h('span',{className:'monitor-copy'},h('strong',null,monitorName(id)),h('small',null,id===1?t('主屏 · ','Primary · ')+(saved[1].managed?t('已启用','Managed'):t('未启用','Not managed')):saved[id].managed?privacyName(saved[id].privacy):t('未启用管理','Not managed'))),dirtyIds.includes(id)&&h('span',{className:'dirty-dot',title:t('有未保存的修改','Unsaved changes')})))),h('div',{className:'nav-bottom'},h('button',{className:'nav-button',disabled:dirty||scenario==='loading',title:dirty?t('请先保存或放弃更改','Save or discard changes first'):t('刷新显示器','Refresh displays'),onClick:refresh},h(Icon,{name:'refresh',size:15}),t('刷新显示器','Refresh displays')),h('button',{className:`nav-button ${general?'selected':''}`,onClick:()=>setGeneral(true)},h(Icon,{name:'gear',size:15}),t('通用设置','General settings'))),h('div',{className:'nav-footnote'},h('span',{className:'tiny-dot'}),t('在后台，安静守护','Quietly running in the tray'))),
        h('div',{className:'main'},general?h(React.Fragment,null,h('div',{className:'screen-header'},h('div',null,h('div',{className:'eyebrow'},'PREFERENCES'),h('h2',null,t('通用设置','General settings')),h('p',null,t('应用偏好，对所有屏幕生效。','App preferences for all your displays.')))),h('div',{className:'editor'},generalPanel())):alternate?specialState():h(React.Fragment,null,
          h('div',{className:'screen-header'},h('div',null,h('div',{className:'eyebrow'},`DISPLAY / 0${selected}`),h('h2',null,monitorName(selected),h('em',null,selected===1?t('主显示器','Primary'):t('扩展显示器','Extended'))),h('p',null,selected===1?'2560 × 1440 · 100%':'1920 × 1080 · 100%',`  ·  ${unsupported?t('仅支持遮黑','Blackout only'):'DDC/CI'}`)),h('div',{className:'screen-actions'},h(Button,{className:'copy-settings',icon:'copy',onClick:()=>setDialog({kind:'copy'}),disabled:errors({...config,managed:true},selected).length>0||readingImages},t('应用到其他屏幕','Apply to displays')),h('span',null,config.managed?t('管理此屏幕','Manage display'):t('未启用管理','Not managed')),h(Toggle,{checked:config.managed,onChange:value=>patch({managed:value}),label:t('管理此屏幕','Manage display')}))),
          h('div',{className:'tabs',role:'tablist'},[['sleep','moon',t('屏幕休息','Screen sleep')],['privacy','shield',t('隐私保护','Privacy protection')]].map(([value,icon,label])=>h('button',{key:value,role:'tab','aria-selected':tab===value,className:`tab ${tab===value?'active':''}`,onClick:()=>setTab(value)},h(Icon,{name:icon,size:16}),label))),
          h('div',{className:'editor',role:'tabpanel','aria-label':tab==='privacy'?t('隐私保护设置','Privacy settings'):t('屏幕休息设置','Sleep settings')},!config.managed?h('div',{className:'no-manage'},h(Icon,{name:'screen',size:30}),h('div',null,h('h3',null,t('这块屏幕，由你掌控','Keep this screen in your hands')),h('p',null,t('启用右上角的管理开关，即可设置独立的隐私保护与休息规则。','Enable the switch above to set independent privacy and sleep rules.')))):tab==='privacy'?privacyPanel():sleepPanel())
        ))),
        h('footer',{className:'footer'},h('div',{className:`footer-status ${dirty?'dirty':''}`,role:'status'},h(Icon,{name:dirty?'info':'check',size:13}),allErrors.length?t(`${allErrors.length} 项设置需要调整`,`${allErrors.length} setting(s) need attention`):dirty?t('有未保存的更改','Unsaved changes'):t('所有设置已保存','All settings saved')),h('div',{className:'footer-right'},h(Button,{icon:'shield',className:'quick-protect',disabled:!anyProtected||alternate,onClick:protect,title:t('只执行已保存并启用隐私的屏幕规则','Uses saved privacy settings only')},t('立即保护全部','Protect all now')),h('span',{className:'footer-separator'}),h(Button,{kind:'quiet',disabled:!dirty,onClick:()=>{imageReadVersion.current++;setDraft(clone(saved));setStartup(savedStartup);notify(t('已放弃本次修改','Changes discarded'));}},t('放弃更改','Discard')),h(Button,{kind:'primary',className:'save',disabled:!dirty||allErrors.length>0||readingImages,onClick:save},t('保存全部设置','Save all settings')))),
        toast&&h('div',{className:`status-toast ${toast.error?'error':''}`,role:'status'},toast.text),modal()
      )),
      h('footer',{className:'review-foot'},h('span',null,'交互原型 · 不影响真实显示器或配置'),h('span',null,'本稿默认展示隐私页 · 屏幕休息仍为首个页签'))
    );
  }
  ReactDOM.createRoot(document.getElementById('root')).render(h(App));
})();
