using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public interface ITranscribeService
    {
        Task<string> TranscribeFile(string filePath);
    }
}
