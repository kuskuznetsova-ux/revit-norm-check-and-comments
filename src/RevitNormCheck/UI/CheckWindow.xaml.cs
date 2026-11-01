using System;
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
        private readonly Dictionary<CheckCategory, TextBox> _pathBoxes = new Dictionary<CheckCategory, TextBox>();

        private static readonly Dictionary<CheckCategory, string> Titles = new Dictionary<CheckCategory, string>
        {
            { CheckCategory.Norms, "A. Нормы (эвакуация, огнестойкость, доступность)" },
            { CheckCategory.CompanyStandards, "B. Стандарты компании" },
            { CheckCategory.Documentation, "C. Состав и оформление, АР, стадия П (для экспертизы)" },
        };

        private static readonly Dictionary<CheckCategory, string> FolderHints = new Dictionary<CheckCategory, string>
        {
            { CheckCategory.Norms, "Папка с нормами (PDF)" },
            { CheckCategory.CompanyStandards, "Папка со стандартами компании (PDF)" },
            { CheckCategory.Documentation, "Папка с требованиями к составу и оформлению (PDF)" },
        };

        public CheckWindow(UIApplication uiApp)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _settings = PluginSettings.Load();

            foreach (CheckCategory category in Enum.GetValues(typeof(CheckCategory)))
                BuildSection(category);
        }

        private string GetFolder(CheckCategory category)
        {
            switch (category)
            {
                case CheckCategory.Norms: return _settings.NormsFolder;
                case CheckCategory.CompanyStandards: return _settings.CompanyStandardsFolder;
                default: return _settings.DocumentationFolder;
            }
        }

        private void SetFolder(CheckCategory category, string path)
        {
            switch (category)
            {
                case CheckCategory.Norms: _settings.NormsFolder = path; break;
                case CheckCategory.CompanyStandards: _settings.CompanyStandardsFolder = path; break;
                default: _settings.DocumentationFolder = path; break;
            }
        }

        private void BuildSection(CheckCategory category)
        {
            var panel = new StackPanel { Margin = new Thickness(8) };

            panel.Children.Add(new TextBlock { Text = FolderHints[category], FontWeight = FontWeights.SemiBold });

            var pathRow = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var pathBox = new TextBox { IsReadOnly = true, Padding = new Thickness(4), Text = GetFolder(category) };
            var browse = new Button { Content = "Выбрать...", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 4, 12, 4) };
            browse.Click += (s, e) => Browse(category);
            Grid.SetColumn(browse, 1);
            pathRow.Children.Add(pathBox);
            pathRow.Children.Add(browse);
            panel.Children.Add(pathRow);
            _pathBoxes[category] = pathBox;

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            var all = new Button { Content = "Выбрать все", Padding = new Thickness(10, 2, 10, 2) };
            var none = new Button { Content = "Снять все", Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(8, 0, 0, 0) };
            all.Click += (s, e) => SetAll(category, true);
            none.Click += (s, e) => SetAll(category, false);
            buttons.Children.Add(all);
            buttons.Children.Add(none);
            panel.Children.Add(buttons);

            foreach (var check in CheckDefinition.All.Where(c => c.Category == category))
            {
                var box = new CheckBox
                {
                    Content = check.Title,
                    Margin = new Thickness(0, 2, 0, 2),
                    IsChecked = _settings.EnabledCheckIds.Contains(check.Id)
                };
                _boxes[check.Id] = box;
                panel.Children.Add(box);
            }

            Root.Children.Add(new GroupBox { Header = Titles[category], Content = panel, Margin = new Thickness(0, 0, 0, 12) });
        }

        private void Browse(CheckCategory category)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = FolderHints[category];
                var current = GetFolder(category);
                if (Directory.Exists(current)) dialog.SelectedPath = current;
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                SetFolder(category, dialog.SelectedPath);
                _pathBoxes[category].Text = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private void SetAll(CheckCategory category, bool value)
        {
            foreach (var check in CheckDefinition.All.Where(c => c.Category == category))
                _boxes[check.Id].IsChecked = value;
        }

        private void RunClick(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            if (_settings.EnabledCheckIds.Count == 0)
            {
                TaskDialog.Show("Проверка норм", "Отметьте хотя бы одну проверку.");
                return;
            }

            var selectedCategories = CheckDefinition.All
                .Where(c => _settings.EnabledCheckIds.Contains(c.Id))
                .Select(c => c.Category)
                .Distinct();
            foreach (var category in selectedCategories)
            {
                if (!Directory.Exists(GetFolder(category)))
                {
                    TaskDialog.Show("Проверка норм", $"Для раздела «{Titles[category]}» укажите существующую папку.");
                    return;
                }
            }

            // Сами проверки и выгрузка в Excel добавляются следующим шагом.
            TaskDialog.Show("Проверка норм",
                $"Выбрано проверок: {_settings.EnabledCheckIds.Count}\n\nЗапуск проверок пока не реализован.");
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
