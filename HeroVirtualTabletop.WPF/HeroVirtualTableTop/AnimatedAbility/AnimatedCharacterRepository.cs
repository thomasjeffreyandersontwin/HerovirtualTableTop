using System;
using System.Collections.Generic;
using System.Linq;
using HeroVirtualTableTop.Crowd;
using Framework.WPF.Library;

namespace HeroVirtualTableTop.AnimatedAbility
{
    public class AnimatedCharacterRepositoryImpl : NotifyPropertyChanged, AnimatedCharacterRepository
    {
        public AnimatedCharacterRepositoryImpl()
        {
            Characters = new List<AnimatedCharacter>();
        }

        public Dictionary<string, AnimatedCharacter> CharacterByName
        {
            get { return Characters.ToDictionary(x => x.Name, y => y); }
        }

        public List<AnimatedCharacter> Characters { get; }
    }
}