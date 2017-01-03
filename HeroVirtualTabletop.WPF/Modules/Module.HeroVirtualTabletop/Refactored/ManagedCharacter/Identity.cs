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

        public IdentityImpl(KeyBindCommandGenerator generator, ManagedCharacter owner)
        {
            _owner = owner;
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
            string model = "Model_Statesman";
            if (Type == SurfaceType.Model)
            {
                model = Surface;
            }
            Generator.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, model, Owner.DesktopLabel);
            Owner.Target(completeEvent);
        }
    }
}
