using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public interface IFileProcessService
    {
        Task TranscribeFile(string filePath);
        void Stop();
        event EventHandler<int> PercentageTranscribed;
        event EventHandler<string> InfoMessage;
    }
}
