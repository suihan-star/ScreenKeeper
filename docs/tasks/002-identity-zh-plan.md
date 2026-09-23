# 第一阶段开发计划

关联设计：[002-identity-zh-design.md](002-identity-zh-design.md)

## 实施清单

- [x] 阅读初始化设计、计划及项目约束，确认范围和配置策略。
- [x] 检查工作区；用户选择在当前目录保留已有变更并开发。
- [x] 重跑基线测试：169 通过；MinVer 因沙箱 Git 所有权检查产生警告，后续使用进程级 safe.directory 修正环境。
- [x] 统一应用身份、隔离安装版及便携版数据。
- [x] 同步安装/打包引用，保留配置和上游许可证。
- [x] 完成核心中文文案、时间单位展示及布局核验。
- [x] 执行测试、构建、格式检查和本地产物验证。
- [x] 更新 README，审查并更新 AGENTS.md，记录最终验证与人工验收项。

## 验证记录

基线命令：`C:\Program Files\dotnet\dotnet.exe test OLED-Sleeper.sln -c Release --no-restore`，169 通过，0 失败，0 跳过。

## 人工验收清单

- 首次启动使用独立默认配置；原版配置与启动项不变。
- 主屏/副屏、复制显示器编号正确；切换时间单位后保存重启仍保持正确时长。
- 遮黑、DDC/CI 调暗与唤醒正常；不支持 DDC/CI 时中文提示正确。
- 托盘打开/隐藏/退出、未保存确认、开机启动开关及二次启动唤起正常。
- 无显示器、加载失败、最小窗口、高 DPI、多屏热插拔状态可读可操作。
- 安装、重装、卸载不影响 OLED Sleeper；重装保留 ScreenKeeper 配置。

## 回退

只逐项撤销本任务差异；保留初始化文档和用户已有变更。不使用强制重置，不删除配置。试用产物放在独立 artifacts 子目录，避免覆盖已有安装。

## 最终实现与验证（2026-09-16）

- 运行时标识集中到 `OLED-Sleeper/Infrastructure/ProductIdentity.cs`；程序输出 `ScreenKeeper.exe`。安装/便携数据隔离，未添加配置继承路径。
- 安装 AppId、启动项、互斥量、唤起事件及打包文件名已同步；移除安装时清空设置规则，不再将上游作者/仓库地址作为本产品安装包发布者与支持地址。版权信息仍保留在根 LICENSE、README 和历史文档中。
- 核心文案中文化，时间单位通过转换器显示，内部枚举及 JSON 模型未修改。配置区可滚动，最小窗口 700×500 下保存按钮固定可见；增加空数据/加载失败反馈。
- `dotnet restore OLED-Sleeper.sln --force`：通过。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore`：通过，0 警告、0 错误。
- `dotnet test OLED-Sleeper.sln -c Release --no-build`：173 通过、0 失败、0 跳过；在原有 169 项上新增安装/便携数据隔离及空状态/错误恢复回归用例。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore`：退出码 0，通过。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/phase1-portable/ScreenKeeper`：通过；最终使用相同参数加 `--no-restore` 重新输出。
- PowerShell Parser 检查 `build.ps1`：通过。安装脚本 AppId、互斥量、启动项、无原版数据删除规则的静态断言通过；publish 输出的 LICENSE 与根文件逐字节相同。
- 本地 WPF 渲染工具 `artifacts/phase1-ui/Render.csproj` 使用模拟显示器，不执行硬件管理；实际运行命令 `dotnet run --project artifacts/phase1-ui/Render.csproj -c Release --no-restore`。
- 渲染并查看 11 个场景：遮黑、调暗、700×500 最小窗口、最小窗口滚动到底部、144 DPI 输出、未选择显示器、空数据、加载、加载失败、不支持调暗、校验错误。WPF 绑定日志无错误；实际 ComboBox 选择秒/分钟/小时后 ViewModel 与保存模型一致，毫秒换算正确。
- 截图、渲染工具和本地便携包位于被忽略的 `artifacts/`，不作为源码提交。144 DPI 为离屏渲染，并非实机跨屏 DPI 切换测试。
- 已更新 README 和本任务设计/计划；已审查并更新根 AGENTS.md，沉淀独立身份、数据目录、中文显示与配置保留规则。

## 环境问题与验证边界

- 沙箱用户和仓库所有者不同导致 MinVer 初始警告；验证命令使用进程级 `GIT_CONFIG_COUNT=1`、`GIT_CONFIG_KEY_0=safe.directory`、`GIT_CONFIG_VALUE_0=<workspace>`，未修改全局 Git 配置。
- 沙箱 NuGet 请求最初发生 TLS/NU1301，后续还原曾出现漏洞元数据 NU1900；允许联网的执行环境重跑后还原、构建、测试和 publish 均成功，最终构建无警告。没有关闭漏洞审计或升级依赖。
- 首轮本机未发现 Inno Setup，未能编译安装包；用户随后授权安装工具并已成功编译，见下方环境补齐记录。仍未执行 ScreenKeeper 安装/卸载共存验证。
- 未完成真实桌面托盘、开机启动、第二实例唤起、真实多屏 DDC/CI、热插拔及跨屏 DPI 验收。离屏渲染和模拟文件系统测试不替代上述人工验证。
- 已知上游行为：窗口尺寸变化会重建布局并加载已保存设置，未保存更改的保留需后续专项核验；本阶段不扩展修改工作区状态管理。
- 当前仍采用上游图标及 MinVer 版本规则，安装向导保持英文；完整第三方许可与自动发布迁移仍待后续，不将本包视为正式发行版。
- 保留初始化文档的已有变更；未提交、推送、创建 PR 或发布。

## 本地交付产物

- `artifacts/phase1-portable/ScreenKeeper-phase1-Portable-x64.zip`：66,796,116 字节，9 个文件，ZIP CRC/完整性检查通过；包含 ScreenKeeper.exe、运行所需 DLL、LICENSE 和中文试用说明，不含任何用户配置。
- `artifacts/phase1-portable/SHA256SUMS.txt`：试用压缩包 SHA-256 校验值。
- `artifacts/phase1-ui/normal.png`：核心中文界面预览；同目录包含其余 10 个场景及空的绑定错误日志。
- 最终 `git diff --check` 和 README/任务文档本地链接检查通过；Git 的 LF/CRLF 提示及沙箱用户全局 ignore 文件读取权限提示不影响结果。

## Inno Setup 环境补齐计划（2026-09-16）

关联设计：002-identity-zh-design.md 的“本机 Inno Setup 补齐”。

- [x] 获取官方 Inno Setup 6.7.3，核验 SHA-256 和数字签名。
- [x] 安装后确认 ISCC 路径与版本。
- [x] 构建 ScreenKeeper 本地安装包，记录实际结果；不运行该安装包。
- [x] 更新本机验证记录并审查 AGENTS.md。


### Inno Setup 安装与安装包编译结果

- 来源：[官方下载页](https://jrsoftware.org/isdl.php) 的 Inno Setup 6.7.3；签名验证按[官方说明](https://jrsoftware.org/isdl-verify.php)执行。
- 下载的 SHA-256：`9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732`，与官方 GitHub release asset digest 一致。
- Authenticode：Valid，签名发布者 Pyrsys B.V.。
- 安装参数：`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CURRENTUSER /NOICONS /TASKS=""`，指定当前用户工具目录并保存安装日志；退出码 0。
- 编译器：`%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`。安装日志确认 Inno Setup 6.7.3；`ISCC.exe /?` 可正常调用（该文件本身的 ProductVersion 为 0.0.0.0，不能据此判断工具版本）。
- `./build.ps1 -SkipPortable`：退出码 0，通过。构建脚本自动找到当前用户目录的 ISCC，完成 x64/x86 framework-dependent payload 构建及单一安装包编译；已有便携试用包未受影响。
- 新产物：`artifacts/ScreenKeeper-2.2.0-Setup.exe`，校验文件 `artifacts/ScreenKeeper-2.2.0-SHA256SUMS.txt`。2.2.0 仍由上游历史标签派生，仅为本地构建产物，并非正式发布。
- 编译日志确认 x64/x86 payload 的 LICENSE 均进入安装包；日志保留在 `artifacts/tooling/screenkeeper-installer-build.log`。
- 不运行 ScreenKeeper 安装包，安装/卸载共存、真实硬件行为仍未验证。本轮没有应用源码修改，不重复运行上一轮已通过的单元测试。
- 已检查 AGENTS.md，本次无需更新：Inno Setup 构建依赖早已记录，新增的是本机安装状态；现有构建方式和项目约束未改变。
- 更新了本任务设计与开发计划；没有提交、推送或发布。
