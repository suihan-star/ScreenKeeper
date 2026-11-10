# 屏幕良伴 · ScreenKeeper 64 位测试版

版本：1.0.1-alpha.0.1；构建：20261028.1。适用 Windows 10／11 x64。

本包包含锁屏关屏后的色温恢复修复，供实机测试，不是新的正式稳定版。

## 如何换到测试版

1. 从旧版右下角托盘菜单选择“退出”，等待退出与恢复流程完成。关闭设置窗口只会隐藏到托盘。
2. 将本 ZIP 完整解压到新文件夹，保留旧版整个目录。不要仅复制 EXE，也不要直接在压缩软件内启动。
3. 如需沿用本机设置，在两份程序均已退出时，将**刚刚使用的旧程序旁完整 Data 文件夹复制**到新 ScreenKeeper.exe 旁。结果应为 `ScreenKeeper/Data/ScreenKeeper/`。保留所有恢复、验证和图片文件，不只复制 settings.json，不使用另一台电脑的数据。
4. 从新目录双击 ScreenKeeper.exe；核对同目录 VERSION.txt 的构建号为 **20261028.1**。不要使用旧快捷方式；两版共用单实例标识，旧版未退出时点新 EXE 可能只唤起旧版。
5. 包内已包含运行环境，无需安装 .NET。本次打包不会修改开机启动入口；重启后请核对启动的是新目录。测试期间保持只有一份屏幕良伴运行。

## 本次修复与保留的保护

锁屏后，如果系统将颜色复原，软件现在先等待连接与读数稳定。在屏幕身份、验证资格、显示模式均匹配且颜色精确等于记录原色时，保留原色记录并平滑恢复已保存策略，避免将已知原色误判为未知改色。短暂读取失败可以重新观察；快速锁屏／解锁、同时保存设置或取消试调时仍会复核。

等待时可能显示“正在等待屏幕连接和原色读数稳定。”。恢复后通常需要数秒等待和约 10 秒渐变，不要求立即恢复颜色。两次有效观察至少间隔 2 秒，后台轮询间隔可能使实际等待更长。

旧冲突、未知外部颜色、真正的显示模式／连接身份变化及未完成写入仍会保留处理提示。本包不会删除记录、自动授予验证资格，也不承诺修复 NVIDIA 控制面板中显示输出被取消启用的问题。

## 建议测试顺序

1. 先确认所选屏幕的已保存色温能正常生效；不需要仅因为换包就重新验证。测试色温时退出 f.lux 等其他调色工具，关闭 Windows 夜间模式。
2. 做一次短测：Win+L 锁屏，关闭显示器电源，稍后开机解锁。等连接稳定后观察色温是否恢复，以及是否仍出现“查看处理步骤”。
3. 再按平时习惯锁屏、关闭显示器，过夜后观察。若当前作息选择“保持原色”，恢复原色是正常结果，应以当时保存的作息目标判断。
4. 如再次出现提示或色温消失，先记下时间、屏幕型号及完整提示，保留 `Data/ScreenKeeper/Logs` 和恢复记录。先反馈现场，不反复重建原色或删除记录来消除提示。

启动时已有处理提示的屏幕，仍按软件原流程处理；本包不会自动清除历史冲突。开启功能与保存策略仍由用户决定。

## 回退与帮助

切回旧版前，在测试版关闭色温／屏幕休息并保存，确认颜色和亮度恢复后从托盘退出。恢复有冲突时先保留现场并处理提示，不用旧备份覆盖较新的恢复记录。格式未变化；同机回退如需保留设置，应在完全退出后保留并复制测试版最新的完整 Data，避免重新使用已经过期的接管记录。

本包未签名。自动化验证不代替实际显卡与显示器验收。通用双语帮助见“使用说明-README.txt”和“常见问题-FAQ.txt”。

---

# ScreenKeeper x64 test build

Version: 1.0.1-alpha.0.1. Build: 20261028.1. Windows 10/11 x64.

This prerelease tests color recovery after session lock and display power-off. It is not a new stable release.

## Update on the same PC

1. Exit the old copy from its tray menu and complete any recovery prompts. Closing settings only hides the app.
2. Extract the entire ZIP into a new folder. Keep the old folder intact and all DLLs beside the new EXE.
3. With both copies closed, COPY the complete Data folder from the copy you were actually using beside the new ScreenKeeper.exe. The resulting path is `ScreenKeeper/Data/ScreenKeeper/`. Keep all recovery, validation and image files; do not copy only settings.json or use another PC's data.
4. Launch from the new folder and check VERSION.txt for build **20261028.1**. An old shortcut or an old running instance can reopen the old copy instead.
5. No .NET installation is required. Packaging does not change your startup entry; after a reboot, check which folder was launched. Run one copy at a time.

## What changed

After unlock, the app waits for stable connection and color readings. When the validated display scope still matches and the color exactly equals the recorded original baseline, it retains that baseline and smoothly resumes the saved policy. Temporary observation failures can be retried; rapid lock/unlock, settings changes and preview cancellation remain guarded.

The waiting message is “Waiting for the display connection and original-color readings to stabilize.” Allow several seconds for observation followed by about 10 seconds of transition. Two valid samples must be at least two seconds apart; polling can make the actual wait longer.

Existing conflicts, unknown external colors, changed display scope/mode and incomplete writes remain protected. This build does not delete records, grant validation automatically, or claim to fix a display becoming unselected in NVIDIA Control Panel.

## Test and report

Confirm that the saved policy currently works; updating alone does not require revalidation. Exit other color tools and turn off Windows Night Light. First try a short lock, display power-off, power-on and unlock cycle. Then try your normal overnight routine. Compare the result with the saved schedule: an original-color period should remain original.

If recovery prompts or lost color recur, record the approximate time, display model and complete message. Keep Data/ScreenKeeper/Logs and all recovery records. Report the state before repeatedly re-establishing a baseline; never delete records to remove warnings. Pre-existing recovery problems still require the existing in-app flow.

Before returning to the old build, disable and save color and screen-rest controls in this test build, confirm restoration, and exit via the tray. Preserve unresolved recovery evidence; do not overwrite newer records with an old backup. The format is unchanged. If retaining settings when reverting on the same PC, copy the latest complete Data only after the test build has fully exited and restored ownership, rather than reusing stale takeover records.

This build is unsigned. Automated checks do not replace real overnight hardware testing. See the bundled bilingual README and FAQ for general usage.
