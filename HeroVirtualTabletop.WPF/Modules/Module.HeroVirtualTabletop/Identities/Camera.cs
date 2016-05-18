using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Identities
{
    public class Camera : Identity
    {

        public Camera(): base("V_Arachnos_Security_Camera", IdentityType.Model, "Camera")
        {
        }

        //public string MoveToTarget(bool completeEvent = true)
        //{
        //    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow, "");
        //    if (completeEvent)
        //    {
        //        return keyBindsGenerator.CompleteEvent();
        //    }
        //    return string.Empty;
        //}

        public new string Render(bool completeEvent = true)
        {
            //We need to untarget everything before loading camera skin
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            return base.Render(completeEvent);
        }
    }
}
