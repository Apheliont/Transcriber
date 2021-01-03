using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.Core;
using System.Configuration;
using System;

namespace Transcriber.Wpf
{
    public partial class App : MvxApplication
    {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<MvxWpfSetup<Core.App>>();

            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["TranscriberServerAddress"];
            Console.WriteLine(result);
        }
    }
}
