using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using HeroVirtualTableTop.Desktop;
using System.Windows.Media;
using IrrKlang;
using System.Threading;
namespace HeroVirtualTableTop.AnimatedAbility
{
    public abstract class AnimationElementImpl : AnimationElement
    {
        public AnimationElementImpl(AnimatedCharacter owner)
        {
            Target = owner;
        }
        public AnimationElementImpl()
        {
        }
        public int Order { get; set; }
        public AnimatedCharacter Target { get; set; }
        public bool PlayWithNext { get; set; }
        public bool Persistent { get; set; }
        public AnimationSequence ParentSequence { get; set; }
        public void DeactivatePersistent()
        {
            throw new NotImplementedException();
        }

        public AnimationElement Clone()
        {
            throw new NotImplementedException();
        }

        public void Play(AnimatedCharacter target)
        {
            if (target.IsTargeted == false)
            {
                target.Target(false);
            }
            PlayResource(target);
        }
        public void Play(List<AnimatedCharacter> targets)
        {
            foreach (AnimatedCharacter target in targets)
            {
                Play(target);
            }
        }
        public abstract void PlayResource(AnimatedCharacter target);
        public void Play()
        {
            Play(Target);
        }

        public abstract void StopResource(AnimatedCharacter target);
        public void Stop()
        {
            Stop(Target);
        }
        public void Stop(AnimatedCharacter target)
        {
            if (target.IsTargeted == false)
            {
                target.Target(false);
            }
            StopResource(target);
        }

        public List<AnimationElement> AddToFlattendedList(List<AnimationElement> list)
        {
            throw new NotImplementedException();
        }

    }

    public class MovElementImpl : AnimationElementImpl, MovElement
    {
        public MovElementImpl(AnimatedCharacter owner, MovResource resource) : base(owner)
        {
            this.Mov = resource;
        }

        public MovElementImpl() : base()
        {

        }
        public MovResource Mov { get; set; }

        public override void PlayResource(AnimatedCharacter target)
        {
            KeyBindCommandGenerator generator = target.Generator;
            string[] para = { Mov.FullResourcePath };
            generator.GenerateDesktopCommandText(DesktopCommand.Move, para);
            if (PlayWithNext == false)
            {
                generator.CompleteEvent();
            }
        }
        public override void StopResource(AnimatedCharacter target)
        { }
    }

    public class FXElementImpl : AnimationElementImpl, FXElement
    {
        public static string COSTUME_DIR = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, "costumes");
        public FXElementImpl(AnimatedCharacter owner, FXResource resource) : base(owner)
        {
            this.FX = resource;
        }

        public FXElementImpl() : base()
        {

        }

        public FXResource FX { get; set; }
        public Color Color1 { get; set; }
        public Color Color2 { get; set; }
        public Color Color3 { get; set; }
        public Color Color4 { get; set; }
        public Position AttackDirection { get; set; }

        public string CostumeText
        {

            get
            {

                return File.ReadAllText(CostumeFilePath);
            }
        }
        public string ModifiedCostumeText
        {
            get
            {
                if (File.Exists(ModifiedCostumeFilePath))
                {
                    return File.ReadAllText(ModifiedCostumeFilePath);
                }
                else {
                    return null;
                }
            }
        }
        public string ModifiedCostumeFilePath
        {
            get
            {
                string costume_name = null;
                if (Target.Identities != null && Target.Identities.Active != null)
                {
                    costume_name = Target.Name + "_" + Target.Identities.Active.Surface + "_Modified.costume";
                }
                else {
                    costume_name = Target.Name + "_Modified.costume";
                }
                return Path.Combine(COSTUME_DIR, costume_name);
            }
        }
        public string CostumeFilePath
        {
            get
            {
                string costume_name = null;
                if (Target.Identities != null && Target.Identities.Active != null)
                {
                    costume_name = Target.Identities.Active.Surface;
                }
                else {
                    costume_name = Target.Name + ".costume";
                }
                return Path.Combine(COSTUME_DIR, costume_name);
            }
        }

        public bool ModifiedCostumeContainsFX
        {
            get
            {
                if (File.Exists(ModifiedCostumeFilePath))
                {
                    string fxString = "Fx " + this.FX.FullResourcePath;
                    Regex re = new Regex(Regex.Escape(fxString));
                    return re.IsMatch(ModifiedCostumeText);
                }
                else
                {
                    return false;
                }
            }
        }
        public override void PlayResource(AnimatedCharacter target)
        {
            if (!File.Exists(CostumeFilePath))
            {
                return;
            }
            if (File.Exists(ModifiedCostumeFilePath) == false)
            {
                File.Copy(CostumeFilePath, ModifiedCostumeFilePath);
            }
            string fileStr = ModifiedCostumeText;

            fileStr = removePreviouslyLoadedFX(fileStr);
            fileStr = addEmptyCostumePart(fileStr);
            fileStr = insertFXIntoCostume(fileStr);

            File.Delete(ModifiedCostumeFilePath);
            File.AppendAllText(ModifiedCostumeFilePath, fileStr);

            loadCostumeWithFxInIt(target);
            Target.LoadedFXs.Add(this);
        }

        private string removePreviouslyLoadedFX(string fileStr)
        {
            Regex re = null;
            foreach (FXElement fx in Target.LoadedFXs)
            {
                if (fx.Persistent != true)
                {
                    re = new Regex(Regex.Escape(fx.FX.FullResourcePath));
                    if (re.IsMatch(fileStr))
                    {
                        fileStr = fileStr.Replace(fx.FX.FullResourcePath, "none");
                    }
                }

            }

            return fileStr;
        }
        private void loadCostumeWithFxInIt(AnimatedCharacter target)
        {
            KeyBindCommandGenerator generator = target.Generator;
            string[] para = { ModifiedCostumeFilePath };
            generator.GenerateDesktopCommandText(DesktopCommand.LoadCostume, para);
            if (PlayWithNext == false)
            {
                generator.CompleteEvent();
            }
        }
        private string insertFXIntoCostume(string fileStr)
        {
            string fxNew = "Fx " + FX.FullResourcePath;
            Regex re = null;
            string fxNone = "Fx none";
            re = new Regex(Regex.Escape(fxNone));

            fileStr = re.Replace(fileStr, fxNew, 1);
            int fxPos = fileStr.IndexOf(fxNew);
            int colorStart = fileStr.IndexOf("Color1", fxPos);
            int colorEnd = fileStr.IndexOf("}", fxPos);
            string outputStart = fileStr.Substring(0, colorStart - 1);
            string outputEnd = fileStr.Substring(colorEnd);
            string outputColors =
            string.Format("\tColor1 {0}, {1}, {2}" + Environment.NewLine +
                    "\tColor2 {3}, {4}, {5}" + Environment.NewLine +
                    "\tColor3 {6}, {7}, {8}" + Environment.NewLine +
                    "\tColor4 {9}, {10}, {11}" + Environment.NewLine,
                    Color1.R, Color1.G, Color1.B,
                    Color2.R, Color2.G, Color2.B,
                   Color3.R, Color3.G, Color3.B,
                    Color4.R, Color4.G, Color4.B
            );
            fileStr = outputStart + outputColors + outputEnd;
            return fileStr;
        }
        private string addEmptyCostumePart(string fileStr)
        {
            Regex re;
            string fxNone = "Fx none";
            re = new Regex(Regex.Escape(fxNone));
            if (!re.IsMatch(ModifiedCostumeText))
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

            return fileStr;
        }

        public void BuildCostumeFileThatWillPlayFX()
        {
            throw new NotImplementedException();
        }
        public override void StopResource(AnimatedCharacter target)
        {
            removePreviouslyLoadedFX(ModifiedCostumeText);
        }
    }

    public class SoundElementImpl : AnimationElementImpl, SoundElement
    {

        public static string SOUND_DIR = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, "sound");
        public SoundElementImpl(AnimatedCharacter owner, SoundResource resource) : base(owner)
        {
            this.Sound = resource;
        }

        public SoundElementImpl() : base() { }
        public SoundResource Sound { get; set; }
        public Position PlayingLocation
        {
            get
            {
                return null;
            }

            set
            {

            }
        }
        private bool _active;
        public bool Active
        {
            get { return _active; }

            set
            {
                _active = value;
                if (_active == false)
                {
                    if (UpdateSoundPlayingPositionTimer != null)
                        UpdateSoundPlayingPositionTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    if (SoundEngine != null)
                        SoundEngine.StopAllSounds();
                }
            }
        }

        public string SoundFileName
        {
            get
            {
                return Path.Combine(Settings.Default.CityOfHeroesGameDirectory, SOUND_DIR) + (string)Sound.FullResourcePath;
            }
        }

        System.Threading.Timer UpdateSoundPlayingPositionTimer;
        public override void PlayResource(AnimatedCharacter target)
        {

            string soundFileName = SoundFileName;
            if (soundFileName == null)
            {
                return;
            }
            if (this.Persistent)
            {
                Active = true;
            }

            Position targetPositionVector = target.Position;
            Position camPositionVector = target.Camera.Position;

            SoundEngine.SetListenerPosition(camPositionVector.X, camPositionVector.Y, camPositionVector.Z, 0, 0, 1);
            SoundEngine.Default3DSoundMinDistance = 10f;
            SoundEngine.Play3D(soundFileName, targetPositionVector.X, targetPositionVector.Y, targetPositionVector.Z, Persistent);

            if (Persistent)
            {
                UpdateSoundPlayingPositionTimer = new Timer(this.UpdateListenerPositionBasedOnCamera_CallBack, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                CancellationToken tokenSrc = new CancellationToken();
                Task.Factory.StartNew(() =>
                {
                    if (this.Active)
                    {
                        UpdateSoundPlayingPositionTimer.Change(1, System.Threading.Timeout.Infinite);
                    }
                }, tokenSrc);
            }
        }
        private void UpdateListenerPositionBasedOnCamera_CallBack(object state)
        {
            if (this.Active)
            {
                Position camPosition = Target.Camera.Position;
                SoundEngine.SetListenerPosition(camPosition.X, camPosition.Y, camPosition.Z, 0, 0, 1);
                UpdateSoundPlayingPositionTimer.Change(500, System.Threading.Timeout.Infinite);
            }
        }

        public SoundEngineWrapper SoundEngine { get; set; }


        public override void StopResource(AnimatedCharacter target)
        {
            if (Active)
            {
                Active = false;
            }
            if (UpdateSoundPlayingPositionTimer != null)
                UpdateSoundPlayingPositionTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            if (SoundEngine != null)
                SoundEngine.StopAllSounds();
        }
    
    }
    public class SoundEngineWrapperImpl : SoundEngineWrapper
    {
        private ISoundEngine engine = new ISoundEngine();
        public void SetListenerPosition(float posX, float posY, float posZ, float lookDirX, float lookDirY, float lookDirZ)
        {
            engine.SetListenerPosition(posX, posY, posZ, lookDirX, lookDirY, lookDirZ);
        }
        public void StopAllSounds()
        {
            engine.StopAllSounds();
        }
        public float Default3DSoundMinDistance
        {
            set
            {
                engine.Default3DSoundMinDistance = value;
            }
        }

        public void Play3D(string soundFilename, float posX, float posY, float posZ, bool playLooped)
        {
            Play3D(soundFilename, posX, posY, posZ, playLooped);
        }

    }

    public class PauseElementImpl : AnimationElementImpl, PauseElement
    {
        public int CloseDistanceDelay { get; set; }

        private int _dur;
        public int Duration {
            get
            {
                if (IsUnitPause == false)
                {
                    return _dur;
                }
                else
                {

                    DelayManager delayManager = new DelayManager(this);
                    float distance = Target.Position.DistanceFrom(TargetPosition);
                    int delay =   (int)delayManager.GetDelayForDistance(distance);
                    return delay;

                }
            }
            set
            {
                _dur = value;
            }

        }
        
        public int LongDistanceDelay { get; set; }
        public int MediumDistanceDelay { get; set; }
        public int ShortDistanceDelay { get; set; }
       
       public  bool IsUnitPause { get; set; }

        public Position TargetPosition { get; set; }

        public override void PlayResource(AnimatedCharacter target)
        {
            //why are we doing this todo
            System.Threading.Thread.Sleep(Duration);
        }
        public override void StopResource(AnimatedCharacter target)
        {
            
        }
    }
    public class DelayManager
    {
        private Dictionary<double, double> distanceDelayMappingDictionary;

        private PauseElement pauseElement;

        public DelayManager(PauseElement pauseElement)
        {
            this.pauseElement = pauseElement;
            this.ConstructDelayDictionary();
        }

        private void ConstructDelayDictionary()
        {
            distanceDelayMappingDictionary = new Dictionary<double, double>();
            distanceDelayMappingDictionary.Add(10, pauseElement.CloseDistanceDelay);
            distanceDelayMappingDictionary.Add(20, pauseElement.ShortDistanceDelay);
            distanceDelayMappingDictionary.Add(50, pauseElement.MediumDistanceDelay);
            distanceDelayMappingDictionary.Add(100, pauseElement.LongDistanceDelay);
            distanceDelayMappingDictionary.Add(15, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[10], distanceDelayMappingDictionary[20], 0.70));
            distanceDelayMappingDictionary.Add(30, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[20], distanceDelayMappingDictionary[50], 0.6));
            distanceDelayMappingDictionary.Add(40, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[20], distanceDelayMappingDictionary[50], 0.875));
            distanceDelayMappingDictionary.Add(60, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.4));
            distanceDelayMappingDictionary.Add(70, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.5));
            distanceDelayMappingDictionary.Add(80, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.7));
            distanceDelayMappingDictionary.Add(90, GetPercentageDelayBetweenTwoDelays(distanceDelayMappingDictionary[50], distanceDelayMappingDictionary[100], 0.87));
        }

        private double GetPercentageDelayBetweenTwoDelays(double firstDelay, double secondDelay, double percentage)
        {
            return firstDelay - (firstDelay - secondDelay) * percentage;
        }

        private double GetLinearDelayBetweenTwoDelays(double firstDistance, double firstDelay, double secondDistance, double secondDelay, double targetDistance)
        {
            // y - y1 = m(x - x1); m = (y2 - y1)/(x2 - x1)
            var m = (secondDelay - firstDelay) / (secondDistance - firstDistance);
            double targetDelay = firstDelay + m * (targetDistance - firstDistance);
            return targetDelay;
        }

        public double GetDelayForDistance(double distance)
        {
            double targetDelay = 0;
            if (distanceDelayMappingDictionary.ContainsKey(distance))
            {
                targetDelay = distanceDelayMappingDictionary[distance];
            }
            else if (distance <= 10)
            {
                targetDelay = distanceDelayMappingDictionary[10];
            }
            else if (distance < 100)
            {
                double nearestLowerDistance = distanceDelayMappingDictionary.Keys.OrderBy(d => d).Last(d => d < distance);
                double nearestHigherDistance = distanceDelayMappingDictionary.Keys.OrderBy(d => d).First(d => d > distance);
                targetDelay = GetLinearDelayBetweenTwoDelays(nearestLowerDistance, distanceDelayMappingDictionary[nearestLowerDistance], nearestHigherDistance, distanceDelayMappingDictionary[nearestHigherDistance], distance);
            }
            else
            {
                double baseDelayDiff = distanceDelayMappingDictionary[50] - distanceDelayMappingDictionary[100];
                double baseDelay = distanceDelayMappingDictionary[100];
                int nearestLowerHundredMultiplier = (int)(distance / 100);
                int nearestHigherHundredMultiplier = nearestLowerHundredMultiplier + 1;
                double nearestLowerHundredDistance = nearestLowerHundredMultiplier * 100;
                double nearestHigherHundredDistance = nearestHigherHundredMultiplier * 100;
                double currentLowerDelay = baseDelay;
                double currentHigherDelay = baseDelay - baseDelayDiff * 0.5;
                for (int i = 1; i < nearestLowerHundredMultiplier; i++)
                {
                    baseDelayDiff = currentLowerDelay - currentHigherDelay;
                    currentLowerDelay = currentHigherDelay;
                    currentHigherDelay = currentLowerDelay - baseDelayDiff * 0.5;
                }
                targetDelay = GetLinearDelayBetweenTwoDelays(nearestLowerHundredDistance, currentLowerDelay, nearestHigherHundredDistance, currentHigherDelay, distance);
            }
            double targetDistance = distance < 10 ? 10 : distance;
            return targetDelay * targetDistance;
        }
    }

    public class SequenceElementImpl :  AnimationElementImpl, SequenceElement
    {
        private AnimationSequence _sequencer;
        public List<AnimationElement> AnimationElements
        {
            get
            {
                return Sequencer.AnimationElements;
            }
        }
        public AnimationSequence Sequencer
        {
            get
            {
                if (_sequencer == null)
                {
                    _sequencer = new AnimationSequenceImpl(this.Target);
                }
                return _sequencer;
            }

        }
        public SequenceType Type
        {
            get
            {
                return _sequencer.Type;
            }

            set
            {
                Sequencer.Type = value;
            }
        }
        public void InsertAnimationElement(AnimationElement animationElement)
        {
            _sequencer.InsertAnimationElement(animationElement);
        }
        public void InsertAnimationElementAfter(AnimationElement toInsert, AnimationElement moveAfter)
        {
            _sequencer.InsertAnimationElementAfter(toInsert, moveAfter);
        }
 
        public void RemoveAnimationElement(AnimationElement animationElement)
        {
            _sequencer.RemoveAnimationElement(animationElement);
        }
        public override void PlayResource(AnimatedCharacter target)
        {
            _sequencer.PlayResource(target);
        }
        public override void StopResource(AnimatedCharacter target)
        {
            _sequencer.StopResource(target);
        }
    }

    public class AnimationSequenceImpl : AnimationSequence
    {
         public int Order { get; set; }
        List<AnimationElement> _animationElements;
        public List<AnimationElement> AnimationElements
        {
            get
            {
                if (_animationElements == null)
                {
                    _animationElements = new List<AnimationElement>();
                }
                return (from element in _animationElements orderby element.Order select element).ToList();
            }
        }
        private SequenceType _type = SequenceType.And;
        public SequenceType Type
        {
            get; set;
        }
        public AnimationSequenceImpl(AnimatedCharacter target)
        {
            Target = target;
        }
        public AnimatedCharacter Target { get; set; }
        public void InsertAnimationElement(AnimationElement animationElement)
        {
            animationElement.Order = AnimationElements.Count;
            animationElement.Target = Target;
            animationElement.ParentSequence = this;
            _animationElements.Add(animationElement);
        }

        public void InsertAnimationElementAfter(AnimationElement toInsert, AnimationElement insertAfter)
        {
            if (toInsert.ParentSequence == insertAfter.ParentSequence)
            {
                if (insertAfter.ParentSequence == this)
                {

                    int originalOrder = toInsert.Order;
                    foreach (AnimationElement element in from element in AnimationElements
                                                         orderby element.Order
                                                         where element.Order > originalOrder
                                                         select element)
                    {
                        element.Order = element.Order - 1;

                    }

                    int destinationOrder = insertAfter.Order + 1;
                    List<AnimationElement> list = (from element in AnimationElements
                                                   orderby element.Order descending
                                                   where element.Order >= destinationOrder
                                                   select element).ToList();
                    foreach (AnimationElement element in list)
                    {
                        element.Order = element.Order + 1;

                    }
                    toInsert.Order = destinationOrder;
                }
                else
                {
                    throw new ArgumentException("the target elements parent does not match the parent you are trying to add to");
                }
            }
            else
            {
                if (insertAfter.ParentSequence == this)
                {
                    toInsert.ParentSequence.RemoveAnimationElement(toInsert);
                    InsertAnimationElement(toInsert);

                    InsertAnimationElementAfter(toInsert, insertAfter);
                }
                else
                {
                    throw new ArgumentException("the target elements parent does not match the parent you are trying to add to");
                }
            }
        }

        public void RemoveAnimationElement(AnimationElement animationElement)
        {
            _animationElements.Remove(animationElement);

            int removeOrder = animationElement.Order;
            foreach (AnimationElement e in from e in AnimationElements orderby e.Order where e.Order > removeOrder select e)
            {
                e.Order = e.Order - 1;
            }
        }


        public void PlayResource(AnimatedCharacter target)
        { 
            //to do add asyncronous code
            if (Type == SequenceType.And)
            {
                foreach (AnimationElement e in from e in AnimationElements orderby e.Order select e)
                {
                    e.Play(target);
                };
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(0, AnimationElements.Count);
                AnimationElements[chosen].Play(target);

            }
        }

        public void StopResource(AnimatedCharacter target)
        {
            //to do add asyncronous code
            if (Type == SequenceType.And)
            {
                foreach (AnimationElement e in from e in AnimationElements orderby e.Order select e)
                {
                    e.Play(target);
                };
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(0, AnimationElements.Count);
                AnimationElements[chosen].Play(target);

            }
        }


    }
}
