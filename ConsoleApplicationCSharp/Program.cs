using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;

namespace ConsoleApplicationCSharp
{
    class Program
    {
        [DllImport("HookCostume.dll")]
        public static extern bool InitGame(int HWND);

        [DllImport("HookCostume.dll")]
        public static extern bool CloseGame(int HWND);

        [DllImport("HookCostume.dll")]
        public static extern bool SetUserHWND(int HWND);

        [DllImport("HookCostume.dll")]
        public static extern bool ExecuteCommand(String commandline);

        [DllImport("HookCostume.dll", EntryPoint = "GetHoveredNPCInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetHoveredNPCInfo();

        [DllImport("HookCostume.dll", EntryPoint = "GetMouseXYZInGame", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetMouseXYZInGame();

        static void Main(string[] args)
        {
            String spawncommand = "/spawn_npc model_Statesman Sample Character";

            Console.WriteLine("This app will interact with MFICON#. Make sure MFICON# is running.");
            Console.WriteLine("This app will interact with MFICON#. Make sure MFICON# is running.");

            //Load HookCostume.dll and run game
            InitGame(0);
            Thread.Sleep(1000);

            //Set user hwnd for NPC hovering;
            SetUserHWND(0);

            Console.ReadKey();

            Console.WriteLine("Press Enter to spawn a sample character");
            Console.ReadKey();
            ExecuteCommand(spawncommand);

            Console.WriteLine("display mouse hovering or target NPC");
            Console.ReadKey();

            IntPtr intPtr = GetHoveredNPCInfo();
            string npcinfo = Marshal.PtrToStringAnsi(intPtr);
            Console.WriteLine(npcinfo);

            Console.WriteLine("display mouse hovering X, Y, Z");
            Console.ReadKey();

            IntPtr intPtr1 = GetMouseXYZInGame();
            string mouseinfo = Marshal.PtrToStringAnsi(intPtr1);
            Console.WriteLine(mouseinfo);


            /*HERE GOES YOUR CALL TO SEND "spawncommand" TO THE GAME*/

            Console.WriteLine("Check if the characer is there.Press Enter to delete it");
            Console.ReadKey();

            String clearcommand = "/clear_npc";

            ExecuteCommand(clearcommand);

            /*HERE GOES YOUR CALL TO SEND "clearcommand" TO THE GAME*/

            Console.WriteLine("Check if the characer is gone. Press Enter to terminate...");
            Console.ReadKey();

            CloseGame(0);

        }
    }
}
