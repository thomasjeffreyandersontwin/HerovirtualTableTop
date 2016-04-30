using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Models.ProcessCommunicator
{
    public interface IMemoryElementPosition
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }

        bool IsWithin(float maxDistance, IMemoryElementPosition From);
    }
}
