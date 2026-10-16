using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using System;
using System.Collections.Generic;
using Grid = Microsoft.Maui.Controls.Grid;
using LabCalculator;

namespace UI
{
    public partial class MainPage : ContentPage
    {
        const int CountColumn = 26; 
        const int CountRow = 50;

        private readonly Dictionary<string, string> cellExpressions = new Dictionary<string, string>();

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
                        catch (Exception)
                        {
                            entry.Text = "Error";
                        }
                    }
                    else
                    {
                        entry.Text = "";
                    }
                }
            }
        }

        private void Entry_Focused(object sender, FocusEventArgs e)
        {
            var entry = (Entry)sender;
            var name = GetCellName(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1);
            string expr;
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
        }

        //створення таблиці
        private void CreateGrid()
        {
            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
        }

        private void AddColumnsAndColumnLabels()
        {
            // Додати стовпці та підписи для стовпців
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
            // Додати рядки, підписи для рядків та комірки
            for (int row = 0; row < CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition());

                // Додати підпис для номера рядка
                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, row + 1);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                // Додати комірки (Entry) для вмісту
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

        private void Entry_Unfocused(object sender, FocusEventArgs e)
        {
            var entry = (Entry)sender;
            var name = GetCellName(Grid.GetRow(entry) - 1, Grid.GetColumn(entry) - 1);
            cellExpressions[name] = entry.Text ?? "";
            RecalculateAll();
        }

        private void CalculateButton_Clicked(object sender, EventArgs e)
        {
            RecalculateAll();
        }

        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            // Обробка кнопки "Зберегти"
        }

        private void ReadButton_Clicked(object sender, EventArgs e)
        {
            // Обробка кнопки "Прочитати"
        }

        private async void ExitButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                System.Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Довідка", "Лабораторна робота 1. Варіант 34", "OK");
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
