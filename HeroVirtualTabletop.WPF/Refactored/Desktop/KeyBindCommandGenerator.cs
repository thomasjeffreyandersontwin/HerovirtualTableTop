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

namespace HeroVirtualTableTop.Desktop
{
    public class KeyBindCommandGeneratorImpl : KeyBindCommandGenerator
    {

        public string GeneratedCommandText { get; set; }
        protected internal static List<string> generatedKeybinds = new List<string>();
        string _lastCommand;
        IconInteractionUtility _iconInteracter;

        public KeyBindCommandGeneratorImpl(IconInteractionUtility iconInteractor)
        {
            _iconInteracter = iconInteractor;
        }

   
        public string Command { get; set; }
        public void GenerateDesktopCommandText(DesktopCommand desktopCommand, params string[] parameters)
        {
            

            string generatedCommandParameters = string.Empty;
            string command = _keyBindsStrings[desktopCommand];
            _lastCommand = command;
            string generatedKeybind = "";
            foreach (string p in parameters)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    generatedCommandParameters = string.Format("{0} {1}", generatedCommandParameters, p.Trim());
                    generatedCommandParameters = generatedCommandParameters.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(generatedCommandParameters))
            {
                if (!string.IsNullOrEmpty(GeneratedCommandText))
                {
                    GeneratedCommandText += string.Format("$${0} {1}", command, generatedCommandParameters);
                }
                else
                {
                    GeneratedCommandText = string.Format("{0} {1}", command, generatedCommandParameters);
                }

                generatedKeybind = string.Format("{0} {1}", command, generatedCommandParameters);
            }
            else
            {
                if (!string.IsNullOrEmpty(GeneratedCommandText))
                {
                    GeneratedCommandText += string.Format("$${0}", command);
                }
                else
                {
                    GeneratedCommandText = command;
                }

                generatedKeybind = command;


            }
        }
        public string CompleteEvent()
        {
            string command = string.Empty;
            command = popEvents();
            _iconInteracter.ExecuteCmd(command);
            return command;
        }
        private string popEvents()
        {
            _lastCommand = GeneratedCommandText;
            generatedKeybinds.Add(_lastCommand);
            string generatedCommandtext = GeneratedCommandText;
            GeneratedCommandText= "";
            //return string.Format("\"{0}\"", GeneratedKeybindText);
            return generatedCommandtext;
        }

        #region KeyBinds Strings
        internal Dictionary<DesktopCommand, string> _keyBindsStrings = new Dictionary<DesktopCommand, string>()
        {
            { Desktop.DesktopCommand.TargetName , "target_name"},
            { DesktopCommand.PrevSpawn , "prev_spawn"},
            { DesktopCommand.NextSpawn , "next_spawn"},
            { DesktopCommand.RandomSpawn , "random_spawn"},
            { DesktopCommand.Fly , "fly"},
            { DesktopCommand.EditPos , "edit_pos"},
            { DesktopCommand.DetachCamera , "detach_camera"},
            { DesktopCommand.NoClip , "no_clip"},
            { DesktopCommand.AccessLevel , "access_level"},
            { DesktopCommand.Command , "~"},
            { DesktopCommand.SpawnNpc , "spawn_npc"},
            { DesktopCommand.Rename , "rename"},
            { DesktopCommand.LoadCostume , "load_costume"},
            { DesktopCommand.MoveNPC , "move_npc"},
            { DesktopCommand.DeleteNPC , "delete_npc"},
            { DesktopCommand.ClearNPC , "clear_npc"},
            { DesktopCommand.Move , "mov"},
            { DesktopCommand.TargetEnemyNear , "target_enemy_near"},
            { DesktopCommand.LoadBind , "load_bind"},
            { DesktopCommand.BeNPC , "benpc"},
            { DesktopCommand.SaveBind , "save_bind"},
            { DesktopCommand.GetPos , "getpos"},
            { DesktopCommand.CamDist , "camdist"},
            { DesktopCommand.Follow , "follow"},
            { DesktopCommand.LoadMap , "loadmap"},
            { DesktopCommand.BindLoadFile , "bind_load_file"},
            { DesktopCommand.Macro , "macro"},
            { DesktopCommand.NOP , "nop" },
            { DesktopCommand.PopMenu , "popmenu" }
        };
    }
    #endregion
}

    
