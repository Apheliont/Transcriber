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
        private string _transcribedText;

        public async Task<string> TranscribeFile(string filePath)
        {
            ClientWebSocket ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri("wss://api.alphacephei.com/asr/ru/"), CancellationToken.None);

            FileStream fsSource = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            byte[] data = new byte[8000];
            while (true)
            {
                int count = fsSource.Read(data, 0, 8000);
                if (count == 0)
                    break;
                await ProcessData(ws, data, count);
            }
            await ProcessFinalData(ws);

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
            return _transcribedText;
        }

        async Task ProcessData(ClientWebSocket ws, byte[] data, int count)
        {
            await ws.SendAsync(new ArraySegment<byte>(data, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
            await RecieveResult(ws);
        }

        async Task ProcessFinalData(ClientWebSocket ws)
        {
            byte[] eof = Encoding.UTF8.GetBytes("{\"eof\" : 1}");
            await ws.SendAsync(new ArraySegment<byte>(eof), WebSocketMessageType.Text, true, CancellationToken.None);
            await RecieveResult(ws);
        }

        async Task RecieveResult(ClientWebSocket ws)
        {
            byte[] result = new byte[4096];
            Task<WebSocketReceiveResult> receiveTask = ws.ReceiveAsync(new ArraySegment<byte>(result), CancellationToken.None);
            await receiveTask;
            var receivedString = Encoding.UTF8.GetString(result, 0, receiveTask.Result.Count);
            _transcribedText += receivedString;
        }

    }

}
