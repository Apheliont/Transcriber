using MvvmCross;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Transcriber.Core.Util;

namespace Transcriber.Core.Services
{
    public class TranscribeService : ITranscribeService
    {
        private readonly ITransportService _transportService;
        public event EventHandler<string> NewTranscriptionData;
        public event EventHandler<int> PercentageTranscribed;
        public TranscribeService()
        {
            _transportService = Mvx.IoCProvider.Resolve<ITransportService>();
            _transportService.NewDataRecieved += OnNewData;

        }

        private void OnNewData(object sender, string data)
        {
            NewTranscriptionData?.Invoke(this, data);
        }


        public async Task TranscribeFile(string filePath, CancellationToken cancellationToken)
        {
            var process = new Process();
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = $"-i \"{filePath}\" -ar 8000 -ac 1 -f s16le -";
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();


            var task = Task.Run(() => {
                FFmpegPercentageParser ffmpegParser = new FFmpegPercentageParser();
                string line;
                int previousPercent;
                while ((line = process.StandardError.ReadLine()) != null && !cancellationToken.IsCancellationRequested)
                {
                    ffmpegParser.Parse(line);
                    if (ffmpegParser.GetPercentCompleted() is int currentPercent)
                    {
                        previousPercent = currentPercent;
                        PercentageTranscribed?.Invoke(this, currentPercent);
                    }
                }
            });

            var buffer = new byte[8000];
            int read;
            while ((read = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
            {
                await _transportService.SendData(buffer, read);
            }

            process.Kill();
        }

        public async Task TranscribeChunk(byte[] data, int count)
        {
            await _transportService.SendData(data, count);
        }


    }

}
