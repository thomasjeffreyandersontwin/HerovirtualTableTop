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
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Characters
{
    public class Character : NotifyPropertyChanged
    {
        private KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
        private string keybind;
        protected internal IMemoryElement gamePlayer;

        [JsonConstructor()]
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
                    if (!availableIdentities.ContainsKey(name))
                    {
                        availableIdentities.Add(new Identity(name, IdentityType.Costume, name));
                    }
                    //Costume exists, use it
                    this.ActiveIdentity = availableIdentities[name];
                }
            }
            else if (identityType == IdentityType.Costume) //A surface has been passed and it should be a Costume
            {
                //Validate the surface by checking if the costume exists
                if (File.Exists(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME, surface + Constants.GAME_COSTUMES_EXT)))
                {
                    if (!availableIdentities.ContainsKey(surface))
                    {
                        availableIdentities.Add(new Identity(surface, identityType, surface));
                    }
                    //If valid, use it
                    this.ActiveIdentity = availableIdentities[surface];
                }
            }
            else //A surface has been passed and it should be a model
            {
                //To do: Validate the model??
                //Use the surface as model
                if (!availableIdentities.ContainsKey(surface))
                {
                    availableIdentities.Add(new Identity(surface, identityType, surface));
                }
                //If valid, use it
                this.ActiveIdentity = availableIdentities[surface];
            }
        }

        [JsonProperty(PropertyName = "Name")]
        private string name;
        [JsonIgnore]
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
        
        private Position position;
        public Position Position
        {
            get
            {
                return position;
            }

            set
            {
                position = value;
            }
        }

        private bool hasBeenSpawned;
        [JsonIgnore]
        public bool HasBeenSpawned
        {
            get
            {
                return hasBeenSpawned;
            }
        }

        [JsonIgnore]
        public string Label
        {
            get
            {
                return GetLabel();
            }
        }

        protected virtual string GetLabel()
        {
            return name;
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
                if (value != null && !availableIdentities.ContainsKey(value.Name))
                {
                    availableIdentities.Add(value);
                }
                defaultIdentity = availableIdentities[value.Name];
                OnPropertyChanged("DefaultIdentity");
            }
        }

        private Identity activeIdentity;
        [JsonProperty(Order = 2)]
        public Identity ActiveIdentity
        {
            get
            {
                if (activeIdentity == null || !availableIdentities.ContainsKey(activeIdentity.Name))
                {
                    activeIdentity = DefaultIdentity;
                }
                
                return activeIdentity;
            }

            set
            {
                if (value != null && !availableIdentities.ContainsKey(value.Name))
                {
                    availableIdentities.Add(value);
                }
                activeIdentity = availableIdentities[value.Name];
                OnPropertyChanged("ActiveIdentity");
            }
        }

        public string Spawn(bool completeEvent = true)
        {
            if (hasBeenSpawned)
            {
                Target();
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.DeleteNPC);
                gamePlayer = null;
            }
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            
            hasBeenSpawned = true;
            string model = "model_Statesman";
            if (ActiveIdentity.Type == IdentityType.Model)
            {
                model = ActiveIdentity.Surface;
            }
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, model, Label);
            Target(false);
            keybind = ActiveIdentity.Render(completeEvent);
            if (completeEvent)
            {
                WaitUntilTargetIsRegistered();
                gamePlayer = new MemoryElement();
                Position = new Position();
            }
            return keybind;
        }

        [JsonIgnore]
        public bool IsTargeted
        {
            get
            {
                try
                {
                    MemoryElement currentTarget = new MemoryElement();
                    if (currentTarget.Label == this.Label)
                    {
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }

            set
            {
                if (value == true)
                {
                    Target();
                }
                else
                {
                    if (value == false)
                    {
                        UnTarget();
                    }
                }
            }
        }

        public string Target(bool completeEvent = true)
        {
            //if (hasBeenSpawned)
            //{
                if (gamePlayer != null && gamePlayer.IsReal)
                {
                    gamePlayer.Target(); //This ensure targeting even if not in view
                    WaitUntilTargetIsRegistered();
                }
                else
                {
                    keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, Label);
                    if (completeEvent)
                    {
                        keybind = keyBindsGenerator.CompleteEvent();
                        gamePlayer = WaitUntilTargetIsRegistered();
                    }
                }
                return keybind;
            //}
            //return string.Empty;
        }

        public void TargetAndFollow(bool completeEvent = true)
        {
            Target(false);
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
            if (completeEvent)
                keyBindsGenerator.CompleteEvent();
        }

        public string UnTarget(bool completeEvent = true)
        {
            keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            if (completeEvent)
            {
                keybind = keyBindsGenerator.CompleteEvent();
                try
                {
                    MemoryElement currentTarget = new MemoryElement();
                    while (currentTarget.Label != string.Empty)
                    {
                        currentTarget = new MemoryElement();
                    }
                }
                catch
                {

                }
            }
            return keybind;
        }

        public MemoryElement WaitUntilTargetIsRegistered()
        {
            int w = 0;
            MemoryElement currentTarget = new MemoryElement();
            while (Label != currentTarget.Label)
            {
                w++;
                currentTarget = new MemoryElement();
                if (w > 500)
                {
                    currentTarget = null;
                    break;
                }
            }
            return currentTarget;
        }

        public string ClearFromDesktop(bool completeEvent = true)
        {
            Target(false);
            keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.DeleteNPC);
            if (completeEvent)
            {
                keyBindsGenerator.CompleteEvent();
            }
            gamePlayer = null;
            hasBeenSpawned = false;
            return keybind;
        }

        public void ToggleTargeted()
        {
            IsTargeted = !IsTargeted;
        }
    }
}
