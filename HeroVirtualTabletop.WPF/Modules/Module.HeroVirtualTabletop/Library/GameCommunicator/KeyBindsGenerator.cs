using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    public class KeyBindsGenerator
    {
        #region KeyBinds Strings
        internal Dictionary<GameEvent, string> keyBindsStrings = new Dictionary<GameEvent, string>()
        {
            { GameEvent.TargetName , "target_name"},
            { GameEvent.PrevSpawn , "prev_spawn"},
            { GameEvent.NextSpawn , "next_spawn"},
            { GameEvent.RandomSpawn , "random_spawn"},
            { GameEvent.Fly , "fly"},
            { GameEvent.EditPos , "edit_pos"},
            { GameEvent.DetachCamera , "detach_camera"},
            { GameEvent.NoClip , "no_clip"},
            { GameEvent.AccessLevel , "access_level"},
            { GameEvent.Command , "~"},
            { GameEvent.SpawnNpc , "spawn_npc"},
            { GameEvent.Rename , "rename"},
            { GameEvent.LoadCostume , "load_costume"},
            { GameEvent.MoveNPC , "move_npc"},
            { GameEvent.DeleteNPC , "delete_npc"},
            { GameEvent.ClearNPC , "clear_npc"},
            { GameEvent.Move , "mov"},
            { GameEvent.TargetEnemyNear , "target_enemy_near"},
            { GameEvent.LoadBind , "load_bind"},
            { GameEvent.BeNPC , "benpc"},
            { GameEvent.SaveBind , "save_bind"},
            { GameEvent.GetPos , "getpos"},
            { GameEvent.CamDist , "camdist"},
            { GameEvent.Follow , "follow"},
            { GameEvent.LoadMap , "loadmap"},
            { GameEvent.BindLoadFile , "bind_load_file"},
            { GameEvent.Macro , "macro"},
            { GameEvent.NOP , "nop" },
            { GameEvent.PopMenu , "popmenu" }
        };
        #endregion

        private string directory;

        public KeyBindsGenerator()
        {
            directory = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bindFile = Path.Combine(directory, loaderKey + ".txt");
        }

        private string bindFile;

        public string BindFile
        {
            get
            {
                return bindFile;
            }
        }

        private string triggerKey = "Y";

        public string TriggerKey
        {
            get
            {
                return triggerKey;
            }
        }

        private string loaderKey = "B";


        private static GameEvent lastEvent;
        private static string generatedKeybindText;

        private static string lastKeyBindGenerated;
        public static string LastKeyBindsGenerated
        {
            get
            {
                return lastKeyBindGenerated;
            }
        }

        protected internal static List<string> generatedKeybinds = new List<string>();

        public string GenerateKeyBindsForEvent(GameEvent gameEvent, params string[] parameters)
        {
            lastEvent = gameEvent;

            string GeneratedKeybindText = string.Empty;
            string command = keyBindsStrings[gameEvent];

            foreach (string p in parameters)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    GeneratedKeybindText = string.Format("{0} {1}", GeneratedKeybindText, p.Trim());
                    GeneratedKeybindText = GeneratedKeybindText.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(GeneratedKeybindText))
            {
                if (!string.IsNullOrEmpty(KeyBindsGenerator.generatedKeybindText))
                {
                    KeyBindsGenerator.generatedKeybindText += string.Format("$${0} {1}", command, GeneratedKeybindText);
                }
                else
                {
                    KeyBindsGenerator.generatedKeybindText = string.Format("{0} {1}", command, GeneratedKeybindText);
                }

                return string.Format("{0} {1}", command, GeneratedKeybindText);
            }
            else
            {
                if (!string.IsNullOrEmpty(KeyBindsGenerator.generatedKeybindText))
                {
                    KeyBindsGenerator.generatedKeybindText += string.Format("$${0}", command);
                }
                else
                {
                    KeyBindsGenerator.generatedKeybindText = command;
                }

                return command;
            }

        }

        private string PopEvents()
        {
            lastKeyBindGenerated = KeyBindsGenerator.generatedKeybindText;
            generatedKeybinds.Add(lastKeyBindGenerated);
            string GeneratedKeybindText = KeyBindsGenerator.generatedKeybindText;
            KeyBindsGenerator.generatedKeybindText = "";
            //return string.Format("\"{0}\"", GeneratedKeybindText);
            return GeneratedKeybindText;
        }

        public string CompleteEvent()
        {
            string command = string.Empty;
            string generatedKeyBindText = string.Empty;

            command = PopEvents();

            //generatedKeyBindText = triggerKey + " " + command;

            string parsedCmd = command;
            while (parsedCmd.Length > 254)
            {
                parsedCmd = parsedCmd.Substring(0, parsedCmd.LastIndexOf("$$", 254) + 1);
                IconInteractionUtility.ExecuteCmd("/" + parsedCmd);
            }

            return command;

            //try
            //{
            //    File.Delete(bindFile);
            //}
            //catch { }

            //try
            //{
            //    command = PopEvents();
            //    generatedKeyBindText = triggerKey + " " + command;
            //    StreamWriter SW = File.AppendText(bindFile);
            //    SW.WriteLine(generatedKeyBindText);
            //    SW.Close();
            //}
            //catch
            //{
            //    System.Windows.MessageBox.Show("Invalid Filename: " + bindFile, "Error");
            //    return null;
            //}

            //IntPtr hWnd = Utility.WindowsUtilities.FindWindow("CrypticWindow", null);

            //if (IntPtr.Zero == hWnd) //Game is not running
            //{
            //    return command;
            //}
            //IntPtr HVThWnd = Process.GetCurrentProcess().MainWindowHandle;

            //Utility.WindowsUtilities.SetForegroundWindow(hWnd);
            //Utility.WindowsUtilities.SetActiveWindow(hWnd);
            //Utility.WindowsUtilities.ShowWindow(hWnd, 3); // 3 = SW_SHOWMAXIMIZED

            //System.Threading.Thread.Sleep(250);

            //AutoItX3Lib.AutoItX3 input = new AutoItX3Lib.AutoItX3();

            //input.Send(loaderKey.ToLower());

            //System.Threading.Thread.Sleep(250);

            //input.Send(triggerKey.ToLower());

            //System.Threading.Thread.Sleep(250);

            //Utility.WindowsUtilities.SetForegroundWindow(HVThWnd);
            //Utility.WindowsUtilities.SetActiveWindow(HVThWnd);
            
            //return command;
        }
    }
}
