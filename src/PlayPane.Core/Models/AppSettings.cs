using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class AppSettings
    {
        public AppSettings()
        {
        }

        [DataMember(Order = 1)]
        public PreviousSourceWindow PreviousSource { get; set; }

        [DataMember(Order = 2)]
        public CaptureMode CaptureMode { get; set; }

        [DataMember(Order = 3)]
        public CropRegion CropRegion { get; set; }

        [DataMember(Order = 4)]
        public WindowBounds OverlayBounds { get; set; }

        [DataMember(Order = 5)]
        public int OpacityPercent { get; set; }

        [DataMember(Order = 6)]
        public FrameRateMode FrameRateMode { get; set; }

        [DataMember(Order = 7)]
        public List<ShortcutBinding> Shortcuts { get; set; }

        [DataMember(Order = 8)]
        public SourcePlacementOptions SourcePlacement { get; set; }

        [DataMember(Order = 9)]
        public bool AspectRatioLocked { get; set; }

        [DataMember(Order = 10)]
        public bool AutoRestorePreviousSessionOnStartup { get; set; }

        [DataMember(Order = 11)]
        public bool StartWithWindows { get; set; }

        [DataMember(Order = 12)]
        public AppLanguage Language { get; set; }

        [DataMember(Order = 13)]
        public CaptureSourceKind CaptureSourceKind { get; set; }

        public static AppSettings CreateDefault()
        {
            var settings = new AppSettings();
            settings.CaptureMode = CaptureMode.FullWindow;
            settings.CropRegion = CropRegion.Full;
            settings.OverlayBounds = new WindowBounds(120, 120, 800, 450);
            settings.OpacityPercent = 80;
            settings.FrameRateMode = FrameRateMode.Standard;
            settings.SourcePlacement = new SourcePlacementOptions();
            settings.AspectRatioLocked = true;
            settings.AutoRestorePreviousSessionOnStartup = false;
            settings.StartWithWindows = false;
            settings.Language = AppLanguage.English;
            settings.CaptureSourceKind = CaptureSourceKind.Window;
            settings.Shortcuts = CreateDefaultShortcuts();
            return settings;
        }

        public void EnsureValid()
        {
            if (CropRegion == null)
            {
                CropRegion = CropRegion.Full;
            }
            else
            {
                CropRegion = CropRegion.Clamp();
            }

            if (OverlayBounds == null || !OverlayBounds.IsUsable)
            {
                OverlayBounds = new WindowBounds(120, 120, 800, 450);
            }

            if (OpacityPercent < 10)
            {
                OpacityPercent = 10;
            }

            if (OpacityPercent > 100)
            {
                OpacityPercent = 100;
            }

            OpacityPercent = (OpacityPercent / 5) * 5;

            if (FrameRateMode != FrameRateMode.LowResource &&
                FrameRateMode != FrameRateMode.Standard &&
                FrameRateMode != FrameRateMode.Smooth)
            {
                FrameRateMode = FrameRateMode.Standard;
            }

            if (SourcePlacement == null)
            {
                SourcePlacement = new SourcePlacementOptions();
            }

            if (Shortcuts == null || Shortcuts.Count == 0)
            {
                Shortcuts = CreateDefaultShortcuts();
            }

            if (Language != AppLanguage.English && Language != AppLanguage.SimplifiedChinese)
            {
                Language = AppLanguage.English;
            }

            if (CaptureSourceKind != CaptureSourceKind.Window && CaptureSourceKind != CaptureSourceKind.BrowserExtension)
            {
                CaptureSourceKind = CaptureSourceKind.Window;
            }
        }

        private static List<ShortcutBinding> CreateDefaultShortcuts()
        {
            var modifiers = HotkeyModifier.Control | HotkeyModifier.Alt;
            return new List<ShortcutBinding>
            {
                new ShortcutBinding(HotkeyAction.ToggleOverlay, new ShortcutDefinition(modifiers, 79)),
                new ShortcutBinding(HotkeyAction.ToggleEditGameMode, new ShortcutDefinition(modifiers, 69)),
                new ShortcutBinding(HotkeyAction.IncreaseOpacity, new ShortcutDefinition(modifiers, 38)),
                new ShortcutBinding(HotkeyAction.DecreaseOpacity, new ShortcutDefinition(modifiers, 40)),
                new ShortcutBinding(HotkeyAction.ReconfigureCrop, new ShortcutDefinition(modifiers, 82)),
                new ShortcutBinding(HotkeyAction.StopMirroring, new ShortcutDefinition(modifiers, 81))
            };
        }
    }
}
