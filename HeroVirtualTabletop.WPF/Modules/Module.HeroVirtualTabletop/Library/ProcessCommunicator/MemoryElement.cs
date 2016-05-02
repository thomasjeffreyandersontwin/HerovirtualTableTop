using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public class MemeoryElement : MemoryInstance, IMemoryElement
    {
        public MemeoryElement(bool initFromCurrentTarget = true) : base(initFromCurrentTarget) { }

        public string Label
        {
            get
            {
                try
                {
                    return GetAttributeAsString(12740, Encoding.UTF8);
                }
                catch
                {
                    return string.Empty;
                }
            }
            set
            {
                try
                {
                    SetTargetAttribute(12740, value, Encoding.UTF8);
                }
                catch { }
            }
        }

        public Position Position
        {
            get;
            set;
        }

        public void Target()
        {
            WriteCurrentTargetToGameMemory();
        }

        public void Untarget()
        {
            WriteToMemory((uint)0);
        }
    }
}
