# 016 · 客户端官网入口计划

关联 [任务设计](016-official-website-design.md)。本轮不提交、推送或上传。

- [x] 加入顶部官网按钮、统一网址常量、默认浏览器打开与失败提示。
- [x] 核验现有布局、本地化、双主题及最小窗口表现。
- [x] 重新归档并构建 1.0.0 双架构便携包（20260924.2），核验产物与旧包保留。
- [x] 更新发布说明、文档入口、验收结果并审查 AGENTS.md。


## 实现与界面验证

- MainWindow.xaml 在右上角主题选择器左侧新增常驻官网按钮，保留 WindowChrome 的客户区命中、键盘焦点及现有双主题样式；官网中文／英文文案、工具提示、辅助名称和失败提示均集中于现有 English.json 即时本地化机制。
- ProductIdentity.WebsiteUrl 固定为用户提供的 HTTPS 地址；MainWindow.xaml.cs 只在按钮点击时通过 ProcessStartInfo.UseShellExecute=true 打开默认浏览器，不传用户信息。Win32 或进程启动异常用 AppDialog 显示说明和完整网址，不修改草稿。
- `dotnet test OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release --no-restore -p:BaseOutputPath=bin/WebsiteEntry/ --filter 'FullyQualifiedName~SettingsLayoutTests.LayoutRendersInBothLanguages_AndCurtainContainsOnlyReleaseButton|FullyQualifiedName~WellbeingSettingsTests.ThemesAndPagesRenderInBothLanguagesAndSizes'`：2 项通过。SCREENKEEPER_RENDER_QA 输出到 `artifacts/website-entry-20260924/screenshots`，覆盖双主题、中英文、1000×736 与 700×500，以及各模块／页签状态。实际查看深色中文正常窗口、浅色英文窄窗口截图，顶部按钮可见、无控件重叠；窄窗口的口号按既有规则省略。
- 对上述新编译测试输出分别执行 `dotnet vstest OLED-Sleeper.Tests/bin/WebsiteEntry/Release/net8.0-windows/OLED-Sleeper.Tests.dll /Platform:x64` 和 `/Platform:x86`，设置 SCREENKEEPER_EXPECT_ARCH 断言实际测试进程位数。两次均 592 项中 590 通过、2 项显式原生测试跳过、0 失败；TRX 在 `artifacts/website-entry-20260924/full-x64.trx` 与 `full-x86.trx`。x86 使用 014 的私有 8.0.31 运行时。
- 首次布局构建出现 MinVer 工作区识别警告（测试输出回退为预发布版本）及 CS8034 分析器因页面文件不足未能加载的警告；构建和布局测试完成。正式归档构建显式指定 1.0.0 与源码身份，使用单节点／禁用共享编译的命令重新编译，未出现这两项警告。保留 NU1900 漏洞源不可达，不宣称漏洞审计通过。
- 网页读取工具未能访问该地址，受限环境 curl 首次因 Windows TLS 凭据失败；以正常网络权限执行 `curl.exe -I --max-time 20 https://screenkeeper.skyrrats.com/` 返回 HTTP 200，未关闭 TLS 校验。HTTP 检查不等于所有用户网络可达，也未实际点击唤起用户默认浏览器；失败弹窗未故意破坏系统关联进行实测。


## 最终交付与维护

- 最终目录：`artifacts/stable-1.0.0-20260924.2-final/release/`。版本 1.0.0，构建 20260924.2；交付只指向此目录，不分发本轮中间候选包。
- 冻结 279 个源码输入；清单 SHA256 `2f178d27fe2b74f1fff009bde5d6f8149d00c6c53503a66e875fd1a1d13018ef`。FileVersion=1.0.0.0，ProductVersion=1.0.0+snapshot.<清单 SHA256>；BUILD-INFO 明示基线提交和未提交的 x86／官网入口改动。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 最终退出码 0。首次发现对象初始化器需要换行，修正后与已完成测试的源码比较，唯一程序差异为该换行；随后重新冻结并编译两个最终程序，未复用之前候选包。
- 最终执行 `dotnet publish ... -c Release -r win-x64/win-x86 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:MinVerVersionOverride=1.0.0 -p:SourceRevisionId=snapshot.<清单 SHA256> -p:RuntimeFrameworkVersion=8.0.31 -p:UseSharedCompilation=false -m:1 -nodeReuse:false --ignore-failed-sources` 均成功，日志在最终目录父级。
- 两运行包各 28 个文件，附双语说明、FAQ、发布说明、VERSION，以及 8 个运行组件和两种对应运行时的许可；deps.json 版本、6 个 EXE／原生 DLL 架构、ZIP CRC、解压后全部文件 SHA256 与帮助原稿一致性检查通过。未含 Data、日志、PDB、凭据或测试探针。
- 对重新解压的两个 EXE 执行已有 startup hook，指定不存在的外部 DOTNET_ROOT，均成功加载包内 .NET 8.0.31，实际架构、便携标识、版本及界面资源正确。探针在 Main 前退出，没有创建 Data 或启动显示器服务；不等于完整 UI 或默认浏览器实测。
- `ScreenKeeper-1.0.0-Portable-x64.zip`：68,899,650 字节；SHA256 `94ff4fd501b4fa6372617fb0c69e147596073916e40ed1e0cb6d1c7ef03deab2`。
- `ScreenKeeper-1.0.0-Portable-x86.zip`：63,974,143 字节；SHA256 `5f85550368e21d11a6e905b4ed393d1e4a4142c66da0532e83949e7e6ac699cc`。
- `ScreenKeeper-1.0.0-Source-20260924.2.zip`：2,595,279 字节；SHA256 `e35509a40cc43dbb2446a1e14e5c6bb23852773a17ee954542adc4dac3678e08`。
- 同目录附源码归档、SHA256SUMS、BUILD-INFO 和双语发布说明。逐项校验最终清单通过；015 两个 ZIP、早期双语 x64 包、HEAD 和原产品标签均未变化。
- UTF-8、行尾空白、67 个相关文档本地链接以及 `git diff --check` 通过。已同步 016 设计／计划、主窗口、ProductIdentity、英文资源、README、CHANGELOG、文档入口、帮助和发布说明。AGENTS.md 已审查并记录官网入口只由用户点击触发的长期约束。
- 保留未签名、Windows 10 原生 32 位未实测及 NU1900 漏洞源不可达边界；不宣称漏洞审计通过。既有用户配置与运行客户端未改动；未提交、推送或上传发行平台。
