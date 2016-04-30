using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Models.ProcessCommunicator
{
    public interface IMemoryInstance
    {
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
