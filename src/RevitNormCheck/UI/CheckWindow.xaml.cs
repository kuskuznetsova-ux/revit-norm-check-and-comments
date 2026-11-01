using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using RevitNormCheck.Checks;
using RevitNormCheck.Settings;

namespace RevitNormCheck.UI
{
    public partial class CheckWindow : Window
    {
        private readonly UIApplication _uiApp;
        private readonly PluginSettings _settings;
        private readonly Dictionary<string, CheckBox> _boxes = new Dictionary<string, CheckBox>();

        public CheckWindow(UIApplication uiApp)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _settings = PluginSettings.Load();

            NormsPathBox.Text = _settings.NormsFolder;
            foreach (var check in CheckDefinition.All)
            {
                var box = new CheckBox
                {
                    Content = check.Title,
                    Margin = new Thickness(0, 2, 0, 2),
                    IsChecked = _settings.EnabledCheckIds.Contains(check.Id)
                };
                _boxes[check.Id] = box;
                ChecksPanel.Children.Add(box);
            }
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку с нормативами";
                if (Directory.Exists(_settings.NormsFolder)) dialog.SelectedPath = _settings.NormsFolder;
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                _settings.NormsFolder = dialog.SelectedPath;
                NormsPathBox.Text = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private void SelectAllClick(object sender, RoutedEventArgs e) => SetAll(true);

        private void ClearAllClick(object sender, RoutedEventArgs e) => SetAll(false);

        private void SetAll(bool value)
        {
            foreach (var box in _boxes.Values) box.IsChecked = value;
        }

        private void RunClick(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            if (!Directory.Exists(_settings.NormsFolder))
            {
                TaskDialog.Show("Проверка норм", "Укажите существующую папку с нормами.");
                return;
            }
            if (_settings.EnabledCheckIds.Count == 0)
            {
                TaskDialog.Show("Проверка норм", "Отметьте хотя бы одну проверку.");
                return;
            }

            // Сами проверки и выгрузка в Excel добавляются следующим шагом.
            TaskDialog.Show("Проверка норм",
                $"Выбрано проверок: {_settings.EnabledCheckIds.Count}\nПапка норм: {_settings.NormsFolder}\n\nЗапуск проверок пока не реализован.");
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            base.OnClosing(e);
        }

        private void SaveSettings()
        {
            _settings.EnabledCheckIds = _boxes.Where(p => p.Value.IsChecked == true).Select(p => p.Key).ToList();
            _settings.Save();
        }
    }
}
