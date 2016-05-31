using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimatedAbility : SequenceElement, ICharacterOption
    {
        public AnimatedAbility(string name, string activateOnKey = null, AnimationSequenceType seqType = AnimationSequenceType.And, int order = 1, Character owner = null)
            : base(name, seqType, order, owner)
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
