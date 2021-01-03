using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;
using Transcriber.Core.Services;

namespace Transcriber.Core.ViewModels
{
    public class RootViewModel : MvxViewModel
    {
        private string _selectedFilePath;
        private string _transcription;
        private readonly ITranscribeService _transcribeService;
        public IMvxAsyncCommand TranscribeFromFileCommand { get; set; }

        public RootViewModel(ITranscribeService transcribeService)
        {
            _transcribeService = transcribeService;
            TranscribeFromFileCommand = new MvxAsyncCommand(async () =>
            {
                Transcription = await _transcribeService.TranscribeFile(SelectedFilePath);
            });
        }



        public string Transcription
        {
            get { return _transcription; }
            set
            {
                SetProperty(ref _transcription, value);
            }
        }

        public string SelectedFilePath
        {
            get { return _selectedFilePath; }
            set
            {
                SetProperty(ref _selectedFilePath, value);
                RaisePropertyChanged(() => CanProcessFile);
            }
        }

        public bool CanProcessFile => SelectedFilePath?.Length > 0;

    }
}
