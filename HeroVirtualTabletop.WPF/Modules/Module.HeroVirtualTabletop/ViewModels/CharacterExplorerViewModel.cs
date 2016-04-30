using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.DomainModels;
using Module.HeroVirtualTabletop.Models;
using Module.HeroVirtualTabletop.Repositories;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.ViewModels
{
    public class CharacterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private ICrowdRepository crowdRepository;

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

        public BaseCrowdMember SelectedCrowdMember
        {
            get;
            set;
        }

        #endregion

        #region Commands

        public DelegateCommand<object> AddCrowdCommand { get; private set; }

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
            //this.BusyService.HideBusy();
        }
        #endregion

        #region Update Selected Crowd
        private void UpdateSelectedCrowdMember(object state)
        {
            BaseCrowdMember crowdMember = state as BaseCrowdMember;
            this.SelectedCrowdMember = crowdMember;
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
            if(this.SelectedCrowdMember is CrowdModel)
            {
                CrowdModel selectedCrowdModel = this.SelectedCrowdMember as CrowdModel;
                if (selectedCrowdModel.ChildCrowdCollection == null)
                    selectedCrowdModel.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>();
                selectedCrowdModel.ChildCrowdCollection.Add(crowdModel);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }
        private CrowdModel GetNewCrowdModel()
        {
            //if (string.IsNullOrEmpty(name))
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
