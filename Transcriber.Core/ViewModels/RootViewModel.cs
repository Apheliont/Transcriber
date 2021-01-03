using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Transcriber.Core.Services;

namespace Transcriber.Core.ViewModels
{
    public class RootViewModel : MvxViewModel
    {
        private string _selectedFilePath;
        private ITranscribeService _transcribeService;

        public RootViewModel(ITranscribeService transcribeService)
        {
            _transcribeService = transcribeService;
        }

        public string SelectedFilePath
        {
            get { return _selectedFilePath; }
            set { SetProperty(ref _selectedFilePath, value); }
        }

        public bool CanProcessFile => SelectedFilePath?.Length > 0;

    }
}
