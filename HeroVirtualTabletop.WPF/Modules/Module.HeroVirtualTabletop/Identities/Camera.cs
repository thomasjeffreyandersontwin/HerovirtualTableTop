using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Identities
{
    public class Camera : Identity
    {
        public Camera(): base("V_Arachnos_Security_Camera", IdentityType.Model, "Camera")
        {
        }

        public string MoveToTarget(bool completeEvent = true)
        {
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow, "");
            if (completeEvent)
            {
                return keyBindsGenerator.CompleteEvent();
            }
            return string.Empty;
        }

        public new string Render(bool completeEvent = true)
        {
            //We need to untarget everything before loading camera skin
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            return base.Render(completeEvent);
        }

        protected internal static Position position = new Position(false, 1696336);
        private static Identity skin;

        private static string[] lastKeybinds;
        public string[] LastKeybinds
        {
            get
            {
                return lastKeybinds;
            }
        }

        private static Character maneuveredCharacter;
        public Character ManeuveredCharacter
        {
            get
            {
                return maneuveredCharacter;
            }
            set
            {
                string[] keybinds = new string[2];
                if (value != null)
                {
                    maneuveredCharacter = value;
                    maneuveredCharacter.Target(false);

                    MoveToTarget(false);
                    keybinds[0] = keyBindsGenerator.CompleteEvent();

                    DateTime k = DateTime.Now;
                    Position lastPosition = position.Clone(false) as Position;
                    float dist = 10;
                    while (position.IsWithin(dist, maneuveredCharacter.Position) == false && position != lastPosition) //(DateTime.Now - k).Seconds < 10 &&
                    {
                        lastPosition = position.Clone() as Position;
                        System.Threading.Thread.Sleep(500);
                    }

                    maneuveredCharacter.ClearFromDesktop();
                    skin = value.ActiveIdentity;
                    keybinds[1] = skin.Render();
                }
                else
                {
                    if (maneuveredCharacter != null)
                    {
                        keybinds[0] = Render();
                        keybinds[1] = maneuveredCharacter.Spawn();
                        maneuveredCharacter = null;
                    }
                }
                lastKeybinds = keybinds;
            }
        }
    }
}
