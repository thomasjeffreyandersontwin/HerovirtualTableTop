<<<<<<< HEAD
﻿using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Enumerations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Movements
{
    public class CharacterMovement: CharacterOption
    {
        private IntPtr hookID;

        [JsonConstructor]
        private CharacterMovement() { }

        public CharacterMovement(string name, Character owner = null)
        {
            this.Name = name;
            this.Character = owner;
        }

        private bool isActive;
        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        }
        private Character character;
        public Character Character
        {
            get
            {
                return character;
            }
            set
            {
                character = value;
                OnPropertyChanged("Character");
            }
        }

        private Movement movement;
        public Movement Movement
        {
            get
            {
                return movement;
            }
            set
            {
                movement = value;
                OnPropertyChanged("Movement");
            }
        }
        public void DeactivateMovement()
        {
            // Reset Active
            this.IsActive = false;
            // Back to still move
            this.Movement.MoveStill(this.Character);
            // Reset MovementInstruction
            this.Character.MovementInstruction = null;
            // Enable Camera Control
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_ENABLE_CAMERA_FILENAME);
            keyBindsGenerator.CompleteEvent();
            // Unload Keyboard Hook
            KeyBoardHook.UnsetHook(hookID);
            this.Movement.StopMovement();
        }

        public void ActivateMovement()
        {
            // Deactivate Current Movement
            CharacterMovement activeCharacterMovement = this.Character.Movements.FirstOrDefault(cm => cm != this && cm.IsActive);
            if (activeCharacterMovement != null)
                activeCharacterMovement.DeactivateMovement();
            // Set Active
            this.IsActive = true;
            // Set the still Move
            this.Movement.MoveStill(this.Character);
            // Initialize MovementInstruction
            this.Character.MovementInstruction = new MovementInstruction();
            // Disable Camera Control
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_DISABLE_CAMERA_FILENAME);
            keyBindsGenerator.CompleteEvent();
            // Load Keyboard Hook
            hookID = KeyBoardHook.SetHook(this.PlayMovementByKeyProc);
        }

        private IntPtr PlayMovementByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardDLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)keyboardDLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    var cohWindow = WindowsUtilities.FindWindow("CrypticWindow", null);
                    var currentProcId = Process.GetCurrentProcess().Id;
                    if (foregroundWindow == cohWindow
                        || currentProcId == wndProcId)
                    {
                        //if(!this.Movement.IsPlaying)
                        {
                            var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                            MovementDirection direction = GetMovementDirectionFromKey(inputKey);
                            if(direction != MovementDirection.Still && this.Character.MovementInstruction.CurrentDirection != direction)
                            {
                                this.Character.MovementInstruction.LastDirection = this.Character.MovementInstruction.CurrentDirection;
                                this.Character.MovementInstruction.CurrentDirection = direction;
                                this.Movement.StartMovment(this.Character);
                            } 
                        }
                    }
                    WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private MovementDirection GetMovementDirectionFromKey(Key key)
        {
            MovementDirection movementDirection = MovementDirection.Still;
            switch(key)
            {
                case Key.A:
                    movementDirection = MovementDirection.Left;
                    break;
                case Key.W:
                    movementDirection = MovementDirection.Forward;
                    break;
                case Key.S:
                    movementDirection = MovementDirection.Backward;
                    break;
                case Key.D:
                    movementDirection = MovementDirection.Right;
                    break;
                case Key.Space:
                    movementDirection = MovementDirection.Upward;
                    break;
                case Key.Z:
                    movementDirection = MovementDirection.Downward;
                    break;

            }
            return movementDirection;
        }
    }
    public class Movement : NotifyPropertyChanged
    {
        private System.Threading.Timer timer;
        [JsonConstructor]
        private Movement() { }

        public Movement(string name)
        {
            this.Name = name;
            this.AddDefaultMemberAbilities();
        }

        public bool IsPlaying { get; set; }

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

        private ObservableCollection<MovementMember> movmementMembers;
        public ObservableCollection<MovementMember> MovementMembers
        {
            get
            {
                return movmementMembers;
            }
            set
            {
                movmementMembers = value;
                OnPropertyChanged("MovementMembers");
            }
        }

        public void Move(MovementDirection direction, double distance, Character target) 
        {

        }

        public void StopMovement()
        {
            this.IsPlaying = false;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMovment(Character target)
        {
            this.IsPlaying = true;
            timer = new System.Threading.Timer(timer_Elapsed, target, 1, Timeout.Infinite);
        }

        private void timer_Elapsed(object state)
        {
            Action d = delegate()
            {
                Character target = state as Character;
                if (target.MovementInstruction != null)
                {
                    MovementMember movementMember = this.MovementMembers.First(mm => mm.MovementDirection == target.MovementInstruction.CurrentDirection);
                    // if last direction is current direction, increment position
                    if (target.MovementInstruction.CurrentDirection == target.MovementInstruction.LastDirection)
                    {
                        target.MovementInstruction.LastDirection = target.MovementInstruction.CurrentDirection;
                        Key key = movementMember.AssociatedKey;
                        if (Keyboard.IsKeyDown(key))
                        {
                            //  
                            double rotationAngle = GetRotationAngle(target.MovementInstruction.CurrentDirection);
                            Vector3 rotationVector = (target.Position as Position).GetRotationVector(rotationAngle);
                            (target.Position as Position).MoveTarget(rotationVector, 2);
                        }
                        else
                        {
                            MoveStill(target);
                            target.MovementInstruction.CurrentDirection = MovementDirection.Still;
                        }
                        if(this.IsPlaying)
                            timer.Change(50, Timeout.Infinite);
                    }
                    else // else change direction and increment position
                    {
                        // Play movement
                        PlayMovementMember(movementMember, target);
                        target.MovementInstruction.LastDirection = target.MovementInstruction.CurrentDirection;
                        if (this.IsPlaying)
                            timer.Change(1, Timeout.Infinite);
                    } 
                }
            };
            System.Windows.Application.Current.Dispatcher.BeginInvoke(d);
            
        }

        public void MoveStill(Character target)
        {
            MovementMember stillMovement = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Still);
            PlayMovementMember(stillMovement, target);
        }

        public void PlayMovementMember(MovementMember movementMember, Character target)
        {
            if (movementMember != null)
            {
                if (movementMember.MemberAbility != null && !movementMember.MemberAbility.IsActive)
                {
                    foreach (var mm in this.MovementMembers.Where(mm => mm != movementMember && mm.MemberAbility.IsActive))
                    {
                        mm.MemberAbility.Stop(target);
                    }
                    movementMember.MemberAbility.Play(false, target);
                }
            }
        }

        public void MoveToLocation(IMemoryElementPosition destination, Character target)
        {

        }

        public void Turn(MovementDirection direction, Character target)
        {

        }

        public void TurnBasedOnKey(string key, Character target)
        {

        }

        private void AddDefaultMemberAbilities()
        {
            if (this.MovementMembers == null || this.MovementMembers.Count == 0)
            {
                this.MovementMembers = new ObservableCollection<MovementMember>();

                MovementMember movementMemberLeft = new MovementMember { MovementDirection = MovementDirection.Left, MemberName = "Left" };
                movementMemberLeft.MemberAbility = new ReferenceAbility("Left", null);
                movementMemberLeft.MemberAbility.DisplayName = "Left";
                this.MovementMembers.Add(movementMemberLeft);

                MovementMember movementMemberRight = new MovementMember { MovementDirection = MovementDirection.Right, MemberName = "Right" };
                movementMemberRight.MemberAbility = new ReferenceAbility("Right", null);
                movementMemberRight.MemberAbility.DisplayName = "Right";
                this.MovementMembers.Add(movementMemberRight);

                MovementMember movementMemberForward = new MovementMember { MovementDirection = MovementDirection.Forward, MemberName = "Forward" };
                movementMemberForward.MemberAbility = new ReferenceAbility("Forward", null);
                movementMemberForward.MemberAbility.DisplayName = "Forward";
                this.MovementMembers.Add(movementMemberForward);

                MovementMember movementMemberBackward = new MovementMember { MovementDirection = MovementDirection.Backward, MemberName = "Back" };
                movementMemberBackward.MemberAbility = new ReferenceAbility("Back", null);
                movementMemberBackward.MemberAbility.DisplayName = "Back";
                this.MovementMembers.Add(movementMemberBackward);

                MovementMember movementMemberUpward = new MovementMember { MovementDirection = MovementDirection.Upward, MemberName = "Up" };
                movementMemberUpward.MemberAbility = new ReferenceAbility("Up", null);
                movementMemberUpward.MemberAbility.DisplayName = "Up";
                this.MovementMembers.Add(movementMemberUpward);

                MovementMember movementMemberDownward = new MovementMember { MovementDirection = MovementDirection.Downward, MemberName = "Down" };
                movementMemberDownward.MemberAbility = new ReferenceAbility("Down", null);
                movementMemberDownward.MemberAbility.DisplayName = "Down";
                this.MovementMembers.Add(movementMemberDownward);

                MovementMember movementMemberStill = new MovementMember { MovementDirection = MovementDirection.Still, MemberName = "Still" };
                movementMemberStill.MemberAbility = new ReferenceAbility("Still", null);
                movementMemberStill.MemberAbility.DisplayName = "Still";
                this.MovementMembers.Add(movementMemberStill);
            }
        }

        private double GetRotationAngle(MovementDirection direction)
        {
            double rotationAngle = 0d;
            switch(direction)
            {
                case MovementDirection.Still:
                case MovementDirection.Forward:
                    rotationAngle = 0d;
                    break;
                case MovementDirection.Backward:
                    rotationAngle = 180d;
                    break;
                case MovementDirection.Left:
                    rotationAngle = 270d;
                    break;
                case MovementDirection.Right:
                    rotationAngle = 90d;
                    break;
                case MovementDirection.Upward:
                    rotationAngle = -90d;
                    break;
                case MovementDirection.Downward:
                    rotationAngle = 90d;
                    break;
            }
            return rotationAngle;
        }
    }

    public class MovementMember  : NotifyPropertyChanged
    {
        private ReferenceAbility memberAbility;
        public ReferenceAbility MemberAbility
        {
            get
            {
                return memberAbility;
            }
            set
            {
                memberAbility = value;
                OnPropertyChanged("MemberAbility");
            }
        }

        private string memberName;
        public string MemberName
        {
            get
            {
                return memberName;
            }
            set
            {
                memberName = value;
                OnPropertyChanged("MemberName");
            }
        }

        private MovementDirection movementDirection;
        public MovementDirection MovementDirection
        {
            get
            {
                return movementDirection;
            }
            set
            {
                movementDirection = value;
                OnPropertyChanged("MovementDirection");
            }
        }
        [JsonIgnore]
        public Key AssociatedKey
        {
            get
            {
                Key key = Key.None;
                switch (MovementDirection)
                {
                    case MovementDirection.Forward:
                        key = Key.W;
                        break;
                    case MovementDirection.Backward:
                        key = Key.S;
                        break;
                    case MovementDirection.Left:
                        key = Key.A;
                        break;
                    case MovementDirection.Right:
                        key = Key.D;
                        break;
                    case MovementDirection.Upward:
                        key = Key.Space;
                        break;
                    case MovementDirection.Downward:
                        key = Key.Z;
                        break;
                    case MovementDirection.Still:
                        key = Key.X;
                        break;
                }
                return key;
            }
        }
    }

    public class MovementInstruction
    {
        public double Distance { get; set; }
        public IMemoryElementPosition Destination { get; set; }
        public int Ground { get; set; }
        public List<IMemoryElementPosition> IncrementalPositions { get; set; }
        public MovementDirection CurrentDirection { get; set; }
        public MovementDirection LastDirection { get; set; }
    }

    public class MovementProcessor
    {
        public static IMemoryElementPosition IncrementPosition(MovementDirection direction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }

        public static List<IMemoryElementPosition> CalculateIncrementalPositions(MovementDirection direction, Vector3 facing, Vector3 pitch)
        {
            return new List<IMemoryElementPosition>();
        }

        public static IMemoryElementPosition IncrementPositionFromInstruction(MovementInstruction instruction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }
        public static Vector3 CalculateNewFacingPitch(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public static bool ArePositionsBesideEachOther(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return false;
        }
        public static Vector3 CalculateTurn(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public static Vector3 CalculateCameraDistance(IMemoryElementPosition cameraPosition, IMemoryElementPosition characterPosition)
        {
            return new Vector3();
        }
    }
}
=======
﻿using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Enumerations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.Movements
{
    public class CharacterMovement: CharacterOption
    {
        private IntPtr hookID;

        [JsonConstructor]
        private CharacterMovement() { }

        public CharacterMovement(string name, Character owner = null)
        {
            this.Name = name;
            this.Character = owner;
        }

        private bool isActive;
        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        }
        private Character character;
        public Character Character
        {
            get
            {
                return character;
            }
            set
            {
                character = value;
                OnPropertyChanged("Character");
            }
        }

        private Movement movement;
        public Movement Movement
        {
            get
            {
                return movement;
            }
            set
            {
                movement = value;
                OnPropertyChanged("Movement");
            }
        }


        public void DeactivateMovement()
        {
            // Reset Active
            this.IsActive = false;
            // Enable Camera Control
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_ENABLE_CAMERA_FILENAME);
            // Unload Keyboard Hook
            KeyBoardHook.UnsetHook(hookID);
        }

        public void ActivateMovement()
        {
            // Deactivate Current Movement
            CharacterMovement activeCharacterMovement = this.Character.Movements.FirstOrDefault(cm => cm.IsActive);
            if (activeCharacterMovement != null)
                activeCharacterMovement.DeactivateMovement();
            // Set Active
            this.IsActive = true;
            // Disable Camera Control
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_DISABLE_CAMERA_FILENAME);
            // Load Keyboard Hook
            hookID = KeyBoardHook.SetHook(this.PlayMovementByKeyProc);
        }

        private IntPtr PlayMovementByKeyProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT keyboardDLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                Keys vkCode = (Keys)keyboardDLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null)
                        || Process.GetCurrentProcess().Id == wndProcId)
                    {
                        var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);

                        while(Keyboard.IsKeyDown(Key.W))
                        {

                        }
                        //if (this.Character.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
                        //{
                        //    this.Character.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode).Play();
                        //}
                    }
                    WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }
    }
    public class Movement : NotifyPropertyChanged
    {
        [JsonConstructor]
        private Movement() { }

        public Movement(string name)
        {
            this.Name = name;
            this.AddDefaultMemberAbilities();
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

        private ObservableCollection<MovementMember> movmementMembers;
        public ObservableCollection<MovementMember> MovementMembers
        {
            get
            {
                return movmementMembers;
            }
            set
            {
                movmementMembers = value;
                OnPropertyChanged("MovementMembers");
            }
        }

        public void Move(MovementDirection direction, double distance, Character target)
        {

        }

        public void MoveBasedOnKey(Key key, Character target)
        {
            MovementMember movementMember = this.GetMovementMemberByKey(key);
            if(movementMember != null && movementMember.MemberAbility != null)
            {
                movementMember.MemberAbility.Play(false, target);
            }
        }

        public void MoveToLocation(IMemoryElementPosition destination, Character target)
        {

        }

        public void Turn(MovementDirection direction, Character target)
        {

        }

        public void TurnBasedOnKey(string key, Character target)
        {

        }

        private void AddDefaultMemberAbilities()
        {
            if (this.MovementMembers == null || this.MovementMembers.Count == 0)
            {
                this.MovementMembers = new ObservableCollection<MovementMember>();

                MovementMember movementMemberLeft = new MovementMember { MovementDirection = MovementDirection.Left, MemberName = "Left" };
                movementMemberLeft.MemberAbility = new ReferenceAbility("Left", null);
                movementMemberLeft.MemberAbility.DisplayName = "Left";
                this.MovementMembers.Add(movementMemberLeft);

                MovementMember movementMemberRight = new MovementMember { MovementDirection = MovementDirection.Right, MemberName = "Right" };
                movementMemberRight.MemberAbility = new ReferenceAbility("Right", null);
                movementMemberRight.MemberAbility.DisplayName = "Right";
                this.MovementMembers.Add(movementMemberRight);

                MovementMember movementMemberForward = new MovementMember { MovementDirection = MovementDirection.Forward, MemberName = "Forward" };
                movementMemberForward.MemberAbility = new ReferenceAbility("Forward", null);
                movementMemberForward.MemberAbility.DisplayName = "Forward";
                this.MovementMembers.Add(movementMemberForward);

                MovementMember movementMemberBackward = new MovementMember { MovementDirection = MovementDirection.Backward, MemberName = "Back" };
                movementMemberBackward.MemberAbility = new ReferenceAbility("Back", null);
                movementMemberBackward.MemberAbility.DisplayName = "Back";
                this.MovementMembers.Add(movementMemberBackward);

                MovementMember movementMemberUpward = new MovementMember { MovementDirection = MovementDirection.Upward, MemberName = "Up" };
                movementMemberUpward.MemberAbility = new ReferenceAbility("Up", null);
                movementMemberUpward.MemberAbility.DisplayName = "Up";
                this.MovementMembers.Add(movementMemberUpward);

                MovementMember movementMemberDownward = new MovementMember { MovementDirection = MovementDirection.Downward, MemberName = "Down" };
                movementMemberDownward.MemberAbility = new ReferenceAbility("Down", null);
                movementMemberDownward.MemberAbility.DisplayName = "Down";
                this.MovementMembers.Add(movementMemberDownward);

                MovementMember movementMemberStill = new MovementMember { MovementDirection = MovementDirection.Still, MemberName = "Still" };
                movementMemberStill.MemberAbility = new ReferenceAbility("Still", null);
                movementMemberStill.MemberAbility.DisplayName = "Still";
                this.MovementMembers.Add(movementMemberStill);
            }
        }

        private MovementMember GetMovementMemberByKey(Key key)
        {
            MovementMember movementMember = null;
            switch(key)
            {
                case Key.W:
                    movementMember = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Forward);
                    break;
                case Key.A:
                    movementMember = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Left);
                    break;
                case Key.S:
                    movementMember = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Backward);
                    break;
                case Key.D:
                    movementMember = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Right);
                    break;
                case Key.Space:
                    movementMember = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Upward);
                    break;
                case Key.Z:
                    movementMember = this.MovementMembers.FirstOrDefault(mm => mm.MovementDirection == MovementDirection.Downward);
                    break;
            }
            return movementMember;
        }
    }

    public class MovementMember  : NotifyPropertyChanged
    {
        private ReferenceAbility memberAbility;
        public ReferenceAbility MemberAbility
        {
            get
            {
                return memberAbility;
            }
            set
            {
                memberAbility = value;
                OnPropertyChanged("MemberAbility");
            }
        }

        private string memberName;
        public string MemberName
        {
            get
            {
                return memberName;
            }
            set
            {
                memberName = value;
                OnPropertyChanged("MemberName");
            }
        }

        private MovementDirection movementDirection;
        public MovementDirection MovementDirection
        {
            get
            {
                return movementDirection;
            }
            set
            {
                movementDirection = value;
                OnPropertyChanged("MovementDirection");
            }
        }
    }

    public class MovementInstruction
    {
        public double Distance { get; set; }
        public IMemoryElementPosition Destination { get; set; }
        public int Ground { get; set; }
        public List<IMemoryElementPosition> IncrementalPositions { get; set; }
        public MovementDirection CurrentDirection { get; set; }
        public MovementDirection LastDirection { get; set; }
        public Character MovableCharacter { get; set; }
    }

    public class MovementProcessor
    {
        public IMemoryElementPosition IncrementPosition(MovementDirection direction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }

        public List<IMemoryElementPosition> CalculateIncrementalPositions(MovementDirection direction, Vector3 facing, Vector3 pitch)
        {
            return new List<IMemoryElementPosition>();
        }

        public IMemoryElementPosition IncrementPositionFromInstruction(MovementInstruction instruction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }
        public Vector3 CalculateNewFacingPitch(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public bool ArePositionsBesideEachOther(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return false;
        }
        public Vector3 CalculateTurn(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }
        public Vector3 CalculateCameraDistance(IMemoryElementPosition cameraPosition, IMemoryElementPosition characterPosition)
        {
            return new Vector3();
        }
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
