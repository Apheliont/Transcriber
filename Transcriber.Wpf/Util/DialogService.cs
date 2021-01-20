using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Transcriber.Core.Services;

namespace Transcriber.Wpf.Util
{
    public class DialogService : IDialogService
    {

        public void ShowToast(string message)
        {
            MessageBoxButton btn = MessageBoxButton.OKCancel;
            var status = MessageBox.Show(message, "Продолжить?", btn);
        }
    }
}
