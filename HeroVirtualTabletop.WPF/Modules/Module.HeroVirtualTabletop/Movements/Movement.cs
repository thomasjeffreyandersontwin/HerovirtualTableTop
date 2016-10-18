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
            this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            this.Character.MovementInstruction.CurrentDirection = MovementDirection.None;
            this.Character.MovementInstruction.LastDirection = MovementDirection.None;
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
                            if (inputKey == Key.Escape)
                            {
                                DeactivateMovement();
                                this.Character.ActiveMovement = null;
                            }
                            else
                            {
                                MovementDirection direction = GetMovementDirectionFromKey(inputKey);
                                if (direction != MovementDirection.None && this.Character.MovementInstruction.CurrentDirection != direction)
                                {
                                    this.Character.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f); // reset collision
                                    this.Character.MovementInstruction.LastDirection = this.Character.MovementInstruction.CurrentDirection;
                                    this.Character.MovementInstruction.CurrentDirection = direction;
                                    this.Movement.StartMovment(this.Character);
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
    }
    public class Movement : NotifyPropertyChanged
    {
        private System.Threading.Timer timer;
        [JsonConstructor]
        private Movement() { }
        private ILogManager logManager = new FileLogManager(typeof(Movement));
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

        public void Move(Character target)
        {
            double rotationAngle = GetRotationAngle(target.MovementInstruction.CurrentDirection);
            Vector3 directionVector = GetDirectionVector(rotationAngle, target.MovementInstruction.CurrentDirection, target.CurrentFacingVector);
            Vector3 allowableDestinationVector = GetAllowableDestinationVector(target, directionVector);
            //(target.Position as Position).SetPosition(destinationVector);
            target.CurrentPositionVector = allowableDestinationVector;
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
                    if (!HasCollision(collisionVector) || collisionDistance < distance) // proper collision
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
            Vector3 destinationVectorNext = GetDestinationVector(directionVector, 0.25f, target);
            Vector3 destinationVectorFar = GetDestinationVector(directionVector, 20f, target);
            Vector3 collisionVector = new Vector3();
            MovementDirection direction = target.MovementInstruction.CurrentDirection;
            float distanceFromDest = Vector3.Distance(currentPositionVector, destinationVectorNext);
            float distanceFromCollisionPoint = 0f;
            Vector3 collisionBodyPoint = new Vector3();
            //logManager.Info(string.Format("Current position: {0}, {1}, {2}", currentPositionVector.X, currentPositionVector.Y, currentPositionVector.Z));
            if (target.MovementInstruction.LastCollisionFreePointInCurrentDirection.X == -10000f
                && target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y == -10000f
                && target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Z == -10000f
                ) // Need to recalculate next collision point
            {
                collisionVector = CalculateNextCollisionPoint(target, destinationVectorFar);
                if (HasCollision(collisionVector)) // Collision ahead - can only move upto the collision point
                {
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = collisionVector;
                    target.MovementInstruction.CollisionAhead = true;
                }
                else // No collision in 20 units, so free to move next 20 units
                {
                    target.MovementInstruction.LastCollisionFreePointInCurrentDirection = destinationVectorFar;
                    target.MovementInstruction.CollisionAhead = false;
                }
            }
            else
            {
                logManager.Info(string.Format("CollisionPoint: {0}, {1}, {2}", target.MovementInstruction.LastCollisionFreePointInCurrentDirection.X, target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Y, target.MovementInstruction.LastCollisionFreePointInCurrentDirection.Z));
                collisionBodyPoint = Vector3.Add(currentPositionVector, target.MovementInstruction.CharacterBodyCollisionOffsetVector);
                //logManager.Info(string.Format("CollisionBodyPoint: {0}, {1}, {2}", collisionBodyPoint.X, collisionBodyPoint.Y, collisionBodyPoint.Z));
                distanceFromCollisionPoint = Vector3.Distance(collisionBodyPoint, target.MovementInstruction.LastCollisionFreePointInCurrentDirection);

                //logManager.Info("Distance from collision: " + distanceFromCollisionPoint.ToString());
                if(distanceFromDest > distanceFromCollisionPoint || distanceFromCollisionPoint < 1)// Collision point nearer, so can't move to destination without checking first
                {
                    if (target.MovementInstruction.CollisionAhead) // the LastCollisionFreePointInCurrentDirection is a collision point
                    {
                        //logManager.Info("Collision ahead true");
                        if (distanceFromDest > distanceFromCollisionPoint)
                            collisionVector = currentPositionVector; // stay where you are
                        else
                            collisionVector = destinationVectorNext; // just go to next point, but no further
                        target.MovementInstruction.IsInCollision = true;
                    }
                    else // the LastCollisionFreePointInCurrentDirection is just the last calculated point, so we need to recalculate the collisions in the current direction
                    {
                        //logManager.Info("Collision ahead false, recalculating");
                        collisionVector = CalculateNextCollisionPoint(target, destinationVectorFar);
                        //logManager.Info(string.Format("Recalculated Collision Point: {0}, {1}, {2}", collisionVector.X, collisionVector.Y, collisionVector.Z));
                        if (HasCollision(collisionVector)) // Collision ahead - can only move upto the collision point
                        {
                            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = collisionVector;
                            target.MovementInstruction.CollisionAhead = true;
                        }
                        else // No collision in 20 units, so free to move next 20 units
                        {
                            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = destinationVectorFar;
                            target.MovementInstruction.CollisionAhead = false;
                        }
                    }
                }
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
                var targetPosition = target.MovementInstruction.LastCollisionFreePointInCurrentDirection;
                if (distanceFromDest > distanceFromCollisionPoint)
                    allowableDestVector = currentPositionVector;
                else
                    allowableDestVector = destinationVectorNext;
                //logManager.Info("Collision detected and stopping");
            }
            else
            {
                allowableDestVector = new Vector3(destinationVectorNext.X, destinationVectorNext.Y, destinationVectorNext.Z);
                //logManager.Info("No collision, carrying on");
            }
            //logManager.Info(string.Format("Next position: {0}, {1}, {2}", allowableDestVector.X, allowableDestVector.Y, allowableDestVector.Z));
            //if (!HasCollision(collisionVector)) // No collision - move to destination
            //    allowableDestVector = new Vector3(destinationVectorNext.X, destinationVectorNext.Y, destinationVectorNext.Z);
            //else // Move to collision point
            //{
            //    allowableDestVector = new Vector3(collisionVector.X, collisionVector.Y, collisionVector.Z);
            //    target.MovementInstruction.IsInCollision = true;
            //}

            // Enable gravity if applicable
            if (allowableDestVector.Y > 0.5 && (direction != MovementDirection.Upward && direction != MovementDirection.Downward))
            {
                Vector3 collisionVectorGround = GetCollisionVector(allowableDestVector, new Vector3(allowableDestVector.X, 0f, allowableDestVector.Z));
                if (collisionVectorGround.Y >= 0f && collisionVectorGround.Y < allowableDestVector.Y)
                    allowableDestVector.Y = collisionVectorGround.Y;
            }
            // Preventing going to absurd locations
            var finalDistance = Vector3.Distance(currentPositionVector, allowableDestVector);
            if (finalDistance > 5f)
                allowableDestVector = currentPositionVector;
            // Preventing from character's feet going under ground
            if (allowableDestVector.Y < 0.25f && (direction != MovementDirection.Upward && direction != MovementDirection.Downward))
                allowableDestVector.Y = 0.25f;

            return allowableDestVector;
        }

        private Vector3 CalculateNextCollisionPoint(Character target, Vector3 destinationVector)
        {
            Vector3 collisionVector = new Vector3();
            bool foundCollision = false;

            Vector3 currentPositionVector = target.CurrentPositionVector;
            MovementDirection direction = target.MovementInstruction.CurrentDirection;

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
                if(HasCollision(collisionVectorBottomMiddle))
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
                if(HasCollision(collisionVectorMiddle))
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

        public void StopMovement()
        {
            this.IsPlaying = false;
            if (timer != null)
                timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMovment(Character target)
        {
            this.IsPlaying = true;
            timer = new System.Threading.Timer(timer_Elapsed, target, 1, Timeout.Infinite);
        }

        private void timer_Elapsed(object state)
        {
            Action d = async delegate()
            {
                Character target = state as Character;
                if (target.MovementInstruction != null)
                {
                    MovementMember movementMember = this.MovementMembers.First(mm => mm.MovementDirection == target.MovementInstruction.CurrentDirection);
                    // if last direction is current direction, increment position
                    if (target.MovementInstruction.CurrentDirection == target.MovementInstruction.LastDirection)
                    {
                        if (!target.MovementInstruction.IsInCollision)
                        {
                            target.MovementInstruction.LastDirection = target.MovementInstruction.CurrentDirection;
                            Key key = movementMember.AssociatedKey;
                            if (Keyboard.IsKeyDown(key))
                            {
                                Move(target);
                            }
                            await Task.Delay(5);
                            if (this.IsPlaying)
                                timer.Change(5, Timeout.Infinite);
                        }
                    }
                    else // else change direction and increment position
                    {
                        target.MovementInstruction.IsInCollision = false;
                        target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
                        target.MovementInstruction.CharacterBodyCollisionOffsetVector = new Vector3();
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

        private Vector3 GetDestinationVector(Vector3 directionVector, float units, Character target)
        {
            Vector3 vCurrent = (target.Position as Position).GetPositionVector();
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

                vX = (float)(a1 * facingVector.X + a2 * facingVector.Y + a3 * facingVector.Z);
                vY = (float)(b1 * facingVector.X + b2 * facingVector.Y + b3 * facingVector.Z);
                vZ = (float)(c1 * facingVector.X + c2 * facingVector.Y + c3 * facingVector.Z);
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
        public double Distance { get; set; }
        public IMemoryElementPosition Destination { get; set; }
        public int Ground { get; set; }
        public List<IMemoryElementPosition> IncrementalPositions { get; set; }
        public MovementDirection CurrentDirection { get; set; }
        public MovementDirection LastDirection { get; set; }
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
        public bool CollisionAhead { get; set; }
        /// <summary>
        /// The point in character body that will have collision in the current direction
        /// </summary>
        public Vector3 CharacterBodyCollisionOffsetVector
        {
            get;
            set;
        }
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
