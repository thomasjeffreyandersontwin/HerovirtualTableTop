using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private ICrowdRepository crowdRepository;
        private HashedObservableCollection<ICrowdMember, string> characterCollection;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        private HashedObservableCollection<CrowdModel, string> crowdCollection;
        public HashedObservableCollection<CrowdModel, string> CrowdCollection
        {
            get
            {
                return crowdCollection;
            }
            set
            {
                crowdCollection = value;
                OnPropertyChanged("CrowdCollection");
            }
        }

        public CrowdModel SelectedCrowdModel
        {
            get;
            set;
        }

        #endregion

        #region Commands

        public DelegateCommand<object> AddCrowdCommand { get; private set; }

        public DelegateCommand<object> AddCharacterCommand { get; private set; }

        public ICommand UpdateSelectedCrowdMemberCommand { get; private set; }

        #endregion

        #region Constructor

        public CharacterExplorerViewModel(IBusyService busyService, IUnityContainer container, ICrowdRepository crowdRepository, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.crowdRepository = crowdRepository;
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            LoadCrowdCollection();

        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddCrowdCommand = new DelegateCommand<object>(this.AddCrowd);
            this.AddCharacterCommand = new DelegateCommand<object>(this.AddCharacter);

            UpdateSelectedCrowdMemberCommand = new SimpleCommand
            {

                ExecuteDelegate = x =>
                    UpdateSelectedCrowdMember(x)

            };
        }

        #endregion

        #region Methods

        #region Load Crowd Collection
        private void LoadCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.GetCrowdCollection(this.LoadCrowdCollectionCallback);
        }

        private void LoadCrowdCollectionCallback(List<CrowdModel> crowdList)
        {
            this.CrowdCollection = new HashedObservableCollection<CrowdModel, string>(crowdList,
                (CrowdModel c) => { return c.Name; }
                );
            CrowdModel allCharactersModel = this.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
            if (allCharactersModel == null)
                allCharactersModel = new CrowdModel();
            this.characterCollection = new HashedObservableCollection<ICrowdMember, string>(allCharactersModel.CrowdMemberCollection,
                (ICrowdMember c) => { return c.Name; }
                );
            //this.BusyService.HideBusy();
        }

        #endregion

        #region Update Selected Crowd
        private void UpdateSelectedCrowdMember(object state)
        {
            Object selectedCrowdModel = Helper.GetCurrentSelectedCrowdInCrowdCollection(state);
            CrowdModel crowdModel = selectedCrowdModel as CrowdModel;
            this.SelectedCrowdModel = crowdModel;
            
        }
        #endregion

        #region Add Crowd
        private void AddCrowd(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel();
            // Add the crowd to List of Crowd Members as a new Crowd Member
            this.CrowdCollection.Add(crowdModel);
            // Also add the crowd under any currently selected crowd
            if(this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>();
                this.SelectedCrowdModel.CrowdMemberCollection.Add(crowdModel);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }
        private CrowdModel GetNewCrowdModel()
        {
            
            string name = "Crowd";
            string suffix = string.Empty;
            int i = 0;
            while (this.CrowdCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return new CrowdModel(name + suffix);
        }

        #endregion

        #region Add Character

        private void AddCharacter(object state)
        {
            // Create a new Character
            Character character = this.GetNewCharacter();
            // Create All Characters List if not already there
            CrowdModel crowdModelAllCharacters = this.CrowdCollection.Where(c => c.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            if (crowdModelAllCharacters == null || crowdModelAllCharacters.CrowdMemberCollection == null || crowdModelAllCharacters.CrowdMemberCollection.Count == 0)
            {
                crowdModelAllCharacters = new CrowdModel(Constants.ALL_CHARACTER_CROWD_NAME);
                this.CrowdCollection.Add(crowdModelAllCharacters);
                crowdModelAllCharacters.CrowdMemberCollection = new ObservableCollection<ICrowdMember>();
                this.characterCollection = new HashedObservableCollection<ICrowdMember, string>(crowdModelAllCharacters.CrowdMemberCollection,
                    (ICrowdMember c) => { return c.Name; });
            }
            // Add the Character under All Characters List
            crowdModelAllCharacters.CrowdMemberCollection.Add(character as CrowdMember);
            this.characterCollection.Add(character as CrowdMember);
            // Also add the character under any currently selected crowd
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>();
                this.SelectedCrowdModel.CrowdMemberCollection.Add(character as CrowdMember);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }

        private Character GetNewCharacter()
        {
            string name = "Character";
            string suffix = string.Empty;
            int i = 0;
            while (this.characterCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return new CrowdMember(name + suffix);
        }

        #endregion

        #region Save Crowd Collection

        private void SaveCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.SaveCrowdCollection(this.SaveCrowdCollectionCallback, this.CrowdCollection.ToList());
        }

        private void SaveCrowdCollectionCallback()
        {
            //this.BusyService.HideBusy();
        }

        #endregion

        #endregion
    }
}
