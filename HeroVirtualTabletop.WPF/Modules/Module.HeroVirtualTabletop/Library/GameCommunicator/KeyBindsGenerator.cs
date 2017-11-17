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
            { GameEvent.TargetName , "targetname"},
            { GameEvent.PrevSpawn , "prevspawn"},
            { GameEvent.NextSpawn , "nextspawn"},
            { GameEvent.RandomSpawn , "randomspawn"},
            { GameEvent.Fly , "fly"},
            { GameEvent.EditPos , "editpos"},
            { GameEvent.DetachCamera , "detachcamera"},
            { GameEvent.NoClip , "noclip"},
            { GameEvent.AccessLevel , "accesslevel"},
            { GameEvent.Command , "~"},
            { GameEvent.SpawnNpc , "spawnnpc"},
            { GameEvent.Rename , "rename"},
            { GameEvent.LoadCostume , "loadcostume"},
            { GameEvent.MoveNPC , "movenpc"},
            { GameEvent.DeleteNPC , "deletenpc"},
            { GameEvent.ClearNPC , "clearnpc"},
            { GameEvent.Move , "mov"},
            { GameEvent.TargetEnemyNear , "targetenemynear"},
            { GameEvent.LoadBind , "loadbind"},
            { GameEvent.BeNPC , "benpc"},
            { GameEvent.SaveBind , "savebind"},
            { GameEvent.GetPos , "getpos"},
            { GameEvent.CamDist , "camdist"},
            { GameEvent.Follow , "follow"},
            { GameEvent.LoadMap , "loadmap"},
            { GameEvent.BindLoadFile , "bindloadfile"},
            { GameEvent.Macro , "macro"},
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
            string generatedKeybind = "";
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
                
                generatedKeybind = string.Format("{0} {1}", command, GeneratedKeybindText);
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

                generatedKeybind = command;
            }

            return generatedKeybind;
        }

        public string PopEvents()
        {
            lastKeyBindGenerated = KeyBindsGenerator.generatedKeybindText;
            generatedKeybinds.Add(lastKeyBindGenerated);
            string GeneratedKeybindText = KeyBindsGenerator.generatedKeybindText;
            KeyBindsGenerator.generatedKeybindText = "";
            //return string.Format("\"{0}\"", GeneratedKeybindText);
            return GeneratedKeybindText;
        }

        public string GetEvent()
        {
            return KeyBindsGenerator.generatedKeybindText;
        }

        public string CompleteEvent(bool preventLoadCostumeWithoutTarget = true)
        {
            string command = string.Empty;
            string generatedKeyBindText = string.Empty;

            command = PopEvents();

            //if (preventLoadCostumeWithoutTarget)
            //{
            //    // HACK: Prevent loading costume without targetting first
            //    if (command.Contains(keyBindsStrings[GameEvent.LoadCostume]))
            //    {
            //        var loadCostumeIndex = command.IndexOf("$$loadcostume");
            //        if (loadCostumeIndex > 0)
            //        {
            //            var prevCommand = command.Substring(0, loadCostumeIndex);
            //            if (string.IsNullOrEmpty(prevCommand) || !prevCommand.Contains(keyBindsStrings[GameEvent.TargetName]))
            //            {
            //                return "";
            //            }
            //        }
            //        else
            //            return "";
            //    } 
            //}
            // Another HACK: Prevent executing multiple targeting in a chain
            IconInteractionUtility.ExecuteCmd(command);

            return command;
        }
    }
}
