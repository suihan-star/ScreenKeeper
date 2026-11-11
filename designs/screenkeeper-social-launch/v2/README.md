# 屏幕良伴推广 v2：单屏多屏，皆由你心定

更新定位：Windows 单屏与多屏均适用，多屏独立设置是扩展能力。

## 交付内容

- posters/：四张 imagegen 定向编辑后的海报，保留 v1 原稿。
- copy-x.txt / copy-xiaohongshu.txt：更新后的两平台文案。
- exports/：四模块各一份 18 秒 MP4 与 GIF。
- index.html：完整媒体预览；motion/*.html 是可暂停、逐帧查看的时间轴演示。
- imagegen-prompts.json：完整编辑提示词；manifest.json：文件尺寸与校验。
- qa/：工作区保存全部关键时间点截图及验证记录；下载包包含四张预览封面和三份验证报告。
- screenkeeper-social-v2.zip：海报、文案、GIF、MP4、离线演示与可编辑源文件；package.py 可重新生成清单并校验打包内容。

## 素材与功能来源

本地官网 Screen-Keeper-web/src/components/ScreenDemo.vue、PrivacyDemo.vue、HealthSection.vue、ColorDemo.vue 作为只读交互参考；颜色来自其 src/styles/base.css。未修改或发布官网。

forest-mist.png 与 logo.svg 复制自本项目 OLED-Sleeper/Assets；山林原图为用户提供并确认的现有产品背景，来源详见该目录 README.md。React 本地运行时复制自既有 designs/screenkeeper-calm/vendor，附原许可。animations-v3 与 tweaks-panel 来自 baoyu-design 技能。图像定向编辑使用内置 imagegen，没有使用 API/CLI 替代路径。

动画是功能示意，不是软件实录，不访问本机硬件、窗口内容、输入记录或真实计时。每段以单屏为例，等待被压缩；隐私幕不替代系统锁屏；调暗需设备支持；色温须先完成客户端观察验证，网页色调不证明实际色温或医学效果。

## 本地预览与生成

在仓库根目录使用 Python HTTP server 服务 designs/，打开 screenkeeper-social-launch/v2/index.html。动画文件与依赖全部本地加载，预览不需要联网。

编辑 motion.jsx / motion.css 后运行 node designs/screenkeeper-social-launch/v2/build.cjs 重新编译。通过 SCREENKEEPER_BABEL 指定本地 Babel standalone 文件、SCREENKEEPER_PLAYWRIGHT 指定 Playwright 模块，或提供可正常解析的 @babel/standalone 与 playwright 模块。渲染器 render.cjs 通过 animations-v3 的同步时间轴事件逐帧导出；Chrome、FFmpeg 与预览地址可分别通过 SCREENKEEPER_CHROME、SCREENKEEPER_FFMPEG、SCREENKEEPER_PREVIEW_URL 指定，不修改应用依赖。

根任务设计与计划仍为 docs/tasks/017-social-promotion-*，第二轮结果追加记录。
