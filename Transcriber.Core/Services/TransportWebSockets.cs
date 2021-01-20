using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Transcriber.Core.Services
{
    public class TransportWebSockets : ITransportService
    {
        private ClientWebSocket _ws;
        private Uri _address;
        public event EventHandler<string> NewDataRecieved;

        private async Task OpenConnection()
        {
            if (_ws?.State != WebSocketState.Open)
            {
                _ws = new ClientWebSocket();
                //_ws.Options.KeepAliveInterval = new TimeSpan(3_000_000); // 30 min
                await _ws.ConnectAsync(Address, CancellationToken.None);
            }
        }

        public Uri Address
        {
            get => _address;
            private set
            {
                _address = value;
            }
        }

        public void SetAddress(string addr)
        {
            Address = new Uri(addr);
            var disconnectTask = Task.Run(() => CloseConnection());
            disconnectTask.Wait();
            var connectTask = Task.Run(() => OpenConnection());
            connectTask.Wait();
        }

        public async Task CloseConnection()
        {
            if (_ws?.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
            }
        }

        public async Task SendData(byte[] data, int count)
        {
            await OpenConnection();
            await _ws.SendAsync(new ArraySegment<byte>(data, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
            await RecieveData();
        }

        public async Task SendFinalData()
        {
            await OpenConnection();
            byte[] eof = Encoding.UTF8.GetBytes("{\"eof\" : 1}");
            await _ws.SendAsync(new ArraySegment<byte>(eof), WebSocketMessageType.Text, true, CancellationToken.None);
            await RecieveData();
        }

        private async Task RecieveData()
        {
            byte[] result = new byte[8192];
            Task<WebSocketReceiveResult> receiveTask = _ws.ReceiveAsync(new ArraySegment<byte>(result), CancellationToken.None);
            await receiveTask;
            NewDataRecieved?.Invoke(this, Encoding.UTF8.GetString(result, 0, receiveTask.Result.Count));
        }
    }
}
