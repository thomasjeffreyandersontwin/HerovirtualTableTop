using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Prism.Events;
using System.Collections.ObjectModel;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Microsoft.Practices.Prism.Commands;
using System.Windows.Input;
using System.Reflection;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AttackConfigurationViewModel : BaseViewModel
    {
        private EventAggregator eventAggregator;

        private ObservableCollection<ActiveAttackViewModel> attackConfigurations;
        public ObservableCollection<ActiveAttackViewModel> AttackConfigurations
        {
            get
            {
                return attackConfigurations;
            }
            private set
            {
                attackConfigurations = value;
                OnPropertyChanged("AttackConfigurations");
            }
        }

        public DelegateCommand<object> ConfirmAttacksCommand { get; private set; }
        public DelegateCommand CancelAttacksCommand { get; private set; }

        public AttackConfigurationViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator) : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<ConfigureAttacksEvent>().Subscribe(this.ConfigureAttacks);
            this.eventAggregator.GetEvent<ConfirmAttacksEvent>().Subscribe(this.ConfirmAttacks);
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            this.ConfirmAttacksCommand = new DelegateCommand<object>(this.ConfirmAttacks);
            this.CancelAttacksCommand = new DelegateCommand(this.CancelAttacks);
        }

        public void ConfigureAttacks(List<Tuple<Attack, List<Character>, Guid>> attacksWithDefenders)
        {
            if (this.AttackConfigurations != null && this.AttackConfigurations.Count > 0)
                foreach (var attackConfig in this.AttackConfigurations)
                    attackConfig.RemoveDesktopKeyEventHandlers();
            this.AttackConfigurations = new ObservableCollection<ActiveAttackViewModel>();
            foreach (var tuple in attacksWithDefenders)
            {
                Attack attack = tuple.Item1;
                List<Character> defenders = tuple.Item2;
                Guid attackConfigKey = tuple.Item3;
                var activeAttackConfig = this.Container.Resolve<ActiveAttackViewModel>();
                activeAttackConfig.ConfigureActiveAttack(new Tuple<List<Character>, Attack, Guid>(defenders, attack, attackConfigKey));
                this.AttackConfigurations.Add(activeAttackConfig);
            }
        }

        private void ConfirmAttacks(object state)
        {
            List<Tuple<Attack, List<Character>, Guid>> attacksWithDefenders = new List<Tuple<Attack, List<Character>, Guid>>();
            foreach (var activeAttackConfig in this.AttackConfigurations)
            {
                activeAttackConfig.SetActiveAttack();
                attacksWithDefenders.Add(new Tuple<Attack, List<Character>, Guid>(activeAttackConfig.ActiveAttack, activeAttackConfig.DefendingCharacters.ToList(), activeAttackConfig.AttackConfigKey));
            }

            // Change mouse pointer to back to bulls eye
            Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            Mouse.OverrideCursor = cursor;

            this.eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
            this.eventAggregator.GetEvent<LaunchAttacksEvent>().Publish(attacksWithDefenders);
        }

        private void CancelAttacks()
        {
            List<Tuple<Attack, List<Character>, Guid>> attacksWithDefenders = new List<Tuple<Attack, List<Character>, Guid>>();
            foreach (var activeAttackConfig in this.AttackConfigurations)
            {
                activeAttackConfig.SetActiveAttack();
                attacksWithDefenders.Add(new Tuple<Attack, List<Character>, Guid>(activeAttackConfig.ActiveAttack, activeAttackConfig.DefendingCharacters.ToList(), activeAttackConfig.AttackConfigKey));
            }
            this.eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
            this.eventAggregator.GetEvent<CancelAttacksEvent>().Publish(attacksWithDefenders);
        }
    }
}
