using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Movements;
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

        private bool isIdentityEditorExpanded;
        public bool IsIdentityEditorExpanded
        {
            get
            {
                return isIdentityEditorExpanded;
            }
            set
            {
                isIdentityEditorExpanded = value;
                OnPropertyChanged("IsIdentityEditorExpanded");
            }
        }

        private bool isAbilityEditorExpanded;
        public bool IsAbilityEditorExpanded
        {
            get
            {
                return isAbilityEditorExpanded;
            }
            set
            {
                isAbilityEditorExpanded = value;
                OnPropertyChanged("IsAbilityEditorExpanded");
            }
        }

        private bool isMovementEditorExpanded;
        public bool IsMovementEditorExpanded
        {
            get
            {
                return isMovementEditorExpanded;
            }
            set
            {
                isMovementEditorExpanded = value;
                OnPropertyChanged("IsMovementEditorExpanded");
            }
        }

        private bool isCrowdFromModelsExpanded;
        public bool IsCrowdFromModelsExpanded
        {
            get
            {
                return isCrowdFromModelsExpanded;
            }
            set
            {
                isCrowdFromModelsExpanded = value;
                OnPropertyChanged("IsCrowdFromModelsExpanded");
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
            this.eventAggregator.GetEvent<EditIdentityEvent>().Subscribe((Tuple<Identity, Character> tuple) => { this.IsIdentityEditorExpanded = true; });
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe((Tuple<AnimatedAbility, Character> tuple) => { this.IsAbilityEditorExpanded = true; });
            this.eventAggregator.GetEvent<EditMovementEvent>().Subscribe((CharacterMovement cm) => { this.IsMovementEditorExpanded = true; });
            this.eventAggregator.GetEvent<CreateCrowdFromModelsEvent>().Subscribe((CrowdModel crowd) => { this.IsCrowdFromModelsExpanded = true; });
            //this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe((Tuple<Character, Attack> tuple) => { this.IsRosterExplorerExpanded = true; });
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
        public void LoadIdentityEditor()
        {
            IdentityEditorView view = this.Container.Resolve<IdentityEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadAbilityEditor()
        {
            AbilityEditorView view = this.Container.Resolve<AbilityEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadMovementEditor()
        {
            MovementEditorView view = this.Container.Resolve<MovementEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadCrowdFromModelsView()
        {
            CrowdFromModelsView view = this.Container.Resolve<CrowdFromModelsView>();
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
                case "IdentityEditor":
                    this.IsIdentityEditorExpanded = false;
                    break;
                case "AbilityEditor":
                    this.IsAbilityEditorExpanded = false;
                    break;
                case "MovementEditor":
                    this.IsMovementEditorExpanded = false;
                    break;
                case "CrowdFromModelsView":
                    this.IsCrowdFromModelsExpanded = false;
                    break;
            }
        }
        #endregion
    }
}
