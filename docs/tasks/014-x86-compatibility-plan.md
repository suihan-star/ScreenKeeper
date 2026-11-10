# 014 · Windows x86 兼容开发计划

关联 [任务设计](014-x86-compatibility-design.md)。保留当前工作区原有未提交变更；本轮不提交／推送。

用户观察步骤见 [双语验收说明](014-x86-validation.txt)，随预览包提供。

- [x] 修正窗口样式 API 的进程位数分派，检查原生结构布局。
- [x] 指定应用改用跨位数路径查询，补成功／失败及自有 x64 子进程回归。
- [x] 运行 x64／x86 自动化与必要的自有窗口检查，记录实际架构及失败边界。
- [x] 便携打包支持 x64／x86，更新双语说明、FAQ 和用户验收步骤。
- [x] 生成独立预览包，核验自包含依赖、许可、文件架构、解压一致性及 SHA256。
- [x] 同步任务记录、文档索引和 AGENTS.md；交给用户进行真实功能观察。
- [x] 用户已在当前电脑试用 x86 预览并反馈暂未发现使用问题；未提供逐项验收结果，详见下方用户观察反馈。
- [ ] Windows 10 原生 x86 系统尚未实测，不以当前电脑的 WOW64 试用替代。

## 验证结果

### 实现与构建

- `NativeMethods.cs` 的 Get/SetWindowLongPtr 保留调用签名，按 IntPtr.Size 分派明确的 W 入口，设置 SetLastError；新增测试覆盖原生错误、高位窗口样式和完整指针值往返。PHYSICAL_MONITOR 的 Unicode 说明修正为 x86 260／x64 264 字节，两种实际测试进程均检查结构大小、偏移及中文描述往返。
- `ProcessImagePath.cs` 使用 SafeProcessHandle、PROCESS_QUERY_LIMITED_INFORMATION 和 QueryFullProcessImageNameW，容量不足时有界扩容，失败不返回路径；`ApplicationWindowAccess` 枚举和运行状态均采用此查询。保持原窗口匹配／恢复保护；失败时仍不重复启动不可检查进程，未记录标题或路径。
- 允许测试程序集访问内部互操作；新增 `ArchitectureInteropTests` 7 项测试。原生应用窗口 fixture 在 WOW64 下通过 Sysnative 明确启动 x64 PowerShell，断言目标进程指针大小为 8，再验证枚举、路径、运行状态、定位、最小化及恢复；只操作测试自己的窗口。
- `build.ps1` 便携阶段循环 x64／x86，分开 staging 和 ZIP 名称，保留双语帮助复制。实际通过 AST 读取并执行脚本现有 Write-Step／Publish-Payload 函数与便携阶段，未执行安装器或清理旧 artifacts。两次 `dotnet publish -c Release -r win-x64/win-x86 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none` 均成功；进程内设置 MinVerVersionOverride=1.0.1-preview.1、BaseOutputPath=bin/X86CompatPublish/。
- 本轮源码基于 main `88492d243a65314f013ef1290ff9897543ce63ff` 加未提交的 014 更改，不移动 1.0.0 标签。FileVersion=1.0.1.0，ProductVersion=1.0.1-preview.1+88492d243a65314f013ef1290ff9897543ce63ff；包内 VERSION 明确注明未提交改动，不冒充已打标签正式版。

### 自动化与环境

- `dotnet test OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release --no-restore -p:BaseOutputPath=bin/X86Compat/ -p:MinVerVersionOverride=1.0.1-preview.1 --filter FullyQualifiedName~ArchitectureInteropTests`：构建成功，x64 新增 7 项通过；首次构建沿用缓存的 NU1900 漏洞源不可达警告，不据此宣称漏洞审计通过。
- 从微软官方 8.0 release-metadata 下载 .NET 8.0.31 x86 Core／Windows Desktop ZIP，按官方 SHA512 校验后解压到 `artifacts/x86-toolchain-8.0.31/dotnet`，未安装／修改系统运行时。受限沙箱 HTTPS 身份验证失败后，在正常网络权限下成功完成下载，未关闭 TLS 校验。
- 测试执行使用 `dotnet vstest OLED-Sleeper.Tests/bin/X86Compat/Release/net8.0-windows/OLED-Sleeper.Tests.dll /Platform:x64` 或 `/Platform:x86`；分别设置 SCREENKEEPER_EXPECT_ARCH=x64／x86，x86 另设置 DOTNET_ROOT_X86、DOTNET_ROOT(x86) 为上述私有运行时目录，DOTNET_MULTILEVEL_LOOKUP=0。测试自身断言实际进程架构；控制台显示的 VSTest x64 是协调进程，不是 x86 测试宿主。
- x86 完整常规测试：592 项，590 通过、2 个显式桌面项跳过、0 失败，见 `artifacts/x86-compat-tests/x86/full-x86.trx`。x64 同样 590 通过、2 跳过、0 失败，见 `x64/standard-x64.trx`。x86 新增接口测试单独执行也是 7 项通过。
- 对 ApplicationWindowAccessTests 单独设置 SCREENKEEPER_NATIVE_QA=1，以正常桌面权限执行，x64 与 x86 各 1 项通过，见 `x64/native-x64.trx`、`x86/native-x86.trx`；x86 实际检查了自己创建的 x64 应用窗口。
- 初次在受限沙箱对整套测试打开 SCREENKEEPER_NATIVE_QA=1 时出现两项失败：桌面枚举受限，以及已有 SettingsLayoutTests 的 Application.Current.Windows 跨 dispatcher 访问。用未修改的首版 `bin/FirstVersion` 测试输出复核：585 项中 582 通过、同样 2 项失败、1 跳过，错误位置相同，见 `baseline/baseline-native-suite.trx`。桌面枚举项已在正常权限下通过；既有扩展渲染测试线程问题未顺带修改，不宣称整套可选原生扩展全绿。
- 首次尝试用 x86 dotnet 直接加载 x64 SDK 的 vstest.console.dll 出现 BadImageFormat；随后采用 x64 VSTest 协调真实 x86 testhost 的标准方式完成检查。该失败属于测试启动方式，不是产品 EXE。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 最终退出码 0；首次发现新加 using 顺序后只调整该文件。PowerShell Parser.ParseFile、`git diff --check` 通过。新文本严格 UTF-8／行尾空白检查通过，FAQ 中英各 14 项，相关文档 84 个本地链接存在。

### 交付与人工验收边界

- 用户成品：`artifacts/x86-compat-preview-20260924/ScreenKeeper-1.0.1-preview.1-Portable-x86.zip`，63,973,299 字节（约 61.0 MiB），28 个文件。包含 8 个运行依赖及两个 x86 运行时的对应许可、双语快速说明、FAQ、验证说明和版本信息。构建专用 MinVer／ILLink 不带运行资产，未作为运行组件列入。
- ZIP SHA256：`af5f950f18a75c5e393c4bfaddcf200f05307cc5fc5a8b3d2dfa79baf2323793`，同目录有 `ScreenKeeper-1.0.1-preview.1-SHA256SUMS.txt`。Python zipfile CRC、完整解压及逐文件 SHA256 一致性通过；6 个 EXE／原生 DLL 的 PE Machine 均为 0x14c（x86）；帮助文件与原稿逐字节一致，无 Data、日志、测试辅助 DLL 或凭据。
- 对重新解压的 EXE 进行 Main 前 startup hook 检查，指定不存在的外部运行时路径且关闭多级查找：成功使用包内 .NET 8.0.31，实际架构 X86，Portable=true，版本／中英文资源正确。检查在业务 Main 前退出，没有显示客户端、创建 Data 或写显示器；记录为 `runtime-probe-x86.json`。这不等于完整 UI 或硬件验收。
- 旧双语 1.0.0 x64 ZIP 的 SHA256 仍为 `a661b7c6258facde44cb932acb3aef023ecc9c9b3468643964536dad44f32446`。原有客户端、用户配置和标签未改动；x64 新构建仅为同源回归产物，不替换旧交付包。
- 当前预览仍未签名；未建立 Windows 10 原生 x86 虚拟机／实机，也未自动测试真实亮度、Gamma、隐私触发及恢复。用户已反馈现有电脑试用暂未发现问题，具体边界见下方记录。不能把上述自动化检查扩大为全部系统／显卡／显示器兼容证明。
- 已更新设计、计划、文档入口、双语帮助以及 AGENTS.md 的目标平台、互操作和双架构打包约定。保留 013 未提交改动，本轮未提交、推送、创建标签或上传产物。

### 用户观察反馈 · 2026-09-24

- 用户反馈：“验证了, 暂时没发现使用上的问题”。对应本轮交付的 1.0.1-preview.1 x86 便携预览版，按既有测试环境记录为当前 x64 Windows 电脑上的试用反馈。
- 记录为当前环境初步试用无已报告问题；用户未列出逐项操作结果，不据此认定双语验收说明所有项目、所有显示器功能或 Windows 10 原生 32 位系统均已通过。
- 本次仅补充验收记录，不修改程序、重新打包或执行新一轮功能测试；上述预览包与校验值保持不变。未提交、推送、创建标签或发布。
- 已复核关联任务设计及 AGENTS.md，需求范围和长期开发规则没有变化，本次无需更新两者。
