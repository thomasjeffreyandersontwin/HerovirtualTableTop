using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Media;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Identities;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public interface IAnimationElement
    {
        string Name { get; set; }
        Character Owner { get; set; }
        int Order { get; set; }
        AnimationType Type { get; set; }
        AnimationResource Resource { get; set; }
        
        string Play(bool persistent = false, Character Target = null);
        void Stop();
    }

    public class AnimationElement : NotifyPropertyChanged, IAnimationElement
    {
        [JsonConstructor]
        private AnimationElement() { }

        public AnimationElement(string name, bool persistent = false, int order = 1, Character owner = null)
        {
            this.Name = name;
            this.Order = order;
            this.Owner = owner;
            this.Persistent = persistent;
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
        private string displayName;
        public virtual string DisplayName
        {
            get
            {
                return displayName;
            }

            set
            {
                displayName = value;
                OnPropertyChanged("DisplayName");
            }
        }

        private bool playOnTargeted;
        public bool PlayOnTargeted
        {
            get
            {
                return playOnTargeted;
            }
            set
            {
                playOnTargeted = value;
                OnPropertyChanged("PlayOnTargeted");
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
        
        //private AnimationResource resource;
        public AnimationResource Resource
        {
            get
            {
                //return resource;
                return GetResource();
            }
            set
            {
                //resource = value;
                SetResource(value);
                OnPropertyChanged("Resource");
            }
        }

        public virtual string Play(bool persistent = false, Character Target = null)
        {
            Character target = Target ?? this.Owner;
            return "Playing " + this.Order + " for " + target.Name;
        }

        public virtual void Stop() { }

        protected virtual AnimationResource GetResource()
        {
            return string.Empty;
        }

        protected virtual void SetResource(AnimationResource value)
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
        [JsonIgnore]
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

        public override string Play(bool persistent = false, Character Target = null)
        {
            System.Threading.Thread.Sleep(Time*1000);
            return string.Empty;
        }

        protected override AnimationResource GetResource()
        {
            return Time.ToString();
        }

        protected override void SetResource(AnimationResource value)
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

        public SoundElement(string name, AnimationResource soundFile, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.SoundFile = soundFile;
            this.Type = AnimationType.Sound;
        }

        private AnimationResource soundFile;
        [JsonIgnore]
        public AnimationResource SoundFile
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
        [JsonIgnore]
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

        private NAudio.Vorbis.VorbisWaveReader soundReader;
        private NAudio.Wave.WaveOut waveOut;
        private Task audioPlaying;

        public override string Play(bool persistent = false, Character Target = null)
        {
            Stop();
            soundReader = new NAudio.Vorbis.VorbisWaveReader(SoundFile);
            waveOut = new NAudio.Wave.WaveOut();
            float dist = 0;
            Character target = Target ?? this.Owner;
            //target.Position.IsWithin(0, Camera.Position, out dist);
            waveOut.Volume = 1.0f; //Determine based on dist
            if (this.Persistent || persistent)
            {
                LoopWaveStream loop = new LoopWaveStream(soundReader);
                waveOut.Init(loop);
                Active = true;
            }
            else
            {
                waveOut.Init(soundReader);
            }
            audioPlaying = Task.Run(() =>
            {
                waveOut.Play();
            });
            return base.Play(this.Persistent || persistent);
        }

        public override void Stop()
        {
            if (Active)
            {
                audioPlaying.Dispose();
                waveOut.Stop();
                Active = false;
            }
        }

        protected override AnimationResource GetResource()
        {
            return SoundFile;
        }

        protected override void SetResource(AnimationResource value)
        {
            SoundFile = value;
        }
    }

    public class MOVElement : AnimationElement
    {
        [JsonConstructor]
        private MOVElement() : base(string.Empty) { }

        public MOVElement(string name, AnimationResource MOVResource, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.MOVResource = MOVResource;
            this.Type = AnimationType.Movement;
        }

        private AnimationResource movResource;
        [JsonIgnore]
        public AnimationResource MOVResource
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

        public override string Play(bool persistent = true, Character Target = null)
        {
            Character target = Target ?? this.Owner;
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            target.Target(false);
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, MOVResource);
            return keyBindsGenerator.CompleteEvent();
        }

        protected override AnimationResource GetResource()
        {
            return MOVResource;
    }

        protected override void SetResource(AnimationResource value)
        {
            MOVResource = value;
        }
    }

    public class FXEffectElement : AnimationElement
    {
        [JsonConstructor]
        private FXEffectElement() : base(string.Empty) { }

        public FXEffectElement(string name, AnimationResource effect, bool persistent = false, bool playWithNext = false,
            int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Effect = effect;
            this.Type = AnimationType.FX;
            this.PlayWithNext = playWithNext;
            Color black = Color.FromRgb(0, 0, 0);
            this.colors = new ObservableCollection<Color>() { black, black, black, black };
        }

        private ObservableCollection<Color> colors;
        public ObservableCollection<Color> Colors
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
        
        private AnimationResource effect;
        [JsonIgnore]
        public AnimationResource Effect
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
                string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", name, FXName, Constants.GAME_COSTUMES_EXT));
                if (File.Exists(newFile))
                return File.ReadAllText(newFile);
                return string.Empty;
            }
        }

        public override string Play(bool persistent = false, Character Target = null)
        {
            Character target = Target ?? this.Owner;
            string keybind = string.Empty;
            string name = target.Name;
            string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            string file = name + Constants.GAME_COSTUMES_EXT;
            string origFile = Path.Combine(location, file);
            string newFolder = Path.Combine(location, name);
            string FXName = ParseFXName(Effect);
            string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", name, FXName, Constants.GAME_COSTUMES_EXT));

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
            if (!re.IsMatch(fileStr))
            {
                fileStr += 
@"CostumePart """"
{
    Fx none
    Geometry none
    Texture1 none
    Texture2 none
    Color1  0,  0,  0
    Color2  0,  0,  0
    Color3  0,  0,  0
    Color4  0,  0,  0
}";
            }
            string output = re.Replace(fileStr, fxNew, 1);
            int fxPos = output.IndexOf(fxNew);
            int colorStart = output.IndexOf("Color1", fxPos);
            int colorEnd = output.IndexOf("}", fxPos);
            string outputStart = output.Substring(0, colorStart - 1);
            string outputEnd = output.Substring(colorEnd);
            string outputColors =
                string.Format("\tColor1 {0}, {1}, {2}" + Environment.NewLine +
                    "\tColor2 {3}, {4}, {5}" + Environment.NewLine +
                    "\tColor3 {6}, {7}, {8}" + Environment.NewLine +
                    "\tColor4 {9}, {10}, {11}" + Environment.NewLine,
                    Colors[0].R, Colors[0].G, Colors[0].B,
                    Colors[1].R, Colors[1].G, Colors[1].B,
                    Colors[2].R, Colors[2].G, Colors[2].B,
                    Colors[3].R, Colors[3].G, Colors[3].B
                    );
            output = outputStart + outputColors + outputEnd;
            File.AppendAllText(newFile, output);
        }

        protected override AnimationResource GetResource()
        {
            return Effect;
        }

        protected override void SetResource(AnimationResource value)
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

        public override string Play(bool persistent = false, Character Target = null)
        {
            if (SequenceType == AnimationSequenceType.And)
            {
                animationElements.Sort(System.ComponentModel.ListSortDirection.Ascending, x => x.Order);
                string retVal = string.Empty;
                foreach (IAnimationElement item in AnimationElements)
                {
                    retVal += item.Play(this.Persistent || persistent, Target);
                }
                return retVal;
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(0, AnimationElements.Count);
                return AnimationElements[chosen].Play(this.Persistent || persistent, Target);
            }
        }
    }

    public class ReferenceAbility : AnimationElement
    {
        public ReferenceAbility(string name, AnimatedAbility reference, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Reference = reference;
            this.Type = AnimationType.Reference;
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

        public override string Play(bool persistent = false, Character Target = null)
        {
            return this.Reference.Play(this.Persistent || persistent, Target ?? this.Owner);
        }

        protected override AnimationResource GetResource()
        {
            return new AnimationResource(this.reference);
        }

        protected override void SetResource(AnimationResource value)
        {
            this.Reference = value;
        }
    }



}
