﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.WPF.Library;
using Module.Shared.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Newtonsoft.Json;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace Module.HeroVirtualTabletop.Identities
{
    /// <summary>
    /// Represents a model or a costume for characters
    /// </summary>
    public class Identity : CharacterOption
    {
        #region ComparisonOverrides

        public static bool operator ==(Identity a, Identity b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a.Name == b.Name && a.Surface == b.Surface && a.Type == b.Type;
        }

        public static bool operator !=(Identity a, Identity b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is Identity)
                return this == (Identity)obj;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        protected KeyBindsGenerator keyBindsGenerator;

        private string surface;

        public string Surface
        {
            get
            {
                return surface;
            }
            set
            {
                surface = value;
                OnPropertyChanged("Surface");
            }
        }

        private IdentityType type;
        public IdentityType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        private AnimatedAbility animationOnLoad = null;
        public AnimatedAbility AnimationOnLoad
        {
            get
            {
                return animationOnLoad;
            }
            set
            {
                animationOnLoad = value;
                OnPropertyChanged("AnimationOnLoad");
            }
        }

        /// <param name="surface">Represents the name of the model or the costume to load</param>
        /// <param name="type">The type of the identity, it can be either a Model or a Costume</param>
        /// <param name="name">The name to be displayed for this identity</param>
        public Identity(string surface, IdentityType type, string name = null)
        {
            Type = type;
            Surface = surface;
            this.Name = name == null ? surface : name;
            //isDefault = false;
            //isActive = false;
            this.keyBindsGenerator = new KeyBindsGenerator();
        }

        public string Render(bool completeEvent = true)
        {
            string keybind = string.Empty;
            if (completeEvent)
            {
                if (AnimationOnLoad != null)
                {
                    if (Type == IdentityType.Costume)
                        AnimationOnLoad.PlayOnLoad(false, null, Surface);
                    else
                        AnimationOnLoad.Play();
                }
            }
            switch (Type)
            {
                case IdentityType.Model:
                    {
                        keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BeNPC, Surface);
                        break;
                    }
                case IdentityType.Costume:
                    {
                        keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, Surface);
                        break;
                    }
            }
            if (completeEvent)
            {
                keybind = keyBindsGenerator.CompleteEvent();
            }
            return keybind;
        }

        public string RenderWoAnimation(bool completeEvent = true)
        {
            string keybind = string.Empty;
            switch (Type)
            {
                case IdentityType.Model:
                    {
                        keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BeNPC, Surface);
                        break;
                    }
                case IdentityType.Costume:
                    {
                        keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, Surface);
                        break;
                    }
            }
            if (completeEvent)
            {
                keybind = keyBindsGenerator.CompleteEvent();
            }
            return keybind;
        }

        public Identity Clone()
        {
            Identity clonedIdentity = new Identity(this.Surface, this.Type, this.Name);
            return clonedIdentity;
        }
    }
}
