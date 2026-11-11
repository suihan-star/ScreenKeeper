# 开发与维护

## 环境与结构

- Windows 与 .NET 8 SDK；应用目标为 `net8.0-windows`，使用 WPF。
- 解决方案保留 `OLED-Sleeper.sln`，应用／测试目录及 `OLED_Sleeper` 命名空间不批量改名；程序输出为 `ScreenKeeper.exe`。
- `Features/` 保存显示器、隐私、健康和色温行为，`UI/` 为设置界面，`Storage/` 为持久化，`Native/` 封装 Windows 访问。
- 不新加运行时依赖或升级现有依赖来完成仓库整理。

## 构建与检查

在仓库根目录执行：

```powershell
dotnet restore OLED-Sleeper.sln
dotnet build OLED-Sleeper.sln -c Release --no-restore
dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore
dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore
```

旧程序正在运行时，使用独立输出目录，避免覆盖已加载文件：

```powershell
dotnet build OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/LocalCheck/
dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore -p:BaseOutputPath=bin/LocalCheck/
```

先运行与变更最接近的检查；首版基线和发布前执行完整现有测试。NuGet 漏洞数据源不可达时记录 NU1900 警告，不能宣称漏洞审计通过。

## 设计与硬件验证

- `designs/` 中原型只模拟交互，不写真实用户配置或显示器；启动预览：`python -m http.server 4311 --bind 127.0.0.1 --directory designs`。
- `SCREENKEEPER_RENDER_QA` 设置为绝对输出目录后，相关测试保存虚构数据的 WPF 截图；截图不能证明真实显示器行为。
- `SCREENKEEPER_NATIVE_QA=1` 显式启用桌面集成测试，只操作测试创建的窗口；无交互桌面时跳过。
- 真实调色测试需明确屏幕、用户观察时段和恢复看护，使用 [ColorProbe](../tools/ColorProbe/README.md) 或客户端受控验证入口。
- 不为通过测试删除用户恢复记录、替换资格文件或清空原色备份。

## 版本与发布

MinVer 使用本产品专属的 `screenkeeper-v` 标签前缀，与保留的上游 `v*` 标签区分。用户确认首版为 `1.0.0`，对应 `screenkeeper-v1.0.0`；决定与验证记录见 [013 计划](tasks/013-first-version-plan.md)。克隆时保留产品标签，避免浅克隆遗漏版本信息；常规未打本产品正式标签的构建保留预发布标识。用户明确指定版本的归档构建可显式传入 MinVerVersionOverride，但必须附精确源码快照、逐文件清单及独立构建身份，不移动既有标签，也不能将工作区更改冒充已提交源码。1.0.0 构建 20260924 见 [015 记录](tasks/015-stable-portable-plan.md)，新增官网入口的构建 20260924.2 见 [016 记录](tasks/016-official-website-plan.md)。

首版交付推送源码 `main` 和产品标签 `screenkeeper-v1.0.0`，未推送上游标签或自动创建线上发行版。完整项目现已迁移到 GitHub 主仓库，保留原产品版本记录与上游归属，迁移记录见 [020 计划](tasks/020-github-hosting-plan.md)。托管迁移不代表发布新版本；以后正式发布须明确版本、验收和标签范围。

- `build.ps1` 调用 Inno Setup 生成双架构安装器和独立的 x64／x86 自包含便携 ZIP，默认写入仓库 `artifacts/`；便携 staging 位于 `publish/portable/x64/ScreenKeeper` 与 `publish/portable/x86/ScreenKeeper`。
- 便携 ZIP 压缩前从 `docs/portable/*.txt` 复制双语快速说明和 FAQ；这里是随包帮助的维护来源。直接使用 `dotnet publish` 时不会运行该脚本步骤，手动重打包须复制这两份文档并重新计算 ZIP 校验值。
- 当前安装器脚本读取默认 `artifacts/publish` 布局；先使用默认输出目录，不把自定义 `OutputDirectory` 当成已验证功能。
- 新版发布前核对许可证、安装／升级／卸载、硬件恢复、不同 DPI 和客户端签名；当前没有宣称安装包已签名。
- GitHub 主仓库的 `.github/workflows/ci.yml` 在面向 `main` 的 PR 或手动触发时执行 Windows 构建、测试和格式检查。`release.yml` 手动构建 Windows 安装器／便携包，`mac-preview.yml` 手动构建 Apple Silicon Mac 开发预览；两个打包工作流只上传产物，不创建 Release。Codeup 不执行这些工作流，若继续使用其构建服务需另行配置流水线。

## x86 兼容检查

x86 兼容设计、双架构检查和人工验收边界见 [014 开发计划](tasks/014-x86-compatibility-plan.md)。窗口指针 API 按进程位数分派；不能因为宿主是 x64 系统就调用 64 位入口。读取其他应用路径使用最小查询权限的 QueryFullProcessImageName，不用跨位数模块枚举。WOW64 测试不等同于 Windows 10 x86 系统或真实显示器验收。

## Git 与数据边界

`origin` 是 [GitHub 主仓库 suihan-star/ScreenKeeper](https://github.com/suihan-star/ScreenKeeper)，`upstream` 仍是上游参考仓库。功能分支默认 `codex/`，主分支为 `main`；只向明确的目标推送指定分支与产品标签，不强推或重写继承历史。

- 源码、测试、设计原稿、必要素材和任务记录进入 Git。
- 构建目录、运行数据、日志、恢复记录、私钥／证书及个人工具状态被忽略；忽略不等于删除本地文件。
- SSH 私钥只由本机 SSH 客户端用于认证，不能写进仓库或日志；首次主机信任需确认目标，已有记录发生变化时停止连接。
- 保存／恢复配置 schema、应用身份和生命周期约束详见 [AGENTS.md](../AGENTS.md)，各次实现与限制详见 [任务索引](README.md)。

公开协作请使用 GitHub 公开身份与 noreply 邮箱提交；个人路径、私有仓库地址和本地审计备份不得进入源码或历史。隐私清理过程与旧版本溯源边界见 [021 计划](tasks/021-open-source-privacy-plan.md)。
