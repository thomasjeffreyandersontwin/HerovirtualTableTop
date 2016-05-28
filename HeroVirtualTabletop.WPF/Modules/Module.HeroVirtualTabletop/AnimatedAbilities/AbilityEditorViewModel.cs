using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AbilityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;

        #endregion

        #region Events

        #endregion

        #region Public Properties
        private Character owner;
        public Character Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
                OnPropertyChanged("Owner");
            }
        }
        private AnimatedAbility editedAbility;
        public AnimatedAbility EditedAbility
        {
            get
            {
                return editedAbility;
            }
            set
            {
                //if (editedAbility != null)
                //{
                //    editedAbility.PropertyChanged -= EditedIdentity_PropertyChanged;
                //}
                editedAbility = value;
                //if (editedAbility != null)
                //{
                //    editedAbility.PropertyChanged += EditedIdentity_PropertyChanged;
                //}
                OnPropertyChanged("EditedAbility");
            }
        }
        private bool isShowingAblityEditor;
        public bool IsShowingAbilityEditor
        {
            get
            {
                return isShowingAblityEditor;
            }
            set
            {
                isShowingAblityEditor = value;
                OnPropertyChanged("IsShowingAbilityEditor");
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> CloseEditorCommand { get; private set; }

        #endregion

        #region Constructor

        public AbilityEditorViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe(this.LoadAnimatedAbility);
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadAbility);
        }

        #endregion

        #region Methods

        private void LoadAnimatedAbility(Tuple<AnimatedAbility, Character> tuple)
        {
            this.IsShowingAbilityEditor = true;
            this.EditedAbility = tuple.Item1 as AnimatedAbility;
            this.Owner = tuple.Item2 as Character;
        }
        private void UnloadAbility(object state = null)
        {
            this.EditedAbility = null;
            //this.Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            this.Owner = null;
            this.IsShowingAbilityEditor = false;
        }
        #endregion
    }
}
