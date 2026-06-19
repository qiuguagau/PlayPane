# PlayPane

[English](README.md)

PlayPane 是一个 Windows 桌面工具，用于把 Chrome 或 Microsoft Edge 标签页镜像到始终置顶的覆盖层窗口中，适合窗口化或无边框全屏游戏场景。

## 当前 MVP

- 通过 `extensions/chromium-playpane` 中的未打包扩展捕获 Chrome 或 Microsoft Edge 标签页。
- 应用启动后会开启本地 WebRTC 信令服务，并在可用时把扩展视频流显示到主窗口预览面板。
- 可以启动一个基于嵌入式 WebView2 WebRTC viewer 的覆盖层窗口，支持移动、缩放和置顶。
- 支持透明度控制、15/30/60 FPS 模式、编辑模式、游戏模式和鼠标穿透。
- 注册默认全局快捷键：
  - `Ctrl + Alt + O`：显示/隐藏覆盖层
  - `Ctrl + Alt + E`：切换编辑/游戏模式
  - `Ctrl + Alt + Up`：增加透明度
  - `Ctrl + Alt + Down`：降低透明度
  - `Ctrl + Alt + Q`：停止镜像
- 本地设置会保存到当前用户的应用数据目录。
- 支持在设置中选择英文或简体中文界面。
- 使用 WebRTC 传输镜像视频，本地 WebSocket 只用于交换控制和信令消息。
- 提供系统托盘菜单，用于常用操作。

## 构建

需要 Windows 和支持 WPF 的 .NET SDK。

```powershell
dotnet build PlayPane.sln
dotnet run --project tests\PlayPane.Tests\PlayPane.Tests.csproj
dotnet run --project src\PlayPane\PlayPane.csproj -- --smoke-test
dotnet run --project src\PlayPane\PlayPane.csproj
```

## Chrome/Edge 扩展捕获

1. 构建并启动 PlayPane。
2. PlayPane 会启动本地 WebRTC 信令服务 `ws://127.0.0.1:17632/playpane`，并在主窗口预览面板中等待视频流。
3. 在 Chrome 或 Edge 中打开 `chrome://extensions` 或 `edge://extensions`。
4. 启用开发者模式，并加载未打包扩展目录 `extensions/chromium-playpane`。
5. 打开目标标签页，点击 PlayPane Capture 扩展图标，然后点击 Start current tab。
6. 当 PlayPane 中出现预览后，点击 Start Overlay。

扩展会通过 Chrome 的 `tabCapture` API 捕获当前标签页，并通过 WebRTC 把视频流发送到覆盖层内嵌的 WebView2 viewer。本地 WebSocket 只交换 `viewer-ready`、`viewer-stopped`、`offer`、`answer` 和 ICE candidates 等控制/信令消息。

标签页选择一次后，停止并重新启动 PlayPane 覆盖层时会自动重新连接到同一个扩展捕获源。只有当你想释放当前浏览器标签页捕获时，才需要在扩展弹窗中点击 Stop。
