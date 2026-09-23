# 上游说明归档

以下原文来自 OLED Sleeper v2.2.0，用于保留项目来源和历史参考，仅清理行尾空白。下载、问题反馈与更新说明属于上游产品，不代表 ScreenKeeper。原版权与许可见仓库根目录 LICENSE。

---

# OLED Sleeper 😴 – Blackout or Dim Secondary Monitors on Windows

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

OLED Sleeper is a lightweight Windows tool to blackout or dim idle monitors, helping users prevent OLED burn-in and temporarily sleep secondary monitors for focus, gaming, or distraction-free work.

<p align="center">
  <img src="https://github.com/user-attachments/assets/0f7c9110-094c-4fdb-8109-62fdd11e87cd" alt="OLED Sleeper Demonstration">
</p>

---

## The Problem

Many users have multi-monitor setups but want to turn off or dim secondary monitors temporarily without putting the entire computer to sleep. OLED and other displays can also suffer from burn-in or image retention if static images stay on screen too long. Windows’ built-in power settings are all-or-nothing — there’s no per-monitor control.

## The Solution

OLED Sleeper monitors each screen for activity. When a monitor is idle for a set time, it will either black it out or dim its brightness based on your preference.

<p align="center">
  <img width="500" height="410" alt="oled-sleeper-002" src="https://github.com/user-attachments/assets/20022234-fe5f-4573-a4e4-ff5ef07b622b" />
</p>

---

## Features

* **Three Idle Detection Modes:** Customize how the application determines if a monitor is idle:
    * **Mouse:** Tracks cursor movement specifically on the target monitor.
    * **Focused Application:** Tracks activity within the active window currently displayed on that monitor.
    * **System-Wide Input:** Tracks overall keyboard and mouse input across the entire system (similar to standard Windows idle detection).
* **Per-Monitor Control:** Blackout or dim any monitor independently.
* **Two Action Modes:** Full blackout or dimming (DDC/CI supported).
* **Instant Wake-Up:** Restore the monitor immediately when activity is detected.
* **Native WPF Application:** Built from the ground up using native Win32 calls. Requires no external dependencies or third-party tools.

---

## Requirements

* **Operating System:** Windows 10 or 11
* **DDC/CI Support (for Dimming Mode):** Dimming requires a monitor that supports DDC/CI brightness control via VCP codes. Most modern monitors support this, but it is not guaranteed on all displays.

---

## How to Use

1. Download the latest installer or the portable zip from the [Releases page](https://github.com/Quorthon13/OLED-Sleeper/releases).
2. Run the installer and follow the on-screen prompts. During installation, you will be prompted to configure automatic startup and create shortcuts. For the portable version, just extract the zip anywhere and run it.
3. Open OLED Sleeper from your Start Menu or desktop shortcut.
4. Use the interface to select your target monitors, choose your preferred idle detection mode, and set your idle timers.
5. Apply your settings. The application will minimize to the system tray and run in the background.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 当前本地开发版：布局 v5

ScreenKeeper 已实施屏幕休息／隐私保护／健康提醒／色温调节的独立导航，以及深绿（默认）／雾白绿主题。主题位于顶部语言旁，通用设置中的预览卡同步切换。每个模块分别保存，切换主题不影响业务草稿。

健康提醒分为提醒节奏与提醒外观；色温分为作息与色温、屏幕与验证。真实改色仍须经过原有的每屏验证，布局更新不会自动启用或迁移其他产品配置。

设计与验证：[008 设计](docs/tasks/008-app-layout-design.md)、[008 计划](docs/tasks/008-app-layout-plan.md)。本地构建：`dotnet build OLED-Sleeper.sln -c Release`，运行 `OLED-Sleeper/bin/Release/net8.0-windows/ScreenKeeper.exe`；需要 .NET 8 Windows Desktop Runtime。该开发输出未打包、安装或发布。上方上游使用说明仅作为来源历史保留。

## 提醒卡与隐私幕交互调整（2026-09-21）

隐私幕按钮统一为「进入此屏幕」。健康操作卡居中并适度放大，正文为「专注了这么久，辛苦啦。眨眨眼，伸个懒腰，让紧绷的自己放松片刻...」。卡片标题居中；开始休息使用独占一行的浅绿圆角长按钮，下方稍后使用暂停方块＋文字、跳过本次使用向右箭头＋文字，悬停可查看操作结果；轻提醒仍不抢焦点。已确认 A 方案的可点击效果稿位于 `designs/screenkeeper-calm/health-card-v4.html`，详情见 [007 设计](docs/tasks/007-wellbeing-design.md) 与 [验证记录](docs/tasks/007-wellbeing-plan.md)。
