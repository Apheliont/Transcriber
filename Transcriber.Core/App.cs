using MvvmCross.ViewModels;
using Transcriber.Core.ViewModels;

namespace Transcriber.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<RootViewModel>();
        }
    }
}
