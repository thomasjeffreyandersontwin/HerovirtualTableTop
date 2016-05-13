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

        #endregion

        #region Events

        #endregion

        #region Public Properties

        public HashedObservableCollection<ICrowdMemberModel, string> Participants
        {
            get
            {
                return partecipants;
            }
            set
            {
                partecipants = value;
                OnPropertyChanged("Participants");
            }
        }

        public IList SelectedParticipants
        {
            get
            {
                return selectedPartecipants;
            }
            set
            {
                selectedPartecipants = value;
                OnPropertyChanged("SelectedParticipants");
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetedCommand { get; private set; }

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddParticipant);

            InitializeCommands();

        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(this.Spawn);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(this.ToggleTargeted);
        }
        
        #endregion

        #region Methods

        private void AddParticipant(IEnumerable<CrowdMemberModel> crowdMembers)
        {
            foreach (var crowdMember in crowdMembers)
            {
                Participants.Add(crowdMember);
            }
        }

        private void Spawn(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Spawn();
            }
        }

        private void ClearFromDesktop(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ClearFromDesktop();
                member.RosterCrowd = null;
            }
            var toBeRemoved = SelectedParticipants.Cast<ICrowdMemberModel>();
            foreach (CrowdMemberModel member in toBeRemoved)
            {
                Participants.Remove(member);
            }
        }


        private void ToggleTargeted(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ToggleTargeted();
            }
        }

        #endregion
    }
}
