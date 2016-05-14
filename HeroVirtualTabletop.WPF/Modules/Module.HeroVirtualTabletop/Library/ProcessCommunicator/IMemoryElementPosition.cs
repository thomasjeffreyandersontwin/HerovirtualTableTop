using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public interface IMemoryElementPosition: IMemoryInstance
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }

        bool IsWithin(float maxDistance, IMemoryElementPosition From);
    }
}
