using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Characters
{
    public class Character : NotifyPropertyChanged
    {
        public Character()
        {
            InitializeCharacter();
        }

        private void InitializeCharacter()
        {
            availableIdentities = new IdentitiesCollection();
        }

        public Character(string name): this()
        {
            Name = name;
            SetActiveIdentity();
        }

        public Character(string name, string surface, IdentityType identityType): this(name)
        {
            SetActiveIdentity(surface, identityType);
        }

        /// <summary>
        /// Create an Identity based on the parameters or the character name and set it as active.
        /// </summary>
        /// <param name="surface">The surface to be used. If null the method will look up for an existing costume with the same name of the character.</param>
        /// <param name="identityType">Can be either Model or Costume. In the second case the surface parameter will be validated checking if the costume exists.</param>
        private void SetActiveIdentity(string surface = null, IdentityType identityType = IdentityType.Model)
        {
            if (surface == null) //No surface passed
            {
                //We look for a costume with the same name of the character
                if (File.Exists(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME, name + Constants.GAME_COSTUMES_EXT)))
                {
                    //Costume exists, use it
                    this.ActiveIdentity = new Identity(name, IdentityType.Costume, name);
                }
            }
            else if (identityType == IdentityType.Costume) //A surface has been passed and it should be a Costume
            {
                //Validate the surface by checking if the costume exists
                if (File.Exists(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME, surface + Constants.GAME_COSTUMES_EXT)))
                {
                    //If valid, use it
                    this.ActiveIdentity = new Identity(surface, identityType, surface);
                }
            }
            else //A surface has been passed and it should be a model
            {
                //To do: Validate the model??
                //Use the surface as model
                this.ActiveIdentity = new Identity(surface, identityType, surface);
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        
        public Identity ActiveIdentity
        {
            get
            {
                return AvailableIdentities.Active;
            }

            set
            {
                AvailableIdentities.Active = value;
                OnPropertyChanged("ActiveIdentity");
            }
        }

        private IdentitiesCollection availableIdentities;
        public IdentitiesCollection AvailableIdentities
        {
            get
            {
                return availableIdentities;
            }
            set
            {
                availableIdentities = value;
                OnPropertyChanged("AvailableIdentities");
            }
        }
    }
}
