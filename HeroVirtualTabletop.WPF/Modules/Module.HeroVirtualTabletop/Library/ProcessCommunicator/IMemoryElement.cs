using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public interface IMemoryElement : IMemoryElementTargeter
    {
        string Label { get; set; }
        IMemoryElementPosition Position { get; set; }
    }
}
