# 项目文档

## 使用与维护

- [项目入口](../README.md)：产品介绍、快速构建和仓库结构。
- [使用指南](user-guide.md)：各模块设置、配置目录、验证与恢复。
- [开发说明](development.md)：构建、检查、版本、Git 和硬件调试边界。
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

专题记录：[健康与色温研究](tasks/007-wellbeing-research.md)、[显示输出异常排查](tasks/011-aoc-display-diagnosis-2026-09-23.md)。这些记录包含未复现或未定论事项，不应当作已完成修复。
