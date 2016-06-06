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

        public AnimationResource(string value, string name, params string[] tags) : this(value)
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
        }

        #region String behaviour
        
        public static implicit operator string(AnimationResource s)
        {
            return s == null ? string.Empty : s.Value.ToString();
        }

        public static implicit operator AnimationResource(string s)
        {
            return new AnimationResource(s);
        }

        #endregion
    }
}
