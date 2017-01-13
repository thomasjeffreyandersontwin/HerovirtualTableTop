using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualTableTop.Desktop;
namespace HeroVirtualTableTop.ManagedCharacter
{
    public class CharacterActionListImpl<T> : Dictionary<string, T>, CharacterActionList<T> where T : CharacterAction
    {
        public ManagedCharacter Owner { get; set; }

        private KeyBindCommandGenerator _generator;
        public KeyBindCommandGenerator Generator
        {
            get { return _generator; }
            set { _generator = value; }
        }
        private CharacterActionType _type;
        public CharacterActionType Type
        {
            get
            {
                return _type;
            }set
            {
                _type = value;
            }
        }
        public CharacterActionListImpl(CharacterActionType type, KeyBindCommandGenerator generator)
        {
            _type = type;
            _generator = generator;
            _listByOrder = new SortedDictionary<int, T>();
        }
        private SortedDictionary<int, T> _listByOrder = new SortedDictionary<int, T>();
        public IEnumerator<T> ByOrder
            {
            get {
                return _listByOrder.Values.GetEnumerator();
            }
        }
        private T _active;
        private T _default;
        public T Active
        {

            get
            {
                if (_active == null)
                {
                    if (_default != null)
                    {
                        _active = _default;
                    }
                    else
                    {
                        if (Count > 1)
                        {
                            _active = Values.First<T>();
                        }
                    }
                }
                return _active;
            }

            set
            {
                if (_active != null)
                {
                    if (ContainsValue(value))
                        _active = value;
                    else
                    {
                        throw new ArgumentException("action cant be set to active it doesnt exist for character");

                    }
                }
            }
        }


        public T Default
        {
            get
            {
                if (_default == null)
                {
                    if (Count > 1)
                    {
                        _default = Values.First<T>();
                    }
                }
                return _default;
            }
            set
            {
                if (_default != null)
                {
                    if (ContainsValue(value))
                        _default = value;
                    else
                    {
                        throw new ArgumentException("action cant be set to default it doesnt exist for character");

                    }
                }
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
            string suffix = string.Empty;
            int i = 0;

            while ((this.Values.Cast<CharacterAction>().Any((CharacterAction action) => { return action.Name == name + suffix; })))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return string.Format("{0}{1}", name, suffix).Trim();
        }

        public T this[int order]
        {
            get
            {
                return _listByOrder[order];
            }
            set
            {
                _listByOrder[order] = value;
            }
        }

        public void Insert(T action)
        {
            action.Owner = Owner;
            action.Generator = Generator;
            int count = 0;
            if(_listByOrder !=null && _listByOrder.Count >0)
            {
                count = _listByOrder.Last().Key;

            }
            action.Order = count + 1;
            this.Add(action.Name, action);
            _listByOrder.Add(action.Order, action);
        }
        public void InsertAfter(T actionToInsert, T precedingAction)
        {
            int precedingOrder = precedingAction.Order;
            foreach (var action in _listByOrder.Reverse())
            {
                if (action.Key > precedingOrder)
                {
                    action.Value.Order++;
                    _listByOrder[action.Value.Order] = action.Value;

                }
            }
            actionToInsert.Order = precedingOrder+1;
            _listByOrder[actionToInsert.Order] = actionToInsert;
            Add(actionToInsert.Name, actionToInsert);
        }
        public void RemoveAction(T actionToRemove)
        {
            Remove(actionToRemove.Name);
            _listByOrder.Remove(actionToRemove.Order);
            int deletedOrder = actionToRemove.Order;
            foreach (var action in _listByOrder)
            {
                if (action.Key > deletedOrder)
                {
                    action.Value.Order--;
                    _listByOrder[action.Value.Order] = action.Value;
                }
            }
        }
        public T AddNew(T newAction)
        {
           
            newAction.Owner = Owner;
            newAction.Generator = Generator;
            newAction.Order = _listByOrder.Last().Key + 1;
            newAction.Name = GetNewValidActionName(newAction.Name);
            _listByOrder.Add(newAction.Order, newAction);
            Add(newAction.Name, newAction);
            return newAction;
        }
        public CharacterActionList<T> Clone()
        {
            CharacterActionListImpl<T> cloneList = new CharacterActionListImpl<T>(_type, _generator);
            foreach (T anAction in Values)
            {
                T clone = (T)anAction.Clone();
                cloneList.Insert(clone);
            }
            return cloneList;
        }
    }
}