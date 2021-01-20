using Microsoft.Win32;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using Transcriber.Core.Services;
using Transcriber.Core.Util;
using Transcriber.Core.ViewModels;
using Transcriber.Wpf.Util;

namespace Transcriber.Wpf.Views
{
    public partial class RootView : MvxWpfView
    {
        private IMvxInteraction<YesNoQuestion> _interaction;
        public RootView()
        {
            InitializeComponent();
/*            Task.Delay(1000);
            var set = this.CreateBindingSet<RootView, RootViewModel>();
            set.Bind(this).For(view => view.Interaction).To(viewModel => viewModel.YesNoInteraction).OneWay();
            set.Apply();*/
        }


        public IMvxInteraction<YesNoQuestion> Interaction
        {
            get => _interaction;
            set
            {
                if (_interaction != null)
                {
                    _interaction.Requested -= OnInteractionRequested;
                }


                _interaction = value;
                _interaction.Requested += OnInteractionRequested;
            }
        }

        private void OnInteractionRequested(object sender, MvxValueEventArgs<YesNoQuestion> e)
        {
            var yesNoQuestion = e.Value;
            // show dialog
            MessageBoxButton btn = MessageBoxButton.OKCancel;
            
            var status = MessageBox.Show(yesNoQuestion.Question, "Продолжить?", btn);
            yesNoQuestion.YesNoCallback(status == MessageBoxResult.Yes);
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
