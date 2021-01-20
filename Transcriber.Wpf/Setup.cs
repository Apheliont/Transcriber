using MvvmCross;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcriber.Core.Services;
using Transcriber.Core.ViewModels;
using Transcriber.Wpf.Util;
using Transcriber.Wpf.Views;

namespace Transcriber.Wpf
{
    public class Setup: MvxWpfSetup<Core.App>
    {
        protected override void InitializeLastChance()
        {
            base.InitializeLastChance();
        }

        protected override IMvxApplication CreateApp()
        {
            return new Core.App();
        }

    }
}
