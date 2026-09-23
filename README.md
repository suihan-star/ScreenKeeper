# 屏幕良伴 · ScreenKeeper

面向 Windows 多显示器用户的屏幕管理工具：让闲置屏幕休息、保护旁人不宜看到的内容，并提供健康提醒与逐屏色温设置。

首版版本号为 **1.0.0**，本仓库保留源码、测试、设计稿和开发记录。实际硬件兼容与发布验收边界见 [首版记录](CHANGELOG.md) 和 [验证结果](docs/tasks/013-first-version-plan.md)。

## 主要功能

- **屏幕休息**：每屏设置空闲时长、遮黑／调暗、保持唤醒条件与临时暂缓。
- **隐私保护**：隐私幕、回到桌面、指定应用；可选使用免打扰、跨屏移动和自定义背景。
- **健康提醒**：独立工作／休息周期、渐显山林或动态雾光、开始休息／稍后／跳过。
- **色温调节**：验证后逐屏启用，设置工作／休息作息与原色／自定义色温，保留恢复保护。
- **界面与效率**：简体中文／英文、深绿／雾白绿、显示器型号识别、批量应用设置及托盘快捷操作。

[使用指南](docs/user-guide.md) · [开发说明](docs/development.md) · [文档索引](docs/README.md) · [设计原型](designs/screenkeeper-calm/README.md)

## 快速开始

构建环境：Windows、.NET 8 SDK。生成安装包还需要 Inno Setup 6。

```powershell
dotnet restore OLED-Sleeper.sln
dotnet build OLED-Sleeper.sln -c Release --no-restore
dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore
dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore
```

运行 `OLED-Sleeper/bin/Release/net8.0-windows/ScreenKeeper.exe`。首次启动各管理功能默认关闭，按需开启并保存；窗口关闭按钮隐藏到托盘，完全退出请使用托盘菜单。

生成本地安装包与便携包：

```powershell
.\build.ps1
```

产物写入 `artifacts/`，不进入 Git。该命令只生成本地文件，不上传或发布。

## 配置与恢复

- 普通／安装版：`%APPDATA%\ScreenKeeper`。
- 便携版：程序旁 `Data\ScreenKeeper`；移动程序时一同保留该目录。
- 本产品使用独立身份和数据，不继承 OLED Sleeper 配置；重装保留自身配置与恢复记录。
- 色温首次启用需要用户观察验证；外部调色冲突时保留记录，不能删除记录绕过保护。
- 隐私幕用于减少意外暴露，不替代 Windows 锁屏或访问控制。详细适用范围见 [使用指南](docs/user-guide.md)。

## 仓库结构

| 路径 | 内容 |
| --- | --- |
| `OLED-Sleeper/` | ScreenKeeper WPF 应用；历史目录名和命名空间保留 |
| `OLED-Sleeper.Tests/` | 自动化测试与可选渲染／原生验收 |
| `designs/screenkeeper-calm/` | 可点击原型、设计源文件与素材 |
| `docs/` | 使用、开发、任务设计与验收记录 |
| `tools/` | 图标生成与受控色温观察工具 |
| `installer/`、`build.ps1` | 安装器及本地打包入口 |
| `.github/workflows/` | 可选 GitHub 镜像检查／手动构建；Codeup 不执行这些工作流 |

## 源码仓库与贡献

产品主分支为 `main`，功能分支使用 `codex/` 前缀。Codeup 为 `origin`：

```text
<private-repository>
```

`upstream` 保留 [Quorthon13/OLED-Sleeper](https://github.com/Quorthon13/OLED-Sleeper)。本产品从上游 `v2.2.0`（`27f1c777a9a1dca6002692b66fe0ec6cf4d71724`）开始独立开发，保留完整历史。版本策略、验证要求和发布边界见 [开发说明](docs/development.md)；开发前阅读 [AGENTS.md](AGENTS.md)。

## 许可与来源

保留上游 [MIT License](LICENSE) 和原作者版权声明，LICENSE 随构建／打包输出。原 README 和自动发布工作流保存在 [上游归档](docs/upstream/README.md)。图标与山林背景来源见 [素材说明](OLED-Sleeper/Assets/README.md)，设计稿 React 许可见 [第三方许可文件](designs/screenkeeper-calm/vendor/LICENSE-react.txt)。正式分发前仍需补齐随包组件与运行时许可核验，不把本次源码提交当作完整发行验收。
