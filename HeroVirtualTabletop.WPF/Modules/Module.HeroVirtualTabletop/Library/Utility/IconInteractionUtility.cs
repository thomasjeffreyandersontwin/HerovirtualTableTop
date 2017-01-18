using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Module.HeroVirtualTabletop.Library.Utility
{
    public static class IconInteractionUtility
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate bool InitGame(IntPtr hWnd);
        private delegate bool InitGame(int x, [MarshalAs(UnmanagedType.LPStr)]string gamePath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CloseGame(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SetUserHWND(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ExecuteCommand([MarshalAs(UnmanagedType.LPStr)]string commandline);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetCOHPath([MarshalAs(UnmanagedType.LPStr)]string path);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetHoveredNPCInfo();

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetMouseXYZInGame();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CheckIfGameLoaded();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate IntPtr DetectCollision(float sourceX, float sourceY, float sourceZ, float destX, float destY, float destZ);

        private static IntPtr dllHandle;
        private static InitGame initGame;
        private static CloseGame closeGame;
        private static SetUserHWND setUserHWnd;
        private static ExecuteCommand executeCmd;
        private static GetHoveredNPCInfo getHoveredNPCInfo;
        private static GetMouseXYZInGame getMouseXYZInGame;
        private static CheckIfGameLoaded checkIfGameLoaded;
        private static DetectCollision detectCollision;


        static IconInteractionUtility()
        {
            dllHandle = WindowsUtilities.LoadLibrary(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, "HookCostume.dll"));
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

                IntPtr getHoveredNPCInfoAddress = WindowsUtilities.GetProcAddress(dllHandle, "GetHoveredNPCInfo");
                if (getHoveredNPCInfoAddress != IntPtr.Zero)
                {
                    getHoveredNPCInfo = (GetHoveredNPCInfo)(Marshal.GetDelegateForFunctionPointer(getHoveredNPCInfoAddress, typeof(GetHoveredNPCInfo)));
                }

                IntPtr getMouseXYZInGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "GetMouseXYZInGame");
                if (getMouseXYZInGameAddress != IntPtr.Zero)
                {
                    getMouseXYZInGame = (GetMouseXYZInGame)(Marshal.GetDelegateForFunctionPointer(getMouseXYZInGameAddress, typeof(GetMouseXYZInGame)));
                }

                IntPtr checkGameDoneAddress = WindowsUtilities.GetProcAddress(dllHandle, "CheckGameDone");
                if (checkGameDoneAddress != IntPtr.Zero)
                {
                    checkIfGameLoaded = (CheckIfGameLoaded)(Marshal.GetDelegateForFunctionPointer(checkGameDoneAddress, typeof(CheckIfGameLoaded)));
                }

                IntPtr detectCollisionAddress = WindowsUtilities.GetProcAddress(dllHandle, "CollisionDetection");
                if (checkGameDoneAddress != IntPtr.Zero)
                {
                    detectCollision = (DetectCollision)(Marshal.GetDelegateForFunctionPointer(detectCollisionAddress, typeof(DetectCollision)));
                }
            }
        }

        public static void RunCOHAndLoadDLL(string path)
        {
            initGame(1, path);
            while (true)
            {
                bool gameLoaded = checkIfGameLoaded();
                if (gameLoaded)
                    break;
                else
                    System.Threading.Thread.Sleep(1000);
            }
            System.Threading.Thread.Sleep(1500);
            setUserHWnd(IntPtr.Zero);
        }

        public static void CloseCOH()
        {
            closeGame(IntPtr.Zero);
        }

        public static void ExecuteCmd(string command)
        {
            if (command.Length > 254)
            {
                string parsedCmd = command;
                int position = 0;
                while (parsedCmd.Length > 254)
                {
                    parsedCmd = parsedCmd.Substring(0, parsedCmd.LastIndexOf("$$", 254));
                    //executeCmd("/" + parsedCmd);
                    ParseDirectionalFXAndExecuteCommand(parsedCmd);
                    System.Threading.Thread.Sleep(500);// Sleep a while after executing a command
                    position += parsedCmd.Length + 2;
                    parsedCmd = command.Substring(position);
                }
                //executeCmd("/" + parsedCmd);
                ParseDirectionalFXAndExecuteCommand(parsedCmd);
            }
            else
            {
                //executeCmd("/" + command);
                ParseDirectionalFXAndExecuteCommand(command);
            }
        }

        private static void ParseDirectionalFXAndExecuteCommand(string command)
        {
            // Commands like this has to be split:
            /*/target_name Fire Blast [Primary]$$load_costume Gehenna\Gehenna_RainOfFire.fx x=131.93 y=0.25 z=-107.23
                     * $$target_name Fire Blast [Primary]$$load_costume Gehenna\Gehenna_RainOfFireHands.fx x=131.93 y=0.25 z=-107.23*/
            int loadCostumeCount = Regex.Matches(command, "load_costume", RegexOptions.IgnoreCase).Count;
            int directionalCostumeCount = Regex.Matches(command, "fx x=", RegexOptions.IgnoreCase).Count;
            string parseCommand = command;
            bool multipleDirectionalFXExist = loadCostumeCount > 1 && directionalCostumeCount > 1;
            int position = 0;
            while(multipleDirectionalFXExist)
            {
                int firstIndexOfdirectionalCostume = parseCommand.IndexOf(".fx x=");
                int secondIndexOfDirectionalCostume = parseCommand.IndexOf(".fx x=", firstIndexOfdirectionalCostume + 1);
                if(firstIndexOfdirectionalCostume < 0  || secondIndexOfDirectionalCostume < 0)
                    break;
                parseCommand = parseCommand.Substring(0, parseCommand.LastIndexOf("$$target_name", secondIndexOfDirectionalCostume));
                executeCmd("/" + parseCommand);
                System.Threading.Thread.Sleep(500); // Sleep a while after executing a command
                position += parseCommand.Length + 2;
                parseCommand = command.Substring(position);
                multipleDirectionalFXExist = Regex.Matches(parseCommand, "load_costume").Count > 1 && Regex.Matches(parseCommand, ".fx x=").Count > 1;
            }
            executeCmd("/" + parseCommand);
        }
        public static string GeInfoFromNpcMouseIsHoveringOver()
        {
            return Marshal.PtrToStringAnsi(getHoveredNPCInfo());
        }
        

        public static string GetMouseXYZString()
        {
            System.Threading.Thread.Sleep(100);
            return Marshal.PtrToStringAnsi(getMouseXYZInGame());
        }

        
       
        public static string GetCollisionInfo(float sourceX, float sourceY, float sourceZ, float destX, float destY, float destZ)
        {
            return Marshal.PtrToStringAnsi(detectCollision(sourceX, sourceY, sourceZ, destX, destY, destZ));
        }
    }
}
