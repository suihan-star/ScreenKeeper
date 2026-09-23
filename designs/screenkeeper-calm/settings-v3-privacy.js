'use strict';
(() => {
  const {h, Icon, Button, Toggle, TimeInput, ModalShell, appName, factors}=SK;
  const {useState}=React;
  const row=(label,control,note)=>h('div',{className:'field-row'},h('div',null,h('h3',null,label),note&&h('p',null,note)),h('div',{className:'field-control'},control));
  function PrivacyPanelV3({config,old,patch,t,runtimeText,paused,onPause,onPreview,onImages,onChoose,onTest,onDesktopTest,onHelp,issues,busy}) {
    const enabled=config.privacy!=='off';
    const mode=config.privacy;
    const picture=config.curtainStyle==='images'&&config.images[0];
    const timingIssues=issues.filter(x=>/等待|delay/.test(x));
    const appIssues=issues.filter(x=>/应用|app window|target app/.test(x));
    const imageIssues=issues.filter(x=>/图片|轮播|image|slideshow|background/.test(x));
    const pauseIssues=issues.filter(x=>/暂缓|pause duration/.test(x));
    const otherIssues=issues.filter(x=>![...timingIssues,...appIssues,...imageIssues,...pauseIssues].includes(x));
    const error=list=>list.length?h('div',{className:'invalid',role:'alert'},list.map((x,i)=>h('p',{key:i},x))):null;
    const options=[['curtain','shield',t('隐私幕','Curtain')],['desktop','desktop',t('回到桌面','Desktop')],['app','app',t('指定应用','Target app')]];
    return h('div',{className:'editor-inner privacy-v3'},
      h('div',{className:'privacy-heading'},
        h('div',null,h('h3',null,t('启用本屏隐私保护','Enable privacy on this display')),h('p',null,t('离开这块屏幕后，自动隐藏私人内容。','Automatically hide private content when you step away.'))),
        h(Toggle,{checked:enabled,label:t('启用本屏隐私保护','Enable privacy on this display'),onChange:value=>patch(value?{privacy:config.rememberedPrivacy||'curtain'}:{privacy:'off',rememberedPrivacy:mode})})),
      old.managed&&old.privacy!=='off'&&h('div',{className:'privacy-runtime'},
        h('span',null,h('span',{className:`runtime-dot ${paused?'paused':''}`}),runtimeText),
        h('button',{className:'text-action',onClick:onPause},h(Icon,{name:paused?'play':'pause',size:12}),paused?t('恢复保护','Resume'):t('暂缓…','Pause…'))),
      enabled!== (old.privacy!=='off')&&h('p',{className:'draft-note'},h(Icon,{name:'info',size:12}),enabled?t('保存后启用本屏隐私保护。','Save to enable privacy on this display.'):t('保存后停用；当前已保存的保护规则仍在运行。','Save to turn privacy off. Saved protection is still active.')),
      enabled?h(React.Fragment,null,
        h('div',{className:'privacy-delay'},h('span',null,t('本屏空闲','After')),h('div',{className:'field-control'},h(TimeInput,{value:config.privacyTime,unit:config.privacyUnit,onValue:value=>patch({privacyTime:value}),onUnit:unit=>patch({privacyUnit:unit}),label:t('隐私等待时间','Privacy delay'),t})),h('span',null,t('后保护','of inactivity on this display'))),
        error(timingIssues),
        h('div',{className:'mode-heading'},h('h3',null,t('保护方式','Protection mode')),h('button',{className:'help-button',onClick:onHelp,'aria-label':t('了解隐私保护规则','About privacy rules')},h(Icon,{name:'help',size:14}))),
        h('div',{className:'mode-segments',role:'radiogroup','aria-label':t('隐私保护方式','Privacy mode'),onKeyDown:e=>{
          if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;
          e.preventDefault();const i=options.findIndex(x=>x[0]===mode);const next=e.key==='Home'?0:e.key==='End'?2:(i+(e.key==='ArrowRight'?1:2))%3;
          patch({privacy:options[next][0]});e.currentTarget.querySelectorAll('button')[next].focus();
        }},options.map(([id,icon,label])=>h('button',{key:id,role:'radio','aria-checked':mode===id,tabIndex:mode===id?0:-1,className:mode===id?'selected':'',onClick:()=>patch({privacy:id})},h(Icon,{name:icon,size:16}),label))),
        h('section',{className:`mode-detail mode-${mode}`,'aria-label':t('保护方式设置','Mode settings')},
          mode==='curtain'&&h(React.Fragment,null,
            h('div',{className:'curtain-layout'},
              h('button',{className:'curtain-mini','aria-label':t('预览隐私幕','Preview curtain'),onClick:onPreview},picture&&h('img',{src:picture.src,alt:'',style:{objectFit:config.imageFit}}),h('span',null,t('继续使用此屏幕','Continue using this screen'))),
              h('div',{className:'curtain-options'},h('h4',null,t('隐私幕外观','Curtain appearance')),h('div',{className:'surface-segments',role:'group','aria-label':t('隐私幕外观','Curtain appearance')},[['solid',t('纯色','Solid')],['images',t('图片','Images')]].map(([id,label])=>h('button',{key:id,'aria-pressed':config.curtainStyle===id,className:config.curtainStyle===id?'selected':'',onClick:()=>patch({curtainStyle:id})},label))),
                config.curtainStyle==='images'&&h('div',{className:'gallery-entry'},h('span',null,config.images.length?t(`已选 ${config.images.length} 张`,`${config.images.length} selected`):t('尚未选择图片','No images yet')),h('button',{className:'text-action',onClick:onImages,disabled:busy},t('管理图片','Manage images'))),
                h('button',{className:'text-action preview-action',onClick:onPreview},h(Icon,{name:'eye',size:14}),t('预览效果','Preview')))),
            error(imageIssues),h('p',{className:'mode-description'},t('唤亮后仍显示隐私幕，点击按钮才显示原内容。','The curtain stays after waking. Use the button to reveal your content.'))),
          mode==='desktop'&&h(React.Fragment,null,
            h('div',{className:'desktop-layout'},h('div',{className:'desktop-mini','aria-hidden':true},h('div',{className:'desktop-wallpaper'}),h('div',{className:'desktop-dock'},[0,1,2,3].map(i=>h('i',{key:i})))),h('div',null,h('h4',null,t('回到干净的桌面','Return to a clear desktop')),h('p',null,t('仅最小化本屏窗口。唤亮后保持桌面，不自动还原。','Minimize windows on this display. Waking keeps the desktop visible.')),h(Button,{icon:'play',onClick:onDesktopTest},t('测试回到桌面','Test desktop'))))),
          mode==='app'&&h(React.Fragment,null,
            h('div',{className:'app-selection'},h('span',{className:'app-glyph'},h(Icon,{name:'app',size:24})),h('div',{className:'target-copy'},h('strong',null,appName(config.targetApp,t)),h('small',null,config.targetApp?t(config.targetApp.window,config.targetApp.windowEn||config.targetApp.window):t('选择一个适合公开展示的窗口','Choose a window that is safe to show'))),h(Button,{onClick:onChoose},config.targetApp?t('更换','Change'):t('选择应用','Choose app'))),
            error(appIssues),h('div',{className:'app-detail-foot'},h('p',null,t('唤亮后显示指定应用，原窗口不自动还原。','Wake to the chosen app. Previous windows stay hidden.')),h('button',{className:'text-action',onClick:onTest,disabled:!config.targetApp},h(Icon,{name:'play',size:13}),t('测试切换','Test switch'))))),
        mode!=='curtain'&&h('p',{className:'fallback-note'},h(Icon,{name:'shield',size:13}),t('操作未完成时，保留隐私幕。','If the action cannot complete, the privacy curtain stays.')),
        h('details',{className:'privacy-more',key:`more-${mode}`},h('summary',null,h(Icon,{name:'chevron',size:14}),t('更多设置','More settings'),h('span',null,mode==='app'?t('暂缓 · 应用行为','Pause · App behavior'):t('暂缓时长','Pause duration'))),
          row(t('默认暂缓时长','Default pause duration'),h(TimeInput,{value:config.pauseTime,unit:config.pauseUnit,onValue:value=>patch({pauseTime:value}),onUnit:unit=>patch({pauseUnit:unit}),label:t('默认暂缓时长','Default pause duration'),t,units:['minutes','hours']}),t('暂缓时可单独调整，到时自动恢复。','Adjust each pause separately; resumes automatically.')),
          error(pauseIssues),
          mode==='app'&&h(React.Fragment,null,
            row(t('应用未运行时','If the app is not running'),h('select',{value:config.appNotRunning,'aria-label':t('应用未运行时','If the app is not running'),onChange:e=>patch({appNotRunning:e.target.value})},h('option',{value:'curtain'},t('保留隐私幕','Keep curtain')),h('option',{value:'launch'},t('启动此应用','Launch app')))),
            row(t('窗口在其他屏幕时','If the window is elsewhere'),h('select',{value:config.appElsewhere,'aria-label':t('窗口在其他屏幕时','If the window is elsewhere'),onChange:e=>patch({appElsewhere:e.target.value})},h('option',{value:'curtain'},t('保留隐私幕','Keep curtain')),h('option',{value:'move'},t('移到此屏幕','Move here'))),t('允许移动会改变另一块屏幕的布局。','Moving changes the other display’s layout.')),
            config.targetApp&&h('p',{className:'path-detail'},h('span',null,t('程序路径','Application path')),h('code',null,config.targetApp.path)))),
        pauseIssues.length>0&&h('p',{className:'invalid'},t('请展开“更多设置”调整暂缓时长。','Open More settings to fix the pause duration.'))
      ):h('div',{className:'privacy-disabled'},h('div',{className:'disabled-symbol'},h(Icon,{name:'shield',size:24})),h('h4',null,t('需要时，再开启','Ready when you need it')),h('p',null,t('已保留上次的保护方式与设置。','Your previous mode and settings are kept.')),h('span',null,t('屏幕休息仍按自己的规则运行。','Screen sleep follows its own rules.'))),
      error(otherIssues));
  }
  function PauseV3({config,t,onClose,onStart}) {
    const [time,setTime]=useState(config.pauseTime),[unit,setUnit]=useState(config.pauseUnit);
    const valid=/^\d+$/.test(time)&&Number(time)>0&&Number(time)*factors[unit]<=86400;
    return h(ModalShell,{label:t('暂缓隐私保护','Pause privacy protection'),onClose},h('h3',null,t('给这块屏幕一点时间','Give this display a little time')),h('p',null,t('暂缓自动隐私保护，到时自动恢复。','Pause automatic privacy protection. It resumes when time is up.')),
      row(t('本次暂缓','Pause for'),h(TimeInput,{value:time,unit,onValue:setTime,onUnit:setUnit,label:t('本次暂缓时长','Pause duration'),t,units:['minutes','hours']})),
      !valid&&h('p',{className:'invalid',role:'alert'},t('请输入正整数，总时长不超过 24 小时。','Enter a positive whole number, up to 24 hours.')),
      h('p',{className:'copy-note'},t('仅用于这一次，不修改默认时长。屏幕休息继续运行，已开启的隐私幕不会解除。','This pause does not change the default. Sleep rules continue, and any active curtain stays.')),
      h('div',{className:'dialog-actions'},h(Button,{onClick:onClose},t('取消','Cancel')),h(Button,{kind:'primary',disabled:!valid,onClick:()=>onStart(time,unit)},t('开始暂缓','Start pause'))));
  }
  function DesktopTestV3({t,onClose,onCurtain}) {
    const [scenario,setScenario]=useState('ready'),[result,setResult]=useState(false);
    return h(ModalShell,{label:t('测试回到桌面','Test desktop'),onClose},h('h3',null,t('测试回到桌面','Test desktop')),h('p',null,t('仅演示效果，不操作真实窗口。','Preview only. No real windows are changed.')),
      h('label',{className:'test-scenario'},t('演示场景','Demo scenario'),h('select',{'aria-label':t('桌面测试场景','Desktop test scenario'),value:scenario,onChange:e=>{setScenario(e.target.value);setResult(false);}},h('option',{value:'ready'},t('本屏窗口可最小化','Windows can be minimized')),h('option',{value:'failed'},t('有窗口无法最小化','A window cannot be minimized')))),
      result&&h('div',{className:`switch-result ${scenario==='ready'?'success':'failure'}`,role:'status'},h('strong',null,scenario==='ready'?t('已显示桌面（演示）','Desktop shown (preview)'):t('已保留隐私幕','Privacy curtain kept')),h('p',null,scenario==='ready'?t('原窗口保持最小化，其他屏幕不受影响。','Previous windows stay minimized. Other displays are unchanged.'):t('部分窗口未能隐藏，继续遮挡本屏内容。','Some windows could not be hidden. The display remains covered.')),scenario==='failed'&&h(Button,{onClick:onCurtain},t('预览保留的隐私幕','Preview retained curtain'))),
      h('div',{className:'dialog-actions'},h(Button,{onClick:onClose},t('完成','Done')),h(Button,{kind:'primary',onClick:()=>setResult(true)},result?t('重新演示','Run again'):t('开始演示','Run demo'))));
  }
  Object.assign(SK,{PrivacyPanelV3,PauseV3,DesktopTestV3});
})();
