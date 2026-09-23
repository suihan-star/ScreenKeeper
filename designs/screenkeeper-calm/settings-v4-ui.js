'use strict';

function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
(() => {
  const {
    useEffect,
    useRef
  } = React;
  const Icon = window.SK.Icon;
  function Button({
    children,
    icon,
    kind = '',
    small = false,
    ...props
  }) {
    return /*#__PURE__*/React.createElement("button", _extends({
      type: "button",
      className: `btn ${kind} ${small ? 'small' : ''}`
    }, props), icon && /*#__PURE__*/React.createElement(Icon, {
      name: icon,
      size: 15
    }), " ", children);
  }
  function Toggle({
    checked,
    onChange,
    label,
    disabled = false
  }) {
    return /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: "switch",
      role: "switch",
      "aria-checked": checked,
      "aria-label": label,
      disabled: disabled,
      onClick: () => onChange(!checked)
    }, /*#__PURE__*/React.createElement("span", null));
  }
  function Tabs({
    items,
    value,
    onChange,
    label
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "tabs",
      role: "tablist",
      "aria-label": label
    }, items.map(([id, name, icon]) => /*#__PURE__*/React.createElement("button", {
      key: id,
      type: "button",
      role: "tab",
      "aria-selected": value === id,
      className: value === id ? 'active' : '',
      onClick: () => onChange(id)
    }, icon && /*#__PURE__*/React.createElement(Icon, {
      name: icon,
      size: 15
    }), " ", name)));
  }
  function Choices({
    items,
    value,
    onChange,
    label
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "choices",
      role: "radiogroup",
      "aria-label": label
    }, items.map(([id, name, note, icon]) => /*#__PURE__*/React.createElement("button", {
      type: "button",
      role: "radio",
      "aria-checked": value === id,
      key: id,
      onClick: () => onChange(id),
      className: `choice ${value === id ? 'active' : ''}`
    }, /*#__PURE__*/React.createElement(Icon, {
      name: icon,
      size: 20
    }), /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("strong", null, name), note && /*#__PURE__*/React.createElement("small", null, note)), /*#__PURE__*/React.createElement("span", {
      className: "choice-check"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "check",
      size: 13
    })))));
  }
  function Unit({
    value,
    unit,
    onValue,
    onUnit,
    units,
    t,
    label,
    min = 1,
    max = 1440
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "unit"
    }, /*#__PURE__*/React.createElement("input", {
      className: "input",
      type: "number",
      min: min,
      max: max,
      value: value,
      "aria-label": label,
      onChange: e => onValue(e.target.value === '' ? '' : Number(e.target.value))
    }), units ? /*#__PURE__*/React.createElement("select", {
      "aria-label": t('时间单位', 'Time unit'),
      value: unit,
      onChange: e => onUnit(e.target.value)
    }, units.map(x => /*#__PURE__*/React.createElement("option", {
      key: x,
      value: x
    }, x === 's' ? t('秒', 'sec') : x === 'm' ? t('分钟', 'min') : t('小时', 'hr')))) : /*#__PURE__*/React.createElement("span", null, unit));
  }
  function Setting({
    label,
    sub,
    children
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "setting-row"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
      className: "label"
    }, label), sub && /*#__PURE__*/React.createElement("div", {
      className: "sub"
    }, sub)), /*#__PURE__*/React.createElement("div", null, children));
  }
  function ThemePicker({
    theme,
    onChange,
    t
  }) {
    return /*#__PURE__*/React.createElement("section", {
      className: "theme-section"
    }, /*#__PURE__*/React.createElement("h3", {
      id: "theme-heading"
    }, t('外观主题', 'Appearance theme')), /*#__PURE__*/React.createElement("div", {
      className: "theme-options",
      role: "radiogroup",
      "aria-labelledby": "theme-heading"
    }, ['forest', 'fog'].map(id => /*#__PURE__*/React.createElement("button", {
      type: "button",
      role: "radio",
      "aria-checked": theme === id,
      "aria-label": id === 'forest' ? t('深绿', 'Forest green') : t('雾白绿', 'Mist green'),
      key: id,
      className: "theme-option",
      onClick: () => onChange(id),
      onKeyDown: e => {
        if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(e.key)) {
          e.preventDefault();
          onChange(id === 'forest' ? 'fog' : 'forest');
          const next = e.currentTarget.parentElement.querySelector(`[aria-label="${id === 'forest' ? t('雾白绿', 'Mist green') : t('深绿', 'Forest green')}"]`);
          next?.focus();
        }
      }
    }, /*#__PURE__*/React.createElement("span", {
      className: `theme-sample ${id}`,
      "aria-hidden": "true"
    }, /*#__PURE__*/React.createElement("span", {
      className: "sample-title"
    }, /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null)), /*#__PURE__*/React.createElement("span", {
      className: "sample-nav"
    }, /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null)), /*#__PURE__*/React.createElement("span", {
      className: "sample-body"
    }, /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("i", null), /*#__PURE__*/React.createElement("b", null))), /*#__PURE__*/React.createElement("span", {
      className: "theme-option-name"
    }, /*#__PURE__*/React.createElement("span", null, id === 'forest' ? t('深绿', 'Forest green') : t('雾白绿', 'Mist green'), id === 'forest' && /*#__PURE__*/React.createElement("span", {
      className: "default-label"
    }, t('默认', 'Default'))), /*#__PURE__*/React.createElement("span", {
      className: "theme-check"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "check",
      size: 14
    }))), /*#__PURE__*/React.createElement("small", null, id === 'forest' ? t('深绿底色，浅绿文字', 'Deep green with soft green text') : t('浅绿灰底色，深绿文字', 'Pale green-grey with dark green text'))))), /*#__PURE__*/React.createElement("p", {
      className: "hint"
    }, t('立即切换，记住你的选择。', 'Applies immediately. Your choice is remembered.')));
  }
  function Notice({
    title,
    children,
    action,
    info = false
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: `notice ${info ? 'info' : ''}`,
      role: "status"
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "info",
      size: 17
    }), /*#__PURE__*/React.createElement("div", {
      className: "notice-copy"
    }, /*#__PURE__*/React.createElement("strong", null, title), children && /*#__PURE__*/React.createElement("p", null, children), action && /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: 8
      }
    }, action)));
  }
  function Scene({
    style = 'fog',
    moving = true,
    rest = false,
    coverage = 65,
    fade = 8,
    intensity = 1,
    animate = false,
    time = '05:00',
    image,
    t
  }) {
    const mode = style === 'scenic' ? 'picture' : style;
    return /*#__PURE__*/React.createElement("div", {
      className: `scene ${mode} ${moving ? 'moving' : ''} ${animate ? 'fade-scene' : ''}`,
      style: {
        '--coverage': rest ? 1 : coverage / 100,
        '--fade': `${fade}s`,
        '--motion-time': `${17 / Number(intensity)}s`
      },
      "aria-label": t('效果示意', 'Effect illustration')
    }, !rest && style !== 'compact' && /*#__PURE__*/React.createElement("div", {
      className: "sample-work"
    }, /*#__PURE__*/React.createElement("span", null), /*#__PURE__*/React.createElement("span", null), /*#__PURE__*/React.createElement("span", null)), /*#__PURE__*/React.createElement("div", {
      className: "atmosphere"
    }, /*#__PURE__*/React.createElement("div", {
      className: "scene-bg",
      style: {
        backgroundImage: mode === 'picture' ? `linear-gradient(#0c201a38,#0c201a38),url("${image || 'forest-mist.png'}")` : undefined
      }
    }), style === 'fog' && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
      className: "fog-layer"
    }), /*#__PURE__*/React.createElement("div", {
      className: "fog-layer second"
    })), mode === 'night' && /*#__PURE__*/React.createElement("div", {
      className: "moon"
    })), /*#__PURE__*/React.createElement("div", {
      className: "scene-card"
    }, /*#__PURE__*/React.createElement("strong", null, rest ? t('给自己一点留白', 'A little room to breathe') : t('让视线，稍作停留', 'Let your eyes wander')), rest ? /*#__PURE__*/React.createElement("div", {
      className: "scene-time"
    }, time) : null, /*#__PURE__*/React.createElement("p", null, rest ? t('离开屏幕，慢慢放松', 'Step away. Take your time.') : t('工作可以稍后，休息就在此刻。', 'Your work can wait a moment.'))));
  }
  function Dialog({
    title,
    subtitle,
    children,
    actions,
    onClose,
    wide = false
  }) {
    const ref = useRef(null),
      closeRef = useRef(onClose);
    closeRef.current = onClose;
    useEffect(() => {
      const before = document.activeElement;
      const dialog = ref.current;
      const focusables = () => Array.from(dialog.querySelectorAll('button:not(:disabled),input:not(:disabled),select:not(:disabled),summary,[tabindex="0"]')).filter(el => el.getClientRects().length);
      (focusables()[0] || dialog).focus();
      const handle = e => {
        if (e.key === 'Escape') {
          e.preventDefault();
          closeRef.current();
        }
        if (e.key === 'Tab') {
          const items = focusables();
          if (!items.length) {
            e.preventDefault();
            return;
          }
          const first = items[0],
            last = items[items.length - 1];
          if (e.shiftKey && document.activeElement === first) {
            e.preventDefault();
            last.focus();
          } else if (!e.shiftKey && document.activeElement === last) {
            e.preventDefault();
            first.focus();
          }
        }
      };
      dialog.addEventListener('keydown', handle);
      return () => {
        dialog.removeEventListener('keydown', handle);
        if (before && before.isConnected) before.focus();
      };
    }, []);
    return /*#__PURE__*/React.createElement("div", {
      className: "modal-cover"
    }, /*#__PURE__*/React.createElement("section", {
      ref: ref,
      className: `dialog ${wide ? 'wide' : ''}`,
      role: "dialog",
      "aria-modal": "true",
      "aria-labelledby": "dialog-title",
      tabIndex: -1
    }, /*#__PURE__*/React.createElement("header", {
      className: "dialog-head"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", {
      id: "dialog-title"
    }, title), subtitle && /*#__PURE__*/React.createElement("p", null, subtitle)), /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: "close-btn",
      "aria-label": "\u5173\u95ED / Close",
      onClick: onClose
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "close",
      size: 19
    }))), /*#__PURE__*/React.createElement("div", {
      className: "dialog-body"
    }, children), /*#__PURE__*/React.createElement("footer", {
      className: "dialog-footer"
    }, actions)));
  }
  window.SK4UI = {
    Icon,
    Button,
    Toggle,
    Tabs,
    Choices,
    Unit,
    Setting,
    ThemePicker,
    Notice,
    Scene,
    Dialog
  };
})();
