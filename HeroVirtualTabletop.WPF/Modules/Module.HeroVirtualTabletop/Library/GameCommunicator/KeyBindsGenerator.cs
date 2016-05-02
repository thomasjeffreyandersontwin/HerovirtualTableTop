using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    public class KeyBindsGenerator
    {
        #region KeyBinds Strings
        private Dictionary<GameEvent, string> _keyBindsStrings = new Dictionary<GameEvent, string>()
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
        };
        #endregion

        private string directory;
        private string bindFile;

        public KeyBindsGenerator()
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.ToLowerInvariant().Contains("unittesting")))
            {
                directory = string.Empty;
            }
            else
            {
                directory = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME);
            }

            bindFile = directory + LoaderKey + ".txt";
        }

        private GameEvent LastEvent;
        private string GeneratedKeybindText;
        private string TriggerKey = "Y";
        private string LoaderKey = "B";
        private string LastKeyBindGenerated;

        public string GenerateKeyBindsForEvent(GameEvent gameEvent, params string[] parameters)
        {
            string generatedKeybindText = "";
            string command = _keyBindsStrings[gameEvent];

            foreach (string p in parameters)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    generatedKeybindText += " " + p.Trim();
                    generatedKeybindText = generatedKeybindText.Trim();
                }
            }

            if (!string.IsNullOrEmpty(GeneratedKeybindText))
            {
                GeneratedKeybindText += "$$" + command + " " + generatedKeybindText;
            }
            else
            {
                GeneratedKeybindText = command + " " + generatedKeybindText;
            }
            LastEvent = gameEvent;
            return command + " " + generatedKeybindText;
        }

        private string PopEvents()
        {
            string generatedKeybindText = GeneratedKeybindText;
            GeneratedKeybindText = "";
            return "\"" + generatedKeybindText + "\"";
        }

        public string CompleteEvent()
        {
            string command = string.Empty;
            string generatedKeyBindText = string.Empty;

            try
            {
                File.Delete(bindFile);
            }
            catch { }

            try
            {
                LastKeyBindGenerated = GeneratedKeybindText;
                command = PopEvents();
                generatedKeyBindText = TriggerKey + " " + command;
                StreamWriter SW = File.AppendText(bindFile);
                SW.WriteLine(generatedKeyBindText);
                SW.Close();
            }
            catch
            {
                System.Windows.MessageBox.Show("Invalid Filename: " + bindFile, "Error");
            }

            IntPtr hWnd = Utility.WindowsUtilities.FindWindow("CrypticWindow", null);

            Utility.WindowsUtilities.SetForegroundWindow(hWnd);
            Utility.WindowsUtilities.SetActiveWindow(hWnd);
            Utility.WindowsUtilities.ShowWindow(hWnd, 3); // 3 = SW_SHOWMAXIMIZED

            System.Threading.Thread.Sleep(250);

            AutoItX3Lib.AutoItX3 input = new AutoItX3Lib.AutoItX3();

            input.Send(LoaderKey.ToLower());

            System.Threading.Thread.Sleep(250);

            input.Send(TriggerKey.ToLower());

            System.Threading.Thread.Sleep(250);

            return command;
        }
    }
}
