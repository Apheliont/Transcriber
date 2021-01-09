using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transcriber.Core.Services
{
    public class TranscribeService : ITranscribeService
    {
        private readonly ITransportService _transportService;
        public event EventHandler<string> NewTranscriptionData;
        public TranscribeService(ITransportService transportService)
        {
            _transportService = transportService;
            _transportService.NewDataRecieved += OnNewData;

        }

        private void OnNewData(object sender, string data)
        {
            NewTranscriptionData?.Invoke(this, data);
        }

        public async Task TranscribeFile(string filePath)
        {
            FileStream fsSource = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            byte[] data = new byte[8000];
            while (true)
            {
                int count = fsSource.Read(data, 0, 8000);
                if (count == 0)
                    break;
                await _transportService.SendData(data, count);
            }
            await _transportService.SendFinalData();
        }

        public async Task TranscribeChunk(byte[] data, int count)
        {
            await _transportService.SendData(data, count);
        }


    }

}
