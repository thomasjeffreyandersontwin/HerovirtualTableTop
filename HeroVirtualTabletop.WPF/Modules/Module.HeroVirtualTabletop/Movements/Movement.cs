using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Enumerations;
using Module.Shared.Logging;
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
    public class CharacterMovement : CharacterOption
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
        [JsonIgnore]
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

        private Keys activationKey;
        public Keys ActivationKey
        {
            get
            {
                return activationKey;
            }
            set
            {
                activationKey = value;
                OnPropertyChanged("ActivationKey");
            }
        }

        private double movementSpeed;
        public double MovementSpeed
        {
            get
            {
                if (movementSpeed == 0)
                    movementSpeed = 1;
                return movementSpeed;
            }
            set
            {
                movementSpeed = value;
                OnPropertyChanged("MovementSpeed");
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
            this.Movement.StopMovement(this.Character);
            Helper.GlobalVariables_CharacterMovement = null;
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
            this.Character.MovementInstruction.IsMoving = false;
            this.Character.MovementInstruction.IsTurning = false;
            this.Character.MovementInstruction.IsMovingToDestination = false;
            this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            this.Character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None;
            this.Character.MovementInstruction.LastMovementDirection = MovementDirection.None;
            // Disable Camera Control
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_DISABLE_CAMERA_FILENAME);
            keyBindsGenerator.CompleteEvent();
            // Load Keyboard Hook
            hookID = KeyBoardHook.SetHook(this.PlayMovementByKeyProc);

            Helper.GlobalVariables_CharacterMovement = this;
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
                        if(this.Character.MovementInstruction != null)
                        {
                            var inputKey = KeyInterop.KeyFromVirtualKey((int)vkCode);
                            if (inputKey == Key.Escape)
                            {
                                DeactivateMovement();
                                this.Character.ActiveMovement = null;
                            }
                            else if(inputKey == Key.Left || inputKey == Key.Right || inputKey == Key.Up || inputKey == Key.Down)
                            {
                                MovementDirection turnDirection = GetTurnAxisDirectionFromKey(inputKey);
                                if (turnDirection != MovementDirection.None)
                                {
                                    this.Character.MovementInstruction.IsMoving = false;
                                    this.Character.MovementInstruction.IsMovingToDestination = false;
                                    this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                    this.Character.MovementInstruction.IsTurning = true;
                                    if (this.Character.MovementInstruction.CurrentRotationAxisDirection != turnDirection)
                                    {
                                        this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision as the character is changing the facing
                                        this.Character.MovementInstruction.LastMovementDirection = this.Character.MovementInstruction.CurrentMovementDirection;
                                        this.Character.MovementInstruction.CurrentMovementDirection = MovementDirection.None; // Reset key movement
                                        this.Character.MovementInstruction.CurrentRotationAxisDirection = turnDirection;
                                        this.Movement.StartMovment(this.Character);
                                    }
                                }
                            }
                            else
                            {
                                MovementDirection direction = GetMovementDirectionFromKey(inputKey);
                                if(direction != MovementDirection.None)
                                {
                                    this.Character.MovementInstruction.IsMoving = true;
                                    this.Character.MovementInstruction.IsMovingToDestination = false;
                                    this.Character.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                    this.Character.MovementInstruction.IsTurning = false;
                                    if (this.Character.MovementInstruction.CurrentMovementDirection != direction)
                                    {
                                        this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
                                        this.Character.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.None; // Reset turn
                                        this.Character.MovementInstruction.LastMovementDirection = this.Character.MovementInstruction.CurrentMovementDirection;
                                        this.Character.MovementInstruction.CurrentMovementDirection = direction;
                                        this.Movement.StartMovment(this.Character);
                                    }
                                }
                                
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
            MovementDirection movementDirection = MovementDirection.None;
            switch (key)
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
                case Key.X:
                    movementDirection = MovementDirection.Still;
                    break;

            }
            return movementDirection;
        }
        private MovementDirection GetTurnAxisDirectionFromKey(Key key)
        {
            MovementDirection turnAxisDirection = MovementDirection.None;
            bool modifierKeyPresent = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            switch (key)
            {
                case Key.Up: 
                    turnAxisDirection = MovementDirection.Right;
                    break;
                case Key.Down:
                    turnAxisDirection = MovementDirection.Left;
                    break;
                case Key.Left:
                    if (modifierKeyPresent)
                        turnAxisDirection = MovementDirection.Backward;
                    else
                        turnAxisDirection = MovementDirection.Downward;
                    break;
                case Key.Right:
                    if (modifierKeyPresent)
                        turnAxisDirection = MovementDirection.Forward;
                    else
                        turnAxisDirection = MovementDirection.Upward;
                    break;
            }
            return turnAxisDirection;
        }
    }
    public class Movement : NotifyPropertyChanged
    {
        private Dictionary<Character, System.Threading.Timer> characterMovementTimerDictionary;
        //private System.Threading.Timer timer;
        [JsonConstructor]
        private Movement() { }
        private ILogManager logManager = new FileLogManager(typeof(Movement));
        public Movement(string name)
        {
            this.Name = name;
            this.characterMovementTimerDictionary = new Dictionary<Character, System.Threading.Timer>();
            this.AddDefaultMemberAbilities();
        }

        //public bool IsPlaying { get; set; }

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

        public void Move(Character target)
        {
            double rotationAngle = GetRotationAngle(target.MovementInstruction.CurrentMovementDirection);
            Vector3 facingToDest = new Vector3();
            Vector3 directionVector = new Vector3();
            if (target.MovementInstruction.IsMovingToDestination)
            {
                if (target.MovementInstruction.IsDestinationPointAdjusted)
                {
                    var distance = Vector3.Distance(target.CurrentPositionVector, target.MovementInstruction.DestinationVector);
                    var adjustmentDest = target.MovementInstruction.DestinationPointHeightAdjustment < 1 ? 10 : target.MovementInstruction.DestinationPointHeightAdjustment < 3 ? 20 : 30;
                    if (distance < adjustmentDest)
                    {
                        target.MovementInstruction.DestinationVector = target.MovementInstruction.OriginalDestinationVector;
                        target.MovementInstruction.IsDestinationPointAdjusted = false;
                    }
                }
                facingToDest = target.MovementInstruction.DestinationVector - target.CurrentPositionVector;
                facingToDest.Normalize();
                directionVector = GetDirectionVector(0, target.MovementInstruction.CurrentMovementDirection, facingToDest);
            }
            else
            {
                facingToDest = new Vector3(target.CurrentFacingVector.X, 0, target.CurrentFacingVector.Z); // disable vertical movement
                facingToDest.Normalize();
                directionVector = GetDirectionVector(rotationAngle, target.MovementInstruction.CurrentMovementDirection, facingToDest);
            }
            target.MovementInstruction.CurrentDirectionVector = directionVector;
            if(directionVector.X != float.NaN && directionVector.Y != float.NaN && directionVector.Z != float.NaN)
            {
                Vector3 allowableDestinationVector = GetAllowableDestinationVector(target, directionVector);
                target.CurrentPositionVector = allowableDestinationVector;
            }
        }

        public void MoveBack(Character target, Vector3 lookatVector, Vector3 destinationVector)
        {
            if (target.MovementInstruction == null)
                target.MovementInstruction = new MovementInstruction();

            SetFacingToDestination(target, lookatVector);

            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
            target.MovementInstruction.IsMovingToDestination = true;
            target.MovementInstruction.IsTurning = target.MovementInstruction.IsMoving = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.DestinationVector = destinationVector;
            target.MovementInstruction.OriginalDestinationVector = destinationVector;
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.StopOnCollision = true;
            target.MovementInstruction.IsCollisionAhead = false;
            target.MovementInstruction.IsDestinationPointAdjusted = false;
            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
            target.MovementInstruction.MovmementDirectionToUseForDestinationMove = MovementDirection.Backward;
            this.StartMovment(target);
        }

        public void Move(Character target, Vector3 destinationVector)
        {
            if (target.MovementInstruction == null)
                target.MovementInstruction = new MovementInstruction();

            SetFacingToDestination(target, destinationVector);
            //// Disable Camera
            //KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            //keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_DISABLE_CAMERA_FILENAME);
            //keyBindsGenerator.CompleteEvent();
            // Start Movement
            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
            target.MovementInstruction.IsMovingToDestination = true;
            target.MovementInstruction.IsTurning = target.MovementInstruction.IsMoving = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.DestinationVector = destinationVector;
            target.MovementInstruction.OriginalDestinationVector = destinationVector;
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.StopOnCollision = false;
            target.MovementInstruction.IsCollisionAhead = false;
            target.MovementInstruction.IsDestinationPointAdjusted = false;
            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
            target.MovementInstruction.MovmementDirectionToUseForDestinationMove = MovementDirection.Forward;
            this.StartMovment(target);
        }

        private void SetFacingToDestination(Character target, Vector3 destinationVector)
        {
            Vector3 currentPositionVector = target.CurrentPositionVector;
            Matrix newRotationMatrix = Matrix.CreateLookAt(currentPositionVector, destinationVector, target.CurrentModelMatrix.Up);
            newRotationMatrix.M11 *= -1;
            newRotationMatrix.M33 *= -1;
            //var newModelMatrix = new Matrix
            //{
            //    M11 = newRotationMatrix.M11,
            //    M12 = newRotationMatrix.M12,
            //    M13 = newRotationMatrix.M13,
            //    M14 = newRotationMatrix.M14,
            //    M21 = newRotationMatrix.M21,
            //    M22 = newRotationMatrix.M22,
            //    M23 = newRotationMatrix.M23,
            //    M24 = newRotationMatrix.M24,
            //    M31 = newRotationMatrix.M31,
            //    M32 = newRotationMatrix.M32,
            //    M33 = newRotationMatrix.M33,
            //    M34 = newRotationMatrix.M34,
            //    M41 = target.CurrentModelMatrix.M41,
            //    M42 = target.CurrentModelMatrix.M42,
            //    M43 = target.CurrentModelMatrix.M43,
            //    M44 = target.CurrentModelMatrix.M44
            //};
            #region old
            var newModelMatrix = new Matrix
            {
                M11 = newRotationMatrix.M11,
                M12 = target.CurrentModelMatrix.M12,
                M13 = newRotationMatrix.M13,
                M14 = target.CurrentModelMatrix.M14,
                M21 = target.CurrentModelMatrix.M21,
                M22 = target.CurrentModelMatrix.M22,
                M23 = target.CurrentModelMatrix.M23,
                M24 = target.CurrentModelMatrix.M24,
                M31 = newRotationMatrix.M31,
                M32 = target.CurrentModelMatrix.M32,
                M33 = newRotationMatrix.M33,
                M34 = target.CurrentModelMatrix.M34,
                M41 = target.CurrentModelMatrix.M41,
                M42 = target.CurrentModelMatrix.M42,
                M43 = target.CurrentModelMatrix.M43,
                M44 = target.CurrentModelMatrix.M44
            };
            #endregion
            //target.CurrentModelMatrix = newModelMatrix;
            // Turn to destination, figure out angle
            Vector3 targetForwardVector = newModelMatrix.Forward; 
            Vector3 currentForwardVector = target.CurrentModelMatrix.Forward;
            bool isClockwiseTurn;
            float origAngle = MathHelper.ToDegrees(Get2DAngleBetweenVectors(currentForwardVector, targetForwardVector, out isClockwiseTurn));
            var angle = origAngle;

            if (isClockwiseTurn)
                target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.Upward;
            else
            {
                target.MovementInstruction.CurrentRotationAxisDirection = MovementDirection.Downward;
            }

            bool turnCompleted = false;
            while (!turnCompleted)
            {
                if (angle > 2)
                {
                    Turn(target, 2);
                    angle -= 2;
                }
                else
                {
                    Turn(target, angle);
                    turnCompleted = true;
                }
                Thread.Sleep(5);
            }
        }

        public void Turn(Character target, float angle = 5)
        {
            Vector3 currentPositionVector = target.CurrentModelMatrix.Translation;
            Vector3 currentForwardVector = target.CurrentModelMatrix.Forward;
            Vector3 currentBackwardVector = target.CurrentModelMatrix.Backward;
            Vector3 currentRightVector = target.CurrentModelMatrix.Right;
            Vector3 currentLeftVector = target.CurrentModelMatrix.Left;
            Vector3 currentUpVector = target.CurrentModelMatrix.Up;
            Vector3 currentDownVector = target.CurrentModelMatrix.Down;
            Matrix rotatedMatrix = new Matrix();
            //var rotMatrix = Matrix.CreateFromAxisAngle(currentFacing, (float)GetRadianAngle(10));
            //target.CurrentModelMatrix *= rotMatrix;

            //target.CurrentPositionVector = currentPosition;
            //Vector3 targetFacing = Vector3.Transform(currentFacing, Matrix.CreateRotationY((float)GetRadianAngle(10)));
            //target.CurrentFacingVector = currentPosition + targetFacing;
            switch(target.MovementInstruction.CurrentRotationAxisDirection)
            {
                case MovementDirection.Upward: // Rotate against Up Axis, e.g. Y axis for a vertically aligned model
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentUpVector, (float)GetRadianAngle(angle));
                    break;
                case MovementDirection.Downward: // Rotate against Down Axis, e.g. -Y axis for a vertically aligned model
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentDownVector, (float)GetRadianAngle(angle));
                    break;
                case MovementDirection.Right: // Rotate against Right Axis, e.g. X axis for a vertically aligned model will tilt the model forward
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentRightVector, (float)GetRadianAngle(angle));
                    break;
                case MovementDirection.Left: // Rotate against Left Axis, e.g. -X axis for a vertically aligned model will tilt the model backward
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentLeftVector, (float)GetRadianAngle(angle));
                    break;
                case MovementDirection.Forward: // Rotate against Forward Axis, e.g. Z axis for a vertically aligned model will tilt the model on right side
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentForwardVector, (float)GetRadianAngle(angle));
                    break;
                case MovementDirection.Backward: // Rotate against Backward Axis, e.g. -Z axis for a vertically aligned model will tilt the model on left side
                    rotatedMatrix = Matrix.CreateFromAxisAngle(currentBackwardVector, (float)GetRadianAngle(angle));
                    break;
            }

            target.CurrentModelMatrix *= rotatedMatrix; // Apply rotation
            target.CurrentPositionVector = currentPositionVector; // Keep position intact;
        }

        private float GetMovementUnit(Character target)
        {
            double movementSpeed = 1;
            var activeCharacterMovement = target.Movements.FirstOrDefault(cm => cm.IsActive && cm.Name == this.Name); // See if this character has speed defined for this movement
            if (activeCharacterMovement != null) 
                movementSpeed = activeCharacterMovement.MovementSpeed;
            else // use speed from default character
            {
                activeCharacterMovement = Helper.GlobalMovements.FirstOrDefault(cm => cm.Name == this.Name); 
                if (activeCharacterMovement != null)
                    movementSpeed = activeCharacterMovement.MovementSpeed;
            }
            
            // Distance is updated once in every 33 milliseconds approximately - i.e. 30 times in 1 sec
            // So, normally he can travel 30 * 0.5 = 15 units per second if unit is 0.5
            float unit = (float)movementSpeed * 0.5f; 
            if(target.MovementInstruction.IsMovingToDestination)
            {
                var distanceFromDestination = Vector3.Distance(target.MovementInstruction.OriginalDestinationVector, target.CurrentPositionVector);
                if(distanceFromDestination < 50) // 1 sec
                {
                    unit = (float)distanceFromDestination / 30;
                }
                else if(distanceFromDestination < 150) // 2 sec
                {
                    unit = (float)distanceFromDestination / 30 / 2;
                }
                else // 3 sec
                {
                    unit = (float)distanceFromDestination / 30 / 3;
                }

                unit *= (float)movementSpeed / 2; // Dividing by two to reduce the speed as high speeds tend to cause more errors
            }
            return unit;
        }

        private Vector3 GetCollisionVector(Vector3 sourceVector, Vector3 destVector)
        {
            float distance = Vector3.Distance(sourceVector, destVector);
            Vector3 collisionVector = new Vector3(0, 0, 0);
            int numRetry = 5; // try 5 times
            while (numRetry > 0)
            {
                try
                {
                    var collisionInfo = IconInteractionUtility.GetCollisionInfo(sourceVector.X, sourceVector.Y, sourceVector.Z, destVector.X, destVector.Y, destVector.Z);
                    collisionVector = Helper.GetCollisionVector(collisionInfo);
                    float collisionDistance = Vector3.Distance(sourceVector, collisionVector);
                    if (!HasCollision(collisionVector) || collisionDistance <= distance) // proper collision
                        break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(500);
                    numRetry--;
                }
            }
            return collisionVector;
        }

        private Vector3 GetAllowableDestinationVector(Character target, Vector3 directionVector)
        {
            // TODO: need to take into account the pitch yaw roll etc. in future
            Vector3 currentPositionVector = target.CurrentPositionVector;
            Vector3 destinationVectorNext = GetDestinationVector(directionVector, target.MovementInstruction.MovementUnit, target);
            Vector3 destinationVectorFar = GetDestinationVector(directionVector, 20f, target);
            Vector3 collisionVector = new Vector3();
            MovementDirection direction = target.MovementInstruction.CurrentMovementDirection;
            float distanceFromDest = Vector3.Distance(currentPositionVector, destinationVectorNext);
            float distanceFromCollisionPoint = 0f;
            Vector3 collisionBodyPoint = new Vector3();
            bool needToCheckAdjustment = false;
            //logManager.Info(string.Format("Current position: {0}, {1}, {2}", currentPositionVector.X, currentPositionVector.Y, currentPositionVector.Z));
            if (target.MovementInstruction.LastCollisionFreePointInCurrentDirection.X == -10000f
                && target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y == -10000f
                && target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Z == -10000f) // Need to recalculate next collision point
            {
                collisionVector = CalculateNextCollisionPoint(target, destinationVectorFar);
                logManager.Info(string.Format("recalculating collision at {0}", DateTime.Now.ToString("HH:mm:ss.fff")));
                if (HasCollision(collisionVector)) // Collision ahead - can only move upto the collision point
                {
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = collisionVector;
                    target.MovementInstruction.IsCollisionAhead = true;
                }
                else // No collision in 20 units, so free to move next 20 units
                {
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = destinationVectorFar;
                    target.MovementInstruction.IsCollisionAhead = false;
                }
            }
            //else
            {
                //logManager.Info(string.Format("CollisionPoint: {0}, {1}, {2}", target.MovementInstruction.LastCollisionFreePointInCurrentDirection.X, target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y, target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Z));
                collisionBodyPoint = Vector3.Add(currentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);
                //logManager.Info(string.Format("CollisionBodyPoint: {0}, {1}, {2}", collisionBodyPoint.X, collisionBodyPoint.Y, collisionBodyPoint.Z));
                distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);

                //logManager.Info("Distance from collision: " + distanceFromCollisionPoint.ToString());
                if (distanceFromDest > distanceFromCollisionPoint || distanceFromCollisionPoint < 1)// Collision point nearer, so can't move to destination without checking first
                {
                    if (target.MovementInstruction.IsCollisionAhead) // the LastCollisionFreePointInCurrentDirection is a collision point
                    {
                        bool canAvoidCollision;
                        Vector3 nextTravelPointToAvoidCollision = GetNextTravelPointToAvoidCollision(target, out canAvoidCollision);
                        collisionVector = nextTravelPointToAvoidCollision;
                        target.MovementInstruction.IsInCollision = !canAvoidCollision;
                        if (canAvoidCollision)
                        {
                            destinationVectorNext = nextTravelPointToAvoidCollision;
                            //target.MovementInstruction.CollisionAhead = false;
                            //target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // we adjusted position, so force recalculation of collision at next iteration
                        }
                        else
                        {
                            target.MovementInstruction.IsInCollision = true;
                        }
                        ////logManager.Info("Collision ahead true");
                        //if (distanceFromDest > distanceFromCollisionPoint)
                        //    collisionVector = currentPositionVector; // stay where you are
                        //else
                        //    collisionVector = destinationVectorNext; // just go to next point, but no further
                        //target.MovementInstruction.IsInCollision = true;
                    }
                    else // the LastCollisionFreePointInCurrentDirection is just the last calculated point, so we need to recalculate the collisions in the current direction
                    {
                        //logManager.Info("Collision ahead false, recalculating");
                        collisionVector = CalculateNextCollisionPoint(target, destinationVectorFar);
                        //logManager.Info(string.Format("Recalculated Collision Point: {0}, {1}, {2}", collisionVector.X, collisionVector.Y, collisionVector.Z));
                        if (HasCollision(collisionVector)) // Collision ahead - can only move upto the collision point
                        {
                            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = collisionVector;
                            target.MovementInstruction.IsCollisionAhead = true;
                        }
                        else // No collision in 20 units, so free to move next 20 units
                        {
                            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = destinationVectorFar;
                            target.MovementInstruction.IsCollisionAhead = false;
                        }
                    }
                }
                else
                    needToCheckAdjustment = true;
            }

            
            // Enable climbing up
            //if (vectorsWithCollisionAllExceptBottom.Count == 0) // only bottom portion collision, so can climb
            //{
            //    destinationVectorNext.Y += 0.25f;
            //}
            Vector3 allowableDestVector = new Vector3();
            // Now we should move to the next destination point or the LastCollisionFreePointInCurrentDirection - whichever is nearer
            collisionBodyPoint = Vector3.Add(currentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);
            distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);
            if ((distanceFromDest > distanceFromCollisionPoint || distanceFromCollisionPoint < 1) && target.MovementInstruction.IsInCollision)
            {
                //var targetPosition = target.MovementInstruction.LastCollisionFreePointInCurrentDirection;
                //if (distanceFromDest > distanceFromCollisionPoint)
                //    allowableDestVector = currentPositionVector;
                //else
                //    allowableDestVector = destinationVectorNext;
                allowableDestVector = collisionVector;
                //logManager.Info("Collision detected and stopping");
            }
            else
            {
                bool canAvoidCollision;
                if (target.MovementInstruction.IsPositionAdjustedToAvoidCollision && needToCheckAdjustment)
                    allowableDestVector = GetNextTravelPointToAvoidCollision(target, out canAvoidCollision);
                else
                    allowableDestVector = new Vector3(destinationVectorNext.X, destinationVectorNext.Y, destinationVectorNext.Z);
                //logManager.Info("No collision, carrying on");
            }
            logManager.Info(string.Format("On {0}, {1}, {2} At {3}", allowableDestVector.X, allowableDestVector.Y, allowableDestVector.Z, DateTime.Now.ToString("HH:mm:ss.fff")));
            //if (!HasCollision(collisionVector)) // No collision - move to destination
            //    allowableDestVector = new Vector3(destinationVectorNext.X, destinationVectorNext.Y, destinationVectorNext.Z);
            //else // Move to collision point
            //{
            //    allowableDestVector = new Vector3(collisionVector.X, collisionVector.Y, collisionVector.Z);
            //    target.MovementInstruction.IsInCollision = true;
            //}

            //// Enable gravity if applicable
            //if (allowableDestVector.Y > 0.5 && (direction != MovementDirection.Upward && direction != MovementDirection.Downward))
            //{
            //    Vector3 collisionVectorGround = GetCollisionVector(allowableDestVector, new Vector3(allowableDestVector.X, 0f, allowableDestVector.Z));
            //    if (collisionVectorGround.Y >= 0f && collisionVectorGround.Y < allowableDestVector.Y)
            //        allowableDestVector.Y = collisionVectorGround.Y;
            //}
            //// Preventing going to absurd locations
            //var finalDistance = Vector3.Distance(currentPositionVector, allowableDestVector);
            //if (finalDistance > 5f)
            //    allowableDestVector = currentPositionVector;
            //// Preventing from character's feet going under ground
            //if (allowableDestVector.Y < 0.25f && (direction != MovementDirection.Upward && direction != MovementDirection.Downward))
            //    allowableDestVector.Y = 0.25f;

            return allowableDestVector;
        }

        private Vector3 GetNextTravelPointToAvoidCollision(Character target, out bool canAvoidCollision)
        {
            Vector3 nextTravelPoint = new Vector3();

            Vector3 destinationVectorNext = GetDestinationVector(target.MovementInstruction.CurrentDirectionVector, target.MovementInstruction.MovementUnit, target);
            Vector3 collisionBodyPoint = Vector3.Add(target.CurrentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);
            float distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);
            float distanceFromDest = Vector3.Distance(target.CurrentPositionVector, destinationVectorNext);
            //if (distanceFromDest > distanceFromCollisionPoint)
            //    nextTravelPoint = target.CurrentPositionVector;
            //else
                nextTravelPoint = destinationVectorNext;

            canAvoidCollision = false;

            if (target.MovementInstruction.IsPositionAdjustedToAvoidCollision)
            {
                nextTravelPoint.Y = target.CurrentPositionVector.Y; // maintain same Y till collision point is passed
                canAvoidCollision = true;
                var collDistance = Vector3.Distance(nextTravelPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);
                if (collDistance > target.MovementInstruction.DistanceFromCollisionFreePoint)
                {
                    // collision point passed, so need to re-calculate
                    target.MovementInstruction.IsCollisionAhead = false;
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // force recalculation of collision at next iteration
                    target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
                }
                target.MovementInstruction.DistanceFromCollisionFreePoint = collDistance;
            }
            else
            {
                // Check if Collision is bottom, then we might have a chance to avoid collision by adjusting position
                BodyPart bodyPart = GetBodyPartFromOffsetVector(target.MovementInstruction.CharacterBodyCollisionOffsetVector);
                if (bodyPart == BodyPart.Bottom || bodyPart == BodyPart.BottomSemiMiddle)
                {
                    // Check if other collisions are also present at same or less distance
                    Dictionary<BodyPart, bool> bodyPartMap = new Dictionary<BodyPart, bool>();
                    bodyPartMap.Add(BodyPart.Bottom, false);
                    bodyPartMap.Add(BodyPart.BottomSemiMiddle, true);
                    bodyPartMap.Add(BodyPart.BottomMiddle, true);
                    bodyPartMap.Add(BodyPart.Middle, true);
                    bodyPartMap.Add(BodyPart.TopMiddle, true);
                    bodyPartMap.Add(BodyPart.Top, true);

                    Vector3 destinationVector = GetDestinationVector(target.MovementInstruction.CurrentDirectionVector, 5f, target);
                    Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = GetCollisionPointsForBodyParts(target, destinationVector, bodyPartMap);
                    logManager.Info(string.Format("calculate collision at {0}", DateTime.Now.ToString("HH:mm:ss.fff")));
                    bool hasCollision = false;
                    foreach (BodyPart key in bodyPartCollisionMap.Keys)
                    {
                        if (bodyPartCollisionMap[key] != null)
                        {
                            hasCollision = true;
                            break;
                        }

                    }
                    if (hasCollision)
                    {
                        // Check if Collision is only bottom semi middle and no other parts, then we can still adjust
                        if (bodyPartCollisionMap[BodyPart.BottomSemiMiddle] != null && bodyPartCollisionMap[BodyPart.BottomMiddle] == null && bodyPartCollisionMap[BodyPart.Middle] == null
                            && bodyPartCollisionMap[BodyPart.TopMiddle] == null && bodyPartCollisionMap[BodyPart.Top] == null)
                        {
                            // only bottom semi middle collision, so adjust position
                            canAvoidCollision = true;
                            if (target.CurrentPositionVector.Y <= destinationVectorNext.Y
                                && destinationVectorNext.Y <= target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y) // We're basically travelling horizontal or upwards, so increase Y 
                            {
                                nextTravelPoint.Y += 0.25f;
                            }
                            else // we're going downwards
                            {
                                nextTravelPoint = destinationVectorNext;
                                if (target.CurrentPositionVector.Y > destinationVectorNext.Y)
                                    nextTravelPoint.Y = target.CurrentPositionVector.Y;
                                //if (destinationVectorNext.Y > target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y)
                                //    nextTravelPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y + 0.1f;
                            }
                            var destVector = target.MovementInstruction.DestinationVector;
                            destVector.Y += 0.25f;
                            target.MovementInstruction.DestinationPointHeightAdjustment += 0.25f;
                            target.MovementInstruction.DestinationVector = destVector;
                            target.MovementInstruction.IsDestinationPointAdjusted = true;
                            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = true;
                        }
                        else
                        {
                            // more collisions exist
                            // check if all the collision distances are significantly larger than current collision distance
                            bool upperCollisionsFar = true;
                            foreach (BodyPart key in bodyPartCollisionMap.Keys)
                            {
                                if (bodyPartCollisionMap[key] != null)
                                {
                                    CollisionInfo collisionInfo = bodyPartCollisionMap[key];
                                    if (distanceFromCollisionPoint + 0.25f > collisionInfo.CollisionDistance) // check if the collisions are further from current collision so that it is worth moving up 
                                    {
                                        upperCollisionsFar = false;
                                        break;
                                    }
                                }
                            }

                            if (upperCollisionsFar)
                            {
                                // we can try to avoid collision by moving up a bit
                                canAvoidCollision = true;
                                if (target.CurrentPositionVector.Y <= destinationVectorNext.Y
                                    && destinationVectorNext.Y <= target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y) // We're basically travelling horizontal or upwards, so increase Y 
                                {
                                    if (bodyPartCollisionMap[BodyPart.BottomSemiMiddle] != null) // bottom semi collision present, so lift up .75 units
                                        nextTravelPoint.Y += 0.75f;
                                    else
                                        nextTravelPoint.Y += 0.25f; // bottom collision only, so lift up .25 units
                                    
                                }
                                else // we're going downwards
                                {
                                    nextTravelPoint = destinationVectorNext;
                                    if (target.CurrentPositionVector.Y > destinationVectorNext.Y)
                                        nextTravelPoint.Y = target.CurrentPositionVector.Y;
                                    //if (destinationVectorNext.Y > target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y)
                                    //    nextTravelPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y + 0.1f;
                                }
                                var destVector = target.MovementInstruction.DestinationVector;
                                destVector.Y += 0.25f;
                                target.MovementInstruction.DestinationPointHeightAdjustment += 0.25f;
                                target.MovementInstruction.DestinationVector = destVector;
                                target.MovementInstruction.IsDestinationPointAdjusted = true;
                                target.MovementInstruction.IsPositionAdjustedToAvoidCollision = true;
                            }
                            else
                            {
                                // no way - definite collision

                            }
                        }
                    }
                    else
                    {
                        // We can avoid collision at the bottom by adjusting position
                        canAvoidCollision = true;
                        if (target.CurrentPositionVector.Y <= destinationVectorNext.Y
                            && destinationVectorNext.Y <= target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y) // We're basically travelling horizontal or upwards, so increase Y 
                        {
                            nextTravelPoint.Y += 0.25f;
                        }
                        else // we're going downwards
                        {
                            nextTravelPoint = destinationVectorNext;
                            if (target.CurrentPositionVector.Y > destinationVectorNext.Y)
                                nextTravelPoint.Y = target.CurrentPositionVector.Y;
                            //if (destinationVectorNext.Y > target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y)
                            //    nextTravelPoint.Y = target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y + 0.1f;
                        }

                        var destVector = target.MovementInstruction.DestinationVector;
                        destVector.Y += 0.25f;
                        target.MovementInstruction.DestinationPointHeightAdjustment += 0.25f;
                        target.MovementInstruction.DestinationVector = destVector;
                        target.MovementInstruction.IsDestinationPointAdjusted = true;
                        target.MovementInstruction.IsPositionAdjustedToAvoidCollision = true;
                    }
                }
            }
            
            return nextTravelPoint;
        }

        private Dictionary<BodyPart, CollisionInfo> GetCollisionPointsForBodyParts(Character target, Vector3 destinationVector, Dictionary<BodyPart, bool> bodyPartMap)
        {
            Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = new Dictionary<BodyPart, CollisionInfo>();
            foreach (var bp in Enum.GetValues(typeof(BodyPart)))
                bodyPartCollisionMap.Add((BodyPart)bp, null);

            Vector3 currentPositionVector = target.CurrentPositionVector;

            Vector3 topOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Top);
            Vector3 currentTopVector = new Vector3(currentPositionVector.X + topOffsetVector.X, currentPositionVector.Y + topOffsetVector.Y, currentPositionVector.Z + topOffsetVector.Z);
            Vector3 destinationTopVector = new Vector3(destinationVector.X + topOffsetVector.X, destinationVector.Y + topOffsetVector.Y, destinationVector.Z + topOffsetVector.Z);
            Vector3 collisionVectorTop = GetCollisionVector(currentTopVector, destinationTopVector);

            Thread.Sleep(5);

            Vector3 topMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.TopMiddle);
            Vector3 currentTopMiddleVector = new Vector3(currentPositionVector.X + topMiddleOffsetVector.X, currentPositionVector.Y + topMiddleOffsetVector.Y, currentPositionVector.Z + topMiddleOffsetVector.Z);
            Vector3 destinationTopMiddleVector = new Vector3(destinationVector.X + topMiddleOffsetVector.X, destinationVector.Y + topMiddleOffsetVector.Y, destinationVector.Z + topMiddleOffsetVector.Z);
            Vector3 collisionVectorTopMiddle = GetCollisionVector(currentTopMiddleVector, destinationTopMiddleVector);

            Thread.Sleep(5);

            Vector3 middleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Middle);
            Vector3 currentMiddleVector = new Vector3(currentPositionVector.X + middleOffsetVector.X, currentPositionVector.Y + middleOffsetVector.Y, currentPositionVector.Z + middleOffsetVector.Z);
            Vector3 destinationMiddleVector = new Vector3(destinationVector.X + middleOffsetVector.X, destinationVector.Y + middleOffsetVector.Y, destinationVector.Z + middleOffsetVector.Z);
            Vector3 collisionVectorMiddle = GetCollisionVector(currentMiddleVector, destinationMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomMiddle);
            Vector3 currentBottomMiddleVector = new Vector3(currentPositionVector.X + bottomMiddleOffsetVector.X, currentPositionVector.Y + bottomMiddleOffsetVector.Y, currentPositionVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 destinationBottomMiddleVector = new Vector3(destinationVector.X + bottomMiddleOffsetVector.X, destinationVector.Y + bottomMiddleOffsetVector.Y, destinationVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomMiddle = GetCollisionVector(currentBottomMiddleVector, destinationBottomMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomSemiMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomSemiMiddle);
            Vector3 currentBottomSemiMiddleVector = new Vector3(currentPositionVector.X + bottomSemiMiddleOffsetVector.X, currentPositionVector.Y + bottomSemiMiddleOffsetVector.Y, currentPositionVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 destinationBottomSemiMiddleVector = new Vector3(destinationVector.X + bottomSemiMiddleOffsetVector.X, destinationVector.Y + bottomSemiMiddleOffsetVector.Y, destinationVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomSemiMiddle = GetCollisionVector(currentBottomSemiMiddleVector, destinationBottomSemiMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Bottom);
            Vector3 currentBottomVector = new Vector3(currentPositionVector.X + bottomOffsetVector.X, currentPositionVector.Y + bottomOffsetVector.Y, currentPositionVector.Z + bottomOffsetVector.Z);
            Vector3 destinationBottomVector = new Vector3(destinationVector.X + bottomOffsetVector.X, destinationVector.Y + bottomOffsetVector.Y, destinationVector.Z + bottomOffsetVector.Z);
            Vector3 collisionVectorBottom = GetCollisionVector(currentBottomVector, destinationBottomVector);

            float distanceFromCollisionPoint = 10000f;
            if (HasCollision(collisionVectorBottom) && bodyPartMap[BodyPart.Bottom])
            {
                float collDist = Vector3.Distance(currentBottomVector, collisionVectorBottom);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Bottom] = new CollisionInfo { 
                        BodyCollisionOffsetVector = bottomOffsetVector, 
                        CollisionBodyPart = BodyPart.Bottom, 
                        CollisionPoint = collisionVectorBottom,
                        CollisionDistance = collDist
                    };
                }

            }
            else
            {
                bodyPartMap[BodyPart.Bottom] = false;
            }
            if (HasCollision(collisionVectorBottomSemiMiddle) && bodyPartMap[BodyPart.BottomSemiMiddle])
            {
                float collDist = Vector3.Distance(currentBottomSemiMiddleVector, collisionVectorBottomSemiMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.BottomSemiMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomSemiMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.BottomSemiMiddle,
                        CollisionPoint = collisionVectorBottomSemiMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.BottomSemiMiddle] = false;
            }
            if (HasCollision(collisionVectorBottomMiddle) && bodyPartMap[BodyPart.BottomMiddle])
            {
                float collDist = Vector3.Distance(currentBottomMiddleVector, collisionVectorBottomMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.BottomMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.BottomMiddle,
                        CollisionPoint = collisionVectorBottomMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.BottomMiddle] = false;
            }
            if (HasCollision(collisionVectorMiddle) && bodyPartMap[BodyPart.Middle])
            {
                float collDist = Vector3.Distance(currentMiddleVector, collisionVectorMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Middle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = middleOffsetVector,
                        CollisionBodyPart = BodyPart.Middle,
                        CollisionPoint = collisionVectorMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Middle] = false;
            }
            if (HasCollision(collisionVectorTopMiddle) && bodyPartMap[BodyPart.TopMiddle])
            {
                float collDist = Vector3.Distance(currentTopMiddleVector, collisionVectorTopMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.TopMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.TopMiddle,
                        CollisionPoint = collisionVectorTopMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.TopMiddle] = false;
            }
            if (HasCollision(collisionVectorTop) && bodyPartMap[BodyPart.Top])
            {
                float collDist = Vector3.Distance(currentTopVector, collisionVectorTop);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Top] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topOffsetVector,
                        CollisionBodyPart = BodyPart.Top,
                        CollisionPoint = collisionVectorTop,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Top] = false;
            }

            return bodyPartCollisionMap;
        }

        private Vector3 CalculateNextCollisionPoint(Character target, Vector3 destinationVector)
        {
            Vector3 collisionVector = new Vector3();
            bool foundCollision = false;

            Vector3 currentPositionVector = target.CurrentPositionVector;
            MovementDirection direction = target.MovementInstruction.CurrentMovementDirection;

            Vector3 topOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Top);
            Vector3 currentTopVector = new Vector3(currentPositionVector.X + topOffsetVector.X, currentPositionVector.Y + topOffsetVector.Y, currentPositionVector.Z + topOffsetVector.Z);
            Vector3 destinationTopVector = new Vector3(destinationVector.X + topOffsetVector.X, destinationVector.Y + topOffsetVector.Y, destinationVector.Z + topOffsetVector.Z);
            Vector3 collisionVectorTop = GetCollisionVector(currentTopVector, destinationTopVector);

            Thread.Sleep(10);

            Vector3 topMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.TopMiddle);
            Vector3 currentTopMiddleVector = new Vector3(currentPositionVector.X + topMiddleOffsetVector.X, currentPositionVector.Y + topMiddleOffsetVector.Y, currentPositionVector.Z + topMiddleOffsetVector.Z);
            Vector3 destinationTopMiddleVector = new Vector3(destinationVector.X + topMiddleOffsetVector.X, destinationVector.Y + topMiddleOffsetVector.Y, destinationVector.Z + topMiddleOffsetVector.Z);
            Vector3 collisionVectorTopMiddle = GetCollisionVector(currentTopMiddleVector, destinationTopMiddleVector);
            
            Thread.Sleep(10);

            Vector3 middleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Middle);
            Vector3 currentMiddleVector = new Vector3(currentPositionVector.X + middleOffsetVector.X, currentPositionVector.Y + middleOffsetVector.Y, currentPositionVector.Z + middleOffsetVector.Z);
            Vector3 destinationMiddleVector = new Vector3(destinationVector.X + middleOffsetVector.X, destinationVector.Y + middleOffsetVector.Y, destinationVector.Z + middleOffsetVector.Z);
            Vector3 collisionVectorMiddle = GetCollisionVector(currentMiddleVector, destinationMiddleVector);
            
            Thread.Sleep(10);

            Vector3 bottomMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomMiddle);
            Vector3 currentBottomMiddleVector = new Vector3(currentPositionVector.X + bottomMiddleOffsetVector.X, currentPositionVector.Y + bottomMiddleOffsetVector.Y, currentPositionVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 destinationBottomMiddleVector = new Vector3(destinationVector.X + bottomMiddleOffsetVector.X, destinationVector.Y + bottomMiddleOffsetVector.Y, destinationVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomMiddle = GetCollisionVector(currentBottomMiddleVector, destinationBottomMiddleVector);
            
            Thread.Sleep(10);

            Vector3 bottomSemiMiddleOffsetVector = GetBodyPartOffsetVector(target, BodyPart.BottomSemiMiddle);
            Vector3 currentBottomSemiMiddleVector = new Vector3(currentPositionVector.X + bottomSemiMiddleOffsetVector.X, currentPositionVector.Y + bottomSemiMiddleOffsetVector.Y, currentPositionVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 destinationBottomSemiMiddleVector = new Vector3(destinationVector.X + bottomSemiMiddleOffsetVector.X, destinationVector.Y + bottomSemiMiddleOffsetVector.Y, destinationVector.Z + bottomSemiMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomSemiMiddle = GetCollisionVector(currentBottomSemiMiddleVector, destinationBottomSemiMiddleVector);

            Thread.Sleep(10);

            Vector3 bottomOffsetVector = GetBodyPartOffsetVector(target, BodyPart.Bottom);
            Vector3 currentBottomVector = new Vector3(currentPositionVector.X + bottomOffsetVector.X, currentPositionVector.Y + bottomOffsetVector.Y, currentPositionVector.Z + bottomOffsetVector.Z);
            Vector3 destinationBottomVector = new Vector3(destinationVector.X + bottomOffsetVector.X, destinationVector.Y + bottomOffsetVector.Y, destinationVector.Z + bottomOffsetVector.Z);
            Vector3 collisionVectorBottom = GetCollisionVector(currentBottomVector, destinationBottomVector);

            if (direction == MovementDirection.Downward)
            {
                collisionVector = collisionVectorBottom;
                if (HasCollision(collisionVector))
                {
                    foundCollision = true;
                    target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomOffsetVector;
                }
                
            }
            else if (direction == MovementDirection.Upward)
            {
                collisionVector = collisionVectorTop;
                if(HasCollision(collisionVector))
                {
                    foundCollision = true;
                    target.MovementInstruction.CharacterBodyCollisionOffsetVector = topOffsetVector;
                }
            }
            else
            {
                float minDist = 10000;
                if (HasCollision(collisionVectorBottom))
                {
                    float collDist = Vector3.Distance(currentBottomVector, collisionVectorBottom);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorBottom;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomOffsetVector;
                    }
                    
                }
                if (HasCollision(collisionVectorBottomSemiMiddle))
                {
                    float collDist = Vector3.Distance(currentBottomSemiMiddleVector, collisionVectorBottomSemiMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorBottomSemiMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomSemiMiddleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorBottomMiddle))
                {
                    float collDist = Vector3.Distance(currentBottomMiddleVector, collisionVectorBottomMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorBottomMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = bottomMiddleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorMiddle))
                {
                    float collDist = Vector3.Distance(currentMiddleVector, collisionVectorMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = middleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorTopMiddle))
                {
                    float collDist = Vector3.Distance(currentTopMiddleVector, collisionVectorTopMiddle);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorTopMiddle;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = topMiddleOffsetVector;
                    }
                }
                if (HasCollision(collisionVectorTop))
                {
                    float collDist = Vector3.Distance(currentTopVector, collisionVectorTop);
                    if (collDist < minDist)
                    {
                        minDist = collDist;
                        foundCollision = true;
                        collisionVector = collisionVectorTop;
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = topOffsetVector;
                    }
                }
            }

            if (!foundCollision)
                target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();

            return collisionVector;
        }

        private Vector3 GetBodyPartOffsetVector(Character target, BodyPart bodyPart)
        {
            Vector3 bodyPartOffsetVector = new Vector3(-10000, -10000, -10000);
            switch(bodyPart)
            {
                case BodyPart.Bottom:
                    bodyPartOffsetVector = new Vector3(0, 0, 0);
                    break;
                case BodyPart.BottomSemiMiddle:
                    bodyPartOffsetVector = new Vector3(0, 0.75f, 0);
                    break;
                case BodyPart.BottomMiddle:
                    bodyPartOffsetVector = new Vector3(0, 1.5f, 0);
                    break;
                case BodyPart.Middle:
                    bodyPartOffsetVector = new Vector3(0, 3, 0);
                    break;
                case BodyPart.TopMiddle:
                    bodyPartOffsetVector = new Vector3(0, 4.5f, 0);
                    break;
                case BodyPart.Top:
                    bodyPartOffsetVector = new Vector3(0, 6, 0);
                    break;
            }

            return bodyPartOffsetVector;
        }

        private bool HasCollision(Vector3 collisionVector)
        {
            return !(collisionVector.X == 0f && collisionVector.Y == 0f && collisionVector.Z == 0f);
        }

        private BodyPart GetBodyPartFromOffsetVector(Vector3 bodyPartOffsetVector)
        {
            BodyPart bodyPart = BodyPart.None;
            if (bodyPartOffsetVector.Y == 0f)
                bodyPart = BodyPart.Bottom;
            else if (bodyPartOffsetVector.Y == 0.75f)
                bodyPart = BodyPart.BottomSemiMiddle;
            else if (bodyPartOffsetVector.Y == 1.5f)
                bodyPart = BodyPart.BottomMiddle;
            else if (bodyPartOffsetVector.Y == 3f)
                bodyPart = BodyPart.Middle;
            else if (bodyPartOffsetVector.Y == 4.5f)
                bodyPart = BodyPart.TopMiddle;
            else if (bodyPartOffsetVector.Y == 6f)
                bodyPart = BodyPart.Top;
            return bodyPart;
        }

        private float Get2DAngleBetweenVectors(Vector3 v1, Vector3 v2, out bool isClockwiseTurn)
        {
            var x = v1.X * v2.Z - v2.X * v1.Z;
            isClockwiseTurn = x < 0;
            var dotProduct = Vector3.Dot(v1, v2);
            if (dotProduct > 1)
                dotProduct = 1;
            if (dotProduct < -1)
                dotProduct = -1;
            var y = (float)Math.Acos(dotProduct);
            return y;
        }

        public void StopMovement(Character target)
        {
            if (this.characterMovementTimerDictionary != null && this.characterMovementTimerDictionary.ContainsKey(target))
            {
                System.Threading.Timer timer = this.characterMovementTimerDictionary[target];
                if (timer != null)
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                this.characterMovementTimerDictionary[target] = null;
            }
        }

        public void StartMovment(Character target)
        {
            if (this.characterMovementTimerDictionary == null)
                this.characterMovementTimerDictionary = new Dictionary<Character, System.Threading.Timer>();
            StopMovement(target);
            System.Threading.Timer timer = new System.Threading.Timer(timer_Elapsed, target, Timeout.Infinite, Timeout.Infinite);
            if (this.characterMovementTimerDictionary.ContainsKey(target))
                this.characterMovementTimerDictionary[target] = timer;
            else
                this.characterMovementTimerDictionary.Add(target, timer);
            target.MovementInstruction.MovementUnit = this.GetMovementUnit(target);
            timer.Change(1, Timeout.Infinite);
        }

        private void timer_Elapsed(object state)
        {
            Action d = async delegate()
            {
                Character target = state as Character;
                if (target.MovementInstruction != null && target.MovementInstruction.IsMoving)
                {
                    MovementMember movementMember = this.MovementMembers.First(mm => mm.MovementDirection == target.MovementInstruction.CurrentMovementDirection);
                    // if last direction is current direction, increment position
                    if (target.MovementInstruction.CurrentMovementDirection == target.MovementInstruction.LastMovementDirection)
                    {
                        if (!target.MovementInstruction.IsInCollision)
                        {
                            target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
                            Key key = movementMember.AssociatedKey;
                            if (Keyboard.IsKeyDown(key))
                            {
                                Move(target);
                            }
                            await Task.Delay(5);
                            //if (this.IsPlaying)
                            {
                                var timer = this.characterMovementTimerDictionary[target];
                                if(timer != null)
                                    timer.Change(5, Timeout.Infinite);
                            }
                        }
                    }
                    else // else change direction and increment position
                    {
                        target.MovementInstruction.IsInCollision = false;
                        target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
                        // Play movement
                        PlayMovementMember(movementMember, target);
                        target.MovementInstruction.LastMovementDirection = target.MovementInstruction.CurrentMovementDirection;
                        //if (this.IsPlaying)
                        {
                            var timer = this.characterMovementTimerDictionary[target];
                            if (timer != null)
                                timer.Change(1, Timeout.Infinite);
                        }
                    }
                }
                else if (target.MovementInstruction != null && target.MovementInstruction.IsTurning)
                {
                    bool associatedKeyPressed = CheckIfAssociatedTurnKeysPressed(target.MovementInstruction.CurrentRotationAxisDirection);
                    if (associatedKeyPressed)
                    {
                        Turn(target);
                    }
                    await Task.Delay(5);
                    //if (this.IsPlaying)
                    {
                        var timer = this.characterMovementTimerDictionary[target];
                        if (timer != null)
                            timer.Change(5, Timeout.Infinite);
                    }
                }
                else if (target.MovementInstruction != null && target.MovementInstruction.IsMovingToDestination)
                {
                    if (target.MovementInstruction.DestinationVector.X != -10000f && target.MovementInstruction.DestinationVector.Y != -10000f && target.MovementInstruction.DestinationVector.Z != -10000f)
                    {
                        var dist = Vector3.Distance(target.MovementInstruction.DestinationVector, target.CurrentPositionVector);
                        if (dist < 5)
                        {
                            if(this.Name == Constants.KNOCKBACK_MOVEMENT_NAME)
                            {
                                MovementMember downMem = this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Downward);
                                PlayMovementMember(downMem, target);
                            }
                            MovementMember stillMem = this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Still);
                            PlayMovementMember(stillMem, target);
                            target.MovementInstruction.IsMovingToDestination = false;
                            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
                            target.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                            this.StopMovement(target);
                            //KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                            //keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BindLoadFile, Constants.GAME_ENABLE_CAMERA_FILENAME);
                            //keyBindsGenerator.CompleteEvent();
                        }
                        else
                        {
                            if(target.MovementInstruction.CurrentMovementDirection == MovementDirection.None)
                            {
                                MovementMember directionMem = this.MovementMembers.First(mm => mm.MovementDirection == target.MovementInstruction.MovmementDirectionToUseForDestinationMove);
                                PlayMovementMember(directionMem, target);
                                target.MovementInstruction.CurrentMovementDirection = directionMem.MovementDirection;
                            }
                            else
                            {
                                if (!target.MovementInstruction.IsInCollision)
                                    Move(target);
                                else
                                {
                                    if(target.MovementInstruction.StopOnCollision)
                                    {
                                        MovementMember stillMem = this.MovementMembers.First(mm => mm.MovementDirection == MovementDirection.Still);
                                        PlayMovementMember(stillMem, target);
                                        target.MovementInstruction.IsMovingToDestination = false;
                                        target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
                                        target.MovementInstruction.DestinationVector = new Vector3(-10000f, -10000f, -10000f);
                                        this.StopMovement(target);
                                    }
                                }
                            }
                        }
                        await Task.Delay(5);
                        //if (this.IsPlaying)
                        {
                            var timer = this.characterMovementTimerDictionary[target];
                            if (timer != null)
                                timer.Change(5, Timeout.Infinite);
                        }
                    }
                }
            };
            System.Windows.Application.Current.Dispatcher.BeginInvoke(d);

        }

        private bool CheckIfAssociatedTurnKeysPressed(MovementDirection turnAxisDirection)
        {
            bool keyPressed = false;
            switch(turnAxisDirection)
            {
                case MovementDirection.Upward:
                    keyPressed = Keyboard.IsKeyDown(Key.Right);
                    break;
                case MovementDirection.Downward:
                    keyPressed = Keyboard.IsKeyDown(Key.Left);
                    break;
                case MovementDirection.Forward:
                    keyPressed = Keyboard.IsKeyDown(Key.Right) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                    break;
                case MovementDirection.Backward:
                    keyPressed = Keyboard.IsKeyDown(Key.Left) && ( Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                    break;
                case MovementDirection.Right:
                    keyPressed = Keyboard.IsKeyDown(Key.Up);
                    break;
                case MovementDirection.Left:
                    keyPressed = Keyboard.IsKeyDown(Key.Down);
                    break;
            }
            return keyPressed;
        }

        private Vector3 GetDestinationVector(Vector3 directionVector, float units, Character target)
        {
            Vector3 vCurrent = target.CurrentPositionVector;
            directionVector.Normalize();
            var destX = vCurrent.X + directionVector.X * units;
            var destY = vCurrent.Y + directionVector.Y * units;
            var destZ = vCurrent.Z + directionVector.Z * units;
            Vector3 dest = new Vector3(destX, destY, destZ);
            dest = Helper.GetRoundedVector(dest, 2);
            return dest;
        }


        public Vector3 GetDirectionVector(double rotaionAngle, MovementDirection direction, Vector3 facingVector)
        {
            float vX, vY, vZ;
            double rotationAxisX = 0, rotationAxisY = 1, rotationAxisZ = 0;
            if (direction == MovementDirection.Upward)
            {
                vX = 0;
                vY = 1;
                vZ = 0;
            }
            else if (direction == MovementDirection.Downward)
            {
                vX = 0;
                vY = -1;
                vZ = 0;
            }
            else
            {
                double rotationAngleRadian = GetRadianAngle(rotaionAngle);
                double tr = 1 - Math.Sin(rotationAngleRadian);
                //a1 = (t(r) * X * X) + cos(r)
                var a1 = tr * rotationAxisX * rotationAxisX + Math.Cos(rotationAngleRadian);
                //a2 = (t(r) * X * Y) - (sin(r) * Z)
                var a2 = tr * rotationAxisX * rotationAxisY - Math.Sin(rotationAngleRadian) * rotationAxisZ;
                //a3 = (t(r) * X * Z) + (sin(r) * Y)
                var a3 = tr * rotationAxisX * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisY;
                //b1 = (t(r) * X * Y) + (sin(r) * Z)
                var b1 = tr * rotationAxisX * rotationAxisY + Math.Sin(rotationAngleRadian) * rotationAxisZ;
                //b2 = (t(r) * Y * Y) + cos(r)
                var b2 = tr * rotationAxisY * rotationAxisY + Math.Cos(rotationAngleRadian);
                //b3 = (t(r) * Y * Z) - (sin(r) * X)
                var b3 = tr * rotationAxisY * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisX;
                //c1 = (t(r) * X * Z) - (sin(r) * Y)
                var c1 = tr * rotationAxisX * rotationAxisZ - Math.Sin(rotationAngleRadian) * rotationAxisY;
                //c2 = (t(r) * Y * Z) + (sin(r) * X)
                var c2 = tr * rotationAxisY * rotationAxisZ + Math.Sin(rotationAngleRadian) * rotationAxisX;
                //c3 = (t(r) * Z * Z) + cos (r)
                var c3 = tr * rotationAxisZ * rotationAxisZ + Math.Cos(rotationAngleRadian);

                //facingVector.Y = 0; // cancelling out vertical components of movement

                //vX = (float)(a1 * facingVector.X + a2 * facingVector.Y + a3 * facingVector.Z);
                //vY = (float)(b1 * facingVector.X + b2 * facingVector.Y + b3 * facingVector.Z);
                //vZ = (float)(c1 * facingVector.X + c2 * facingVector.Y + c3 * facingVector.Z);
                Vector3 facingVectorToDestination = facingVector;
                vX = (float)(a1 * facingVectorToDestination.X + a2 * facingVectorToDestination.Y + a3 * facingVectorToDestination.Z);
                vY = (float)(b1 * facingVectorToDestination.X + b2 * facingVectorToDestination.Y + b3 * facingVectorToDestination.Z);
                vZ = (float)(c1 * facingVectorToDestination.X + c2 * facingVectorToDestination.Y + c3 * facingVectorToDestination.Z);
            }

            return Helper.GetRoundedVector(new Vector3(vX, vY, vZ), 2);
        }
        public double GetRadianAngle(double angle)
        {
            return (Math.PI / 180) * angle;
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
                if (movementMember.MemberAbility != null) // && !movementMember.MemberAbility.IsActive)
                {
                    foreach (var mm in this.MovementMembers.Where(mm => mm != movementMember)) // && mm.MemberAbility.IsActive))
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
            switch (direction)
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
                    rotationAngle = 90d;
                    break;
                case MovementDirection.Downward:
                    rotationAngle = -90d;
                    break;
            }
            return rotationAngle;
        }
    }

    public class MovementMember : NotifyPropertyChanged
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
        private object lockObj = new object();
        public bool IsMoving { get; set; }
        public bool IsTurning { get; set; }
        public bool IsMovingToDestination { get; set; }
        public Vector3 DestinationVector { get; set; }
        public Vector3 OriginalDestinationVector { get; set; }
        public Vector3 CurrentDirectionVector { get; set; }
        public MovementDirection CurrentMovementDirection { get; set; }
        public MovementDirection LastMovementDirection { get; set; }
        public MovementDirection CurrentRotationAxisDirection { get; set; }
        public MovementDirection MovmementDirectionToUseForDestinationMove { get; set; }
        public bool StopOnCollision { get; set; }

        public float MovementUnit { get; set; }

        private bool isInCollision;
        public bool IsInCollision
        {
            get
            {
                lock (lockObj)
                {
                    return isInCollision;
                }
            }
            set
            {
                lock (lockObj)
                {
                    isInCollision = value;
                }
            }
        }
        /// <summary>
        /// This is the point upto which we have performed collision calculations, and the character can move to this point in current movement direction without recalculating collision
        /// </summary>
        public Vector3 LastCollisionFreePointInCurrentDirection
        {
            get;
            set;
        }
        /// <summary>
        /// Indicates whether there is a collision in next 20 units in current direction. True means the LastCollisionFreePointInCurrentDirection is an actual collision point. False means
        /// we have only calculated upto the LastCollisionFreePointInCurrentDirection point, but it is not an actual collision
        /// </summary>
        public bool IsCollisionAhead { get; set; }
        /// <summary>
        /// The point in character body that will have collision in the current direction
        /// </summary>
        public Vector3 CharacterBodyCollisionOffsetVector
        {
            get;
            set;
        }

        public float DistanceFromCollisionFreePoint { get; set; }
        public bool IsPositionAdjustedToAvoidCollision { get; set; }
        public float DestinationPointHeightAdjustment { get; set; }
        public bool IsDestinationPointAdjusted { get; set; }
    }

    public class CollisionInfo
    {
        public Vector3 BodyCollisionOffsetVector { get; set; }
        public BodyPart CollisionBodyPart { get; set; }
        public float CollisionDistance { get; set; }
        public Vector3 CollisionPoint { get; set; }
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
