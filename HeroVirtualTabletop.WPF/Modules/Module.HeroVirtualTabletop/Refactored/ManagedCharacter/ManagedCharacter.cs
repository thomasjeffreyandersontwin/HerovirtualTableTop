using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
namespace HeroVirtualTableTop.ManagedCharacter
{
    class ManagedCharacterImpl : ManagedCharacter
    {
        public DesktopCharacterMemoryInstance MemoryInstance { get; set; }
        public DesktopCharacterTargeter Targeter { get; set; }

        private KeyBindCommandGenerator _generator;
        public KeyBindCommandGenerator Generator { get { return _generator; } }

        private Camera _camera;
        public Camera Camera { get { return _camera; } }

        private CharacterActionList<Identity> _identities;

        public ManagedCharacterImpl(DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities)
        {

            Targeter = targeter;
            _generator = generator;
            _camera = camera;
            _identities = identities;
            foreach (Identity id in _identities.Values)
            {
                id.Owner = this;
            }
        }


        public Position Position
        {
            get
            {
                return MemoryInstance.Position;
            }
        }
        public string Name { get; set; }
        public string DesktopLabel
        {
            get
            { if (MemoryInstance != null)
                {
                    return MemoryInstance.Label;
                }
                else
                {
                    return Name;
                }

            }
        }

        public void ToggleTargeted()
        {
            if (IsTargeted)
            {
                UnTarget();
            }
            else
            {
                Target();
            }
        }
        public bool IsTargeted
        {
            get
            {
                DesktopCharacterMemoryInstance targetedInstance = Targeter.TargetedInstance;
                if (MemoryInstance.MemoryAddress == targetedInstance.MemoryAddress)
                {
                    return true;
                }
                else
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
        public void Target(bool completeEvent = true) {

            if (MemoryInstance != null)
            {
                MemoryInstance.Target();
                MemoryInstance.WaitUntilTargetIsRegistered();
            }
            else
            {

                Generator.GenerateDesktopCommandText(DesktopCommand.TargetName, DesktopLabel);
                if (completeEvent)
                {
                    Generator.CompleteEvent();
                    MemoryInstance = Targeter.TargetedInstance;
                    MemoryInstance = MemoryInstance.WaitUntilTargetIsRegistered();
                }
            }
        }
        public void UnTarget(bool completeEvent = true)
        {
            Generator.GenerateDesktopCommandText(DesktopCommand.TargetEnemyNear);
            this.UnFollow();
            if (completeEvent)
            {
                Generator.CompleteEvent();
                try
                {

                    DesktopCharacterMemoryInstance currentTarget = Targeter.TargetedInstance;
                    while (currentTarget.Label != string.Empty)
                    {
                        currentTarget = Targeter.TargetedInstance;
                    }
                }
                catch
                {

                }
            }
            return;
        }

        public bool IsFollowed { get; set; }
        public void UnFollow(bool completeEvent = true)
        {
            if (this.IsFollowed)
            {
                Generator.GenerateDesktopCommandText(DesktopCommand.Follow);
                Generator.CompleteEvent();
                this.IsFollowed = false;
            }
        }
        public void Follow(bool completeEvent = true)
        {
            this.IsFollowed = true;
            Generator.GenerateDesktopCommandText(DesktopCommand.Follow);
            Generator.CompleteEvent();
        }
        public void TargetAndMoveCameraToCharacter(bool completeEvent = true) {
            Camera.MoveToCharacter(this);
        }

        private bool _maneuveringWithCamera;
        public void ToggleManueveringWithCamera()
        {
            IsManueveringWithCamera = !IsManueveringWithCamera;
        }
        public bool IsManueveringWithCamera
        {
            get
            {
                return _maneuveringWithCamera;
            }

            set
            {
                _maneuveringWithCamera = value;
                if (value == true)
                {
                    Camera.ManueveringCharacter = this;
                }
                else
                {
                    if (value == false)
                    {
                        Camera.ManueveringCharacter = null;
                    }
                }
            }
        }

        public CharacterActionList<Identity> Identities { get { return _identities; } }

        public bool IsSpawned { get; set; }
        public void SpawnToDesktop(bool completeEvent = true)
        {
            if (IsManueveringWithCamera)
            {
                IsManueveringWithCamera = false;
            }
            if (IsSpawned)
            {
                ClearFromDesktop();
            }

            Generator.GenerateDesktopCommandText(DesktopCommand.TargetEnemyNear);
            Generator.GenerateDesktopCommandText(DesktopCommand.NOP); //No operation, let the game untarget whatever it has targeted

            IsSpawned = true;
            if (Identities == null)
            {
                IsSpawned = true;
                if (Identities == null)
                {
                    _identities = (CharacterActionList<Identity>)new CharacterActionListImpl<IdentityImpl>(CharacterActionType.Identity);
                }
                if (Identities.Count == 0)
                {
                    Identity newId = _identities.CreateNew();
                    newId.Name = this.Name;
                    newId.Type = SurfaceType.Costume;
                    newId.Surface = this.Name;
                    Identities.Active = newId;
                }
                Identity active = Identities.Active;
                active.Render();
            }
        }
        public void ClearFromDesktop(bool completeEvent = true) {
            Target();
            Generator.GenerateDesktopCommandText(DesktopCommand.DeleteNPC);
            IsSpawned = false;
            IsManueveringWithCamera = false;
            IsFollowed = false;
            this.MemoryInstance = null;
        }
        public void MoveCharacterToCamera(bool completeEvent = true) { }
        public CharacterProgressBarStats ProgressBar { get; set; }
    }
    
}

