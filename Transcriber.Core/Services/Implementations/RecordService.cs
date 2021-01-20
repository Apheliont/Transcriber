using MvvmCross;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public class RecordService : IRecordService
    {
        private Process ffmpegProcess;
        private WasapiCapture _audioCapture;
        private int _threadSafeBoolBackValue = 0;
        private readonly ITransportService _transportService;

        public event EventHandler<float> RecordLevel;
        public event EventHandler RecordStopped;
        public event EventHandler<string> InfoMessage;

        public RecordService()
        {
            _transportService = Mvx.IoCProvider.Resolve<ITransportService>();
        }

        public void StartRecord()
        {
            try
            {
                _audioCapture = new WasapiLoopbackCapture();
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

                InfoMessage?.Invoke(this, "Запись...");

                _audioCapture.StartRecording();
            }
            catch (Exception e)
            {
                InfoMessage?.Invoke(this, $"Ошибка: {e.Message}");
            }
        }


        private void OnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            var ffmpegIn = ffmpegProcess.StandardInput.BaseStream;
            RecordLevel?.Invoke(this, CalculateRecordLevel(waveInEventArgs));
            ffmpegIn.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            Task.Run(async () => await ProcessData());
        }

        private async Task ProcessData()
        {
            if (StreamingIsBusy) return;
            // Лочим под отдельный поток
            StreamingIsBusy = true;

            int read;
            var buffer = new byte[8000];
            while ((read = ffmpegProcess.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                await _transportService.SendData(buffer, read);
            }

            StreamingIsBusy = false;
        }


        private void OnRecordingStopped(object sender, StoppedEventArgs err)
        {
            if (err.Exception != null)
            {
                InfoMessage?.Invoke(this, $"Ошибка: {err.Exception.Message}");
            }

            ffmpegProcess?.StandardOutput.Close();
            ffmpegProcess?.StandardInput.Close();
            ffmpegProcess?.Kill();

            _audioCapture.RecordingStopped -= OnRecordingStopped;
            _audioCapture.DataAvailable -= OnDataAvailable;


            _audioCapture.Dispose();
            _audioCapture = null;
            _threadSafeBoolBackValue = 0;

            Task.Run(() => { _transportService.SendFinalData(); }).Wait();
            Task.Run(() => { _transportService.CloseConnection(); }).Wait();
            InfoMessage?.Invoke(this, "Запись остановлена");
            RecordLevel?.Invoke(this, 0.0F);
            RecordStopped?.Invoke(this, EventArgs.Empty);
        }

        public void StopRecording()
        {
            InfoMessage?.Invoke(this, "Запись останавливается...");
            _audioCapture?.StopRecording();
        }


        // Lock to process items in the queue one at time.
        private bool StreamingIsBusy
        {
            get => (Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 1) == 1);
            set
            {
                if (value) Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 0);
                else Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 0, 1);
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

    }
}
