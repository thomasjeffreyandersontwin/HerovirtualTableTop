using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
namespace HeroVirtualTableTop.ManagedCharacter
{
    public class Identities : CharacterActionListImpl<IdentityImpl>
    {
        public Identities() : base(CharacterActionType.Identity)
        { }


    }
    public class IdentityImpl : Identity, CharacterAction
    {
        ManagedCharacter _owner;
        public string Name { get; set; }
        public ManagedCharacter Owner { get { return _owner; } set { _owner = value; } }

        private KeyBindCommandGenerator _generator;
        public KeyBindCommandGenerator Generator { get { return _generator; } set { _generator = value; } }

        public IdentityImpl(KeyBindCommandGenerator generator, ManagedCharacter owner, SurfaceType type)
        {
            _owner = owner;
            Type = type;
            _generator = generator;
        }
        public IdentityImpl()
        {
        }

        public int Order { get; set; }

        public String Surface { get; set; }
        public SurfaceType Type { get; set; }
        public void Render(bool completeEvent = true)
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
    }
}
