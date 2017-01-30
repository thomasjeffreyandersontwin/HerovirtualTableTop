using System;
using System.Collections.Generic;
using System.Linq;
using HeroVirtualTableTop.Desktop;

namespace HeroVirtualTableTop.ManagedCharacter
{
    public class CharacterActionListImpl<T> : Dictionary<string, T>, CharacterActionList<T> where T : CharacterAction
    {
        private T _active;
        private T _default;

        public CharacterActionListImpl(CharacterActionType type, KeyBindCommandGenerator generator,
            ManagedCharacter owner)
        {
            Type = type;
            Generator = generator;
            //ListByOrder = new SortedDictionary<int, T>();
            Owner = owner;
        }

        public KeyBindCommandGenerator Generator { get; set; }

        private Dictionary<int, T> ListByOrder
        {
            get { return (from action in Values orderby action.Order select action).ToDictionary(x => x.Order); }
        }

        public IEnumerator<T> ByOrder => ListByOrder.Values.GetEnumerator();

        public ManagedCharacter Owner { get; }

        public CharacterActionType Type { get; set; }

        public T Active
        {
            get
            {
                if (_active == null)
                    if (_default != null)
                    {
                        _active = _default;
                    }
                    else
                    {
                        if (Count > 1)
                            _active = Values.First();
                    }
                return _active;
            }

            set
            {
                if (value != null)
                    _active = value;
            }
        }

        public void Deactivate()
        {
            _active = default(T);
        }

        public T Default
        {
            get
            {
                if (_default == null)
                    if (Count > 1)
                        _default = Values.First();
                return _default;
            }
            set
            {
                if (_default != null)
                    if (ContainsValue(value))
                        _default = value;
                    else
                        throw new ArgumentException("action cant be set to default it doesnt exist for character");
            }
        }

        public string GetNewValidActionName(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (Type == CharacterActionType.Identity)
                    name = "Identity";
                if (Type == CharacterActionType.Movement)
                    name = "Movement";
                if (Type == CharacterActionType.Ability)
                    name = "Ability";
            }
            var suffix = string.Empty;
            var i = 0;

            while (Values.Cast<CharacterAction>().Any(action => action.Name == name + suffix))
                suffix = $" ({++i})";
            return $"{name}{suffix}".Trim();
        }

        public T this[int order]
        {
            get { return ListByOrder[order]; }
            set { ListByOrder[order] = value; }
        }

        public void AddMany(List<T> list)
        {
            foreach (var item in list)
                Insert(item);
        }

        public void Insert(T action)
        {
            action.Owner = Owner;
            action.Generator = Generator;
            var count = 0;
            if (ListByOrder != null && ListByOrder.Count > 0)
                count = ListByOrder.Last().Key;
            action.Order = count + 1;
            Add(action.Name, action);
        }

        public void InsertAfter(T actionToInsert, T precedingAction)
        {
            var precedingOrder = precedingAction.Order;
            foreach (var action in ListByOrder.Reverse())
                if (action.Key > precedingOrder)
                {
                    action.Value.Order++;
                    ListByOrder[action.Value.Order] = action.Value;
                }
            actionToInsert.Order = precedingOrder + 1;
            ListByOrder[actionToInsert.Order] = actionToInsert;
            Add(actionToInsert.Name, actionToInsert);
        }

        public void RemoveAction(T actionToRemove)
        {
            var deletedOrder = actionToRemove.Order;
            Remove(actionToRemove.Name);
            ListByOrder.Values.ToList().Where(c => c.Order > deletedOrder).ToList().ForEach(c => c.Order--);
        }

        public T AddNew(T newAction)
        {
            newAction.Owner = Owner;
            newAction.Generator = Generator;
            if (ListByOrder.Count == 0)
                newAction.Order = 1;
            else
                newAction.Order = ListByOrder.Last().Key + 1;
            newAction.Name = GetNewValidActionName(newAction.Name);
            Add(newAction.Name, newAction);
            return newAction;
        }

        public CharacterActionList<T> Clone()
        {
            var cloneList = new CharacterActionListImpl<T>(Type, Generator, Owner);
            foreach (var anAction in Values)
            {
                var clone = (T) anAction.Clone();
                cloneList.Insert(clone);
            }
            return cloneList;
        }

        public void PlayByKey(string shortcut)
        {
        }
    }

    public abstract class CharacterActionImpl : CharacterAction
    {
        protected CharacterActionImpl(ManagedCharacter owner, string name, KeyBindCommandGenerator generator,
            string shortcut)
        {
            Name = name;
            Owner = owner;
            Generator = generator;
            KeyboardShortcut = shortcut;
        }

        protected CharacterActionImpl()
        {
        }

        public string KeyboardShortcut { get; set; }
        public KeyBindCommandGenerator Generator { get; set; }

        public string Name { get; set; }
        public int Order { get; set; }
        public ManagedCharacter Owner { get; set; }

        public abstract CharacterAction Clone();
        public abstract void Render(bool completeEvent = true);
    }
}