using Framework.WPF.Library;
using Module.Shared.Models.ProcessCommunicator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.DomainModels
{
    public interface ICrowdMember
    {
        string Name { get; set; }
        ICrowdMember Parent { get; set; }
        ObservableCollection<ICrowdMember> CrowdMemberCollection { get; set; }

        // Following methods would be added as necessary. These can be virtual or abstract. Will decide later.
        //void Place(Position position);
        //void SavePosition();
        //string Save(string filename = null);
        //ICrowdMember Clone();
    }
}
