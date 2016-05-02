using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.WPF.Library;
using Module.Shared.Enumerations;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.HeroVirtualTabletop.Identities
{
    /// <summary>
    /// Represents a model or a costume for characters
    /// </summary>
    public class Identity : NotifyPropertyChanged, ICharacterOption
    {
        private KeyBindsGenerator keyBindsGenerator;

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        
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

        private bool isDefault;

        public bool IsDefault
        {
            get
            {
                return isDefault;
            }
            set
            {
                isDefault = value;
                OnPropertyChanged("IsDefault");
            }
        }

        private bool isActive;

        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        } 
        
        /// <param name="surface">Represents the name of the model or the costume to load</param>
        /// <param name="type">The type of the identity, it can be either a Model or a Costume</param>
        /// <param name="name">The name to be displayed for this identity</param>
        public Identity(string surface, IdentityType type, string name = null)
        {
            Type = type;
            Surface = surface;
            this.Name = name;
            this.keyBindsGenerator = new KeyBindsGenerator();
        }   

        public string Render()
        {
            switch (Type)
            {
                case IdentityType.Model:
                    {
                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.BeNPC, Surface);
                        break;
                    }
                case IdentityType.Costume:
                    {
                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, Surface);
                        break;
                    }
            }
            return keyBindsGenerator.CompleteEvent();
        }

        //TODO AnimationOnLoad
    }
}
