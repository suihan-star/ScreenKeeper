# 多屏管家仓库初始化计划

关联设计：[001-bootstrap-design.md](001-bootstrap-design.md)

## 实施

- [x] 确认父级约束、原仓库状态以及目标目录为空。
- [x] 远程确认 v2.2.0 指向 27f1c777a9a1dca6002692b66fe0ec6cf4d71724。
- [x] 独立克隆，保留完整历史及标签，建立 main，配置 upstream。
- [x] 核对 v2.2.0 的 MIT LICENSE 和项目文件。
- [x] 添加 README 产品说明、任务设计、计划和 AGENTS.md。
- [x] 完成最终 Git 状态、历史、差异与文档链接检查。
- [x] 验证 .NET 构建、测试和格式检查基线：已安装 SDK，结果见下方本机验证记录。

## 验证命令与记录

- git ls-remote 上游 refs/tags/v2.2.0：标签已确认。
- git rev-parse HEAD v2.2.0^{commit}：两个提交一致。
- git rev-parse --is-shallow-repository：false，非浅克隆。
- git remote -v：只有 upstream，没有 origin。
- dotnet --info：未发现 SDK；当前 PATH 中的运行时为 .NET 5.0.11。

仓库现有 CI 命令（本机安装 SDK 后已执行，结果见下方）：

```powershell
dotnet restore OLED-Sleeper.sln
dotnet build OLED-Sleeper.sln -c Release --no-restore
dotnet test OLED-Sleeper.sln -c Release --no-build
dotnet format OLED-Sleeper.sln --verify-no-changes
```

不以文档检查代替应用构建、测试或 UI 验证。
没有构建安装包，没有验证多显示器硬件行为。

## 交付边界

本次修改仅限初始化文档。远程产品仓库、品牌替换、独立应用身份、
许可证打包和发布流程迁移尚待后续任务。
已执行 AGENTS.md 维护审查，并新增根目录规则以记录产品来源及真实开发约束。

## 仓库初始化阶段验证结果（安装 SDK 前）

- git fsck --connectivity-only：通过，历史连通性正常。
- git rev-list --count HEAD：197，与原仓库 v2.2.0 可达提交数量一致。
- 原仓库 git status --short：无变更，HEAD 未改变。
- 对比 LICENSE、应用、测试、installer、build.ps1 和 .github：与 v2.2.0 无差异。
- README 的本地文档链接目标均存在。
- dotnet restore OLED-Sleeper.sln：失败，明确提示没有已安装的 .NET SDK。
  未继续执行 build、test 或 format；属于环境阻塞，不是代码测试失败。
- 首轮 diff --check 发现 README 文件尾多余空行，已修正后复查。

## 本机 SDK 安装与基线验证
- [x] 获取微软官方 .NET 8 SDK，核验校验和及签名。
- [x] 执行本机安装并确认 SDK/桌面运行时。
- [x] 运行 restore、Release build、test、format 检查并记录结果。


## SDK 安装后验证结果（2026-09-16）

- 官方元数据：https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json
- 已安装 .NET SDK 8.0.425（Windows x64），目录 C:\Program Files\dotnet\sdk。
- 安装包 SHA-512 与官方元数据一致，Authenticode 签名有效且签名人为 Microsoft Corporation。
- 安装器使用 /install /quiet /norestart，退出码 0，没有要求重启。
- dotnet --version：8.0.425。
- 已安装 Microsoft.NETCore.App、Microsoft.AspNetCore.App、Microsoft.WindowsDesktop.App 8.0.31。
- 已有 .NET 5.0.11 运行时保留。
- dotnet restore OLED-Sleeper.sln：通过。
- dotnet build OLED-Sleeper.sln -c Release --no-restore：通过，0 警告、0 错误。
- dotnet test OLED-Sleeper.sln -c Release --no-build：169 通过、0 失败、0 跳过。
- dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore：退出码 0，通过，无源码修改。
- git diff --check：通过。
- 本次未运行应用 UI、多显示器硬件测试或安装包构建。
- 已审查 AGENTS.md，将开发工具要求明确为 .NET 8 SDK；安装版本和本机状态仅记录在此计划中。
- 未提交、推送或发布。
