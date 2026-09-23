'use strict';
(() => {
  const {h,Icon,Button}=SK;
  const {useState,useEffect,useRef}=React;
  const factors={seconds:1,minutes:60,hours:3600};
  function TimeInput({value,unit,onValue,onUnit,label,t,units=['seconds','minutes','hours']}) {
    return h(React.Fragment,null,
      h('input',{type:'number',min:1,step:1,value,'aria-label':label,onChange:e=>onValue(e.target.value)}),
      h('select',{value:unit,'aria-label':label+t('单位',' unit'),onChange:e=>onUnit(e.target.value)},units.map(u=>h('option',{key:u,value:u},{seconds:t('秒','Seconds'),minutes:t('分钟','Minutes'),hours:t('小时','Hours')}[u]))));
  }
  function ModalShell({label,children,onClose,className=''}) {
    const ref=useRef(null);
    useEffect(()=>{
      const previous=document.activeElement;
      const first=ref.current?.querySelector('button,input,select');first?.focus();
      const trap=e=>{
        if(e.key!=='Tab')return;
        const items=[...ref.current.querySelectorAll('button:not(:disabled),input:not(:disabled),select:not(:disabled),[tabindex="0"]')].filter(el=>el.getClientRects().length);
        const index=items.indexOf(document.activeElement);
        if(!items.length)return;
        if(e.shiftKey&&(index===0||index===-1)){e.preventDefault();items.at(-1).focus();}
        else if(!e.shiftKey&&(index===items.length-1||index===-1)){e.preventDefault();items[0].focus();}
      };
      const node=ref.current;node.addEventListener('keydown',trap);
      return()=>{node.removeEventListener('keydown',trap);previous?.isConnected&&previous.focus();};
    },[]);
    return h('div',{className:`overlay ${className}`,onClick:e=>{if(e.target===e.currentTarget)onClose?.();}},h('section',{ref,role:'dialog','aria-modal':true,'aria-label':label,className:'dialog'},children));
  }
  function CurtainPreview({config,onClose,t}) {
    const [index,setIndex]=useState(0),[failed,setFailed]=useState(false);
    const pictures=config.curtainStyle==='images'?config.images:[];
    const interval=Number(config.slideTime)*factors[config.slideUnit];
    useEffect(()=>{
      if(pictures.length<2||!Number.isFinite(interval)||interval<1)return;
      const timer=setInterval(()=>setIndex(i=>(i+1)%pictures.length),interval*1000);
      return()=>clearInterval(timer);
    },[pictures.length,interval]);
    useEffect(()=>setFailed(false),[index]);
    return h(ModalShell,{className:'curtain-overlay',label:t('隐私幕预览','Privacy curtain preview')},
      pictures[index]&&!failed&&h('img',{className:`curtain-picture ${config.imageFit}`,src:pictures[index].src,alt:'',onError:()=>setFailed(true)}),
      h(Button,{kind:'primary',className:'curtain-continue',onClick:onClose},t('继续使用此屏幕','Continue using this screen')));
  }
  function AppearancePanel({config,patch,t,onFiles,onPreview,busy}) {
    return h('section',{className:'appearance'},
      h('div',{className:'section-heading'},h('div',null,h('h3',null,t('隐私幕外观','Curtain appearance')),h('p',null,t('安静的背景，只留下继续使用按钮。','A quiet background, with just one button to return.'))),h(Button,{icon:'eye',onClick:onPreview},t('预览幕布','Preview curtain'))),
      h('div',{className:'appearance-modes'},[['solid',t('默认纯色','Default solid')],['images',t('自定义图片','Custom images')]].map(([value,label])=>h('button',{key:value,className:config.curtainStyle===value?'selected':'','aria-pressed':config.curtainStyle===value,onClick:()=>patch({curtainStyle:value})},h(Icon,{name:value==='solid'?'moon':'image',size:17}),label))),
      config.curtainStyle==='images'&&h(React.Fragment,null,
        h('div',{className:'image-toolbar'},h('label',{className:`btn file-pick ${busy?'busy':''}`},h(Icon,{name:'image',size:16}),busy?t('正在读取…','Reading…'):t('选择图片','Choose images'),h('input',{type:'file',accept:'image/png,image/jpeg,image/webp',multiple:true,disabled:busy,'aria-label':t('选择隐私幕图片','Choose curtain images'),onChange:e=>{const files=Array.from(e.target.files);e.target.value='';onFiles(files);}})),h(Button,{kind:'quiet',disabled:busy,onClick:()=>patch({images:[{name:t('静山','Quiet hills'),src:'curtain-hills.svg'},{name:t('雾林','Misty grove'),src:'curtain-grove.svg'}]})},t('使用示例图片','Use sample images')),h('span',{className:'image-count'},t(`${config.images.length} 张`,`${config.images.length} image(s)`))),
        config.images.length?h('div',{className:'image-grid'},config.images.map((img,i)=>h('div',{key:img.src+i,className:'image-tile'},h('img',{src:img.src,alt:img.name}),h('span',{title:img.name},`${i+1}. ${img.name}`),h('button',{className:'remove-image','aria-label':t(`移除图片 ${i+1}`,`Remove image ${i+1}`),onClick:()=>patch({images:config.images.filter((_,n)=>n!==i)})},h(Icon,{name:'close',size:12}))))):h('div',{className:'image-empty'},h(Icon,{name:'image',size:23}),h('p',null,t('选择一张作为背景，或多张自动轮播。','Choose one background or several for a slideshow.'))),
        h('p',{className:'image-hint'},t('PNG / JPG / WebP · 最多 12 张，每张不超过 10 MB · 仅在本地读取','PNG / JPG / WebP · Up to 12 images, 10 MB each · Read locally only')),
        h('div',{className:'field-row'},h('h3',null,t('图片显示方式','Image layout')),h('div',{className:'field-control'},h('select',{value:config.imageFit,'aria-label':t('图片显示方式','Image layout'),onChange:e=>patch({imageFit:e.target.value})},h('option',{value:'cover'},t('铺满屏幕（裁剪）','Fill screen (crop)')),h('option',{value:'contain'},t('完整显示（留边）','Fit image (borders)'))))),
        config.images.length>1&&h('div',{className:'field-row'},h('div',null,h('h3',null,t('每张图片停留','Change image every')),h('p',null,t('按上方顺序循环播放','Loop in the order shown above'))),h('div',{className:'field-control'},h(TimeInput,{value:config.slideTime,unit:config.slideUnit,onValue:value=>patch({slideTime:value}),onUnit:unit=>patch({slideUnit:unit}),label:t('图片轮播间隔','Slideshow interval'),t,units:['seconds','minutes']})))));
  }
  function CopyDialog({source,sourceName,targets,dirtyIds,onClose,onApply,t,unitName,privacyName,sleepName}) {
    const [mode,setMode]=useState('all'),[checked,setChecked]=useState([]);
    const ids=mode==='all'?targets.map(x=>x.id):checked;
    const incompatible=targets.filter(x=>ids.includes(x.id)&&x.unsupported&&(source.sleep==='dim'||source.lower));
    return h(ModalShell,{label:t('应用到其他屏幕','Apply to other displays'),onClose,className:'copy-overlay'},
      h('h3',null,t('把这套设置用到其他屏幕','Reuse these display settings')),
      h('p',null,t(`来源：${sourceName} 的当前设置（含未保存修改）`,`Source: current settings for ${sourceName}, including unsaved changes`)),
      h('div',{className:'copy-summary'},h('span',null,`${sleepName(source.sleep)} · ${source.sleepTime} ${unitName(source.unit)}`),h('span',null,`${privacyName(source.privacy)} · ${source.privacyTime} ${unitName(source.privacyUnit)}`),h('small',null,t('包含等待、唤醒条件、暂缓、幕布图片及指定应用','Includes timing, wake conditions, pause, curtain images and target app'))),
      h('div',{className:'copy-scope',role:'radiogroup','aria-label':t('应用范围','Target scope')},[['all',t('全部其他屏幕','All other displays')],['selected',t('指定屏幕','Selected displays')]].map(([v,label])=>h('label',{key:v},h('input',{type:'radio',name:'copy-scope',checked:mode===v,onChange:()=>setMode(v)}),label))),
      h('div',{className:'copy-targets'},targets.map(target=>h('label',{key:target.id,className:'check-row'},h('input',{type:'checkbox',checked:ids.includes(target.id),disabled:mode==='all',onChange:e=>setChecked(list=>e.target.checked?[...list,target.id]:list.filter(id=>id!==target.id))}),h('span',null,target.name,h('small',null,dirtyIds.includes(target.id)?t('将覆盖这块屏幕的未保存修改','Will replace unsaved changes on this display'):target.managed?t('已启用管理','Management enabled'):t('保持未启用管理','Management stays off')))))),
      source.privacy==='app'&&h('p',{className:'copy-warning'},t('同一个窗口不能同时显示在多屏；复制后请为各屏选择独立窗口。','One window cannot appear on several displays at once. Select a separate window for each display after copying.')),
      incompatible.length>0&&h('div',{className:'copy-warning',role:'status'},t(`${incompatible.map(x=>x.name).join('、')} 不支持亮度控制：将使用遮黑，关闭降亮度选项。`,`${incompatible.map(x=>x.name).join(', ')} cannot adjust brightness: use blackout and disable brightness reduction.`)),
      h('p',{className:'copy-note'},t('保持各屏幕的管理开关不变。应用只更新待保存设置，保存全部设置后生效。','Keep each display’s management switch unchanged. Apply updates drafts; save all settings to activate.')),
      h('div',{className:'dialog-actions'},h(Button,{onClick:onClose},t('取消','Cancel')),h(Button,{kind:'primary',disabled:!ids.length,onClick:()=>onApply(ids)},t(`应用到 ${ids.length} 块屏幕`,`Apply to ${ids.length} display(s)`))));
  }
  function PauseDialog({config,onClose,onStart,t}) {
    const [time,setTime]=useState(config.pauseTime),[unit,setUnit]=useState(config.pauseUnit);
    const valid=/^\d+$/.test(time)&&Number(time)>0&&Number(time)*factors[unit]<=86400;
    return h(ModalShell,{label:t('暂缓隐私保护','Pause privacy protection'),onClose},h('h3',null,t('暂缓多久？','How long would you like to pause?')),h('p',null,t('只暂缓当前屏幕的自动隐私保护，屏幕休息规则继续运行。','Pause automatic privacy protection on this display. Screen sleep rules keep running.')),
      h('div',{className:'field-row'},h('h3',null,t('暂缓时长','Pause duration')),h('div',{className:'field-control'},h(TimeInput,{value:time,unit,onValue:setTime,onUnit:setUnit,label:t('本次暂缓时长','Pause for'),t,units:['minutes','hours']}))),
      !valid&&h('p',{className:'invalid',role:'alert'},t('请输入正整数，总时长不超过 24 小时。','Enter a positive whole number, up to 24 hours.')),
      h('p',{className:'copy-note'},t('到时自动恢复，也可提前恢复。本次时长会填入设置草稿，保存后作为默认值。','Resumes automatically; you can resume earlier. Save the updated draft to keep this duration as your default.')),
      h('div',{className:'dialog-actions'},h(Button,{onClick:onClose},t('取消','Cancel')),h(Button,{kind:'primary',disabled:!valid,onClick:()=>onStart(time,unit)},t('开始暂缓','Start pause'))));
  }
  Object.assign(SK,{TimeInput,CurtainPreview,AppearancePanel,CopyDialog,PauseDialog,ModalShell,factors});
})();
