using System.Collections.Generic;
using PlayPane.Core.Models;

namespace PlayPane.Core.Services
{
    public sealed class LocalizationService
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "App.Ready", "Ready" },
            { "Common.Cancel", "Cancel" },
            { "Common.Confirm", "Confirm" },
            { "Common.Reset", "Reset" },
            { "Common.Settings", "Settings" },
            { "Language.English", "English" },
            { "Language.SimplifiedChinese", "Simplified Chinese" },
            { "Main.Title", "PlayPane" },
            { "Main.Subtitle", "Mirror a browser window as a game-friendly overlay" },
            { "Main.RestorePrevious", "Restore Previous Session" },
            { "Main.Settings", "Settings" },
            { "Main.Refresh", "Refresh" },
            { "Main.ShowAllWindows", "Show all windows" },
            { "Main.ColumnBrowser", "Browser" },
            { "Main.ColumnTitle", "Title" },
            { "Main.ColumnProcess", "Process" },
            { "Main.ColumnMonitor", "Monitor" },
            { "Main.SourcePreview", "Source Preview" },
            { "Main.CaptureSource", "Capture source" },
            { "Main.WindowCaptureSource", "Window Capture" },
            { "Main.ExtensionCaptureSource", "Chrome/Edge Extension Capture" },
            { "Main.ExtensionWaiting", "Extension capture server will start when the overlay starts." },
            { "Main.ExtensionServerReady", "Extension server ready at {0}. Start PlayPane Capture in Chrome or Edge." },
            { "Main.Mirroring", "Mirroring" },
            { "Main.FullWindow", "Full window" },
            { "Main.CropRegion", "Crop region" },
            { "Main.EditCrop", "Edit Crop" },
            { "Main.FrameRate", "Frame rate" },
            { "Main.FrameRateLow", "Low Resource - 15 FPS" },
            { "Main.FrameRateStandard", "Standard - 30 FPS" },
            { "Main.FrameRateSmooth", "Smooth - 60 FPS" },
            { "Main.Opacity", "Opacity" },
            { "Main.LockAspectRatio", "Lock aspect ratio" },
            { "Main.SourcePlacement", "Source window placement" },
            { "Main.KeepOriginalPosition", "Keep original position" },
            { "Main.MoveRightEdge", "Move to right edge" },
            { "Main.MoveMostlyOffScreen", "Move mostly off-screen" },
            { "Main.StartOverlay", "Start Overlay" },
            { "Main.StopMirroring", "Stop Mirroring" },
            { "Main.RestoreSource", "Restore Source Window" },
            { "Status.ControlPanelMinimized", "Control panel minimized to the system tray." },
            { "Status.HotkeysFailed", "Some global shortcuts could not be registered. Open Settings to review them." },
            { "Status.SourceRestored", "Source window position restored." },
            { "Status.WindowsFound", "{0} capturable windows found." },
            { "Status.SelectSourceFirst", "Select a browser window first." },
            { "Status.OverlayStarted", "Overlay started in Edit Mode." },
            { "Status.MirroringStopped", "Mirroring stopped." },
            { "Status.NoPreviousSource", "No previous source window is saved." },
            { "Status.PreviousSourceNotFound", "Previous source window was not found. Select another window." },
            { "Status.SelectSourceBeforeCrop", "Select a source window before editing the crop region." },
            { "Status.CropUpdated", "Crop region updated." },
            { "Status.SettingsSaved", "Settings saved." },
            { "Status.RecoveredSource", "Recovered previous source window position." },
            { "Status.OverlayHidden", "Overlay hidden. Use Ctrl + Alt + O or tray menu to show it." },
            { "Status.ExtensionClientConnected", "Browser extension connected." },
            { "Status.ExtensionClientDisconnected", "Browser extension disconnected." },
            { "Status.ExtensionFrameReceived", "Browser extension frames are being received." },
            { "Recovery.Prompt", "A previous mirroring session did not close correctly. Restore the source browser window position?" },
            { "Recovery.Title", "PlayPane Recovery" },
            { "Tray.ShowControlPanel", "Show control panel" },
            { "Tray.ShowOverlay", "Show overlay" },
            { "Tray.HideOverlay", "Hide overlay" },
            { "Tray.EnterEditMode", "Enter Edit Mode" },
            { "Tray.EnterGameMode", "Enter Game Mode" },
            { "Tray.StopMirroring", "Stop mirroring" },
            { "Tray.RestoreSource", "Restore source window position" },
            { "Tray.Settings", "Settings" },
            { "Tray.Exit", "Exit PlayPane" },
            { "Overlay.Title", "PlayPane Overlay" },
            { "Overlay.Opacity", "Opacity" },
            { "Overlay.Crop", "Crop" },
            { "Overlay.Lock", "Lock" },
            { "Overlay.Hide", "Hide" },
            { "Overlay.Stop", "Stop" },
            { "Overlay.SourceMinimized", "The source browser is minimized. The mirrored image may stop updating." },
            { "Overlay.FramePaused", "The mirrored image may be paused. Restore the source browser window." },
            { "Crop.Title", "Crop Source Region" },
            { "Settings.Title", "PlayPane Settings" },
            { "Settings.Startup", "Startup" },
            { "Settings.Language", "Language" },
            { "Settings.AutoRestore", "Automatically restore the previous session on startup" },
            { "Settings.StartWithWindows", "Start with Windows" },
            { "Settings.GlobalShortcuts", "Global shortcuts" },
            { "Settings.ShortcutToggleOverlay", "Ctrl + Alt + O: Show or hide overlay" },
            { "Settings.ShortcutToggleMode", "Ctrl + Alt + E: Switch Edit/Game Mode" },
            { "Settings.ShortcutOpacity", "Ctrl + Alt + Up/Down: Adjust opacity" },
            { "Settings.ShortcutCrop", "Ctrl + Alt + R: Reconfigure crop" },
            { "Settings.ShortcutStop", "Ctrl + Alt + Q: Stop mirroring" },
            { "Settings.Save", "Save" }
        };

        private static readonly Dictionary<string, string> SimplifiedChinese = new Dictionary<string, string>
        {
            { "App.Ready", "就绪" },
            { "Common.Cancel", "取消" },
            { "Common.Confirm", "确认" },
            { "Common.Reset", "重置" },
            { "Common.Settings", "设置" },
            { "Language.English", "英文" },
            { "Language.SimplifiedChinese", "简体中文" },
            { "Main.Title", "PlayPane" },
            { "Main.Subtitle", "将浏览器窗口镜像为适合游戏使用的覆盖层" },
            { "Main.RestorePrevious", "恢复上次会话" },
            { "Main.Settings", "设置" },
            { "Main.Refresh", "刷新" },
            { "Main.ShowAllWindows", "显示所有窗口" },
            { "Main.ColumnBrowser", "浏览器" },
            { "Main.ColumnTitle", "标题" },
            { "Main.ColumnProcess", "进程" },
            { "Main.ColumnMonitor", "显示器" },
            { "Main.SourcePreview", "源窗口预览" },
            { "Main.CaptureSource", "捕获来源" },
            { "Main.WindowCaptureSource", "窗口捕获" },
            { "Main.ExtensionCaptureSource", "Chrome/Edge 扩展捕获" },
            { "Main.ExtensionWaiting", "扩展捕获服务会在启动覆盖层时开启。" },
            { "Main.ExtensionServerReady", "扩展服务已就绪：{0}。请在 Chrome 或 Edge 中启动 PlayPane Capture 扩展。" },
            { "Main.Mirroring", "镜像设置" },
            { "Main.FullWindow", "完整窗口" },
            { "Main.CropRegion", "裁剪区域" },
            { "Main.EditCrop", "编辑裁剪" },
            { "Main.FrameRate", "帧率" },
            { "Main.FrameRateLow", "低资源 - 15 FPS" },
            { "Main.FrameRateStandard", "标准 - 30 FPS" },
            { "Main.FrameRateSmooth", "流畅 - 60 FPS" },
            { "Main.Opacity", "透明度" },
            { "Main.LockAspectRatio", "锁定宽高比" },
            { "Main.SourcePlacement", "源窗口位置" },
            { "Main.KeepOriginalPosition", "保持原位置" },
            { "Main.MoveRightEdge", "移动到右侧边缘" },
            { "Main.MoveMostlyOffScreen", "大部分移出屏幕" },
            { "Main.StartOverlay", "开始覆盖层" },
            { "Main.StopMirroring", "停止镜像" },
            { "Main.RestoreSource", "恢复源窗口" },
            { "Status.ControlPanelMinimized", "控制面板已最小化到系统托盘。" },
            { "Status.HotkeysFailed", "部分全局快捷键注册失败，请在设置中检查。" },
            { "Status.SourceRestored", "源窗口位置已恢复。" },
            { "Status.WindowsFound", "找到 {0} 个可捕获窗口。" },
            { "Status.SelectSourceFirst", "请先选择一个浏览器窗口。" },
            { "Status.OverlayStarted", "覆盖层已启动，当前为编辑模式。" },
            { "Status.MirroringStopped", "镜像已停止。" },
            { "Status.NoPreviousSource", "没有保存上次源窗口。" },
            { "Status.PreviousSourceNotFound", "未找到上次源窗口，请选择其他窗口。" },
            { "Status.SelectSourceBeforeCrop", "编辑裁剪区域前请先选择源窗口。" },
            { "Status.CropUpdated", "裁剪区域已更新。" },
            { "Status.SettingsSaved", "设置已保存。" },
            { "Status.RecoveredSource", "已恢复上次源窗口位置。" },
            { "Status.OverlayHidden", "覆盖层已隐藏。可使用 Ctrl + Alt + O 或托盘菜单显示。" },
            { "Status.ExtensionClientConnected", "浏览器扩展已连接。" },
            { "Status.ExtensionClientDisconnected", "浏览器扩展已断开。" },
            { "Status.ExtensionFrameReceived", "正在接收浏览器扩展画面。" },
            { "Recovery.Prompt", "上一次镜像会话未正常关闭。是否恢复源浏览器窗口位置？" },
            { "Recovery.Title", "PlayPane 恢复" },
            { "Tray.ShowControlPanel", "显示控制面板" },
            { "Tray.ShowOverlay", "显示覆盖层" },
            { "Tray.HideOverlay", "隐藏覆盖层" },
            { "Tray.EnterEditMode", "进入编辑模式" },
            { "Tray.EnterGameMode", "进入游戏模式" },
            { "Tray.StopMirroring", "停止镜像" },
            { "Tray.RestoreSource", "恢复源窗口位置" },
            { "Tray.Settings", "设置" },
            { "Tray.Exit", "退出 PlayPane" },
            { "Overlay.Title", "PlayPane 覆盖层" },
            { "Overlay.Opacity", "透明度" },
            { "Overlay.Crop", "裁剪" },
            { "Overlay.Lock", "锁定" },
            { "Overlay.Hide", "隐藏" },
            { "Overlay.Stop", "停止" },
            { "Overlay.SourceMinimized", "源浏览器已最小化，镜像画面可能停止更新。" },
            { "Overlay.FramePaused", "镜像画面可能已暂停，请恢复源浏览器窗口。" },
            { "Crop.Title", "裁剪源区域" },
            { "Settings.Title", "PlayPane 设置" },
            { "Settings.Startup", "启动" },
            { "Settings.Language", "语言" },
            { "Settings.AutoRestore", "启动时自动恢复上次会话" },
            { "Settings.StartWithWindows", "开机启动" },
            { "Settings.GlobalShortcuts", "全局快捷键" },
            { "Settings.ShortcutToggleOverlay", "Ctrl + Alt + O：显示或隐藏覆盖层" },
            { "Settings.ShortcutToggleMode", "Ctrl + Alt + E：切换编辑/游戏模式" },
            { "Settings.ShortcutOpacity", "Ctrl + Alt + 上/下：调整透明度" },
            { "Settings.ShortcutCrop", "Ctrl + Alt + R：重新配置裁剪" },
            { "Settings.ShortcutStop", "Ctrl + Alt + Q：停止镜像" },
            { "Settings.Save", "保存" }
        };

        public LocalizationService(AppLanguage language)
        {
            Language = language;
        }

        public AppLanguage Language { get; private set; }

        public string Get(string key)
        {
            string value;
            Dictionary<string, string> current = Language == AppLanguage.SimplifiedChinese ? SimplifiedChinese : English;
            if (current.TryGetValue(key, out value))
            {
                return value;
            }

            if (English.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }
}
