
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
using Transcriber.Core.Services;

namespace Transcriber.Core.ViewModels
{


    public class RootViewModel : MvxViewModel
    {
        private string _debugInfo;
        private string _transcription;
        private string _partial;

        private ConcurrentQueue<byte[]> _bufferQueue = new ConcurrentQueue<byte[]>();
        private BufferedWaveProvider _bufferedWaveProvider;
        private MediaFoundationResampler _resampler;
        private string _selectedFilePath;
        private readonly ITranscribeService _transcribeService;
        private MMDevice _selectedDevice;
        private ObservableCollection<MMDevice> _deviceNames;
        private bool _enableStartRecord;
        private bool _stopRecordStopRecord;
        /// Records the audio of the selected device.
        private WasapiCapture _audioCapture;

        /// Converts the device source into a wavesource.

        private float recordLevel;

        public IMvxAsyncCommand TranscribeFromFileCommand { get; set; }
        public IMvxCommand StopRecordingCommand { get; set; }
        public IMvxCommand StartRecordingCommand { get; set; }


        public RootViewModel(ITranscribeService transcribeService)
        {
            _transcription = string.Empty;
            _partial = string.Empty;

            //_writer = new WaveFileWriter("test.wav", new WaveFormat(8000, 16, 1));
            _transcribeService = transcribeService;
            _transcribeService.NewTranscriptionData += ProcessRawData;
            TranscribeFromFileCommand = new MvxAsyncCommand(async () => await _transcribeService.TranscribeFile(SelectedFilePath));

            StopRecordingCommand = new MvxCommand(StopRecording);

            StartRecordingCommand = new MvxCommand(() =>
            {
                if (CanExecuteStartRecording())
                {
                    StartRecord();
                }
            });

            LoadAvailableCaptureDevices();
        }

        private string Partial
        {
            get => _partial;
            set
            {
                _partial = value;
                RaisePropertyChanged(() => Transcription);
            }
        }

        public string Transcription
        {
            get
            {
                if (String.IsNullOrEmpty(Partial))
                {
                    return _transcription;
                }
                else
                {
                    return _transcription + " " + Partial;
                }
            }

            set
            {
                _transcription += " " + value;
                Partial = string.Empty;
                RaisePropertyChanged(() => Transcription);
            }
        }

        private void ProcessRawData(object sender, string rawStr)
        {
            try
            {
                JObject respond = JObject.Parse(rawStr);
                if (respond.ContainsKey("partial"))
                {
                    Partial = respond["partial"].ToString();
                    RaisePropertyChanged(() => Transcription);
                }
                if (respond.ContainsKey("text"))
                {
                    Transcription = respond["text"].ToString();
                    RaisePropertyChanged(() => Transcription);
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
            get => recordLevel;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (recordLevel != value)
                {
                    SetProperty(ref recordLevel, value);
                    if (_audioCapture != null)
                    {
                        SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                    }
                }
            }
        }

        // ========Capture audio part==========


        public bool EnableStartRecord
        {
            get => _enableStartRecord;
            set => SetProperty(ref _enableStartRecord, value);
        }


        public bool EnableStopRecord
        {
            get => _stopRecordStopRecord;
            set
            {
                SetProperty(ref _stopRecordStopRecord, value);
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

        private void LoadAvailableCaptureDevices()
        {

            var enumerator = new MMDeviceEnumerator();
            CaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToArray();


            //AvailableRecordDevices = new ObservableCollection<MMDevice>(enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active));

            if (CaptureDevices.Count() != 0)
            {
                SelectedDevice = CaptureDevices.First();
                EnableStartRecord = true;
            }

        }




        private void StartRecord()
        {
            if (SelectedDevice != null)
            {
                try
                {
                    _audioCapture = SelectedDevice.DataFlow == DataFlow.Capture ?
                        new WasapiCapture() : new WasapiLoopbackCapture();

                    //RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                    /*                    capture.StartRecording();*/

                    _audioCapture.RecordingStopped += OnRecordingStopped;
                    _audioCapture.DataAvailable += OnDataAvailable;

                    /* Message = "Recording...";*/
                    _bufferedWaveProvider = new BufferedWaveProvider(_audioCapture.WaveFormat);


                    _resampler = new MediaFoundationResampler(_bufferedWaveProvider, new WaveFormat(8000, 16, 1));
                    _resampler.ResamplerQuality = 60;

                    EnableStartRecord = false;
                    EnableStopRecord = true;
                    _audioCapture.StartRecording();
                }
                catch (Exception e)
                {
                    /*MessageBox.Show(e.Message);*/
                }
            }
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

            EnableStopRecord = false;
            EnableStartRecord = true;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {

            _bufferedWaveProvider.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            byte[] buffer = new byte[1000];
            _resampler.Read(buffer, 0, buffer.Length);

            _bufferQueue.Enqueue(buffer);
            Task.Run(WriteData);
            //UpdatePeakMeter();
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

                DebugInfo = _bufferQueue.Count.ToString();

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
            DebugInfo = _bufferQueue.Count.ToString();
            StreamingIsBusy = false;
        }

        public IEnumerable<MMDevice> CaptureDevices { get; set; }

        private void StopRecording()
        {
            _audioCapture?.StopRecording();
        }


        public ObservableCollection<MMDevice> AvailableRecordDevices
        {
            get => _deviceNames;
            set => SetProperty(ref _deviceNames, value);
        }

        public MMDevice SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                SetProperty(ref _selectedDevice, value);
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
        private bool CanExecuteStartRecording() => SelectedDevice != null;
    }
}
