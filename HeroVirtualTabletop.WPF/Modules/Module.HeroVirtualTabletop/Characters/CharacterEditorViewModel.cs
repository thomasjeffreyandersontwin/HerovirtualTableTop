using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Characters
{
    public class CharacterEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private Character editedCharacter;
        private OptionGroupViewModel<Identity> identitiesViewModel;

        #endregion

        #region Events

        #endregion

        #region Public Properties
        
        public Character EditedCharacter
        {
            get
            {
                return editedCharacter;
            }
            set
            {
                editedCharacter = value;
                this.identitiesViewModel = new OptionGroupViewModel<Identity>(BusyService, Container, eventAggregator, editedCharacter.AvailableIdentities, editedCharacter);
                OnPropertyChanged("EditedCharacter");
            }
        }

        public OptionGroupViewModel<Identity> IdentitiesViewModel
        {
            get
            {
                return identitiesViewModel;
            }
            set
            {
                identitiesViewModel = value;
                OnPropertyChanged("IdentitiesViewModel");
            }
        }

        #endregion

        #region Commands

        
        #endregion

        #region Constructor

        public CharacterEditorViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();

        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            
        }

        #endregion

        #region Methods

        #endregion
    }
}
