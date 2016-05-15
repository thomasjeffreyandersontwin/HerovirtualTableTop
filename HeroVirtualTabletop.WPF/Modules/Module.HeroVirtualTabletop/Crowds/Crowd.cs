using Framework.WPF.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framework.WPF.Extensions;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class Crowd : NotifyPropertyChanged, ICrowdMember
    {
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

        private Crowd rosterCrowd;
        [JsonIgnore]
        public Crowd RosterCrowd
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

        private ObservableCollection<ICrowdMember> crowdMemberCollection;
        public ObservableCollection<ICrowdMember> CrowdMemberCollection
        {
            get
            {
                return crowdMemberCollection;
            }
            set
            {
                crowdMemberCollection = value;
                OnPropertyChanged("CrowdMemberCollection");
            }
        }

        public Crowd()
        {
            this.CrowdMemberCollection = new ObservableCollection<ICrowdMember>();
        }

        public Crowd(string name) : this()
        {
            this.Name = name;
        }

        public virtual ICrowdMember Clone()
        {
            Crowd crowd = this.DeepClone() as Crowd;
            return crowd;
        }

        public virtual void SavePosition()
        {
            foreach (ICrowdMember crowdMember in this.CrowdMemberCollection)
                crowdMember.SavePosition();
        }

        public virtual void SavePosition(ICrowdMember c)
        { 
        
        }

        public virtual void Place(IMemoryElementPosition position)
        {

        }

        public virtual void Place(ICrowdMember crowdMember)
        {

        }
    }

    public class CrowdModel : Crowd, ICrowdMemberModel
    {
        private SortableObservableCollection<ICrowdMemberModel, string> crowdMemberCollection;
        public new SortableObservableCollection<ICrowdMemberModel, string> CrowdMemberCollection
        {
            get
            {
                return crowdMemberCollection;
            }
            set
            {
                crowdMemberCollection = value;
                OnPropertyChanged("CrowdMemberCollection");
            }
        }

        private Dictionary<string, IMemoryElementPosition> savedPositions;
        public Dictionary<string, IMemoryElementPosition> SavedPositions
        {
            get
            {
                if (savedPositions == null)
                    savedPositions = new Dictionary<string, IMemoryElementPosition>();
                return savedPositions;
            }
            set
            {
                savedPositions = value;
            }
        }

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

        public void ApplyFilter(string filter)
        {
            if (alreadyFiltered == true && isMatched == true)
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
            if (IsMatched)
            {
                foreach (ICrowdMemberModel cm in CrowdMemberCollection)
                {
                    cm.ApplyFilter(string.Empty);
                }
            }
            else
            {
                foreach (ICrowdMemberModel cm in CrowdMemberCollection)
                {
                    cm.ApplyFilter(filter);
                }
                if (CrowdMemberCollection.Any(cm => { return (cm as ICrowdMemberModel).IsMatched; }))
                {
                    IsMatched = true;
                }
            }
            
            IsExpanded = IsMatched;
            alreadyFiltered = true;
        }

        private bool alreadyFiltered = false;
        public void ResetFilter()
        {
            alreadyFiltered = false;
            foreach (ICrowdMemberModel cm in CrowdMemberCollection)
            {
                cm.ResetFilter();
            }
        }

        public override ICrowdMember Clone()
        {
            CrowdModel crowdModel = this.DeepClone() as CrowdModel;
            return crowdModel;
        }
        public override void SavePosition()
        {
            foreach (ICrowdMember crowdMember in this.CrowdMemberCollection)
            {
                if (crowdMember is CrowdModel)
                    crowdMember.SavePosition();
                else
                {
                    this.SavePosition(crowdMember);
                }
            }
        }
        public override void SavePosition(ICrowdMember c)
        {
            var position = (c as Character).Position.Clone(false);
            if (this.SavedPositions.ContainsKey(c.Name))
                this.SavedPositions[c.Name] = position;
            else
                this.SavedPositions.Add(c.Name, (c as Character).Position.Clone(false));
        }
        public override void Place(IMemoryElementPosition position)
        {
            foreach (ICrowdMember crowdMember in this.CrowdMemberCollection)
            {
                if (crowdMember is Crowd)
                {
                    crowdMember.Place(null);
                }
                else
                {
                    crowdMember.Place(this.SavedPositions[crowdMember.Name]);
                }
            }
        }

        public override void Place(ICrowdMember crowdMember)
        {
            IMemoryElementPosition pos;
            if (this.SavedPositions.TryGetValue(crowdMember.Name, out pos))
            {
                CrowdMemberModel model = crowdMember as CrowdMemberModel;
                model.Position = pos.Clone(false, (model.Position as MemoryInstance).GetTargetPointer());
            }
            else if(this.Name == Constants.ALL_CHARACTER_CROWD_NAME)
            {
                CrowdMemberModel model = crowdMember as CrowdMemberModel;
                if(model.SavedPosition != null)
                {
                    MemoryInstance memIns = (model.Position as MemoryInstance);
                    uint x = memIns.GetTargetPointer();
                    model.Position = model.SavedPosition.Clone(false, x);
                }
                    
            }
        }
        public CrowdModel() : base()
        {
            this.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
        }
        public CrowdModel(string name) : base(name)
        {
            this.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name);
            //this.Name = name; Handled by base class
        }
    }
}
