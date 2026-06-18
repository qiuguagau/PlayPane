using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class ShortcutBinding
    {
        public ShortcutBinding()
        {
        }

        public ShortcutBinding(HotkeyAction action, ShortcutDefinition shortcut)
        {
            Action = action;
            Shortcut = shortcut;
        }

        [DataMember(Order = 1)]
        public HotkeyAction Action { get; set; }

        [DataMember(Order = 2)]
        public ShortcutDefinition Shortcut { get; set; }
    }
}
