using System;
using System.Collections.Generic;
using System.Text;

namespace Transcriber.Core.Services.Interfaces
{
    public interface IServerResponseService
    {
        event EventHandler<string> HandledServerResponse;
        event EventHandler<string> InfoMessage;
        void Clear();
    }
}
