const options = [{
  id: 'stackmoon',
  letter: 'A+',
  name: '叠屏静夜',
  tag: '已确认实施',
  text: '保留叠屏轮廓，前屏加入一弯月牙。多屏与休息，合成一个标识。'
}, {
  id: 'stack',
  letter: 'A',
  name: '叠屏',
  tag: '原版对照',
  text: '前后两块屏幕，共用一个安静的轮廓。缩小后仍能读出“多屏”。'
}, {
  id: 'moon',
  letter: 'B',
  name: '静夜',
  tag: '强调休息',
  text: '屏幕中留一弯月牙，表达让屏幕休息。更柔和，但容易联想到夜间模式。'
}];
function Icon({
  id,
  theme = 'dark',
  size = 16
}) {
  return /*#__PURE__*/React.createElement("img", {
    className: "icon",
    src: 'tray-icons/' + id + '-' + theme + '.svg',
    width: size,
    height: size,
    alt: options.find(o => o.id === id).name + '图标',
    draggable: "false"
  });
}
function App() {
  const [selected, setSelected] = React.useState('stackmoon');
  const option = options.find(o => o.id === selected);
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("header", null, /*#__PURE__*/React.createElement("div", {
    className: "brand"
  }, /*#__PURE__*/React.createElement(Icon, {
    id: "stackmoon",
    size: 24
  }), /*#__PURE__*/React.createElement("span", null, "ScreenKeeper ", /*#__PURE__*/React.createElement("span", {
    className: "brand-detail"
  }, "/ \u591A\u5C4F\u7BA1\u5BB6")), /*#__PURE__*/React.createElement("span", {
    className: "draft"
  }, "\u56FE\u6807\u63D0\u6848 \xB7 02")), /*#__PURE__*/React.createElement("div", {
    className: "intro"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("p", {
    className: "eyebrow"
  }, "\u4E00\u4E2A\u5B89\u9759\u7684\u5E38\u9A7B\u6807\u8BC6"), /*#__PURE__*/React.createElement("h1", null, "\u5C0F\u5230 16 \u50CF\u7D20\uFF0C", /*#__PURE__*/React.createElement("br", null), "\u4E5F\u80FD\u4E00\u773C\u8BA4\u51FA\u3002")), /*#__PURE__*/React.createElement("p", {
    className: "description"
  }, "\u6CBF\u7528\u9759\u7EFF\u7684\u514B\u5236\u4E0E\u6E29\u548C\u3002", /*#__PURE__*/React.createElement("br", null), "\u7528\u6E05\u6670\u7684\u5C4F\u5E55\u8F6E\u5ED3\uFF0C\u7559\u5728\u4EFB\u52A1\u680F\u4E00\u89D2\u3002", /*#__PURE__*/React.createElement("br", null), /*#__PURE__*/React.createElement("span", null, "\u65E0\u6587\u5B57 \xB7 \u65E0\u5149\u6655 \xB7 \u65E0\u52A8\u753B")))), /*#__PURE__*/React.createElement("main", null, /*#__PURE__*/React.createElement("section", {
    className: "actual"
  }, /*#__PURE__*/React.createElement("div", {
    className: "section-title"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", null, "\u5148\u770B\u5B9E\u9645\u5927\u5C0F"), /*#__PURE__*/React.createElement("p", null, "\u91CD\u70B9\u770B 16 px \u6708\u7259\u7684\u8FA8\u8BC6\u5EA6\uFF1B\u5C3A\u5BF8\u5355\u4F4D\u4E3A CSS \u50CF\u7D20\u3002")), /*#__PURE__*/React.createElement("div", {
    className: "switch",
    role: "group",
    "aria-label": "\u5207\u6362\u56FE\u6807\u65B9\u6848"
  }, options.map(o => /*#__PURE__*/React.createElement("button", {
    key: o.id,
    "aria-pressed": selected === o.id,
    onClick: () => setSelected(o.id)
  }, o.letter, " \xB7 ", o.name)))), /*#__PURE__*/React.createElement("div", {
    className: "tray-grid"
  }, ['dark', 'light'].map(theme => /*#__PURE__*/React.createElement("div", {
    className: 'tray ' + theme,
    key: theme
  }, /*#__PURE__*/React.createElement("div", {
    className: "tray-label"
  }, theme === 'dark' ? '深色任务栏' : '浅色任务栏', /*#__PURE__*/React.createElement("span", null, "\u9759\u6001\u60C5\u5883\u9884\u89C8")), /*#__PURE__*/React.createElement("div", {
    className: "sizes"
  }, [16, 20, 24, 32].map(size => /*#__PURE__*/React.createElement("div", {
    className: "size",
    key: size
  }, /*#__PURE__*/React.createElement("div", {
    className: "icon-cell"
  }, /*#__PURE__*/React.createElement(Icon, {
    id: selected,
    theme: theme,
    size: size
  })), /*#__PURE__*/React.createElement("span", null, size, " px")))), /*#__PURE__*/React.createElement("div", {
    className: "taskbar"
  }, /*#__PURE__*/React.createElement("span", {
    className: "chevron"
  }, "\u2303"), /*#__PURE__*/React.createElement(Icon, {
    id: selected,
    theme: theme
  }), /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "16",
    viewBox: "0 0 16 16",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.4",
    "aria-hidden": "true"
  }, /*#__PURE__*/React.createElement("path", {
    d: "m2 6 3 0 4-3v10l-4-3H2Z M11 5q4 3 0 6"
  })), /*#__PURE__*/React.createElement("span", {
    className: "clock"
  }, "14:28", /*#__PURE__*/React.createElement("small", null, "2026/09/16")))))), /*#__PURE__*/React.createElement("div", {
    className: "selection"
  }, /*#__PURE__*/React.createElement("div", {
    className: "selected-large"
  }, /*#__PURE__*/React.createElement(Icon, {
    id: selected,
    theme: "light",
    size: 64
  })), /*#__PURE__*/React.createElement("p", null, /*#__PURE__*/React.createElement("strong", null, option.letter, " \xB7 ", option.name), /*#__PURE__*/React.createElement("span", null, option.tag, " / ", option.text)), /*#__PURE__*/React.createElement("div", {
    className: "downloads"
  }, /*#__PURE__*/React.createElement("a", {
    href: 'tray-icons/' + selected + '-dark.svg',
    download: true
  }, "\u6DF1\u8272\u680F SVG \u2197"), /*#__PURE__*/React.createElement("a", {
    href: 'tray-icons/' + selected + '-light.svg',
    download: true
  }, "\u6D45\u8272\u680F SVG \u2197")))), /*#__PURE__*/React.createElement("section", {
    className: "exploration"
  }, /*#__PURE__*/React.createElement("div", {
    className: "canvas-note"
  }, "\u65B9\u5411\u5BF9\u7167 ", /*#__PURE__*/React.createElement("span", null, "\u4E0B\u65B9\u4E3A\u653E\u5927\u8F6E\u5ED3\uFF0C\u53EF\u7F29\u653E\u753B\u5E03\u6216\u53CC\u51FB\u6807\u9898\u805A\u7126\u3002")), /*#__PURE__*/React.createElement(DesignCanvas, {
    style: {
      height: 490,
      width: '100%'
    }
  }, /*#__PURE__*/React.createElement(DCSection, {
    id: "tray-moon-revision",
    title: "\u53E0\u5C4F\u91CC\uFF0C\u7559\u4E00\u5F2F\u6708\u4EAE",
    subtitle: "A+ \u5408\u5E76\u6548\u679C \xB7 A \u539F\u59CB\u8F6E\u5ED3 \xB7 B \u6708\u7259\u6765\u6E90",
    gap: 28
  }, options.map(o => /*#__PURE__*/React.createElement(DCArtboard, {
    key: o.id,
    id: o.id,
    label: o.letter + ' · ' + o.name,
    width: 260,
    height: 300
  }, /*#__PURE__*/React.createElement("article", {
    className: "concept"
  }, /*#__PURE__*/React.createElement("div", {
    className: "concept-top"
  }, /*#__PURE__*/React.createElement("span", null, o.letter, " / ", o.name), /*#__PURE__*/React.createElement("span", null, o.tag)), /*#__PURE__*/React.createElement("div", {
    className: "hero-icon"
  }, /*#__PURE__*/React.createElement(Icon, {
    id: o.id,
    size: 96
  })), /*#__PURE__*/React.createElement("div", {
    className: "concept-bottom"
  }, /*#__PURE__*/React.createElement("p", null, o.text), /*#__PURE__*/React.createElement("div", {
    className: "mini-palette"
  }, /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("span", null, "16 \xD7 16 \u57FA\u7840\u7F51\u683C"))))))))), /*#__PURE__*/React.createElement("footer", null, /*#__PURE__*/React.createElement("span", null, "\u672C\u8F6E A+ \xB7 \u53E0\u5C4F\u9759\u591C\uFF1AA \u7684\u53CC\u5C4F\u8F6E\u5ED3\uFF0C\u52A0\u4E0A B \u7684\u6708\u7259\u3002"), /*#__PURE__*/React.createElement("small", null, "A+ \u5DF2\u786E\u8BA4 \xB7 \u5DF2\u751F\u6210\u591A\u5C3A\u5BF8 ICO \u5E76\u63A5\u5165\u5BA2\u6237\u7AEF\u3002"), /*#__PURE__*/React.createElement("a", {
    href: "index.html"
  }, "\u8FD4\u56DE\u5BA2\u6237\u7AEF\u8BBE\u8BA1 \u2197"))));
}
ReactDOM.createRoot(document.getElementById('root')).render(/*#__PURE__*/React.createElement(App, null));