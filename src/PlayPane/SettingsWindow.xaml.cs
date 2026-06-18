using System.Windows;
using System.Windows.Controls;
using PlayPane.Core.Models;
using PlayPane.Core.Services;

namespace PlayPane
{
    public partial class SettingsWindow : Window
    {
        private readonly LocalizationService _localizer;

        public SettingsWindow(AppSettings settings, LocalizationService localizer)
        {
            InitializeComponent();
            _localizer = localizer;
            ApplyLocalization();
            AutoRestoreCheckBox.IsChecked = settings.AutoRestorePreviousSessionOnStartup;
            StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;
            SelectLanguage(settings.Language);
        }

        public bool AutoRestorePreviousSessionOnStartup
        {
            get { return AutoRestoreCheckBox.IsChecked == true; }
        }

        public bool StartWithWindows
        {
            get { return StartWithWindowsCheckBox.IsChecked == true; }
        }

        public AppLanguage SelectedLanguage
        {
            get
            {
                ComboBoxItem item = LanguageComboBox.SelectedItem as ComboBoxItem;
                AppLanguage language;
                if (item != null && item.Tag != null && System.Enum.TryParse(item.Tag.ToString(), out language))
                {
                    return language;
                }

                return AppLanguage.English;
            }
        }

        private void ApplyLocalization()
        {
            Title = _localizer.Get("Settings.Title");
            StartupLabel.Text = _localizer.Get("Settings.Startup");
            LanguageLabel.Text = _localizer.Get("Settings.Language");
            EnglishItem.Content = _localizer.Get("Language.English");
            SimplifiedChineseItem.Content = _localizer.Get("Language.SimplifiedChinese");
            AutoRestoreCheckBox.Content = _localizer.Get("Settings.AutoRestore");
            StartWithWindowsCheckBox.Content = _localizer.Get("Settings.StartWithWindows");
            GlobalShortcutsLabel.Text = _localizer.Get("Settings.GlobalShortcuts");
            ShortcutToggleOverlayText.Text = _localizer.Get("Settings.ShortcutToggleOverlay");
            ShortcutToggleModeText.Text = _localizer.Get("Settings.ShortcutToggleMode");
            ShortcutOpacityText.Text = _localizer.Get("Settings.ShortcutOpacity");
            ShortcutCropText.Text = _localizer.Get("Settings.ShortcutCrop");
            ShortcutStopText.Text = _localizer.Get("Settings.ShortcutStop");
            SaveButton.Content = _localizer.Get("Settings.Save");
            CancelButton.Content = _localizer.Get("Common.Cancel");
        }

        private void SelectLanguage(AppLanguage language)
        {
            foreach (object itemObject in LanguageComboBox.Items)
            {
                ComboBoxItem item = itemObject as ComboBoxItem;
                if (item != null && item.Tag != null && item.Tag.ToString() == language.ToString())
                {
                    LanguageComboBox.SelectedItem = item;
                    return;
                }
            }

            LanguageComboBox.SelectedItem = EnglishItem;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
