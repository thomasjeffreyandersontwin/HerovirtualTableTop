<<<<<<< HEAD
﻿using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop;
using Module.Shared;
using Module.Shared.Enumerations;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApplication1;

namespace ApplicationShell.Models.Navigation
{
    public class NavigationManager
    {
        public static void Navigate(ModuleEnum module)
        {
            IUnityContainer container = App.appBootstrapper.Container;
            IModuleManager moduleManager = container.Resolve<IModuleManager>();

            if (module == ModuleEnum.HeroVirtualTabletop)
            {
                moduleManager.LoadModule(ModuleNames.HeroVirtualTabletop);
                HeroVirtualTabletopModule hvtModule = container.Resolve<HeroVirtualTabletopModule>();
                hvtModule.ActivateModule();
            }
        }
    }
}
=======
﻿using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop;
using Module.Shared;
using Module.Shared.Enumerations;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApplication1;

namespace ApplicationShell.Models.Navigation
{
    public class NavigationManager
    {
        public static void Navigate(ModuleEnum module)
        {
            IUnityContainer container = App.appBootstrapper.Container;
            IModuleManager moduleManager = container.Resolve<IModuleManager>();

            if (module == ModuleEnum.HeroVirtualTabletop)
            {
                moduleManager.LoadModule(ModuleNames.HeroVirtualTabletop);
                HeroVirtualTabletopModule hvtModule = container.Resolve<HeroVirtualTabletopModule>();
                hvtModule.ActivateModule();
            }
        }
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
