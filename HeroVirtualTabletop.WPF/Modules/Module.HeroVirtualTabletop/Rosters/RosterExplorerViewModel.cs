using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Roster
{
    public class RosterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;
        private HashedObservableCollection<ICrowdMemberModel, string> partecipants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name);
        private List<ICrowdMemberModel> selectedPartecipants;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        public HashedObservableCollection<ICrowdMemberModel, string> Partecipants
        {
            get
            {
                return partecipants;
            }
            set
            {
                partecipants = value;
                OnPropertyChanged("Partecipants");
            }
        }

        public List<ICrowdMemberModel> SelectedPartecipants
        {
            get
            {
                return selectedPartecipants;
            }
            set
            {
                selectedPartecipants = value;
                OnPropertyChanged("SelectedPartecipants");
            }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddPartecipant);

            InitializeCommands();

        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {

        }

        #endregion

        #region Methods

        private void AddPartecipant(Tuple<ICrowdMemberModel, CrowdModel> crowdMembership)
        {
            if (crowdMembership.Item1 is CrowdModel)
            {
                CrowdModel crowd = crowdMembership.Item1 as CrowdModel;
                foreach (ICrowdMemberModel x in (crowd.CrowdMemberCollection))
                {
                    AddPartecipant(new Tuple<ICrowdMemberModel, CrowdModel>(x, crowd));
                }
            }
            else
            {
                if (Partecipants.Contains(crowdMembership.Item1))
                {
                    //CrowdMemberModel clone = crowdMembership.Item1.Clone();
                    //crowdMembership.Item2.CrowdMemberCollection.Add(clone);
                    //AddPartecipant(new Tuple<ICrowdMemberModel, CrowdModel>(clone, crowdMembership.Item2));
                }
                else
                {
                    crowdMembership.Item1.RosterCrowd = crowdMembership.Item2;
                    Partecipants.Add(crowdMembership.Item1);
                }
            }
        }

        #endregion
    }
}
