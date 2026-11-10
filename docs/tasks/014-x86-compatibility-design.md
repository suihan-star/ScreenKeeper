# 014 · Windows x86 兼容与便携交付设计

日期：2026-09-24。关联 [开发计划](014-x86-compatibility-plan.md)。

## 目标与范围

用户确认开发 x86 版本，并由用户进行功能观察验证。目标平台为 Windows 10 x86／x64 与 Windows 11 x64（Windows 11 无 32 位系统版）；x86 程序也在当前 x64 Windows 的 WOW64 环境验证。不扩展到旧 Windows 或 ARM，不把构建通过等同于全部硬件兼容。

在当前目录开发，保留 013 任务及双语帮助的未提交更改；不提交、推送、创建标签、安装替换正在运行的客户端或改动其 Data。保留已有 1.0.0 验收记录和产物，对公共接口改动执行 x64 回归。

## 原因与实现边界

1. NativeMethods 当前直接导入 GetWindowLongPtr／SetWindowLongPtr，32 位 user32 使用 GetWindowLongW／SetWindowLongW。保留公共调用形状，按进程 IntPtr.Size 分派到明确的 W 入口；不能按操作系统位数分派。保留指针宽度和错误码语义。
2. ApplicationWindowAccess 使用 Process.MainModule 读取目标路径，32 位调用方不能可靠读取 64 位目标模块。改用最小查询权限 OpenProcess 与 QueryFullProcessImageNameW，安全释放句柄，失败仍保守回退，不提升权限、不输出路径／标题。窗口会话标识继续使用 PID、启动时间、窗口和类别；不改变匹配及隐私遮挡规则。
3. 审核含指针的输入／物理显示器结构，验证 PHYSICAL_MONITOR 的 Unicode 字段与两种架构的布局，不修改硬件调色、恢复或存储 schema。
4. build.ps1 的便携阶段同时输出 x64／x86，使用独立 staging 目录并复制双语帮助；既有安装器双架构流程保持。使用新的 bin/X86Compat* 与 artifacts 子目录构建／打包，不执行脚本原有的历史产物清理步骤。x64 构建用于回归验证，本轮向用户交付附齐许可和验收说明的 x86 预览 ZIP，原 1.0.0 x64 交付包保持不变。
5. 本次生成明确标识的 1.0.1-preview.1 本地验收包，版本覆盖仅用于构建命令，不更改正式 MinVer 标签策略。附带版本、依赖许可、双语使用说明、FAQ 和 x86 验收说明；不携带配置／恢复记录。

## 顺序、验证与限制

先修正互操作并补回归，再运行 x64 和真实 x86 进程测试，最后发布自包含包与检查 ZIP。测试只操作自行创建的隐藏窗口或 fixture；跨位数测试读取自己启动的 x64 子进程，不操作用户窗口。指定应用原生集成检查显式启用 SCREENKEEPER_NATIVE_QA，仅操作测试窗口；不自动改亮度／Gamma 或执行真实休眠。

验证包括窗口样式读写往返、跨架构进程路径查询、指针／Unicode 结构布局、现有业务测试、PowerShell 语法与打包步骤、输出 PE 架构、包内依赖及 SHA256、解压后的自包含加载。没有 Windows 10 32 位实机／虚拟机时如实保留该项待验收；WOW64 不冒充真实 x86 系统。人工验证由用户在退出原版后从新目录启动，先检查普通 UI 和隐私操作，再使用既有受控入口观察亮度与色温恢复。

## 回退

程序身份、配置 schema 及权限不变。预览包独立目录、使用新建便携 Data；正常退出并完成恢复后可重新运行原 1.0.0。禁止复制未解决的恢复记录或删除旧记录来切换版本。代码回退只撤销 014 变更，不覆盖 013 文档、双语帮助或其他用户更改。

参考：[GetWindowLongPtr](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowlongptra)、[QueryFullProcessImageNameW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew)。
