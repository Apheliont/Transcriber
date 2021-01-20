using MvvmCross;
using MvvmCross.ViewModels;
using Transcriber.Core.Services;
using Transcriber.Core.Services.Implementations;
using Transcriber.Core.Services.Interfaces;
using Transcriber.Core.ViewModels;

namespace Transcriber.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            Mvx.IoCProvider.RegisterType<IFileProcessService, FileProcessService>();
            Mvx.IoCProvider.RegisterType<IRecordService, RecordService>();
            Mvx.IoCProvider.RegisterType<IServerResponseService, ServerResponseService>();
            Mvx.IoCProvider.RegisterType<IConfigurationService, ConfigurationService>();
            Mvx.IoCProvider.RegisterSingleton<ITransportService>(new TransportWebSockets());
            RegisterAppStart<RootViewModel>();
        }
    }
}
