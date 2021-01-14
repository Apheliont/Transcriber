using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;


namespace Transcriber.Wpf.Views
{
    [MvxContentPresentation(WindowIdentifier = nameof(RootView), StackNavigation = false)]
    public partial class TextEditorView : MvxWpfView
    {
        public TextEditorView()
        {
            InitializeComponent();
        }
    }
}
