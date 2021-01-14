using Microsoft.Win32;
using MvvmCross.Platforms.Wpf.Views;
using Transcriber.Core.ViewModels;

namespace Transcriber.Wpf.Views
{
    public partial class RootView : MvxWpfView
    {

        public RootView()
        {
            InitializeComponent();
        }

        private void SelectFilePath(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Media Files |*.wav;*.mp3;*.mp4;*.avi;*.mov;*.mpeg;*.mkv;*.mxf",
                Multiselect = false,
                Title = "Please select a wav file."
            };

            if ((bool)dialog.ShowDialog())
            {
                var _viewModel = ViewModel as RootViewModel;
                _viewModel.SelectedFilePath = dialog.FileName;
            }
        }
    }
}
