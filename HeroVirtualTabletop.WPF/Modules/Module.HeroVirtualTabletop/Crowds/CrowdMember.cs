using Module.HeroVirtualTabletop.Characters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framework.WPF.Extensions;
using Module.Shared.Enumerations;
using Module.Shared;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Framework.WPF.Library;
using System.ComponentModel;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowdMember : INotifyPropertyChanged
    {
        string Name { get; set; }
        string OldName { get; }
        ICrowd RosterCrowd { get; set; }
        //HashedObservableCollection<ICrowdMember, string> CrowdMemberCollection { get; set; }

        void Place(IMemoryElementPosition position);
        void SavePosition();
        //string Save(string filename = null);
        ICrowdMember Clone();
    }

    public interface ICrowdMemberModel : ICrowdMember
    {
        new ICrowdModel RosterCrowd { get; set; }
        bool IsExpanded { get; set; }
        bool IsMatched { get; set; }
        void ApplyFilter(string filter);
        void ResetFilter();
        int Order { get; set; }
    }

    public class CrowdMember : Character, ICrowdMember
    {
        private ICrowd rosterCrowd;
        [JsonIgnore]
        public ICrowd RosterCrowd
        {
            get
            {
                return rosterCrowd;
            }

            set
            {
                rosterCrowd = value;
                OnPropertyChanged("RosterCrowd");
            }
        }
        
        private IMemoryElementPosition savedPosition;
        public IMemoryElementPosition SavedPosition
        {
            get
            {
                return savedPosition;
            }
            set
            {
                savedPosition = value;
                OnPropertyChanged("SavedPosition");
            }
        }
        [JsonConstructor]
        public CrowdMember(): base() { }
        
        public CrowdMember(string name): base(name) { }

        public CrowdMember(string name, string surface, IdentityType identityType) : base(name, surface, identityType) { }

        public virtual ICrowdMember Clone()
        {
            CrowdMember crowdMember = this.DeepClone() as CrowdMember;
            return crowdMember;
        }

        public virtual void SavePosition()
        {
            if (this.Position != null)
                this.SavedPosition = this.Position.Clone(false);
        }

        public virtual void Place(IMemoryElementPosition position = null)
        {
            if (!this.HasBeenSpawned)
            {
                this.Spawn();
            }
            if (this.RosterCrowd != null)
            {
                RosterCrowd.Place(this);
            }
            else if (position != null)
            {
                Position = position.Clone(false, (Position as MemoryInstance).GetTargetPointer());
            }
        }

        protected override string GetLabel()
        {
            if (gamePlayer != null && gamePlayer.IsReal)
            {
                return gamePlayer.Label;
            }
            else
            {
                string crowdLabel = string.Empty;
                if (RosterCrowd != null && RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME && RosterCrowd.Name != Constants.NO_CROWD_CROWD_NAME)
                {
                    crowdLabel = " [" + RosterCrowd.Name + "]";
                }
                return Name + crowdLabel;
            }
        }
        
        internal void CheckIfExistsInGame()
        {
            try
            {
                //bool retV = false;
                MemoryElement oldTargeted = new MemoryElement();
                Target();
                MemoryElement currentTargeted = new MemoryElement();
                if (currentTargeted.Label == Label)
                {
                    //retV = true;
                    SetAsSpawned();
                }
                oldTargeted.Target();
                //return retV;
            }
            catch (Exception)
            {
                
            }
        }

        public new string Spawn(bool completeEvent = true)
        {
            CheckIfExistsInGame();
            return base.Spawn();
        }
    }
    public class CrowdMemberModel : CrowdMember, ICrowdMemberModel
    {
        private bool isExpanded;
        [JsonIgnore]
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        private bool isMatched = true;
        [JsonIgnore]
        public bool IsMatched
        {
            get
            {
                return isMatched;
            }
            set
            {
                isMatched = value;
                OnPropertyChanged("IsMatched");
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

        public void ApplyFilter(string filter)
        {
            if (alreadyFiltered == true && IsMatched == true)
            {
                return;
            }
            if (string.IsNullOrEmpty(filter))
            {
                IsMatched = true;
            }
            else
            {
                Regex re = new Regex(filter, RegexOptions.IgnoreCase);
                IsMatched = re.IsMatch(Name);
            }
            IsExpanded = IsMatched;
            alreadyFiltered = true;
        }

        private bool alreadyFiltered = false;
        public void ResetFilter()
        {
            alreadyFiltered = false;
        }

        //[JsonIgnore]
        public new ICrowdModel RosterCrowd
        {
            get
            {
                return base.RosterCrowd as ICrowdModel;
            }
            set
            {
                base.RosterCrowd = value;
                OnPropertyChanged("RosterCrowd");//To check if this line is actually needed
            }
        }

        public override ICrowdMember Clone()
        {
            //CrowdMemberModel crowdMemberModel = this.DeepClone() as CrowdMemberModel;
            CrowdMemberModel crowdMemberModel = new CrowdMemberModel()
            {
                Name = this.Name,
                RosterCrowd = null
            };
            crowdMemberModel.InitializeCharacter();
            
            foreach (AnimatedAbility ab in this.AnimatedAbilities)
            {
                AnimatedAbility clonedAbility = ab.Clone() as AnimatedAbility;
                clonedAbility.Owner = crowdMemberModel;
                crowdMemberModel.AnimatedAbilities.Add(clonedAbility);
            }

            foreach (Identity id in this.AvailableIdentities)
            {
                Identity clonedIdentity = id.Clone();
                if(id.AnimationOnLoad != null)
                {
                    AnimatedAbility animationOnLoad = crowdMemberModel.AnimatedAbilities.Where(aa => aa.Name == id.AnimationOnLoad.Name).FirstOrDefault();
                    clonedIdentity.AnimationOnLoad = animationOnLoad;
                }
                crowdMemberModel.AvailableIdentities.Add(clonedIdentity);
            }
            if(this.DefaultIdentity != null)
            {
                Identity defaultIdentity = crowdMemberModel.AvailableIdentities.Where(i => i.Name == this.DefaultIdentity.Name).FirstOrDefault();
                crowdMemberModel.DefaultIdentity = defaultIdentity;
            }
            if (this.ActiveIdentity != null)
            {
                Identity activeIdentity = crowdMemberModel.AvailableIdentities.Where(i => i.Name == this.ActiveIdentity.Name).FirstOrDefault();
                crowdMemberModel.ActiveIdentity = activeIdentity;
            }

            // Need to add logic for Movements and other option groups
            return crowdMemberModel;
        }
        public override void SavePosition()
        {
            base.SavePosition();
            if (this.RosterCrowd != null)
            {
                this.RosterCrowd.SavePosition(this);
            }
        }

        [JsonConstructor]
        public CrowdMemberModel() : base() { }
        public CrowdMemberModel(string name, int order = 0): base(name)
        {
            this.order = order;
        }
        public CrowdMemberModel(string name, string surface, IdentityType identityType, int order = 0) : base(name, surface, identityType)
        {
            this.order = order;
        }
    }
}
