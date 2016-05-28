using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Module.Shared;
using Prism.Events;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System;
using Microsoft.Practices.Prism.Commands;
using Module.HeroVirtualTabletop.Library.Utility;
using Framework.WPF.Services.MessageBoxService;
using Module.Shared.Messages;

namespace Module.HeroVirtualTabletop.Identities
{
    public class IdentityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;
        private Character owner;
        private Identity editedidentity;
        private Visibility visibility = Visibility.Collapsed;
        private string filter;
        private ObservableCollection<string> models;
        private ObservableCollection<string> costumes;
        private CollectionViewSource modelsCVS;
        private CollectionViewSource costumesCVS;

        #endregion

        #region Public Properties

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

        public Identity EditedIdentity
        {
            get
            {
                return editedidentity;
            }
            set
            {
                if (editedidentity != null)
                {
                    editedidentity.PropertyChanged -= EditedIdentity_PropertyChanged;
                }
                editedidentity = value;
                if (editedidentity != null)
                {
                    editedidentity.PropertyChanged += EditedIdentity_PropertyChanged;
                }
                OnPropertyChanged("EditedIdentity");
            }
        }
        
        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                visibility = value;
                OnPropertyChanged("Visibility");
            }
        }

        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                ModelsCVS.View.Refresh();
                CostumesCVS.View.Refresh();
                OnPropertyChanged("Filter");
            }
        }
        
        public ObservableCollection<string> Models
        {
            get
            {
                return models;
            }
            set
            {
                models = value;
                OnPropertyChanged("Models");
            }
        }

        public ObservableCollection<string> Costumes
        {
            get
            {
                return costumes;
            }
            set
            {
                costumes = value;
                OnPropertyChanged("Costumes");
            }
        }

        public CollectionViewSource ModelsCVS
        {
            get
            {
                return modelsCVS;
            }
        }

        public CollectionViewSource CostumesCVS
        {
            get
            {
                return costumesCVS;
            }
        }

        public bool IsDefault
        {
            get
            {
                return EditedIdentity != null && EditedIdentity == Owner.DefaultIdentity;
            }
            set
            {
                if (value == true)
                    Owner.DefaultIdentity = EditedIdentity;
                else if (value == false)
                    Owner.DefaultIdentity = null;
                OnPropertyChanged("IsDefault");
            }
        }

        public string OriginalName { get; set; }

        #endregion

        #region Events

        public event EventHandler EditModeEnter;
        public void OnEditModeEnter(object sender, EventArgs e)
        {
            if (EditModeEnter != null)
                EditModeEnter(sender, e);
        }

        public event EventHandler EditModeLeave;
        public void OnEditModeLeave(object sender, EventArgs e)
        {
            if (EditModeLeave != null)
                EditModeLeave(sender, e);
        }

        #endregion

        #region Commands

        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitIdentityRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }

        #endregion

        #region Constructor

        public IdentityEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            CreateModelsViewSource();
            CreateCostumesViewSource();
            eventAggregator.GetEvent<EditIdentityEvent>().Subscribe(this.LoadIdentity);
        }
        
        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadIdentity);
            this.SubmitIdentityRenameCommand = new DelegateCommand<object>(this.SubmitIdentityRename);
            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
        }
        
        #endregion

        #region Methods

        private void LoadIdentity(Tuple<Identity, Character> data)
        {
            Filter = null;
            this.EditedIdentity = data.Item1;
            this.Owner = data.Item2;
            this.Owner.AvailableIdentities.CollectionChanged += AvailableIdentities_CollectionChanged;
            this.Visibility = Visibility.Visible;
        }

        private void UnloadIdentity(object state = null)
        {
            this.EditedIdentity = null;
            this.Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            this.Owner = null;
            this.Visibility = Visibility.Collapsed;
        }

        private void EnterEditMode(object state)
        {
            this.OriginalName = EditedIdentity.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelEditMode(object state)
        {
            EditedIdentity.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitIdentityRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = this.Owner.AvailableIdentities.ContainsKey(updatedName);

                if (!duplicateName)
                {
                    RenameIdentity(updatedName);
                    OnEditModeLeave(state, null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Identity", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.CancelEditMode(state);
                }
            }
        }

        private void RenameIdentity(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            EditedIdentity.Name = updatedName;
            Owner.AvailableIdentities.UpdateKey(OriginalName, updatedName);
            OriginalName = null;
        }

        private void AvailableIdentities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove 
                && e.OldItems.Contains(this.EditedIdentity))
            {
                this.UnloadIdentity();
            }
        }

        private void CreateModelsViewSource()
        {
            models = new ObservableCollection<string>(File.ReadAllLines(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_MODELS_FILENAME)));
            modelsCVS = new CollectionViewSource();
            modelsCVS.Source = Models;
            modelsCVS.View.Filter += stringsCVS_Filter;
        }

        private void CreateCostumesViewSource()
        {
            costumes = new ObservableCollection<string>(
                Directory.EnumerateFiles
                    (Path.Combine(
                        Settings.Default.CityOfHeroesGameDirectory,
                        Constants.GAME_COSTUMES_FOLDERNAME),
                    "*.costume").Select((file) => { return Path.GetFileNameWithoutExtension(file); }));
            costumesCVS = new CollectionViewSource();
            costumesCVS.Source = Costumes;
            costumesCVS.View.Filter += stringsCVS_Filter;
        }

        private bool stringsCVS_Filter(object item)
        {
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }

            string strItem = item as string;
            if (EditedIdentity != null && EditedIdentity.Surface == strItem)
            {
                return true;
            }
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(strItem);
        }

        private void EditedIdentity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Surface")
            {
                if (Owner.HasBeenSpawned)
                {
                    if (Owner.ActiveIdentity == EditedIdentity)
                    {
                        Owner.ActiveIdentity.Render();
                    }
                }
            }
        }

        #endregion

    }
}
