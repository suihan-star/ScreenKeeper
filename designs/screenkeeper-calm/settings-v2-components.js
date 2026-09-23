'use strict';
window.SK = (() => {
  const h = React.createElement;
  const paths = {
    app: 'M3 3h18v18H3z M3 8h18 M7 5.5h.01 M10 5.5h.01 M7 12h10 M7 16h6',
    copy: 'M8 8h13v13H8z M16 8V3H3v13h5',
    image: 'M3 3h18v18H3z M3 17l6-6 4 4 3-3 5 5 M8 7h.01',
    screen: 'M3 4h18v12H3z M8 21h8 M12 16v5',
    shield: 'M12 3 4 6v6c0 5 8 9 8 9s8-4 8-9V6z M8.5 12l2.5 2.5 4.5-5',
    moon: 'M20.5 13A8.5 8.5 0 0 1 11 3.5 8.5 8.5 0 1 0 20.5 13Z',
    desktop: 'M3 4h18v13H3z M3 13h18 M8 21h8 M12 17v4',
    sun: 'M12 2v2 M12 20v2 M2 12h2 M20 12h2 M5 5l1.5 1.5 M17.5 17.5 19 19 M5 19l1.5-1.5 M17.5 6.5 19 5 M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0',
    off: 'M12 2v9 M6 5a9 9 0 1 0 12 0',
    refresh: 'M20 8a8 8 0 0 0-14-2L3 9 M3 3v6h6 M4 16a8 8 0 0 0 14 2l3-3 M21 21v-6h-6',
    gear: 'm9 3 1-1h4l1 3 3 1 3 1v4l-2 2v3l-3 2-1 3h-4l-2-2-3-1-3-2v-4l2-2V7l3-2z M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0',
    check: 'm5 12 4 4L19 6',
    chevron: 'm9 5 7 7-7 7',
    help: 'M9 8a3 3 0 1 1 4 3v3 M12 18h.01 M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0',
    pause: 'M8 5v14 M16 5v14',
    play: 'm8 4 12 8-12 8z',
    maximize: 'M4 4h16v16H4z',
    restore: 'M8 8V3h13v13h-5 M3 8h13v13H3z',
    minimize: 'M4 17h16',
    close: 'm5 5 14 14 M19 5 5 19',
    arrow: 'M4 12h16 m-6-6 6 6-6 6',
    clock: 'M12 6v6l4 2 M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0',
    info: 'M12 11v6 M12 7h.01 M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0',
    mouse: 'M5 9a7 7 0 0 1 14 0v6a7 7 0 0 1-14 0z M12 3v6',
    key: 'M2 5h20v14H2z M6 9h.01 M10 9h.01 M14 9h.01 M18 9h.01 M7 15h10',
    eye: 'M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12 M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0'
  };
  function Icon({name, size=18, ...props}) { return h('svg', {width:size,height:size,viewBox:'0 0 24 24',fill:'none',stroke:'currentColor',strokeWidth:1.5,strokeLinecap:'round',strokeLinejoin:'round','aria-hidden':true,...props}, h('path',{d:paths[name]||paths.screen})); }
  function Button({children, icon, kind='', className='', ...props}) { return h('button',{type:'button',className:`btn ${kind} ${className}`,...props},icon&&h(Icon,{name:icon,size:16}),children); }
  function Toggle({checked,onChange,label,disabled=false}) { return h('button',{type:'button',role:'switch','aria-checked':checked,'aria-label':label,title:label,className:'switch',onClick:()=>onChange(!checked),disabled},h('span')); }
  function Choices({value,onChange,options,label,disabled=false}) { return h('div',{className:'choices',role:'radiogroup','aria-label':label},options.map(o=>h('button',{key:o.value,type:'button',role:'radio','aria-checked':value===o.value,className:`choice ${value===o.value?'selected':''}`,disabled:disabled||o.disabled,onClick:()=>onChange(o.value),title:o.hint||o.title},h('span',{className:'choice-top'},h(Icon,{name:o.icon,size:21}),h('span',{className:'radio-mark'},value===o.value&&h(Icon,{name:'check',size:10}))),h('strong',null,o.title),h('small',null,o.note)))); }
  return {h,Icon,Button,Toggle,Choices};
})();
