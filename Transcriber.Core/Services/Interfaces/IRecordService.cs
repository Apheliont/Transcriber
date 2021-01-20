using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public interface IRecordService
    {
        event EventHandler<float> RecordLevel;
        event EventHandler RecordStopped;
        event EventHandler<string> InfoMessage;
        void StartRecord();
        void StopRecording();
    }
}
