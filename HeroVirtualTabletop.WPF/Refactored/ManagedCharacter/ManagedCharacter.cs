using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.Crowd;
using Framework.WPF.Library;

namespace HeroVirtualTableTop.ManagedCharacter
{
    public class ManagedCharacterImpl : NotifyPropertyChanged, ManagedCharacter
    {
        public DesktopCharacterMemoryInstance MemoryInstance { get; set; }
        public DesktopCharacterTargeter Targeter { get; set; }

        private KeyBindCommandGenerator _generator;
        public KeyBindCommandGenerator Generator { get { return _generator; }  set { _generator = value; }   }

        private Camera _camera;
        public Camera Camera { get { return _camera; } set { _camera = value; } }

        private CharacterActionList<Identity> _identities;

        public ManagedCharacterImpl(DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera, CharacterActionList<Identity> identities)
        {

            Targeter = targeter;
            _generator = generator;
            _camera = camera;
            if (_identities == null)
            {
                identities = new CharacterActionListImpl<Identity>(CharacterActionType.Identity,Generator, this);
            }
            _identities = identities;
            foreach (Identity id in _identities.Values)
            {
                id.Owner = this;
            }
        }

        public ManagedCharacterImpl(DesktopCharacterTargeter targeter, KeyBindCommandGenerator generator, Camera camera):this(targeter, generator, camera, null)
        {

            
        }

        
        public Position Position
        {
            get
            {
                return MemoryInstance.Position;
            }
        }
        public string Name { get; set; }
        public virtual string DesktopLabel
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
                if (MemoryInstance == null)
                {
                    return false;
                }
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

                Generator.GenerateDesktopCommandText(DesktopCommand.TargetName, Name + " ["+DesktopLabel+"]");
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
                    while (currentTarget.Label != string.Empty )
                    {
                        currentTarget = Targeter.TargetedInstance;
                        if(currentTarget.Label == null) { break; }
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
            Target();
            Camera.MoveToTarget();
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
                _identities = (CharacterActionList<Identity>)new CharacterActionListImpl<Identity>(CharacterActionType.Identity, Generator ,this);
            }
            if (Identities.Count == 0 && Identities.Active==null)
            {
                Identity newId = _identities.AddNew(new IdentityImpl());
                newId.Owner= this;
                newId.Name = this.Name;
                newId.Type = SurfaceType.Costume;
                newId.Surface = this.Name;
                Identities.Active = newId;
            }
            string spawnText = Name;
            if (DesktopLabel !=null || DesktopLabel != "")
            {
                spawnText = Name + " [" + DesktopLabel + "]";
            }

            Identity active = Identities.Active;
            Generator.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, "model_statesmen", spawnText);
            active.Render();
        }
        
        public void ClearFromDesktop(bool completeEvent = true) {
            Target();
            Generator.GenerateDesktopCommandText(DesktopCommand.DeleteNPC);
            IsSpawned = false;
            IsTargeted = false;
            IsManueveringWithCamera = false;
            IsFollowed = false;
            this.MemoryInstance = null;
        }
        public void MoveCharacterToCamera(bool completeEvent = true) {
            Target();
            Generator.GenerateDesktopCommandText(DesktopCommand.MoveNPC);
            Generator.CompleteEvent();
        }
        public CharacterProgressBarStats ProgressBar { get; set; }

    }
    
}

