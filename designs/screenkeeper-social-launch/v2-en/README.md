# ScreenKeeper — English campaign pack

One screen or many. Your way.

- `posters/`: four English posters, localized with the built-in imagegen tool.
- `exports/`: four 18-second looping GIFs (540×720) and MP4s (900×1200, 24 fps).
- `index.html`: gallery. `motion/*.html`: editable timeline players.
- `motion.jsx` / `motion.css`: English composition source; `vendor/` contains local timeline and React dependencies.
- `imagegen-prompts.json`: exact prompts and source references. `manifest.json`: checksums.
- `qa/`: the workspace holds all keyframes; the ZIP includes representative thumbnails and validation reports.

The illustrations preserve the existing Chinese campaign’s composition and colors. Single-screen setups are supported. Screen rest, privacy and color temperature can be configured per display; break reminders use one independent work cycle.

These are feature illustrations, not screen recordings. Waits are shortened. The privacy curtain does not replace Windows lock. Dimming requires device support; color temperature requires the app’s display verification. No medical or hardware compatibility claim is implied.

Serve the repository’s `designs/` directory over HTTP and open `screenkeeper-social-launch/v2-en/index.html`. All preview dependencies are local. Edit `motion.jsx` and run `node build.cjs`; run `node render.cjs qa`, then `node render.cjs export`, `python verify-media.py`, and `python package.py` from this directory. The render script expects the preview server on port 4311.

Build uses the existing local Babel/Playwright/Chrome and the isolated FFmpeg export tool; environment overrides are supported in the scripts. No application dependencies changed. Forest and logo assets come from the existing ScreenKeeper project. React’s license is included. The Chinese source pack remains unchanged.

Media verification needs Pillow. It was run with the bundled Python runtime at `<user-home>/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`; the system Python does not include Pillow. The ZIP contains final assets and editable sources, not these build runtimes.
