using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    public class IconInteractionUtility
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitGame(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CloseGame(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SetUserHWND(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ExecuteCommand([MarshalAs(UnmanagedType.LPStr)]string commandline);

        private IntPtr dllHandle;
        private InitGame initGame;
        private CloseGame closeGame;
        private SetUserHWND setUserHWnd;
        private ExecuteCommand executeCmd;

        public IconInteractionUtility()
        {
            dllHandle = WindowsUtilities.LoadLibrary("HookCostume.dll");
            if (dllHandle != null)
            {
                IntPtr initGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "InitGame");
                if (initGameAddress != IntPtr.Zero)
                {
                    initGame = (InitGame)(Marshal.GetDelegateForFunctionPointer(initGameAddress, typeof(InitGame)));
                }

                IntPtr closeGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "CloseGame");
                if (closeGameAddress != IntPtr.Zero)
                {
                    closeGame = (CloseGame)(Marshal.GetDelegateForFunctionPointer(closeGameAddress, typeof(CloseGame)));
                }

                IntPtr setUserHWndAddress = WindowsUtilities.GetProcAddress(dllHandle, "SetUserHWND");
                if (setUserHWndAddress != IntPtr.Zero)
                {
                    setUserHWnd = (SetUserHWND)(Marshal.GetDelegateForFunctionPointer(setUserHWndAddress, typeof(SetUserHWND)));
                }

                IntPtr executeCmdAddress = WindowsUtilities.GetProcAddress(dllHandle, "ExecuteCommand");
                if (executeCmdAddress != IntPtr.Zero)
                {
                    executeCmd = (ExecuteCommand)(Marshal.GetDelegateForFunctionPointer(executeCmdAddress, typeof(ExecuteCommand)));
                }
            }
        }

        public void RunCOHAndLoadDLL()
        {
            initGame(IntPtr.Zero);
        }

        public void CloseCOH()
        {
            closeGame(IntPtr.Zero);
        }

        public void ExecuteCmd(string command)
        {
            executeCmd(command);
        }
        
    }
}
