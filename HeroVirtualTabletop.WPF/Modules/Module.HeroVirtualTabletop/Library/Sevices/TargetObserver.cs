using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Sevices
{
    public class TargetObserver : ITargetObserver
    {
        private BackgroundWorker bgWorker;

        public TargetObserver()
        {
            currentTarget = new MemoryInstance().TargetPointer;
            bgWorker = new BackgroundWorker();
            bgWorker.WorkerReportsProgress = false;
            bgWorker.DoWork += listenForTargetChanged;
            bgWorker.RunWorkerCompleted += restart;
            bgWorker.RunWorkerAsync();
        }

        private void restart(object sender, RunWorkerCompletedEventArgs e)
        {
            bgWorker.RunWorkerAsync();
        }

        private void listenForTargetChanged(object sender, DoWorkEventArgs e)
        {
            uint actualTarget = new MemoryInstance().TargetPointer;
            while (actualTarget == currentTarget)
            {
                Thread.Sleep(500);
                actualTarget = new MemoryInstance().TargetPointer;
            }
            currentTarget = actualTarget;
            OnTargetChanged(currentTarget, new EventArgs());
        }

        public event EventHandler TargetChanged;

        private void OnTargetChanged(object sender, EventArgs e)
        {
            if (TargetChanged != null)
                TargetChanged(sender, e);
        }

        private uint currentTarget;
        public uint CurrentTarget
        {
            get
            {
                return currentTarget;
            }
        }
    }
}
