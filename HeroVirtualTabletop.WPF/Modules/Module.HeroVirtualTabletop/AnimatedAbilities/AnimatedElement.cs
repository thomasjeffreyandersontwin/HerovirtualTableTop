using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public interface IAnimationElement
    {
        string Name { get; set; }
        Character Owner { get; set; }
        int Order { get; set; }
        AnimationType Type { get; set; }
        string Resource { get; set; }
        string TagLine { get; }

        string Play(bool persistent = false);
    }

    public class AnimationElement : NotifyPropertyChanged, IAnimationElement
    {
        [JsonConstructor]
        private AnimationElement() { }

        public AnimationElement(string name, bool persistent = false, int order = 1, Character owner = null, params string[] tags)
        {
            this.Name = name;
            this.Order = order;
            this.Owner = owner;
            this.Persistent = persistent;
            this.tags = new ObservableCollection<string>(tags);
            Tags = new ReadOnlyObservableCollection<string>(this.tags);
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

        private int order;
        public int Order
        {
            get
            {
                return order;
            }

            set
            {
                order = value;
                OnPropertyChanged("Order");
            }
        }

        private Character owner;
        public Character Owner
        {
            get
            {
                return owner;
            }

            set
            {
                owner = value;
                OnPropertyChanged("Owner");
            }
        }

        private AnimationType type;
        public AnimationType Type
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
        
        private bool persistent;
        public bool Persistent
        {
            get
            {
                return persistent;
            }
            set
            {
                persistent = value;
                OnPropertyChanged("Persistent");
            }
        }

        private ObservableCollection<string> tags;
        public ReadOnlyObservableCollection<string> Tags { get; private set; }

        public string TagLine
        {
            get
            {
                return string.Join(", ", tags);
            }
        }

        public string Resource
        {
            get
            {
                return GetResource();
            }
            set
            {
                SetResource(value);
                OnPropertyChanged("Resource");
            }
        }

        public virtual string Play(bool persistent = false)
        {
            return "Playing " + this.Order + " for " + this.Owner.Name;
        }

        protected virtual string GetResource()
        {
            return string.Empty;
        }

        protected virtual void SetResource(string value)
        {
            
        }
    }

    public class PauseElement : AnimationElement
    {
        [JsonConstructor]
        private PauseElement() : base(string.Empty) { }

        public PauseElement(string name, int time, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Time = time;
            this.Type = AnimationType.Pause;
        }

        private int time;
        public int Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
                OnPropertyChanged("Time");
            }
        }

        public override string Play(bool persistent = false)
        {
            System.Threading.Thread.Sleep(Time);
            return string.Empty;
        }

        protected override string GetResource()
        {
            return Time.ToString();
        }

        protected override void SetResource(string value)
        {
            int x;
            if (int.TryParse(value, out x))
                Time = x;
        }
    }

    public class SoundElement : AnimationElement
    {
        [JsonConstructor]
        private SoundElement() : base(string.Empty) { }

        public SoundElement(string name, string soundFile, bool persistent = false, int order = 1, Character owner = null, params string[] tags)
            : base(name, persistent, order, owner, tags)
        {
            this.SoundFile = soundFile;
            this.Type = AnimationType.Sound;
        }

        private string soundFile;
        public string SoundFile
        {
            get
            {
                return soundFile;
            }
            set
            {
                soundFile = value;
                OnPropertyChanged("SoundFile");
            }
        }

        private bool active;
        public bool Active
        {
            get
            {
                return active;
            }
            private set
            {
                active = value;
                OnPropertyChanged("Active");
            }
        }

        private SoundPlayer player;
        public override string Play(bool persistent = false)
        {
            if (Active)
            {
                player.Stop();
                Active = false;
                return string.Empty;
            }
            player = new SoundPlayer(SoundFile);
            if (this.Persistent || persistent)
            {
                player.PlayLooping();
                Active = true;
            }
            else
                player.PlaySync();
            return base.Play(this.Persistent || persistent);
        }

        protected override string GetResource()
        {
            return SoundFile;
        }

        protected override void SetResource(string value)
        {
            SoundFile = value;
        }
    }

    public class MOVElement : AnimationElement
    {
        [JsonConstructor]
        private MOVElement() : base(string.Empty) { }

        public MOVElement(string name, string MOVResource, bool persistent = false, int order = 1, Character owner = null, params string[] tags)
            : base(name, persistent, order, owner, tags)
        {
            this.MOVResource = MOVResource;
            this.Type = AnimationType.Movement;
        }

        private string movResource;
        public string MOVResource
        {
            get
            {
                return movResource;
            }
            set
            {
                movResource = value;
                OnPropertyChanged("MOVResource");
            }
        }

        public override string Play(bool persistent = true)
        {
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, MOVResource);
            return keyBindsGenerator.CompleteEvent();
        }

        protected override string GetResource()
        {
            return MOVResource;
        }

        protected override void SetResource(string value)
        {
            MOVResource = value;
        }
    }

    public class FXEffectElement : AnimationElement
    {
        [JsonConstructor]
        private FXEffectElement() : base(string.Empty) { }

        public FXEffectElement(string name, string effect, bool persistent = false, bool playWithNext = false,
            int order = 1, Character owner = null, params string[] tags)
            : base(name, persistent, order, owner, tags)
        {
            this.Effect = effect;
            this.Type = AnimationType.FX;
            this.PlayWithNext = playWithNext;
            this.Colors = new Color[4];
            this.Colors.Initialize();
        }

        private Color[] colors;
        public Color[] Colors
        {
            get
            {
                return colors;
            }
            set
            {
                colors = value;
                OnPropertyChanged("Colors");
            }
        }

        private string effect;
        public string Effect
        {
            get
            {
                return effect;
            }
            set
            {
                effect = value;
                OnPropertyChanged("Effect");
            }
        }
        
        private bool playWithNext;
        public bool PlayWithNext
        {
            get
            {
                return playWithNext;
            }
            set
            {
                playWithNext = value;
                OnPropertyChanged("PlayWithNext");
            }
        }
        [JsonIgnore]
        public string CostumeText
        {
            get
            {
                if (Owner == null)
                {
                    return string.Empty;
                }
                string name = Owner.Name;
                string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
                string file = name + Constants.GAME_COSTUMES_EXT;
                string newFolder = Path.Combine(location, name);
                string FXName = ParseFXName(Effect);
                string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", newFolder, FXName, Constants.GAME_COSTUMES_EXT));
                if (File.Exists(newFile))
                    return File.ReadAllText(newFile);
                return string.Empty;
            }
        }

        public override string Play(bool persistent = false)
        {
            string keybind = string.Empty;
            string name = Owner.Name;
            string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            string file = name + Constants.GAME_COSTUMES_EXT;
            string origFile = Path.Combine(location, file);
            string newFolder = Path.Combine(location, name);
            string FXName = ParseFXName(Effect);
            string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", newFolder, FXName, Constants.GAME_COSTUMES_EXT));

            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(newFolder);
            }
            if (File.Exists(newFile))
            {
                File.Delete(newFile);
            }
            if (File.Exists(origFile))
            {
                insertFXIntoCharacterCostumeFile(origFile, newFile);
                string fxCostume = Path.Combine(name, string.Format("{0}_{1}", name, FXName));
                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, fxCostume);
                if (PlayWithNext == false)
                {
                    keybind = keyBindsGenerator.CompleteEvent();
                }
            }
            if (Persistent || persistent)
            {
                archiveOriginalCostumeFileAndSwapWithModifiedFile(name, newFile);
            }
            return keybind;
        }

        private void archiveOriginalCostumeFileAndSwapWithModifiedFile(string name, string newFile)
        {
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + Constants.GAME_COSTUMES_EXT);
            string archFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + "_original" + Constants.GAME_COSTUMES_EXT);
            if (File.Exists(archFile))
            {
                File.Copy(origFile, archFile, true);
            }
            File.Copy(newFile, origFile, true);

        }

        private string ParseFXName(string effect)
        {
            Regex re = new Regex(@"\w+.fx");
            return re.Match(effect).Value;
        }

        private void insertFXIntoCharacterCostumeFile(string origFile, string newFile)
        {
            string fileStr = File.ReadAllText(origFile);
            string fxNone = "Fx none";
            string fxNew = "Fx " + Effect;
            Regex re = new Regex(Regex.Escape(fxNone));
            string output = re.Replace(fileStr, fxNew, 1);
            int fxPos = output.IndexOf(fxNew);
            int colorStart = output.IndexOf("Color1", fxPos);
            int colorEnd = output.IndexOf("}", fxPos);
            string outputStart = output.Substring(0, colorStart - 1);
            string outputEnd = output.Substring(colorEnd);
            string outputColors =
                string.Format("Color1 {0}, {1}, {2}\n" +
                    "\tColor2 {3}, {4}, {5}\n" +
                    "\tColor3 {6}, {7}, {8}\n" +
                    "\tColor4 {9}, {10}, {11}\n",
                    Colors[0].R, Colors[0].G, Colors[0].B,
                    Colors[1].R, Colors[1].G, Colors[1].B,
                    Colors[2].R, Colors[2].G, Colors[2].B,
                    Colors[3].R, Colors[3].G, Colors[3].B
                    );
            output = outputStart + outputColors + outputEnd;
            File.AppendAllText(newFile, output);
        }

        protected override string GetResource()
        {
            return Effect;
        }

        protected override void SetResource(string value)
        {
            Effect = value;
        }
    }
    
    public class SequenceElement : AnimationElement
    {
        [JsonConstructor]
        private SequenceElement() : base(string.Empty)
        {
            Initialize();
        }

        public SequenceElement(string name, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            Initialize();
            this.SequenceType = seqType;
            this.Type = AnimationType.Sequence;
            this.lastOrder = 0;
        }

        private void Initialize()
        {
            this.animationElements = new HashedObservableCollection<IAnimationElement, string>(x => x.Name, x => x.Order);
            this.AnimationElements = new ReadOnlyHashedObservableCollection<IAnimationElement, string>(animationElements);
        }

        private AnimationSequenceType sequenceType;
        public AnimationSequenceType SequenceType
        {
            get
            {
                return sequenceType;
            }
            set
            {
                sequenceType = value;
                OnPropertyChanged("SequenceType");
            }
        }

        [JsonProperty(PropertyName = "AnimationElements")]
        private HashedObservableCollection<IAnimationElement, string> animationElements;
        [JsonIgnore]
        public ReadOnlyHashedObservableCollection<IAnimationElement, string> AnimationElements { get; private set; }

        private int lastOrder;
        public int LastOrder
        {
            get
            {
                return lastOrder;
            }
        }

        public void AddAnimationElement(IAnimationElement element, int order = 0)
        {
            this.lastOrder++;
            element.Owner = this.Owner;
            if (order == 0)
            element.Order = this.LastOrder;
            else
                element.Order = order;
            this.animationElements.Add(element);
            this.animationElements.Sort();
        }

        public void RemoveAnimationElement(IAnimationElement element)
        {
            foreach (IAnimationElement elem in animationElements.Where(a => a.Order > element.Order))
                elem.Order -= 1;
            animationElements.Remove(element);
        }

        public void RemoveAnimationElement(string name)
        {
            IAnimationElement element = animationElements[name];
            RemoveAnimationElement(element);
        }

        public override string Play(bool persistent = false)
        {
            if (SequenceType == AnimationSequenceType.And)
            {
                animationElements.Sort(System.ComponentModel.ListSortDirection.Ascending, x => x.Order);
                string retVal = string.Empty;
                foreach (IAnimationElement item in AnimationElements)
                {
                    retVal += item.Play(this.Persistent || persistent);
                }
                return retVal;
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(AnimationElements.Count - 1);
                return AnimationElements[chosen].Play(this.Persistent || persistent);
            }
        }
    }

    public class ReferenceAbility : AnimationElement
    {
        public ReferenceAbility(string name, AnimationElement reference, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Reference = reference;
            this.Type = AnimationType.Reference;
        }

        private AnimationElement reference;
        public AnimationElement Reference
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

        public override string Play(bool persistent = false)
        {
            this.Reference.Owner = this.Owner;
            return this.Reference.Play(this.Persistent || persistent);
        }
    }



}
