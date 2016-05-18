using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Utility;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Module.HeroVirtualTabletop.Library
{
    public class HeroVirtualTabletopMainViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        public IPopupService PopupService
        {
            get { return this.Container.Resolve<IPopupService>(); }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        public HeroVirtualTabletopMainViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator) 
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;

            // Just testing how things look at the moment...will change soon.
            //Load camera on start
            new Camera().Render();
            LoadCharacterExplorer();
            LoadCharacterEditor();
            LoadRosterExplorer();
        }

        #endregion

        #region Initialization

        #endregion

        #region Methods

        private void LoadCharacterExplorer()
        {
            System.Windows.Style style = Helper.GetCustomWindowStyle();
            CharacterExplorerViewModel characterExplorerViewModel = this.Container.Resolve<CharacterExplorerViewModel>();
            PopupService.ShowDialog("CharacterExplorerView", characterExplorerViewModel, "Character Explorer", false, null, new SolidColorBrush(Colors.Transparent), style);
        }

        private void LoadCharacterEditor()
        {
            System.Windows.Style style = Helper.GetCustomWindowStyle();
            Characters.CharacterEditorViewModel characterEditorViewModel = this.Container.Resolve<Characters.CharacterEditorViewModel>();
            characterEditorViewModel.EditedCharacter = new Characters.Character("Test Character");
            PopupService.ShowDialog("CharacterEditorView", characterEditorViewModel, "Character Editor", false, null, new SolidColorBrush(Colors.Transparent), style);
        }

        private void LoadRosterExplorer()
        {
            System.Windows.Style style = Helper.GetCustomWindowStyle();
            Roster.RosterExplorerViewModel RosterExplorerViewModel = this.Container.Resolve<Roster.RosterExplorerViewModel>();
            PopupService.ShowDialog("RosterExplorerView", RosterExplorerViewModel, "Roster Explorer", false, null, new SolidColorBrush(Colors.Transparent), style);
        }

        #endregion
    }
}
