using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Events;
using Module.Shared;
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
        private CrowdModel noCrowdCrowd = new CrowdModel(Constants.NO_CROWD_CROWD_NAME);

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
            ICrowdMemberModel crowdMember = crowdMembership.Item1;
            CrowdModel crowd = crowdMembership.Item2;

            if (crowd == null || crowd.Name == Constants.ALL_CHARACTER_CROWD_NAME)
            {
                crowd = noCrowdCrowd;
            }

            if (crowdMember is CrowdModel)
            {
                CrowdModel crowdMemberAsCrowd = crowdMember as CrowdModel;
                foreach (ICrowdMemberModel x in (crowdMemberAsCrowd.CrowdMemberCollection))
                {
                    AddPartecipant(new Tuple<ICrowdMemberModel, CrowdModel>(x, crowdMemberAsCrowd));
                }
            }
            else
            {
                if (Partecipants.Contains(crowdMember))
                {
                    CrowdMemberModel clone = crowdMember.Clone() as CrowdMemberModel;
                    string suffix = string.Empty;
                    int i = 0;
                    while (crowdMembership.Item2.CrowdMemberCollection.Any(x => x.Name == clone.Name + suffix) 
                        || Partecipants.Any(x => x.Name == clone.Name + suffix))
                    {
                        i++;
                        suffix = string.Format(" ({0})", i);
                    }
                    clone.Name += suffix;
                    crowdMembership.Item2.CrowdMemberCollection.Add(clone);
                    AddPartecipant(new Tuple<ICrowdMemberModel, CrowdModel>(clone, crowd));
                }
                else
                {
                    crowdMember.RosterCrowd = crowd;
                    Partecipants.Add(crowdMember);
                }
            }
        }

        #endregion
    }
}
