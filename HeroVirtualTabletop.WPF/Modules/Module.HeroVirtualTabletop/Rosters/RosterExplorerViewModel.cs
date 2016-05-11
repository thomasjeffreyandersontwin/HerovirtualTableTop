using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Events;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Roster
{
    public class RosterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;
        private HashedObservableCollection<ICrowdMemberModel, string> partecipants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name);
        private IList selectedPartecipants;
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

        public IList SelectedPartecipants
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

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }

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
            this.SpawnCommand = new DelegateCommand<object>(this.Spawn);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop);
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
                    if (crowdMember.RosterCrowd == crowd)
                        return;
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

        private void Spawn(object state)
        {
            foreach (CrowdMemberModel member in SelectedPartecipants)
            {
                member.Spawn();
            }
        }

        private void ClearFromDesktop(object state)
        {
            foreach (CrowdMemberModel member in SelectedPartecipants)
            {
                member.ClearFromDesktop();
                member.RosterCrowd = null;
            }
            foreach (CrowdMemberModel member in SelectedPartecipants)
            {
                Partecipants.Remove(member);
            }
        }

        #endregion
    }
}
