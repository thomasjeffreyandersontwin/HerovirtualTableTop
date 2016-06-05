using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimationResource
    {
        public string Tag
        {
            get;
            set;
        }
        public string ResourceName
        {
            get;
            set;
        }
    }

    public class MovResource : AnimationResource
    {
        
    }

    public class FXResouce:AnimationResource
    {
        public string EffectName
        {
            get;
            set;
        }
    }

    public class SoundResource:AnimationResource
    {
        public string FileName
        {
            get;
            set;
        }
    }
}
