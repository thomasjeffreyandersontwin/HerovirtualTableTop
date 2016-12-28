using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.Commands;
using System.Timers;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    
    
    public static class HookCodes
    {
        public const int HC_ACTION = 0;
        public const int HC_GETNEXT = 1;
        public const int HC_SKIP = 2;
        public const int HC_NOREMOVE = 3;
        public const int HC_NOREM = HC_NOREMOVE;
        public const int HC_SYSMODALON = 4;
        public const int HC_SYSMODALOFF = 5;
    }

    public enum HookType
    {
        WH_KEYBOARD = 2,
        WH_MOUSE = 7,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard 
    /// input event. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEHOOKSTRUCT
    {
        public POINT pt;        // The x and y coordinates in screen coordinates
        public int hwnd;        // Handle to the window that'll receive the mouse message
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    /// <summary>
    /// The MOUSEHOOKSTRUCT structure contains information about a mouse event passed 
    /// to a WH_MOUSE hook procedure, MouseProc. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;        // The x and y coordinates in screen coordinates. 
        public int mouseData;   // The mouse wheel and button info.
        public int flags;
        public int time;        // Specifies the time stamp for this message. 
        public IntPtr dwExtraInfo;
    }

    public enum MouseMessage
    {
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,

        WM_MOUSEWHEEL = 0x020A,
        WM_MOUSEHWHEEL = 0x020E,

        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9
    }

    /// <summary>
    /// The structure contains information about a low-level keyboard input event. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public int vkCode;      // Specifies a virtual-key code
        public int scanCode;    // Specifies a hardware scan code for the key
        public int flags;
        public int time;        // Specifies the time stamp for this message
        public int dwExtraInfo;
    }

    public enum KeyboardMessage
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105
    }

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    public abstract class Hooker : BaseViewModel
    {
        public IntPtr hookID;
        public IntPtr mouseHookID;
        public Keys vkCode;
        public System.Windows.Input.Key _inputKey;
        private int maxClickTime = (int)(System.Windows.Forms.SystemInformation.DoubleClickTime * 2);
        public System.Timers.Timer DoubleTripleQuadMouseClicksTracker = new System.Timers.Timer();

        public Hooker(IBusyService busyService, IUnityContainer container) : base(busyService, container)
        {

            DoubleTripleQuadMouseClicksTracker.AutoReset = false;
            DoubleTripleQuadMouseClicksTracker.Interval = maxClickTime;
            DoubleTripleQuadMouseClicksTracker.Elapsed +=
                new ElapsedEventHandler(DoubleTripleQuadMouseClicksTrackerElapsed);
        }
        public enum DesktopMouseState { LEFT_CLICK = 1, DOUBLE_CLICK = 2, RIGHT_CLICK =3, MOUSE_MOVE= 4, RIGHT_CLICK_UP =5, LEFT_CLICK_UP = 6, TRIPLE_CLICK = 7, QUAD_CLICK=8 };

        public void ActivateKeyboardHook()
        {

            hookID = KeyBoardHook.SetHook(this.HandleKeyboardEvent);
            mouseHookID = MouseHook.SetHook(this.HandleMouseEvent);
        }

        internal void DeactivateKeyboardHook()
        {
            KeyBoardHook.UnsetHook(hookID);
        }
        internal IntPtr CallNextHook(IntPtr hookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return KeyBoardHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        internal abstract DelegateCommand<object> RetrieveCommandFromKeyInput(Keys vkCode);
        internal abstract DelegateCommand<object> RetrieveCommandFromMouseInput(DesktopMouseState mouseState);
        internal System.Windows.Input.Key InputKey
        {
            get {
                return KeyInterop.KeyFromVirtualKey((int)this.vkCode);
            }
        }
        internal Boolean ApplicationIsActiveWindow
        {
            get
            {
                uint wndProcId;
                IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                
                uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                var currentProcId = Process.GetCurrentProcess().Id;
                return currentProcId == wndProcId;
            }
        }

        public DesktopMouseState MouseState = DesktopMouseState.MOUSE_MOVE;
        int MouseClickCount = 0;

        void DoubleTripleQuadMouseClicksTrackerElapsed(object sender, ElapsedEventArgs e)
        {
            DoubleTripleQuadMouseClicksTracker.Stop();
            MouseClickCount = 0;
        }
        internal IntPtr HandleMouseEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                //MouseState = DesktopMouseState.UP;
                if (MouseMessage.WM_LBUTTONDOWN == (MouseMessage)wParam)
                {
                    MouseClickCount += 1;
                    switch (MouseClickCount)
                    {
                        case 1:
                            MouseState = DesktopMouseState.LEFT_CLICK;
                            Action action = delegate ()
                            {
                                DoubleTripleQuadMouseClicksTracker.Start();
                            };
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
                            break;
                        case 2: MouseState = DesktopMouseState.DOUBLE_CLICK;break;
                        case 3: MouseState = DesktopMouseState.TRIPLE_CLICK; break;
                        case 4: MouseState = DesktopMouseState.QUAD_CLICK; break;
                    }
                }
                else if (MouseMessage.WM_RBUTTONDOWN == (MouseMessage)wParam)
                {
                    MouseState = DesktopMouseState.RIGHT_CLICK;
                }
                else if (MouseMessage.WM_RBUTTONUP == (MouseMessage)wParam)
                {
                    MouseState = DesktopMouseState.RIGHT_CLICK_UP;
                }
                else if (MouseMessage.WM_LBUTTONUP == (MouseMessage)wParam)
                {
                    MouseState = DesktopMouseState.LEFT_CLICK_UP;
                }
                else if (MouseMessage.WM_MOUSEMOVE == (MouseMessage)wParam)
                {
                    MouseState = DesktopMouseState.MOUSE_MOVE;
                }
                IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null))
                {
                    DelegateCommand<object> command = RetrieveCommandFromMouseInput(MouseState);
                    if (command != null && command.CanExecute(null))
                    {
                        command.Execute(null);

                    }

                }
               // MouseClickCount = 0;
            }
            return MouseHook.CallNextHookEx(mouseHookID, nCode, wParam, lParam);
        }

        internal IntPtr HandleKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            
            if (nCode >= 0)
            {  
                KBDLLHOOKSTRUCT keyboardLLHookStruct = (KBDLLHOOKSTRUCT)(Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)));
                this.vkCode = (Keys)keyboardLLHookStruct.vkCode;
                KeyboardMessage wmKeyboard = (KeyboardMessage)wParam;
                if ((wmKeyboard == KeyboardMessage.WM_KEYDOWN || wmKeyboard == KeyboardMessage.WM_SYSKEYDOWN))
                {
                    IntPtr foregroundWindow = WindowsUtilities.GetForegroundWindow();
                    uint wndProcId;
                    uint wndProcThread = WindowsUtilities.GetWindowThreadProcessId(foregroundWindow, out wndProcId);
                    if (foregroundWindow == WindowsUtilities.FindWindow("CrypticWindow", null)
                        || Process.GetCurrentProcess().Id == wndProcId)
                    {
                        DelegateCommand<object> command = RetrieveCommandFromKeyInput(vkCode);
                        if (command != null && command.CanExecute(null)) { 
                            command.Execute(null);

                        }
                    }
                    WindowsUtilities.SetForegroundWindow(foregroundWindow);
                }
            }
            return CallNextHook(hookID, nCode, wParam, lParam);
        }
    }

        
        

    public static class KeyBoardHook
    {
        #region Imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        static KeyBoardHook()
        {
            AppDomain.CurrentDomain.ProcessExit += UnsetHooks;
        }
        private static Dictionary<IntPtr, LowLevelKeyboardProc> hookedProcs = new Dictionary<IntPtr, LowLevelKeyboardProc>();
        private static List<IntPtr> hookedProcIDs = new List<IntPtr>();
        public static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hookID = SetWindowsHookEx((int)HookType.WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                hookedProcIDs.Add(hookID);
                hookedProcs.Add(hookID, proc);
                return hookID;
            }
        }
  
        public static void UnsetHook(IntPtr hookID)
        {
            if (hookedProcIDs.Contains(hookID))
            {
                UnhookWindowsHookEx(hookID);
                hookedProcIDs.Remove(hookID);
                hookedProcs.Remove(hookID);
            }
        }
                
        private static void UnsetHooks(object sender, EventArgs e)
        {
            foreach (IntPtr hookID in hookedProcIDs)
            {
                UnhookWindowsHookEx(hookID);
            }
        }
    }

    public static class MouseHook
    {
        #region Imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        static MouseHook()
        {
            AppDomain.CurrentDomain.ProcessExit += UnsetHooks;
        }
        
        private static Dictionary<IntPtr, LowLevelMouseProc> hookedProcs = new Dictionary<IntPtr, LowLevelMouseProc>();

        private static List<IntPtr> hookedProcIDs = new List<IntPtr>();

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// This function lets hook a function with LowLevelMouseProc signature to Windows key processing queue.
        /// </summary>
        /// <param name="proc">The function to be executed. Must end with "return CallNextHookEx(hookID, nCode, wParam, lParam);" to let the processing continue correctly.</param>
        /// <returns>Return the hook identifier assigned in Windows hooks queue</returns>
        public static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hookID = SetWindowsHookEx((int)HookType.WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                hookedProcIDs.Add(hookID);
                hookedProcs.Add(hookID, proc);
                return hookID;
            }
        }

        public static void UnsetHook(IntPtr hookID)
        {
            if (hookedProcIDs.Contains(hookID))
            {
                UnhookWindowsHookEx(hookID);
                hookedProcIDs.Remove(hookID);
                hookedProcs.Remove(hookID);
            }
        }

        private static void UnsetHooks(object sender, EventArgs e)
        {
            foreach (IntPtr hookID in hookedProcIDs)
            {
                UnhookWindowsHookEx(hookID);
            }
        }

    }
}
