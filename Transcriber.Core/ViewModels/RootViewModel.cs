
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transcriber.Core.Models;
using Transcriber.Core.Services;

namespace Transcriber.Core.ViewModels
{
    public class RootViewModel : MvxViewModel
    {
        private CancellationTokenSource _cancellationTokenSource = null;
        private string _debugInfo;
        private int[] _fontSizes;
        private int _selectedFontSize;
        private TranscriptionModel _transcriptionModel;
        private readonly IConfigurationService _configurationService;
        private string _selectedLanguage;
        private ConcurrentQueue<byte[]> _bufferQueue = new ConcurrentQueue<byte[]>();
        private BufferedWaveProvider _bufferedWaveProvider;
        private MediaFoundationResampler _resampler;
        private string _selectedFilePath;
        private readonly ITranscribeService _transcribeService;

        private IEnumerable<string> _availableLanguages;


        /// Records the audio of the selected device.
        private WasapiCapture _audioCapture;
        private bool _isTranscribingInProgress;
        /// Converts the device source into a wavesource.

        private float _recordLevel = 0;

        public MvxCommand ToggleTranscriptionCommand { get; set; }
        public MvxCommand ClearTextCommand { get; set; }
        public MvxAsyncCommand TranscribeFileCommand { get; set; }

        public RootViewModel(ITranscribeService transcribeService, IConfigurationService _configurationService)
        {
            _fontSizes = new int[] { 8, 12, 14, 16, 18, 20, 22, 24 };
            _selectedFontSize = _fontSizes[3];
            _isTranscribingInProgress = false;
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

            AvailableLanguages = _configurationService.GetLanguages().Keys.AsEnumerable();
            if (AvailableLanguages.Count() != 0)
            {
                SelectedLanguage = AvailableLanguages.First();
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
            DebugInfo = percentage.ToString();
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

                _audioCapture.RecordingStopped += OnRecordingStopped;
                _audioCapture.DataAvailable += OnDataAvailable;

                /* Message = "Recording...";*/
                _bufferedWaveProvider = new BufferedWaveProvider(_audioCapture.WaveFormat);

                _resampler = new MediaFoundationResampler(_bufferedWaveProvider, new WaveFormat(8000, 16, 1));
                _resampler.ResamplerQuality = 60;

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

            _audioCapture.Dispose();
            _audioCapture = null;
            _bufferQueue = new ConcurrentQueue<byte[]>();
            _bufferedWaveProvider.ClearBuffer();
            RecordLevel = 0.0F;
            IsTranscribingInProgress = false;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            RecordLevel = CalculateRecordLevel(waveInEventArgs);

            _bufferedWaveProvider.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            byte[] buffer = new byte[1000];
            _resampler.Read(buffer, 0, buffer.Length);

            _bufferQueue.Enqueue(buffer);
            Task.Run(WriteData);

        }

        private async Task WriteData()
        {
            if (StreamingIsBusy) return;
            // Лочим под отдельный поток
            StreamingIsBusy = true;

            // отправляем серверу куски по 8000 байт
            byte[] buf = new byte[8000];

            while (true)
            {
                if (_bufferQueue.Count < 8 && _audioCapture != null)
                {
                    await Task.Delay(300);
                    continue;
                }
                if (_bufferQueue.Count == 0 && _audioCapture == null)
                {
                    break;
                }


                for (int i = 0; i < 8; i++)
                {
                    if (_bufferQueue.TryDequeue(out byte[] buffer))
                    {
                        Buffer.BlockCopy(buffer, 0, buf, i * 1000, buffer.Length);
                    }
                    else
                    {
                        buf = buf.Take(i * 1000).ToArray();
                        break;
                    }
                }
                await _transcribeService.TranscribeChunk(buf, buf.Length);

            }
            StreamingIsBusy = false;
        }


        private void StopRecording()
        {
            _audioCapture?.StopRecording();
        }

        public IEnumerable<string> AvailableLanguages
        {
            get => _availableLanguages;
            set
            {
                SetProperty(ref _availableLanguages, value);
            }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                SetProperty(ref _selectedLanguage, value);
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
