using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
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
        private T selectedOption;
        private T defaultOption;
        private T activeOption;
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

        public T SelectedOption
        {
            get
            {
                return selectedOption;
            }
            set
            {
                selectedOption = value;
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
        public ICommand SetActiveOptionCommand { get; private set; }
        
        #endregion

        #region Constructor

        public OptionGroupViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator, OptionGroup<T> optionGroup, Character owner)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.Owner = owner;
            this.OptionGroup = optionGroup;
            InitializeCommands();
        }
        
        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddOptionCommand = new DelegateCommand<object>(this.AddOption);
            this.RemoveOptionCommand = new DelegateCommand<object>(this.RemoveOption);

            this.SetDefaultOptionCommand = new DelegateCommand<object>(this.SetDefaultOption);
        }

        #endregion

        #region Methods

        private void AddOption(object state)
        {
            if (typeof(T) == typeof(Identities.Identity))
            {
                AddIdentity(state);
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
        }

        private T GetDefaultOption()
        {
            if (typeof(T) == typeof(Identities.Identity))
            {
                return (T)Convert.ChangeType(owner.DefaultIdentity, typeof(T));
            }

            return default(T);
        }

        private void SetDefaultOption(T value)
        {
            if (typeof(T) == typeof(Identities.Identity))
            {
                owner.DefaultIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
        }

        private void SetDefaultOption(object state)
        {
            DefaultOption = (T)(state as FrameworkElement).DataContext;
        }

        private T GetActiveOption()
        {
            if (typeof(T) == typeof(Identities.Identity))
            {
                return (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }

            return default(T);
        }

        private void SetActiveOption(T value)
        {
            if (typeof(T) == typeof(Identities.Identity))
            {
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
        }

        private void AddIdentity(object state)
        {
            (optionGroup as OptionGroup<Identity>).Add(GetNewIdentity());
        }

        private void RemoveIdentity(object state)
        {
            if (selectedOption == null)
                return;
            if (OptionGroup.Count == 1)
            {
                System.Windows.MessageBox.Show("Every character must have at least 1 Identity");
                return;
            }
            optionGroup.Remove(selectedOption);
        }

        private Identity GetNewIdentity()
        {
            return new Identity("model_Statesman", IdentityType.Model, optionGroup.NewValidOptionName("Identity"));
        }

        #endregion
    }
}
