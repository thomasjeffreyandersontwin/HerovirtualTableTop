using Prism.Events;
using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;

namespace Module.HeroVirtualTabletop.Library.Events
{
    public class AddToRosterEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { }
    public class DeleteCrowdMemberEvent : PubSubEvent<ICrowdMemberModel> { }
    public class EditCharacterEvent : PubSubEvent<Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>> { }
    public class SaveCrowdEvent : PubSubEvent<object> { }
    public class SaveCrowdCompletedEvent : PubSubEvent<object> { }
    public class AddToRosterThruCharExplorerEvent : PubSubEvent<Tuple<CrowdMemberModel, CrowdModel>> { };
    public class EditIdentityEvent : PubSubEvent<Tuple<Identity, Character>> { };
    public class EditAbilityEvent : PubSubEvent<Tuple<AnimatedAbility, Character>> { };
    public class CheckRosterConsistencyEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { };
    public class CreateCrowdFromModelsEvent : PubSubEvent<CrowdModel> { };
    public class ActivateCharacterEvent : PubSubEvent<Character> { };
    public class NeedAbilityCollectionRetrievalEvent : PubSubEvent<object> { };
    public class FinishedAbilityCollectionRetrievalEvent : PubSubEvent<ObservableCollection<AnimatedAbility>> { };
    public class StopAllActiveAbilitiesEvent : PubSubEvent<object> { };
    public class ListenForTargetChanged : PubSubEvent<object> { };
    public class StopListeningForTargetChanged : PubSubEvent<object> { };

    #region Attack Events
    public class AttackInitiatedEvent : PubSubEvent<Tuple<Character, Attack>> { };
    public class AttackCompletedEvent : PubSubEvent<Tuple<Character, Attack>> { };
    public class AttackTargetSelectedEvent : PubSubEvent<Tuple<Character, Attack>> { };
    public class ResetCharacterStateEvent : PubSubEvent<Character> { };
    public class AttackTargetUpdatedEvent : PubSubEvent<Tuple<List<Character>, Attack>> { };
    public class ConfigureActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { };
    public class SetActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class CloseActiveAttackEvent : PubSubEvent<object> { }
    #endregion

    #region Movement Events
    public class EditMovementEvent : PubSubEvent<Tuple<Movement, Character>> { };
    #endregion
}
