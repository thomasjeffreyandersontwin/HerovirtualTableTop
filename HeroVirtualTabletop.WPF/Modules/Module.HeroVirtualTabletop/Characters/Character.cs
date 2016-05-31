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
using Module.HeroVirtualTabletop.AnimatedAbilities;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Characters
{
    public class Character : NotifyPropertyChanged
    {
        private KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
        protected internal Camera camera = new Camera();
        private string keybind;
        private string[] keybinds;
        protected internal IMemoryElement gamePlayer;

        [JsonConstructor()]
        public Character()
        {
            InitializeCharacter();
        }

        private void InitializeCharacter()
        {
            availableIdentities = new OptionGroup<Identity>();
            animatedAbilities = new OptionGroup<AnimatedAbility>();
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
                    this.DefaultIdentity = availableIdentities[name];
                    if (availableIdentities.ContainsKey("Base"))
                    {
                        AvailableIdentities.Remove("Base");
                    }
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
                OldName = name;
                name = value;
                SetActiveIdentity();
                OnPropertyChanged("Name");
            }
        }

        [JsonIgnore]
        public string OldName { get; private set; }
        
        private IMemoryElementPosition position;
        public IMemoryElementPosition Position
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
                        AvailableIdentities.Add(defaultIdentity);
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
                if (value != null)
                    defaultIdentity = availableIdentities[value.Name];
                else
                    defaultIdentity = null;
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
                if (value != null)
                {
                    activeIdentity = availableIdentities[value.Name];
                    if (HasBeenSpawned)
                        activeIdentity.Render();
                }
                else
                {
                    activeIdentity = null;
                }
                OnPropertyChanged("ActiveIdentity");
            }
        }

        public string Spawn(bool completeEvent = true)
        {
            if (ManeuveringWithCamera)
            {
                ManeuveringWithCamera = false;
            }
            if (hasBeenSpawned)
            {
                Target();
                WaitUntilTargetIsRegistered();
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.DeleteNPC);
                gamePlayer = null;
            }
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.NOP); //No operation, let the game untarget whatever it has targeted
            
            hasBeenSpawned = true;
            string model = "Model_Statesman";
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
                if (w > 5)
                {
                    currentTarget = null;
                    break;
                }
            }
            return currentTarget;
        }

        private string clearFromDesktop(bool completeEvent = true)
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

        public string ClearFromDesktop(bool completeEvent = true)
        {
            if (ManeuveringWithCamera)
            {
                ManeuveringWithCamera = false;
            }
            return clearFromDesktop(completeEvent);
        }

        public string ClearFromDesktop(bool completeEvent, bool callingFromCamera = false)
        {
            if (callingFromCamera)
                return clearFromDesktop(completeEvent);
            else
                return ClearFromDesktop(completeEvent);
        }

        public void ToggleTargeted()
        {
            IsTargeted = !IsTargeted;
        }

        public string MoveToCamera(bool completeEvent = true)
        {
            Target(false);
            keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.MoveNPC);
            if (completeEvent)
            {
                keyBindsGenerator.CompleteEvent();
            }
            return keybind;
        }

        public void ToggleManueveringWithCamera()
        {
            ManeuveringWithCamera = !ManeuveringWithCamera;
        }

        private bool maneuveringWithCamera;
        [JsonIgnore]
        public bool ManeuveringWithCamera
        {
            get
            {
                return maneuveringWithCamera;
            }

            set
            {
                maneuveringWithCamera = value;
                if (value == true)
                {
                    camera.ManeuveredCharacter = this;
                    keybinds = camera.LastKeybinds;
                }
                else
                {
                    if (value == false)
                    {
                        camera.ManeuveredCharacter = null;
                        keybinds = camera.LastKeybinds;
                    }
                }
            }
        }
        
        internal void SetAsSpawned()
        {
            hasBeenSpawned = true;
            gamePlayer = new MemoryElement();
            Position = new Position();
        }

        private OptionGroup<AnimatedAbility> animatedAbilities;
        [JsonProperty(Order = 3)]
        public OptionGroup<AnimatedAbility> AnimatedAbilities
        {
            get
            {
                return animatedAbilities;
            }
            set
            {
                animatedAbilities = value;
                OnPropertyChanged("AnimatedAbilities");
            }
        }
    }
}
