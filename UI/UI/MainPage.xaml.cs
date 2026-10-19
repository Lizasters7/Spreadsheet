using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using System;
using System.Collections.Generic;
using Grid = Microsoft.Maui.Controls.Grid;
using LabCalculator;
using CommunityToolkit.Maui.Storage;
using System.Text;
using System.Text.Json;

namespace UI
{
    public partial class MainPage : ContentPage
    {
        const int CountColumn = 26;
        const int CountRow = 50;

        private readonly Dictionary<string, string> cellExpressions = new Dictionary<string, string>();

        private readonly Dictionary<string, string> cellErrors = new Dictionary<string, string>();

        public MainPage()
        {
            InitializeComponent();
            CreateGrid();
        }

        private string GetCellName(int row, int col)
        {
            return GetColumnName(col + 1) + (row + 1);
        }

        private object EvaluateCell(string cellName, HashSet<string> visiting)
        {
            if (!cellExpressions.TryGetValue(cellName, out var expr) || string.IsNullOrWhiteSpace(expr))
            {
                return 0.0;
            }
            if (!visiting.Add(cellName))
            {
                throw new ArgumentException($"Circular reference in {cellName}");
            }
            try
            {
                return Calculator.Evaluate(expr, name => EvaluateCell(name, visiting));
            }
            finally
            {
                visiting.Remove(cellName);
            }
        }

        private void RecalculateAll()
        {
            cellErrors.Clear();
            foreach (var child in grid.Children)
            {
                if (child is Entry entry && !entry.IsFocused)
                {
                    var name = GetCellName(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1);
                    if (cellExpressions.TryGetValue(name, out var expr) && !string.IsNullOrWhiteSpace(expr))
                    {
                        try
                        {
                            var result = EvaluateCell(name, new HashSet<string>());
                            entry.Text = result is bool b ? (b ? "True" : "False") : result.ToString();
                        }
                        catch (Exception ex)
                        {
                            entry.Text = "Error";
                            cellErrors[name] = ex.Message;
                        }
                    }
                    else
                    {
                        entry.Text = "";
                    }
                }
            }
        }

        private void Entry_Focused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                var name = GetCellName(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1);
                string? expr;
                if (cellExpressions.TryGetValue(name, out expr))
                {
                    entry.Text = expr;
                    textInput.Text = expr;
                }
                else
                {
                    entry.Text = "";
                    textInput.Text = "";
                }
                string? error;
                if (cellErrors.TryGetValue(name, out error))
                {
                    errorLabel.Text = name + ": " + error;
                }
                else
                {
                    errorLabel.Text = "";
                }
            }
        }

        private void CreateGrid()
        {
            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
        }

        private void AddColumnsAndColumnLabels()
        {
            for (int col = 0; col < CountColumn + 1; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());

                if (col > 0)
                {
                    var label = new Label
                    {
                        Text = GetColumnName(col),
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    Grid.SetRow(label, 0);
                    Grid.SetColumn(label, col);
                    grid.Children.Add(label);
                }
            }
        }

        private void AddRowsAndCellEntries()
        {
            for (int row = 0; row < CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition());

                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, row + 1);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);
                for (int col = 0; col < CountColumn; col++)
                {
                    var entry = new Entry
                    {
                        Text = "",
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    entry.Unfocused += Entry_Unfocused;
                    entry.Focused += Entry_Focused;
                    Grid.SetRow(entry, row + 1);
                    Grid.SetColumn(entry, col + 1);
                    grid.Children.Add(entry);
                }
            }
        }

        private string GetColumnName(int colIndex)
        {
            int dividend = colIndex;
            string columnName = string.Empty;

            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private void Entry_Unfocused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                var name = GetCellName(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1);
                if (entry.Text != null)
                {
                    cellExpressions[name] = entry.Text;
                }
                else
                {
                    cellExpressions[name] = "";
                }
                RecalculateAll();
            }
        }

        private void CalculateButton_Clicked(object sender, EventArgs e)
        {
            RecalculateAll();
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            var json = JsonSerializer.Serialize(cellExpressions, new JsonSerializerOptions { WriteIndented = true });
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var result = await FileSaver.Default.SaveAsync("table.json", stream, CancellationToken.None);
            if (result.IsSuccessful)
            {
                await DisplayAlertAsync("Збереження", "Таблицю збережено у файл:\n" + result.FilePath, "OK");
            }
            else if (result.Exception != null)
            {
                await DisplayAlertAsync("Збереження", "Файл не збережено: " + result.Exception.Message, "OK");
            }
        }

        private async void ReadButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var jsonType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.json" } }
                });

                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Оберіть файл таблиці",
                    FileTypes = jsonType
                });

                if (file != null)
                {
                    using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();

                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null)
                    {
                        cellExpressions.Clear();
                        foreach (var pair in data)
                        {
                            cellExpressions[pair.Key] = pair.Value;
                        }
                        RecalculateAll();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Помилка", "Не вдалося прочитати файл: " + ex.Message, "OK");
            }
        }


        private async void ExitButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlertAsync("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                System.Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlertAsync("Довідка", "Лабораторна робота 1. Варіант 34", "OK");
        }

        private void DeleteRowButton_Clicked(object sender, EventArgs e)
        {
            if (grid.RowDefinitions.Count > 1)
            {
                int lastRowIndex = grid.RowDefinitions.Count - 1;
                grid.RowDefinitions.RemoveAt(lastRowIndex);
                grid.Children.RemoveAt(lastRowIndex * (CountColumn + 1));
                for (int col = 0; col < CountColumn; col++)
                {
                    grid.Children.RemoveAt((lastRowIndex * CountColumn) + col + 1);
                }
            }
        }

        private void AddRowButton_Clicked(object sender, EventArgs e)
        {
            int newRow = grid.RowDefinitions.Count;

            grid.RowDefinitions.Add(new RowDefinition());

            var label = new Label
            {
                Text = newRow.ToString(),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetRow(label, newRow);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            for (int col = 0; col < CountColumn; col++)
            {
                var entry = new Entry
                {
                    Text = "",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                entry.Unfocused += Entry_Unfocused;
                entry.Focused += Entry_Focused;
                Grid.SetRow(entry, newRow);
                Grid.SetColumn(entry, col + 1);
                grid.Children.Add(entry);
            }
        }
    }
}
