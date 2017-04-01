using Caliburn.Micro;
using HeroVirtualTableTop.Common;

namespace Shell {
    //public class ShellViewModel : Caliburn.Micro.PropertyChangedBase, IShell { }

    public class ShellViewModelImpl : Conductor<object>, IShell
    {
        public ShellViewModelImpl()
        {
            ActivateItem(new HeroVirtualTableTop.Common.HeroVirtualTabletopMainViewModelImpl());
        }
    }
}