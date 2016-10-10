<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Services.BusyService
{
    public interface IBusyService
    {
        void ShowBusy();
        void ShowBusy(string text);
        void ShowBusy(string[] windowNames);
        void HideBusy();
        void HideAllBusy();
        bool IsShowingBusy { get; }

    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Services.BusyService
{
    public interface IBusyService
    {
        void ShowBusy();
        void ShowBusy(string text);
        void ShowBusy(string[] windowNames);
        void HideBusy();
        void HideAllBusy();
        bool IsShowingBusy { get; }

    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
