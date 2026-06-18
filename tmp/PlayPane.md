# PlayPane Product Specification

## 1. Document Information

| Item                         | Description                                  |
| ---------------------------- | -------------------------------------------- |
| Product Name                 | PlayPane                                     |
| Document Version             | V1.0                                         |
| Product Stage                | MVP / First Release                          |
| Target Platform              | Windows 10 first, Windows 11 compatible      |
| Product Type                 | Windows desktop utility                      |
| Recommended Technology Stack | C#, WPF, Windows Graphics Capture, Win32 API |

---

# 2. Product Overview

## 2.1 Product Positioning

PlayPane is a browser window mirroring tool designed for PC gamers.

The user selects an existing Chrome, Firefox, or Microsoft Edge window. PlayPane captures the selected browser window in real time and displays it as an adjustable transparent overlay above a windowed or borderless fullscreen game.

The overlay is display-only. In Game Mode, it becomes click-through, does not capture keyboard or mouse input, and does not interfere with gameplay.

## 2.2 Core Objectives

The first release should solve the following problems:

1. Players need to view guides, maps, videos, statistics, or other web content while playing.
2. Players want to avoid repeatedly switching between the game and browser with `Alt + Tab`.
3. Players want to keep using their existing browser session, including login status, extensions, cookies, bookmarks, and website settings.
4. The overlay must not intercept mouse or keyboard input intended for the game.
5. The user should be able to display only a selected rectangular area of the browser window.
6. The source browser window should be movable to the edge of the screen or another monitor without being minimized.

---

# 3. Product Scope

## 3.1 Supported in V1.0

The first release supports:

* Windows 10
* Windows 11
* Windowed games
* Borderless fullscreen games
* Google Chrome
* Mozilla Firefox
* Microsoft Edge
* One mirrored window at a time
* Full-window mirroring
* Rectangular region cropping
* Adjustable overlay opacity
* Movable and resizable overlay window
* Mouse click-through
* Global keyboard shortcuts
* Multi-monitor environments
* Different Windows DPI scaling levels
* System tray integration
* Local settings persistence
* Source window position recovery

## 3.2 Not Supported in V1.0

The first release does not support:

* Exclusive fullscreen games
* DirectX, Vulkan, or OpenGL hooking
* Code injection into game processes
* Multiple simultaneous overlay windows
* Clicking or scrolling inside the mirrored overlay
* Keyboard input inside the overlay
* Capturing a background tab inside a multi-tab browser window
* Automatically detaching a tab into a separate window
* Browser extensions
* An embedded browser
* Separate browser audio capture
* Screen recording
* Screenshot capture
* Cloud synchronization
* User accounts
* Automatic movement to Windows virtual desktops
* Automatic software updates

Website audio continues to play through the original source browser.

---

# 4. Core Usage Rules

## 4.1 Fixed Tab Requirement

Windows window capture can reliably capture an entire top-level browser window, but it cannot independently capture a background tab inside that window.

To keep a specific tab fixed in the overlay, the user must first detach the target tab into a separate browser window.

PlayPane then captures that dedicated browser window.

V1.0 does not automatically detach browser tabs.

If the user switches tabs inside the dedicated source browser window, the mirrored content also changes.

## 4.2 Source Window State

PlayPane must not automatically minimize the source browser window.

The source window may:

* Remain in its original position
* Be covered by the game window
* Be moved to the edge of a monitor
* Be moved to another monitor
* Be moved mostly outside the visible desktop area while keeping a small visible portion

The user may manually minimize the source browser window, but PlayPane does not guarantee that the mirrored content will continue updating.

Possible results after minimizing include:

* Frozen frames
* Black output
* Paused animations
* Video frame updates stopping
* The last valid frame remaining visible

---

# 5. User Flow

## 5.1 First Launch Flow

1. The user launches PlayPane.
2. PlayPane displays a list of capturable windows.
3. The list shows Chrome, Firefox, and Edge windows by default.
4. The user may enable the “Show all windows” option.
5. The user selects a dedicated browser window.
6. PlayPane displays a live preview of the selected source window.
7. The user chooses one of the following:

   * Full-window mirroring
   * Rectangular region cropping
8. The user selects a source window placement option.
9. The user clicks “Start Overlay.”
10. PlayPane creates the overlay and enters Edit Mode.
11. The user adjusts the overlay position, size, and opacity.
12. The user locks the overlay or uses a shortcut to enter Game Mode.
13. The main control panel minimizes to the system tray.

## 5.2 Regular Launch Flow

1. PlayPane loads the previous configuration.
2. It restores the previous overlay position, size, opacity, and crop settings.
3. It does not automatically start capture by default.
4. The user selects “Restore Previous Session.”
5. PlayPane searches for the previous source window.
6. If the window is found, capture begins.
7. If the window is not found, the user is asked to select another source window.

An optional “Automatically restore the previous session on startup” setting is available and disabled by default.

---

# 6. Functional Requirements

## FR-01 Window Discovery and Selection

PlayPane shall scan the system for capturable top-level windows.

By default, the window list shall include only:

* Google Chrome
* Mozilla Firefox
* Microsoft Edge

Each list item shall display:

* Application icon
* Browser name
* Window title
* Static or live thumbnail
* Current monitor

A “Show all windows” option shall be available.

The following windows shall be excluded:

* PlayPane windows
* Invisible windows
* Invalid or extremely small windows
* The desktop window
* The Windows taskbar

## FR-02 Real-Time Window Mirroring

PlayPane shall display the selected source window inside the overlay in real time.

The capture system shall:

* Continue capturing when the source is covered by another window
* Continue capturing after the source window is moved
* Adapt when the source window is resized
* Exclude the system mouse cursor from the captured image
* Avoid activating the source browser
* Avoid changing the foreground game window
* Never send input events to the source browser

## FR-03 Full-Window Mirroring

Full-window mirroring shall be the default capture mode.

The mirrored output may include:

* Browser tab bar
* Address bar
* Web page content
* Browser borders
* Browser dialogs shown inside the source window

## FR-04 Rectangular Region Cropping

The user shall be able to enable rectangular crop mode.

The crop interface shall support:

* Creating a crop rectangle by dragging
* Moving the crop rectangle
* Resizing from all four edges
* Resizing from all four corners
* Resetting to the full source window
* Displaying crop dimensions
* Displaying crop aspect ratio

Crop coordinates shall be stored relative to the source window.

If the source window size changes, the crop region shall scale proportionally.

The crop rectangle shall never exceed the valid source window area.

## FR-05 Overlay Window

The overlay window shall support:

* Always-on-top behavior
* Free movement
* Free resizing
* Borderless display
* Adjustable opacity
* Visibility in the Windows taskbar
* Visibility in the `Alt + Tab` switcher
* Movement between monitors
* Restoration of its previous position and size

The overlay shall display mirrored content only and shall not directly support web interaction.

## FR-06 Edit Mode

Edit Mode is used to configure the overlay.

In Edit Mode, the user can:

* Move the overlay
* Resize the overlay
* Adjust opacity
* Change the crop region
* Select another source window
* Change the frame rate
* Change the source window placement option
* Enable or disable aspect ratio locking
* Enter Game Mode

Edit Mode shall display:

* A visible window border
* A compact toolbar
* Current opacity
* Current frame rate
* Lock button
* Hide button
* Stop mirroring button

## FR-07 Game Mode

When Game Mode is enabled:

* Window borders shall be hidden
* The toolbar shall be hidden
* Mouse click-through shall be enabled
* Mouse clicks shall not be received by the overlay
* Mouse wheel input shall not be received by the overlay
* Keyboard input shall not be received by the overlay
* The overlay shall not request foreground focus
* The overlay shall not change the active game window
* The overlay shall remain always on top
* Only the mirrored image shall remain visible

## FR-08 Alt + Tab Behavior

The overlay shall appear in the Windows `Alt + Tab` task switcher.

When the user selects the overlay through `Alt + Tab`:

1. The overlay receives focus.
2. Game Mode is disabled.
3. Mouse click-through is disabled.
4. Edit Mode is enabled.
5. The border and toolbar become visible.

When the user switches from the overlay back to the active game window:

1. Game Mode is restored automatically.
2. Mouse click-through is enabled.
3. Borders and controls are hidden.
4. The overlay remains visible above the game.

Automatic Game Mode restoration shall only occur when switching back to the recognized game window.

Switching to a normal desktop application shall not automatically enable Game Mode.

## FR-09 Opacity Control

Overlay opacity shall use the following values:

* Minimum: 10%
* Maximum: 100%
* Default: 80%
* Adjustment step: 5%

Opacity shall affect the mirrored image.

In Edit Mode, borders, controls, and status messages shall remain clearly visible.

In Game Mode, no controls shall be displayed.

## FR-10 Overlay Scaling

The overlay shall support free resizing.

An “Lock aspect ratio” option shall be available:

* Enabled by default
* Preserves the captured image aspect ratio
* Can be disabled by the user
* Allows image stretching when disabled

## FR-11 Source Window Placement

The user may select one of the following source window placement modes.

### Keep Original Position

The source window position and size remain unchanged.

### Move to Screen Edge

Supported target edges:

* Left
* Right
* Top
* Bottom

Approximately 20 to 40 pixels of the source window shall remain visible so that the user can manually recover it.

### Move to Another Monitor

PlayPane shall detect all connected monitors.

The user may select:

* Target monitor
* Top-left
* Top-right
* Bottom-left
* Bottom-right
* Center
* Maximize on selected monitor

The source window shall retain its original size unless the user explicitly selects maximization.

### Move Mostly Off-Screen

PlayPane may move most of the source window outside the visible desktop area.

A small portion shall remain visible.

The source window shall not be moved to a position that cannot reasonably be recovered.

## FR-12 Source Window State Restoration

Before moving the source window, PlayPane shall record:

* Original position
* Original width and height
* Original monitor
* Normal or maximized state
* Original window placement state

The source window shall be restored when:

* The user stops mirroring
* The user selects a different source window
* The user exits PlayPane normally
* An unrecoverable capture error occurs

If the original monitor is disconnected, the source window shall be restored to a visible area on the primary monitor.

## FR-13 Recovery After Unexpected Exit

Before moving the source browser, PlayPane shall save the original window state to a temporary local session file.

If PlayPane detects an unfinished session on the next launch, it shall display:

> A previous mirroring session did not close correctly. Restore the source browser window position?

Available actions:

* Restore now
* Ignore
* View source window information

## FR-14 Minimized Source Window Detection

If the source browser is minimized, PlayPane shall display a non-blocking notification:

> The source browser is minimized. The mirrored image may stop updating.

If no new frames are received for a defined period, the overlay shall display:

> The mirrored image may be paused. Restore the source browser window.

The notification shall not take focus away from the game.

## FR-15 Frame Rate

PlayPane shall provide three capture frame rate options:

| Mode         | Frame Rate | Recommended Use             |
| ------------ | ---------: | --------------------------- |
| Low Resource |     15 FPS | Guides, maps, text pages    |
| Standard     |     30 FPS | Default, normal web content |
| Smooth       |     60 FPS | Video and dynamic content   |

The default frame rate shall be 30 FPS.

When the overlay is hidden, capture rendering shall pause or significantly reduce resource usage.

## FR-16 Global Shortcuts

Default shortcuts:

| Shortcut            | Action                                 |
| ------------------- | -------------------------------------- |
| `Ctrl + Alt + O`    | Show or hide the overlay               |
| `Ctrl + Alt + E`    | Switch between Edit Mode and Game Mode |
| `Ctrl + Alt + Up`   | Increase opacity                       |
| `Ctrl + Alt + Down` | Decrease opacity                       |
| `Ctrl + Alt + R`    | Reconfigure the crop region            |
| `Ctrl + Alt + Q`    | Stop the current mirroring session     |

Shortcuts shall be customizable.

PlayPane shall detect shortcut conflicts.

If shortcut registration fails, the user shall be asked to choose another combination.

## FR-17 System Tray

PlayPane shall provide a system tray icon.

The tray menu shall contain:

* Show control panel
* Show overlay
* Hide overlay
* Enter Edit Mode
* Enter Game Mode
* Stop mirroring
* Restore source window position
* Settings
* Exit PlayPane

Closing the main control panel shall minimize it to the system tray by default.

The application process shall only terminate when the user selects “Exit PlayPane” or uses an equivalent explicit exit action.

## FR-18 Taskbar Behavior

The overlay shall have its own taskbar entry.

When the user selects the overlay from the taskbar:

* The overlay becomes visible
* Edit Mode is enabled
* Mouse click-through is disabled
* The user can modify the overlay

The main control panel and overlay may share the same application icon.

## FR-19 Settings Persistence

PlayPane shall save:

* Previous source window information
* Browser type
* Crop region
* Overlay position
* Overlay size
* Opacity
* Frame rate
* Global shortcuts
* Source window placement mode
* Aspect ratio lock setting
* Startup behavior
* Automatic session restoration preference

PlayPane shall not save:

* Web page content
* Browser cookies
* Browser passwords
* Browser history
* User account information
* Captured screenshots
* Captured video

## FR-20 Application Startup

During startup, PlayPane shall:

1. Check for unfinished sessions.
2. Check whether the previous source window still exists.
3. Load user settings.
4. Display the control panel.

Start with Windows shall be disabled by default.

Automatic session restoration shall be disabled by default.

## FR-21 Application Exit

Before exiting, PlayPane shall:

1. Stop window capture.
2. Release capture resources.
3. Unregister global shortcuts.
4. Restore the source browser window.
5. Save the current settings.
6. Remove the temporary session recovery file.

Exiting PlayPane shall not close the source browser.

---

# 7. Error Handling

## 7.1 Source Window Closed

If the source window is closed:

* Keep the last valid frame visible
* Display “Source window closed”
* Stop the capture session
* Keep the overlay window open
* Provide a “Select another window” action

## 7.2 Browser Crash

PlayPane should attempt to locate a replacement source window using:

* Browser process
* Window title
* Browser type
* Previous position
* Previous dimensions

If automatic recovery fails, the user shall be asked to select another window.

## 7.3 Source Window Handle Changed

Browser updates, crashes, or restored sessions may cause the native window handle to change.

PlayPane may attempt to reconnect using:

* Browser process name
* Browser type
* Window title
* Previous position
* Window dimensions

PlayPane shall not reconnect using only the window title, because this may result in capturing the wrong window.

## 7.4 Monitor Disconnected

If the monitor containing the overlay is disconnected:

* Move the overlay to the primary monitor
* Ensure the overlay is fully visible

If the monitor containing the source browser is disconnected:

* Move the source browser to a visible area on the primary monitor
* Update the recovery information

## 7.5 Black or Frozen Capture

If no valid frames are received for a defined period:

* Keep the last valid frame
* Display a non-blocking warning
* Attempt to restart the capture session
* Do not take focus away from the game
* Do not automatically terminate PlayPane

---

# 8. Multi-Monitor and DPI Support

V1.0 shall support:

* Single-monitor systems
* Dual-monitor and multi-monitor systems
* Different monitor resolutions
* Different DPI values across monitors
* 100% Windows scaling
* 125% Windows scaling
* 150% Windows scaling
* 175% Windows scaling
* 200% Windows scaling

The source window and overlay may be located on different monitors.

All window and crop calculations shall correctly handle DPI scaling.

The implementation must avoid:

* Crop coordinate offsets
* Incorrect overlay positioning
* Unexpected size changes
* Aspect ratio changes when moving between monitors
* Incorrect drag distances

---

# 9. User Interface

## 9.1 Main Control Panel

The main control panel shall include:

### Window Selection Section

* Browser window list
* Search input
* Refresh button
* “Show all windows” option
* Source preview

### Mirroring Settings Section

* Full-window mode
* Rectangular crop mode
* Frame rate
* Opacity
* Aspect ratio lock
* Source window placement

### Action Section

* Start Overlay
* Stop Mirroring
* Restore Source Window
* Open Settings

## 9.2 Crop Editor

The crop editor shall display a preview of the source window.

It shall include:

* Draggable crop rectangle
* Crop dimensions
* Crop aspect ratio
* Reset button
* Confirm button
* Cancel button

## 9.3 Overlay Edit Toolbar

The overlay edit toolbar should contain:

* Drag area
* Opacity slider
* Frame rate selector
* Crop button
* Lock button
* Hide button
* Stop button

The toolbar should be compact and should avoid covering important mirrored content.

## 9.4 Game Mode Interface

In Game Mode, the overlay shall contain:

* No border
* No toolbar
* No buttons
* No permanent status text
* No interactive controls
* Mirrored content only

Small temporary warnings may appear when an error occurs.

---

# 10. Application State Model

PlayPane shall use the following primary states.

## No Source Selected

No source window has been selected.

The application displays the window selection interface.

## Preview State

A source window has been selected.

The user configures full-window capture or rectangular cropping.

## Edit State

Mirroring is active.

The user can modify the overlay.

## Game State

Mirroring is active.

The overlay is click-through and does not interfere with gameplay.

## Paused State

The overlay is hidden or the source temporarily stops producing frames.

## Error State

The source closes, the browser crashes, or capture fails.

State transitions:

```text
No Source Selected
        ↓
     Preview
        ↓
       Edit
        ↓
       Game

Game ↔ Edit
Game → Paused
Paused → Game
Any active capture state → Error
Error → Preview or No Source Selected
```

---

# 11. Non-Functional Requirements

## 11.1 Performance

Performance objectives:

* Stable 30 FPS capture under normal conditions
* No intentional limitation of game frame rate
* No noticeable mouse input delay
* No noticeable keyboard input delay
* Reduced resource usage when the overlay is hidden
* No continuous memory growth
* GPU-based frame processing and scaling where practical

For a reference test at 1080p and 30 FPS:

* Target average game frame rate reduction: no more than 5%
* Target average CPU usage: no more than 8%
* No continuous memory leak during long sessions

Exact performance targets may be adjusted after hardware testing.

## 11.2 Stability

PlayPane should support at least four hours of continuous operation without:

* Application crashes
* Capture thread failure
* Continuous memory growth
* Failure to restore the source window
* Global shortcut failure
* Unexpected focus changes

## 11.3 Security

PlayPane shall not:

* Inject code into game processes
* Modify game files
* Read game memory
* Simulate gameplay input
* Record keyboard input
* Upload captured frames
* Read browser cookies
* Read browser passwords

## 11.4 Privacy

All capture and configuration processing shall remain local.

V1.0 shall not upload data over the network.

PlayPane shall not store captured frames.

## 11.5 Compatibility

Minimum supported system:

* Windows 10 version 2004 or later

Primary test systems:

* Windows 10 22H2
* Windows 11

Primary test browsers:

* Google Chrome
* Mozilla Firefox
* Microsoft Edge

---

# 12. Acceptance Criteria

## AC-01 Browser Window Selection

Given that Chrome, Firefox, and Edge are open, the user shall be able to view and select their windows from the source list.

## AC-02 Full-Window Mirroring

After selecting a browser and starting capture, the overlay shall display the complete browser window in real time.

## AC-03 Capture While Covered

When the source browser is completely covered by the game, the overlay shall continue displaying the source browser instead of the covering game window.

## AC-04 Rectangular Cropping

After selecting a crop rectangle, the overlay shall display only the selected source region.

## AC-05 Opacity

When opacity is set to 50%, the mirrored image shall be semi-transparent and the underlying game shall remain visible.

## AC-06 Mouse Click-Through

In Game Mode, mouse clicks and mouse wheel input inside the overlay area shall be received by the game underneath.

## AC-07 Focus Protection

Updating the overlay shall not cause the game to lose foreground focus.

## AC-08 Alt + Tab

The overlay shall appear in the `Alt + Tab` list.

Selecting it shall automatically enter Edit Mode.

## AC-09 Return to Game

When the user switches back to the game, the overlay shall automatically restore Game Mode and click-through behavior.

## AC-10 Move Source to Screen Edge

When the user selects an edge placement option, the source browser shall move to that edge while retaining a small visible portion.

## AC-11 Restore Source Position

When mirroring stops or PlayPane exits, the source browser shall return to its previous position and display state.

## AC-12 Minimized Source Warning

When the source browser is minimized, PlayPane shall display a warning without crashing.

## AC-13 Multi-Monitor Operation

When the overlay is moved to another monitor, its size, crop region, and scaling shall remain correct.

## AC-14 Unexpected Exit Recovery

After an abnormal termination, PlayPane shall ask whether the source browser position should be restored.

## AC-15 Source Window Closed

When the source browser closes, PlayPane shall retain the last valid frame and ask the user to select another source.

---

# 13. Recommended Project Structure

```text
PlayPane
├── Application
│   ├── AppBootstrapper
│   ├── SessionManager
│   └── StateManager
├── Capture
│   ├── WindowCaptureService
│   ├── CaptureFrameProcessor
│   ├── CropProcessor
│   └── FrameRateController
├── Windowing
│   ├── WindowEnumerator
│   ├── OverlayWindowManager
│   ├── SourceWindowManager
│   ├── DisplayManager
│   └── DpiManager
├── Input
│   ├── GlobalHotkeyService
│   └── ClickThroughService
├── Views
│   ├── MainWindow
│   ├── OverlayWindow
│   ├── CropWindow
│   └── SettingsWindow
├── Services
│   ├── TrayService
│   ├── SettingsService
│   ├── RecoveryService
│   └── NotificationService
├── Models
│   ├── AppSettings
│   ├── CaptureSession
│   ├── SourceWindowInfo
│   ├── CropRegion
│   └── WindowPlacementSnapshot
└── Native
    ├── Win32Api
    └── WindowsCaptureInterop
```

---

# 14. Development Priorities

## P0 — Required

* Browser window discovery
* Source window selection
* Full-window real-time mirroring
* Always-on-top overlay
* Overlay movement and resizing
* Opacity adjustment
* Game Mode
* Mouse click-through
* Focus protection
* `Alt + Tab` visibility
* Global shortcuts
* Source window position restoration

## P1 — Important for V1.0

* Rectangular region cropping
* Three frame rate options
* System tray support
* Multi-monitor support
* DPI support
* Source window placement
* Unexpected exit recovery
* Frozen capture detection

## P2 — Enhancements

* Live source thumbnails
* Automatic source reconnection
* Improved shortcut conflict detection
* Performance monitoring
* More accurate source window matching
* Improved error notification animations

---

# 15. Future Version Candidates

Future versions may include:

* Multiple simultaneous overlay windows
* Browser extension integration
* Direct Chrome tab capture
* Automatic tab detachment
* Interactive overlay input
* Mouse coordinate forwarding
* Independent browser audio controls
* Saved layout presets
* Per-game overlay profiles
* Enhanced video picture-in-picture mode
* Custom overlay shapes
* Experimental Windows virtual desktop support
* Local network remote control

These features are outside the V1.0 development and acceptance scope.
