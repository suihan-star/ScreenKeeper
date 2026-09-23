# 009 共用山林背景开发计划

关联 [009 设计](009-shared-background-design.md)。在当前目录保留全部已有变更，不提交或推送。

1. 将原图复制到 Assets 并嵌入程序集；建立两模块共用的冻结图像缓存，记录来源和 SHA256。
2. 隐私添加默认背景选项及兼容字段，接入图像／遮罩和仅图片的渐显；保留不透明保护与缺图回退。
3. 健康静景和图片模式使用共用默认资源，设置预览同步；保留已保存样式和用户图像。
4. 同步 v4 可点击原型，执行针对性行为测试、渲染检查、Release 构建及适当回归。
5. 更新设计、计划和 AGENTS.md 的长期事实，交付独立构建目录，不终止运行中的客户端。

回退仅恢复本任务代码／资源；旧版本忽略新增 PrivacyOptions 字段并回到其原纯色默认，不删除产品设置、原色日志或用户图片。

## 实施与验证记录（2026-09-20）

1–5 已完成。主实现文件为 OLED-Sleeper/UI/Helpers/BuiltInBackground.cs、Features/MonitorPrivacy/PrivacyCurtainHost.cs／PrivacyOptions.cs、UI/Views/PrivacySettingsView.xaml(.cs) 和 HealthSceneWindow.cs。补齐 MonitorConfigurationViewModel.Options、HealthReminderSettings 的默认值、健康设置文案和英文资源。原图以 WPF Resource 内嵌，客户端无需读取临时附件或连接网络。

v4 原型同步默认山林、纯色、自定义图片及健康默认背景；JSX 重新编译到对应 JS。图片来源及 SHA256 已记录在 Assets/README.md；README 与 AGENTS.md 已同步长期约定。

验证命令（OutputPath 均为 artifacts/009-background-build 的绝对路径）：
- `dotnet build OLED-Sleeper.sln -c Release --no-restore -p:OutputPath=...`：通过；NU1900 为包漏洞源不可达的网络警告。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:OutputPath=... --logger "trx;LogFileName=009-background.trx" --results-directory artifacts/009-test-results`：497 通过、2 跳过、0 失败，总计 499。跳过项为需显式启用的真实桌面窗口测试。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore`：通过，保留现有工作区加载警告。
- `node --check designs/screenkeeper-calm/settings-v4.js` 和 `settings-v4-ui.js`：通过。
- 设置 `SCREENKEEPER_RENDER_QA=artifacts/009-background-render`（绝对路径）生成 WPF 核验图：检查两主题／中英文的三种外观选项及草稿回退、图片完全透明时的底层遮挡、纯黑休眠、横屏和竖屏裁切及健康倒计时组合。原图缓存为同一冻结对象，自定义图片优先、缺图回退及旧 JSON 均由测试覆盖。

新增 SharedBackgroundTests 的早期逐像素检查曾受按钮文字抗锯齿的 alpha 254 舍入影响；改为独立核验保护底层（同时检查实际窗口禁止透明、窗口 opacity=1），未修改产品遮挡逻辑，随后全量通过。

浏览器已核验山林背景及三选项弹窗可见；后续浏览器控制连接超时，未完成原型纯色／图片交互重测。客户端对应绑定切换、缺图、回退已由 WPF 测试覆盖。未触发用户实际隐私保护、健康休息或任何调色操作；未重启／替换运行中客户端，未改用户配置，未安装、提交或推送。

交付：artifacts/009-background-build/ScreenKeeper.exe；从托盘正常退出旧版本后可运行。已保存为雾光／静夜的用户，可在健康提醒 → 提醒外观中选择“静景渐显”／“背景图片”体验山林。
