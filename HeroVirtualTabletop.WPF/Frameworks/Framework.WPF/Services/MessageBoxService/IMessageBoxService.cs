<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Framework.WPF.Services.MessageBoxService
{
    public interface IMessageBoxService
    {
        MessageBoxResult ShowDialog(string message);
        MessageBoxResult ShowDialog(string message, string caption);
        MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button);
        MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button, MessageBoxImage image);
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Framework.WPF.Services.MessageBoxService
{
    public interface IMessageBoxService
    {
        MessageBoxResult ShowDialog(string message);
        MessageBoxResult ShowDialog(string message, string caption);
        MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button);
        MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button, MessageBoxImage image);
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
