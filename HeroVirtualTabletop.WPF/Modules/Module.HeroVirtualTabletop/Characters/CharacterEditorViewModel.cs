using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.OptionGroups;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private HashedObservableCollection<ICrowdMemberModel, string> characterCollection;

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
                OnPropertyChanged("EditedCharacter");
            }
        }

        public OptionGroupViewModel<Identity> IdentityViewModel
        {
            get
            {
                return identitiesViewModel;
            }
            set
            {
                identitiesViewModel = value;
                OnPropertyChanged("IdentityViewModel");
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
            this.eventAggregator.GetEvent<EditCharacterEvent>().Subscribe(this.LoadCharacter);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(this.UnLoadCharacter);
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            
        }

        private void LoadCharacter(object state)
        {
            Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>> tuple = state as Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>;
            if (tuple != null)
            {
                Character character = tuple.Item1 as Character;
                HashedObservableCollection<ICrowdMemberModel, string> collection = new HashedObservableCollection<ICrowdMemberModel, string>(tuple.Item2, x => x.Name);
                if(character != null && collection != null)
                {
                    this.IdentityViewModel = this.Container.Resolve<OptionGroupViewModel<Identity>>(
                        new ParameterOverride("optionGroup", character.AvailableIdentities),
                        new ParameterOverride("owner", character)
                        );
                    this.EditedCharacter = character;
                    this.characterCollection = collection;
                }
            }
        }

        private void UnLoadCharacter(object state)
        {
            ICrowdMemberModel model = state as ICrowdMemberModel;
            //if (model != null && this.EditedCharacter.Name == model.Name)
            //    this.EditedCharacter = null;
        }

        #endregion

        #region Methods

        #endregion
    }
}
