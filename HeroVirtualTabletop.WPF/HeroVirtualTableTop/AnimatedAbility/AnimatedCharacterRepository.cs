using System;
using System.Collections.Generic;
using System.Linq;
using HeroVirtualTableTop.Crowd;

namespace HeroVirtualTableTop.AnimatedAbility
{
    internal class AnimatedCharacterRepositoryImpl : CrowdRepositoryImpl, AnimatedCharacterRepository
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

        public AnimatedCharacter NewCrowd(string name = "Character")
        {
            throw new NotImplementedException();
        }
    }
}