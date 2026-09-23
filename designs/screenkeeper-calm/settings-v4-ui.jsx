'use strict';
(() => {
  const {useEffect, useRef} = React;
  const Icon = window.SK.Icon;
  function Button({children,icon,kind='',small=false,...props}) {
    return <button type="button" className={`btn ${kind} ${small?'small':''}`} {...props}>{icon&&<Icon name={icon} size={15}/>} {children}</button>;
  }
  function Toggle({checked,onChange,label,disabled=false}) {
    return <button type="button" className="switch" role="switch" aria-checked={checked} aria-label={label} disabled={disabled} onClick={()=>onChange(!checked)}><span></span></button>;
  }
  function Tabs({items,value,onChange,label}) {
    return <div className="tabs" role="tablist" aria-label={label}>{items.map(([id,name,icon])=><button key={id} type="button" role="tab" aria-selected={value===id} className={value===id?'active':''} onClick={()=>onChange(id)}>{icon&&<Icon name={icon} size={15}/>} {name}</button>)}</div>;
  }
  function Choices({items,value,onChange,label}) {
    return <div className="choices" role="radiogroup" aria-label={label}>{items.map(([id,name,note,icon])=><button type="button" role="radio" aria-checked={value===id} key={id} onClick={()=>onChange(id)} className={`choice ${value===id?'active':''}`}><Icon name={icon} size={20}/><span><strong>{name}</strong>{note&&<small>{note}</small>}</span><span className="choice-check"><Icon name="check" size={13}/></span></button>)}</div>;
  }
  function Unit({value,unit,onValue,onUnit,units,t,label,min=1,max=1440}) {
    return <div className="unit"><input className="input" type="number" min={min} max={max} value={value} aria-label={label} onChange={e=>onValue(e.target.value===''?'':Number(e.target.value))}/>{units?<select aria-label={t('时间单位','Time unit')} value={unit} onChange={e=>onUnit(e.target.value)}>{units.map(x=><option key={x} value={x}>{x==='s'?t('秒','sec'):x==='m'?t('分钟','min'):t('小时','hr')}</option>)}</select>:<span>{unit}</span>}</div>;
  }
  function Setting({label,sub,children}) {
    return <div className="setting-row"><div><div className="label">{label}</div>{sub&&<div className="sub">{sub}</div>}</div><div>{children}</div></div>;
  }
  function ThemePicker({theme,onChange,t}) {
    return <section className="theme-section"><h3 id="theme-heading">{t('外观主题','Appearance theme')}</h3><div className="theme-options" role="radiogroup" aria-labelledby="theme-heading">{['forest','fog'].map(id=><button type="button" role="radio" aria-checked={theme===id} aria-label={id==='forest'?t('深绿','Forest green'):t('雾白绿','Mist green')} key={id} className="theme-option" onClick={()=>onChange(id)} onKeyDown={e=>{if(['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)){e.preventDefault();onChange(id==='forest'?'fog':'forest');const next=e.currentTarget.parentElement.querySelector(`[aria-label="${id==='forest'?t('雾白绿','Mist green'):t('深绿','Forest green')}"]`);next?.focus();}}}>
      <span className={`theme-sample ${id}`} aria-hidden="true"><span className="sample-title"><i></i><i></i><i></i></span><span className="sample-nav"><i></i><i></i><i></i></span><span className="sample-body"><i></i><i></i><b></b></span></span>
      <span className="theme-option-name"><span>{id==='forest'?t('深绿','Forest green'):t('雾白绿','Mist green')}{id==='forest'&&<span className="default-label">{t('默认','Default')}</span>}</span><span className="theme-check"><Icon name="check" size={14}/></span></span><small>{id==='forest'?t('深绿底色，浅绿文字','Deep green with soft green text'):t('浅绿灰底色，深绿文字','Pale green-grey with dark green text')}</small>
    </button>)}</div><p className="hint">{t('立即切换，记住你的选择。','Applies immediately. Your choice is remembered.')}</p></section>;
  }
  function Notice({title,children,action,info=false}) {
    return <div className={`notice ${info?'info':''}`} role="status"><Icon name="info" size={17}/><div className="notice-copy"><strong>{title}</strong>{children&&<p>{children}</p>}{action&&<div style={{marginTop:8}}>{action}</div>}</div></div>;
  }
  function Scene({style='fog',moving=true,rest=false,coverage=65,fade=8,intensity=1,animate=false,time='05:00',image,t}) {
    const mode=style==='scenic'?'picture':style;
    return <div className={`scene ${mode} ${moving?'moving':''} ${animate?'fade-scene':''}`} style={{'--coverage':rest?1:coverage/100,'--fade':`${fade}s`,'--motion-time':`${17/Number(intensity)}s`}} aria-label={t('效果示意','Effect illustration')}>
      {!rest&&style!=='compact'&&<div className="sample-work"><span></span><span></span><span></span></div>}
      <div className="atmosphere"><div className="scene-bg" style={{backgroundImage:mode==='picture'?`linear-gradient(#0c201a38,#0c201a38),url("${image||'forest-mist.png'}")`:undefined}}></div>
      {(style==='fog')&&<><div className="fog-layer"></div><div className="fog-layer second"></div></>}
      {(mode==='night')&&<div className="moon"></div>}</div>
      <div className="scene-card"><strong>{rest?t('给自己一点留白','A little room to breathe'):t('让视线，稍作停留','Let your eyes wander')}</strong>{rest?<div className="scene-time">{time}</div>:null}<p>{rest?t('离开屏幕，慢慢放松','Step away. Take your time.'):t('工作可以稍后，休息就在此刻。','Your work can wait a moment.')}</p></div>
    </div>;
  }
  function Dialog({title,subtitle,children,actions,onClose,wide=false}) {
    const ref=useRef(null), closeRef=useRef(onClose);
    closeRef.current=onClose;
    useEffect(()=>{
      const before=document.activeElement;
      const dialog=ref.current;
      const focusables=()=>Array.from(dialog.querySelectorAll('button:not(:disabled),input:not(:disabled),select:not(:disabled),summary,[tabindex="0"]')).filter(el=>el.getClientRects().length);
      (focusables()[0]||dialog).focus();
      const handle=e=>{if(e.key==='Escape'){e.preventDefault();closeRef.current();} if(e.key==='Tab'){const items=focusables();if(!items.length){e.preventDefault();return;} const first=items[0],last=items[items.length-1];if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}};
      dialog.addEventListener('keydown',handle);
      return ()=>{dialog.removeEventListener('keydown',handle);if(before&&before.isConnected)before.focus();};
    },[]);
    return <div className="modal-cover"><section ref={ref} className={`dialog ${wide?'wide':''}`} role="dialog" aria-modal="true" aria-labelledby="dialog-title" tabIndex={-1}><header className="dialog-head"><div><h2 id="dialog-title">{title}</h2>{subtitle&&<p>{subtitle}</p>}</div><button type="button" className="close-btn" aria-label="关闭 / Close" onClick={onClose}><Icon name="close" size={19}/></button></header><div className="dialog-body">{children}</div><footer className="dialog-footer">{actions}</footer></section></div>;
  }
  window.SK4UI={Icon,Button,Toggle,Tabs,Choices,Unit,Setting,ThemePicker,Notice,Scene,Dialog};
})();
