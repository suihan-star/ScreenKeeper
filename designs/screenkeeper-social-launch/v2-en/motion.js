const SK = {
  rest: {
    num: "01",
    module: "Screen rest",
    title: ["Let your screen", "take a breather."],
    extra: "Black out or dim idle screens. Snooze when you need to.",
    note: "Illustrative demo · Shortened wait · Dimming needs device support"
  },
  privacy: {
    num: "02",
    module: "Privacy protection",
    title: ["Your screen.", "Your private space."],
    extra: "Scheduled protection, with manual control when you need it.",
    note: "Illustrative demo · Shortened wait · Not a Windows lock"
  },
  health: {
    num: "03",
    module: "Break reminders",
    title: ["Take a break.", "Work can wait."],
    extra: "Set work and break durations to suit your day.",
    note: "Illustrative demo · Sample timer · No real work cycle changed"
  },
  color: {
    num: "04",
    module: "Color temperature",
    title: ["From day to night,", "at your pace."],
    extra: "Keep original colors, or set work and rest temperatures.",
    note: "Illustrative colors · Verify your display before enabling"
  }
};
const MOTION = {
  enter: (t, a, b) => Easing.easeInOutCubic(clamp((t - a) / (b - a), 0, 1)),
  draw: (t, a, b, from = 0, to = 1) => interpolate([a, b], [from, to], Easing.easeInOutCubic)(t),
  pop: (t, a, b) => Math.sin(Math.PI * clamp((t - a) / (b - a), 0, 1))
};
function useTarget(rootRef, targetRef, frameMode) {
  const [p, setP] = React.useState({
    x: 380,
    y: 220
  });
  React.useLayoutEffect(() => {
    const update = () => {
      const r = rootRef.current,
        b = targetRef.current;
      if (!r || !b) return;
      const a = r.getBoundingClientRect(),
        z = b.getBoundingClientRect();
      const scale = a.width / r.offsetWidth;
      setP({
        x: (z.left - a.left + z.width / 2) / scale,
        y: (z.top - a.top + z.height / 2) / scale
      });
    };
    update();
    const observer = new ResizeObserver(update);
    if (rootRef.current) observer.observe(rootRef.current);
    window.addEventListener("resize", update);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", update);
    };
  }, [frameMode]);
  return p;
}
function Pointer({
  x,
  y,
  show = 1,
  click = 0
}) {
  return React.createElement("div", {
    className: "pointer",
    style: {
      left: x,
      top: y,
      opacity: show
    }
  }, React.createElement("i", {
    className: "click-ring",
    style: {
      opacity: click,
      transform: "scale(" + (0.7 + click * 0.6) + ")"
    }
  }));
}
function Workspace() {
  return React.createElement("div", {
    className: "workspace"
  }, React.createElement("div", {
    className: "appbar"
  }, React.createElement("span", null, "Work notes \xB7 Fictional example"), React.createElement("span", null, "\u2014\u3000\u25A1\u3000\xD7")), React.createElement("div", {
    className: "work-grid"
  }, React.createElement("aside", {
    className: "work-nav"
  }, React.createElement("strong", null, "My workspace"), React.createElement("span", null, "Today"), React.createElement("span", null, "Projects"), React.createElement("span", null, "Ideas")), React.createElement("main", {
    className: "work-body"
  }, React.createElement("h3", null, "Make room for a calmer day"), React.createElement("div", {
    className: "work-meta"
  }, React.createElement("span", null, "Work notes"), React.createElement("span", null, "Tasks & ideas")), React.createElement("div", {
    className: "work-line"
  }), React.createElement("div", {
    className: "work-line"
  }), React.createElement("div", {
    className: "work-line"
  }), React.createElement("div", {
    className: "work-line"
  }), React.createElement("div", {
    className: "work-line"
  }), React.createElement("div", {
    className: "work-card-row"
  }, React.createElement("div", {
    className: "work-card"
  }, "One thing at a time"), React.createElement("div", {
    className: "work-card"
  }, "Make time for a break")))));
}
function DemoScreen({
  kind,
  T,
  CUES,
  screenRef
}) {
  const targetRef = React.useRef(null),
    p = useTarget(screenRef, targetRef, kind === 'health' && T >= CUES.Active + 1.7 && T < CUES.Return + 2.2);
  const A = CUES.Active,
    R = CUES.Return,
    L = CUES.Loop,
    G = CUES.Trigger;
  const inWait = T >= G && T < A,
    clickAt = kind === "privacy" ? R + 1.5 : kind === "health" ? A + 1.5 : R + 1.5;
  const click = MOTION.pop(T, clickAt - .12, clickAt + .3);
  let target = {
    x: p.x,
    y: p.y
  };
  let progress = MOTION.enter(T, clickAt - 1.6, clickAt - .2);
  let x = target.x + 110 * (1 - progress),
    y = target.y + 105 * (1 - progress);
  let show = T >= clickAt - 1.7 && T < clickAt + .55 ? 1 : 0;
  const closed = T >= A && T < R + 1.85;
  const reminder = MOTION.enter(T, G, G + .65) * (1 - MOTION.enter(T, R + 2, R + 2.35));
  const resting = T >= A + 1.7 && T < R + 2.2;
  const seconds = Math.max(0, 300 - Math.floor(Math.max(0, T - (A + 1.7))));
  const warm = MOTION.enter(T, G, A + 1) * (1 - MOTION.enter(T, R + 1.5, R + 1.85));
  if (kind === "rest") {
    x = MOTION.draw(T, R - .5, R + 1.4, 680, 440);
    y = MOTION.draw(T, R - .5, R + 1.4, 390, 260);
    show = T >= R - .5 && T < R + 2 ? 1 : 0;
  }
  if (kind === "color") show = T >= R - .1 && T < L ? 1 : 0;
  if (kind === "privacy" && T > A + 1 && T < R - .5) {
    const q = MOTION.enter(T, A + 1, A + 4);
    x = 80 + 550 * q;
    y = 96;
    show = 1;
  }
  const healthReturned = T >= R + 2.2;
  const healthClick2 = MOTION.pop(T, R + 1.85, R + 2.15);
  const healthEndMove = MOTION.enter(T, R, R + 1.85);
  if (kind === "health" && T >= R) {
    x = p.x + 90 * (1 - healthEndMove);
    y = p.y + 80 * (1 - healthEndMove);
    show = T < R + 2.3 ? 1 : 0;
  }
  return React.createElement(React.Fragment, null, React.createElement(Workspace, null), kind === "rest" && React.createElement(React.Fragment, null, React.createElement("div", {
    className: "idle-card",
    style: {
      opacity: inWait ? 1 : 0
    }
  }, "Idle delay (demo)", React.createElement("strong", null, Math.max(0, Math.ceil(A - T)), " s")), React.createElement("div", {
    className: "idle-track",
    style: {
      opacity: inWait ? 1 : 0
    }
  }, React.createElement("i", {
    style: {
      width: clamp((T - G) / (A - G), 0, 1) * 100 + "%"
    }
  })), React.createElement("div", {
    className: "screen-layer",
    "data-blackout": "",
    style: {
      background: "#000",
      opacity: MOTION.enter(T, A, A + .5) * (1 - MOTION.enter(T, R + 1, R + 1.6))
    }
  })), kind === "privacy" && React.createElement(React.Fragment, null, React.createElement("div", {
    className: "idle-card",
    style: {
      opacity: inWait ? 1 : 0
    }
  }, "Privacy delay (demo)", React.createElement("strong", null, Math.max(0, Math.ceil(A - T)), " s")), React.createElement("div", {
    className: "screen-layer curtain",
    "data-curtain": "",
    style: {
      opacity: closed ? 1 : 0
    }
  }, React.createElement("img", {
    className: "forest",
    src: "../assets/forest-mist.png",
    alt: "",
    style: {
      opacity: MOTION.enter(T, A + .1, A + 1.1)
    }
  }), React.createElement("button", {
    ref: targetRef,
    className: "curtain-button",
    style: {
      filter: click ? "brightness(1.08)" : ""
    }
  }, "Enter this screen"))), kind === "health" && React.createElement(React.Fragment, null, React.createElement("div", {
    className: "screen-layer",
    style: {
      opacity: resting ? 1 : reminder * .22
    }
  }, React.createElement("img", {
    className: "forest",
    src: "../assets/forest-mist.png",
    alt: ""
  })), React.createElement("div", {
    className: "screen-layer " + (resting ? "health-resting" : ""),
    style: {
      opacity: reminder
    }
  }, React.createElement("div", {
    className: "health-card",
    "data-health-state": healthReturned ? "ended" : resting ? "resting" : "reminder"
  }, React.createElement("span", {
    className: "health-close"
  }, "\xD7"), React.createElement("div", {
    className: "health-label"
  }, "ScreenKeeper \xB7 Break reminders"), React.createElement("h3", null, resting ? "Look into the distance" : "Time for a break"), React.createElement("p", null, resting ? "Take a moment. Look away from the screen." : "Blink, stretch, take a moment."), resting && React.createElement("div", {
    className: "health-timer"
  }, "0", Math.floor(seconds / 60), ":", String(seconds % 60).padStart(2, "0")), React.createElement("button", {
    ref: targetRef,
    className: "health-primary",
    style: {
      filter: click || healthClick2 ? "brightness(1.08)" : ""
    }
  }, resting ? "End early" : "Start break"), !resting && React.createElement("div", {
    className: "health-secondary"
  }, React.createElement("span", null, "\u2161\u3000Snooze"), React.createElement("span", null, "\u2192\u3000Skip this time"))))), kind === "color" && React.createElement(React.Fragment, null, React.createElement("div", {
    className: "screen-layer"
  }, React.createElement("img", {
    className: "forest",
    src: "../assets/forest-mist.png",
    alt: ""
  }), React.createElement("div", {
    className: "color-overlay",
    "data-warm": "",
    style: {
      opacity: warm * .58
    }
  })), React.createElement("div", {
    className: "color-pills"
  }, React.createElement("span", null, warm > .1 ? "Rest hours · Warm colors" : "Work hours · Original colors"), React.createElement("span", null, "Single screen")), React.createElement("button", {
    className: "compare-button",
    ref: targetRef,
    style: {
      background: warm < .1 && T >= R && T < L ? "#b4d6a1" : undefined
    }
  }, "Compare original")), React.createElement(Pointer, {
    x: x,
    y: y,
    show: show,
    click: kind === "health" && T >= R ? healthClick2 : click
  }));
}
function Piece() {
  const {
      T,
      CUES,
      duration
    } = useComposition(),
    tl = useTimeline(),
    screenRef = React.useRef(null);
  const kind = window.SCREENKEEPER_DEMO,
    d = SK[kind],
    G = CUES.Trigger,
    A = CUES.Active,
    R = CUES.Return,
    L = CUES.Loop;
  const warm = MOTION.enter(T, G, A + 1) * (1 - MOTION.enter(T, R + 1.5, R + 1.85));
  const state = kind === "rest" ? T < G || T >= L ? "Screen in use" : T < A ? "Waiting while idle" : T < R + 1 ? "Screen resting" : "Activity detected · Restored" : kind === "privacy" ? T < G || T >= L ? "Your content, on screen" : T < A ? "Waiting to protect" : T < R + 1.85 ? "Privacy curtain active" : "Revealed by you" : kind === "health" ? T < G || T >= L ? "Time to focus" : T < A + 1.7 ? "Break reminder" : T < R + 2.2 ? "Break countdown" : "Back to work" : warm < .1 ? "Original colors" : warm > .95 ? "Custom warm colors" : "Gradual transition";
  const captions = kind === "rest" ? [{
    at: -1,
    text: "Stay focused while your screen is in use."
  }, {
    at: G,
    text: "When idle, it rests on your terms."
  }, {
    at: A,
    text: "Nothing to show? Let the screen take a breather."
  }, {
    at: R,
    text: "Your chosen activity brings the display back."
  }, {
    at: L,
    text: "Stay focused while your screen is in use."
  }] : kind === "privacy" ? [{
    at: -1,
    text: "Your chats and documents need not stay on display."
  }, {
    at: G,
    text: "Set your rules. Let protection follow."
  }, {
    at: A,
    text: "Content is covered first. The forest fades in next.",
    until: A + 1.8
  }, {
    at: A + 1.8,
    text: "Moving the pointer does not reveal your content."
  }, {
    at: R,
    text: "Choose “Enter this screen” to reveal your content."
  }, {
    at: L,
    text: "Your chats and documents need not stay on display."
  }] : kind === "health" ? [{
    at: -1,
    text: "A rhythm for work. A little room for rest."
  }, {
    at: G,
    text: "A gentle reminder. The choice stays yours."
  }, {
    at: A,
    text: "The countdown starts when you choose Start break."
  }, {
    at: R,
    text: "Ready to return? You can end the break early."
  }, {
    at: L,
    text: "A rhythm for work. A little room for rest."
  }] : [{
    at: -1,
    text: "Keep original colors during your work hours."
  }, {
    at: G,
    text: "Ease into warmer colors at your chosen time."
  }, {
    at: A,
    text: "Set a separate schedule for each display."
  }, {
    at: R,
    text: "Compare with original colors whenever you need to."
  }, {
    at: L,
    text: "Keep original colors during your work hours."
  }];
  React.useLayoutEffect(() => {
    window.__animStage = {
      duration,
      captureActive: document.documentElement.hasAttribute("data-capture"),
      setTime(time) {
        document.querySelector("[data-om-exportable-video-with-duration-secs]").dispatchEvent(new CustomEvent("data-om-seek-to-time-frame", {
          detail: {
            time,
            sync: true
          }
        }));
      },
      setPlaying: tl.setPlaying
    };
  }, [duration, tl.setPlaying]);
  const zoom = 1 + .018 * MOTION.enter(T, A - 1, A + 1) * (1 - MOTION.enter(T, R + 2, L));
  return React.createElement("div", {
    className: "piece",
    "data-screen-label": d.module + " · " + Math.floor(T) + "s",
    "data-demo": kind,
    "data-time": T.toFixed(3),
    "data-state": state
  }, React.createElement("div", {
    className: "brand"
  }, React.createElement("img", {
    src: "../assets/logo.svg",
    alt: ""
  }), React.createElement("span", null, "ScreenKeeper")), React.createElement("div", {
    className: "series"
  }, "FEATURE DEMO ", d.num, " / 04"), React.createElement("div", {
    className: "hairline"
  }), React.createElement("div", {
    className: "eyebrow"
  }, d.module), React.createElement("h1", {
    className: "headline"
  }, React.createElement("span", null, d.title[0]), React.createElement("span", null, d.title[1])), React.createElement("div", {
    className: "promise"
  }, "One screen or many. Your way."), React.createElement("div", {
    className: "state-line"
  }, React.createElement("span", {
    className: "state-pill"
  }, state), React.createElement("span", {
    className: "single-label"
  }, kind === "health" ? "One work cycle across your displays" : "Single-screen demo · Per-display settings")), React.createElement("div", {
    className: "monitor-group",
    style: {
      transform: "scale(" + zoom + ")"
    }
  }, React.createElement("div", {
    className: "monitor-frame"
  }, React.createElement("div", {
    className: "screen",
    ref: screenRef
  }, React.createElement(DemoScreen, {
    kind: kind,
    T: T,
    CUES: CUES,
    screenRef: screenRef
  }))), React.createElement("div", {
    className: "stand"
  })), kind === "color" ? React.createElement("div", {
    className: "color-track"
  }, React.createElement("span", null, "Original"), React.createElement("div", {
    className: "rail"
  }, React.createElement("div", {
    className: "fill",
    style: {
      width: warm * 100 + "%"
    }
  }), React.createElement("i", {
    className: "knob",
    style: {
      left: warm * 100 + "%"
    }
  })), React.createElement("span", null, "Warm colors")) : React.createElement("div", {
    className: "module-extra"
  }, d.extra), React.createElement(Captions, {
    items: captions,
    style: {
      bottom: 126,
      left: 40,
      right: 40,
      fontFamily: '"Segoe UI",sans-serif',
      fontSize: 26,
      color: "#153b30",
      textShadow: "none",
      fontWeight: 600
    }
  }), React.createElement("div", {
    className: "footer-line"
  }), React.createElement("div", {
    className: "footnote"
  }, React.createElement("span", null, d.note), React.createElement("span", null, "screenkeeper.skyrrats.com")));
}
function App() {
  const [t, setTweak] = useTweaks(window.TWEAK_DEFAULTS),
    [panel, setPanel] = React.useState(false);
  const capture = new URLSearchParams(location.search).has("capture");
  return React.createElement("div", {
    className: t.motionEditor ? "" : "hide-editor"
  }, React.createElement(CompositionStage, {
    width: 900,
    height: 1200,
    scenes: window.OM_SCENES,
    playback: window.OM_PLAYBACK,
    autoplay: !capture && !matchMedia("(prefers-reduced-motion: reduce)").matches
  }, React.createElement(Piece, null)), !capture && React.createElement(React.Fragment, null, React.createElement("button", {
    className: "tweaks-open",
    onClick: () => setPanel(!panel)
  }, panel ? "Hide settings" : "Playback settings"), panel && React.createElement("div", {
    className: "motion-panel"
  }, React.createElement("h3", null, "Playback settings"), React.createElement(TweakToggle, {
    label: "Motion editor",
    value: t.motionEditor,
    onChange: v => setTweak("motionEditor", v)
  }), React.createElement("p", null, "Space to play or pause. Arrow keys to step through frames. Illustrative demo."), React.createElement("a", {
    href: "../index.html"
  }, "Back to all four demos"))));
}
ReactDOM.createRoot(document.getElementById("root")).render(React.createElement(App, null));
