using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
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
    public class AutoFireAttackConfigurationViewModel : BaseViewModel
    {
        private EventAggregator eventAggregator;

        private Attack currentAttack;
        public Attack CurrentAttack
        {
            get
            {
                return currentAttack;
            }
            set
            {
                currentAttack = value;
                OnPropertyChanged("CurrentAttack");
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
                this.DistributeNumberOfShotsCommand.RaiseCanExecuteChanged();
            }
        }

        private Guid attackConfigKey = Guid.Empty;

        public DelegateCommand ConfirmAutoFireAttackCommand { get; private set; }
        public DelegateCommand<object> DistributeNumberOfShotsCommand { get; private set; }
        public AutoFireAttackConfigurationViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<ConfigureAutoFireAttackEvent>().Subscribe(this.LoadAttackTargets);
            this.ConfirmAutoFireAttackCommand = new DelegateCommand(ConfirmAutoFireAttack);
            this.DistributeNumberOfShotsCommand = new DelegateCommand<object>(this.DistributeNumberOfShots, CanDistributeNumberOfShots);
        }

        private void LoadAttackTargets(Tuple<Attack, List<Character>, Guid> tuple)
        {
            this.CurrentAttack = tuple.Item1;
            this.DefendingCharacters = new ObservableCollection<Character>(tuple.Item2);
            this.attackConfigKey = tuple.Item3;
            DistributeNumberOfShots((Character)null);
            Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Arrow;
            });
        }

        private void ConfirmAutoFireAttack()
        {
            this.eventAggregator.GetEvent<AutoFireAttackConfiguredEvent>().Publish(this.DefendingCharacters.ToList());
            Dispatcher.Invoke(() => {
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Mouse.OverrideCursor = cursor;
            });
        }

        private bool CanDistributeNumberOfShots(object state)
        {
            return this.DefendingCharacters.Count > 1;
        }
        private bool isUpdating = false;
        private void DistributeNumberOfShots(object state)
        {
            if (!isUpdating)
                DistributeNumberOfShots((Character)state);
        }
        private void DistributeNumberOfShots(Character lastUpdatedCharacter)
        {
            isUpdating = true;
            int maxNumberOfShots = this.CurrentAttack.AttackInfo.AutoFireMaxShots;
            int lastAssignment = 0;
            if (lastUpdatedCharacter != null)
                lastAssignment = lastUpdatedCharacter.AttackConfigurationMap[attackConfigKey].Item2.NumberOfShotsAssigned;
            int remainingNumberOfShots = maxNumberOfShots - lastAssignment;
            foreach (var dc in this.DefendingCharacters.Where(dc => dc != lastUpdatedCharacter))
                dc.AttackConfigurationMap[attackConfigKey].Item2.NumberOfShotsAssigned = 0;
            for(int i = 0; i < this.DefendingCharacters.Count; i++)
            {
                if (this.DefendingCharacters[i] != lastUpdatedCharacter)
                {
                    if (remainingNumberOfShots == 0)
                        break;
                    this.DefendingCharacters[i].AttackConfigurationMap[attackConfigKey].Item2.NumberOfShotsAssigned += 1;
                    remainingNumberOfShots--;
                }
                if (i == DefendingCharacters.Count - 1 && remainingNumberOfShots > 0)
                    i = -1;
            }
            isUpdating = false;
        }
    }
}
