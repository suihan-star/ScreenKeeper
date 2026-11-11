# 项目文档

## 使用与维护

- [项目入口](../README.md)：产品介绍、快速构建和仓库结构。
- [使用指南](user-guide.md)：各模块设置、配置目录、验证与恢复。
- [便携版快速说明](portable/使用说明-README.txt)与[常见问题 FAQ](portable/常见问题-FAQ.txt)：随 ZIP 分发的中英双语离线帮助。
- [开发说明](development.md)：构建、检查、版本、Git 和硬件调试边界。
- [1.0.0 构建 20261014 双语发布说明](releases/1.0.0-20261014.md)：当前正式便携包、修复范围、从旧版／测试版升级及兼容边界。
- [1.0.0 双语发布说明](releases/1.0.0.md)：历史正式便携包与版本身份。
- [2026-10-09 x64 修复测试说明](releases/20261009-x64-test.md)：自动退出修复的历史测试版，后续修复已纳入 20261014 正式构建。
- [2026-10-09 x64 SDR 识别修复测试说明](releases/20261009-x64-sdr-test.md)：构建 20261009.3，在上一测试版上修正新版 Windows 将实际 SDR 误判为高级颜色的兼容问题。
- [首版变更记录](../CHANGELOG.md)：已完成的主要功能与当前交付范围。
- [设计原型说明](../designs/screenkeeper-calm/README.md)：已确认设计及保留的历史对照。
- [产品素材](../OLED-Sleeper/Assets/README.md)：图标、默认背景与来源。
- [上游说明归档](upstream/README.md)：原项目 README；原自动发布脚本留在 `upstream/github-release.yml`，仅供历史参考。

## 任务设计与验收

按时间顺序保留设计、实施及真实验证记录；后续任务中的最终确认优先于早期提案。文档中的本机产物路径用于追溯，产物本身不进入 Git。

| 编号 | 范围 | 设计 | 计划与验收 |
| --- | --- | --- | --- |
| 001 | 仓库初始化与环境 | [设计](tasks/001-bootstrap-design.md) | [计划](tasks/001-bootstrap-plan.md) |
| 002 | 独立身份与中文化 | [设计](tasks/002-identity-zh-design.md) | [计划](tasks/002-identity-zh-plan.md) |
| 003 | 静绿 UI 与双语 | [设计](tasks/003-calm-ui-design.md) | [计划](tasks/003-calm-ui-plan.md) |
| 004 | 图标与系统主题 | [设计](tasks/004-tray-icon-design.md) | [计划](tasks/004-tray-icon-plan.md) |
| 005 | 每屏隐私保护 | [设计](tasks/005-monitor-privacy-design.md) | [计划](tasks/005-monitor-privacy-plan.md) |
| 006 | 设置布局与扩展选项 | [设计](tasks/006-settings-layout-design.md) | [计划](tasks/006-settings-layout-plan.md) |
| 007 | 健康提醒与色温基础 | [设计](tasks/007-wellbeing-design.md) | [计划](tasks/007-wellbeing-plan.md) |
| 008 | 全应用布局与独立菜单 | [设计](tasks/008-app-layout-design.md) | [计划](tasks/008-app-layout-plan.md) |
| 009 | 共用山林背景 | [设计](tasks/009-shared-background-design.md) | [计划](tasks/009-shared-background-plan.md) |
| 010 | 逐屏色温设置 | [设计](tasks/010-color-onboarding-design.md) | [计划](tasks/010-color-onboarding-plan.md) |
| 011 | 遮黑与退出恢复 | [设计](tasks/011-blackout-recovery-design.md) | [计划](tasks/011-blackout-recovery-plan.md) |
| 012 | 设置分区与原色重建 | [设计](tasks/012-settings-recovery-design.md) | [计划](tasks/012-settings-recovery-plan.md) |
| 013 | 首版整理与 Codeup | [设计](tasks/013-first-version-design.md) | [计划](tasks/013-first-version-plan.md) |
| 014 | Windows x86 兼容与便携包 | [设计](tasks/014-x86-compatibility-design.md) | [计划](tasks/014-x86-compatibility-plan.md) |
| 015 | 正式便携版整理（1.0.0 不同构建） | [设计](tasks/015-stable-portable-design.md) | [计划](tasks/015-stable-portable-plan.md) |
| 016 | 客户端官网入口 | [设计](tasks/016-official-website-design.md) | [计划](tasks/016-official-website-plan.md) |
| 018 | 异常退出与色温失效排查 | [设计](tasks/018-crash-color-investigation-design.md) | [计划与证据](tasks/018-crash-color-investigation-plan.md) |
| 019 | macOS Apple Silicon 版（源码开发预览） | [设计](tasks/019-macos-apple-silicon-design.md) | [计划](tasks/019-macos-apple-silicon-plan.md) |
| 020 | GitHub 完整仓库托管与 Mac 构建 | [设计](tasks/020-github-hosting-design.md) | [计划](tasks/020-github-hosting-plan.md) |
| 021 | 开源前隐私清理 | [设计](tasks/021-open-source-privacy-design.md) | [计划](tasks/021-open-source-privacy-plan.md) |

专题记录：[健康与色温研究](tasks/007-wellbeing-research.md)、[显示输出异常排查](tasks/011-aoc-display-diagnosis-2026-09-23.md)。这些记录包含未复现或未定论事项，不应当作已完成修复。

后续排查：[2026-10-13 关屏过夜后软件未运行与 AOC 输出未启用](tasks/018-overnight-display-diagnosis-20261013.md)，已区分主动重启后未自启动与尚未确定根因的副屏连接恢复问题。

色温专项：[2026-10-26 锁屏关屏后反复出现原色处理提示](tasks/018-overnight-color-diagnosis-20261026.md)，区分当前测试版与正式目录、原色重置冲突与连接作用域变化；后续已按用户确认修复源码，稳定观察和精确原色续接见 [018 修复计划](tasks/018-crash-color-investigation-plan.md#2026-10-26--解锁原色恢复修复计划)。正在运行的客户端及历史发行包尚未替换，真实过夜效果待验收。

最新测试包：[20261028.1 x64 会话恢复测试说明](releases/20261028-x64-resume-test.md)。已提供独立便携 ZIP，包含原色恢复修复、完整运行环境和双语帮助；仍待实机验收，正式版入口保持不变。
