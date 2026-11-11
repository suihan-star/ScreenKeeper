const candidates = [{
  id: 'outline',
  letter: 'A',
  name: '雾绿轮廓',
  lead: '已确认 · 两处统一',
  body: '保持原来的叠屏与月牙，深色任务栏用浅绿，浅色任务栏用深绿。',
  tradeoff: '轻巧熟悉，图形的色块面积仍然较小。'
}, {
  id: 'tile',
  letter: 'B',
  name: '雾绿底牌',
  lead: '历史对照',
  body: '柔和的浅绿底牌托住深绿叠屏，在一排应用里更容易找到。',
  tradeoff: '任务栏更醒目；16 px 托盘建议保留 A 的透明轮廓。'
}, {
  id: 'filled',
  letter: 'C',
  name: '双层实色',
  lead: '更清晰的月牙',
  body: '填实前方屏幕、保留后屏轮廓，用反色月牙增加小尺寸辨识度。',
  tradeoff: '没有外部底牌，但比原标识更厚实。'
}];
function iconPath(id, theme = 'dark') {
  return 'taskbar-icons-v6/' + (id === 'original' ? 'original.svg' : id === 'tile' ? 'tile.svg' : id + '-' + theme + '.svg');
}
function Mark({
  id,
  theme = 'dark',
  size = 24
}) {
  return /*#__PURE__*/React.createElement("img", {
    className: "mark",
    src: iconPath(id, theme),
    width: size,
    height: size,
    alt: id === 'original' ? '现有深绿图标' : candidates.find(c => c.id === id).name,
    draggable: "false"
  });
}
function MiniTray({
  id,
  theme
}) {
  return /*#__PURE__*/React.createElement("div", {
    className: 'mini-tray ' + theme
  }, /*#__PURE__*/React.createElement("span", {
    "aria-hidden": "true"
  }, "\u2303"), /*#__PURE__*/React.createElement(Mark, {
    id: id,
    theme: theme,
    size: 16
  }), /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "16",
    viewBox: "0 0 16 16",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "1.4",
    "aria-hidden": "true"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M2 6h3l4-3v10l-4-3H2ZM11 5q4 3 0 6"
  })), /*#__PURE__*/React.createElement("span", {
    className: "time"
  }, "14:28"));
}
function ScreenshotRow({
  id,
  label
}) {
  return /*#__PURE__*/React.createElement("div", {
    className: "screenshot-line"
  }, /*#__PURE__*/React.createElement("div", {
    className: "shot-label"
  }, label), /*#__PURE__*/React.createElement("div", {
    className: "shot-scroll"
  }, /*#__PURE__*/React.createElement("div", {
    className: "shot"
  }, /*#__PURE__*/React.createElement("img", {
    src: "taskbar-icons-v6/taskbar-generic.svg",
    width: "984",
    height: "52",
    alt: "\u901A\u7528\u4EFB\u52A1\u680F\u793A\u610F\uFF0C\u4E0D\u542B\u4E2A\u4EBA\u5E94\u7528\u5217\u8868"
  }), id !== 'original' && /*#__PURE__*/React.createElement("div", {
    className: "replacement",
    "aria-label": "\u56FE\u6807\u4F4D\u7F6E\u6A21\u62DF\u66FF\u6362"
  }, /*#__PURE__*/React.createElement(Mark, {
    id: id
  })))));
}
function App() {
  const [selected, setSelected] = React.useState('outline');
  const [original, setOriginal] = React.useState(false);
  const [tweaks, setTweaks] = React.useState(false);
  const [theme, setTheme] = React.useState('dark');
  const choice = candidates.find(c => c.id === selected);
  return /*#__PURE__*/React.createElement("div", {
    className: "review-page"
  }, /*#__PURE__*/React.createElement("header", {
    className: "page-header"
  }, /*#__PURE__*/React.createElement("a", {
    className: "brand",
    href: "index.html"
  }, /*#__PURE__*/React.createElement(Mark, {
    id: "outline",
    theme: "light",
    size: 27
  }), /*#__PURE__*/React.createElement("strong", null, "\u5C4F\u5E55\u826F\u4F34"), /*#__PURE__*/React.createElement("span", null, "\u56FE\u6807\u53EF\u89C1\u5EA6 \xB7 06")), /*#__PURE__*/React.createElement("span", {
    className: "draft"
  }, "\u5DF2\u786E\u8BA4 A \xB7 \u4EFB\u52A1\u680F\u4E0E\u6258\u76D8")), /*#__PURE__*/React.createElement("main", null, /*#__PURE__*/React.createElement("section", {
    className: "intro"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("p", {
    className: "eyebrow"
  }, "A+ \u53E0\u5C4F\u9759\u591C / \u53EF\u89C1\u5EA6\u4F18\u5316"), /*#__PURE__*/React.createElement("h1", null, "\u66F4\u5BB9\u6613\u627E\u5230\uFF0C", /*#__PURE__*/React.createElement("br", null), "\u4ECD\u7136\u5B89\u9759\u8212\u670D\u3002")), /*#__PURE__*/React.createElement("p", null, "\u53E0\u5C4F\u4E0E\u6708\u7259\u4FDD\u7559\u3002", /*#__PURE__*/React.createElement("br", null), "\u8FD9\u6B21\u91CD\u70B9\u770B\uFF1A\u653E\u56DE\u4EFB\u52A1\u680F\uFF0C", /*#__PURE__*/React.createElement("br", null), "\u80FD\u4E0D\u80FD\u4E00\u773C\u8BA4\u51FA\u6765\u3002")), /*#__PURE__*/React.createElement("section", {
    className: "comparison",
    "aria-labelledby": "real-title"
  }, /*#__PURE__*/React.createElement("div", {
    className: "section-heading"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", {
    id: "real-title"
  }, "\u5148\u653E\u56DE\u4EFB\u52A1\u680F"), /*#__PURE__*/React.createElement("p", null, "\u901A\u7528\u4EFB\u52A1\u680F\u793A\u610F\u4E0E 24 px \u56FE\u6807\u6A21\u62DF\uFF0C\u70B9\u51FB\u4E0B\u65B9\u5207\u6362\u3002")), /*#__PURE__*/React.createElement("button", {
    className: "text-button",
    "aria-pressed": original,
    onClick: () => setOriginal(!original)
  }, original ? '返回所选方案' : '对照原图标')), /*#__PURE__*/React.createElement("div", {
    className: "choice-tabs",
    role: "group",
    "aria-label": "\u9009\u62E9\u56FE\u6807\u65B9\u6848"
  }, candidates.map(c => /*#__PURE__*/React.createElement("button", {
    key: c.id,
    className: "choice-tab",
    "aria-pressed": selected === c.id,
    onClick: () => {
      setSelected(c.id);
      setOriginal(false);
    }
  }, /*#__PURE__*/React.createElement(Mark, {
    id: c.id,
    size: 24
  }), /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("strong", null, c.letter, " \xB7 ", c.name), /*#__PURE__*/React.createElement("small", null, c.lead))))), /*#__PURE__*/React.createElement("div", {
    className: "screenshot-pair"
  }, /*#__PURE__*/React.createElement(ScreenshotRow, {
    id: "original",
    label: "\u539F\u56FE\u6807"
  }), /*#__PURE__*/React.createElement(ScreenshotRow, {
    id: original ? 'original' : selected,
    label: original ? '原图标' : choice.letter + ' 方案'
  })), /*#__PURE__*/React.createElement("div", {
    className: "selection-caption",
    "aria-live": "polite"
  }, /*#__PURE__*/React.createElement("strong", null, original ? '现有深绿色图标' : choice.letter + ' · ' + choice.name), /*#__PURE__*/React.createElement("span", null, original ? '在深色任务栏上，轮廓容易融入背景。' : choice.body)), /*#__PURE__*/React.createElement("p", {
    className: "fineprint"
  }, "\u901A\u7528\u793A\u610F\u4E0D\u5BF9\u5E94\u771F\u5B9E\u7528\u6237\u7684\u8F6F\u4EF6\u5217\u8868\uFF1B\u4E0B\u6392\u4EC5\u5728\u56FE\u6807\u4F4D\u7F6E\u53E0\u52A0\u6A21\u62DF\u56FE\u5C42\u3002\u6D4F\u89C8\u5668\u6548\u679C\u9700\u5728\u5B9E\u65BD\u540E\u7528 Windows \u5B9E\u673A\u590D\u6838\u3002")), /*#__PURE__*/React.createElement("section", {
    className: "size-section",
    "aria-labelledby": "size-title"
  }, /*#__PURE__*/React.createElement("div", {
    className: "section-heading"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", {
    id: "size-title"
  }, "\u5927\u5C0F\u4E0D\u53D8\uFF0C\u8FA8\u8BC6\u5EA6\u4E0D\u540C"), /*#__PURE__*/React.createElement("p", null, "\u5B9E\u9645 CSS \u50CF\u7D20\u5927\u5C0F\uFF0C\u4E0D\u968F\u4E0B\u65B9\u753B\u5E03\u7F29\u653E\u3002")), /*#__PURE__*/React.createElement("button", {
    className: "text-button",
    "aria-expanded": tweaks,
    onClick: () => setTweaks(!tweaks)
  }, "\u80CC\u666F\u5207\u6362 \xB7 Tweaks ", tweaks ? '−' : '＋')), tweaks && /*#__PURE__*/React.createElement("div", {
    className: "tweaks",
    role: "group",
    "aria-label": "\u4EFB\u52A1\u680F\u80CC\u666F"
  }, /*#__PURE__*/React.createElement("span", null, "\u5BF9\u6BD4\u80CC\u666F"), /*#__PURE__*/React.createElement("button", {
    className: "chip",
    "aria-pressed": theme === 'dark',
    onClick: () => setTheme('dark')
  }, "\u6DF1\u8272\u4EFB\u52A1\u680F"), /*#__PURE__*/React.createElement("button", {
    className: "chip",
    "aria-pressed": theme === 'light',
    onClick: () => setTheme('light')
  }, "\u6D45\u8272\u4EFB\u52A1\u680F")), /*#__PURE__*/React.createElement("div", {
    className: 'size-table ' + theme,
    role: "table",
    "aria-label": "\u56FE\u6807\u5C3A\u5BF8\u5BF9\u7167"
  }, /*#__PURE__*/React.createElement("div", {
    className: "size-row heading",
    role: "row"
  }, /*#__PURE__*/React.createElement("span", {
    role: "columnheader"
  }, theme === 'dark' ? '深色任务栏' : '浅色任务栏'), [16, 20, 24, 32].map(s => /*#__PURE__*/React.createElement("span", {
    role: "columnheader",
    key: s
  }, s, " px"))), [{
    id: 'original',
    letter: '原',
    name: '当前图标'
  }, ...candidates].map(c => /*#__PURE__*/React.createElement("div", {
    key: c.id,
    className: 'size-row ' + (c.id === selected ? 'selected' : ''),
    role: "row"
  }, /*#__PURE__*/React.createElement("span", {
    role: "rowheader"
  }, c.letter, " \xB7 ", c.name, c.id === selected && /*#__PURE__*/React.createElement("small", null, "\u5F53\u524D\u9884\u89C8")), [16, 20, 24, 32].map(s => /*#__PURE__*/React.createElement("div", {
    role: "cell",
    key: s
  }, /*#__PURE__*/React.createElement(Mark, {
    id: c.id,
    theme: theme,
    size: s
  })))))), /*#__PURE__*/React.createElement("div", {
    className: "usage"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("p", {
    className: "eyebrow"
  }, "\u6700\u7EC8\u9009\u62E9"), /*#__PURE__*/React.createElement("h3", null, "\u4EFB\u52A1\u680F\u4E0E\u6258\u76D8\uFF0C\u7EDF\u4E00\u7528 A\u3002"), /*#__PURE__*/React.createElement("p", null, "\u6DF1\u8272\u4EFB\u52A1\u680F\u7528\u6D45\u7EFF\uFF0C\u6D45\u8272\u4EFB\u52A1\u680F\u7528\u6DF1\u7EFF\u3002", /*#__PURE__*/React.createElement("br", null), "\u4E24\u5904\u90FD\u4FDD\u7559\u900F\u660E\u80CC\u666F\u3001\u539F\u6765\u7684\u53E0\u5C4F\u4E0E\u6708\u7259\u3002")), /*#__PURE__*/React.createElement("div", {
    className: "usage-previews"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("span", null, "\u4EFB\u52A1\u680F / 24 px"), /*#__PURE__*/React.createElement("div", {
    className: 'mini-taskbar ' + theme
  }, /*#__PURE__*/React.createElement(Mark, {
    id: "outline",
    theme: theme,
    size: 24
  }), /*#__PURE__*/React.createElement("i", null))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("span", null, "\u53F3\u4E0B\u89D2\u6258\u76D8 / 16 px"), /*#__PURE__*/React.createElement(MiniTray, {
    id: "outline",
    theme: theme
  }))))), /*#__PURE__*/React.createElement("section", {
    className: "canvas-section"
  }, /*#__PURE__*/React.createElement("div", {
    className: "section-heading"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", null, "\u653E\u5927\u770B\u4E09\u79CD\u5904\u7406"), /*#__PURE__*/React.createElement("p", null, "\u53EF\u5E73\u79FB\u3001\u7F29\u653E\u753B\u5E03\uFF1B\u4E0A\u65B9\u5B9E\u9645\u5C3A\u5BF8\u66F4\u9002\u5408\u5224\u65AD\u65E5\u5E38\u89C2\u611F\u3002"))), /*#__PURE__*/React.createElement(DesignCanvas, {
    style: {
      height: 540,
      width: '100%',
      overflow: 'clip'
    }
  }, /*#__PURE__*/React.createElement(DCSection, {
    id: "taskbar-visibility-v6",
    title: "\u540C\u4E00\u4E2A A+\uFF0C\u4E09\u79CD\u6E05\u6670\u5EA6\u5904\u7406",
    subtitle: "\u5EF6\u7EED\u9759\u7EFF\uFF0C\u4E0D\u52A0\u5165\u9AD8\u4EAE\u5149\u6655",
    gap: 24
  }, candidates.map(c => /*#__PURE__*/React.createElement(DCArtboard, {
    key: c.id,
    id: 'visibility-' + c.id,
    label: c.letter + ' · ' + c.name,
    width: 292,
    height: 300
  }, /*#__PURE__*/React.createElement("article", {
    className: "concept"
  }, /*#__PURE__*/React.createElement("div", {
    className: "concept-id"
  }, /*#__PURE__*/React.createElement("span", null, c.letter, " / ", c.name), /*#__PURE__*/React.createElement("small", null, c.lead)), /*#__PURE__*/React.createElement("div", {
    className: "concept-mark"
  }, /*#__PURE__*/React.createElement(Mark, {
    id: c.id,
    size: 90
  }), /*#__PURE__*/React.createElement("div", {
    className: "light-sample"
  }, /*#__PURE__*/React.createElement(Mark, {
    id: c.id,
    theme: "light",
    size: 48
  }))), /*#__PURE__*/React.createElement("p", null, c.tradeoff), /*#__PURE__*/React.createElement("button", {
    className: "concept-select",
    onClick: () => {
      setSelected(c.id);
      setOriginal(false);
      window.scrollTo({
        top: 0,
        behavior: 'smooth'
      });
    }
  }, "\u9884\u89C8 ", c.letter, " \u65B9\u6848 \u2197"))))))), /*#__PURE__*/React.createElement("footer", null, /*#__PURE__*/React.createElement("p", null, "\u5DF2\u786E\u8BA4 A\uFF1A\u4EFB\u52A1\u680F\u4E0E\u6258\u76D8\u7EDF\u4E00\u8DDF\u968F Windows \u7CFB\u7EDF\u4E3B\u9898\u3002"), /*#__PURE__*/React.createElement("a", {
    href: iconPath(selected, theme),
    download: true
  }, "\u4E0B\u8F7D\u5F53\u524D SVG \u2197"), /*#__PURE__*/React.createElement("a", {
    href: "tray-icon.html"
  }, "\u67E5\u770B\u4E0A\u4E00\u8F6E\u8BBE\u8BA1 \u2197"))));
}
ReactDOM.createRoot(document.getElementById('root')).render(/*#__PURE__*/React.createElement(App, null));
