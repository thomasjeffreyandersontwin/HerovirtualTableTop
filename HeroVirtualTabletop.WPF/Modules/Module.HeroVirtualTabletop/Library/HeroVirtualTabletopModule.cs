using Framework.WPF.Library;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop
{
    public class HeroVirtualTabletopModule : BaseModule
    {
        #region Private Fields

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;

        private HeroVirtualTabletopModuleView view;
        private IRegion mainRegion;

        #endregion Private Fields

        #region Constructor

        public HeroVirtualTabletopModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.container = container;
            this.regionManager = regionManager;
        }

        #endregion Constructor

        #region BaseModule Members

        protected override object ModuleView
        {
            get { return view; }
        }

        protected override IRegion ModuleRegion
        {
            get { return mainRegion; }
        }

        public override void Initialize()
        {
            this.RegisterViewsAndRepositories();

            view = this.container.Resolve<HeroVirtualTabletopModuleView>();
            mainRegion = this.regionManager.Regions[RegionNames.Instance.MainRegion];
            mainRegion.Add(view);

            IPopupService popupService = this.container.Resolve<IPopupService>();
            popupService.Register("CharacterExplorerView", typeof(CharacterExplorerView));
            popupService.Register("RosterExplorerView", typeof(RosterExplorerView));

            this.regionManager.RegisterViewWithRegion(RegionNames.Instance.HeroVirtualTabletopRegion, typeof(HeroVirtualTabletopMainView));
        }

        protected void RegisterViewsAndRepositories()
        {
            this.container.RegisterType<HeroVirtualTabletopModuleView, HeroVirtualTabletopModuleView>();
            
            this.container.RegisterType<HeroVirtualTabletopMainView, HeroVirtualTabletopMainView>();
            this.container.RegisterType<HeroVirtualTabletopMainViewModel, HeroVirtualTabletopMainViewModel>();
            
            this.container.RegisterType<CharacterExplorerView, CharacterExplorerView>();
            this.container.RegisterType<CharacterExplorerViewModel, CharacterExplorerViewModel>();

            this.container.RegisterType<RosterExplorerView, RosterExplorerView>();
            this.container.RegisterType<RosterExplorerViewModel, RosterExplorerViewModel>();

            this.container.RegisterType<ICrowdRepository, CrowdRepository>(new ContainerControlledLifetimeManager());
        }

        #endregion BaseModule Members
    }
}
