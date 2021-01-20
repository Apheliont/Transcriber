
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transcriber.Core.Models;
using Transcriber.Core.Services;
using Transcriber.Core.Util;

namespace Transcriber.Core.ViewModels
{
    public class RootViewModel : MvxViewModel
    {

        private Process ffmpegProcess;
        private CancellationTokenSource _cancellationTokenSource = null;
        private readonly ITransportService _transportService;
        private string _debugInfo;
        private readonly int[] _fontSizes;
        private int _selectedFontSize;
        private TranscriptionModel _transcriptionModel;
        private readonly IConfigurationService _configurationService;
        private KeyValuePair<string, string> _selectedLanguage;

        private string _selectedFilePath;
        private readonly ITranscribeService _transcribeService;

        private IEnumerable<KeyValuePair<string, string>> _availableLanguages;

        private readonly MvxInteraction<YesNoQuestion> _yesNoInteraction = new MvxInteraction<YesNoQuestion>();

        private WasapiCapture _audioCapture;
        private bool _isTranscribingInProgress;

        private float _recordLevel = 0.0F;

        public MvxCommand ToggleTranscriptionCommand { get; set; }
        public MvxCommand ClearTextCommand { get; set; }
        public MvxAsyncCommand TranscribeFileCommand { get; set; }
        public IMvxInteraction<YesNoQuestion> YesNoInteraction => _yesNoInteraction;
        public RootViewModel(ITranscribeService transcribeService, IConfigurationService _configurationService)
        {

            _fontSizes = new int[] { 8, 12, 14, 16, 18, 20, 22, 24, 26 };
            _selectedFontSize = _fontSizes[3];
            _isTranscribingInProgress = false;
            _transportService = Mvx.IoCProvider.Resolve<ITransportService>();
            _transcriptionModel = new TranscriptionModel();
            _configurationService = new ConfigurationService();
            _transcribeService = transcribeService;
            _transcribeService.NewTranscriptionData += ProcessRawData;
            _transcribeService.PercentageTranscribed += UpdatePercentageTranscribed;

            ToggleTranscriptionCommand = new MvxCommand(StartTranscribing);
            ClearTextCommand = new MvxCommand(() => {
                TranscriptionModel.Clear();
                RaisePropertyChanged(TranscriptionModel.Text);
            });
            TranscribeFileCommand = new MvxAsyncCommand(TranscribeFile);
            //Keys.AsEnumerable();
            AvailableLanguages = _configurationService.GetLanguages().ToArray();
            if (AvailableLanguages.Count() != 0)
            {
                SelectedLanguage = AvailableLanguages.FirstOrDefault();
            }

        }

        public int SelectedFontSize
        {
            get => _selectedFontSize;
            set
            {
                SetProperty(ref _selectedFontSize, value);
            }
        }

        public int[] FontSizes
        {
            get => _fontSizes;
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
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            await _transcribeService.TranscribeFile(SelectedFilePath, cancellationToken);
            _cancellationTokenSource.Dispose();
            IsTranscribingInProgress = false;
        }

        public TranscriptionModel TranscriptionModel
        {
            get => _transcriptionModel;
            set
            {
                if (_transcriptionModel != value)
                {
                    SetProperty(ref _transcriptionModel, value);
                }
            }
        }

    private void StartTranscribing()
        {
            if (!IsTranscribingInProgress)
            {
                if (TranscriptionModel.Text.Length > 0)
                {
                    var request = new YesNoQuestion
                    {
                        YesNoCallback = (ok) =>
                        {
                            if (!ok)
                                return;
                        },
                        Question = "Весь несохраненный текст будет потерян. Вы уверены?"
                    };

                    _yesNoInteraction.Raise(request);
                }
                if (CanProcessFile)
                {
                    TranscribeFileCommand.Execute();
                }
                else
                {
                    StartRecord();
                }
                IsTranscribingInProgress = true;
            }
            else
            {
                if (CanProcessFile)
                {
                    if (_cancellationTokenSource != null)
                    {
                        _cancellationTokenSource.Cancel();
                    }

                }
                else
                {
                    StopRecording();
                }
                IsTranscribingInProgress = false;
            }
        }

        public string ToggleTranscriptionBtnContent => !IsTranscribingInProgress ? "Старт" : "Стоп";



        private void UpdatePercentageTranscribed(object sender, int percentage)
        {
            PercentageTranscribed = percentage;
        }

        private int _percentageTranscribed;

        public int PercentageTranscribed
        {
            get => _percentageTranscribed;
            set
            {
                SetProperty(ref _percentageTranscribed, value);
            }
        }



        private void ProcessRawData(object sender, string rawStr)
        {
            try
            {
                JObject respond = JObject.Parse(rawStr);
                if (respond.ContainsKey("partial"))
                {
                    TranscriptionModel.Partial = respond["partial"].ToString();
                    RaisePropertyChanged(() => TranscriptionModel);
                }
                if (respond.ContainsKey("text"))
                {
                    TranscriptionModel.Text = respond["text"].ToString();
                    RaisePropertyChanged(() => TranscriptionModel);
                }
            }
            catch (JsonReaderException e)
            {
                DebugInfo = e.Message;
            }

        }


        public string DebugInfo
        {
            get => _debugInfo;
            set
            {
                SetProperty(ref _debugInfo, value);
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




        private int _threadSafeBoolBackValue = 0;
        /// Lock to process items in the queue one at time.
        public bool StreamingIsBusy
        {
            get => (Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 1) == 1);
            set
            {
                if (value) Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 0);
                else Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 0, 1);
            }
        }



        private void StartRecord()
        {
            try
            {
                _audioCapture = new WasapiLoopbackCapture();
                DebugInfo = _audioCapture.WaveFormat.Encoding.ToString();
                ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = "ffmpeg.exe";
                ffmpegProcess.StartInfo.Arguments = $"-f f32le -ac 2 -ar 44100 -i - -ar 8000 -ac 1 -f s16le -";
                ffmpegProcess.StartInfo.RedirectStandardInput = true;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.Start();


                _audioCapture.RecordingStopped += OnRecordingStopped;
                _audioCapture.DataAvailable += OnDataAvailable;

                /* Message = "Recording...";*/

                _audioCapture.StartRecording();
            }
            catch (Exception e)
            {
                /*MessageBox.Show(e.Message);*/
            }
        }

        private float CalculateRecordLevel(WaveInEventArgs args)
        {
            float max = 0;
            var buffer2 = new WaveBuffer(args.Buffer);
            // interpret as 32 bit floating point audio
            for (int index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer2.FloatBuffer[index];

                // absolute value 
                if (sample < 0) sample = -sample;
                // is this the max value?
                if (sample > max) max = sample;
            }

            return max;
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception == null)
            {
                /*Message = "Recording Stopped";*/
            }
            else
            {
                /*Message = "Recording Error: " + e.Exception.Message;*/
            }
            ffmpegProcess.StandardInput.BaseStream.Flush();
            ffmpegProcess.StandardInput.BaseStream.Close();
            ffmpegProcess?.Kill();

            _audioCapture.Dispose();
            _audioCapture = null;

            RecordLevel = 0.0F;
            IsTranscribingInProgress = false;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            var ffmpegIn = ffmpegProcess.StandardInput.BaseStream;
            RecordLevel = CalculateRecordLevel(waveInEventArgs);
            ffmpegIn.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            Task.Run(async () => await WriteData());
        }

        private async Task WriteData()
        {
            if (StreamingIsBusy) return;
            // Лочим под отдельный поток
            StreamingIsBusy = true;

            int read;
            var buffer = new byte[8000];
            while ((read = ffmpegProcess.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                await _transcribeService.TranscribeChunk(buffer, read);
            }

            StreamingIsBusy = false;
        }


        private void StopRecording()
        {
            _audioCapture?.StopRecording();
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


        // =========Capture Audio End ============


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
