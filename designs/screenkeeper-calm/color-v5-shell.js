'use strict';

(() => {
  const {
    useState,
    useEffect,
    useRef
  } = React;
  const {
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
  } = window.SK4UI;
  const themeKey = 'screenkeeper-design-v4-theme';
  const clone = x => JSON.parse(JSON.stringify(x));
  const curtainImage = s => s.background === 'solid' ? null : s.background === 'custom' ? s.images[0] || null : 'forest-mist.png';
  const curtainStyle = image => image ? `linear-gradient(#0c201a38,#0c201a38),url("${image}")` : undefined;
  const initial = () => ({
    screens: [{
      id: 'q',
      name: 'Q27P10',
      primary: true,
      managed: true,
      behavior: 'black',
      idle: 5,
      idleUnit: 'm',
      brightness: 20,
      wake: [true, true, false],
      privacy: true,
      privacyMode: 'curtain',
      privacyIdle: 30,
      privacyUnit: 's',
      snooze: 15,
      snoozeUnit: 'm',
      images: [],
      background: 'builtin',
      fit: 'cover',
      carousel: 30,
      application: '',
      launch: false,
      move: false
    }, {
      id: 'd',
      name: 'DELL SE2723DS',
      primary: false,
      managed: true,
      behavior: 'dim',
      idle: 3,
      idleUnit: 'm',
      brightness: 25,
      wake: [true, true, false],
      privacy: false,
      privacyMode: 'curtain',
      privacyIdle: 30,
      privacyUnit: 's',
      snooze: 15,
      snoozeUnit: 'm',
      images: [],
      background: 'builtin',
      fit: 'cover',
      carousel: 30,
      application: '',
      launch: false,
      move: false
    }],
    health: {
      enabled: true,
      work: 50,
      rest: 5,
      restUnit: 'm',
      snooze: 5,
      dnd: true,
      idlePause: true,
      reminder: 'scenic',
      restStyle: 'picture',
      fade: 8,
      coverage: 65,
      moving: true,
      intensity: 1,
      image: ''
    },
    color: {
      enabled: true,
      mode: 'schedule',
      dayTime: '07:00',
      nightTime: '18:00',
      original: true,
      day: 5300,
      night: 4500,
      transition: 30,
      participating: {
        q: true,
        d: false
      }
    }
  });
  const positive = (v, min, max) => Number.isInteger(Number(v)) && v !== '' && Number(v) >= min && Number(v) <= max;
  const seconds = (v, u) => Number(v) * (u === 'h' ? 3600 : u === 'm' ? 60 : 1);
  const timeMinutes = s => {
    const [h, m] = s.split(':').map(Number);
    return h * 60 + m;
  };
  const fmt = s => `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(Math.floor(s % 60)).padStart(2, '0')}`;
  function App() {
    const [lang, setLang] = useState('zh'),
      t = (zh, en) => lang === 'zh' ? zh : en;
    const [theme, setTheme] = useState(() => {
      try {
        return localStorage.getItem(themeKey) === 'fog' ? 'fog' : 'forest';
      } catch {
        return 'forest';
      }
    });
    const [module, setModule] = useState('color'),
      [tabs, setTabs] = useState({
        screens: 'rest',
        health: 'rhythm',
        color: 'schedule'
      });
    const [draft, setDraft] = useState(initial),
      [saved, setSaved] = useState(initial);
    const [selected, setSelected] = useState('q'),
      [size, setSize] = useState('standard'),
      [expanded, setExpanded] = useState(false),
      [hidden, setHidden] = useState(false);
    const [colorModel, setColorModel] = useState(() => window.SKColor.initial('normal'));
    const [scenario, setScenario] = useState('normal'),
      [states, setStates] = useState({
        q: 'ready',
        d: 'unverified'
      }),
      [pauses, setPauses] = useState({});
    const [healthPaused, setHealthPaused] = useState(false),
      [healthRest, setHealthRest] = useState(false),
      [healthClock, setHealthClock] = useState(1938);
    const [privacyPauses, setPrivacyPauses] = useState({}),
      [trialTarget, setTrialTarget] = useState(''),
      [trial, setTrial] = useState(null),
      [compare, setCompare] = useState(false),
      [trialEnded, setTrialEnded] = useState(false);
    const [phase, setPhase] = useState('reminder'),
      [modal, setModal] = useState(null),
      [preview, setPreview] = useState(null),
      [toast, setToast] = useState(null),
      [saveError, setSaveError] = useState('');
    const [startup, setStartup] = useState(true),
      [now, setNow] = useState(performance.now());
    const editor = useRef(null),
      toastTimer = useRef(null),
      fileRef = useRef(null),
      previewRoot = useRef(null);
    const screen = draft.screens.find(x => x.id === selected) || draft.screens[0],
      savedScreen = saved.screens.find(x => x.id === selected) || saved.screens[0];
    const dirty = {
      screens: JSON.stringify(draft.screens) !== JSON.stringify(saved.screens),
      health: JSON.stringify(draft.health) !== JSON.stringify(saved.health),
      color: JSON.stringify(colorModel.draft) !== JSON.stringify(colorModel.saved)
    };
    const patch = (area, change) => {
      setDraft(prev => ({
        ...prev,
        [area]: {
          ...prev[area],
          ...change
        }
      }));
      setSaveError('');
    };
    const patchScreen = change => {
      setDraft(prev => ({
        ...prev,
        screens: prev.screens.map(x => x.id === selected ? {
          ...x,
          ...change
        } : x)
      }));
      setSaveError('');
    };
    const notify = (zh, en, error = false) => {
      clearTimeout(toastTimer.current);
      setToast({
        zh,
        en,
        error
      });
      toastTimer.current = setTimeout(() => setToast(null), 3800);
    };
    const stopTrial = (message = false) => {
      setTrial(null);
      setCompare(false);
      setTrialEnded(message);
    };
    const navigate = (m, tab) => {
      stopTrial();
      setModule(m);
      setSaveError('');
      if (tab) setTabs(prev => ({
        ...prev,
        [m]: tab
      }));
    };
    const changeTab = tab => {
      stopTrial();
      setTabs(prev => ({
        ...prev,
        [module]: tab
      }));
    };
    const closeModal = () => {
      if (modal?.step === 'running') notify('模拟验证已取消，已恢复原色。', 'Simulated test cancelled. Original color restored.');
      setModal(null);
    };
    useEffect(() => {
      document.documentElement.lang = lang === 'zh' ? 'zh-CN' : 'en';
    }, [lang]);
    // Browser-only prototype preference. No ScreenKeeper application settings are read or written.
    useEffect(() => {
      try {
        localStorage.setItem(themeKey, theme);
      } catch {/* Theme still works for this preview session. */}
    }, [theme]);
    useEffect(() => {
      if (editor.current) editor.current.scrollTop = 0;
    }, [module, tabs[module], selected]);
    useEffect(() => {
      const id = setInterval(() => setNow(performance.now()), 250);
      return () => clearInterval(id);
    }, []);
    useEffect(() => {
      if (trial && now - trial.start >= 15000) stopTrial(true);
      if (modal?.step === 'running' && now - modal.start >= 15000) setModal(prev => ({
        ...prev,
        step: 'observe',
        checks: [false, false, false]
      }));
    }, [now, trial, modal]);
    useEffect(() => {
      if (!saved.health.enabled || healthPaused) return;
      const id = setInterval(() => setHealthClock(v => v > 0 ? v - 1 : 0), 1000);
      return () => clearInterval(id);
    }, [saved.health.enabled, healthPaused]);
    useEffect(() => {
      if (healthClock === 0 && healthRest) {
        setHealthRest(false);
        setHealthClock(saved.health.work * 60);
      }
    }, [healthClock, healthRest, saved.health.work]);
    useEffect(() => {
      const key = e => {
        if (e.key === 'Escape' && preview) {
          setPreview(null);
        }
      };
      window.addEventListener('keydown', key);
      return () => window.removeEventListener('keydown', key);
    }, [preview]);
    useEffect(() => {
      const blur = () => setCompare(false);
      window.addEventListener('blur', blur);
      return () => window.removeEventListener('blur', blur);
    }, []);
    useEffect(() => {
      if (!preview) return;
      const before = document.activeElement,
        frame = previewRoot.current;
      const items = () => Array.from(frame.querySelectorAll('button:not(:disabled)')).filter(el => el.getClientRects().length);
      items()[0]?.focus();
      const trap = e => {
        if (e.key !== 'Tab') return;
        const list = items();
        if (e.shiftKey && document.activeElement === list[0]) {
          e.preventDefault();
          list[list.length - 1]?.focus();
        } else if (!e.shiftKey && document.activeElement === list[list.length - 1]) {
          e.preventDefault();
          list[0]?.focus();
        }
      };
      frame.addEventListener('keydown', trap);
      return () => {
        frame.removeEventListener('keydown', trap);
        if (before?.isConnected) before.focus();
      };
    }, [!!preview]);
    const unitName = u => u === 's' ? t('秒', 'sec') : u === 'm' ? t('分钟', 'min') : t('小时', 'hr');
    const stateName = id => pauses[id] ? t('临时原色', 'Original temporarily') : states[id] === 'ready' ? t('可用', 'Ready') : states[id] === 'issue' ? t('需要处理', 'Needs attention') : states[id] === 'unsupported' ? t('当前模式不支持', 'Mode not supported') : states[id] === 'disconnected' ? t('已断开', 'Disconnected') : t('待验证', 'Not verified');
    const hasConflict = Object.values(states).includes('issue');
    const canTrial = trialTarget && states[trialTarget] === 'ready' && !pauses[trialTarget] && !hasConflict;
    const startTrial = (period, value) => {
      if (canTrial) {
        setTrial({
          target: trialTarget,
          value,
          label: period,
          start: performance.now()
        });
        setTrialEnded(false);
      } else stopTrial();
    };
    const setKelvin = (period, value) => {
      patch('color', {
        [period]: value
      });
      if (positive(value, 3500, 6500)) startTrial(period, value);
    };
    function validate() {
      if (module === 'screens') {
        for (const s of draft.screens) {
          if (s.managed && s.behavior === 'none' && !s.privacy) return t(`${s.name}：请启用屏幕休息或隐私保护。`, `${s.name}: enable screen rest or privacy protection.`);
          if (s.behavior !== 'none' && (!positive(s.idle, 1, 86400) || seconds(s.idle, s.idleUnit) > 86400)) return t('空闲时长须为正整数，且不超过 24 小时。', 'Idle time must be a positive whole number, up to 24 hours.');
          if (s.privacy && (!positive(s.privacyIdle, 1, 86400) || seconds(s.privacyIdle, s.privacyUnit) > 86400)) return t('隐私等待时间须为正整数，且不超过 24 小时。', 'Privacy delay must be a positive whole number, up to 24 hours.');
          if (s.privacy && s.privacyMode === 'app' && !s.application) return t('请先选择用于保护屏幕的应用。', 'Choose a privacy application first.');
          if (s.privacy && (!positive(s.snooze, 1, 1440) || seconds(s.snooze, s.snoozeUnit) > 86400)) return t('暂缓时长须为正整数，且不超过 24 小时。', 'Snooze must be a positive whole number, up to 24 hours.');
          if (s.privacyMode === 'curtain' && s.images.length > 1 && !positive(s.carousel, 5, 3600)) return t('图片轮播间隔应为 5–3600 秒。', 'Slideshow interval must be 5–3600 seconds.');
          if (s.behavior !== 'none' && !s.wake.some(Boolean)) return t('请至少选择一个保持唤醒的条件。', 'Select at least one keep-awake condition.');
        }
      }
      if (module === 'health') {
        const h = draft.health;
        if (!positive(h.work, 5, 180) || !positive(h.rest, 1, 900) || seconds(h.rest, h.restUnit) < 20 || seconds(h.rest, h.restUnit) > 900 || !positive(h.snooze, 1, 30)) return t('请检查时长：工作 5–180 分钟，休息 20 秒–15 分钟，稍后 1–30 分钟。', 'Check durations: work 5–180 min, rest 20 sec–15 min, snooze 1–30 min.');
      }
      if (module === 'color') {
        const c = draft.color;
        if (!positive(c.day, 3500, 6500) || !positive(c.night, 3500, 6500)) return t('色温数值应为 3500–6500 K。', 'Temperature must be 3500–6500 K.');
        if (c.mode === 'schedule') {
          if (!c.dayTime || !c.nightTime || c.dayTime === c.nightTime) return t('请设置两个不同的作息开始时间。', 'Set two different start times.');
          const delta = (timeMinutes(c.nightTime) - timeMinutes(c.dayTime) + 1440) % 1440;
          if (!positive(c.transition, 0, 120) || c.transition >= Math.min(delta, 1440 - delta)) return t('过渡须为 0–120 分钟，并短于相邻时段。', 'Transition must be 0–120 min and shorter than either period.');
        }
        if (c.enabled && hasConflict) return t('请先处理屏幕颜色冲突，再开启调节。', 'Resolve the display color conflict before enabling adjustment.');
      }
      return '';
    }
    function save() {
      stopTrial();
      const error = validate();
      if (error) {
        setSaveError(error);
        return;
      }
      if (scenario === 'save-fail') {
        setSaveError(t('未能保存，所有修改仍保留。请重试。', 'Could not save. Your changes are kept. Please retry.'));
        return;
      }
      setSaved(prev => ({
        ...prev,
        [module]: clone(draft[module])
      }));
      setSaveError('');
      notify('设置已保存（演示）。', 'Settings saved (demo).');
    }
    function discard() {
      stopTrial();
      setDraft(prev => ({
        ...prev,
        [module]: clone(saved[module])
      }));
      setSaveError('');
      notify('已放弃本模块的修改。', 'Changes in this module discarded.');
    }
    function setScenarioState(value) {
      setColorModel(window.SKColor.initial(value));
      // Review controls replace only the in-memory fictional scenario.
      const base = initial();
      stopTrial();
      setModal(null);
      setPreview(null);
      setSaveError('');
      setPrivacyPauses({});
      setPauses({});
      setTrialTarget('');
      setHealthPaused(false);
      setHealthRest(false);
      setHealthClock(1938);
      if (value === 'unverified') {
        base.color.enabled = false;
        base.color.participating = {
          q: false,
          d: false
        };
        setStates({
          q: 'unverified',
          d: 'unverified'
        });
        navigate('color', 'devices');
      } else if (value === 'conflict') {
        base.color.enabled = false;
        setStates({
          q: 'issue',
          d: 'issue'
        });
        navigate('color', 'devices');
      } else if (value === 'unsupported') {
        setStates({
          q: 'ready',
          d: 'unsupported'
        });
        navigate('color', 'devices');
      } else if (value === 'disconnected') {
        setStates({
          q: 'ready',
          d: 'disconnected'
        });
        navigate('color', 'devices');
      } else if (value === 'empty') {
        base.screens = [];
        setStates({});
        navigate('screens', 'rest');
      } else if (value === 'off') {
        base.health.enabled = false;
        base.color.enabled = false;
        setStates({
          q: 'ready',
          d: 'unverified'
        });
        navigate('health', 'rhythm');
      } else {
        setStates({
          q: 'ready',
          d: 'unverified'
        });
      }
      setDraft(clone(base));
      setSaved(clone(base));
      setSelected('q');
      setScenario(value);
      navigate('color');
    }
    // Keep the scenario setter separate from the human-facing action.
    function setup(id) {
      stopTrial();
      const state = states[id];
      setModal({
        type: 'setup',
        id,
        step: state === 'unsupported' ? 'unsupported' : state === 'disconnected' ? 'disconnected' : saved.color.enabled ? 'policy' : state === 'issue' ? 'issue' : 'ready',
        prepared: false
      });
    }
    function previewHealth() {
      stopTrial();
      setPreview({
        type: 'health',
        phase,
        settings: clone(draft.health)
      });
    }
    function temporaryOriginal(id) {
      setPauses(prev => ({
        ...prev,
        [id]: true
      }));
      if (trialTarget === id) stopTrial();
      notify('此屏幕已暂时恢复原色 1 小时（演示）。', 'This display uses original color for 1 hour (demo).');
    }
    const title = module === 'screens' ? t('屏幕管理', 'Display care') : module === 'health' ? t('健康提醒', 'Healthy breaks') : t('色温调节', 'Color temperature');
    const enabled = module === 'screens' ? screen?.managed : draft[module].enabled;
    const savedEnabled = module === 'screens' ? savedScreen?.managed : saved[module].enabled;
    const setEnabled = v => module === 'screens' ? patchScreen({
      managed: v
    }) : patch(module, {
      enabled: v
    });
    const saveLabel = module === 'screens' ? t('保存屏幕设置', 'Save displays') : module === 'health' ? t('保存健康提醒', 'Save breaks') : t('保存色温设置', 'Save color');
    function renderScreens() {
      if (!screen) return /*#__PURE__*/React.createElement("div", {
        className: "empty"
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "screen",
        size: 36
      }), /*#__PURE__*/React.createElement("h3", null, t('暂未发现屏幕', 'No displays found')), /*#__PURE__*/React.createElement("p", null, t('连接屏幕后，刷新即可继续设置。', 'Connect a display and refresh to continue.')), /*#__PURE__*/React.createElement(Button, {
        icon: "refresh",
        onClick: () => setScenarioState('normal')
      }, t('刷新显示器', 'Refresh displays')));
      return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "runtime-plain"
      }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("span", {
        className: "state-dot"
      }), savedScreen.managed ? t('此屏幕已开启管理', 'Display care is on') : t('此屏幕未开启管理', 'Display care is off')), /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        icon: "copy",
        onClick: () => setModal({
          type: 'copy',
          targets: [],
          scope: 'all'
        })
      }, t('复制本屏设置…', 'Copy these settings…'))), !screen.managed ? /*#__PURE__*/React.createElement("div", {
        className: "empty"
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "screen",
        size: 30
      }), /*#__PURE__*/React.createElement("h3", null, t('此屏幕暂不参与管理', 'Care is disabled for this display')), /*#__PURE__*/React.createElement("p", null, t('已有设置为你保留，开启后可继续调整。', 'Your settings are kept for when you turn care back on.'))) : tabs.screens === 'rest' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("section", {
        className: "section"
      }, /*#__PURE__*/React.createElement("h3", {
        className: "section-title"
      }, t('空闲时，让屏幕', 'When this display is idle')), /*#__PURE__*/React.createElement(Choices, {
        label: t('屏幕休息方式', 'Rest behavior'),
        value: screen.behavior,
        onChange: v => patchScreen({
          behavior: v
        }),
        items: [['none', t('保持亮着', 'Stay on'), t('不自动休息', 'No automatic rest'), 'sun'], ['black', t('遮黑', 'Black out'), t('覆盖黑色画面', 'Cover the display'), 'moon'], ['dim', t('调暗', 'Dim'), t('降低屏幕亮度', 'Lower brightness'), 'screen']]
      }), screen.behavior !== 'none' && /*#__PURE__*/React.createElement(Setting, {
        label: t('本屏空闲多久后休息', 'Rest after inactivity'),
        sub: t('只影响当前选择的屏幕', 'Applies to this display only')
      }, /*#__PURE__*/React.createElement(Unit, {
        value: screen.idle,
        unit: screen.idleUnit,
        units: ['s', 'm', 'h'],
        onValue: v => patchScreen({
          idle: v
        }),
        onUnit: v => patchScreen({
          idleUnit: v
        }),
        t: t,
        label: t('屏幕空闲时长', 'Display idle duration')
      })), screen.behavior === 'dim' && /*#__PURE__*/React.createElement("div", {
        className: "range-control"
      }, /*#__PURE__*/React.createElement("label", null, t('目标亮度', 'Target brightness')), /*#__PURE__*/React.createElement("div", {
        className: "range-line"
      }, /*#__PURE__*/React.createElement("input", {
        "aria-label": t('目标亮度', 'Target brightness'),
        type: "range",
        min: "0",
        max: "100",
        value: screen.brightness,
        onChange: e => patchScreen({
          brightness: Number(e.target.value)
        })
      }), /*#__PURE__*/React.createElement("output", null, screen.brightness, "%")))), screen.behavior !== 'none' && /*#__PURE__*/React.createElement("section", {
        className: "section"
      }, /*#__PURE__*/React.createElement("h3", {
        className: "section-title"
      }, t('这些时候，保持唤醒', 'Keep awake when')), /*#__PURE__*/React.createElement("div", {
        className: "stack"
      }, [t('鼠标位于此屏幕', 'The pointer is on this display'), t('当前活动窗口位于此屏幕', 'The active window is on this display'), t('任意屏幕有键盘或鼠标输入', 'There is keyboard or mouse input on any display')].map((label, i) => /*#__PURE__*/React.createElement("label", {
        className: "checkline",
        key: i
      }, /*#__PURE__*/React.createElement("input", {
        type: "checkbox",
        checked: screen.wake[i],
        onChange: e => patchScreen({
          wake: screen.wake.map((v, j) => i === j ? e.target.checked : v)
        })
      }), label))), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('满足其中任一条件，就不会进入屏幕休息。', 'Any selected condition keeps this display awake.')))) : renderPrivacy());
    }
    function renderPrivacy() {
      return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Setting, {
        label: t('启用隐私保护', 'Enable privacy protection'),
        sub: t('离开这块屏幕时，收起不想被看见的内容。', 'Keep private content out of view while you are away.')
      }, /*#__PURE__*/React.createElement(Toggle, {
        checked: screen.privacy,
        onChange: v => patchScreen({
          privacy: v
        }),
        label: t('启用隐私保护', 'Enable privacy protection')
      })), screen.privacy ? /*#__PURE__*/React.createElement(React.Fragment, null, savedScreen.privacy && /*#__PURE__*/React.createElement("div", {
        className: "runtime-plain",
        style: {
          marginTop: 10
        }
      }, /*#__PURE__*/React.createElement("span", null, privacyPauses[selected] ? t('本屏保护已暂缓', 'Protection snoozed on this display') : t('正在按已保存规则保护', 'Using the saved privacy rule')), /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        onClick: () => privacyPauses[selected] ? setPrivacyPauses(p => ({
          ...p,
          [selected]: false
        })) : setModal({
          type: 'snooze',
          value: savedScreen.snooze,
          unit: savedScreen.snoozeUnit
        })
      }, privacyPauses[selected] ? t('恢复保护', 'Resume') : t('暂缓…', 'Snooze…'))), /*#__PURE__*/React.createElement(Setting, {
        label: t('本屏无操作多久后保护', 'Protect after inactivity')
      }, /*#__PURE__*/React.createElement(Unit, {
        value: screen.privacyIdle,
        unit: screen.privacyUnit,
        units: ['s', 'm', 'h'],
        onValue: v => patchScreen({
          privacyIdle: v
        }),
        onUnit: v => patchScreen({
          privacyUnit: v
        }),
        t: t,
        label: t('隐私等待时长', 'Privacy delay')
      })), /*#__PURE__*/React.createElement("section", {
        className: "section"
      }, /*#__PURE__*/React.createElement("h3", {
        className: "section-title"
      }, t('保护方式', 'Protection method')), /*#__PURE__*/React.createElement(Choices, {
        label: t('保护方式', 'Protection method'),
        value: screen.privacyMode,
        onChange: v => patchScreen({
          privacyMode: v
        }),
        items: [['curtain', t('隐私幕', 'Curtain'), null, 'shield'], ['desktop', t('回到桌面', 'Desktop'), null, 'desktop'], ['app', t('指定应用', 'Application'), null, 'app']]
      }), /*#__PURE__*/React.createElement("div", {
        className: "mode-detail"
      }, screen.privacyMode === 'curtain' ? /*#__PURE__*/React.createElement("div", {
        className: "detail-layout"
      }, /*#__PURE__*/React.createElement("div", {
        className: "mini-curtain",
        style: {
          backgroundImage: curtainStyle(curtainImage(screen))
        }
      }, /*#__PURE__*/React.createElement("button", {
        type: "button",
        className: "btn",
        onClick: () => setPreview({
          type: 'curtain',
          image: curtainImage(screen)
        })
      }, t('继续使用', 'Continue'))), /*#__PURE__*/React.createElement("div", {
        className: "detail-text"
      }, /*#__PURE__*/React.createElement("h3", null, t('安静遮住，安心离开', 'A quiet cover for your screen')), /*#__PURE__*/React.createElement("p", null, screen.background === 'custom' ? t(`已选择 ${screen.images.length} 张图片`, `${screen.images.length} image(s) selected`) : screen.background === 'solid' ? t('纯色背景', 'Solid background') : t('默认山林 · 与健康提醒共用', 'Forest mist · shared with health reminders')), /*#__PURE__*/React.createElement(Button, {
        small: true,
        icon: "image",
        onClick: () => setModal({
          type: 'images',
          images: [...screen.images],
          background: screen.background,
          fit: screen.fit,
          carousel: screen.carousel
        })
      }, t('设置外观…', 'Customize…')))) : screen.privacyMode === 'desktop' ? /*#__PURE__*/React.createElement("div", {
        className: "detail-text"
      }, /*#__PURE__*/React.createElement("h3", null, t('把这块屏幕留在桌面', 'Leave this display on the desktop')), /*#__PURE__*/React.createElement("p", null, t('先遮挡，再最小化本屏窗口。无法安全隐藏时保留隐私幕，唤醒后不自动还原窗口。', 'Cover first, then minimize this display’s windows. Keep the curtain if hiding fails; windows are not restored on wake.')), /*#__PURE__*/React.createElement(Button, {
        small: true,
        icon: "play",
        onClick: () => setModal({
          type: 'privacy-test',
          mode: 'desktop'
        })
      }, t('测试效果', 'Try the effect'))) : /*#__PURE__*/React.createElement("div", {
        className: "stack"
      }, /*#__PURE__*/React.createElement("div", {
        className: "app-target"
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "app",
        size: 26
      }), /*#__PURE__*/React.createElement("div", {
        className: "target-name"
      }, /*#__PURE__*/React.createElement("strong", null, screen.application || t('还没有选择应用', 'No application selected')), /*#__PURE__*/React.createElement("small", null, t('用于遮住本屏的私人内容', 'Show this application on this display'))), /*#__PURE__*/React.createElement(Button, {
        small: true,
        onClick: () => setModal({
          type: 'apps',
          app: screen.application
        })
      }, screen.application ? t('更换', 'Change') : t('选择', 'Choose'))), screen.application && /*#__PURE__*/React.createElement(Button, {
        small: true,
        kind: "link",
        icon: "play",
        onClick: () => setModal({
          type: 'privacy-test',
          mode: 'app'
        })
      }, t('测试切换效果', 'Try switching')), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('应用无法显示时，保留隐私幕。', 'Keep the curtain if the application cannot be shown.'))))), /*#__PURE__*/React.createElement("details", {
        className: "advanced"
      }, /*#__PURE__*/React.createElement("summary", null, t('更多设置', 'More settings')), /*#__PURE__*/React.createElement(Setting, {
        label: t('默认暂缓时长', 'Default snooze')
      }, /*#__PURE__*/React.createElement(Unit, {
        value: screen.snooze,
        unit: screen.snoozeUnit,
        units: ['m', 'h'],
        onValue: v => patchScreen({
          snooze: v
        }),
        onUnit: v => patchScreen({
          snoozeUnit: v
        }),
        t: t,
        label: t('默认暂缓时长', 'Default snooze')
      })), screen.privacyMode === 'app' && /*#__PURE__*/React.createElement("div", {
        className: "stack"
      }, /*#__PURE__*/React.createElement("label", {
        className: "checkline"
      }, /*#__PURE__*/React.createElement("input", {
        type: "checkbox",
        checked: screen.launch,
        onChange: e => patchScreen({
          launch: e.target.checked
        })
      }), t('必要时允许启动应用', 'Allow launching the app when needed')), /*#__PURE__*/React.createElement("label", {
        className: "checkline"
      }, /*#__PURE__*/React.createElement("input", {
        type: "checkbox",
        checked: screen.move,
        onChange: e => patchScreen({
          move: e.target.checked
        })
      }), t('允许把应用窗口移到本屏', 'Allow moving the app to this display')))), /*#__PURE__*/React.createElement("div", {
        className: "section"
      }, /*#__PURE__*/React.createElement(Button, {
        kind: "quiet",
        small: true,
        icon: "shield",
        disabled: !saved.screens.some(s => s.managed && s.privacy),
        onClick: () => setPreview({
          type: 'curtain'
        })
      }, t('立即保护所有屏幕', 'Protect all displays now')), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('使用所有屏幕已保存并启用的隐私规则。', 'Uses the saved, enabled privacy rules for all displays.')))) : /*#__PURE__*/React.createElement("div", {
        className: "empty",
        style: {
          marginTop: 18
        }
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "shield",
        size: 28
      }), /*#__PURE__*/React.createElement("h3", null, t('隐私保护已关闭', 'Privacy protection is off')), /*#__PURE__*/React.createElement("p", null, t('原来的方式和图片仍会保留。', 'Your previous method and images are kept.'))));
    }
    function renderHealth() {
      const h = draft.health;
      return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: `runtime ${tabs.health === 'appearance' ? 'condensed' : ''}`
      }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
        className: "timer-caption"
      }, !saved.health.enabled ? t('当前未开启', 'Currently off') : healthPaused ? t('已暂停提醒', 'Reminders paused') : healthRest ? t('休息剩余', 'Break remaining') : t('距离下次提醒', 'Until your next reminder')), /*#__PURE__*/React.createElement("div", {
        className: "timer"
      }, !saved.health.enabled ? '— —' : fmt(healthClock))), /*#__PURE__*/React.createElement("div", {
        className: "runtime-actions"
      }, /*#__PURE__*/React.createElement(Button, {
        small: true,
        icon: "play",
        disabled: !saved.health.enabled,
        onClick: () => {
          setHealthRest(true);
          setHealthPaused(false);
          setHealthClock(seconds(saved.health.rest, saved.health.restUnit));
          setPreview({
            type: 'health',
            phase: 'rest',
            live: true,
            settings: clone(saved.health)
          });
        }
      }, t('现在休息', 'Take a break')), /*#__PURE__*/React.createElement(Button, {
        small: true,
        kind: "quiet",
        icon: healthPaused ? 'play' : 'pause',
        disabled: !saved.health.enabled,
        onClick: () => setHealthPaused(!healthPaused)
      }, healthPaused ? t('继续', 'Resume') : t('暂停', 'Pause')))), !h.enabled ? /*#__PURE__*/React.createElement("div", {
        className: "empty"
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "clock",
        size: 30
      }), /*#__PURE__*/React.createElement("h3", null, t('给专注留一点休息的空间', 'Make room for a little rest')), /*#__PURE__*/React.createElement("p", null, t('开启后安排工作节奏，已有偏好为你保留。', 'Turn on reminders to set a rhythm. Your preferences are kept.'))) : tabs.health === 'rhythm' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("section", {
        className: "section"
      }, /*#__PURE__*/React.createElement("h3", {
        className: "section-title"
      }, t('舒服的节奏，由你决定', 'Find a rhythm that suits you')), /*#__PURE__*/React.createElement("div", {
        className: "rhythm-grid"
      }, /*#__PURE__*/React.createElement("div", {
        className: "rhythm-cell"
      }, /*#__PURE__*/React.createElement("div", {
        className: "rhythm-label"
      }, t('连续工作', 'Work for')), /*#__PURE__*/React.createElement(Unit, {
        value: h.work,
        unit: t('分钟', 'min'),
        onValue: v => patch('health', {
          work: v
        }),
        t: t,
        label: t('连续工作时长', 'Work duration'),
        min: 5,
        max: 180
      })), /*#__PURE__*/React.createElement("div", {
        className: "rhythm-cell"
      }, /*#__PURE__*/React.createElement("div", {
        className: "rhythm-label"
      }, t('每次休息', 'Rest for')), /*#__PURE__*/React.createElement(Unit, {
        value: h.rest,
        unit: h.restUnit,
        units: ['s', 'm'],
        onValue: v => patch('health', {
          rest: v
        }),
        onUnit: v => patch('health', {
          restUnit: v
        }),
        t: t,
        label: t('休息时长', 'Break duration')
      })), /*#__PURE__*/React.createElement("div", {
        className: "rhythm-cell"
      }, /*#__PURE__*/React.createElement("div", {
        className: "rhythm-label"
      }, t('稍后提醒', 'Snooze for')), /*#__PURE__*/React.createElement(Unit, {
        value: h.snooze,
        unit: t('分钟', 'min'),
        onValue: v => patch('health', {
          snooze: v
        }),
        t: t,
        label: t('稍后提醒时长', 'Reminder snooze'),
        max: 30
      }))), dirty.health && /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('保存后，新时长从下一周期使用。', 'After saving, new durations apply to the next cycle.'))), /*#__PURE__*/React.createElement("section", {
        className: "section"
      }, /*#__PURE__*/React.createElement(Setting, {
        label: t('全屏时不打扰', 'Do not disturb in fullscreen'),
        sub: t('退出全屏后，再择机提醒。', 'Reminders wait until fullscreen ends.')
      }, /*#__PURE__*/React.createElement(Toggle, {
        checked: h.dnd,
        onChange: v => patch('health', {
          dnd: v
        }),
        label: t('全屏免打扰', 'Fullscreen do not disturb')
      }))), /*#__PURE__*/React.createElement("details", {
        className: "advanced"
      }, /*#__PURE__*/React.createElement("summary", null, t('计时与暂停', 'Timing and pauses')), /*#__PURE__*/React.createElement(Setting, {
        label: t('无操作 5 分钟后暂停累计', 'Pause after 5 minutes without input'),
        sub: t('阅读或观看内容时，也可能被视为无操作。', 'Reading or watching may also count as inactivity.')
      }, /*#__PURE__*/React.createElement(Toggle, {
        checked: h.idlePause,
        onChange: v => patch('health', {
          idlePause: v
        }),
        label: t('无操作暂停', 'Pause on inactivity')
      })), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('只有点击“开始休息”后才开始休息倒计时。锁屏和系统休眠会暂停工作累计。', 'The break timer starts only when you choose to rest. Locking or sleeping pauses work time.')))) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "row between",
        style: {
          marginBottom: 20
        }
      }, /*#__PURE__*/React.createElement("div", {
        className: "segmented",
        "aria-label": t('提醒阶段', 'Reminder stage')
      }, /*#__PURE__*/React.createElement("button", {
        type: "button",
        className: phase === 'reminder' ? 'active' : '',
        onClick: () => setPhase('reminder')
      }, t('提醒时', 'At reminder')), /*#__PURE__*/React.createElement("button", {
        type: "button",
        className: phase === 'rest' ? 'active' : '',
        onClick: () => setPhase('rest')
      }, t('开始休息后', 'During a break')))), /*#__PURE__*/React.createElement("div", {
        className: "appearance-grid"
      }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
        className: "phase-label"
      }, phase === 'reminder' ? t('用什么方式轻轻提醒你', 'How would you like to be reminded?') : t('把屏幕留给安静的画面', 'Make space for a quiet scene')), /*#__PURE__*/React.createElement("select", {
        className: "input",
        style: {
          width: '100%'
        },
        "aria-label": t('提醒外观', 'Reminder appearance'),
        value: phase === 'reminder' ? h.reminder : h.restStyle,
        onChange: e => patch('health', {
          [phase === 'reminder' ? 'reminder' : 'restStyle']: e.target.value
        })
      }, (phase === 'reminder' ? [['fog', t('雾光漫入', 'Drifting mist')], ['scenic', t('静景渐显', 'Quiet landscape')], ['compact', t('简洁卡片', 'Compact card')]] : [['night', t('静夜微动', 'Quiet night')], ['picture', t('背景图片', 'Background picture')]]).map(([v, label]) => /*#__PURE__*/React.createElement("option", {
        key: v,
        value: v
      }, label))), phase === 'reminder' && h.reminder !== 'compact' && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "range-control"
      }, /*#__PURE__*/React.createElement("label", {
        htmlFor: "fade"
      }, t('慢慢浮现', 'Fade in')), /*#__PURE__*/React.createElement("div", {
        className: "range-line"
      }, /*#__PURE__*/React.createElement("input", {
        id: "fade",
        type: "range",
        min: "2",
        max: "20",
        value: h.fade,
        onChange: e => patch('health', {
          fade: Number(e.target.value)
        })
      }), /*#__PURE__*/React.createElement("output", null, h.fade, " ", t('秒', 's')))), /*#__PURE__*/React.createElement("div", {
        className: "range-control"
      }, /*#__PURE__*/React.createElement("label", {
        htmlFor: "coverage"
      }, t('覆盖程度', 'Coverage')), /*#__PURE__*/React.createElement("div", {
        className: "range-line"
      }, /*#__PURE__*/React.createElement("input", {
        id: "coverage",
        type: "range",
        min: "10",
        max: "100",
        value: h.coverage,
        onChange: e => patch('health', {
          coverage: Number(e.target.value)
        })
      }), /*#__PURE__*/React.createElement("output", null, h.coverage, "%")))), (phase === 'rest' || h.reminder !== 'compact') && /*#__PURE__*/React.createElement(Setting, {
        label: t('让画面缓缓流动', 'Gentle motion')
      }, /*#__PURE__*/React.createElement(Toggle, {
        label: t('启用动态', 'Enable motion'),
        checked: h.moving,
        onChange: v => patch('health', {
          moving: v
        })
      })), (phase === 'rest' || h.reminder !== 'compact') && h.moving && /*#__PURE__*/React.createElement("div", {
        className: "range-control"
      }, /*#__PURE__*/React.createElement("label", {
        htmlFor: "intensity"
      }, t('动态强度', 'Motion intensity')), /*#__PURE__*/React.createElement("div", {
        className: "range-line"
      }, /*#__PURE__*/React.createElement("input", {
        id: "intensity",
        type: "range",
        min: "1",
        max: "2",
        step: "0.1",
        value: h.intensity,
        onChange: e => patch('health', {
          intensity: Number(e.target.value)
        })
      }), /*#__PURE__*/React.createElement("output", null, Number(h.intensity).toFixed(1), "\xD7"))), (phase === 'rest' && h.restStyle === 'picture' || phase === 'reminder' && h.reminder === 'scenic') && /*#__PURE__*/React.createElement("div", {
        className: "section"
      }, /*#__PURE__*/React.createElement(Button, {
        small: true,
        icon: "image",
        onClick: () => setModal({
          type: 'health-image'
        })
      }, h.image ? t('更换图片…', 'Change picture…') : t('选择图片…', 'Choose picture…')), h.image && /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        small: true,
        onClick: () => patch('health', {
          image: ''
        })
      }, t('恢复默认', 'Use default')))), /*#__PURE__*/React.createElement("div", {
        className: "preview-pane"
      }, /*#__PURE__*/React.createElement("div", {
        className: "preview-caption"
      }, /*#__PURE__*/React.createElement("span", null, t('效果示意', 'APPEARANCE PREVIEW')), /*#__PURE__*/React.createElement("span", null, phase === 'rest' ? t('休息画面', 'Break scene') : t('提醒画面', 'Reminder scene'))), /*#__PURE__*/React.createElement(Scene, {
        style: phase === 'reminder' ? h.reminder === 'scenic' && h.image ? 'picture' : h.reminder : h.restStyle,
        image: h.image || 'forest-mist.png',
        moving: h.moving,
        rest: phase === 'rest',
        coverage: h.coverage,
        intensity: h.intensity,
        time: fmt(seconds(h.rest, h.restUnit)),
        t: t
      }), /*#__PURE__*/React.createElement(Button, {
        icon: "play",
        onClick: previewHealth
      }, t('在屏幕上预览', 'Preview on screen')), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('预览不会改变当前计时。', 'Preview does not change your current timer.'))))));
    }
    function renderPeriod(period) {
      const c = draft.color,
        isDay = period === 'day';
      return /*#__PURE__*/React.createElement("section", {
        className: "period"
      }, /*#__PURE__*/React.createElement("div", {
        className: "period-head"
      }, /*#__PURE__*/React.createElement("div", {
        className: "period-name"
      }, /*#__PURE__*/React.createElement(Icon, {
        name: isDay ? 'sun' : 'moon',
        size: 20
      }), isDay ? t('工作时段', 'Work hours') : t('休息时段', 'Evening hours')), c.mode === 'schedule' && /*#__PURE__*/React.createElement("label", {
        className: "period-time"
      }, t('开始于', 'Starts at'), /*#__PURE__*/React.createElement("input", {
        className: "input",
        type: "time",
        "aria-label": isDay ? t('工作开始时间', 'Work start time') : t('休息开始时间', 'Evening start time'),
        value: c[isDay ? 'dayTime' : 'nightTime'],
        onChange: e => patch('color', {
          [isDay ? 'dayTime' : 'nightTime']: e.target.value
        })
      }))), isDay && /*#__PURE__*/React.createElement("div", {
        className: "period-check"
      }, /*#__PURE__*/React.createElement("label", {
        className: "checkline"
      }, /*#__PURE__*/React.createElement("input", {
        type: "checkbox",
        checked: c.original,
        onChange: e => {
          patch('color', {
            original: e.target.checked
          });
          stopTrial();
        }
      }), t('保持原色', 'Keep original color'))), isDay && c.original ? /*#__PURE__*/React.createElement("div", {
        className: "original-note"
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "check",
        size: 16
      }), t('使用屏幕接管前的颜色，不额外调节。', 'Use the original display color, without additional adjustment.')) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "temperature-line"
      }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("input", {
        className: "kelvin",
        type: "range",
        min: "3500",
        max: "6500",
        step: "100",
        value: c[period],
        "aria-label": isDay ? t('工作色温滑杆', 'Work temperature slider') : t('休息色温滑杆', 'Evening temperature slider'),
        onChange: e => setKelvin(period, Number(e.target.value))
      }), /*#__PURE__*/React.createElement("div", {
        className: "range-labels"
      }, /*#__PURE__*/React.createElement("span", null, t('更暖', 'Warmer')), /*#__PURE__*/React.createElement("span", null, t('接近中性', 'More neutral')))), /*#__PURE__*/React.createElement("div", {
        className: "unit"
      }, /*#__PURE__*/React.createElement("input", {
        className: "input",
        type: "number",
        min: "3500",
        max: "6500",
        step: "100",
        "aria-label": isDay ? t('工作色温数值', 'Work temperature value') : t('休息色温数值', 'Evening temperature value'),
        value: c[period],
        onChange: e => setKelvin(period, e.target.value === '' ? '' : Number(e.target.value))
      }), /*#__PURE__*/React.createElement("span", null, "K")))));
    }
    function renderColor() {
      const c = draft.color,
        participantCount = Object.values(c.participating).filter(Boolean).length,
        savedParticipants = Object.values(saved.color.participating).filter(Boolean).length,
        validTimes = c.dayTime && c.nightTime && c.dayTime !== c.nightTime;
      return tabs.color === 'schedule' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "runtime-plain"
      }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("span", {
        className: "state-dot"
      }), saved.color.enabled ? savedParticipants ? t('已开启 · 已保存参与屏幕的策略', 'On · Policy saved for participating displays') : t('设置已保存，但没有参与屏幕，尚未自动调色。', 'Settings saved, but no displays are participating. Automatic adjustment has not started.') : t('当前已关闭 · 可先编辑设置', 'Currently off · You can still edit settings')), /*#__PURE__*/React.createElement("select", {
        className: "input",
        "aria-label": t('色温模式', 'Color mode'),
        value: c.mode,
        onChange: e => {
          patch('color', {
            mode: e.target.value
          });
          stopTrial();
        }
      }, /*#__PURE__*/React.createElement("option", {
        value: "schedule"
      }, t('固定作息', 'Fixed schedule')), /*#__PURE__*/React.createElement("option", {
        value: "manual"
      }, t('手动色温', 'Manual')))), hasConflict && /*#__PURE__*/React.createElement(Notice, {
        title: t('有屏幕需要处理，暂不可试调', 'A display needs attention before previewing'),
        action: /*#__PURE__*/React.createElement(Button, {
          kind: "link",
          onClick: () => changeTab('devices')
        }, t('查看屏幕状态', 'View display status'), " ", /*#__PURE__*/React.createElement(Icon, {
          name: "arrow",
          size: 14
        }))
      }, t('先处理颜色记录，再继续试调。已编辑的数值会保留。', 'Resolve the color record issue first. Your edited values are kept.')), /*#__PURE__*/React.createElement("div", {
        className: "color-participation"
      }, /*#__PURE__*/React.createElement(Setting, {
        label: participantCount ? t(`已选择 ${participantCount} 块参与屏幕；修改后需保存。`, `${participantCount} participating display(s) selected. Save to apply changes.`) : t('尚未选择参与屏幕，保存作息后也不会自动调色。', 'No participating displays selected. Saving a schedule alone will not change display colors.')
      }, /*#__PURE__*/React.createElement(Button, {
        small: true,
        kind: "link",
        onClick: () => changeTab('devices')
      }, t('选择参与屏幕…', 'Choose participating displays…'))), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('试调选屏仅用于临时预览；长期调色需单独勾选参与屏幕。未连接、暂停或待验证的屏幕不会立即应用。', 'The preview display is only for temporary trials. Select participating displays separately for ongoing adjustment. Disconnected, paused or unverified displays will not apply it immediately.'))), c.mode === 'schedule' && renderPeriod('day'), renderPeriod('night'), c.mode === 'schedule' && /*#__PURE__*/React.createElement("div", {
        style: {
          paddingTop: 15
        }
      }, /*#__PURE__*/React.createElement(Setting, {
        label: t('两个时段之间，逐渐过渡', 'Transition gradually between periods')
      }, /*#__PURE__*/React.createElement(Unit, {
        value: c.transition,
        unit: t('分钟', 'min'),
        onValue: v => patch('color', {
          transition: v
        }),
        t: t,
        label: t('过渡时长', 'Transition duration'),
        min: 0,
        max: 120
      })), /*#__PURE__*/React.createElement("div", {
        className: "timeline",
        "aria-label": t('一天的作息示意', 'Daily schedule illustration')
      }, /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('当前编辑的作息', 'Schedule being edited')), !validTimes ? /*#__PURE__*/React.createElement("p", {
        className: "error"
      }, t('请输入有效且不同的工作、休息开始时间。', 'Enter valid, different work and rest start times.')) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "timeline-rail"
      }, validTimes && (timeMinutes(c.dayTime) < timeMinutes(c.nightTime) ? /*#__PURE__*/React.createElement("span", {
        className: "timeline-work",
        style: {
          left: `${timeMinutes(c.dayTime) / 14.4}%`,
          width: `${(timeMinutes(c.nightTime) - timeMinutes(c.dayTime)) / 14.4}%`
        }
      }) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("span", {
        className: "timeline-work",
        style: {
          left: 0,
          width: `${timeMinutes(c.nightTime) / 14.4}%`
        }
      }), /*#__PURE__*/React.createElement("span", {
        className: "timeline-work",
        style: {
          left: `${timeMinutes(c.dayTime) / 14.4}%`,
          right: 0
        }
      })))), /*#__PURE__*/React.createElement("div", {
        className: "timeline-labels"
      }, /*#__PURE__*/React.createElement("span", null, "00:00"), /*#__PURE__*/React.createElement("span", null, "12:00"), /*#__PURE__*/React.createElement("span", null, "24:00")), /*#__PURE__*/React.createElement("div", {
        className: "timeline-legend"
      }, /*#__PURE__*/React.createElement("p", null, /*#__PURE__*/React.createElement("i", {
        className: "work-key"
      }), t('工作时段', 'Work period'), " \xB7 ", c.dayTime, " \u2192 ", c.nightTime, " \xB7 ", c.original ? t('原色', 'Original colors') : `${c.day} K`), /*#__PURE__*/React.createElement("p", null, /*#__PURE__*/React.createElement("i", {
        className: "rest-key"
      }), t('休息时段', 'Rest period'), " \xB7 ", c.nightTime, " \u2192 ", c.dayTime, " \xB7 ", c.night, " K"), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('时段交界处按设置逐渐过渡；色块表示时段，不代表实际屏幕颜色。', 'Transitions follow the chosen duration at each boundary. The bands identify periods, not actual display colors.')))))), /*#__PURE__*/React.createElement("div", {
        className: `trial-panel ${trial ? 'active' : ''}`
      }, /*#__PURE__*/React.createElement("div", {
        className: "trial-top"
      }, /*#__PURE__*/React.createElement("label", null, t('试调屏幕', 'Preview display'), " ", /*#__PURE__*/React.createElement("select", {
        className: "input",
        "aria-label": t('试调屏幕', 'Preview display'),
        value: trialTarget,
        onChange: e => {
          stopTrial();
          setTrialTarget(e.target.value);
        }
      }, /*#__PURE__*/React.createElement("option", {
        value: ""
      }, t('选择一块屏幕', 'Choose a display')), draft.screens.map(s => /*#__PURE__*/React.createElement("option", {
        key: s.id,
        value: s.id
      }, s.name, " \xB7 ", stateName(s.id))))), /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        small: true,
        onClick: () => changeTab('devices')
      }, t('验证试调屏幕…', 'Verify a trial display…'))), trial ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "trial-feedback"
      }, /*#__PURE__*/React.createElement("p", null, compare ? t('正在对比原色', 'Comparing original color') : t(`正在试调 ${trial.value} K · ${Math.min(15, Math.max(0, Math.ceil((15000 - (now - trial.start)) / 1000)))} 秒后恢复`, `Previewing ${trial.value} K · restores in ${Math.min(15, Math.max(0, Math.ceil((15000 - (now - trial.start)) / 1000)))}s`)), /*#__PURE__*/React.createElement("div", {
        className: "row"
      }, /*#__PURE__*/React.createElement(Button, {
        small: true,
        onPointerDown: e => {
          e.currentTarget.setPointerCapture(e.pointerId);
          setCompare(true);
        },
        onPointerUp: () => setCompare(false),
        onLostPointerCapture: () => setCompare(false),
        onBlur: () => setCompare(false),
        onKeyDown: e => {
          if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            setCompare(true);
          }
        },
        onKeyUp: e => {
          if (e.key === ' ' || e.key === 'Enter') setCompare(false);
        }
      }, t('按住对比原色', 'Hold to compare')), /*#__PURE__*/React.createElement(Button, {
        small: true,
        kind: "link",
        onClick: () => stopTrial(true)
      }, t('结束试调', 'End preview')))), /*#__PURE__*/React.createElement("div", {
        className: "trial-meter"
      }, /*#__PURE__*/React.createElement("span", {
        style: {
          width: `${Math.max(0, 100 - (now - trial.start) / 150)}%`
        }
      }))) : trialTarget && !canTrial ? /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, pauses[trialTarget] ? t('此屏幕正临时保持原色，可在屏幕管理中恢复自动。', 'This display is temporarily using original color. Resume it in display management.') : t('可以编辑数值；此屏幕暂不可试调，请先查看屏幕状态。', 'Values can be edited, but this display is not ready for preview. Check its status first.')) : /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, trialEnded ? t('试调已结束，恢复已保存策略；修改的数值仍保留。', 'Preview ended. Saved policy restored; your edited values are kept.') : trialTarget ? t('拖动滑杆感受变化；停止调整 15 秒后恢复。', 'Move a slider to preview; the saved policy returns after 15 seconds.') : t('先选择可用屏幕，再拖动滑杆感受变化。', 'Choose a ready display, then move a slider to preview.'))), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('色温按固定作息变化，与健康提醒的休息倒计时独立。', 'Color follows its own schedule, independently of break reminders.'))) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "runtime-plain"
      }, /*#__PURE__*/React.createElement("span", null, t('所有参与屏幕共用一套作息，可分别暂停。', 'Participating displays share a schedule and can be paused separately.'))), draft.screens.length === 0 ? /*#__PURE__*/React.createElement("div", {
        className: "empty"
      }, /*#__PURE__*/React.createElement("h3", null, t('暂未发现屏幕', 'No displays found'))) : draft.screens.map((s, i) => /*#__PURE__*/React.createElement("div", {
        className: "monitor-row",
        key: s.id
      }, /*#__PURE__*/React.createElement("div", {
        className: "monitor-drawing"
      }, i + 1), /*#__PURE__*/React.createElement("div", {
        className: "monitor-info"
      }, /*#__PURE__*/React.createElement("h3", null, s.name, /*#__PURE__*/React.createElement("span", {
        className: `pill ${states[s.id] === 'ready' ? 'good' : 'warn'}`
      }, stateName(s.id))), /*#__PURE__*/React.createElement("p", null, states[s.id] === 'ready' ? pauses[s.id] ? t('临时恢复原色 · 1 小时后继续自动调节', 'Original color · automatic adjustment resumes in 1 hour') : t('已完成测试，可参与自动调节。', 'Test complete. Ready for scheduled adjustment.') : states[s.id] === 'issue' ? t('当前颜色与之前记录不同，需要先确认原色。', 'Current color differs from the saved original. Check it before continuing.') : states[s.id] === 'unsupported' ? t('演示：当前屏幕开启 HDR，请切换为 SDR 后检查。', 'Demo: HDR is on. Switch to SDR before checking again.') : states[s.id] === 'disconnected' ? t('屏幕已断开，设置仍会保留。', 'The display is disconnected. Its settings are kept.') : t('完成一次 15 秒测试，确认变化和恢复正常。', 'Complete a 15-second test to check adjustment and recovery.')), /*#__PURE__*/React.createElement("div", {
        className: "monitor-actions"
      }, states[s.id] === 'ready' ? /*#__PURE__*/React.createElement(React.Fragment, null, pauses[s.id] ? /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        onClick: () => setPauses(p => ({
          ...p,
          [s.id]: false
        }))
      }, t('恢复自动', 'Resume automatic')) : /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        onClick: () => temporaryOriginal(s.id)
      }, t('原色 1 小时', 'Original for 1 hour')), /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        onClick: () => {
          setTrialTarget(s.id);
          changeTab('schedule');
        }
      }, t('在此屏幕试调', 'Preview on this display')), /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        onClick: () => setup(s.id)
      }, t('查看验证', 'View test'))) : /*#__PURE__*/React.createElement(Button, {
        small: true,
        icon: states[s.id] === 'unverified' ? 'play' : 'info',
        onClick: () => setup(s.id)
      }, states[s.id] === 'unverified' ? t('开始验证', 'Start verification') : t('查看处理步骤', 'View next steps')))), /*#__PURE__*/React.createElement("div", {
        className: "monitor-toggle"
      }, /*#__PURE__*/React.createElement("span", null, t('参与', 'Use')), /*#__PURE__*/React.createElement(Toggle, {
        label: t(`${s.name} 参与色温调节`, `Use ${s.name} for color adjustment`),
        checked: !!c.participating[s.id],
        disabled: states[s.id] !== 'ready',
        onChange: v => patch('color', {
          participating: {
            ...c.participating,
            [s.id]: v
          }
        })
      })))), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('新屏幕不会自动参与。完成验证后，可自行开启并保存。', 'New displays are not included automatically. Enable and save after verification.')));
    }
    async function importPictures(files, health = false) {
      const accepted = [];
      for (const file of Array.from(files).slice(0, health ? 1 : 12)) {
        if (file.size > 10 * 1024 * 1024 || !['image/png', 'image/jpeg', 'image/webp'].includes(file.type)) {
          notify('请选择 10 MB 以内的 PNG、JPEG 或 WebP 图片。', 'Choose a PNG, JPEG or WebP image under 10 MB.', true);
          continue;
        }
        try {
          const bitmap = await createImageBitmap(file);
          const pixels = bitmap.width * bitmap.height;
          bitmap.close();
          if (pixels > 40000000) {
            notify('图片不能超过 4000 万像素。', 'Images must be under 40 megapixels.', true);
            continue;
          }
          accepted.push(URL.createObjectURL(file));
        } catch {
          notify('这张图片无法读取，请换一张。', 'This image could not be read. Try another.', true);
        }
      }
      if (health && accepted[0]) {
        patch('health', {
          image: accepted[0]
        });
        setModal(null);
      } else setModal(prev => prev?.type === 'images' ? {
        ...prev,
        background: 'custom',
        images: [...prev.images, ...accepted].slice(0, 12)
      } : prev);
    }
    function renderSetup() {
      const name = draft.screens.find(s => s.id === modal.id)?.name || 'Display';
      const step = modal.step;
      const stepIndex = step === 'running' ? 1 : step === 'observe' ? 2 : step === 'done' ? 3 : 0;
      let body, actions;
      const cancel = /*#__PURE__*/React.createElement(Button, {
        kind: "quiet",
        onClick: closeModal
      }, t('稍后再说', 'Not now'));
      if (step === 'policy') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          title: t('先关闭并保存色温调节', 'Turn off color adjustment and save first')
        }, t('自动策略仍在运行。结束自动调节后，才能单独测试这块屏幕。', 'The saved automatic policy is still running. Turn it off before testing this display.')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('返回“作息与色温”，关闭右上角开关并保存，再回到这里继续。其他设置和草稿不会被清除。', 'Go to Schedule & color, turn off the top-right switch and save, then return here. Your other settings and drafts are kept.')));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, cancel, /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: () => {
            setModal(null);
            navigate('color', 'schedule');
            notify('关闭右上角开关后，点击“保存色温设置”。', 'Turn off the top-right switch, then choose Save color.');
          }
        }, t('去关闭色温调节', 'Go to the switch')));
      } else if (step === 'issue') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          title: t('这块屏幕的颜色发生过变化', 'This display’s color has changed')
        }, t('当前颜色与之前保存的原色不同，暂时不能开始验证。', 'The current color differs from the recorded original, so testing is paused.')), /*#__PURE__*/React.createElement("h3", null, t('先确认其他调色软件已退出', 'First, exit other color adjustment tools')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('完全退出 f.lux 等工具，并关闭系统夜间模式，再检查一次。检查只读取颜色，不会改动屏幕。', 'Fully quit tools such as f.lux and turn off Night light, then check again. This check only reads the display color.')), /*#__PURE__*/React.createElement("details", {
          className: "advanced"
        }, /*#__PURE__*/React.createElement("summary", null, t('为什么需要这一步？', 'Why is this needed?')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('避免把其他软件调过的颜色当成原色，也避免强行覆盖其他软件的调整。', 'This avoids treating another tool’s adjustment as the original color, or overwriting that tool’s changes.'))));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, cancel, /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          icon: "refresh",
          onClick: () => setModal(m => ({
            ...m,
            step: 'mismatch'
          }))
        }, t('重新检查原色', 'Recheck original color')));
      } else if (step === 'mismatch') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          title: t('检查后，仍与旧记录不同', 'The color still differs from the old record')
        }, t('旧记录可能是在其他调色软件运行时保存的。重复检查不会改变这份记录。', 'The old record may have been captured while another color tool was active. Rechecking does not change it.')), /*#__PURE__*/React.createElement("h3", null, t('如果当前画面已经正常', 'If the picture now looks normal')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('可以查看“重新建立原色”。它会保留旧记录，之后需要重新完成每块屏幕的验证。', 'You can review Reset original color. It preserves the old record and requires a new test for each affected display.')));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, cancel, /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: () => setModal(m => ({
            ...m,
            step: 'reset',
            confirmed: false
          }))
        }, t('查看重新建立流程', 'Review reset steps')));
      } else if (step === 'reset') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("h3", null, t('确认这些屏幕的当前画面', 'Check the current picture on these displays')), /*#__PURE__*/React.createElement("div", {
          className: "screen-tags"
        }, draft.screens.filter(s => states[s.id] === 'issue').map(s => /*#__PURE__*/React.createElement("span", {
          key: s.id
        }, s.name))), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('旧记录会先归档保留，再将这些屏幕改为待验证。此步骤不改变屏幕颜色，也不会自动开启调节。', 'Old records are archived first, then these displays are marked for a new test. This step does not change display color or turn adjustment on.')), /*#__PURE__*/React.createElement("label", {
          className: "checkline"
        }, /*#__PURE__*/React.createElement("input", {
          type: "checkbox",
          checked: !!modal.confirmed,
          onChange: e => setModal(m => ({
            ...m,
            confirmed: e.target.checked
          }))
        }), t('我已退出其他调色工具，并确认上面所有屏幕的画面正常', 'I have exited other color tools and all listed displays look normal')), /*#__PURE__*/React.createElement("p", {
          className: "hint"
        }, t('本设计稿仅演示流程，不读取或归档真实数据。', 'This prototype simulates the flow; it does not read or archive real data.')));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('取消', 'Cancel')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: !modal.confirmed,
          onClick: () => {
            setStates(prev => Object.fromEntries(Object.entries(prev).map(([id, state]) => [id, state === 'issue' ? 'unverified' : state])));
            setModal(m => ({
              ...m,
              step: 'resetdone'
            }));
          }
        }, t('确认并重新建立', 'Confirm reset')));
      } else if (step === 'resetdone') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          info: true,
          title: t('已重新建立，接下来验证此屏幕', 'Reset complete. Now test this display.')
        }, t('所有受影响屏幕都需要重新验证，请逐块完成。', 'Every affected display needs a fresh test. Complete them one at a time.')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('现在可以继续完成 15 秒测试；完成后再选择参与并保存。', 'Continue with the 15-second test, then choose participation and save.')));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, cancel, /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: () => setModal(m => ({
            ...m,
            step: 'ready',
            prepared: false
          }))
        }, t('继续设置此屏幕', 'Continue setup')));
      } else if (step === 'unsupported' || step === 'disconnected') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          title: step === 'unsupported' ? t('当前显示模式暂不支持', 'This display mode is not supported') : t('这块屏幕已断开连接', 'This display is disconnected')
        }, step === 'unsupported' ? t('当前演示状态为 HDR。请先切换到 SDR 扩展模式，再回来检查。', 'This demo display uses HDR. Switch to SDR extended mode before checking again.') : t('重新连接后再检查，已有设置不会丢失。', 'Reconnect before checking again. Existing settings are kept.')), /*#__PURE__*/React.createElement("p", {
          className: "hint"
        }, t('设计稿不会改变系统显示设置。', 'This prototype does not change system display settings.')));
        actions = /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: closeModal
        }, t('知道了', 'Got it'));
      } else if (step === 'running') {
        const remaining = Math.min(15, Math.max(0, Math.ceil((15000 - (now - modal.start)) / 1000)));
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
          className: "verification-timer"
        }, /*#__PURE__*/React.createElement("strong", null, remaining), /*#__PURE__*/React.createElement("span", null, t('秒后自动恢复原色', 'seconds until original color returns'))), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t(`${name} 正在模拟 5000 K 调暖。请留意目标屏幕和其他屏幕，结束后确认观察结果。`, `${name}: simulating a 5000 K adjustment. Observe this display and the others, then confirm the result.`)), /*#__PURE__*/React.createElement(Notice, {
          info: true,
          title: t('演示模式 · 不会真正改变屏幕颜色', 'Demo mode · no actual display color change')
        }));
        actions = /*#__PURE__*/React.createElement(Button, {
          onClick: closeModal
        }, t('取消并恢复', 'Cancel and restore'));
      } else if (step === 'observe') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("h3", null, t('已恢复，请确认刚才的效果', 'Restored. Confirm what you observed.')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('实际客户端需要你亲眼确认，读回成功不能代替观察。此处演示确认步骤。', 'In the client, your visual confirmation is required. Readback cannot replace observation. This is a simulated confirmation.')), /*#__PURE__*/React.createElement("div", {
          className: "check-stack"
        }, [t('只有所选屏幕变暖', 'Only the selected display became warmer'), t('其他屏幕保持原样', 'Other displays remained unchanged'), t('结束后恢复正常，画面无异常', 'Original color returned with no visual issues')].map((s, i) => /*#__PURE__*/React.createElement("label", {
          className: "checkline",
          key: i
        }, /*#__PURE__*/React.createElement("input", {
          type: "checkbox",
          checked: modal.checks[i],
          onChange: e => setModal(m => ({
            ...m,
            checks: m.checks.map((v, j) => i === j ? e.target.checked : v)
          }))
        }), s))));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: () => setModal(m => ({
            ...m,
            step: 'failed'
          }))
        }, t('效果不符合', 'Something was wrong')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: !modal.checks.every(Boolean),
          onClick: () => {
            setStates(prev => ({
              ...prev,
              [modal.id]: 'ready'
            }));
            setModal(m => ({
              ...m,
              step: 'done'
            }));
          }
        }, t('确认通过', 'Confirm success')));
      } else if (step === 'failed') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          title: t('暂不启用这块屏幕', 'This display will stay disabled')
        }, t('观察结果不符合预期，未保存验证通过状态。你可以检查显示模式和其他调色工具后重试。', 'The result was not as expected. No verification was saved. Check display mode and other color tools before trying again.')));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('完成', 'Done')), /*#__PURE__*/React.createElement(Button, {
          onClick: () => setModal(m => ({
            ...m,
            step: 'ready',
            prepared: false
          }))
        }, t('重新准备', 'Prepare again')));
      } else if (step === 'done') {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Notice, {
          info: true,
          title: t('这块屏幕已准备好', 'This display is ready')
        }, t('验证通过不会自动开启调节。你可以选择参与屏幕，再保存设置。', 'Verification does not turn adjustment on. Choose participating displays and save your settings.')));
        actions = /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: () => {
            setModal(null);
            navigate('color', 'devices');
          }
        }, t('返回参与屏幕', 'Back to displays'));
      } else {
        body = /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
          className: "screen-tags"
        }, /*#__PURE__*/React.createElement("span", null, name), /*#__PURE__*/React.createElement("span", null, t('SDR · 扩展模式', 'SDR · Extended'))), /*#__PURE__*/React.createElement("h3", null, t('观察一次 15 秒的颜色变化', 'Observe a 15-second color change')), /*#__PURE__*/React.createElement("p", {
          className: "body-copy"
        }, t('只把这块屏幕轻微调暖到 5000 K，随后自动恢复。请同时留意其他屏幕是否保持原样。', 'Only this display warms slightly to 5000 K, then returns to its original color. Check that other displays stay unchanged.')), /*#__PURE__*/React.createElement("label", {
          className: "checkline"
        }, /*#__PURE__*/React.createElement("input", {
          type: "checkbox",
          checked: modal.prepared,
          onChange: e => setModal(m => ({
            ...m,
            prepared: e.target.checked
          }))
        }), t('其他调色工具已退出，我可以观察这次测试', 'Other color tools are closed and I can watch this test')), /*#__PURE__*/React.createElement("p", {
          className: "hint"
        }, t('设计稿只模拟计时和反馈，不操作真实屏幕。', 'The prototype only simulates the timer and feedback.')));
        actions = /*#__PURE__*/React.createElement(React.Fragment, null, cancel, /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: !modal.prepared,
          onClick: () => setModal(m => ({
            ...m,
            step: 'running',
            start: performance.now()
          }))
        }, t('开始 15 秒测试', 'Start 15-second test')));
      }
      return /*#__PURE__*/React.createElement(Dialog, {
        title: t('设置这块屏幕', 'Set up this display'),
        subtitle: name,
        onClose: closeModal,
        actions: actions
      }, /*#__PURE__*/React.createElement("div", {
        className: "stepper"
      }, [t('准备', 'Prepare'), t('试调', 'Test'), t('确认', 'Confirm'), t('完成', 'Done')].map((label, i) => /*#__PURE__*/React.createElement("span", {
        key: i,
        className: i === stepIndex ? 'current' : ''
      }, /*#__PURE__*/React.createElement("b", null, i + 1), label))), body);
    }
    function renderModal() {
      if (!modal) return null;
      if (modal.type === 'setup') return renderSetup();
      if (modal.type === 'general') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('通用设置', 'General settings'),
        subtitle: "ScreenKeeper",
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: closeModal
        }, t('完成', 'Done'))
      }, /*#__PURE__*/React.createElement(ThemePicker, {
        theme: theme,
        onChange: setTheme,
        t: t
      }), /*#__PURE__*/React.createElement(Setting, {
        label: "\u8BED\u8A00 / Language"
      }, /*#__PURE__*/React.createElement("select", {
        className: "input",
        value: lang,
        onChange: e => setLang(e.target.value),
        "aria-label": "\u8BED\u8A00 / Language"
      }, /*#__PURE__*/React.createElement("option", {
        value: "zh"
      }, "\u7B80\u4F53\u4E2D\u6587"), /*#__PURE__*/React.createElement("option", {
        value: "en"
      }, "English"))), /*#__PURE__*/React.createElement(Setting, {
        label: t('开机启动', 'Launch at sign-in')
      }, /*#__PURE__*/React.createElement(Toggle, {
        checked: startup,
        onChange: setStartup,
        label: t('开机启动', 'Launch at sign-in')
      })), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('关闭窗口后仍在托盘运行。', 'The app keeps running in the tray when you close the window.')), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('设计稿中的修改仅对当前演示有效。', 'Changes here only affect this demo.')));
      if (modal.type === 'copy') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('复制本屏设置', 'Copy display settings'),
        subtitle: t(`来源：${screen.name}`, `From: ${screen.name}`),
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('取消', 'Cancel')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: !modal.targets.length,
          onClick: () => {
            const fields = modal.scope === 'rest' ? ['behavior', 'idle', 'idleUnit', 'brightness', 'wake'] : modal.scope === 'privacy' ? ['privacy', 'privacyMode', 'privacyIdle', 'privacyUnit', 'snooze', 'snoozeUnit', 'images', 'fit', 'carousel', 'application', 'launch', 'move'] : Object.keys(screen).filter(k => !['id', 'name', 'primary', 'managed'].includes(k));
            const copied = Object.fromEntries(fields.map(k => [k, clone(screen[k])]));
            setDraft(prev => ({
              ...prev,
              screens: prev.screens.map(s => modal.targets.includes(s.id) ? {
                ...s,
                ...clone(copied)
              } : s)
            }));
            setModal(null);
            notify('已复制到所选屏幕，保存后生效。', 'Copied to selected displays. Save to apply.');
          }
        }, t('复制设置', 'Copy settings')))
      }, /*#__PURE__*/React.createElement(Setting, {
        label: t('复制哪些设置', 'What to copy')
      }, /*#__PURE__*/React.createElement("select", {
        className: "input",
        value: modal.scope,
        onChange: e => setModal(m => ({
          ...m,
          scope: e.target.value
        }))
      }, /*#__PURE__*/React.createElement("option", {
        value: "all"
      }, t('屏幕休息与隐私保护', 'Rest and privacy')), /*#__PURE__*/React.createElement("option", {
        value: "rest"
      }, t('仅屏幕休息', 'Rest only')), /*#__PURE__*/React.createElement("option", {
        value: "privacy"
      }, t('仅隐私保护', 'Privacy only')))), /*#__PURE__*/React.createElement("div", {
        className: "row between",
        style: {
          margin: '14px 0 11px'
        }
      }, /*#__PURE__*/React.createElement("h3", {
        style: {
          margin: 0
        }
      }, t('应用到', 'Copy to')), /*#__PURE__*/React.createElement(Button, {
        kind: "link",
        onClick: () => setModal(m => ({
          ...m,
          targets: draft.screens.filter(s => s.id !== selected).map(s => s.id)
        }))
      }, t('选择全部其他屏幕', 'Select all other displays'))), /*#__PURE__*/React.createElement("div", {
        className: "choice-list"
      }, draft.screens.filter(s => s.id !== selected).map(s => /*#__PURE__*/React.createElement("label", {
        key: s.id
      }, /*#__PURE__*/React.createElement("span", {
        className: "checkline"
      }, /*#__PURE__*/React.createElement("input", {
        type: "checkbox",
        checked: modal.targets.includes(s.id),
        onChange: e => setModal(m => ({
          ...m,
          targets: e.target.checked ? [...m.targets, s.id] : m.targets.filter(x => x !== s.id)
        }))
      }), s.name), /*#__PURE__*/React.createElement("span", {
        className: "muted"
      }, s.managed ? t('管理已开启', 'Care on') : t('管理已关闭', 'Care off'))))), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('保留目标屏幕的管理开关；复制只产生待保存修改。', 'Each target keeps its care switch. Copying creates unsaved changes.')));
      if (modal.type === 'snooze') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('暂缓本屏隐私保护', 'Snooze privacy on this display'),
        subtitle: savedScreen.name,
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('取消', 'Cancel')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: !positive(modal.value, 1, 1440) || seconds(modal.value, modal.unit) > 86400,
          onClick: () => {
            setPrivacyPauses(p => ({
              ...p,
              [selected]: true
            }));
            setModal(null);
            notify('已暂缓本屏保护，不改变默认时长。', 'Privacy snoozed on this display. The default is unchanged.');
          }
        }, t('开始暂缓', 'Snooze')))
      }, /*#__PURE__*/React.createElement(Setting, {
        label: t('这次暂缓多久', 'Snooze this time for')
      }, /*#__PURE__*/React.createElement(Unit, {
        value: modal.value,
        unit: modal.unit,
        units: ['m', 'h'],
        onValue: v => setModal(m => ({
          ...m,
          value: v
        })),
        onUnit: v => setModal(m => ({
          ...m,
          unit: v
        })),
        t: t,
        label: t('本次暂缓时长', 'Snooze duration')
      })), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('只影响本次计时，不修改已保存的默认值。', 'Only affects this pause, not the saved default.')));
      if (modal.type === 'apps') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('选择指定应用', 'Choose an application'),
        subtitle: t('示例窗口 · 不读取真实窗口', 'Example windows · no real windows are read'),
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('取消', 'Cancel')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: !modal.app,
          onClick: () => {
            patchScreen({
              application: modal.app
            });
            setModal(null);
          }
        }, t('使用此应用', 'Use application')))
      }, /*#__PURE__*/React.createElement("div", {
        className: "choice-list"
      }, [['Notepad', t('记事本', 'Notepad'), t('空白文档', 'Blank document')], ['Explorer', t('文件资源管理器', 'File Explorer'), t('此电脑', 'This PC')], ['Browser', t('浏览器', 'Browser'), t('新标签页', 'New tab')]].map(([id, name, note]) => /*#__PURE__*/React.createElement("button", {
        key: id,
        type: "button",
        className: `app-option ${modal.app === id ? 'active' : ''}`,
        onClick: () => setModal(m => ({
          ...m,
          app: id
        }))
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "app",
        size: 23
      }), /*#__PURE__*/React.createElement("span", null, name, /*#__PURE__*/React.createElement("small", null, note, " \xB7 ", screen.name))))), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('默认不启动新程序，也不移动其他屏幕上的窗口。', 'By default, no new app is launched and no window is moved from another display.')));
      if (modal.type === 'images') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('隐私幕外观', 'Privacy curtain appearance'),
        subtitle: t('画面只显示“继续使用”按钮。', 'The curtain only shows a Continue button.'),
        wide: true,
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('取消', 'Cancel')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          disabled: modal.background === 'custom' && (!modal.images.length || modal.images.length > 1 && !positive(modal.carousel, 5, 3600)),
          onClick: () => {
            patchScreen({
              images: modal.images,
              background: modal.background,
              fit: modal.fit,
              carousel: modal.carousel
            });
            setModal(null);
          }
        }, t('保留修改', 'Keep changes')))
      }, /*#__PURE__*/React.createElement("div", {
        className: "segmented",
        role: "radiogroup",
        "aria-label": t('背景方式', 'Background type')
      }, [['builtin', t('默认山林', 'Forest mist')], ['solid', t('纯色', 'Solid color')], ['custom', t('自定义图片', 'Custom pictures')]].map(([value, label]) => /*#__PURE__*/React.createElement("button", {
        key: value,
        type: "button",
        role: "radio",
        "aria-checked": modal.background === value,
        className: modal.background === value ? 'active' : '',
        onClick: () => setModal(m => ({
          ...m,
          background: value
        }))
      }, label))), modal.background === 'custom' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
        className: "row wrap",
        style: {
          marginTop: 16
        }
      }, /*#__PURE__*/React.createElement(Button, {
        icon: "image",
        disabled: modal.images.length >= 12,
        onClick: () => fileRef.current?.click()
      }, t('添加图片', 'Add images')), /*#__PURE__*/React.createElement(Button, {
        kind: "quiet",
        onClick: () => setModal(m => ({
          ...m,
          images: ['curtain-hills.svg', 'curtain-grove.svg']
        }))
      }, t('使用示例图片', 'Use sample images')), /*#__PURE__*/React.createElement("input", {
        ref: fileRef,
        className: "hidden-file",
        type: "file",
        accept: "image/png,image/jpeg,image/webp",
        multiple: true,
        onChange: e => importPictures(e.target.files)
      })), /*#__PURE__*/React.createElement("div", {
        className: "gallery"
      }, modal.images.map((src, i) => /*#__PURE__*/React.createElement("div", {
        className: "gallery-item",
        key: `${src}-${i}`
      }, /*#__PURE__*/React.createElement("img", {
        src: src,
        alt: t(`图片 ${i + 1}`, `Image ${i + 1}`)
      }), /*#__PURE__*/React.createElement("button", {
        type: "button",
        "aria-label": t(`移除图片 ${i + 1}`, `Remove image ${i + 1}`),
        onClick: () => setModal(m => ({
          ...m,
          images: m.images.filter((_, j) => j !== i)
        }))
      }, /*#__PURE__*/React.createElement(Icon, {
        name: "close",
        size: 12
      }))))), /*#__PURE__*/React.createElement(Setting, {
        label: t('图片显示方式', 'Image fit')
      }, /*#__PURE__*/React.createElement("select", {
        className: "input",
        value: modal.fit,
        onChange: e => setModal(m => ({
          ...m,
          fit: e.target.value
        }))
      }, /*#__PURE__*/React.createElement("option", {
        value: "cover"
      }, t('填满屏幕', 'Fill')), /*#__PURE__*/React.createElement("option", {
        value: "contain"
      }, t('完整显示', 'Fit')))), modal.images.length > 1 && /*#__PURE__*/React.createElement(Setting, {
        label: t('每张图片停留', 'Show each image for')
      }, /*#__PURE__*/React.createElement(Unit, {
        value: modal.carousel,
        unit: t('秒', 'sec'),
        onValue: v => setModal(m => ({
          ...m,
          carousel: v
        })),
        min: 5,
        max: 3600,
        t: t,
        label: t('轮播间隔', 'Slideshow interval')
      })), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('最多 12 张，每张不超过 10 MB。单图不轮播。', 'Up to 12 images, 10 MB each. A single image stays still.'))) : /*#__PURE__*/React.createElement("div", {
        className: "mini-curtain",
        style: {
          margin: '17px 0',
          maxHeight: 220,
          backgroundImage: curtainStyle(curtainImage(modal))
        }
      }, /*#__PURE__*/React.createElement("span", {
        className: "btn"
      }, t('继续使用', 'Continue'))));
      if (modal.type === 'health-image') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('选择提醒图片', 'Choose a reminder image'),
        subtitle: t('图片用于提醒静景和图片休息画面', 'Used for landscape reminders and picture breaks'),
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(Button, {
          onClick: closeModal
        }, t('取消', 'Cancel'))
      }, /*#__PURE__*/React.createElement("div", {
        className: "row wrap"
      }, /*#__PURE__*/React.createElement(Button, {
        icon: "image",
        onClick: () => fileRef.current?.click()
      }, t('从电脑选择', 'Choose from computer')), /*#__PURE__*/React.createElement(Button, {
        onClick: () => {
          patch('health', {
            image: 'curtain-grove.svg'
          });
          setModal(null);
        }
      }, t('使用林间示例', 'Use woodland sample')), /*#__PURE__*/React.createElement("input", {
        className: "hidden-file",
        type: "file",
        ref: fileRef,
        accept: "image/png,image/jpeg,image/webp",
        onChange: e => importPictures(e.target.files, true)
      })), /*#__PURE__*/React.createElement("p", {
        className: "hint"
      }, t('图片只在浏览器本地预览，不上传。', 'Images stay in this browser and are not uploaded.')));
      if (modal.type === 'privacy-test') return /*#__PURE__*/React.createElement(Dialog, {
        title: t('测试保护效果', 'Try privacy protection'),
        subtitle: screen.name,
        onClose: closeModal,
        actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
          kind: "quiet",
          onClick: closeModal
        }, t('取消', 'Cancel')), /*#__PURE__*/React.createElement(Button, {
          kind: "primary",
          onClick: () => {
            setModal(null);
            setPreview({
              type: modal.mode === 'desktop' ? 'desktop' : 'application'
            });
          }
        }, t('查看效果', 'View effect')))
      }, /*#__PURE__*/React.createElement("p", {
        className: "body-copy"
      }, modal.mode === 'desktop' ? t('先遮挡屏幕，再收起本屏窗口。唤醒后停留在桌面，不自动还原私人窗口。', 'The display is covered before its windows are minimized. Waking leaves it on the desktop.') : t('先安全隐藏其他窗口，再显示所选应用。应用不可用时，保留隐私幕。', 'Other windows are hidden before the chosen app appears. If it is unavailable, the curtain stays.')), /*#__PURE__*/React.createElement(Notice, {
        info: true,
        title: t('设计预览，不操作真实窗口', 'Design preview; no real windows are changed')
      }));
      return null;
    }
    const pageTabs = module === 'screens' ? [['rest', t('屏幕休息', 'Screen rest'), 'moon'], ['privacy', t('隐私保护', 'Privacy'), 'shield']] : module === 'health' ? [['rhythm', t('提醒节奏', 'Rhythm'), 'clock'], ['appearance', t('提醒外观', 'Appearance'), 'image']] : [['schedule', t('作息与色温', 'Schedule & color'), 'sun'], ['devices', t('屏幕与验证', 'Displays & setup'), 'screen']];
    const toggleLabel = module === 'screens' ? t('管理本屏', 'Manage display') : module === 'health' ? t('启用健康提醒', 'Enable break reminders') : t('启用色温调节', 'Enable color adjustment');
    const previewSettings = preview?.settings || draft.health;
    const previewStyle = preview?.phase === 'rest' ? previewSettings.restStyle : previewSettings.reminder === 'scenic' && previewSettings.image ? 'picture' : previewSettings.reminder;
    return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("header", {
      className: "review"
    }, /*#__PURE__*/React.createElement("div", {
      className: "review-brand"
    }, /*#__PURE__*/React.createElement("div", {
      className: "review-number"
    }, "05"), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", null, "\u8272\u6E29\u8C03\u8282 \xB7 \u6BCF\u5C4F\u5404\u6709\u8282\u594F"), /*#__PURE__*/React.createElement("p", null, "\u4FDD\u7559\u53CC\u9875\u7B7E / \u9010\u5C4F\u5F00\u542F\u4E0E\u8BBE\u7F6E"))), /*#__PURE__*/React.createElement("div", {
      className: "review-tools"
    }, /*#__PURE__*/React.createElement("div", {
      className: "size-switch",
      "aria-label": "\u8BBE\u8BA1\u7A3F\u5C3A\u5BF8"
    }, /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: size === 'standard' ? 'active' : '',
      onClick: () => {
        setSize('standard');
        setExpanded(false);
      }
    }, "\u6807\u51C6\u7A97\u53E3"), /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: size === 'compact' ? 'active' : '',
      onClick: () => {
        setSize('compact');
        setExpanded(false);
      }
    }, "700 \xD7 500")), /*#__PURE__*/React.createElement("select", {
      "aria-label": "\u6F14\u793A\u573A\u666F",
      value: scenario,
      onChange: e => setScenarioState(e.target.value)
    }, /*#__PURE__*/React.createElement("option", {
      value: "normal"
    }, "\u65E5\u5E38\u4F7F\u7528 \xB7 \u53CC\u5C4F"), /*#__PURE__*/React.createElement("option", {
      value: "validated"
    }, "\u5DF2\u9A8C\u8BC1 \xB7 \u5C1A\u672A\u542F\u7528"), /*#__PURE__*/React.createElement("option", {
      value: "no-participants"
    }, "\u5168\u90E8\u5173\u95ED"), /*#__PURE__*/React.createElement("option", {
      value: "unverified"
    }, "\u5C1A\u672A\u9A8C\u8BC1"), /*#__PURE__*/React.createElement("option", {
      value: "pending"
    }, "\u4E00\u5C4F\u5F85\u9A8C\u8BC1"), /*#__PURE__*/React.createElement("option", {
      value: "many"
    }, "\u56DB\u5C4F\u8BBE\u7F6E"), /*#__PURE__*/React.createElement("option", {
      value: "conflict"
    }, "\u5F02\u5E38 \xB7 \u539F\u8272\u9700\u786E\u8BA4"), /*#__PURE__*/React.createElement("option", {
      value: "unsupported"
    }, "\u5F02\u5E38 \xB7 HDR \u4E0D\u53EF\u7528"), /*#__PURE__*/React.createElement("option", {
      value: "disconnected"
    }, "\u5F02\u5E38 \xB7 \u5C4F\u5E55\u65AD\u5F00"), /*#__PURE__*/React.createElement("option", {
      value: "empty"
    }, "\u672A\u53D1\u73B0\u5C4F\u5E55"), /*#__PURE__*/React.createElement("option", {
      value: "save-fail"
    }, "\u4FDD\u5B58\u5931\u8D25")), /*#__PURE__*/React.createElement("a", {
      href: "settings-v4.html",
      target: "_blank",
      rel: "noreferrer"
    }, "\u5BF9\u7167 v4 \u2197"))), /*#__PURE__*/React.createElement("div", {
      className: "stage"
    }, hidden ? /*#__PURE__*/React.createElement("div", {
      className: "restored-window"
    }, /*#__PURE__*/React.createElement("img", {
      src: "settings-v2-icon.svg",
      width: "45",
      height: "45",
      alt: "ScreenKeeper"
    }), /*#__PURE__*/React.createElement("p", null, t('窗口已收起，设置草稿仍保留。', 'The window is hidden. Your drafts are kept.')), /*#__PURE__*/React.createElement(Button, {
      onClick: () => setHidden(false)
    }, t('重新打开 ScreenKeeper', 'Reopen ScreenKeeper'))) : /*#__PURE__*/React.createElement("div", {
      className: `app-window ${size === 'compact' ? 'compact' : ''} ${expanded ? 'expanded' : ''}`,
      "data-theme": theme,
      "data-screen-label": `${title} / ${tabs[module]}`
    }, /*#__PURE__*/React.createElement("div", {
      className: "titlebar",
      onDoubleClick: e => {
        if (!e.target.closest('button,select')) setExpanded(!expanded);
      }
    }, /*#__PURE__*/React.createElement("div", {
      className: "title-brand"
    }, /*#__PURE__*/React.createElement("img", {
      src: theme === 'fog' ? 'tray-icons/stackmoon-light.svg' : 'settings-v2-icon.svg',
      alt: ""
    }), /*#__PURE__*/React.createElement("b", {
      className: "cp-product-name"
    }, t('屏幕良伴', 'ScreenKeeper')), /*#__PURE__*/React.createElement("span", {
      className: "cp-brand-tagline",
      title: t('照顾屏幕，也照顾自己', 'Care for your screen. And you.')
    }, t('照顾屏幕，也照顾自己', 'Care for your screen. And you.'))), /*#__PURE__*/React.createElement("div", {
      className: "window-tools"
    }, /*#__PURE__*/React.createElement("select", {
      className: "language theme-quick",
      "aria-label": t('外观主题', 'Appearance theme'),
      title: t('外观主题', 'Appearance theme'),
      value: theme,
      onChange: e => setTheme(e.target.value)
    }, /*#__PURE__*/React.createElement("option", {
      value: "forest"
    }, t('深绿', 'Forest green')), /*#__PURE__*/React.createElement("option", {
      value: "fog"
    }, t('雾白绿', 'Mist green'))), /*#__PURE__*/React.createElement("select", {
      className: "language",
      "aria-label": "\u8BED\u8A00 / Language",
      value: lang,
      onChange: e => setLang(e.target.value)
    }, /*#__PURE__*/React.createElement("option", {
      value: "zh"
    }, "\u7B80\u4F53\u4E2D\u6587"), /*#__PURE__*/React.createElement("option", {
      value: "en"
    }, "English")), /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: "chrome",
      "aria-label": t('最小化', 'Minimize'),
      onClick: () => {
        stopTrial();
        setPreview(null);
        setHidden(true);
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "minimize",
      size: 15
    })), /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: "chrome",
      "aria-label": expanded ? t('还原', 'Restore') : t('最大化', 'Maximize'),
      onClick: () => setExpanded(!expanded)
    }, /*#__PURE__*/React.createElement(Icon, {
      name: expanded ? 'restore' : 'maximize',
      size: 13
    })), /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: "chrome",
      "aria-label": t('关闭到托盘', 'Close to tray'),
      onClick: () => {
        stopTrial();
        setPreview(null);
        setHidden(true);
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "close",
      size: 16
    })))), /*#__PURE__*/React.createElement("div", {
      className: "workspace",
      inert: modal || preview ? '' : undefined
    }, /*#__PURE__*/React.createElement("aside", {
      className: "sidebar"
    }, /*#__PURE__*/React.createElement("div", {
      className: "nav-caption"
    }, t('我的工作空间', 'WORKSPACE')), [['screens', t('屏幕管理', 'Display care'), 'screen'], ['health', t('健康提醒', 'Healthy breaks'), 'clock'], ['color', t('色温调节', 'Color temperature'), 'sun']].map(([id, label, icon]) => /*#__PURE__*/React.createElement("button", {
      type: "button",
      key: id,
      className: `nav-item ${module === id ? 'selected' : ''}`,
      "aria-current": module === id ? 'page' : undefined,
      onClick: () => navigate(id)
    }, /*#__PURE__*/React.createElement(Icon, {
      name: icon,
      size: 19
    }), /*#__PURE__*/React.createElement("span", null, label), dirty[id] && /*#__PURE__*/React.createElement("i", {
      className: "nav-dot",
      "aria-label": t('有未保存修改', 'Unsaved changes')
    }))), /*#__PURE__*/React.createElement("div", {
      className: "nav-bottom"
    }, /*#__PURE__*/React.createElement("button", {
      className: "nav-item",
      type: "button",
      onClick: () => {
        stopTrial();
        setModal({
          type: 'general'
        });
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "gear",
      size: 18
    }), t('通用设置', 'General')))), /*#__PURE__*/React.createElement("main", {
      className: "main"
    }, module === 'color' ? /*#__PURE__*/React.createElement(window.SKColor.Workspace, {
      key: scenario,
      model: colorModel,
      setModel: setColorModel,
      t: t,
      scenario: scenario
    }) : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("header", {
      className: "page-head"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h2", null, title, /*#__PURE__*/React.createElement("span", {
      className: "scope"
    }, module === 'screens' ? t('按屏幕', 'PER DISPLAY') : module === 'health' ? t('全局计时', 'GLOBAL') : t('共用作息', 'SHARED SCHEDULE'))), /*#__PURE__*/React.createElement("p", null, module === 'screens' ? t('每一块屏幕，都有合适的休息方式。', 'Give each display its own way to rest.') : module === 'health' ? t('专注的时候，也记得给自己留一点空隙。', 'Make room for a pause, even in your flow.') : t('从工作到休息，让光线慢慢柔和下来。', 'A gentler light, from work hours to evening.'))), (module !== 'screens' || screen) && /*#__PURE__*/React.createElement("div", {
      className: "head-toggle"
    }, /*#__PURE__*/React.createElement("span", null, enabled ? t('已开启', 'On') : t('已关闭', 'Off'), enabled !== savedEnabled && /*#__PURE__*/React.createElement("small", {
      style: {
        display: 'block',
        fontSize: 10,
        color: 'var(--accent)',
        marginTop: 4
      }
    }, t('待保存', 'Unsaved'))), /*#__PURE__*/React.createElement(Toggle, {
      checked: !!enabled,
      onChange: setEnabled,
      label: toggleLabel
    }))), module === 'screens' && screen && /*#__PURE__*/React.createElement("div", {
      className: "screen-picker"
    }, /*#__PURE__*/React.createElement("div", {
      className: "screen-chips"
    }, draft.screens.map((s, i) => /*#__PURE__*/React.createElement("button", {
      type: "button",
      className: `screen-chip ${selected === s.id ? 'active' : ''}`,
      key: s.id,
      "aria-pressed": selected === s.id,
      onClick: () => setSelected(s.id)
    }, /*#__PURE__*/React.createElement("span", {
      className: "selection-check",
      "aria-hidden": "true"
    }, "\u2713"), /*#__PURE__*/React.createElement("span", {
      className: "number"
    }, i + 1), /*#__PURE__*/React.createElement("span", null, s.name, s.primary && /*#__PURE__*/React.createElement("small", null, t('主屏', 'Main'))), JSON.stringify(s) !== JSON.stringify(saved.screens.find(x => x.id === s.id)) && /*#__PURE__*/React.createElement("span", {
      className: "state-dot"
    })))), /*#__PURE__*/React.createElement("select", {
      className: "input screen-select",
      "aria-label": t('当前屏幕', 'Current display'),
      value: selected,
      onChange: e => setSelected(e.target.value)
    }, draft.screens.map((s, i) => /*#__PURE__*/React.createElement("option", {
      key: s.id,
      value: s.id
    }, i + 1, " \xB7 ", s.name, " ", s.primary ? t(' · 主屏', ' · Main') : ''))), /*#__PURE__*/React.createElement(Button, {
      small: true,
      kind: "quiet",
      icon: "refresh",
      disabled: dirty.screens,
      "aria-label": t('刷新显示器', 'Refresh displays'),
      onClick: () => notify('显示器列表已更新（演示）。', 'Display list refreshed (demo).')
    })), /*#__PURE__*/React.createElement(Tabs, {
      value: tabs[module],
      items: pageTabs,
      onChange: changeTab,
      label: title
    }), /*#__PURE__*/React.createElement("div", {
      className: "editor",
      ref: editor,
      role: "tabpanel",
      "aria-label": pageTabs.find(x => x[0] === tabs[module])[1]
    }, module === 'screens' ? renderScreens() : module === 'health' ? renderHealth() : renderColor()), saveError && /*#__PURE__*/React.createElement("div", {
      className: "save-error",
      role: "alert"
    }, saveError), /*#__PURE__*/React.createElement("footer", {
      className: "footer"
    }, /*#__PURE__*/React.createElement("div", {
      className: "footer-state"
    }, /*#__PURE__*/React.createElement("span", {
      className: "state-dot",
      style: {
        opacity: dirty[module] ? 1 : .45
      }
    }), /*#__PURE__*/React.createElement("div", null, dirty[module] ? t('有未保存的修改', 'Unsaved changes') : t('设置已保存', 'Settings saved'), /*#__PURE__*/React.createElement("small", null, module === 'screens' ? t('保存所有屏幕的修改', 'Saves edits for all displays') : module === 'health' ? t('仅保存健康提醒设置', 'Saves break settings only') : t('仅保存色温设置', 'Saves color settings only')))), /*#__PURE__*/React.createElement("div", {
      className: "footer-actions"
    }, /*#__PURE__*/React.createElement(Button, {
      kind: "quiet",
      disabled: !dirty[module],
      onClick: discard
    }, t('放弃更改', 'Discard')), /*#__PURE__*/React.createElement(Button, {
      kind: "primary",
      disabled: !dirty[module],
      onClick: save
    }, saveLabel)))))), renderModal(), preview && /*#__PURE__*/React.createElement("div", {
      ref: previewRoot,
      className: "full-preview",
      role: "dialog",
      "aria-modal": "true",
      "aria-label": t('效果预览', 'Effect preview')
    }, preview.type === 'health' ? /*#__PURE__*/React.createElement(Scene, {
      key: preview.phase,
      style: previewStyle,
      image: previewSettings.image || 'forest-mist.png',
      moving: previewSettings.moving,
      rest: preview.phase === 'rest',
      coverage: previewSettings.coverage,
      intensity: previewSettings.intensity,
      fade: previewSettings.fade,
      animate: preview.phase !== 'rest',
      time: preview.live ? fmt(healthClock) : fmt(seconds(previewSettings.rest, previewSettings.restUnit)),
      t: t
    }) : preview.type === 'curtain' ? /*#__PURE__*/React.createElement("div", {
      className: "mini-curtain",
      style: {
        position: 'absolute',
        inset: 0,
        aspectRatio: 'auto',
        border: 0,
        borderRadius: 0,
        backgroundImage: curtainStyle(preview.image === undefined ? 'forest-mist.png' : preview.image)
      }
    }, /*#__PURE__*/React.createElement(Button, {
      onClick: () => setPreview(null)
    }, t('继续使用', 'Continue'))) : /*#__PURE__*/React.createElement("div", {
      className: "empty",
      style: {
        border: 0
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: preview.type === 'desktop' ? 'desktop' : 'app',
      size: 60
    }), /*#__PURE__*/React.createElement("h3", null, preview.type === 'desktop' ? t('本屏已回到桌面', 'This display is on the desktop') : t('本屏已切换到指定应用', 'This display shows the chosen application')), /*#__PURE__*/React.createElement("p", null, t('这里是模拟效果，真实窗口未被改动。', 'This is a simulation. Real windows are unchanged.'))), preview.type !== 'curtain' && /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("div", {
      className: "full-preview-toolbar"
    }, /*#__PURE__*/React.createElement("span", null, t('效果预览 · 不影响真实屏幕', 'PREVIEW · No real display changes')), /*#__PURE__*/React.createElement(Button, {
      small: true,
      onClick: () => setPreview(null)
    }, t('退出预览', 'Close preview'), " ", /*#__PURE__*/React.createElement(Icon, {
      name: "close",
      size: 14
    }))), preview.type === 'health' && /*#__PURE__*/React.createElement("div", {
      className: "preview-bottom"
    }, /*#__PURE__*/React.createElement(Button, {
      onClick: () => setPreview(null)
    }, preview.phase === 'rest' ? t('结束预览', 'End preview') : t('稍后提醒', 'Remind me later')), preview.phase === 'reminder' && /*#__PURE__*/React.createElement(Button, {
      kind: "primary",
      onClick: () => setPreview(p => ({
        ...p,
        phase: 'rest'
      }))
    }, t('看看休息画面', 'See the break scene'))))), toast && /*#__PURE__*/React.createElement("div", {
      className: `toast ${toast.error ? 'error-toast' : ''}`,
      role: "status"
    }, t(toast.zh, toast.en)))), /*#__PURE__*/React.createElement("div", {
      className: "review-note"
    }, "\u53EF\u70B9\u51FB\u8BBE\u8BA1\u7A3F \xB7 \u793A\u4F8B\u6570\u636E \xB7 \u4E0D\u64CD\u4F5C\u771F\u5B9E\u5C4F\u5E55\u6216\u4EA7\u54C1\u914D\u7F6E \xB7 \u5207\u6362\u6F14\u793A\u573A\u666F\u4F1A\u91CD\u7F6E\u793A\u4F8B"));
  }
  ReactDOM.createRoot(document.getElementById('root')).render(/*#__PURE__*/React.createElement(App, null));
})();
