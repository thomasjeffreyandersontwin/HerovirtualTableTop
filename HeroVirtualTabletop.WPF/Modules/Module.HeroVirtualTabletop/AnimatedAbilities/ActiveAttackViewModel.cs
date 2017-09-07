using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
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
        public DelegateCommand<string> ActivatePanelCommand { get; private set; }
        public DelegateCommand<string> DeactivatePanelCommand { get; private set; }

        #endregion

        #region Constructor

        public ActiveAttackViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            this.eventAggregator.GetEvent<ConfigureActiveAttackEvent>().Subscribe(this.ConfigureActiveAttack);
            this.eventAggregator.GetEvent<ConfirmAttackEvent>().Subscribe(this.SetActiveAttack);

            DesktopKeyEventHandler keyHandler = new DesktopKeyEventHandler(RetrieveEventFromKeyInput);
        }
        
        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.SetActiveAttackCommand = new DelegateCommand<object>(this.SetActiveAttack);
            this.CancelActiveAttackCommand = new DelegateCommand<object>(this.CancelActiveAttack);
            this.CenterTargetChangedCommand = new DelegateCommand<object>(this.ChangeCenterTarget);
            this.ActivatePanelCommand = new DelegateCommand<string>(this.ActivatePanel);
            this.DeactivatePanelCommand = new DelegateCommand<string>(this.DeactivatePanel);
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
            SetActiveAttack();
        }
        private void SetActiveAttack()
        {
            foreach (Character ch in this.DefendingCharacters)
            {
                SetAttackParameters(ch);
            }
            // Change mouse pointer to back to bulls eye
            Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            Mouse.OverrideCursor = cursor;

            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.ActiveAttack);
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Publish(new Tuple<List<Character>, Attack>(this.DefendingCharacters.ToList(), this.ActiveAttack));
        }
        private void SetAttackParameters(Character ch)
        {
            if (ch.ActiveAttackConfiguration.IsHit)
                ch.ActiveAttackConfiguration.AttackResult = AttackResultOption.Hit;
            else
                ch.ActiveAttackConfiguration.AttackResult = AttackResultOption.Miss;

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

            if (ch.ActiveAttackConfiguration.IsKnockedBack)
                ch.ActiveAttackConfiguration.KnockBackOption = KnockBackOption.KnockBack;
            else
                ch.ActiveAttackConfiguration.KnockBackOption = KnockBackOption.KnockDown;
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

        private void ActivatePanel(string panelName)
        {
            Helper.GlobalVariables_CurrentActiveWindowName = panelName;
        }

        private void DeactivatePanel(string panelName)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == panelName)
                Helper.GlobalVariables_CurrentActiveWindowName = "";
        }

        #region Desktop Key Handling
        public DesktopKeyEventHandler.EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.ACTIVE_ATTACK_WIDGET)
            {
                if (inputKey == Key.Enter)
                {
                    if (this.SetActiveAttackCommand.CanExecute(null))
                        this.SetActiveAttackCommand.Execute(null);
                }
                else if (inputKey == Key.Escape)
                {
                    if (this.CancelActiveAttackCommand.CanExecute(null))
                        this.CancelActiveAttackCommand.Execute(null);
                }
                else if (inputKey == Key.H || inputKey == Key.M || inputKey == Key.S || inputKey == Key.U
                    || inputKey == Key.Y || inputKey == Key.D || inputKey == Key.K || inputKey == Key.B
                    || (inputKey >= Key.D0 && inputKey <= Key.D9) || (inputKey >= Key.NumPad0 && inputKey <= Key.NumPad9))
                {
                    foreach (var defender in this.DefendingCharacters)
                    {
                        if (inputKey == Key.H)
                        {
                            defender.ActiveAttackConfiguration.AttackResult = AttackResultOption.Hit;
                        }
                        else if (inputKey == Key.M)
                        {
                            defender.ActiveAttackConfiguration.AttackResult = AttackResultOption.Miss;
                        }
                        else if (inputKey == Key.S)
                        {
                            defender.ActiveAttackConfiguration.IsStunned = true;
                        }
                        else if (inputKey == Key.U)
                        {
                            defender.ActiveAttackConfiguration.IsUnconcious = true;
                        }
                        else if (inputKey == Key.Y)
                        {
                            defender.ActiveAttackConfiguration.IsDying = true;
                        }
                        else if (inputKey == Key.D)
                        {
                            defender.ActiveAttackConfiguration.IsDead = true;
                        }
                        else if (inputKey == Key.B)
                        {
                            defender.ActiveAttackConfiguration.KnockBackOption = KnockBackOption.KnockBack;
                        }
                        else if (inputKey == Key.K)
                        {
                            defender.ActiveAttackConfiguration.KnockBackOption = KnockBackOption.KnockDown;
                        }
                        else if ((inputKey >= Key.D0 && inputKey <= Key.D9) || (inputKey >= Key.NumPad0 && inputKey <= Key.NumPad9))
                        {
                            var intkey = (inputKey >= Key.D0 && inputKey <= Key.D9) ? inputKey - Key.D0 : inputKey - Key.NumPad0;
                            if (defender.ActiveAttackConfiguration.KnockBackDistance > 0)
                            {
                                string current = defender.ActiveAttackConfiguration.KnockBackDistance.ToString();
                                current += intkey.ToString();
                                defender.ActiveAttackConfiguration.KnockBackDistance = Convert.ToInt32(current);
                            }
                            else
                            {
                                defender.ActiveAttackConfiguration.KnockBackDistance = intkey;
                            }
                        }
                    } 
                }
            }
            return null;
        }

        #endregion

        #endregion
    }
}
