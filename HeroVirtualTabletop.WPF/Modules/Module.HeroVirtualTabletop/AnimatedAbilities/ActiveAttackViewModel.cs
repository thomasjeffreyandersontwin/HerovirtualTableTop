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
using System.Collections.ObjectModel;
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

        private Attack activeAttack;
        public Attack ActiveAttack
        {
            get
            {
                return activeAttack;
            }
            set
            {
                activeAttack = value;
                OnPropertyChanged("ActiveAttack");
            }
        }

        //private bool isStunnedSelected;
        //public bool IsStunnedSelected
        //{
        //    get
        //    {
        //        return isStunnedSelected;
        //    }
        //    set
        //    {
        //        isStunnedSelected = value;
        //        OnPropertyChanged("IsStunnedSelected");
        //    }
        //}

        //private bool isUnconciousSelected;
        //public bool IsUnconciousSelected
        //{
        //    get
        //    {
        //        return isUnconciousSelected;
        //    }
        //    set
        //    {
        //        isUnconciousSelected = value;
        //        OnPropertyChanged("IsUnconciousSelected");
        //    }
        //}

        //private bool isDyingSelected;
        //public bool IsDyingSelected
        //{
        //    get
        //    {
        //        return isDyingSelected;
        //    }
        //    set
        //    {
        //        isDyingSelected = value;
        //        OnPropertyChanged("IsDyingSelected");
        //    }
        //}

        //private bool isDeadSelected;
        //public bool IsDeadSelected
        //{
        //    get
        //    {
        //        return isDeadSelected;
        //    }
        //    set
        //    {
        //        isDeadSelected = value;
        //        OnPropertyChanged("IsDeadSelected");
        //    }
        //}

        private ObservableCollection<Character> defendingCharacters;
        public ObservableCollection<Character> DefendingCharacters
        {
            get
            {
                return defendingCharacters;
            }
            set
            {
                defendingCharacters = value;
                OnPropertyChanged("DefendingCharacters");
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> CenterTargetChangedCommand { get; private set; }
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

        private void InitializeCommands()
        {
            this.SetActiveAttackCommand = new DelegateCommand<object>(this.SetActiveAttack);
            this.CancelActiveAttackCommand = new DelegateCommand<object>(this.CancelActiveAttack);
            this.CenterTargetChangedCommand = new DelegateCommand<object>(this.ChangeCenterTarget);
        }

        #endregion

        #region Methods

        private void ChangeCenterTarget(object state)
        {
            if(this.ActiveAttack.IsAreaEffect)
            {
                Character character = state as Character;
                if (character != null && character.ActiveAttackConfiguration.IsCenterTarget)
                {
                    foreach (Character ch in this.DefendingCharacters.Where(dc => dc.Name != character.Name))
                    {
                        ch.ActiveAttackConfiguration.IsCenterTarget = false;
                    }
                }
            }
        }

        private void ConfigureActiveAttack(Tuple<List<Character>, Attack> tuple)
        {
            this.DefendingCharacters = new ObservableCollection<Character>(tuple.Item1);
            this.ActiveAttack = tuple.Item2;
        }

        private void SetActiveAttack(object state)
        {
            // Change mouse pointer to back to bulls eye
            Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            Mouse.OverrideCursor = cursor;

            // If any character has Attack Effect not set for Hit, we set it to Stunned by default
            foreach(var character in this.DefendingCharacters)
            {
                if(character.ActiveAttackConfiguration.AttackResult == AttackResultOption.Hit)
                {
                    if (character.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.None)
                        character.ActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Stunned;
                }
            }

            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Publish(new Tuple<List<Character>, Attack>(this.DefendingCharacters.ToList(), this.ActiveAttack));
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
