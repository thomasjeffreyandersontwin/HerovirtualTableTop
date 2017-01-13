using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
using System.Collections;

namespace HeroVirtualTableTop.ManagedCharacter
{
    class CameraImpl : Camera
    {

        public CameraImpl(KeyBindCommandGenerator generator)
        {
            _generator = generator;
        }
        private KeyBindCommandGenerator _generator;
        public KeyBindCommandGenerator Generator { get { return _generator; } }
        
        public Position Position { get; set; }

        private Identity _identity;
        public Identity Identity { get { return _identity; } }

        private ManagedCharacter _manueveringCharacter;
        public ManagedCharacter ManueveringCharacter
        {
            get
            {
                return _manueveringCharacter;
            }
            set
            {
                if (value != null)
                {
                    _manueveringCharacter = value;
                    _manueveringCharacter.Target(false);
                    MoveToTarget();

                    float dist = 13.23f, calculatedDistance;
                    int maxRecalculationCount = 5; // We will allow the same distance to be calculated 5 times at max. After this we'll assume that the camera is stuck.
                    Hashtable distanceTable = new Hashtable();
                    while (Position.IsWithin(dist, _manueveringCharacter.Position, out calculatedDistance) == false)
                    {
                        if (distanceTable.Contains(calculatedDistance))
                        {
                            int count = (int)distanceTable[calculatedDistance];
                            count++;
                            if (count > maxRecalculationCount)
                                break;
                            else
                                distanceTable[calculatedDistance] = count;
                        }
                        else
                        {
                            distanceTable.Add(calculatedDistance, 1);
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                    distanceTable.Clear();

                    _manueveringCharacter.ClearFromDesktop(true);
                    _identity = value.Identities.Active;
                    _identity.Render();                  
                }              
            }        
        }

        public void MoveToTarget(bool completeEvent = true) {
            Generator.GenerateDesktopCommandText(DesktopCommand.Follow, "");
            if (completeEvent)
            {
                Generator.CompleteEvent();
            }
        }
        public void ActivateCameraIdentity() { }
        public void ActivateManueveringCharacterIdentity() { }

        public void DisableMovement() { }
    }
}
