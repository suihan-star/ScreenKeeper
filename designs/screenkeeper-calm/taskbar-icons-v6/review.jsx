const candidates = [
  {id:'outline',letter:'A',name:'雾绿轮廓',lead:'已确认 · 两处统一',body:'保持原来的叠屏与月牙，深色任务栏用浅绿，浅色任务栏用深绿。',tradeoff:'轻巧熟悉，图形的色块面积仍然较小。'},
  {id:'tile',letter:'B',name:'雾绿底牌',lead:'历史对照',body:'柔和的浅绿底牌托住深绿叠屏，在一排应用里更容易找到。',tradeoff:'任务栏更醒目；16 px 托盘建议保留 A 的透明轮廓。'},
  {id:'filled',letter:'C',name:'双层实色',lead:'更清晰的月牙',body:'填实前方屏幕、保留后屏轮廓，用反色月牙增加小尺寸辨识度。',tradeoff:'没有外部底牌，但比原标识更厚实。'}
];
function iconPath(id,theme='dark') { return 'taskbar-icons-v6/'+(id==='original'?'original.svg':id==='tile'?'tile.svg':id+'-'+theme+'.svg'); }
function Mark({id,theme='dark',size=24}) { return <img className="mark" src={iconPath(id,theme)} width={size} height={size} alt={id==='original'?'现有深绿图标':candidates.find(c=>c.id===id).name} draggable="false"/>; }
function MiniTray({id,theme}) { return <div className={'mini-tray '+theme}><span aria-hidden="true">⌃</span><Mark id={id} theme={theme} size={16}/><svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.4" aria-hidden="true"><path d="M2 6h3l4-3v10l-4-3H2ZM11 5q4 3 0 6"/></svg><span className="time">14:28</span></div>; }
function ScreenshotRow({id,label}) { return <div className="screenshot-line"><div className="shot-label">{label}</div><div className="shot-scroll"><div className="shot"><img src="taskbar-icons-v6/taskbar-reference.png" width="984" height="52" alt="用户提供的原始任务栏截图"/>{id!=='original'&&<div className="replacement" aria-label="图标位置模拟替换"><Mark id={id}/></div>}</div></div></div>; }
function App(){
  const [selected,setSelected]=React.useState('outline');
  const [original,setOriginal]=React.useState(false);
  const [tweaks,setTweaks]=React.useState(false);
  const [theme,setTheme]=React.useState('dark');
  const choice=candidates.find(c=>c.id===selected);
  return <div className="review-page">
    <header className="page-header"><a className="brand" href="index.html"><Mark id="outline" theme="light" size={27}/><strong>屏幕良伴</strong><span>图标可见度 · 06</span></a><span className="draft">已确认 A · 任务栏与托盘</span></header>
    <main>
      <section className="intro"><div><p className="eyebrow">A+ 叠屏静夜 / 可见度优化</p><h1>更容易找到，<br/>仍然安静舒服。</h1></div><p>叠屏与月牙保留。<br/>这次重点看：放回你的任务栏，<br/>能不能一眼认出来。</p></section>
      <section className="comparison" aria-labelledby="real-title"><div className="section-heading"><div><h2 id="real-title">先放回你的任务栏</h2><p>原截图与 24 px 图标模拟，点击下方切换。</p></div><button className="text-button" aria-pressed={original} onClick={()=>setOriginal(!original)}>{original?'返回所选方案':'对照原图'}</button></div>
        <div className="choice-tabs" role="group" aria-label="选择图标方案">{candidates.map(c=><button key={c.id} className="choice-tab" aria-pressed={selected===c.id} onClick={()=>{setSelected(c.id);setOriginal(false);}}><Mark id={c.id} size={24}/><span><strong>{c.letter} · {c.name}</strong><small>{c.lead}</small></span></button>)}</div>
        <div className="screenshot-pair"><ScreenshotRow id="original" label="现在"/><ScreenshotRow id={original?'original':selected} label={original?'原图':choice.letter+' 方案'}/></div>
        <div className="selection-caption" aria-live="polite"><strong>{original?'现有深绿色图标':choice.letter+' · '+choice.name}</strong><span>{original?'在深色任务栏上，轮廓容易融入背景。':choice.body}</span></div>
        <p className="fineprint">截图原样保留；下排仅在图标位置叠加模拟图层。浏览器效果需在实施后用 Windows 实机复核。</p>
      </section>
      <section className="size-section" aria-labelledby="size-title"><div className="section-heading"><div><h2 id="size-title">大小不变，辨识度不同</h2><p>实际 CSS 像素大小，不随下方画布缩放。</p></div><button className="text-button" aria-expanded={tweaks} onClick={()=>setTweaks(!tweaks)}>背景切换 · Tweaks {tweaks?'−':'＋'}</button></div>
        {tweaks&&<div className="tweaks" role="group" aria-label="任务栏背景"><span>对比背景</span><button className="chip" aria-pressed={theme==='dark'} onClick={()=>setTheme('dark')}>深色任务栏</button><button className="chip" aria-pressed={theme==='light'} onClick={()=>setTheme('light')}>浅色任务栏</button></div>}
        <div className={'size-table '+theme} role="table" aria-label="图标尺寸对照"><div className="size-row heading" role="row"><span role="columnheader">{theme==='dark'?'深色任务栏':'浅色任务栏'}</span>{[16,20,24,32].map(s=><span role="columnheader" key={s}>{s} px</span>)}</div>{[{id:'original',letter:'原',name:'当前图标'},...candidates].map(c=><div key={c.id} className={'size-row '+(c.id===selected?'selected':'')} role="row"><span role="rowheader">{c.letter} · {c.name}{c.id===selected&&<small>当前预览</small>}</span>{[16,20,24,32].map(s=><div role="cell" key={s}><Mark id={c.id} theme={theme} size={s}/></div>)}</div>)}</div>
        <div className="usage"><div><p className="eyebrow">最终选择</p><h3>任务栏与托盘，统一用 A。</h3><p>深色任务栏用浅绿，浅色任务栏用深绿。<br/>两处都保留透明背景、原来的叠屏与月牙。</p></div><div className="usage-previews"><div><span>任务栏 / 24 px</span><div className={'mini-taskbar '+theme}><Mark id="outline" theme={theme} size={24}/><i/></div></div><div><span>右下角托盘 / 16 px</span><MiniTray id="outline" theme={theme}/></div></div></div>
      </section>
      <section className="canvas-section"><div className="section-heading"><div><h2>放大看三种处理</h2><p>可平移、缩放画布；上方实际尺寸更适合判断日常观感。</p></div></div><DesignCanvas style={{height:540,width:'100%',overflow:'clip'}}><DCSection id="taskbar-visibility-v6" title="同一个 A+，三种清晰度处理" subtitle="延续静绿，不加入高亮光晕" gap={24}>{candidates.map(c=><DCArtboard key={c.id} id={'visibility-'+c.id} label={c.letter+' · '+c.name} width={292} height={300}><article className="concept"><div className="concept-id"><span>{c.letter} / {c.name}</span><small>{c.lead}</small></div><div className="concept-mark"><Mark id={c.id} size={90}/><div className="light-sample"><Mark id={c.id} theme="light" size={48}/></div></div><p>{c.tradeoff}</p><button className="concept-select" onClick={()=>{setSelected(c.id);setOriginal(false);window.scrollTo({top:0,behavior:'smooth'});}}>预览 {c.letter} 方案 ↗</button></article></DCArtboard>)}</DCSection></DesignCanvas></section>
      <footer><p>已确认 A：任务栏与托盘统一跟随 Windows 系统主题。</p><a href={iconPath(selected,theme)} download>下载当前 SVG ↗</a><a href="tray-icon.html">查看上一轮设计 ↗</a></footer>
    </main>
  </div>;
}
ReactDOM.createRoot(document.getElementById('root')).render(<App/>);
