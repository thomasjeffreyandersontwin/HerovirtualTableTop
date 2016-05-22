using Prism.Events;
using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Characters;

namespace Module.HeroVirtualTabletop.Library.Events
{
    public class AddToRosterEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { }
    public class DeleteFromRosterEvent : PubSubEvent<ICrowdMemberModel> { }
    public class EditCharacterEvent : PubSubEvent<Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>> { }
    public class SaveCrowdEvent : PubSubEvent<object> { }
    public class AddMemberToRosterEvent : PubSubEvent<Tuple<CrowdMemberModel, CrowdModel>> { };
    public class EditIdentityEvent : PubSubEvent<Tuple<Identity, Character>> { };
}
