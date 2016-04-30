using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Models.ProcessCommunicator
{
    public interface IMemoryElementTargeter
    {
        void Target();
        void Untarget();
    }
}
