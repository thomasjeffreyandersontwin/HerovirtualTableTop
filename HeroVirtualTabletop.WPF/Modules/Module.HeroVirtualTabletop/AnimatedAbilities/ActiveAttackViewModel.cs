using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.Shared.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class ActiveAttackViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;

        #endregion

        #region Public Properties

        private Character character;
        private Attack attack;

        private ActiveAttackConfiguration selectedActiveAttackConfig;
        public ActiveAttackConfiguration SelectedActiveAttackConfiguration
        {
            get
            {
                return selectedActiveAttackConfig;
            }
            set
            {
                selectedActiveAttackConfig = value;
                OnPropertyChanged("SelectedActiveAttackConfiguration");
            }
        }

        private bool isStunnedSelected;
        public bool IsStunnedSelected
        {
            get
            {
                return isStunnedSelected;
            }
            set
            {
                isStunnedSelected = value;
                OnPropertyChanged("IsStunnedSelected");
            }
        }

        private bool isUnconciousSelected;
        public bool IsUnconciousSelected
        {
            get
            {
                return isUnconciousSelected;
            }
            set
            {
                isUnconciousSelected = value;
                OnPropertyChanged("IsUnconciousSelected");
            }
        }

        private bool isDyingSelected;
        public bool IsDyingSelected
        {
            get
            {
                return isDyingSelected;
            }
            set
            {
                isDyingSelected = value;
                OnPropertyChanged("IsDyingSelected");
            }
        }

        private bool isDeadSelected;
        public bool IsDeadSelected
        {
            get
            {
                return isDeadSelected;
            }
            set
            {
                isDeadSelected = value;
                OnPropertyChanged("IsDeadSelected");
            }
        }
        
        #endregion

        #region Commands

        
        public DelegateCommand<object> SetActiveAttackCommand { get; private set; }
        public DelegateCommand<object> CancelActiveAttackCommand { get; private set; }

        #endregion

        #region Constructor

        public ActiveAttackViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            this.eventAggregator.GetEvent<ConfigureActiveAttackEvent>().Subscribe(this.ConfigureActiveAttack);
        }
        
        #endregion

        #region Initialization

        private void InitializeAnimationElementSelections()
        {
            
        }

        private void InitializeCommands()
        {
            this.SetActiveAttackCommand = new DelegateCommand<object>(this.SetActiveAttack);
            this.CancelActiveAttackCommand = new DelegateCommand<object>(this.CancelActiveAttack);
        }

        #endregion

        #region Methods

        private void ConfigureActiveAttack(Tuple<Character, Attack> tuple)
        {
            this.character = tuple.Item1;
            this.attack = tuple.Item2;
            this.SelectedActiveAttackConfiguration = new ActiveAttackConfiguration();
            SelectedActiveAttackConfiguration.AttackMode = AttackMode.Defend;
            SelectedActiveAttackConfiguration.AttackResult = AttackResultOption.Hit;
            SelectedActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.None;
            SelectedActiveAttackConfiguration.KnockBackOption = KnockBackOption.KnockDown;
        }

        private void SetActiveAttack(object state)
        {
            if (this.IsDeadSelected)
                this.SelectedActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Dead;
            else if (this.IsDyingSelected)
                this.SelectedActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Dying;
            else if (this.IsUnconciousSelected)
                this.SelectedActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Unconcious;
            else
                this.SelectedActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Stunned;
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Publish(new Tuple<Character, ActiveAttackConfiguration, Attack>(this.character, this.SelectedActiveAttackConfiguration, this.attack));
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(null);
        }

        private void CancelActiveAttack(object state)
        {
            // Change mouse pointer to back to bulls eye
            Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            Mouse.OverrideCursor = cursor;
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(null);
        }

        #endregion
    }
}
