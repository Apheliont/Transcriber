using MvvmCross;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

using Transcriber.Core.Models;
using Transcriber.Core.Services.Interfaces;

namespace Transcriber.Core.Services.Implementations
{
    public class ServerResponseService : IServerResponseService
    {
        private readonly TranscriptionModel _transcriptionModel;
        private readonly ITransportService _transportService;

        public event EventHandler<string> HandledServerResponse;
        public event EventHandler<string> InfoMessage;
        public ServerResponseService()
        {
            _transcriptionModel = new TranscriptionModel();

            _transportService = Mvx.IoCProvider.Resolve<ITransportService>();
            _transportService.NewDataRecieved += ProcessServerResponse;
        }

        private void ProcessServerResponse(object sender, string rawStr)
        {
            try
            {
                JObject respond = JObject.Parse(rawStr);
                if (respond.ContainsKey("partial"))
                {
                    _transcriptionModel.Partial = respond["partial"].ToString();
                }
                if (respond.ContainsKey("text"))
                {
                    _transcriptionModel.Text = respond["text"].ToString();
                }
                HandledServerResponse?.Invoke(this, _transcriptionModel.Text);
            }
            catch (JsonReaderException e)
            {
                InfoMessage?.Invoke(this, e.Message);
            }

        }

        public void Clear()
        {
            _transcriptionModel.Clear();
            HandledServerResponse?.Invoke(this, _transcriptionModel.Text);
        }
    }
}
