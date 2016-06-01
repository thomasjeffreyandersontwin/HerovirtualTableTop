using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimatedAbility : SequenceElement, ICharacterOption
    {
        [JsonConstructor]
        private AnimatedAbility() : base(string.Empty) { }

        public AnimatedAbility(string name, string activateOnKey = null, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, seqType, persistent, order, owner)
        {
            this.ActivateOnKey = activateOnKey;
        }

        private string activateOnKey;
        public string ActivateOnKey
        {
            get
            {
                return activateOnKey;
            }
            set
            {
                activateOnKey = value;
                OnPropertyChanged("ActivateOnKey");
            }
        }

    }
}
