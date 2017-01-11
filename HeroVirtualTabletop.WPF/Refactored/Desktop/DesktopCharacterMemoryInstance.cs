using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroVirtualTableTop.Desktop
{
    class DesktopCharacterMemoryInstanceImpl: DesktopCharacterMemoryInstance
    {
        
        public Position Position { get; set; }
        public String Label { get; set; }
        public float MemoryAddress { get; set; }
        public void Target() { }
        public MemoryManager memoryManager { get; }
        public dynamic GetAttributeFromAdress(float address, string varType) { return null; }
        public void SetTargetAttribute(float offset, dynamic value, string varType){}

        public DesktopCharacterMemoryInstance WaitUntilTargetIsRegistered()
        {
            int w = 0;
            DesktopCharacterMemoryInstanceImpl currentTarget = new DesktopCharacterMemoryInstanceImpl();
            while (Label != currentTarget.Label)
            {
                w++;
                currentTarget = new DesktopCharacterMemoryInstanceImpl();
                if (w > 5)
                {
                    currentTarget = null;
                    break;
                }
            }
            return currentTarget;
        }
    }
}
