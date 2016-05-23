using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                editedidentity = value;
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
                    "*.costume"));
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
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(strItem);
        }

        #endregion

    }
}
