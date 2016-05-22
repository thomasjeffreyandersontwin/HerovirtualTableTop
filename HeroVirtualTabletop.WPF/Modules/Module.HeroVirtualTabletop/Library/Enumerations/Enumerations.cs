using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Enumerations
{
    public enum AnimationSequenceType
    {
        And,
        Or
    }

    public enum IdentityType
    {
        Model,
        Costume
    }
    public enum ClipboardAction
    {
        Clone, 
        Cut, 
        Link
    }
    public enum ExpansionUpdateEvent
    {
        Filter,
        Delete,
        Paste
    }
}
