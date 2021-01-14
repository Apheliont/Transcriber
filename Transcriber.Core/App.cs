using MvvmCross;
using MvvmCross.ViewModels;
using Transcriber.Core.Services;
using Transcriber.Core.ViewModels;

namespace Transcriber.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            Mvx.IoCProvider.RegisterType<ITranscribeService, TranscribeService>();
            Mvx.IoCProvider.RegisterType<IConfigurationService, ConfigurationService>();
            Mvx.IoCProvider.RegisterType<ITransportService, TransportWebSockets>();
            RegisterAppStart<RootViewModel>();
        }
    }
}
