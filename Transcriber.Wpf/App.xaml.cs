using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.Core;


namespace Transcriber.Wpf
{
    public partial class App : MvxApplication
    {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<MvxWpfSetup<Core.App>>();        
        }
    }
}
