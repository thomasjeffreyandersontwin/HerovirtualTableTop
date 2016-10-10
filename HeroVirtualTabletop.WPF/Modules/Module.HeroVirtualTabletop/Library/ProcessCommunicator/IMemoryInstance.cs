<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public interface IMemoryInstance
    {
        uint Pointer { get; }
        bool IsReal { get; }

        void InitFromCurrentTarget();
        string GetAttributeAsString(int offset);
        string GetAttributeAsString(int offset, Encoding encoding);
        float GetAttributeAsFloat(int offset);
        void SetTargetAttribute(int offset, string value);
        void SetTargetAttribute(int offset, float value);
        void SetTargetAttribute(int offset, string value, Encoding encoding);
        void WriteToMemory<T>(T obj);
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
    public interface IMemoryInstance
    {
        uint Pointer { get; }
        bool IsReal { get; }

        void InitFromCurrentTarget();
        string GetAttributeAsString(int offset);
        string GetAttributeAsString(int offset, Encoding encoding);
        float GetAttributeAsFloat(int offset);
        void SetTargetAttribute(int offset, string value);
        void SetTargetAttribute(int offset, float value);
        void SetTargetAttribute(int offset, string value, Encoding encoding);
        void WriteToMemory<T>(T obj);
    }
}
>>>>>>> 68fdcebd8c83dbcfdbac1d97e85345c9412bacd6
