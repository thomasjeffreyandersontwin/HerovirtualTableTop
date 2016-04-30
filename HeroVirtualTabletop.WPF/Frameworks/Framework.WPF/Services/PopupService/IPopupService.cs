using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Services.PopupService
{
    public interface IPopupService
    {
        void Register(string key, Type controlType);
        bool Unregister(string key);
        void ShowDialog(string key, BaseViewModel viewModel, bool isModal, Action<CancelEventArgs> winClosing = null, System.Windows.Media.SolidColorBrush background = null, System.Windows.Style customStyle = null);
        void ShowDialog(string key, BaseViewModel viewModel, string title, bool isModal, Action<CancelEventArgs> winClosing = null, System.Windows.Media.SolidColorBrush background = null, System.Windows.Style customStyle = null);
        void ShowDialog(string key, BaseViewModel viewModel, string title, Dictionary<string, object> ctrlPropertiesToSet,
            Dictionary<string, object> windowPropertiesToSet, bool isModal, Action<CancelEventArgs> winClosing = null, System.Windows.Media.SolidColorBrush background = null, System.Windows.Style customStyle = null);
        void CloseDialog(string key);
    }
}
