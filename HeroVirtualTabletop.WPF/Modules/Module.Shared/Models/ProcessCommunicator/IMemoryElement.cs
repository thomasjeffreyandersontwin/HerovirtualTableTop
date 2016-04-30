using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Models.ProcessCommunicator
{
    public interface IMemoryElement
    {
        string Label { get; set; }
        Position Position { get; set; }
    }
}
