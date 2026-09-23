/* ScreenKeeper design-only prototype. All data is simulated and stays in memory. */
const {
  useState,
  useEffect
} = React;
function Icon({
  name
}) {
  const paths = {
    screen: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("rect", {
      x: "3",
      y: "4",
      width: "18",
      height: "13",
      rx: "2"
    }), /*#__PURE__*/React.createElement("path", {
      d: "M8 21h8M12 17v4"
    })),
    brand: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("rect", {
      x: "2",
      y: "7",
      width: "13",
      height: "10",
      rx: "1.5"
    }), /*#__PURE__*/React.createElement("path", {
      d: "M8 4h14v11M7 21h6M10 17v4"
    })),
    refresh: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
      d: "M20 10a8 8 0 0 0-14-5L3 8m0-5v5h5M4 14a8 8 0 0 0 14 5l3-3m0 5v-5h-5"
    })),
    check: /*#__PURE__*/React.createElement("path", {
      d: "m5 12 4 4L19 6"
    }),
    close: /*#__PURE__*/React.createElement("path", {
      d: "m6 6 12 12M6 18 18 6"
    }),
    minus: /*#__PURE__*/React.createElement("path", {
      d: "M5 12h14"
    }),
    expand: /*#__PURE__*/React.createElement("rect", {
      x: "5",
      y: "5",
      width: "14",
      height: "14",
      rx: "1"
    }),
    moon: /*#__PURE__*/React.createElement("path", {
      d: "M20 15a9 9 0 0 1-11-11 9 9 0 1 0 11 11Z"
    }),
    sun: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("circle", {
      cx: "12",
      cy: "12",
      r: "4"
    }), /*#__PURE__*/React.createElement("path", {
      d: "M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1 1m12 12 1 1M5 19l1-1M18 6l1-1"
    })),
    alert: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("path", {
      d: "m12 3 10 18H2Z"
    }), /*#__PURE__*/React.createElement("path", {
      d: "M12 9v5m0 3v.1"
    }))
  };
  return /*#__PURE__*/React.createElement("svg", {
    viewBox: "0 0 24 24",
    "aria-hidden": "true"
  }, paths[name] || paths.screen);
}
const initial = [{
  managed: false,
  mode: 'black',
  brightness: 30,
  zero: false,
  time: '5',
  unit: 'minutes',
  checks: [true, false, false]
}, {
  managed: true,
  mode: 'dim',
  brightness: 30,
  zero: false,
  time: '5',
  unit: 'minutes',
  checks: [true, true, false]
}];
const clone = x => JSON.parse(JSON.stringify(x));
function App() {
  const [language, setLanguage] = useState('zh-CN');
  const t = key => language === 'en' ? key.replace(key.trim(), englishCopy[key.trim()] || key.trim()) : key;
  const [theme, setTheme] = useState(new URLSearchParams(location.search).get('theme') === 'fog' ? 'fog' : 'forest');
  const [compact, setCompact] = useState(false),
    [scene, setScene] = useState('normal'),
    [selected, setSelected] = useState(1);
  const [draft, setDraft] = useState(clone(initial)),
    [saved, setSaved] = useState(clone(initial)),
    [dialog, setDialog] = useState(null),
    [notice, setNotice] = useState('设置已保存');
  const current = draft[selected],
    dirty = JSON.stringify(draft) !== JSON.stringify(saved),
    noDDC = scene === 'unsupported';
  const badTime = c => !/^\d+$/.test(c.time) || Number(c.time) < 1 || Number(c.time) > 2147483647;
  const invalid = draft.some(c => c.managed && (badTime(c) || !c.checks.some(Boolean)));
  const unavailable = ['loading', 'empty', 'error'].includes(scene);
  useEffect(() => {
    document.title = `ScreenKeeper · ${theme === 'forest' ? '深绿' : '雾白绿'}设计稿`;
  }, [theme]);
  useEffect(() => {
    function key(e) {
      if (e.key === 'Escape') setDialog(null);
    }
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, []);
  useEffect(() => {
    if (!dialog) return;
    const previous = document.activeElement;
    const box = document.querySelector('.dialog');
    const controls = () => Array.from(box.querySelectorAll('button:not(:disabled)'));
    controls()[0]?.focus();
    function trap(e) {
      if (e.key !== 'Tab') return;
      const list = controls(),
        first = list[0],
        last = list[list.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }
    box.addEventListener('keydown', trap);
    return () => {
      box.removeEventListener('keydown', trap);
      previous?.focus();
    };
  }, [dialog]);
  function update(patch) {
    setDraft(a => a.map((c, i) => i === selected ? {
      ...c,
      ...patch
    } : c));
    setNotice('');
  }
  function save(close = false) {
    if (invalid) return;
    if (scene === 'save-error') {
      setDialog('save-error');
      return;
    }
    setSaved(clone(draft));
    setNotice('全部显示器设置已保存');
    setDialog(close ? 'hidden' : null);
  }
  function refresh() {
    if (dirty) {
      setDialog('refresh');
      return;
    }
    setScene('loading');
    setTimeout(() => setScene('normal'), 750);
  }
  function chooseScene(value) {
    setScene(value);
    setDialog(null);
    if (value === 'unsupported') {
      setDraft(a => a.map((c, i) => i === selected ? {
        ...c,
        mode: 'black',
        zero: false
      } : c));
    }
    if (value === 'save-error') {
      setDraft(a => a.map((c, i) => i === selected ? {
        ...c,
        time: String(Number(c.time || 5) + 1)
      } : c));
    }
  }
  function close() {
    setDialog(dirty ? 'unsaved' : 'hidden');
  }
  const choices = [['鼠标位于此显示器', '光标停留在这里时保持唤醒'], ['当前活动窗口位于此显示器', '正在使用的窗口在这里时保持唤醒'], ['系统有键盘或鼠标输入', '在任意屏幕操作时都保持唤醒']];
  return /*#__PURE__*/React.createElement("main", {
    className: "review"
  }, /*#__PURE__*/React.createElement("header", {
    className: "review-header"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    className: "eyebrow"
  }, "SCREENKEEPER / DESIGN STUDY 01"), /*#__PURE__*/React.createElement("h1", null, "\u9759\u7EFF \xB7 \u8BA9\u754C\u9762\u5B89\u9759\u4E0B\u6765")), /*#__PURE__*/React.createElement("div", {
    className: "review-actions"
  }, /*#__PURE__*/React.createElement("div", {
    className: "themes",
    "aria-label": "\u8BBE\u8BA1\u7A3F\u914D\u8272"
  }, /*#__PURE__*/React.createElement("button", {
    className: theme === 'forest' ? 'active' : '',
    "aria-pressed": theme === 'forest',
    onClick: () => setTheme('forest')
  }, "01 \u6DF1\u7EFF"), /*#__PURE__*/React.createElement("button", {
    className: theme === 'fog' ? 'active' : '',
    "aria-pressed": theme === 'fog',
    onClick: () => setTheme('fog')
  }, "02 \u96FE\u767D\u7EFF")), /*#__PURE__*/React.createElement("select", {
    "aria-label": "\u9884\u89C8\u72B6\u6001",
    value: scene,
    onChange: e => chooseScene(e.target.value)
  }, /*#__PURE__*/React.createElement("option", {
    value: "normal"
  }, "\u5E38\u89C4\u8BBE\u7F6E"), /*#__PURE__*/React.createElement("option", {
    value: "loading"
  }, "\u52A0\u8F7D\u4E2D"), /*#__PURE__*/React.createElement("option", {
    value: "empty"
  }, "\u65E0\u663E\u793A\u5668"), /*#__PURE__*/React.createElement("option", {
    value: "error"
  }, "\u8BFB\u53D6\u5931\u8D25"), /*#__PURE__*/React.createElement("option", {
    value: "unsupported"
  }, "\u4E0D\u652F\u6301\u4EAE\u5EA6"), /*#__PURE__*/React.createElement("option", {
    value: "save-error"
  }, "\u4FDD\u5B58\u5931\u8D25")), /*#__PURE__*/React.createElement("select", {
    "aria-label": "\u7A97\u53E3\u5C3A\u5BF8",
    value: compact ? 'compact' : 'regular',
    onChange: e => setCompact(e.target.value === 'compact')
  }, /*#__PURE__*/React.createElement("option", {
    value: "regular"
  }, "\u6807\u51C6\u7A97\u53E3"), /*#__PURE__*/React.createElement("option", {
    value: "compact"
  }, "700 \xD7 500")))), /*#__PURE__*/React.createElement("div", {
    className: "stage"
  }, /*#__PURE__*/React.createElement("section", {
    className: `app ${theme} ${compact ? 'compact' : ''}`,
    "data-screen-label": theme === 'forest' ? t("深绿主方案") : t("雾白绿对照稿"),
    lang: language,
    "aria-label": t("多屏管家设置窗口")
  }, /*#__PURE__*/React.createElement("header", {
    className: "titlebar"
  }, /*#__PURE__*/React.createElement("div", {
    className: "brand"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "brand"
  }), t("多屏管家 "), language === 'zh-CN' && /*#__PURE__*/React.createElement("small", null, "ScreenKeeper")), /*#__PURE__*/React.createElement("div", {
    className: "titlebar-tools"
  }, /*#__PURE__*/React.createElement("label", {
    className: "language-setting"
  }, /*#__PURE__*/React.createElement("span", null, "\u8BED\u8A00 / Language"), /*#__PURE__*/React.createElement("select", {
    "aria-label": "\u8BED\u8A00 / Language",
    value: language,
    onChange: e => setLanguage(e.target.value)
  }, /*#__PURE__*/React.createElement("option", {
    value: "zh-CN"
  }, "\u7B80\u4F53\u4E2D\u6587"), /*#__PURE__*/React.createElement("option", {
    value: "en"
  }, "English"))), /*#__PURE__*/React.createElement("div", {
    className: "window-actions"
  }, /*#__PURE__*/React.createElement("button", {
    "aria-label": t("收起窗口"),
    onClick: close
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "minus"
  })), /*#__PURE__*/React.createElement("button", {
    "aria-label": t("切换窗口尺寸"),
    onClick: () => setCompact(!compact)
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "expand"
  })), /*#__PURE__*/React.createElement("button", {
    "aria-label": t("关闭窗口"),
    onClick: close
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "close"
  }))))), /*#__PURE__*/React.createElement("div", {
    className: "workspace"
  }, /*#__PURE__*/React.createElement("div", {
    className: "page-head"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", null, t("显示器管理")), /*#__PURE__*/React.createElement("p", null, t("为每一块屏幕，设置合适的休息方式。"))), /*#__PURE__*/React.createElement("button", {
    className: "button",
    onClick: refresh,
    disabled: scene === 'loading'
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "refresh"
  }), t("刷新显示器"))), unavailable ? /*#__PURE__*/React.createElement("div", {
    className: "empty-state"
  }, /*#__PURE__*/React.createElement("div", {
    className: "empty-icon"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: scene === 'error' ? 'alert' : 'screen'
  })), /*#__PURE__*/React.createElement("h3", null, scene === 'loading' ? t("正在识别显示器") : scene === 'empty' ? t("暂未发现显示器") : t("暂时无法读取显示器")), /*#__PURE__*/React.createElement("p", null, scene === 'loading' ? t("正在读取显示器布局与亮度控制能力。") : scene === 'empty' ? t("请确认显示器已连接并开启，然后重新检测。") : t("请检查显示器连接后重试。已有配置仍然保留。")), scene !== 'loading' && /*#__PURE__*/React.createElement("button", {
    className: "button primary",
    onClick: refresh
  }, t("重新检测"))) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("section", {
    className: "topology",
    "aria-label": t("显示器布局")
  }, /*#__PURE__*/React.createElement("div", {
    className: "layout-note"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "02", /*#__PURE__*/React.createElement("span", null, t(" 台"))), /*#__PURE__*/React.createElement("span", null, t("已连接显示器"))), /*#__PURE__*/React.createElement("div", {
    className: "dotline"
  }, /*#__PURE__*/React.createElement("span", {
    className: "dot"
  }), t("点击屏幕"), /*#__PURE__*/React.createElement("br", null), t("分别设置"))), /*#__PURE__*/React.createElement("div", {
    className: "monitors"
  }, draft.map((m, i) => /*#__PURE__*/React.createElement("button", {
    key: i,
    className: `monitor ${i === 1 ? 'secondary' : ''} ${i === selected ? 'selected' : ''}`,
    "aria-label": `${t('选择显示器')} ${i + 1}${i === 0 ? t("（主屏）") : ''}`,
    "aria-pressed": i === selected,
    onClick: () => setSelected(i)
  }, /*#__PURE__*/React.createElement("div", {
    className: "monitor-top"
  }, /*#__PURE__*/React.createElement("span", {
    className: "number"
  }, "0", i + 1), /*#__PURE__*/React.createElement("span", {
    className: "chip"
  }, m.managed ? t("已启用管理") : t("未启用管理"))), /*#__PURE__*/React.createElement("div", {
    className: "monitor-bottom"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    className: "name"
  }, t("显示器 "), i + 1, i === 0 ? t(" · 主屏") : ''), /*#__PURE__*/React.createElement("div", {
    className: "resolution"
  }, i === 0 ? '1920 × 1080' : '2560 × 1440')), selected === i && /*#__PURE__*/React.createElement("span", {
    className: "selected-check"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "check"
  }))))))), /*#__PURE__*/React.createElement("section", {
    className: "config",
    "aria-label": t("显示器设置")
  }, /*#__PURE__*/React.createElement("div", {
    className: "config-head"
  }, /*#__PURE__*/React.createElement("div", {
    className: "config-title"
  }, /*#__PURE__*/React.createElement("div", {
    className: "screen-badge"
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "screen"
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h3", null, t("显示器 "), selected + 1, selected === 0 ? t(" · 主屏") : ''), /*#__PURE__*/React.createElement("p", null, noDDC ? t("此显示器不支持 DDC/CI 亮度调节") : t("单独设置，仅影响这块屏幕")))), /*#__PURE__*/React.createElement("div", {
    className: "switch-label"
  }, /*#__PURE__*/React.createElement("span", null, current.managed ? t("已启用管理") : t("未启用管理")), /*#__PURE__*/React.createElement("button", {
    role: "switch",
    "aria-label": t("管理此显示器"),
    "aria-checked": current.managed,
    className: `switch ${current.managed ? 'on' : ''}`,
    onClick: () => update({
      managed: !current.managed
    })
  }, /*#__PURE__*/React.createElement("span", null)))), /*#__PURE__*/React.createElement("fieldset", {
    disabled: !current.managed,
    className: !current.managed ? 'dimmed' : ''
  }, /*#__PURE__*/React.createElement("div", {
    className: "config-grid"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    className: "field-title"
  }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("span", {
    className: "index"
  }, "01"), t("空闲时的操作"))), /*#__PURE__*/React.createElement("div", {
    className: "modes",
    role: "group",
    "aria-label": t("空闲时的操作")
  }, /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: `mode ${current.mode === 'black' ? 'active' : ''}`,
    "aria-pressed": current.mode === 'black',
    onClick: () => update({
      mode: 'black'
    })
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "moon"
  }), t("遮黑")), /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: `mode ${current.mode === 'dim' ? 'active' : ''}`,
    "aria-pressed": current.mode === 'dim',
    disabled: noDDC,
    onClick: () => update({
      mode: 'dim'
    })
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "sun"
  }), t("调暗"))), /*#__PURE__*/React.createElement("div", {
    className: "option-area"
  }, current.mode === 'dim' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("p", {
    className: "subtle"
  }, t("目标亮度")), /*#__PURE__*/React.createElement("div", {
    className: "range-row"
  }, /*#__PURE__*/React.createElement("input", {
    type: "range",
    "aria-label": t("目标亮度"),
    min: "0",
    max: "100",
    value: current.brightness,
    onChange: e => update({
      brightness: Number(e.target.value)
    })
  }), /*#__PURE__*/React.createElement("output", null, current.brightness, "%"))) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("label", {
    className: "check-row"
  }, /*#__PURE__*/React.createElement("input", {
    type: "checkbox",
    disabled: noDDC,
    checked: current.zero && !noDDC,
    onChange: e => update({
      zero: e.target.checked
    })
  }), t("同时将亮度降至零")), /*#__PURE__*/React.createElement("p", {
    className: "subtle"
  }, noDDC ? t("可使用遮黑；亮度控制不可用。") : t("使用黑色遮罩覆盖这块屏幕。")))), /*#__PURE__*/React.createElement("div", {
    className: "timeout"
  }, /*#__PURE__*/React.createElement("div", {
    className: "field-title"
  }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("span", {
    className: "index"
  }, "02"), t("空闲多久后执行"))), /*#__PURE__*/React.createElement("div", {
    className: "time-input"
  }, /*#__PURE__*/React.createElement("input", {
    type: "number",
    "aria-label": t("空闲时长"),
    min: "1",
    max: "2147483647",
    step: "1",
    "aria-invalid": badTime(current),
    value: current.time,
    onChange: e => update({
      time: e.target.value
    })
  }), /*#__PURE__*/React.createElement("select", {
    "aria-label": t("时间单位"),
    value: current.unit,
    onChange: e => update({
      unit: e.target.value
    })
  }, /*#__PURE__*/React.createElement("option", {
    value: "seconds"
  }, t("秒")), /*#__PURE__*/React.createElement("option", {
    value: "minutes"
  }, t("分钟")), /*#__PURE__*/React.createElement("option", {
    value: "hours"
  }, t("小时"))), /*#__PURE__*/React.createElement("span", {
    className: "subtle"
  }, t("无活动后开始计时"))), badTime(current) && /*#__PURE__*/React.createElement("p", {
    role: "alert",
    className: "validation"
  }, t("请输入 1～2147483647 的整数。")))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    className: "field-title"
  }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("span", {
    className: "index"
  }, "03"), t("保持唤醒的条件"))), /*#__PURE__*/React.createElement("p", {
    className: "subtle condition-head"
  }, t("满足以下任一条件，这块屏幕就保持唤醒。")), /*#__PURE__*/React.createElement("div", {
    className: "condition-list"
  }, choices.map(([title, subtitle], i) => /*#__PURE__*/React.createElement("label", {
    key: title,
    className: "check-row"
  }, /*#__PURE__*/React.createElement("input", {
    type: "checkbox",
    checked: current.checks[i],
    onChange: e => update({
      checks: current.checks.map((c, k) => i === k ? e.target.checked : c)
    })
  }), /*#__PURE__*/React.createElement("span", null, t(title), /*#__PURE__*/React.createElement("small", null, t(subtitle)))))), !current.checks.some(Boolean) && /*#__PURE__*/React.createElement("p", {
    role: "alert",
    className: "validation"
  }, t("请至少选择一个保持唤醒的条件。")))))))), /*#__PURE__*/React.createElement("footer", {
    className: "footer"
  }, /*#__PURE__*/React.createElement("div", {
    className: `save-status ${dirty ? 'dirty' : ''}`,
    role: "status"
  }, /*#__PURE__*/React.createElement("span", {
    className: "dot"
  }), dirty ? t("有未保存的更改") : t(notice || '设置已保存')), /*#__PURE__*/React.createElement("div", {
    className: "footer-actions"
  }, /*#__PURE__*/React.createElement("button", {
    className: "button",
    disabled: !dirty || unavailable,
    onClick: () => {
      setDraft(clone(saved));
      setNotice('已放弃本次更改');
    }
  }, t("放弃更改")), /*#__PURE__*/React.createElement("button", {
    className: "button primary",
    disabled: !dirty || invalid || unavailable,
    onClick: () => save()
  }, t("保存全部设置")))), dialog && /*#__PURE__*/React.createElement("div", {
    className: "dialog-shade"
  }, /*#__PURE__*/React.createElement("section", {
    className: "dialog",
    role: "dialog",
    "aria-modal": "true",
    "aria-label": dialog === 'unsaved' ? t("有未保存的更改") : dialog === 'save-error' ? t("设置未能保存") : dialog === 'refresh' ? t("刷新前请先处理更改") : t("已收起到托盘")
  }, /*#__PURE__*/React.createElement("h3", null, dialog === 'unsaved' ? t("有未保存的更改") : dialog === 'save-error' ? t("设置未能保存") : dialog === 'refresh' ? t("刷新前请先处理更改") : t("已收起到托盘")), /*#__PURE__*/React.createElement("p", null, dialog === 'unsaved' ? t("这些更改尚未生效。保存后关闭，或放弃本次更改。") : dialog === 'save-error' ? t("请确认配置目录可写后重试。你的更改仍保留在当前窗口中。") : dialog === 'refresh' ? t("先保存或放弃更改，再重新读取显示器。") : t("这是设计演示。正式客户端会继续在托盘中运行。")), /*#__PURE__*/React.createElement("div", {
    className: "dialog-actions"
  }, dialog === 'unsaved' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("button", {
    className: "button",
    autoFocus: true,
    onClick: () => setDialog(null)
  }, t("继续编辑")), /*#__PURE__*/React.createElement("button", {
    className: "button",
    onClick: () => {
      setDraft(clone(saved));
      setDialog('hidden');
    }
  }, t("放弃并关闭")), /*#__PURE__*/React.createElement("button", {
    className: "button primary",
    disabled: invalid,
    onClick: () => save(true)
  }, t("保存并关闭"))) : /*#__PURE__*/React.createElement("button", {
    className: "button primary",
    autoFocus: true,
    onClick: () => setDialog(null)
  }, dialog === 'hidden' ? t("返回设计稿") : t("返回设置"))))))), /*#__PURE__*/React.createElement("aside", {
    className: "review-notes"
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, theme === 'forest' ? '01 / 深绿 · 主方案' : '02 / 雾白绿 · 对照方案'), theme === 'forest' ? '森林绿底色，鼠尾草绿强调，适合偏暗环境。' : '淡绿灰底色，深绿文字，适合明亮环境。', /*#__PURE__*/React.createElement("div", {
    className: "swatches"
  }, /*#__PURE__*/React.createElement("span", {
    className: `swatch ${theme}1`
  }), /*#__PURE__*/React.createElement("span", {
    className: `swatch ${theme}2`
  }), /*#__PURE__*/React.createElement("span", {
    className: `swatch ${theme}3`
  }))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "\u53EF\u4EE5\u8BD5\u7740\u64CD\u4F5C"), "\u5207\u6362\u5C4F\u5E55\u3001\u8C03\u6574\u4EAE\u5EA6\u3001\u4FEE\u6539\u6761\u4EF6\uFF0C\u518D\u4FDD\u5B58\u6216\u653E\u5F03\u3002", /*#__PURE__*/React.createElement("div", {
    className: "review-caption"
  }, "\u8BBE\u8BA1\u539F\u578B \xB7 \u793A\u4F8B\u6570\u636E \xB7 \u4E0D\u63A7\u5236\u771F\u5B9E\u663E\u793A\u5668")), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "\u5F85\u786E\u8BA4"), "\u6574\u4F53\u914D\u8272 / \u5E03\u5C40\u5BC6\u5EA6 / \u63A7\u4EF6\u6837\u5F0F", /*#__PURE__*/React.createElement("br", null), /*#__PURE__*/React.createElement("a", {
    className: "review-link",
    href: "README.md"
  }, "\u8BBE\u8BA1\u8BF4\u660E \u2197"))));
}
ReactDOM.createRoot(document.getElementById('root')).render(/*#__PURE__*/React.createElement(App, null));