<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public class MemoryElement : MemoryInstance, IMemoryElement
    {
        public MemoryElement(bool initFromCurrentTarget = true) : base(initFromCurrentTarget) { }

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

        public IMemoryElementPosition Position
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
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public class MemoryElement : MemoryInstance, IMemoryElement
    {
        public MemoryElement(bool initFromCurrentTarget = true) : base(initFromCurrentTarget) { }

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

        public IMemoryElementPosition Position
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
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
