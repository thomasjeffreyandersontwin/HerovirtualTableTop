using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.Shared.Enumerations;
using Newtonsoft.Json;

namespace Module.HeroVirtualTabletop.Characters
{
    public class Character : NotifyPropertyChanged
    {
        //private KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
        //private bool hasBeenSpawned = false;
        //private MemoryElement cohPlayer;


        public Character()
        {
            InitializeCharacter();
        }

        private void InitializeCharacter()
        {
            availableIdentities = new OptionGroup<Identity>();
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
                SetActiveIdentity();
                OnPropertyChanged("Name");
            }
        }

        private OptionGroup<Identity> availableIdentities;
        [JsonProperty(Order = 0)]
        public OptionGroup<Identity> AvailableIdentities
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

        private Identity defaultIdentity;
        [JsonProperty(Order = 1)]
        public Identity DefaultIdentity
        {
            get
            {
                if (defaultIdentity == null || !availableIdentities.Contains(defaultIdentity))
                {
                    if (availableIdentities.Count > 0)
                    {
                        defaultIdentity = availableIdentities[0];
                    }
                    else
                    {
                        defaultIdentity = new Identity("model_Statesman", IdentityType.Model, "Base");
                    }
                }
                return defaultIdentity;
            }
            set
            {
                if (value != null && !availableIdentities.Contains(value))
                {
                    availableIdentities.Add(value);
                }
                defaultIdentity = value;
                OnPropertyChanged("DefaultIdentity");
            }
        }

        private Identity activeIdentity;
        [JsonProperty(Order = 2)]
        public Identity ActiveIdentity
        {
            get
            {
                if (activeIdentity == null || !availableIdentities.Contains(activeIdentity))
                {
                    activeIdentity = DefaultIdentity;
                }
                
                return activeIdentity;
            }

            set
            {
                if (value != null && !availableIdentities.Contains(value))
                {
                    availableIdentities.Add(value);
                }
                activeIdentity = value;
                OnPropertyChanged("ActiveIdentity");
            }
        }


    }
}
