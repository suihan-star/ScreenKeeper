const options=[
{id:'stackmoon',letter:'A+',name:'叠屏静夜',tag:'已确认实施',text:'保留叠屏轮廓，前屏加入一弯月牙。多屏与休息，合成一个标识。'},
{id:'stack',letter:'A',name:'叠屏',tag:'原版对照',text:'前后两块屏幕，共用一个安静的轮廓。缩小后仍能读出“多屏”。'},
{id:'moon',letter:'B',name:'静夜',tag:'强调休息',text:'屏幕中留一弯月牙，表达让屏幕休息。更柔和，但容易联想到夜间模式。'}];
function Icon({id,theme='dark',size=16}){return <img className="icon" src={'tray-icons/'+id+'-'+theme+'.svg'} width={size} height={size} alt={options.find(o=>o.id===id).name+'图标'} draggable="false"/>;}
function App(){
const [selected,setSelected]=React.useState('stackmoon');
const option=options.find(o=>o.id===selected);
return <><header><div className="brand"><Icon id="stackmoon" size={24}/><span>ScreenKeeper <span className="brand-detail">/ 多屏管家</span></span><span className="draft">图标提案 · 02</span></div>
<div className="intro"><div><p className="eyebrow">一个安静的常驻标识</p><h1>小到 16 像素，<br/>也能一眼认出。</h1></div><p className="description">沿用静绿的克制与温和。<br/>用清晰的屏幕轮廓，留在任务栏一角。<br/><span>无文字 · 无光晕 · 无动画</span></p></div></header>
<main><section className="actual"><div className="section-title"><div><h2>先看实际大小</h2><p>重点看 16 px 月牙的辨识度；尺寸单位为 CSS 像素。</p></div><div className="switch" role="group" aria-label="切换图标方案">{options.map(o=><button key={o.id} aria-pressed={selected===o.id} onClick={()=>setSelected(o.id)}>{o.letter} · {o.name}</button>)}</div></div>
<div className="tray-grid">{['dark','light'].map(theme=><div className={'tray '+theme} key={theme}><div className="tray-label">{theme==='dark'?'深色任务栏':'浅色任务栏'}<span>静态情境预览</span></div><div className="sizes">{[16,20,24,32].map(size=><div className="size" key={size}><div className="icon-cell"><Icon id={selected} theme={theme} size={size}/></div><span>{size} px</span></div>)}</div><div className="taskbar"><span className="chevron">⌃</span><Icon id={selected} theme={theme}/><svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.4" aria-hidden="true"><path d="m2 6 3 0 4-3v10l-4-3H2Z M11 5q4 3 0 6"/></svg><span className="clock">14:28<small>2026/09/16</small></span></div></div>)}</div>
<div className="selection"><div className="selected-large"><Icon id={selected} theme="light" size={64}/></div><p><strong>{option.letter} · {option.name}</strong><span>{option.tag} / {option.text}</span></p><div className="downloads"><a href={'tray-icons/'+selected+'-dark.svg'} download>深色栏 SVG ↗</a><a href={'tray-icons/'+selected+'-light.svg'} download>浅色栏 SVG ↗</a></div></div></section>
<section className="exploration"><div className="canvas-note">方向对照 <span>下方为放大轮廓，可缩放画布或双击标题聚焦。</span></div><DesignCanvas style={{height:490,width:'100%'}}><DCSection id="tray-moon-revision" title="叠屏里，留一弯月亮" subtitle="A+ 合并效果 · A 原始轮廓 · B 月牙来源" gap={28}>{options.map(o=><DCArtboard key={o.id} id={o.id} label={o.letter+' · '+o.name} width={260} height={300}><article className="concept"><div className="concept-top"><span>{o.letter} / {o.name}</span><span>{o.tag}</span></div><div className="hero-icon"><Icon id={o.id} size={96}/></div><div className="concept-bottom"><p>{o.text}</p><div className="mini-palette"><i/><i/><span>16 × 16 基础网格</span></div></div></article></DCArtboard>)}</DCSection></DesignCanvas></section>
<footer><span>本轮 A+ · 叠屏静夜：A 的双屏轮廓，加上 B 的月牙。</span><small>A+ 已确认 · 已生成多尺寸 ICO 并接入客户端。</small><a href="index.html">返回客户端设计 ↗</a></footer></main></>;
}
ReactDOM.createRoot(document.getElementById('root')).render(<App/>);
