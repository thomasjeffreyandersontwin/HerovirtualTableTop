using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Commands;
using System.Timers;
namespace Module.HeroVirtualTabletop.Library.Utility
{
    public class AsyncDelegateCommand<T> : DelegateCommand<T>
    {
        private Timer AsynchronousTimer = new Timer();
        public AsyncDelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod) : base(executeMethod, canExecuteMethod)
        {
            AsynchronousTimer.AutoReset = false;
            AsynchronousTimer.Interval = 50;
            AsynchronousTimer.Elapsed += ExecuteOnTimerElapsed;

        }

        public override Task Execute(T parameter)
        {
            AsynchronousTimer.Start();
            return null;
        }

        public void ExecuteOnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            AsynchronousTimer.Stop();
            if (CanExecute(null))
            {
                Action d = delegate ()
                {
                    base.Execute(null);
                };
                System.Windows.Application.Current.Dispatcher.BeginInvoke(d);
            } 
        }
    }
}






