using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public interface ITransportService
    {
        event EventHandler<string> NewDataRecieved;
        event EventHandler<string> InfoMessage;
        Task SendData(byte[] data, int count);
        Task SendFinalData();
        Task CloseConnection();
        void SetAddress(string addr);
    }
}
