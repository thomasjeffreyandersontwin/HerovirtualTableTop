using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
namespace HeroVirtualTableTop.ManagedCharacter
{
    
    public class IdentityImpl : CharacterActionImpl,Identity 
    {
        public IdentityImpl(ManagedCharacter owner, string name, string surface, SurfaceType type, KeyBindCommandGenerator generator, string shortcut):base(owner,name,generator, shortcut)
        {
            Type = type;
            Surface = surface;
        }
        public IdentityImpl():base()
        {
        }
        public String Surface { get; set; }
        public SurfaceType Type { get; set; }
        public override void Render(bool completeEvent = true)
        {
            switch (Type)
            {
                case SurfaceType.Model:
                    {
                        Generator.GenerateDesktopCommandText(DesktopCommand.BeNPC, Surface);
                        break;
                    }
                case SurfaceType.Costume:
                    {
                         Generator.GenerateDesktopCommandText(DesktopCommand.LoadCostume, Surface);
                        break;
                    }
            }
            if (completeEvent)
            {
                Generator.CompleteEvent();
            }
            Owner.Target(completeEvent);
        }

        public override CharacterAction Clone()
        {
            Identity clone = new IdentityImpl(Owner, Name, Surface, Type, Generator,KeyboardShortcut);
            return clone;
        }
    }
}
