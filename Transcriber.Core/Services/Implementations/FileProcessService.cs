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
    public class FileProcessService : IFileProcessService
    {
        private readonly ITransportService _transportService;
        public event EventHandler<int> PercentageTranscribed;
        public event EventHandler<string> InfoMessage;
        private CancellationTokenSource _cancellationTokenSource = null;

        public FileProcessService()
        {
            _transportService = Mvx.IoCProvider.Resolve<ITransportService>();
        }


        public async Task TranscribeFile(string filePath)
        {
            InfoMessage.Invoke(this, "Обработка файла...");
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            var process = new Process();
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = $"-i \"{filePath}\" -ar 8000 -ac 1 -f s16le -";
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();


            var task = Task.Run(() =>
            {
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
            await _transportService.SendFinalData();
            await _transportService.CloseConnection();
            process.Kill();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            InfoMessage.Invoke(this, "Операция завершена");
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            InfoMessage?.Invoke(this, "Обработка файла отменена");
        }
    }

}
