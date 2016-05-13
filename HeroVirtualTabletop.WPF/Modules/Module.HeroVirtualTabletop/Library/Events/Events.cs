using Prism.Events;
using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Module.HeroVirtualTabletop.Library.Events
{
    public class AddToRosterEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { }
    public class EditCharacterEvent : PubSubEvent<Tuple<ICrowdMemberModel, Collection<ICrowdMemberModel>>> { }
    public class SaveCrowdEvent : PubSubEvent<object> { }
}
