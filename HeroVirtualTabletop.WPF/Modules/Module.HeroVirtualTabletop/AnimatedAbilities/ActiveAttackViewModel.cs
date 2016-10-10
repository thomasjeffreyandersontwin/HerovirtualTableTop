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
            foreach(Character ch in this.DefendingCharacters)
            {
                SetAttackEffect(ch);
            }
            // Change mouse pointer to back to bulls eye
            Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            Mouse.OverrideCursor = cursor;

            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.ActiveAttack);
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Publish(new Tuple<List<Character>, Attack>(this.DefendingCharacters.ToList(), this.ActiveAttack));
        }
        private void SetAttackEffect(Character ch)
        {
            if (ch.ActiveAttackConfiguration.IsDead)
                ch.ActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Dead;
            else if (ch.ActiveAttackConfiguration.IsDying)
                ch.ActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Dying;
            else if (ch.ActiveAttackConfiguration.IsUnconcious)
                ch.ActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Unconcious;
            else if (ch.ActiveAttackConfiguration.IsStunned)
                ch.ActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.Stunned;
            else
                ch.ActiveAttackConfiguration.AttackEffectOption = AttackEffectOption.None;
        }
        private void CancelActiveAttack(object state)
        {
            foreach(var c in this.DefendingCharacters)
            {
                c.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.None, AttackEffectOption = AttackEffectOption.None };
            }
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.DefendingCharacters.ToList());
            //this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(null);
        }

        #endregion
    }
}
