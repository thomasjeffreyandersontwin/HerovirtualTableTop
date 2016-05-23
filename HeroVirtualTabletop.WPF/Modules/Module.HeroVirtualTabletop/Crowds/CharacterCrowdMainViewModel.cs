using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Roster;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterCrowdMainViewModel : BaseViewModel
    {
        #region Private Members
        private EventAggregator eventAggregator;

        #endregion

        #region Events
        public event EventHandler ViewLoaded;
        public void OnViewLoaded(object sender, EventArgs e)
        {
            if (ViewLoaded != null)
                ViewLoaded(sender, e);
        }
        #endregion

        #region Public Properties

        private bool isCharacterExplorerExpanded;
        public bool IsCharacterExplorerExpanded
        {
            get
            {
                return isCharacterExplorerExpanded;
            }
            set
            {
                isCharacterExplorerExpanded = value;
                OnPropertyChanged("IsCharacterExplorerExpanded");
            }
        }

        private bool isRosterExplorerExpanded;
        public bool IsRosterExplorerExpanded
        {
            get
            {
                return isRosterExplorerExpanded;
            }
            set
            {
                isRosterExplorerExpanded = value;
                OnPropertyChanged("IsRosterExplorerExpanded");
            }
        }

        private bool isCharacterEditorExpanded;
        public bool IsCharacterEditorExpanded
        {
            get
            {
                return isCharacterEditorExpanded;
            }
            set
            {
                isCharacterEditorExpanded = value;
                OnPropertyChanged("IsCharacterEditorExpanded");
            }
        }

        #endregion
        #region Commands

        public DelegateCommand<object> CollapsePanelCommand { get; private set; }

        #endregion

        #region Constructor
        public CharacterCrowdMainViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator) 
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe((IEnumerable<CrowdMemberModel> models) => { this.IsRosterExplorerExpanded = true; });
            this.eventAggregator.GetEvent<EditCharacterEvent>().Subscribe((Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>> tuple) => { this.IsCharacterEditorExpanded = true; });
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CollapsePanelCommand = new DelegateCommand<object>(this.CollapsePanel);
        }

        #endregion

        #region Methods
        public void LoadCharacterExplorer()
        {
            CharacterExplorerView view = this.Container.Resolve<CharacterExplorerView>();
            OnViewLoaded(view, null);
        }
        public void LoadRosterExplorer()
        {
            RosterExplorerView view = this.Container.Resolve<RosterExplorerView>();
            OnViewLoaded(view, null);
        }
        public void LoadCharacterEditor()
        {
            CharacterEditorView view = this.Container.Resolve<CharacterEditorView>();
            OnViewLoaded(view, null);
        }

        private void CollapsePanel(object state)
        {
            switch(state.ToString())
            {
                case "CharacterExplorer":
                    this.IsCharacterExplorerExpanded = false;
                    break;
                case "RosterExplorer":
                    this.IsRosterExplorerExpanded = false;
                    break;
                case "CharacterEditor":
                    this.IsCharacterEditorExpanded = false;
                    break;
            }
        }
        #endregion
    }
}
