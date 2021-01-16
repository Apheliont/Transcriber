using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public interface ITranscribeService
    {
        Task TranscribeFile(string filePath, CancellationToken cancellationToken);
        Task TranscribeChunk(byte[] data, int count);
        event EventHandler<string> NewTranscriptionData;
        event EventHandler<int> PercentageTranscribed;
    }
}
