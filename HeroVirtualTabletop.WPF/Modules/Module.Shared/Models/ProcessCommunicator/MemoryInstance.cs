using Binarysharp.MemoryManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Models.ProcessCommunicator
{
    public class MemoryInstance : IMemoryInstance
    {
        private MemorySharp gameMemory;
        private uint targetPointer;
        private IntPtr targetMemoryAddress;

        public MemoryInstance(bool initFromCurrentTarget = true)
        {
            this.targetPointer = 0;
            this.targetMemoryAddress = new IntPtr(0x00F14FB0);
            this.InitializeGameInMemory();
            if (initFromCurrentTarget)
                InitFromCurrentTarget();
        }

        private void InitializeGameInMemory()
        {
            Process[] processes = Process.GetProcessesByName(Constants.GAME_PROCESSNAME);
            if(processes.Count() > 0)
                this.gameMemory = new MemorySharp(processes[0]);
        }

        public bool IsReal
        {
            get
            {
                return (this.targetPointer != 0);
            }
        }

        public void InitFromCurrentTarget()
        {
            this.targetPointer = this.gameMemory[this.targetMemoryAddress, false].Read<uint>();
        }

        public string GetAttributeAsString(int offset)
        {
            return this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Read<string>();
        }

        public string GetAttributeAsString(int offset, Encoding encoding)
        {
            return this.gameMemory.ReadString((IntPtr)(this.targetPointer + offset), Encoding.UTF8, false);
        }

        public float GetAttributeAsFloat(int offset)
        {
            return this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Read<float>();
        }

        public void SetTargetAttribute(int offset, string value)
        {
            this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Write<string>(value);
        }

        public void SetTargetAttribute(int offset, float value)
        {
            this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Write<float>(value);
        }

        public void SetTargetAttribute(int offset, string value, Encoding encoding)
        {
            gameMemory.WriteString((IntPtr)(this.targetPointer + offset), value, encoding,  false);
        }

        public void WriteToMemory<T>(T obj)
        {
            gameMemory[targetMemoryAddress, false].Write<T>(obj);
        }

        protected void WriteCurrentTargetToGameMemory()
        {
            WriteToMemory(this.targetPointer);
        }

        protected void SetTargetPointerFromGameMemoryInstance(MemoryInstance gameMemoryInstance)
        {
            this.targetPointer = gameMemoryInstance.targetPointer;
        }

        protected void SetTargetPointer(uint targetPointer)
        {
            this.targetPointer = targetPointer;
        }
    }
}
