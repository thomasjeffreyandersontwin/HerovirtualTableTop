using HeroVirtualTableTop.Crowd;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroVirtualTableTop.Common
{
    public class AddToRosterEvent : PubSubEvent<object> { }
    public class SaveCrowdEvent : PubSubEvent<object> { }
    public class SaveCrowdCompletedEvent : PubSubEvent<object> { }
    public class CreateCrowdFromModelsEvent : PubSubEvent<object> { };
}
