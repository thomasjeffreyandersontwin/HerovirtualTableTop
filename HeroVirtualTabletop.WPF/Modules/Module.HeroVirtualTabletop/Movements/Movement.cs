using Framework.WPF.Library;
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
