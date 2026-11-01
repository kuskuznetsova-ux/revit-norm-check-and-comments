using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RevitNormCheck.Corpus;

namespace RevitNormCheck.UI
{
    // Поиск по тексту норм из corpus.json: найти пункт и скопировать цитату для правила.
    public class CorpusSearchWindow : Window
    {
        private class Hit
        {
            public string Document;
            public string Number;
            public int Page;
            public string Text;
            public string Display => $"{Document}, п. {Number} (стр. {Page}): {(Text.Length > 110 ? Text.Substring(0, 110) + "…" : Text)}";
        }

        private readonly CorpusFile _corpus;
        private readonly TextBox _query = new TextBox { Padding = new Thickness(4) };
        private readonly ListBox _results = new ListBox { DisplayMemberPath = "Display", Margin = new Thickness(0, 8, 0, 8) };
        private readonly TextBox _detail = new TextBox
        {
            IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Height = 120,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(4)
        };

        public CorpusSearchWindow(CorpusFile corpus)
        {
            _corpus = corpus;
            Title = "Поиск по нормам";
            Width = 760;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var search = new Button { Content = "Найти", Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
            search.Click += (s, e) => Search();
            var copy = new Button { Content = "Копировать цитату", Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            copy.Click += (s, e) => CopyQuote();
            _results.SelectionChanged += (s, e) =>
            {
                var hit = _results.SelectedItem as Hit;
                _detail.Text = hit == null ? "" : $"{hit.Document}, п. {hit.Number}\n\n{hit.Text}";
            };

            var top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(search, 1);
            top.Children.Add(_query);
            top.Children.Add(search);

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(_results, 1);
            Grid.SetRow(_detail, 2);
            Grid.SetRow(copy, 3);
            root.Children.Add(top);
            root.Children.Add(_results);
            root.Children.Add(_detail);
            root.Children.Add(copy);
            Content = root;
        }

        private void Search()
        {
            var words = CorpusStore.Normalize(_query.Text).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return;

            var hits = new List<Hit>();
            foreach (var document in _corpus.Documents)
            {
                foreach (var clause in document.Clauses)
                {
                    var text = CorpusStore.Normalize(clause.Text);
                    if (words.All(w => text.Contains(w)))
                        hits.Add(new Hit { Document = document.Name, Number = clause.Number, Page = clause.Page, Text = clause.Text });
                    if (hits.Count >= 300) break;
                }
                if (hits.Count >= 300) break;
            }
            _results.ItemsSource = hits;
            _detail.Text = hits.Count == 0 ? "Ничего не найдено." : "Найдено пунктов: " + hits.Count + (hits.Count >= 300 ? " (показаны первые 300)" : "");
        }

        private void CopyQuote()
        {
            var hit = _results.SelectedItem as Hit;
            if (hit == null) return;
            try { Clipboard.SetText($"{hit.Document}, п. {hit.Number}: {hit.Text}"); } catch { }
        }
    }
}
