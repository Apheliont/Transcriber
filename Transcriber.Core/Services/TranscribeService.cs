using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace Transcriber.Core.Services
{
    public class TranscribeService : ITranscribeService
    {
        private readonly ITransportService _transportService;
        public event EventHandler<string> NewTranscriptionData;
        public event EventHandler<int> PercentageTranscribed;
        public TranscribeService(ITransportService transportService)
        {
            _transportService = transportService;
            _transportService.NewDataRecieved += OnNewData;

        }

        private void OnNewData(object sender, string data)
        {
            NewTranscriptionData?.Invoke(this, data);
        }

        public async Task TranscribeFile(string filePath, CancellationToken cancellationToken)
        {
            long inputFileSize = new FileInfo(filePath).Length;
            long totalBytesRead = 0;

            var process = new Process();
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = $"-i \"{filePath}\" -ar 8000 -ac 1 -f s16le -";
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();



            var buffer = new byte[8000];
            int read;
            while ((read = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
            {
                totalBytesRead += read;
                PercentageTranscribed?.Invoke(this, (int)(((double)totalBytesRead / inputFileSize) * 100));
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
