using Core.Entities;
using Core.Interfaces.Services;
using System.Diagnostics;
using System.Windows;
using WPF.Utilities;
using WPF.ViewModels;

namespace MemoSphere.WPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(
            MainViewModel viewModel)
        {
            _viewModel = viewModel;

            DataContext = _viewModel;
            InitializeComponent();
        }

        public async Task LoadDataAsync()
        {
            await InitializeDataAndRefreshAsync(_viewModel);
        }

        private async Task InitializeDataAndRefreshAsync(MainViewModel mainViewModel)
        {
            try
            {
                await mainViewModel.InitializeAsync();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Startup error: {ex}");
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}