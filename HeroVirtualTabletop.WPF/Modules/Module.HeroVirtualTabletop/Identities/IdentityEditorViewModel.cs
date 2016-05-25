using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using Prism.Events;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Identities
{
    public class IdentityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
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
                editedidentity.PropertyChanged += EditedIdentity_PropertyChanged;
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
                return EditedIdentity == Owner.DefaultIdentity;
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

        #endregion

        #region Commands
        #endregion

        #region Constructor

        public IdentityEditorViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            CreateModelsViewSource();
            CreateCostumesViewSource();
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            
        }

        #endregion

        #region Methods

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
