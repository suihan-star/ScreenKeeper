'use strict';
(() => {
  const {h,Icon,Button,ModalShell}=SK;
  const {useState}=React;
  const samples=[
    {id:'calendar-main',name:'日历',en:'Calendar',window:'月视图',windowEn:'Month view',path:'C:\\DemoApps\\Calendar.exe',location:2},
    {id:'notes-blank',name:'记事本',en:'Notepad',window:'空白文档',windowEn:'Blank document',path:'C:\\Windows\\System32\\notepad.exe',location:2},
    {id:'weather-main',name:'天气',en:'Weather',window:'天气概览',windowEn:'Weather overview',path:'C:\\DemoApps\\Weather.exe',location:1}
  ];
  const appName=(app,t)=>app?t(app.name,app.en||app.name):t('尚未选择应用','No app selected');
  function AppPicker({value,onClose,onSelect,t}) {
    const [mode,setMode]=useState('running'),[query,setQuery]=useState(''),[picked,setPicked]=useState(value?.id||''),[path,setPath]=useState('');
    const filtered=samples.filter(app=>`${app.name} ${app.en} ${app.window} ${app.windowEn}`.toLowerCase().includes(query.toLowerCase()));
    const valid=/^[a-z]:\\[^<>:"|?*\r\n]+\.exe$/i.test(path.trim());
    const selection=mode==='running'?samples.find(x=>x.id===picked):valid?{id:path.trim().toLowerCase(),name:path.trim().split('\\').pop().replace(/\.exe$/i,''),path:path.trim(),manual:true,window:'启动后的主窗口',windowEn:'Main window after launch'}:null;
    return h(ModalShell,{label:t('选择指定应用','Choose target app'),onClose,className:'app-picker'},
      h('h3',null,t('选择一个可公开展示的窗口','Choose a window that is safe to show')),
      h('p',null,t('以下是演示列表，不读取你电脑上的应用。','This is a sample list, not the apps running on your computer.')),
      h('div',{className:'appearance-modes'},[['running',t('运行中的窗口','Running windows')],['path',t('填写程序路径','Enter app path')]].map(([id,label])=>h('button',{key:id,'aria-pressed':mode===id,className:mode===id?'selected':'',onClick:()=>setMode(id)},label))),
      mode==='running'?h(React.Fragment,null,
        h('input',{className:'app-search',placeholder:t('搜索应用或窗口','Search apps or windows'),'aria-label':t('搜索应用或窗口','Search apps or windows'),value:query,onChange:e=>setQuery(e.target.value)}),
        h('div',{className:'app-list',role:'radiogroup','aria-label':t('示例窗口','Sample windows')},filtered.length?filtered.map(app=>h('button',{key:app.id,role:'radio','aria-checked':picked===app.id,className:`app-option ${picked===app.id?'selected':''}`,onClick:()=>setPicked(app.id)},h('span',{className:'app-glyph'},h(Icon,{name:'app',size:21})),h('span',null,h('strong',null,appName(app,t)),h('small',null,t(app.window,app.windowEn)+t(` · 显示器 ${app.location}`,` · Display ${app.location}`))),h('span',{className:'radio-mark'},picked===app.id&&h(Icon,{name:'check',size:10})))):h('p',{className:'app-empty'},t('没有匹配的示例窗口，可填写程序路径。','No sample windows match. You can enter an app path.')))
      ):h('div',{className:'app-path-form'},h('label',null,t('程序完整路径','Full application path'),h('input',{className:'app-search',value:path,placeholder:'C:\\Apps\\Calendar.exe','aria-label':t('程序完整路径','Full application path'),onChange:e=>setPath(e.target.value)})),h('p',null,t('填写本地 .exe 路径；原型不会检查文件或启动程序。','Enter a local .exe path. The prototype does not inspect files or launch apps.')),path&&!valid&&h('p',{className:'invalid',role:'alert'},t('请输入盘符开头、以 .exe 结尾的完整路径。','Enter an absolute drive path ending in .exe.'))),
      h('p',{className:'copy-note'},t('应用现有内容不会被清空；建议选择日历、空白文档等适合公开显示的内容。','Existing app content will not be cleared. Choose content you are comfortable showing.')),
      h('div',{className:'dialog-actions'},h(Button,{onClick:onClose},t('取消','Cancel')),h(Button,{kind:'primary',disabled:!selection,onClick:()=>onSelect(selection)},t('使用此应用','Use this app'))));
  }
  function AppTargetPanel({config,patch,t,onChoose,onTest}) {
    const target=config.targetApp;
    return h('section',{className:'app-target'},
      h('div',{className:'section-heading'},h('div',null,h('h3',null,t('切换到哪个应用','Which app should appear?')),h('p',null,t('在本屏准备好目标窗口后，才撤下隐私幕。','Keep the curtain until the target window is ready here.')))),
      h('div',{className:'target-card'},h('span',{className:'app-glyph'},h(Icon,{name:'app',size:24})),h('div',{className:'target-copy'},h('strong',null,appName(target,t)),h('small',null,target?t(target.window,target.windowEn||target.window):t('选择运行窗口，或填写程序路径','Choose a running window or enter an app path')),target&&h('span',{title:target.path},target.path)),h(Button,{onClick:onChoose},target?t('更换','Change'):t('选择应用','Choose app'))),
      h('div',{className:'field-row'},h('div',null,h('h3',null,t('应用未运行时','When the app is not running')),h('p',null,t('运行中优先复用，不重复打开','Reuse the running app instead of launching duplicates'))),h('div',{className:'field-control'},h('select',{value:config.appNotRunning,'aria-label':t('应用未运行时','When the app is not running'),onChange:e=>patch({appNotRunning:e.target.value})},h('option',{value:'curtain'},t('保留隐私幕','Keep privacy curtain')),h('option',{value:'launch'},t('启动此应用','Launch this app'))))),
      h('div',{className:'field-row'},h('div',null,h('h3',null,t('窗口在其他屏幕时','When the window is elsewhere')),h('p',null,t('移动会改变另一块屏幕的布局','Moving it changes the other display’s layout'))),h('div',{className:'field-control'},h('select',{value:config.appElsewhere,'aria-label':t('窗口在其他屏幕时','When the window is elsewhere'),onChange:e=>patch({appElsewhere:e.target.value})},h('option',{value:'curtain'},t('保留隐私幕','Keep privacy curtain')),h('option',{value:'move'},t('移到此屏幕','Move to this display'))))),
      h('div',{className:'app-test-entry'},h('p',null,t('无法确定窗口、无法启动或切换失败时，始终保留隐私幕。','If the window is ambiguous or cannot be opened or switched, keep the curtain.')),h(Button,{icon:'play',disabled:!target,onClick:onTest},t('测试切换','Test switch'))));
  }
  function AppTestDialog({config,t,onClose,onCurtain}) {
    const [scenario,setScenario]=useState('ready'),[result,setResult]=useState(null);
    const failReason=scenario==='closed'&&config.appNotRunning==='curtain'?t('应用未运行，按设置保留隐私幕。','The app is not running. Your rule keeps the curtain.'):scenario==='elsewhere'&&config.appElsewhere==='curtain'?t('窗口位于其他屏幕，未授权移动，保留隐私幕。','The window is on another display. Moving is disabled, so the curtain stays.'):scenario==='ambiguous'?t('无法唯一确定目标窗口，保留隐私幕；请重新选择窗口。','The target window is ambiguous. Keep the curtain and select a window again.'):scenario==='failed'?t('应用未能启动或窗口切换失败，保留隐私幕。','The app could not launch or switch. The curtain stays.'):null;
    return h(ModalShell,{label:t('测试指定应用','Test target app'),onClose,className:'app-test'},
      h('h3',null,t('测试切换','Test switch')+' · '+appName(config.targetApp,t)),
      h('p',null,t('仅演示流程，不启动应用，也不移动真实窗口。','Flow preview only. No apps are launched and no real windows are moved.')),
      h('label',{className:'test-scenario'},t('演示场景','Demo scenario'),h('select',{'aria-label':t('应用测试场景','App test scenario'),value:scenario,onChange:e=>{setScenario(e.target.value);setResult(null);}},[['ready',t('目标窗口已在本屏','Window on this display')],['closed',t('应用未运行','App not running')],['elsewhere',t('窗口在其他屏幕','Window on another display')],['ambiguous',t('有多个窗口，无法确定','Ambiguous windows')],['failed',t('启动或切换失败','Launch or switch failed')]].map(([id,label])=>h('option',{key:id,value:id},label)))),
      h('ol',{className:'switch-steps'},[t('先用隐私幕遮住本屏','Cover this display first'),t('隐藏原窗口，准备指定应用','Hide previous windows and prepare the app'),t('确认覆盖到位，再显示应用','Reveal only after the app covers the display')].map((label,i)=>h('li',{key:i},h('span',null,i+1),label))),
      result&&h('div',{className:`switch-result ${result.ok?'success':'failure'}`,role:'status'},h('strong',null,result.ok?t('演示成功：唤醒后显示指定应用','Demo succeeded: wake to the chosen app'):t('已保留隐私幕','Privacy curtain kept')),h('p',null,result.message),result.ok?h('div',{className:'safe-app-mock'},h('div',null,h(Icon,{name:'app',size:15}),appName(config.targetApp,t),h('small',null,t('示意画面','Sample view'))),h('section',null,[0,1,2].map(i=>h('span',{key:i})))):h(Button,{onClick:onCurtain},t('查看隐私幕效果','Preview retained curtain'))),
      h('div',{className:'dialog-actions'},h(Button,{onClick:onClose},t('关闭','Close')),h(Button,{kind:'primary',onClick:()=>setResult({ok:!failReason,message:failReason||t('原窗口保持隐藏，所选应用留在此屏幕；不会自动还原原窗口。','Previous windows stay hidden; the chosen app remains here. Previous windows are not restored automatically.')})},result?t('重新演示','Run again'):t('开始演示','Run demo'))));
  }
  Object.assign(SK,{AppPicker,AppTargetPanel,AppTestDialog,appName});
})();
