using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitNormCheck.Checks;
using RevitNormCheck.Reporting;
using RevitNormCheck.Settings;

// У Revit API есть одноимённые типы, поэтому явно выбираем WPF.
using Grid = System.Windows.Controls.Grid;
using TextBox = System.Windows.Controls.TextBox;

namespace RevitNormCheck.UI
{
    public partial class CheckWindow : Window
    {
        private readonly UIApplication _uiApp;
        private readonly PluginSettings _settings;
        private readonly Dictionary<string, CheckBox> _boxes = new Dictionary<string, CheckBox>();
        private readonly Dictionary<CheckCategory, TextBox> _pathBoxes = new Dictionary<CheckCategory, TextBox>();
        private readonly List<ScopeItem> _scopeItems = new List<ScopeItem>();
        private RadioButton _modeCurrent, _modeSelected, _modeWhole;
        private UIElement _scopeList;

        private class ScopeItem
        {
            public string UniqueId;
            public string Title;
            public CheckBox Box;
        }

        private static readonly Dictionary<CheckCategory, string> Titles = new Dictionary<CheckCategory, string>
        {
            { CheckCategory.Norms, "A. Нормы (эвакуация, огнестойкость, доступность)" },
            { CheckCategory.CompanyStandards, "B. Стандарты компании" },
            { CheckCategory.Documentation, "C. Стандарты АГР, «Строим просто», ДГП" },
        };

        private static readonly Dictionary<CheckCategory, string> FolderHints = new Dictionary<CheckCategory, string>
        {
            { CheckCategory.Norms, "Папка с нормами (PDF)" },
            { CheckCategory.CompanyStandards, "Папка со стандартами компании (PDF)" },
            { CheckCategory.Documentation, "Папка со стандартами АГР, «Строим просто», ДГП (PDF)" },
        };

        public CheckWindow(UIApplication uiApp)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _settings = PluginSettings.Load();

            foreach (CheckCategory category in Enum.GetValues(typeof(CheckCategory)))
                BuildSection(category);
            BuildScopeSection();
        }

        private void BuildScopeSection()
        {
            var panel = new StackPanel { Margin = new Thickness(8) };

            _modeCurrent = new RadioButton { Content = "Текущий вид или лист", Margin = new Thickness(0, 2, 0, 2) };
            _modeSelected = new RadioButton { Content = "Выбранные листы и виды", Margin = new Thickness(0, 2, 0, 2) };
            _modeWhole = new RadioButton { Content = "Вся модель", Margin = new Thickness(0, 2, 0, 2) };
            _modeCurrent.IsChecked = _settings.ScopeMode == "CurrentView";
            _modeSelected.IsChecked = _settings.ScopeMode == "Selected";
            _modeWhole.IsChecked = _settings.ScopeMode == "WholeModel";
            if (_modeCurrent.IsChecked != true && _modeSelected.IsChecked != true && _modeWhole.IsChecked != true)
                _modeCurrent.IsChecked = true;
            panel.Children.Add(_modeCurrent);
            panel.Children.Add(_modeSelected);
            panel.Children.Add(_modeWhole);

            var filter = new TextBox { Margin = new Thickness(0, 8, 0, 4), Padding = new Thickness(4) };
            filter.ToolTip = "Фильтр по названию или номеру";
            panel.Children.Add(new TextBlock { Text = "Листы и виды (фильтр по названию):" });
            panel.Children.Add(filter);

            var list = new StackPanel();
            var scroll = new ScrollViewer { MaxHeight = 240, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = list };
            _scopeList = scroll;
            panel.Children.Add(scroll);

            var doc = _uiApp.ActiveUIDocument.Document;

            list.Children.Add(new TextBlock { Text = "Листы", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 2) });
            var sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
                .OrderBy(v => v.SheetNumber);
            foreach (var sheet in sheets)
                AddScopeItem(list, sheet.UniqueId, $"{sheet.SheetNumber} - {sheet.Name}");

            list.Children.Add(new TextBlock { Text = "Виды", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
            var allowed = new HashSet<ViewType>
            {
                ViewType.FloorPlan, ViewType.CeilingPlan, ViewType.EngineeringPlan, ViewType.AreaPlan,
                ViewType.Section, ViewType.Elevation, ViewType.Detail, ViewType.ThreeD
            };
            var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => !v.IsTemplate && allowed.Contains(v.ViewType))
                .OrderBy(v => v.ViewType.ToString()).ThenBy(v => v.Name);
            foreach (var view in views)
                AddScopeItem(list, view.UniqueId, $"{view.Name} ({view.ViewType})");

            filter.TextChanged += (s, e) =>
            {
                var text = filter.Text.Trim();
                foreach (var item in _scopeItems)
                    item.Box.Visibility = text.Length == 0 ||
                        item.Title.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            };

            RoutedEventHandler modeChanged = (s, e) => _scopeList.IsEnabled = _modeSelected.IsChecked == true;
            _modeCurrent.Checked += modeChanged;
            _modeSelected.Checked += modeChanged;
            _modeWhole.Checked += modeChanged;
            _scopeList.IsEnabled = _modeSelected.IsChecked == true;

            Root.Children.Add(new GroupBox { Header = "Что проверять на чертеже: листы и виды", Content = panel, Margin = new Thickness(0, 0, 0, 12) });
        }

        private void AddScopeItem(StackPanel list, string uniqueId, string title)
        {
            var box = new CheckBox
            {
                Content = title,
                Margin = new Thickness(0, 1, 0, 1),
                IsChecked = _settings.ScopeUniqueIds.Contains(uniqueId)
            };
            _scopeItems.Add(new ScopeItem { UniqueId = uniqueId, Title = title, Box = box });
            list.Children.Add(box);
        }

        private string CurrentScopeMode() =>
            _modeSelected.IsChecked == true ? "Selected" :
            _modeWhole.IsChecked == true ? "WholeModel" : "CurrentView";

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

            foreach (var group in CheckDefinition.All.Where(c => c.Category == category).GroupBy(c => c.SubGroup))
            {
                var indent = 0.0;
                if (group.Key != null)
                {
                    var members = group.ToList();
                    var header = new CheckBox
                    {
                        Content = group.Key,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 8, 0, 2)
                    };
                    header.Click += (s, e) =>
                    {
                        foreach (var m in members) _boxes[m.Id].IsChecked = header.IsChecked == true;
                    };
                    panel.Children.Add(header);
                    indent = 20;
                }

                foreach (var check in group)
                {
                    var box = new CheckBox
                    {
                        Content = check.Title,
                        Margin = new Thickness(indent, 2, 0, 2),
                        IsChecked = _settings.EnabledCheckIds.Contains(check.Id)
                    };
                    _boxes[check.Id] = box;
                    panel.Children.Add(box);
                }
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

            if (_settings.ScopeMode == "Selected" && _settings.ScopeUniqueIds.Count == 0)
            {
                TaskDialog.Show("Проверка норм", "Отметьте листы или виды для проверки.");
                return;
            }

            try
            {
                var findings = CheckRunner.Run(_uiApp.ActiveUIDocument, _settings);

                using (var dialog = new System.Windows.Forms.SaveFileDialog())
                {
                    dialog.Filter = "Excel (*.xlsx)|*.xlsx";
                    dialog.FileName = "Замечания_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".xlsx";
                    if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                    ReportWriter.Write(dialog.FileName, findings);
                    TaskDialog.Show("Проверка норм", ReportWriter.Summary(findings) + "\n\n" + dialog.FileName);
                    try { System.Diagnostics.Process.Start(dialog.FileName); } catch { }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Проверка норм", "Ошибка: " + ex.Message);
            }
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
            _settings.ScopeMode = CurrentScopeMode();
            _settings.ScopeUniqueIds = _scopeItems.Where(i => i.Box.IsChecked == true).Select(i => i.UniqueId).ToList();
            _settings.Save();
        }
    }
}
