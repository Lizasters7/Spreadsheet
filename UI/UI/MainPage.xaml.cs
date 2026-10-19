using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using System;
using System.Collections.Generic;
using Grid = Microsoft.Maui.Controls.Grid;
using LabCalculator;
using CommunityToolkit.Maui.Storage;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UI
{
    public partial class MainPage : ContentPage
    {
        const int CountColumn = 26;
        const int CountRow = 50;

        private int selectedRow = -1;      
        private int selectedColumn = -1;

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
                            if (result is bool b)
                            {
                                if (b)
                                {
                                    entry.Text = "True";
                                }
                                else
                                {
                                    entry.Text = "False";
                                }
                            }
                            else
                            {
                                entry.Text = result.ToString();
                            }
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
                selectedRow = Grid.GetRow(entry);
                selectedColumn = Grid.GetColumn(entry);

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
            grid.RowDefinitions.Add(new RowDefinition());
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

        private int GetColumnIndex(string letters)
        {
            int result = 0;
            foreach (char c in letters.ToUpper())
            {
                result = result * 26 + (c - 'A' + 1);
            }
            return result;
        }

        private string? ShiftCellName(string name, int deletedRow, int deletedColumn)
        {
            string letters = new string(name.TakeWhile(char.IsLetter).ToArray());
            int column = GetColumnIndex(letters);
            int row = int.Parse(name.Substring(letters.Length));

            if (row == deletedRow || column == deletedColumn)
            {
                return null;
            }
            if (deletedRow != -1 && row > deletedRow)
            {
                row--;
            }
            if (deletedColumn != -1 && column > deletedColumn)
            {
                column--;
            }
            return GetColumnName(column) + row;
        }

        private string UpdateReferences(string expression, int deletedRow, int deletedColumn)
        {
            string result = "";
            int lastIndex = 0;  

            foreach (Match match in Regex.Matches(expression, "[A-Za-z]+[1-9][0-9]*"))
            {
                result += expression.Substring(lastIndex, match.Index - lastIndex);
                string? newName = ShiftCellName(match.Value, deletedRow, deletedColumn);
                if (newName == null)
                {
                    result += "#REF";
                }
                else
                {
                    result += newName;
                }

                lastIndex = match.Index + match.Length;
            }

            result += expression.Substring(lastIndex);
            return result;
        }

        private void ShiftExpressions(int deletedRow, int deletedColumn)
        {
            var updated = new Dictionary<string, string>();
            foreach (var pair in cellExpressions)
            {
                string? newName = ShiftCellName(pair.Key, deletedRow, deletedColumn);
                if (newName != null)
                {
                    updated[newName] = UpdateReferences(pair.Value, deletedRow, deletedColumn);
                }
            }
            cellExpressions.Clear();
            foreach (var pair in updated)
            {
                cellExpressions[pair.Key] = pair.Value;
            }
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

        private void DeleteRow(int rowIndex)
        {
            var viewsToRemove = new List<View>();
            foreach (var child in grid.Children)
            {
                if (child is View view)
                {
                    int row = Grid.GetRow(view);
                    if (row == rowIndex)
                    {
                        viewsToRemove.Add(view);
                    }
                    else if (row > rowIndex)
                    {
                        Grid.SetRow(view, row - 1);  
                        if (view is Label label && Grid.GetColumn(view) == 0)
                        {
                            label.Text = (row - 1).ToString();   
                        }
                    }
                }
            }
            foreach (var view in viewsToRemove)
            {
                grid.Children.Remove(view);
            }
            grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);

            ShiftExpressions(rowIndex, -1);
            selectedRow = -1;
            selectedColumn = -1;
            RecalculateAll();
        }

        private void DeleteColumn(int columnIndex)
        {
            var viewsToRemove = new List<View>();
            foreach (var child in grid.Children)
            {
                if (child is View view)
                {
                    int column = Grid.GetColumn(view);
                    if (column == columnIndex)
                    {
                        viewsToRemove.Add(view);
                    }
                    else if (column > columnIndex)
                    {
                        Grid.SetColumn(view, column - 1);   
                        if (view is Label label && Grid.GetRow(view) == 0)
                        {
                            label.Text = GetColumnName(column - 1);   
                        }
                    }
                }
            }
            foreach (var view in viewsToRemove)
            {
                grid.Children.Remove(view);
            }
            grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);

            ShiftExpressions(-1, columnIndex);
            selectedRow = -1;
            selectedColumn = -1;
            RecalculateAll();
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

        private async void DeleteRowButton_Clicked(object sender, EventArgs e)
        {
            if (selectedRow < 1)
            {
                await DisplayAlertAsync("Видалення рядка", "Спочатку клацніть на будь-яку комірку рядка, який треба видалити.", "OK");
            }
            else if (grid.RowDefinitions.Count <= 2)
            {
                await DisplayAlertAsync("Видалення рядка", "У таблиці має лишитися хоча б один рядок.", "OK");
            }
            else
            {
                bool answer = await DisplayAlertAsync("Видалення рядка", $"Видалити рядок {selectedRow}?", "Так", "Ні");
                if (answer)
                {
                    DeleteRow(selectedRow);
                }
            }
        }

        private async void DeleteColumnButton_Clicked(object sender, EventArgs e)
        {
            if (selectedColumn < 1)
            {
                await DisplayAlertAsync("Видалення стовпчика", "Спочатку клацніть на будь-яку комірку стовпчика, який треба видалити.", "OK");
            }
            else if (grid.ColumnDefinitions.Count <= 2)
            {
                await DisplayAlertAsync("Видалення стовпчика", "У таблиці має лишитися хоча б один стовпчик.", "OK");
            }
            else
            {
                bool answer = await DisplayAlertAsync("Видалення стовпчика", $"Видалити стовпчик {GetColumnName(selectedColumn)}?", "Так", "Ні");
                if (answer)
                {
                    DeleteColumn(selectedColumn);
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

            RecalculateAll();
        }

        private void AddColumnButton_Clicked(object sender, EventArgs e)
        {
            int newColumn = grid.ColumnDefinitions.Count;

            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new Label
            {
                Text = GetColumnName(newColumn),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, newColumn);
            grid.Children.Add(label);

            for (int row = 1; row < grid.RowDefinitions.Count; row++)
            {
                var entry = new Entry
                {
                    Text = "",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                entry.Unfocused += Entry_Unfocused;
                entry.Focused += Entry_Focused;
                Grid.SetRow(entry, row);
                Grid.SetColumn(entry, newColumn);
                grid.Children.Add(entry);
            }

            RecalculateAll();
        }
    }
}
