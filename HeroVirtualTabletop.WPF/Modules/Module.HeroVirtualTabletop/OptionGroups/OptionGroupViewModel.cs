using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    public class OptionGroupViewModel<T> : BaseViewModel where T : ICharacterOption
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private OptionGroup<T> optionGroup;
        #endregion

        #region Events

        #endregion

        #region Public Properties

        public OptionGroup<T> OptionGroup
        {
            get
            {
                return optionGroup;
            }
            private set
            {
                optionGroup = value;
                OnPropertyChanged("OptionGroup");
            }
        }

        private T selectedOption;
        public T SelectedOption
        {
            get
            {
                return GetSelectedOption();
            }
            set
            {
                SetSelectedOption(value);
                OnPropertyChanged("SelectedOption");
            }
        }

        public T DefaultOption
        {
            get
            {
                return GetDefaultOption();
            }
            set
            {
                SetDefaultOption(value);
                OnPropertyChanged("DefaultOption");
            }
        }

        public T ActiveOption
        {
            get
            {
                return GetActiveOption();
            }
            set
            {
                SetActiveOption(value);
                OnPropertyChanged("ActiveOption");
            }
        }

        private Character owner;
        public Character Owner
        {
            get { return owner; }
            private set { owner = value; }
        }


        #endregion

        #region Commands

        public DelegateCommand<object> AddOptionCommand { get; private set; }
        public DelegateCommand<object> RemoveOptionCommand { get; private set; }

        public DelegateCommand<object> SetDefaultOptionCommand { get; private set; }

        public DelegateCommand<object> EditOptionCommand { get; private set; }

        public ICommand SetActiveOptionCommand { get; private set; }
        
        #endregion

        #region Constructor

        public OptionGroupViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator, OptionGroup<T> optionGroup, Character owner)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.Owner = owner;
            this.Owner.PropertyChanged += Owner_PropertyChanged;
            this.OptionGroup = optionGroup;
            InitializeCommands();
        }

        private void Owner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (typeof(T) == typeof(Identity))
            {
                switch (e.PropertyName)
                {
                    case "ActiveIdentity":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultIdentity":
                        OnPropertyChanged("DefaultOption");
                        break;
                }
            }
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddOptionCommand = new DelegateCommand<object>(this.AddOption);
            this.RemoveOptionCommand = new DelegateCommand<object>(this.RemoveOption);

            this.SetDefaultOptionCommand = new DelegateCommand<object>(this.SetDefaultOption);
            this.EditOptionCommand = new DelegateCommand<object>(this.EditOption);
        }

        #endregion

        #region Methods

        private void AddOption(object state)
        {
            if (typeof(T) == typeof(Identity))
            {
                AddIdentity(state);
                return;
            }
            if (typeof(T) == typeof(AnimatedAbility))
            {
                AddAbility(state);
                return;
            }
        }
        
        private void RemoveOption(object state)
        {
            if (typeof(T) == typeof(Identities.Identity))
            {
                RemoveIdentity(state);
                return;
            }
            else
            {
                optionGroup.Remove(SelectedOption);
            }
        }

        private T GetDefaultOption()
        {
            if (typeof(T) == typeof(Identity))
            {
                return (T)Convert.ChangeType(owner.DefaultIdentity, typeof(T));
            }

            return default(T);
        }

        private void SetDefaultOption(T value)
        {
            if (typeof(T) == typeof(Identity))
            {
                owner.DefaultIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
        }

        private void SetDefaultOption(object state)
        {
            DefaultOption = SelectedOption;
        }

        private T GetActiveOption()
        {
            if (typeof(T) == typeof(Identity))
            {
                return (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }

            return default(T);
        }

        private void SetActiveOption(T value)
        {
            if (typeof(T) == typeof(Identity))
            {
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
        }

        private T GetSelectedOption()
        {
            if (typeof(T) == typeof(Identity))
            {
                return (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }
            else
            {
                return selectedOption;
            }
        }

        private void SetSelectedOption(T value)
        {
            if (typeof(T) == typeof(Identity))
            {
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else
            {
                selectedOption = value;
            }
        }

        private void AddIdentity(object state)
        {
            (optionGroup as OptionGroup<Identity>).Add(GetNewIdentity());
        }

        private void RemoveIdentity(object state)
        {
            if (SelectedOption == null)
                return;
            if (OptionGroup.Count == 1)
            {
                MessageBox.Show("Every character must have at least 1 Identity");
                return;
            }
            optionGroup.Remove(SelectedOption);
        }

        private Identity GetNewIdentity()
        {
            return new Identity("Model_Statesman", IdentityType.Model, optionGroup.NewValidOptionName("Identity"));
        }
        
        private void AddAbility(object state)
        {
            (optionGroup as OptionGroup<AnimatedAbility>).Add(GetNewAbility());
        }

        private AnimatedAbility GetNewAbility()
        {
            return new AnimatedAbility(optionGroup.NewValidOptionName("Ability"), owner: this.Owner);
        }

        private void EditOption(object obj)
        {
            if (typeof(T) == typeof(Identity))
            {
                Identity identity = (Identity)Convert.ChangeType(SelectedOption, typeof(Identity));
                eventAggregator.GetEvent<EditIdentityEvent>().Publish(new Tuple<Identity, Character>(identity, Owner));
            }
        }

        #endregion
    }
}
