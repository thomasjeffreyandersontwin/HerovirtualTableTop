using Prism.Events;
using Module.HeroVirtualTabletop.Crowds;
using System;

namespace Module.HeroVirtualTabletop.Library.Events
{
    public class AddToRosterEvent : PubSubEvent<Tuple<ICrowdMemberModel, CrowdModel>>
    {
    }
}
