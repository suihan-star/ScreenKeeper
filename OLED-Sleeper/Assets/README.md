# ScreenKeeper A+「叠屏静夜」

用户确认的产品标识：叠屏轮廓＋前屏月牙。图标不代表保护状态。

- `icon.svg` / `icon.ico`：深绿 #315F43，用于浅色任务栏、程序文件及安装器。
- `icon-dark.svg` / `icon-dark.ico`：浅绿 #B4D6A1，用于深色任务栏及静绿窗口标题栏。
- 每个 ICO 包含 16、20、24、32、40、48、64、128、256 像素透明 PNG 帧。
- 在 Windows 仓库根目录执行 `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tools/Generate-Icons.ps1` 可重新生成。ExecutionPolicy 参数仅作用于本次进程，不修改系统策略。无需额外软件包。
- SVG 为设计源文件，生成脚本支持本图标使用的路径和月牙变换；修改轮廓后同步生成两个 ICO，并运行 ApplicationIconTests。

运行中的任务栏按钮与托盘均采用第六轮确认的 A「雾绿轮廓」，通过 `UI/Helpers/SystemTaskbarTheme` 共用 Windows `SystemUsesLightTheme` 读取逻辑，并监听系统偏好更新。深色任务栏使用浅绿，浅色任务栏使用深绿；读取失败使用浅绿。`MainWindow.Icon.cs` 在窗口显示前加载对应 ICO，隐藏时继续跟随系统、真正关闭时解除订阅。客户端内部标题栏图标仍按应用主题切换，两者相互独立。

未运行时固定快捷方式、程序文件和安装器使用静态图标，由 Windows 管理；本次运行时适配不修改用户固定快捷方式。Windows 资源管理器可能缓存旧程序／快捷方式图标，不应通过删除用户图标缓存处理。

设计与验收记录见 `../../docs/tasks/004-tray-icon-design.md` 和 `../../docs/tasks/004-tray-icon-plan.md`（仓库根目录）。

当前版本已撤销提亮，改为扩大图像内的轮廓占比：几何边界由 15×13 扩为 16×16，前屏和月牙同步放大，托盘槽位由 Windows 管理。

## 共用山林背景

`forest-mist.png` 为用户提供并确认采用的官网山林图，原附件名 `codex-clipboard-a667ad70-449a-4a1d-a8c3-f067a6e676d3.png`。原图 1672×941，未做像素修改；客户端叠加轻微深绿遮罩。SHA256：`F63B615B8860F85967B5E47F54C7B0C59B39F04EB3D60EC2475B50D2609087EC`。

资源嵌入程序集，经 UI/Helpers/BuiltInBackground 的冻结缓存供隐私幕和健康提醒共用，无外部路径或联网依赖。designs/screenkeeper-calm/forest-mist.png 为同一原图的原型副本。用途及兼容约定见 docs/tasks/009-shared-background-design.md。此处记录用户提供来源，不推断额外的第三方许可。
