using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;

namespace RecipeFilterApp
{
    public partial class MainWindow : Window
    {
        public List<Recipe> Recipes { get; set; } = new List<Recipe>();
        private List<IngredientDetail> CurrentIngredients { get; set; } = new List<IngredientDetail>();

        public MainWindow()
        {
            InitializeComponent();
            RecipeListView.ItemsSource = Recipes;
            IngredientListView.ItemsSource = CurrentIngredients;
            PieChart.Series = new SeriesCollection();
        }

        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            var ingredientName = IngredientTextBox.Text;
            var foodGroup = ((ComboBoxItem)FoodGroupComboBox.SelectedItem)?.Content.ToString();
            if (int.TryParse(CaloriesTextBox.Text, out int calories))
            {
                CurrentIngredients.Add(new IngredientDetail { Name = ingredientName, FoodGroup = foodGroup, Calories = calories, OriginalCalories = calories });
                IngredientTextBox.Clear();
                CaloriesTextBox.Clear();
                UpdateIngredientListView();
            }
        }

        private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var recipeName = RecipeNameTextBox.Text;
            if (!string.IsNullOrWhiteSpace(recipeName) && CurrentIngredients.Any())
            {
                var totalCalories = CurrentIngredients.Sum(i => i.Calories);
                var newRecipe = new Recipe { Name = recipeName, TotalCalories = totalCalories, Ingredients = new List<IngredientDetail>(CurrentIngredients) };
                Recipes.Add(newRecipe);
                Recipes = Recipes.OrderBy(r => r.Name).ToList(); // Sort alphabetically
                UpdateRecipeListView();
                CurrentIngredients.Clear();
                RecipeNameTextBox.Clear();
                UpdateIngredientListView();
            }
            else
            {
                MessageBox.Show("Please enter a recipe name and at least one ingredient.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecipes = RecipeListView.SelectedItems.Cast<Recipe>().ToList();
            foreach (var recipe in selectedRecipes)
            {
                Recipes.Remove(recipe);
            }
            UpdateRecipeListView();
        }

        private void EditRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipeListView.SelectedItem is Recipe selectedRecipe)
            {
                RecipeNameTextBox.Text = selectedRecipe.Name;
                CurrentIngredients = new List<IngredientDetail>(selectedRecipe.Ingredients);
                UpdateIngredientListView();
                Recipes.Remove(selectedRecipe);
                UpdateRecipeListView();
            }
            else
            {
                MessageBox.Show("Please select a recipe to edit.", "Edit Recipe", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ScaleRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ScalingFactorTextBox.Text, out double scalingFactor) && scalingFactor > 0)
            {
                foreach (var ingredient in CurrentIngredients)
                {
                    ingredient.Calories = (int)(ingredient.OriginalCalories * scalingFactor);
                }
                UpdateIngredientListView();
            }
        }

        private void ResetScaleButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ingredient in CurrentIngredients)
            {
                ingredient.Calories = ingredient.OriginalCalories;
            }
            ScalingFactorTextBox.Clear();
            UpdateIngredientListView();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var filteredRecipes = Recipes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(IngredientTextBox.Text))
            {
                filteredRecipes = filteredRecipes.Where(r => r.Ingredients.Any(i => i.Name.Contains(IngredientTextBox.Text, StringComparison.OrdinalIgnoreCase)));
            }

            if (FoodGroupComboBox.SelectedItem is ComboBoxItem selectedFoodGroup)
            {
                var foodGroup = selectedFoodGroup.Content.ToString();
                filteredRecipes = filteredRecipes.Where(r => r.Ingredients.Any(i => i.FoodGroup == foodGroup));
            }

            if (int.TryParse(CaloriesTextBox.Text, out int maxCalories))
            {
                filteredRecipes = filteredRecipes.Where(r => r.TotalCalories <= maxCalories);
            }

            RecipeListView.ItemsSource = filteredRecipes.ToList();
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            IngredientTextBox.Clear();
            FoodGroupComboBox.SelectedIndex = -1;
            CaloriesTextBox.Clear();
            RecipeListView.ItemsSource = Recipes;
        }

        private void RecipeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRecipes = RecipeListView.SelectedItems.Cast<Recipe>().ToList();

            if (selectedRecipes.Count == 1)
            {
                var selectedRecipe = selectedRecipes[0];
                CurrentIngredients = new List<IngredientDetail>(selectedRecipe.Ingredients);
                UpdateIngredientListView();
                TotalCaloriesTextBlock.Text = $"Total Calories: {selectedRecipe.TotalCalories}";
                UpdatePieChart(selectedRecipe.Ingredients);
            }
            else if (selectedRecipes.Count > 1)
            {
                var combinedIngredients = selectedRecipes.SelectMany(r => r.Ingredients).GroupBy(i => i.FoodGroup)
                                                         .Select(g => new IngredientDetail { Name = g.Key, FoodGroup = g.Key, Calories = g.Sum(i => i.Calories) })
                                                         .ToList();
                UpdatePieChart(combinedIngredients);
            }
            else
            {
                CurrentIngredients.Clear();
                UpdateIngredientListView();
                TotalCaloriesTextBlock.Text = "Total Calories: 0";
                PieChart.Series.Clear();
            }
        }

        private void UpdateRecipeListView()
        {
            RecipeListView.ItemsSource = null;
            RecipeListView.ItemsSource = Recipes; // Refresh the ListView
        }

        private void UpdateIngredientListView()
        {
            IngredientListView.ItemsSource = null;
            IngredientListView.ItemsSource = CurrentIngredients; // Refresh the ListView
        }

        private void UpdatePieChart(List<IngredientDetail> ingredients)
        {
            PieChart.Series.Clear();

            var foodGroupTotals = ingredients.GroupBy(i => i.FoodGroup)
                                             .Select(g => new PieSeries
                                             {
                                                 Title = g.Key,
                                                 Values = new ChartValues<double> { g.Sum(i => i.Calories) }
                                             });

            PieChart.Series = new SeriesCollection();
            foreach (var series in foodGroupTotals)
            {
                PieChart.Series.Add(series);
            }
        }
    }

    public class Recipe
    {
        public string Name { get; set; }
        public int TotalCalories { get; set; }
        public List<IngredientDetail> Ingredients { get; set; }
    }

    public class IngredientDetail
    {
        public string Name { get; set; }
        public string FoodGroup { get; set; }
        public int Calories { get; set; }
        public int OriginalCalories { get; set; } // Store the original calorie value for scaling purposes
    }
}
