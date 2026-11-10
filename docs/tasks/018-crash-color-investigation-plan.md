# 018 · 异常退出与色温失效排查计划

关联 [排查设计](018-crash-color-investigation-design.md)。公开记录将真实用户的作息和故障时刻改为相对事件顺序，保留构建日期、证据数量与验证结论。

- [x] 确认活动便携版、保存目录、版本及发布文件一致性。
- [x] 核对当天异常退出与应用日志、系统资源和显示错误事件。
- [x] 比较色温保存策略、恢复日志与当前 Gamma；区分冲突和未验证。
- [x] 分析现有崩溃转储并记录证据能够支持的结论。
- [x] 修复已经确认的选屏取消递归，补充回归并执行双架构检查。
- [x] 给出后续处理与验证方案，记录未验证项，审查并更新 AGENTS.md。

## 初步证据

- 用户确认当前使用 `artifacts/stable-1.0.0-20260924.2-final/release/ScreenKeeper-1.0.0-Portable-x64/ScreenKeeper`，与进程路径一致，版本为 1.0.0／构建 20260924.2。原 ZIP 的 SHA256 与发布记录一致；运行目录中全部 7 个 EXE／DLL／config 与 ZIP 内容相同。
- Windows Application Error 1000 和 WER 1001 确认排查前发生三次崩溃：第一起代码 0xc0000005，后两起 0xc00000fd；现场未提供更精确的操作时间。CrashDumps 保留了对应三份转储。
- 第一次崩溃前约一分钟的日志连续报告上一轮显示器轮询未完成，随后应用崩溃；另外两次日志末尾没有异常堆栈。此时序不足以证明三次崩溃都由显示器扫描引起。
- 当天 System 警告／错误筛选没有找到 Resource-Exhaustion、Display、nvlddmkm、WHEA、Kernel-Power 事件。当前空闲内存充足，但不能反推崩溃瞬间的全部资源状态。
- Q27P10 保存策略开启，包含工作与休息两个时段，分别为自定义 4000 K 和 4500 K；DELL 关闭。恢复日志备份仍未冲突，随后的主记录已 `Conflicted=true`、`PendingValue=null`。原色基线通道终点为 65535／65535／65535，最后应用终点为 65535／52895／42683。
- 冲突发生后的只读硬件查询：Q27P10 的物理目标、拓扑、模式均匹配记录；当前通道终点为 65535／65445／65506，506 个采样值不同于原色、511 个不同于最后应用值。DELL 的当前通道终点也为 65535／65445／65506；是否全部采样值一致仍需单独比较，不能仅凭终点声称整条曲线一致。
- 观察到 f.lux 在颜色冲突发生前已运行，可作为外部调色候选，但没有记录可以识别冲突时的实际写入者。
- 独立 ColorProbe 不读取客户端验证注册表，其显示「未验证」是工具默认隔离行为，不代表客户端资格丢失。

## 崩溃分析结论

- 排查期间，原便携客户端又发生第四次崩溃；没有向该进程注入、附加调试器、终止进程或启动新客户端。使用 Windows 自带 DbgEng 和独立 Microsoft ClrMD 3.1.512801 工具对已有转储离线取栈；没有上传转储、导出对象内容或变更系统转储配置。
- 后三份转储均显示重复的 `ColorSettingsViewModel.EndPreviewAsync → ColorTemperatureService.CancelPreviewAsync → WellbeingCoordinator.ColorChanged → ColorSettingsViewModel.UpdateCapabilities` 链。旧 `SelectedScreen` setter 在取消后才替换旧屏，同步通知又试图替换同一个失效屏，导致无限递归。这是已确认的应用缺陷，不能归咎于用户验证操作。
- 第一次崩溃的访问冲突通过匹配的 Microsoft `singlefilehost.pdb` 解析为 `Thread::VirtualUnwindCallFrame → Thread::VirtualUnwindToFirstManagedCallFrame → EEPolicy::HandleFatalStackOverflow`，即处理致命栈溢出时的二次异常。原始业务栈未完整恢复，因此只能说与栈溢出相关，不能直接证明也是上述选屏链。
- 颜色冲突先于第四次崩溃。当前证据支持「颜色变化触发恢复保护，界面处理不可用选屏时又可能进入递归」；不支持「色温消失一定由崩溃导致」或「一定是 f.lux 写入」。后端发现当前 Gamma 与最后应用值不同后停止写入，这项保护保留。
- 后续完整双屏 Gamma 复核未完成：DISPLAY1 创建只读 DC 失败，再枚举时仅有 Q27P10。此时出现新的显示拓扑变化，不能把先前只读查询时的模式匹配无限延伸到之后，也不能依据仅通道终点相同宣称两屏整条曲线完全相同。未尝试启用或唤醒消失的屏幕。

Microsoft 原生 API 说明确认其他程序、系统以及显示事件都可能覆盖／重置 Gamma，但不能识别某次实际写入者：[SetDeviceGammaRamp](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdevicegammaramp)。栈溢出代码含义与调查方式见 [Debugging a Stack Overflow](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/debugging-a-stack-overflow)。

## 修改与验证

- `ColorSettingsViewModel.cs`：先同步更新作息选屏和预览目标，再结束旧试调。去掉同一切屏过程中原先两次重复取消，但继续取消旧请求、保留草稿与恢复保护。
- `ColorSettingsViewModel.Preview.cs`：直接切换试调屏幕时也先更新目标，再调用取消并通知。
- `ColorLiveSettingsTests.cs`：新增 4 个参数化用例，覆盖冲突后有／无可用替代屏、手动切屏和直接选择试调目标；断言取消回调看到一致的新状态，不启动试调、不保存参数、不丢弃已有草稿。
- 新增测试的初次运行在旧实现上 4 项均失败，出现回调中仍可见旧目标、切屏重复取消以及递归重复刷新；受控次数上限避免测试进程真正栈溢出。修复后首次色温测试发现夹具在准备草稿时过早注入冲突，调整为完成准备后才开启注入，再运行通过。
- `dotnet test OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release --no-restore -p:BaseOutputPath=bin/CrashColor/ -p:UseSharedCompilation=false -m:1 -nodeReuse:false --filter 'FullyQualifiedName~ColorLiveSettingsTests|FullyQualifiedName~Features.ColorTemperature'`：187 项通过，0 失败。构建与测试不启动用户规则、不写真实 Gamma。
- 对同一测试输出执行 `dotnet vstest OLED-Sleeper.Tests/bin/CrashColor/Release/net8.0-windows/OLED-Sleeper.Tests.dll /Platform:x64` 和 `/Platform:x86`：各 596 项中 594 通过、2 项显式原生测试跳过、0 失败。设置 `SCREENKEEPER_EXPECT_ARCH` 断言实际位数；x86 使用已有私有 8.0.31 运行时。不能替代原生 Windows 10 x86 硬件验收。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/UI/ViewModels/ColorSettingsViewModel.cs OLED-Sleeper/UI/ViewModels/ColorSettingsViewModel.Preview.cs OLED-Sleeper.Tests/UI/ViewModels/ColorLiveSettingsTests.cs`：退出码 0。`git diff --check` 通过。
- 原生与托管调用栈摘要、测试结果保存在被忽略的 `artifacts/diagnostics-20261008/`，转储仍留在本机原有 CrashDumps，不进入源码。
- 已同步 018 设计、计划与文档索引；AGENTS.md 追加「选屏在取消／通知前更新，测试覆盖同步回调」的长期规则，保留其他任务已有修改。

## 后续与未验证事项

1. 本轮已完成定位、源码修复及自动回归；没有重新发布 ZIP、替换用户运行文件或改动其 Data。原 ZIP 仍包含旧代码，不能把源码测试通过说成用户已经运行修复版。
2. 颜色竞争仍需受控实机对照：完整退出 f.lux、保持 Windows 夜间模式关闭，确保希望参与的屏幕处于连接状态；在修复版中关闭并保存色温策略，再逐屏「重新检查原色」。若仍不匹配，应确认当前画面正常后走已有「重新建立原色」及验证流程，保留归档，不删除恢复记录。
3. 只执行一次确认后的恢复流程，之后观察至少超过本次冲突出现的时间，并覆盖屏幕开关、锁屏恢复及工作／休息切换；没有这一步不能断言色温稳定性问题已解决。本轮没有操作该对照，也未取得用户实际画面观察。
4. 第一次崩溃的原始业务栈仍未确定；不承诺已修复所有可能的退出原因。若修复版还出现退出，继续分析新转储并对照是否仍是选屏递归。

## 2026-10-09 · x64 测试包计划

- [x] 冻结当前修复源码与双语帮助，记录版本、清单和基线。
- [x] 从快照构建 x64 自包含便携程序，并复核回归测试。
- [x] 附齐对应许可、双语测试说明，检查 ZIP 与启动探针。
- [x] 交付测试 ZIP，更新实测边界并审查 AGENTS.md。
- 双架构正式包须等待用户本次实测确认；本轮不制作、不提交或推送。

### 构建与包体检查

- 本地目录 `artifacts/color-fix-preview-20261009/`，MinVer 正常派生版本 `1.0.1-alpha.0.1`，构建 `20261009.1`，没有使用版本覆盖或创建标签。基线仍为 `88492d243a65314f013ef1290ff9897543ce63ff`，包含当前未提交修复。
- 冻结 279 个输入文件，清单 SHA256：`5ed1202085e1fef568c6ae0498e7f080e47df3ed4e84bc954b616be7dad9bdd3`。与 20260924.2 正式包快照比较，应用代码只有 `ColorSettingsViewModel.cs` 和 `ColorSettingsViewModel.Preview.cs` 两项差异。
- 从快照执行 `dotnet publish ... -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:SourceRevisionId=snapshot.<清单 SHA256> -p:RuntimeFrameworkVersion=8.0.31 -p:UseSharedCompilation=false -m:1 -nodeReuse:false --ignore-failed-sources` 成功。SDK 8.0.425，包内 .NET／Windows Desktop 8.0.31，与原包相同；保留 NU1900 漏洞数据源不可达警告，不宣称漏洞查询通过。
- ZIP 共 28 个文件，6 个 EXE／原生 DLL 均为 x64；8 个托管运行依赖和 2 个对应架构运行时与前次发布一致，许可与声明齐全。通用中英帮助匹配冻结原稿，额外附本次双语测试说明及 VERSION.txt；不含 Data、配置、恢复记录、日志、转储、测试工具或调试符号。
- ZIP CRC 与解压后逐文件 SHA256 检查通过；原 20260924.2 x64 ZIP 校验值未改变。对解压出的 EXE 执行启动探针，确认 .NET 8.0.31、X64、Portable=true、中英文／WPF 资源存在，信息版本为 `1.0.1-alpha.0.1+snapshot.5ed1202085e1fef568c6ae0498e7f080e47df3ed4e84bc954b616be7dad9bdd3`。探针在 Main 前结束，没有创建 Data 或启动屏幕控制服务，不等于完整 UI 或硬件实测。
- 包名：`ScreenKeeper-1.0.1-alpha.0.1-Test-20261009-Portable-x64.zip`；大小 68,899,545 字节；SHA256 `93ab000e35b0db2ece8e1395dab63b0d68942713e3b32abff53761f1aa3f53be`。`release/SHA256SUMS.txt`、`BUILD-INFO.json` 和 `SOURCE-FILES.json` 保留身份与校验信息。
- 同一快照的 `dotnet test ... -c Release -p:BaseOutputPath=bin/Acceptance/` 完成编译，但首次测试控制器无法在 90 秒内连接 testhost，未执行用例；保留失败记录 `tests/full-x64.trx`。随后在正常本机权限下对同一测试程序集运行 x64 VSTest，不将环境启动失败记作通过。
- 重跑命令 `dotnet vstest artifacts/color-fix-preview-20261009/source/OLED-Sleeper.Tests/bin/Acceptance/Release/net8.0-windows/OLED-Sleeper.Tests.dll /Platform:x64`（设置 `SCREENKEEPER_EXPECT_ARCH=x64`）：596 项中 594 通过、2 项原生测试跳过、0 失败，约 38 秒；结果为 `tests/full-x64-rerun.trx`。本轮没有运行 x86 打包／测试，10 月 8 日的双架构回归记录单独保留。
- 已更新本设计、计划、双语测试说明和文档索引。已检查 AGENTS.md，本次无需更新：沿用既有便携打包、快照身份及恢复保护规则，没有新的架构或持久化变化。未提交、推送或上传；等待用户实机结果后再制作正式包。

## 2026-10-09 · SDR 识别误判修复计划

关联设计中的「新系统 SDR 识别修复」。

- [x] 正常本机权限只读交叉核对旧／新接口，确认两屏实际 SDR；确认运行的是本次测试包，当前未见 f.lux 进程。此时 Windows build 26300 与前一天记录不同，不能仅凭版本变化归因于某次系统升级。
- [x] 加入新接口实际模式判断及旧系统兼容回退，保留所有硬件写入门禁。
- [x] 回归真实 flags 组合、HDR／WCG、未知模式、读取异常、旧系统回退和原生结构布局；执行相关及完整回归。
- [x] 使用更新后端进行双屏只读资格检查，不能把“可开始验证”描述成已通过人工验证。
- [x] 冻结源码并构建独立 x64 测试 ZIP，核验架构、包体与源码身份；保留旧产物。
- [x] 记录交付与未验证边界，审查 AGENTS.md，不提交、推送或制作正式包。

### SDR 修复与验证结果

- `Native/ColorNative.cs` 增加 36 字节 INFO_2 结构和只读入口；`WindowsGammaBackend.cs` 优先使用实际模式，只在错误 1／50／87／120 时保守回退。旧、新读取不一致不接管，不能以旧 bit2 或新接口保留位判断高级颜色已开启。
- 保持已确认 SDR 的作用域指纹；任何非 SDR 或不确定结果（包括回退后的非 SDR／未知状态）均追加指纹后缀，防止验证读回／重新检查原色以旧作用域解除保护。初版内部候选 20261009.2 在审查发现回退非 SDR 指纹边界后被替代，保留并标记 SUPERSEDED，未交付为验收版；本次交付为 20261009.3。
- `AdvancedColorEligibilityTests.cs` 增加 58 个参数展开案例，覆盖本机 legacy=5／INFO2=69／mode=0、支持与启用区别、HDR／WCG、矛盾／未知模式、读取失败、旧系统回退、指纹稳定与结构布局。测试不写真实显示器。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include <本次三个 C# 文件>` 通过；`dotnet build tools/ColorProbe/ColorProbe.csproj -c Release --no-restore -p:BaseOutputPath=bin/AdvancedColor/` 成功。最终后端正常权限只读复核：两屏均 AdvancedColorEnabled=false、IsCloned=false、GammaReadable=true、CanValidate=true；本环境的 SDR 模式及拓扑指纹与修复前相同。独立工具不加载客户端资格，不能据其 Unverified 状态判断客户端资格丢失。
- 从最终快照执行 `dotnet build .../source/OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release -p:BaseOutputPath=bin/Acceptance/` 后，`dotnet vstest <快照测试程序集> /Platform:x64`（SCREENKEEPER_EXPECT_ARCH=x64）共 654 项，652 通过、2 项显式原生测试跳过、0 失败。x86 以私有 8.0.31 运行时执行新增测试及实际进程架构断言，共 59 项通过、0 失败；不等于 Windows 10 x86 真机验收。
- 最终源码快照 280 个输入文件，清单 SHA256 `9877c5be862055c98f66999004234793149bf083408552ea8fe1e9499929170c`；MinVer 派生版本仍为 1.0.1-alpha.0.1，构建 20261009.3，基线提交不变。使用独立 `artifacts/color-fix-preview-20261009.3/`，不覆盖用户正在运行的 20261009.1。
- `dotnet publish ... -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:RuntimeFrameworkVersion=8.0.31 -p:SourceRevisionId=snapshot.<清单>` 成功。构建保留 NU1900（NuGet 漏洞数据源不可达）警告，不宣称漏洞检查通过。未升级依赖。
- ZIP 为 `ScreenKeeper-1.0.1-alpha.0.1-Test-20261009.3-Portable-x64.zip`，68,900,769 字节，SHA256 `83d59ba33f544d21ad9f52381affe9473bc5fa6e3da14900ac25ca6a0396139a`。28 个文件，6 个 EXE／原生 DLL 均 AMD64；ZIP CRC、解压逐文件 SHA256、双语帮助、依赖许可和源清单核验通过。启动探针确认 X64／自包含 .NET 8.0.31／Portable=true／快照身份；在应用 Main 前退出，未创建 Data 或启动硬件服务。
- 本轮未进行实际 Gamma 写入、15 秒验证或长时间稳定性验收，未替换用户运行目录或改动 Data。Win10 不支持新接口的路径由模拟错误码回归，仍需真实旧系统验收；原色冲突、连接变化后的重新验证及外部调色影响继续按原保护流程处理。
- 已同步设计、计划、双语测试说明、文档索引；AGENTS.md 已审查并新增长期的 SDR 实际模式判定与作用域保护规则。未提交、推送或制作正式双架构包。

## 2026-10-13 · 过夜退出与副屏排查

关联设计「关屏过夜后的退出与副屏排查」，详细证据和未确定项见 [诊断记录](018-overnight-display-diagnosis-20261013.md)。

- [x] 阅读作用域规则、README、011／018 历史记录，检查现有 Git 改动；确认指定运行目录的 7 个 EXE／DLL／config 与 20261009.3 原包一致。
- [x] 对照应用最后正常退出、系统关机再开机、自启值和新崩溃报告。用户确认主动重启；本产品自启动项不存在，重启后该目录没有启动日志，不能误判为夜间崩溃。
- [x] 核查 PnP、显示驱动、会话、电源策略和图形报告；区分 AOC、DELL、Coocaa TV 显示标识与贝锐虚拟显卡。
- [x] 复核当前源码与快照：锁屏不退出，没有停用显示输出、创建虚拟屏或关机系统的代码路径。没有证据将本次 AOC 输出未启用归咎于软件当晚持续操作。
- [x] 记录尚不能确认的 HDMI／关屏／驱动恢复原因、人工对照建议和权限限制；不修改系统设置、用户 Data 或运行状态。
- [x] 更新设计、计划、诊断记录和索引。已检查 AGENTS.md，本次无需更新：只有一次性现场证据，没有新的已确认产品或架构规则。未修改生产代码，不运行构建／单元测试，不提交或推送。

## 2026-10-14 · 用户验收与正式包

用户反馈 20261009.3 测试版“验证没什么问题”，并要求重制正式包；版本选择为 1.0.0、以新构建日期区分。应用及测试源码保持与已验收版一致，双架构正式便携包、完整自动化与构建身份见 [015 正式包记录](015-stable-portable-plan.md#20261014-正式交付)。此反馈不扩大为全部显卡／系统环境已验收，也不改变 10 月 13 日 AOC 连接恢复问题尚未确定根因的结论。

## 2026-10-26 · 锁屏关屏后的色温处理提示排查

关联 [设计范围](018-crash-color-investigation-design.md#2026-10-26--锁屏关显示器后反复出现色温处理提示)。

- [x] 读取当前规则与工作区状态，确认既有变更；不覆盖已有修改。
- [x] 核对指定目录、真实进程与开机启动；分别读取两份数据，区分历史与当前现场。
- [x] 检查恢复冲突、验证记录、归档与当前 SDR／作用域；保留不能反推的时间和原因边界。
- [x] 以纯内存模拟验证锁屏、Gamma 被重置回原色及解锁的状态机路径。
- [x] 汇总根因证据与后续修复建议，审查 AGENTS.md，不修改用户配置或屏幕状态。

### 本轮结果

详细证据见 [10 月 26 日诊断记录](018-overnight-color-diagnosis-20261026.md)。

- 指定正式目录 EXE 与发布 payload 一致；当前进程及自启动实际指向 20261009.3 测试版，该 EXE 也与其原始 payload 一致。两版相关代码相同，本轮未修改启动入口。
- 正式目录保留较早的冲突；当前测试目录本次重建归档证明 AOC 当前观察与旧 Baseline 全部 768 值一致、完整 Scope 相同，但旧记录已标记冲突。现场已完成处理，活动记录无冲突。不能从稍后的归档反推冲突发生瞬间的写入者。
- 在 `artifacts/color-overnight-20261026/repro/` 创建隔离诊断程序，ProjectReference 当前真实应用服务，后端、恢复存储及时间全部模拟；不实例化硬件后端、不访问用户 Data、不修改应用或测试源文件。
- 执行 `dotnet build artifacts/color-overnight-20261026/repro/MemoryLockRepro.csproj -c Release --artifacts-path artifacts/color-overnight-20261026/repro/build -p:NuGetAudit=false`：0 警告、0 错误。该参数仅用于临时本地复现构建，不是依赖安全审计，不更改仓库配置或正式构建方式。
- 执行 `dotnet artifacts/color-overnight-20261026/repro/build/bin/MemoryLockRepro/release/MemoryLockRepro.dll`：三组诊断断言通过。锁屏模拟 12 小时期间颜色回到精确原色，解锁立即 Conflict，10 秒后再刷新仍为 Conflict；保持 LastApplied 时保持 Warm；未知外部颜色也进入 Conflict。三组解锁阶段新增设备写入均为 0，强制重验证列表均为空。这里“通过”是成功复现当前缺陷及对照，不能描述为已修复。
- 诊断结果、模拟代码与源文件校验值保存在被忽略的上述目录；`archive-comparison.json` 保存不含完整硬件标识的归档比较。未执行真实锁屏／关电源／Gamma 写入测试，不把模拟 12 小时当作真实过夜验收。
- 已更新 018 设计／计划、诊断报告与文档索引。已检查 AGENTS.md，本次无需更新：尚无生产代码、持久化或已实施的长期行为变更。未提交、推送、替换客户端或重制发行包。

## 2026-10-26 · 解锁原色恢复修复计划

关联 [已批准修复设计](018-crash-color-investigation-design.md#2026-10-26--已批准的解锁恢复修复)。保留前节诊断事实与未验证边界。

- [x] 检查现有变更与服务边界，确定精确作用域、原色所有权及耐久更新顺序。
- [x] 新增隔离回归，覆盖恢复观察、拒绝条件、竞态与真实文件存储。
- [x] 实现仅针对会话恢复的稳定观察及平滑续接，补充双语等待状态。
- [x] 执行相关与双架构完整测试、格式／差异检查及独立审查。
- [x] 同步实际结果及 AGENTS.md 长期约束，明确用户运行文件尚未替换。

### 源码修复与验证结果

- 新增 `ColorTemperatureService.Resume.cs`，主服务接入暂停前记录观察、至少 2 秒稳定采样、精确原色的耐久确认与平滑续接。保持 Baseline 不变；实际写入前仍独立读取，资格和 SameScope 检查不变。临时失败只在这段恢复观察内重试，旧冲突、未完成写入、其他进程会话与未知颜色不自动放行。
- 快速锁／解锁通过请求代次与待观察标记避免漏掉暂停、旧解锁解除新暂停。设置保存／取消发生在耐久确认或最后独立读取时，会重新计时观察；未完成续接的屏幕也不允许预览。独立审查发现的这几个边界均已补齐并覆盖回归，最终复核无阻断问题。
- `ColorSettingsViewModel.cs` 在 Detecting 时保留已验证屏幕与草稿、禁用试调，并显示中英等待提示；`English.json` 补对应译文。没有新增页面或改变布局。
- 新增 `ColorResumeRecoveryTests.cs` 共 **29** 个案例，包含观察时长、已知／未知颜色、拓扑／模式／地址／物理身份／资格变化、离线、读取异常、持久化失败、真实 `ColorRecoveryFileStore` 临时文件链路及并发顺序。新增 1 个双语 UI 状态测试；原有 2 个会话测试调整为先等待稳定观察，再断言原先的恢复结果。
- 修复前先运行最初 19 个回归：15 失败、4 通过，成功暴露原缺陷与边界；初次相关集仍有 2 个旧测试要求解锁立即进入暖色，与新增等待机制不符，已调整对应时间断言。过程结果保留于 `artifacts/color-resume-fix-20261026/tests/`，不冒充最终验收结果。
- 最终 x64 构建及完整测试：`SCREENKEEPER_EXPECT_ARCH=x64`，执行 `dotnet test OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release --artifacts-path artifacts/color-resume-fix-20261026/build --no-restore --logger "trx;LogFileName=full-final-x64.trx" --results-directory artifacts/color-resume-fix-20261026/tests -p:UseSharedCompilation=false -m:1 -nodeReuse:false --verbosity quiet`，**684 总计，682 通过、2 跳过、0 失败**。
- 最终 x86 完整测试：配置已有 `artifacts/x86-toolchain-8.0.31/dotnet` 为 `DOTNET_ROOT_X86`／`DOTNET_ROOT(x86)`、`DOTNET_MULTILEVEL_LOOKUP=0`、`SCREENKEEPER_EXPECT_ARCH=x86`，执行 `dotnet vstest artifacts/color-resume-fix-20261026/build/bin/OLED-Sleeper.Tests/release/OLED-Sleeper.Tests.dll /Platform:x86 "/Logger:trx;LogFileName=full-final-x86.trx" "/ResultsDirectory:<workspace>/artifacts/color-resume-fix-20261026/tests"`，**684 总计，682 通过、2 跳过、0 失败**。测试内断言实际进程架构；启动器显示 x64 不代表测试宿主为 x64。WOW64 自动化不代替原生 Windows 10 x86 真机验收。
- 两架构均跳过 2 个需显式交互桌面条件的既有测试：`ApplicationWindowAccessTests.NativePlacementAndMinimization_WorkOnAnOwnedFixtureProcess`、`HealthReminderWindowTests.ShowingCardDoesNotActivateOrReplaceForegroundFixture`。所有新增色温案例均使用模拟后端，没有写真实 Gamma。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include <本次六个 C# 文件> --verbosity quiet` 通过；首次发现新测试初始化器 4 处空白格式，已修正后复查通过。范围为主服务、新 Resume 文件、ColorSettingsViewModel、ColorTemperatureServiceTests、ColorResumeRecoveryTests、ColorLiveSettingsTests。相关已跟踪文件 `git diff --check` 通过，保留仓库既有 LF／CRLF 提示。
- 已更新本设计／计划、历史诊断的后续说明、文档索引及未发布变更记录；AGENTS.md 新增长期的会话恢复观察与安全边界规则。保留用户所有既有改动；未提交、推送、打标签、发布或打包。
- **尚未验收**：真实锁屏＋关闭显示器电源过夜后的恢复、硬件重枚举与全部驱动差异。实际显示模式／作用域变化或未知颜色仍可能需要处理。当前运行 EXE、用户 Data、自启动入口及历史 ZIP 均未替换；应在后续独立测试包中验证，不能认为此次源码修改已作用于当前运行程序。

## 2026-10-28 · 最新 x64 测试包

关联 [会话恢复测试包设计](018-crash-color-investigation-design.md#2026-10-28--会话恢复-x64-测试包)。

- [x] 检查规则、当前变更和之前包体流程；MinVer 读取仍为 1.0.1-alpha.0.1。
- [x] 冻结构建 20261028.1 的完整源码和双语说明，保存清单及快照身份。
- [x] 从冻结源码执行 x64 回归并发布自包含便携产物。
- [x] 核验依赖许可、原生位数、无业务启动探针、ZIP 完整性和源清单；独立复核成品。
- [x] 记录产物与校验值，交付升级／回退及过夜验收步骤；审查 AGENTS.md。

### 构建 20261028.1 的实际结果

- 本轮只制作 x64 测试 ZIP，没有修改应用／测试代码、依赖、配置结构或版本标签。与 20261014 正式源码比较，应用和测试只有 10 月 26 日修复对应的 5 项修改、2 项新增，无删除。MinVer 实际读取为 `1.0.1-alpha.0.1`，未传版本覆盖。
- 源码冻结于 `artifacts/color-resume-preview-20261028.1/source`，共 282 文件（含双语通用帮助及本次测试说明）；源清单 SHA256 为 `fadc7f0bc9e110512c98c598bf147ca9978d03e60eef4b971d3f5bce4a1d8fca`。基线提交仍为 `88492d243a65314f013ef1290ff9897543ce63ff`，明确包含未提交内容。构建和打包前后逐项核对工作区与快照原文件一致。
- 从快照执行 `SCREENKEEPER_EXPECT_ARCH=x64` 与 `dotnet test <source>/OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release -p:BaseOutputPath=bin/Acceptance/ -p:UseSharedCompilation=false -m:1 -nodeReuse:false --logger "trx;LogFileName=frozen-x64.trx" --results-directory artifacts/color-resume-preview-20261028.1/tests`：**684 总计、682 通过、2 条件跳过、0 失败**；实际位数断言通过。跳过项仍为原有交互桌面窗口测试。本轮未重跑 x86，10 月 26 日双架构结果保留为此前证据。
- 执行 `dotnet publish <source>/OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -o <payload>/ScreenKeeper -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:RuntimeFrameworkVersion=8.0.31 -p:SourceRevisionId=snapshot.<清单 SHA256> -p:UseSharedCompilation=false -m:1 -nodeReuse:false` 成功。SDK 为 8.0.425，本次测试与发布日志没有构建警告；不扩大为完整依赖安全审计。
- 新旧 `ScreenKeeper.deps.json` 的全部非项目依赖相同；保留并逐文件核对 20261014 已核验同版本组件许可。包内包含 LICENSE、THIRD-PARTY-NOTICES、Licenses、双语 README／FAQ、本次测试说明及 VERSION.txt。根许可与帮助均匹配冻结源码，没有用户 Data、日志、签名私钥或调试文件。
- ZIP 为 `artifacts/color-resume-preview-20261028.1/release/ScreenKeeper-1.0.1-alpha.0.1-Test-20261028.1-Portable-x64.zip`，**68,903,288 字节**，SHA256 **`2a4e8cb832e2192bdcb9a74c5ddc8ce238af745d57d13d2f0ada4fa9ac3a3fee`**。共 28 个文件，6 个 EXE／原生 DLL 均为 AMD64，ZIP CRC 和全部解压文件 SHA256 核对通过。历史正式包及 20261009.3 测试 ZIP 的校验值均未变化。
- 解压 EXE 通过现有 StartupHook 探针：外部 .NET 路径指向不存在目录，仍使用自包含 `.NET 8.0.31`；实际进程 X64、Portable=true、中英及 WPF 资源存在、精确 `snapshot.fadc7f...` 身份匹配。FileVersion 为 1.0.1.0，ProductVersion 含完整预发布版本与源清单身份。探针在 Main 前退出，没有创建 Data 或启动业务／调色服务；程序未签名。
- 测试说明见 [20261028 x64 会话恢复测试](../releases/20261028-x64-resume-test.md)，包内同样提供中英版本。要求旧程序托盘退出后复制完整 Data、从新目录启动，避免旧快捷方式／自启动项误启旧版；异常时保留现场，回退前先完成恢复，不覆盖较新的接管记录。
- 已更新本设计／计划、测试说明与文档索引。已检查 AGENTS.md，本次无需更新：使用既有冻结、便携打包、许可与探针流程，没有新增长期架构或构建规则。未提交、推送、上传、替换正在运行的程序、修改用户数据或自启动项。
- 未验证：真实锁屏、物理关显示器及过夜恢复由用户测试；模式／身份改变或未知颜色仍可能显示恢复提示。测试 ZIP 成功不意味着这些实机场景已验收。
- 成品独立只读复核通过：重新核对 ZIP CRC／全部文件、282 个冻结输入、15 份组件许可、TRX、探针和发布目录校验值；4 个历史 ZIP、HEAD 与产品标签均保持原样，没有阻断问题。复核未再次运行 EXE 或测试。
