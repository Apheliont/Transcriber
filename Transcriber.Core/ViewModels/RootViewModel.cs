using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transcriber.Core.Services;
using Transcriber.Core.Services.Interfaces;
using Transcriber.Core.Util;

namespace Transcriber.Core.ViewModels
{
    public class RootViewModel : MvxViewModel
    {
        private readonly IServerResponseService _serverResponseService;
        private readonly IRecordService _recordService;
        private readonly ITransportService _transportService;
        private readonly IConfigurationService _configurationService;
        private readonly IFileProcessService _fileProcessService;
        private KeyValuePair<string, string> _selectedLanguage;
        private IEnumerable<KeyValuePair<string, string>> _availableLanguages;
        private string _transcription = string.Empty;
        private string _infoMessage;
        private readonly int[] _fontSizes;
        private int _selectedFontSize;
        private int _percentageTranscribed;
        private string _selectedFilePath;

        private readonly MvxInteraction<YesNoQuestion> _yesNoInteraction = new MvxInteraction<YesNoQuestion>();

        private bool _isTranscribingInProgress = false;

        private float _recordLevel = 0.0F;

        public MvxCommand ToggleTranscriptionCommand { get; set; }
        public MvxCommand ClearTextCommand { get; set; }
        public MvxAsyncCommand TranscribeFileCommand { get; set; }
        public IMvxInteraction<YesNoQuestion> YesNoInteraction => _yesNoInteraction;

        public RootViewModel(
            IServerResponseService serverResponseService,
            IFileProcessService fileProcessService,
            IConfigurationService _configurationService,
            IRecordService recordService)
        {
            _recordService = recordService;
            _recordService.RecordStopped += delegate { IsTranscribingInProgress = false; };
            _recordService.InfoMessage += delegate (object _sender, string message) { InfoMessage = message; };
            _recordService.RecordLevel += delegate (object _sender, float level) { RecordLevel = level; };

            _fontSizes = new int[] { 8, 12, 14, 16, 18, 20, 22, 24, 26 };
            _selectedFontSize = _fontSizes[3];

            _transportService = Mvx.IoCProvider.Resolve<ITransportService>();
            _transportService.InfoMessage += delegate (object _sender, string message) { InfoMessage = message; };

            _configurationService = new ConfigurationService();

            _fileProcessService = fileProcessService;
            _fileProcessService.PercentageTranscribed += UpdatePercentageTranscribed;
            _fileProcessService.InfoMessage += delegate (object _sender, string message) { InfoMessage = message; };

            _serverResponseService = serverResponseService;
            _serverResponseService.HandledServerResponse += delegate (object _sender, string resp) { Transcription = resp; };
            _serverResponseService.InfoMessage += delegate (object _sender, string message) { InfoMessage = message; };

            ToggleTranscriptionCommand = new MvxCommand(StartTranscribing);
            ClearTextCommand = new MvxCommand(_serverResponseService.Clear);
            TranscribeFileCommand = new MvxAsyncCommand(TranscribeFile);

            AvailableLanguages = _configurationService.GetLanguages().ToArray();
            if (AvailableLanguages.Count() != 0)
            {
                SelectedLanguage = AvailableLanguages.Where(x => x.Key.ToLower() == "russian").FirstOrDefault();
            }

        }

        public int[] FontSizes => _fontSizes;
        public int SelectedFontSize
        {
            get => _selectedFontSize;
            set
            {
                SetProperty(ref _selectedFontSize, value);
            }
        }


        public bool IsTranscribingInProgress {
            get => _isTranscribingInProgress;
            set
            {
                SetProperty(ref _isTranscribingInProgress, value);
                RaisePropertyChanged(() => ToggleTranscriptionBtnContent);
            }
        }

        private async Task TranscribeFile()
        {
            await _fileProcessService.TranscribeFile(SelectedFilePath);
            IsTranscribingInProgress = false;
        }

        public string Transcription
        {
            get => _transcription;
            set
            {
                if (_transcription != value)
                {
                    SetProperty(ref _transcription, value);
                }
            }
        }

    private void StartTranscribing()
        {
            if (!IsTranscribingInProgress)
            {
                if (Transcription.Length > 0)
                {
                    /*var request = new YesNoQuestion
                    {
                        YesNoCallback = (ok) =>
                        {
                            if (!ok)
                                return;
                        },
                        Question = "Весь несохраненный текст будет потерян. Вы уверены?"
                    };

                    _yesNoInteraction.Raise(request);*/
                }
                if (CanProcessFile)
                {
                    TranscribeFileCommand.Execute();
                }
                else
                {
                    _recordService.StartRecord();
                }
                IsTranscribingInProgress = true;
            }
            else
            {
                if (CanProcessFile)
                {
                    _fileProcessService.Stop();

                }
                else
                {
                    _recordService.StopRecording();
                }
                IsTranscribingInProgress = false;
            }
        }

        public string ToggleTranscriptionBtnContent => !IsTranscribingInProgress ? "Старт" : "Стоп";



        private void UpdatePercentageTranscribed(object sender, int percentage)
        {
            PercentageTranscribed = percentage;
        }



        public int PercentageTranscribed
        {
            get => _percentageTranscribed;
            set
            {
                SetProperty(ref _percentageTranscribed, value);
            }
        }


        public string InfoMessage
        {
            get => _infoMessage;
            set
            {
                SetProperty(ref _infoMessage, value);
            }
        }

        public float RecordLevel
        {
            get => _recordLevel;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_recordLevel != value)
                {
                    SetProperty(ref _recordLevel, value * 100);
                }
            }
        }



        public IEnumerable<KeyValuePair<string, string>> AvailableLanguages
        {
            get => _availableLanguages;
            set
            {
                SetProperty(ref _availableLanguages, value);
            }
        }

        public KeyValuePair<string, string> SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                SetProperty(ref _selectedLanguage, value);
                _transportService.SetAddress(value.Value);
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
