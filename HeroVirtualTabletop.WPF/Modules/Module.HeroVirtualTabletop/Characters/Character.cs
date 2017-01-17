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
using System.Text.RegularExpressions;
using System.Windows.Media;
using Framework.WPF.Extensions;
using System.Runtime.Serialization;
using Module.HeroVirtualTabletop.Movements;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Utility;

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

        protected void InitializeCharacter()
        {
            //availableIdentities = new OptionGroup<Identity>();
            //animatedAbilities = new OptionGroup<AnimatedAbility>();
            optionGroups = new HashedObservableCollection<IOptionGroup, string>(x => x.Name);
            OptionGroups = new ReadOnlyHashedObservableCollection<IOptionGroup, string>(optionGroups);
            this.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.None, AttackEffectOption = AttackEffectOption.None };
            this.OptionGroupExpansionStates = new Dictionary<string, bool>();
        }

        public Character(string name): this()
        {
            Name = name;
            IOptionGroup tmp = AvailableIdentities;
            tmp = AnimatedAbilities;
            tmp = Movements;
            tmp = null;
            SetActiveIdentity();
        }

        public Character(string name, string surface, IdentityType identityType): this(name)
        {
            SetActiveIdentity(surface, identityType);
        }

        [OnDeserialized]
        private void AfterDeserialized(StreamingContext stream)
        {
            foreach (IOptionGroup opt in this.OptionGroups)
            {
                opt.UpdateIndices();
            }
            IOptionGroup x = AvailableIdentities;
            x = AnimatedAbilities;
            x = Movements;
            x = null;
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
                    if (!AvailableIdentities.ContainsKey(name))
                    {
                        AvailableIdentities.Add(new Identity(name, IdentityType.Costume, name));
                    }
                    //Costume exists, use it
                    this.ActiveIdentity = AvailableIdentities[name];
                    this.DefaultIdentity = AvailableIdentities[name];
                    if (AvailableIdentities.ContainsKey("Base"))
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
                    if (!AvailableIdentities.ContainsKey(surface))
                    {
                        AvailableIdentities.Add(new Identity(surface, identityType, surface));
                    }
                    //If valid, use it
                    this.ActiveIdentity = AvailableIdentities[surface];
                }
            }
            else //A surface has been passed and it should be a model
            {
                //To do: Validate the model??
                //Use the surface as model
                if (!AvailableIdentities.ContainsKey(surface))
                {
                    AvailableIdentities.Add(new Identity(surface, identityType, surface));
                }
                //If valid, use it
                this.ActiveIdentity = AvailableIdentities[surface];
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

        private ActiveAttackConfiguration activeAttackConfig;
        [JsonIgnore]
        public ActiveAttackConfiguration ActiveAttackConfiguration
        {
            get
            {
                return activeAttackConfig;
            }
            set
            {
                activeAttackConfig = value;
                OnPropertyChanged("ActiveAttackConfiguration");
            }
        }

        [JsonIgnore]
        public string OldName { get; private set; }
        
        private IMemoryElementPosition position;
        [JsonIgnore]
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
        [JsonIgnore]
        public Microsoft.Xna.Framework.Matrix CurrentModelMatrix
        {
            get
            {
                return (Position as Position).GetModelMatrix();
            }
            set
            {
                (Position as Position).SetModelMatrix(value);
            }
        }

        [JsonIgnore]
        public Vector3 CurrentPositionVector
        {
            get
            {
                return Helper.GetRoundedVector((Position as Position).GetPositionVector(), 2);
            }
            set
            {
                (Position as Position).SetPosition(value);
            }
        }

        [JsonIgnore]
        public Vector3 CurrentFacingVector
        {
            get
            {
                return Helper.GetRoundedVector((Position as Position).GetFacingVector(), 2);
            }
            set
            {
                (Position as Position).SetTargetFacing(value);
            }
        }

        [JsonIgnore]
        public bool IsFollowed
        {
            get;
            set;
        }
        [JsonIgnore]
        public bool IsMoving
        {
            get;
            set;
        }
        [JsonIgnore]
        public Dictionary<string, bool> OptionGroupExpansionStates
        {
            get;
            set;
        }

        protected virtual string GetLabel()
        {
            return name;
        }

        [JsonProperty(PropertyName = "OptionGroups", Order = 0)]
        private HashedObservableCollection<IOptionGroup, string> optionGroups;
        [JsonIgnore]
        public ReadOnlyHashedObservableCollection<IOptionGroup, string> OptionGroups { get; private set; }


        public void RemoveOptionGroup(IOptionGroup optGroup)
        {
            optionGroups.Remove(optGroup);
            OnPropertyChanged("OptionGroups");
        }

        public void RemoveOptionGroupAt(int index)
        {
            optionGroups.RemoveAt(index);
            OnPropertyChanged("OptionGroups");
        }

        public void AddOptionGroup(IOptionGroup optGroup)
        {
            optionGroups.Add(optGroup);
            OnPropertyChanged("OptionGroups");
        }

        public void InsertOptionGroup(int index, IOptionGroup optGroup)
        {
            optionGroups.Insert(index, optGroup);
            OnPropertyChanged("OptionGroups");
        }
        
        //private OptionGroup<Identity> availableIdentities;
        //[JsonProperty(Order = 0)]
        [JsonIgnore]
        public OptionGroup<Identity> AvailableIdentities
        {
            get
            {
                OptionGroup<Identity> availableIdentities = optionGroups.DefaultIfEmpty(null).FirstOrDefault((optg) => { return optg != null && optg.Name == Constants.IDENTITY_OPTION_GROUP_NAME; }) as OptionGroup<Identity>;
                if (availableIdentities == null)
                {
                    availableIdentities = new OptionGroup<Identity>(Constants.IDENTITY_OPTION_GROUP_NAME);
                    optionGroups.Add(availableIdentities);
                }
                return availableIdentities;
            }
            //set
            //{
            //    availableIdentities = value;
            //    OnPropertyChanged("AvailableIdentities");
            //}
        }

        [JsonProperty(PropertyName = "defaultIdentity")]
        private Identity defaultIdentity;
        [JsonProperty(Order = 1)]
        public Identity DefaultIdentity
        {
            get
            {
                if (defaultIdentity == null || !AvailableIdentities.Contains(defaultIdentity))
                {
                    if (AvailableIdentities.Count > 0)
                    {
                        defaultIdentity = AvailableIdentities[0];
                    }
                    else
                    {
                        defaultIdentity = new Identity("Model_Statesman", IdentityType.Model, "Base");
                        AvailableIdentities.Add(defaultIdentity);
                    }
                }
                return defaultIdentity;
            }
            set
            {
                AvailableIdentities.UpdateIndices();
                if (value != null && !AvailableIdentities.ContainsKey(value.Name))
                {
                    AvailableIdentities.Add(value);
                }
                if (value != null)
                    defaultIdentity = AvailableIdentities[value.Name];
                else
                    defaultIdentity = null;
                OnPropertyChanged("DefaultIdentity");
            }
        }

        private CharacterMovement defaultMovement;
        [JsonProperty(Order = 3)]
        public CharacterMovement DefaultMovement
        {
            get
            {
                return defaultMovement;
            }
            set
            {
                defaultMovement = value;
                OnPropertyChanged("DefaultMovement");
            }
        }

        private MovementInstruction movementInstruction;
        [JsonIgnore]
        public MovementInstruction MovementInstruction
        {
            get
            {
                return movementInstruction;
            }
            set
            {
                movementInstruction = value;
            }
        }

        private Identity activeIdentity;
        [JsonProperty(Order = 2)]
        public Identity ActiveIdentity
        {
            get
            {
                if (activeIdentity == null || !AvailableIdentities.ContainsKey(activeIdentity.Name))
                {
                    activeIdentity = DefaultIdentity;
                }
                
                return activeIdentity;
            }

            set
            {
                //Deactivate all active persistent abilities on Identity Change
                AnimatedAbilities.Where((ab) => { return ab.IsActive; }).ToList().ForEach((ab) => { ab.Stop(); });
                //Deactive any effect activated as a result of former Identity loading
                if (activeIdentity != null && activeIdentity.AnimationOnLoad != null)
                    activeIdentity.AnimationOnLoad.Stop();
                AvailableIdentities.UpdateIndices();
                if (value != null && !AvailableIdentities.ContainsKey(value.Name))
                {   
                    AvailableIdentities.Add(value);
                }
                if (value != null)
                {
                    activeIdentity = AvailableIdentities[value.Name];
                    if (HasBeenSpawned)
                    {
                        Target(false);
                        activeIdentity.Render();
                    }
                        
                }
                else
                {
                    activeIdentity = null;
                }
                OnPropertyChanged("ActiveIdentity");
            }
        }
        private CharacterMovement activeMovement;
        [JsonIgnore]
        public CharacterMovement ActiveMovement
        {
            get
            {
                return activeMovement;
            }
            set
            {
                activeMovement = value;
                OnPropertyChanged("ActiveMovement");
            }
        }

        private AnimatedAbility defaultAbility;
        [JsonProperty(Order = 4)]
        public AnimatedAbility DefaultAbility
        {
            get
            {
                return defaultAbility;
            }
            set
            {
                defaultAbility = value;
                OnPropertyChanged("DefaultAbility");
            }
        }

        private AnimatedAbility activeAbility;
        [JsonIgnore]
        public AnimatedAbility ActiveAbility
        {
            get
            {
                return activeAbility;
            }
            set
            {
                activeAbility = value;
                OnPropertyChanged("ActiveAbility");
            }
        }
        [JsonIgnore]
        public AnimatedAbility DefaultAbilityToActivate
        {
            get
            {
                AnimatedAbility ability = null;

                if(this.DefaultAbility != null)
                    ability = this.DefaultAbility;
                else if(this.AnimatedAbilities != null && this.AnimatedAbilities.Count > 0)
                {
                    ability = this.AnimatedAbilities.First();
                }

                return ability;
            }
        }
        [JsonIgnore]
        public string LastCostumeFile
        {
            get;
            set;
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
        public bool IsSyncedWithGame
        { get; set; }

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
            keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, Label);

            if (gamePlayer != null && gamePlayer.IsReal)
            {
                gamePlayer.Target(); //This ensure targeting even if not in view
                WaitUntilTargetIsRegistered();
            }
            else
            {
                if (completeEvent)
                {
                    keybind = keyBindsGenerator.CompleteEvent();
                    gamePlayer = WaitUntilTargetIsRegistered();
                    Position = new Position();
                }
            }
            return keybind;
            //}
            //return string.Empty;
        }

        public void ResetOrientation()
        {
            Vector3 currentPositionVector = this.CurrentPositionVector;
            Vector3 currentForwardVector = this.CurrentModelMatrix.Forward;
            Vector3 currentFacing = this.CurrentFacingVector;

            Microsoft.Xna.Framework.Matrix defaultMatrix = new Microsoft.Xna.Framework.Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            this.CurrentModelMatrix = defaultMatrix;
            this.CurrentFacingVector = currentFacing;
            this.CurrentPositionVector = currentPositionVector;

            #region old angle based calculations - doesn't work for all scenarios
            //Vector3 currentRightVector = this.CurrentModelMatrix.Right;
            //Vector3 currentLeftVector = this.CurrentModelMatrix.Left;
            //double forwardX = Math.Round(currentForwardVector.X, 2);
            //double forwardZ = Math.Round(currentForwardVector.Z, 2);
            //float negY = -1 * currentForwardVector.Y;
            //float sineValue = negY > 1 ? 1 : negY < -1 ? -1 : negY;
            //float currentPitchAngle = (float)(Math.Asin(sineValue) * 180 / Math.PI);
            
            //var yaw = Math.Atan2(forwardX, forwardZ);
            //var yawAngle = (float)(yaw * 180 / Math.PI);

            //var roll = Vector3.Dot(Vector3.Up, this.CurrentModelMatrix.Up);
            //var rollValue = roll > 1 ? 1 : roll < -1 ? -1 : roll;
            //var rollAngle1Red= Math.Acos(rollValue);
            //var rollAngle = (float)(rollAngle1Red * 180 / Math.PI);

            //currentPitchAngle = (float)Math.Round(currentPitchAngle, 2);
            //var currentYawAngle = (float)Math.Round(yawAngle, 2);
            //var currentRollAngle = (float)Math.Round(rollAngle, 2);

            //if (currentPitchAngle != 0)
            //{
            //    if (forwardX < 0)
            //        currentPitchAngle = 180 - currentPitchAngle;
            //    Microsoft.Xna.Framework.Matrix rotatedMatrix = Microsoft.Xna.Framework.Matrix.CreateFromAxisAngle(currentRightVector, (float)Helper.GetRadianAngle(currentPitchAngle));
            //    this.CurrentModelMatrix *= rotatedMatrix;
            //    this.CurrentPositionVector = currentPositionVector; // Keep position intact; 
            //}
            #endregion
        }

        public void TargetAndFollow(bool completeEvent = true)
        {
            Target(false);
            if(!this.IsMoving)
            {
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
                this.IsFollowed = true;
            }
            
            if (completeEvent)
                keyBindsGenerator.CompleteEvent();
        }

        public void UnFollow()
        {
            if(this.IsFollowed)
            {
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
                keyBindsGenerator.CompleteEvent();
                this.IsFollowed = false;
            }
        }

        public string UnTarget(bool completeEvent = true)
        {
            keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            this.UnFollow();
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
            if(this.HasBeenSpawned)
            {
                this.UnFollow();
                Target(false);
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.DeleteNPC);
                if (completeEvent)
                {
                    keyBindsGenerator.CompleteEvent();
                }
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

        //Jeff - added so we always get the right movement when dragging, moving, etc...
        [JsonIgnore]
        public CharacterMovement DefaultMovementToActivate
        {
            get {
                CharacterMovement defaultMovementToActivate =null;
                if (this.ActiveMovement != null)
                {
                    defaultMovementToActivate = this.ActiveMovement;
                }
                else
                {
                    if (this.DefaultMovement != null)
                    {
                        defaultMovementToActivate = this.DefaultMovement;
                    }
                    else
                    {

                        if (this.Movements["Walk"] != null)
                        {
                            defaultMovementToActivate = this.Movements["Walk"];
                        }
                        else
                        {
                            if (this.Movements.Count> 0)
                            {
                                defaultMovementToActivate = this.Movements[0];
                            }
                        }
                    }
                }
                return defaultMovementToActivate;
            }
        }
        public string MoveToCamera(bool completeEvent = true)
        {
            Target(false);
            CharacterMovement characterMovement = this.DefaultMovementToActivate;
            
            // characterMovement = this.Movements.FirstOrDefault(cm => cm.IsActive || cm == this.DefaultMovement || cm.Name == "Walk");
            if (characterMovement == null)
            {
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.MoveNPC);
                if (completeEvent)
                {
                    keyBindsGenerator.CompleteEvent();
                }
            }
            else
            {
                var cameraPos = new Camera().GetPositionVector();
 
                cameraPos.Y = new Camera().GetPositionVector().Y - (float) 3.2;
                characterMovement.Movement.Move(this, cameraPos);
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

        public void AddDefaultMovements()
        {
            string[] defaultMovementNames = new string[] { "Walk", "Run", "Swim" };
            foreach (CharacterMovement cm in Helper.GlobalMovements.Where(gm => defaultMovementNames.Contains(gm.Name)))
            {
                if (this.Name != Constants.DEFAULT_CHARACTER_NAME && this.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && this.Movements.FirstOrDefault(m => m.Name == cm.Name) == null)
                {
                    CharacterMovement cmDefault = new CharacterMovement(cm.Name, this);
                    cmDefault.Movement = cm.Movement;
                    this.Movements.Add(cmDefault);
                }
            }
        }
        
        internal void SetAsSpawned()
        {
            hasBeenSpawned = true;
            gamePlayer = new MemoryElement();
            Position = new Position();
        }

        [JsonIgnore]
        public OptionGroup<AnimatedAbility> AnimatedAbilities
        {
            get
            {
                OptionGroup<AnimatedAbility> animatedAbilities = optionGroups.DefaultIfEmpty(null).FirstOrDefault((optg) => { return optg != null && optg.Name == Constants.ABILITY_OPTION_GROUP_NAME; }) as OptionGroup<AnimatedAbility>;
                if (animatedAbilities == null)
                {
                    animatedAbilities = new OptionGroup<AnimatedAbility>(Constants.ABILITY_OPTION_GROUP_NAME);
                    optionGroups.Add(animatedAbilities);
                }
                return animatedAbilities;
            }
        }

        [JsonIgnore]
        public OptionGroup<CharacterMovement> Movements
        {
            get
            {
                OptionGroup<CharacterMovement> movements = optionGroups.DefaultIfEmpty(null).FirstOrDefault((optg) => { return optg != null && optg.Name == Constants.MOVEMENT_OPTION_GROUP_NAME; }) as OptionGroup<CharacterMovement>;
                if (movements == null)
                {
                    movements = new OptionGroup<CharacterMovement>(Constants.MOVEMENT_OPTION_GROUP_NAME);
                    optionGroups.Add(movements);
                }
                return movements;
            }
        }

        public void Activate()
        {
            if (HasBeenSpawned && this.ActiveIdentity.Type == IdentityType.Costume)
            {
                Target(false);
                //ChangeCostumeColor(new ColorExtensions.RGB() { R = 255, G = 0, B = 51 });
            }
        }

        private void changeColorIntoCharacterCostumeFile(string origFile, string newFile, ColorExtensions.RGB color, int colorNumber = 2)
        {
            if (colorNumber < 1 || colorNumber > 4)
            {
                return;
            }
            string fileStr = File.ReadAllText(origFile);
            string color2 = "Color" + colorNumber + @"\s+([\d]{1,3}),\s+([\d]{1,3}),\s+([\d]{1,3})";
            Regex re = new Regex(color2);
            fileStr = re.Replace(fileStr, string.Format("Color2 {0}, {1}, {2}", color.R, color.G, color.B));

            File.AppendAllText(newFile, fileStr);
        }

        public void Deactivate()
        {

            if (this.ActiveIdentity.Type != IdentityType.Costume)
                return;

            bool persistentAbilityActive = this.AnimatedAbilities.Where(aa => aa.Persistent && aa.IsActive).FirstOrDefault() != null || (this.ActiveIdentity.AnimationOnLoad != null && this.ActiveIdentity.AnimationOnLoad.Persistent);

            string archFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                ActiveIdentity.Surface + "_original" + Constants.GAME_COSTUMES_EXT);
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                ActiveIdentity.Surface + Constants.GAME_COSTUMES_EXT);
            // Load persistent fx if present
            if (persistentAbilityActive)
            {
                string persistentCostumeFile = Path.Combine(
                    Settings.Default.CityOfHeroesGameDirectory,
                    Constants.GAME_COSTUMES_FOLDERNAME,
                    this.ActiveIdentity.Surface + "_persistent" + Constants.GAME_COSTUMES_EXT);
                if (File.Exists(persistentCostumeFile))
                {
                    Target(false);
                    File.Copy(persistentCostumeFile, origFile, true);
                    KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, ActiveIdentity.Surface);
                    keyBindsGenerator.CompleteEvent();
                }
                
            } // Otherwise load default costume
            else if (File.Exists(archFile))
            {
                Target(false);
                File.Copy(archFile, origFile, true);
                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, ActiveIdentity.Surface);
                keyBindsGenerator.CompleteEvent();
            }
        }

        public void ChangeCostumeColor(ColorExtensions.RGB color, int colorNumber = 2)
        {
            if (this.ActiveIdentity.Type != IdentityType.Costume)
                return;

            bool persistentAbilityActive = this.AnimatedAbilities.Where(aa => aa.Persistent && aa.IsActive).FirstOrDefault() != null || (this.ActiveIdentity.AnimationOnLoad != null && this.ActiveIdentity.AnimationOnLoad.Persistent);

            string name = ActiveIdentity.Surface;
            string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            string file = name + Constants.GAME_COSTUMES_EXT;
            string origFile = Path.Combine(location, file);

            // Archive original file
            string archFile = Path.Combine(
            Settings.Default.CityOfHeroesGameDirectory,
            Constants.GAME_COSTUMES_FOLDERNAME,
            this.ActiveIdentity.Surface + "_original" + Constants.GAME_COSTUMES_EXT);
            if (!File.Exists(archFile))
            {
                File.Copy(origFile, archFile, true);
            }
            // Archive persistent fx
            if(persistentAbilityActive)
            {
                string persistentCostumeFile = Path.Combine(
                    Settings.Default.CityOfHeroesGameDirectory,
                    Constants.GAME_COSTUMES_FOLDERNAME,
                    this.ActiveIdentity.Surface + "_persistent" + Constants.GAME_COSTUMES_EXT);

                File.Copy(origFile, persistentCostumeFile, true);
            }

            string newFolder = Path.Combine(location, name);
            string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", name, color, Constants.GAME_COSTUMES_EXT));
            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(newFolder);
            }
            if (File.Exists(newFile))
            {
                File.Delete(newFile);
            }

            if (File.Exists(origFile))
            {
                changeColorIntoCharacterCostumeFile(origFile, newFile, color, colorNumber);
                string coloredCostume = Path.Combine(name, string.Format("{0}_{1}", name, color));
                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                Target(false);
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, coloredCostume);
                keybind = keyBindsGenerator.CompleteEvent();
            }
        }

        //private void invertColorIntoCharacterCostumeFile(string origFile, string newFile, int colorNumber = 2)
        //{
        //    if (colorNumber < 1 || colorNumber > 4)
        //    {
        //        return;
        //    }
        //    string fileStr = File.ReadAllText(origFile);
        //    string color2 = "Color" + colorNumber + @"\s+(?<Red>[\d]{1,3}),\s+(?<Green>[\d]{1,3}),\s+(?<Blue>[\d]{1,3})";
        //    Regex re = new Regex(color2);

        //    List<Color> colorsFound = new List<Color>();
        //    Dictionary<Color,Color> contrastColors = new Dictionary<Color, Color>();

        //    foreach (Match match in re.Matches(fileStr))
        //    {
        //        ColorExtensions.RGB rgb = new ColorExtensions.RGB()
        //        {
        //            R = double.Parse(match.Groups["Red"].Value),
        //            G = double.Parse(match.Groups["Green"].Value),
        //            B = double.Parse(match.Groups["Blue"].Value)
        //        };
        //        Color color = Color.FromRgb((byte)rgb.R, (byte)rgb.G, (byte)rgb.B);
        //        if (!colorsFound.Contains(color))
        //        {
        //            colorsFound.Add(color);
        //            Color contrast = color.GetContrast();
        //            contrastColors.Add(color, contrast);
        //        }
        //    }

        //    foreach (Color c in colorsFound)
        //    {
        //        string pattern = string.Format("Color" + colorNumber + @"\s+({0}),\s+({1}),\s+({2})", c.R, c.G, c.B);
        //        re = new Regex(pattern);
        //        fileStr = re.Replace(fileStr, string.Format("Color2 {0}, {1}, {2}", contrastColors[c].R, contrastColors[c].G, contrastColors[c].B));
        //    }

        //    File.AppendAllText(newFile, fileStr);
        //}

        #region Movements

        public void ActivateMovement(Movement movement)
        {

        }

        public void MoveBasedOnKey(string key)
        {

        }

        public void DisableCamera()
        {

        }

        public void SwitchToCamera()
        {

        }

        public void MoveToDirection(MovementDirection direction)
        {

        }

        public void MoveToLocation(Vector3 destinationVector)
        {
            CharacterMovement characterMovement = this.DefaultMovementToActivate;
            //CharacterMovement characterMovement = this.Movements.FirstOrDefault(cm => cm.IsActive || cm == this.DefaultMovement || cm.Name == "Walk");
            if (characterMovement != null)
            {
                characterMovement.Movement.Move(this, destinationVector);
            }
        }

        public void Turn(MovementDirection direction, double distance)
        {

        }

        public void TurnBasedOnKey(string key)
        {

        }

        public void TurnOnFacing(Vector3 facing)
        {

        }

        #endregion
    }
}
