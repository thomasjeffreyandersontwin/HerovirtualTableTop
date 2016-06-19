using Framework.WPF.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AnimationResource : NotifyPropertyChanged
    {
        [JsonConstructor]
        private AnimationResource()
        {
            this.tags = new ObservableCollection<string>();
            Tags = new ReadOnlyObservableCollection<string>(this.tags);
        }

        public AnimationResource(string value): this()
        {
            this.Value = value;
        }

        public AnimationResource(AnimatedAbility reference) : this()
        {
            this.Reference = reference;
        }

        public AnimationResource(string value, string name, params string[] tags) : this(value)
        {
            this.Name = name;
            this.tags.AddRange(tags);
        }

        public AnimationResource(AnimatedAbility reference, string name, params string[] tags) : this(reference)
        {
            this.Name = name;
            this.tags.AddRange(tags);
        }

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
                OnPropertyChanged("Name");
            }
        }

        private string value;
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                OnPropertyChanged("Value");
            }
        }

        private AnimatedAbility reference;
        public AnimatedAbility Reference
        {
            get
            {
                return reference;
            }
            set
            {
                reference = value;
                OnPropertyChanged("Reference");
            }
        }

        [JsonProperty(PropertyName = "Tags")]
        private ObservableCollection<string> tags;
        [JsonIgnore]
        public ReadOnlyObservableCollection<string> Tags { get; private set; }

        [JsonIgnore]
        public string TagLine
        {
            get
            {
                return tags != null ? string.Join(", ", tags) : "";
            }
            set
            {
                tags = new ObservableCollection<string>(value.Split(',').Select((s) => { return s.Trim(); }));
                OnPropertyChanged("TagLine");
            }
        }

        #region String behaviour
        
        public static implicit operator string(AnimationResource s)
        {
            return s == null ? string.Empty : ( s.Value != null ? s.Value.ToString() : s.Reference.Name);
        }

        public static implicit operator AnimatedAbility(AnimationResource s)
        {
            return s == null ? null : (s.Reference != null ? s.Reference : null);
        }

        public static implicit operator AnimationResource(string s)
        {
            return new AnimationResource(s);
        }

        public static implicit operator AnimationResource(AnimatedAbility s)
        {
            return new AnimationResource(s);
        }

        #endregion

        #region Equality Comparer and Operator Overloading
        public static bool operator ==(AnimationResource a, AnimationResource b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a.Value == b.Value && a.Reference == b.Reference && a.Name == b.Name;
        }

        public static bool operator !=(AnimationResource a, AnimationResource b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            bool areEqual = false;
            if(obj is AnimationResource)
                areEqual = this == (AnimationResource)obj;
            return areEqual;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
