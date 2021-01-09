using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public interface ITranscribeService
    {
        Task TranscribeFile(string filePath);
        Task TranscribeChunk(byte[] data, int count);
        event EventHandler<string> NewTranscriptionData;
    }
}
