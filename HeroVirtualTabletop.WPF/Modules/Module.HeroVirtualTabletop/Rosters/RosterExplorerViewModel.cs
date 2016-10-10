﻿using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Sevices;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Enumerations;
using Module.Shared.Events;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Module.HeroVirtualTabletop.Roster
{
    public class RosterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private ITargetObserver targetObserver;
        private EventAggregator eventAggregator;

        private bool isPlayingAttack = false;
        private bool isPlayingAreaEffect = false;
        private bool isCharacterReset = false;
        private bool isMenuDisplayed = false;
        private Attack currentAttack = null;
        private Character attackingCharacter = null;
        private List<Character> targetCharacters = new List<Character>();
        private List<CrowdMemberModel> oldSelection = new List<CrowdMemberModel>();

        private IntPtr hookID;

        private int clickCount;
        private bool isDoubleClick = false;
        private bool isTripleClick = false;
        private bool isQuadrupleClick = false;
        private int maxClickTime = (int)(System.Windows.Forms.SystemInformation.DoubleClickTime * 1.5);
        private Timer clickTimer = new Timer();
        private Timer clickTimer1 = new Timer();

        private FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();

        #endregion

        #region Events

        #endregion

        #region Public Properties

        public IPopupService PopupService
        {
            get { return this.Container.Resolve<IPopupService>(); }
        }
        private HashedObservableCollection<ICrowdMemberModel, string> participants;
        public HashedObservableCollection<ICrowdMemberModel, string> Participants
        {
            get
            {
                if (participants == null)
                    participants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.RosterCrowd.Order, x => x.RosterCrowd.Name, x => x.Name);
                return participants;
            }
            set
            {
                participants = value;
                OnPropertyChanged("Participants");
            }
        }
        private IList selectedParticipants = new ArrayList();
        public IList SelectedParticipants
        {
            get
            {
                return selectedParticipants;
            }
            set
            {
                selectedParticipants = value;
                synchSelectionWithGame();
                OnPropertyChanged("SelectedParticipants");
                OnPropertyChanged("ShowAttackContextMenu");
                Commands_RaiseCanExecuteChanged();
            }
        }

        private ICrowdMemberModel activeCharacter;
        public ICrowdMemberModel ActiveCharacter
        {
            get
            {
                return activeCharacter;
            }
            set
            {
                activeCharacter = value;
                OnPropertyChanged("ActiveCharacter");
            }
        }

        public bool ShowAttackContextMenu
        {
            get
            {
                bool showAttackContextMenu = false;
                if (this.isPlayingAreaEffect && this.SelectedParticipants != null && this.SelectedParticipants.Count > 0)
                {
                    showAttackContextMenu = true;
                    foreach (var participant in this.SelectedParticipants)
                    {
                        if (!(participant as Character).HasBeenSpawned)
                        {
                            showAttackContextMenu = false;
                            break;
                        }
                    }
                }

                return showAttackContextMenu;
            }
        }

        public bool IsSingleSpawnedCharacterSelected
        {
            get
            {
                return this.SelectedParticipants != null && SelectedParticipants.Count == 1 && (SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned;
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand<object> SavePositionCommand { get; private set; }
        public DelegateCommand<object> PlaceCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetedCommand { get; private set; }
        public DelegateCommand<object> TargetAndFollowCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCameraCommand { get; private set; }
        public DelegateCommand<object> ToggleManeuverWithCameraCommand { get; private set; }
        public DelegateCommand<object> EditCharacterCommand { get; private set; }
        public DelegateCommand<object> ActivateCharacterCommand { get; private set; }
        public DelegateCommand<object> ResetCharacterStateCommand { get; private set; }
        public DelegateCommand<object> AreaAttackTargetCommand { get; private set; }
        public DelegateCommand<object> AreaAttackTargetAndExecuteCommand { get; private set; }

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ITargetObserver targetObserver, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.targetObserver = targetObserver;

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddParticipants);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(DeleteParticipant);
            this.eventAggregator.GetEvent<CheckRosterConsistencyEvent>().Subscribe(CheckRosterConsistency);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(InitiateRosterCharacterAttack);
            this.eventAggregator.GetEvent<SetActiveAttackEvent>().Subscribe(this.LaunchActiveAttack);
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Subscribe(this.CancelActiveAttack);
            this.eventAggregator.GetEvent<AddOptionEvent>().Subscribe(this.HandleCharacterOptionAddition);

            this.eventAggregator.GetEvent<ListenForTargetChanged>().Subscribe((obj) =>
            {
                this.targetObserver.TargetChanged += TargetObserver_TargetChanged;
            });
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Subscribe((obj) =>
            {
                this.targetObserver.TargetChanged -= TargetObserver_TargetChanged;
            });

            InitializeCommands();
            clickCount = 0;
            clickTimer.AutoReset = false;
            clickTimer.Interval = maxClickTime;
            clickTimer.Elapsed +=
                new ElapsedEventHandler(clickTimer_Elapsed);
            clickTimer1.AutoReset = false;
            clickTimer1.Interval = 50;
            clickTimer1.Elapsed +=
                new ElapsedEventHandler(clickTimer1_Elapsed);
            hookID = MouseHook.SetHook(clickCharacterInDesktop);
            fileSystemWatcher.Path = string.Format("{0}\\", Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME));
            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.Filter = "*.txt";
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileSystemWatcher.Changed += fileSystemWatcher_Changed;
            //fileSystemWatcher.Created += fileSystemWatcher_Changed;
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        IntPtr clickCharacterInDesktop(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessage.WM_LBUTTONDOWN == (MouseMessage)wParam)
                {
                    if (WindowsUtilities.GetForegroundWindow() == WindowsUtilities.FindWindow("CrypticWindow", null))
                    {
                        if(!this.isPlayingAttack)
                        {
                            //Handle clicks
                            clickCount += 1;
                            switch (clickCount)
                            {
                                case 1: Action action = delegate() { clickTimer.Start(); };
                                    Application.Current.Dispatcher.BeginInvoke(action);
                                    break;
                                case 2: isDoubleClick = true; break;
                                case 3: isTripleClick = true; break;
                                case 4: isQuadrupleClick = true; break;
                                default: break;
                            }
                        }
                        else
                        {
                            if (!this.isMenuDisplayed)
                            {
                                bool canFireAttackAnimation = true;
                                string hoveredCharacterInfo = IconInteractionUtility.GetHoveredNPCInfoFromGame();
                                string characterName = null;
                                if (!string.IsNullOrWhiteSpace(hoveredCharacterInfo))
                                {
                                    characterName = GetCharacterNameFromHoveredInfo(hoveredCharacterInfo);
                                }
                                if (!string.IsNullOrEmpty(characterName))
                                {
                                    ICrowdMemberModel model = this.Participants.FirstOrDefault(p => p.Name == characterName);
                                    if (model != null && this.SelectedParticipants != null && !this.SelectedParticipants.Contains(model))
                                    {
                                        canFireAttackAnimation = false;
                                    }
                                    //else if(model != null && model.Name != this.attackingCharacter.Name)
                                    //{
                                    //    canFireAttackAnimation = false;
                                    //}
                                }
                                if (canFireAttackAnimation)
                                {
                                    if (this.currentAttack != null && this.attackingCharacter != null)
                                    {
                                        clickTimer1.Start();// Doing this in another thread so that if the game window has lost focus, the attack doesn't animate.
                                    }
                                }

                            }
                            else
                            {
                                this.isMenuDisplayed = false;
                            }
                        }
                    }
                }
                else if (MouseMessage.WM_RBUTTONUP == (MouseMessage)wParam)
                {
                    if (WindowsUtilities.GetForegroundWindow() == WindowsUtilities.FindWindow("CrypticWindow", null))
                    {
                        new PauseElement("", 1000).Play();
                        string hoveredCharacterInfo = IconInteractionUtility.GetHoveredNPCInfoFromGame();
                        if (!string.IsNullOrWhiteSpace(hoveredCharacterInfo))
                        {
                            string characterName = GetCharacterNameFromHoveredInfo(hoveredCharacterInfo);
                            CrowdMemberModel hoveredCharacter = this.Participants.FirstOrDefault(p => p.Name == characterName) as CrowdMemberModel;
                            if (!string.IsNullOrWhiteSpace(characterName) && hoveredCharacter != null)
                            {
                                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                                if (isPlayingAreaEffect)
                                {
                                    if (this.attackingCharacter != null && this.attackingCharacter.Name != characterName)
                                    {
                                        AddDesktopTargetToRosterSelection(hoveredCharacter);
                                        hoveredCharacter.Target();
                                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.PopMenu, "areaattack");
                                        keyBindsGenerator.CompleteEvent();
                                        this.isMenuDisplayed = true;
                                    }
                                }
                                else
                                {
                                    AddDesktopTargetToRosterSelection(hoveredCharacter);
                                    GenerateMenuFileForCharacter(hoveredCharacter);
                                    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.PopMenu, "character");
                                    fileSystemWatcher.EnableRaisingEvents = true;
                                    keyBindsGenerator.CompleteEvent();
                                    this.isMenuDisplayed = true;
                                }
                            }
                        }
                    }
                }
            }
            return MouseHook.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        public Vector3 GetDirectionVectorFromMouseXYZInfo(string mouseXYZInfo)
        {
            Vector3 vector3 = new Vector3();
            float f;
            int xStart = mouseXYZInfo.IndexOf("[");
            int xEnd = mouseXYZInfo.IndexOf("]");
            string xStr = mouseXYZInfo.Substring(xStart + 1, xEnd - xStart - 1);
            if (float.TryParse(xStr, out f))
                vector3.X = f;
            int yStart = mouseXYZInfo.IndexOf("[", xEnd);
            int yEnd = mouseXYZInfo.IndexOf("]", yStart);
            string yStr = mouseXYZInfo.Substring(yStart + 1, yEnd - yStart - 1);
            if (float.TryParse(yStr, out f))
                vector3.Y = f;
            int zStart = mouseXYZInfo.IndexOf("[", yEnd);
            int zEnd = mouseXYZInfo.IndexOf("]", zStart);
            string zStr = mouseXYZInfo.Substring(zStart + 1, zEnd - zStart - 1);
            if (float.TryParse(zStr, out f))
                vector3.Z = f;
            return vector3;
        }
        private string GetCharacterNameFromHoveredInfo(string hoveredCharacterInfo)
        {
            // Sample : "Name: [Agents of Orisha 3 [Agents]] X:[137.50] Y:[-0.50] Z:[-77.23]"
            int nameEnd = hoveredCharacterInfo.IndexOf("[", 7);
            string name = hoveredCharacterInfo.Substring(7, nameEnd - 7).Trim();
            if (name.EndsWith("] X:"))
            {
                name = name.Substring(0, name.LastIndexOf("]"));
            }
            return name;
        }

        private void GenerateMenuFileForCharacter(CrowdMemberModel character)
        {
            string fileCharacterMenu = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_TEXTS_FOLDERNAME, Constants.GAME_LANGUAGE_FOLDERNAME, Constants.GAME_MENUS_FOLDERNAME, Constants.GAME_CHARACTER_MENU_FILENAME);
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "Module.HeroVirtualTabletop.Resources.character.mnu";
            List<string> menuFileLines = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    menuFileLines.Add(line);
                }

                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < menuFileLines.Count -1; i++)
                {
                    sb.AppendLine(menuFileLines[i]);
                }
                if(character.OptionGroups != null && character.OptionGroups.Count > 0)
                {
                    foreach (var optionGroup in character.OptionGroups)
                    {
                        sb.AppendLine(string.Format("Menu \"{0}\"", optionGroup.Name));
                        sb.AppendLine("{");
                        foreach (ICharacterOption option in optionGroup.Options)
                        {
                            string whiteSpaceReplacedOptionGroupName = optionGroup.Name.Replace(" ", Constants.SPACE_REPLACEMENT_CHARACTER);
                            string whiteSpaceReplacedOptionName = option.Name.Replace(" ", Constants.SPACE_REPLACEMENT_CHARACTER);
                            sb.AppendLine(string.Format("Option \"{0}\" \"bind_save_file {1}{2}{3}.txt\"", option.Name, whiteSpaceReplacedOptionGroupName, Constants.DEFAULT_DELIMITING_CHARACTER, whiteSpaceReplacedOptionName));
                        }
                        sb.AppendLine("}");
                    }
                }
                sb.AppendLine(menuFileLines[menuFileLines.Count - 1]);

                File.WriteAllText(
                    fileCharacterMenu, sb.ToString()
                    );
            }
        }

        void clickTimer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            clickTimer1.Stop();
            bool bWindow = WindowsUtilities.GetForegroundWindow() == WindowsUtilities.FindWindow("CrypticWindow", null);
            if(bWindow)
            {
                string mouseXYZInfo = IconInteractionUtility.GetMouseXYZFromGame();
                Vector3 mouseDirection = GetDirectionVectorFromMouseXYZInfo(mouseXYZInfo);
                AttackDirection direction = new AttackDirection(mouseDirection);
                this.currentAttack.AnimateAttack(direction, attackingCharacter);
            }
        }

        void clickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            clickTimer.Stop();

            if (isQuadrupleClick)
            {
                ToggleManueverWithCamera();
            }
            else if (isTripleClick)
            {
                Character character = Participants.FirstOrDefault(p => (p as Character).HasBeenSpawned && (p as Character).gamePlayer.Pointer == targetObserver.CurrentTargetPointer) as Character;
                if (character != null)
                    ActivateCharacter(character);
            }
            else if (isDoubleClick)
            {
                TargetAndFollow();
            }

            clickCount = 0;
            isDoubleClick = isTripleClick = isQuadrupleClick = false;
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(this.Spawn);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop, this.CanClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(this.ToggleTargeted, this.CanToggleTargeted);
            this.SavePositionCommand = new DelegateCommand<object>(this.SavePostion, this.CanSavePostion);
            this.PlaceCommand = new DelegateCommand<object>(this.Place, this.CanPlace);
            this.TargetAndFollowCommand = new DelegateCommand<object>(this.TargetAndFollow, this.CanTargetAndFollow);
            this.MoveTargetToCameraCommand = new DelegateCommand<object>(this.MoveTargetToCamera, this.CanMoveTargetToCamera);
            this.ToggleManeuverWithCameraCommand = new DelegateCommand<object>(this.ToggleManeuverWithCamera, this.CanToggleManeuverWithCamera);
            this.EditCharacterCommand = new DelegateCommand<object>(this.EditCharacter, this.CanEditCharacter);
            this.ActivateCharacterCommand = new DelegateCommand<object>(this.ActivateCharacter, this.CanActivateCharacter);
            this.ResetCharacterStateCommand = new DelegateCommand<object>(this.ResetCharacterState);
            this.AreaAttackTargetCommand = new DelegateCommand<object>(this.TargetCharacterForAreaAttack);
            this.AreaAttackTargetAndExecuteCommand = new DelegateCommand<object>(this.TargetAndExecuteAreaAttack);
        }

        #endregion

        #region Methods

        private void Commands_RaiseCanExecuteChanged()
        {
            this.ClearFromDesktopCommand.RaiseCanExecuteChanged();
            this.ToggleTargetedCommand.RaiseCanExecuteChanged();
            this.SavePositionCommand.RaiseCanExecuteChanged();
            this.PlaceCommand.RaiseCanExecuteChanged();
            this.TargetAndFollowCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            this.ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
            this.EditCharacterCommand.RaiseCanExecuteChanged();
            this.ActivateCharacterCommand.RaiseCanExecuteChanged();
        }

        public void RaiseEventToImportRosterMember()
        {
            this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(null);
        }

        private void CheckRosterConsistency(IEnumerable<CrowdMemberModel> members)
        {
            foreach (CrowdMemberModel member in members)
            {
                if (!Participants.Contains(member))
                {
                    AddParticipants(new List<CrowdMemberModel>() { member });
                }
            }
        }

        public ICrowdMemberModel GetCurrentTarget()
        {
            MemoryElement target = new MemoryElement();
            return this.Participants.FirstOrDefault((x) => {
                return (x as CrowdMemberModel).Label == target.Label;
            });
        }

        private void synchSelectionWithGame()
        {
            List<CrowdMemberModel> unselected = oldSelection.Except(SelectedParticipants.Cast<CrowdMemberModel>()).ToList();
            unselected.ForEach(
                (member) =>
                {
                    if (!member.HasBeenSpawned)
                        return;
                    if (!(this.isPlayingAttack && this.attackingCharacter != null && this.attackingCharacter.Name == member.Name))
                        member.Deactivate();
                    oldSelection.Remove(member);
                });
            List<CrowdMemberModel> selected = SelectedParticipants.Cast<CrowdMemberModel>().Except(oldSelection).ToList();
            if (selected.Count > 1)
                selected.ForEach(
                    (member) =>
                    {
                        if (!member.HasBeenSpawned)
                            return;
                        if (!(this.isPlayingAttack && this.attackingCharacter != null && this.attackingCharacter.Name == member.Name)) // Don't make the attacking character blue
                            member.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                        oldSelection.Add(member);
                    });
        }

        private void TargetObserver_TargetChanged(object sender, EventArgs e)
        {
            // sometimes i get exception here in this method's dispatcher calls during application closing. we need to handle this
            try
            {
                uint currentTargetPointer = targetObserver.CurrentTargetPointer;
                CrowdMemberModel currentTarget = (CrowdMemberModel)Participants.DefaultIfEmpty(null).Where(
                    (p) =>
                    {
                        Character c = p as Character;
                        return c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                    }).FirstOrDefault();
                AddDesktopTargetToRosterSelection(currentTarget);
            }
            catch (TaskCanceledException ex)
            {

            }
        }

        private void AddDesktopTargetToRosterSelection(CrowdMemberModel currentTarget)
        {
            Dispatcher.Invoke(() =>
            {
                if (SelectedParticipants == null)
                    SelectedParticipants = new ObservableCollection<object>() as IList;
            });

            if (currentTarget == null)
                return;
            if ((bool)Dispatcher.Invoke(DispatcherPriority.Normal, new Func<bool>(() => { return Keyboard.Modifiers != ModifierKeys.Control; })))
            {
                Dispatcher.Invoke(() => { if (SelectedParticipants != null) SelectedParticipants.Clear(); });
            }
            if (!SelectedParticipants.Contains(currentTarget))
            {
                Dispatcher.Invoke(() => { SelectedParticipants.Add(currentTarget); OnPropertyChanged("SelectedParticipants"); });
            }
        }

        #region Add Participants
        private void AddParticipants(IEnumerable<CrowdMemberModel> crowdMembers)
        {
            foreach (var crowdMember in crowdMembers)
            {
                Participants.Add(crowdMember);
                InitializeAttackEventHandlers(crowdMember);
                CheckIfCharacterExistsInGame(crowdMember);
            }
            Participants.Sort();
        }

        private void CheckIfCharacterExistsInGame(CrowdMemberModel crowdMember)
        {
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Publish(null);
            MemoryElement oldTargeted = new MemoryElement();
            crowdMember.Target();
            MemoryElement currentTargeted = new MemoryElement();
            if (currentTargeted.Label == crowdMember.Label)
            {
                crowdMember.SetAsSpawned();
            }
            try
            {
                oldTargeted.Target();
            }
            catch { }
            this.eventAggregator.GetEvent<ListenForTargetChanged>().Publish(null);
        }
        #endregion

        #region Delete Participant
        private void DeleteParticipant(ICrowdMemberModel crowdMember)
        {
            if (this.SelectedParticipants == null)
                this.SelectedParticipants = new List<CrowdMemberModel>();
            this.SelectedParticipants.Clear();
            if (crowdMember is CrowdMemberModel)
            {
                this.SelectedParticipants.Add(crowdMember);
            }
            else if (crowdMember is CrowdModel)
            {
                var participants = this.Participants.Where(p => p.RosterCrowd.Name == crowdMember.Name);
                foreach (var participant in participants)
                    this.SelectedParticipants.Add(participant);
            }
            this.ClearFromDesktop(null);
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
        #endregion

        #region Spawn
        private void Spawn(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Spawn();
            }
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region Clear from Desktop
        private bool CanClearFromDesktop(object state)
        {
            if (SelectedParticipants != null && SelectedParticipants.Count > 0 && !this.isPlayingAttack)
            {
                return true;
            }
            return false;
        }

        private void ClearFromDesktop(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ClearFromDesktop();
            }
            while (SelectedParticipants.Count != 0)
            {
                var participant = SelectedParticipants[0] as CrowdMemberModel;
                Participants.Remove(participant);
                SelectedParticipants.Remove(participant);
                participant.RosterCrowd = null;
            }
            Commands_RaiseCanExecuteChanged();
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        #endregion

        #region Save Positon

        private bool CanSavePostion(object state)
        {
            bool canSavePosition = false;
            if (this.SelectedParticipants != null)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var character = c as Character;
                    if (character != null && character.HasBeenSpawned)
                    {
                        canSavePosition = true;
                        break;
                    }
                }
            }
            return canSavePosition;
        }
        private void SavePostion(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.SavePosition();
            }
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.PlaceCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Place
        private bool CanPlace(object state)
        {
            bool canPlace = false;
            if (this.SelectedParticipants != null)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var crowdMemberModel = c as CrowdMemberModel;
                    if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME && crowdMemberModel.SavedPosition != null)
                    {
                        canPlace = true;
                        break;
                    }
                    else if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                    {
                        CrowdModel rosterCrowdModel = crowdMemberModel.RosterCrowd as CrowdModel;
                        if (rosterCrowdModel.SavedPositions.ContainsKey(crowdMemberModel.Name))
                        {
                            canPlace = true;
                            break;
                        }
                    }
                }
            }
            return canPlace;
        }
        private void Place(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.Place();
            }
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region ToggleTargeted

        private bool CanToggleTargeted(object state)
        {
            bool canToggleTargeted = false;
            if (IsSingleSpawnedCharacterSelected)
            {
                canToggleTargeted = true;
            }
            return canToggleTargeted;
        }

        private void ToggleTargeted(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ToggleTargeted();
            }
        }

        #endregion

        #region Target And Follow

        private bool CanTargetAndFollow(object state)
        {
            return CanToggleTargeted(state);
        }

        private void TargetAndFollow(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.TargetAndFollow();
            }
        }

        public void TargetOrFollow()
        {
            if (this.CanToggleTargeted(null))
            {
                CrowdMemberModel member = SelectedParticipants[0] as CrowdMemberModel;
                if (member.IsTargeted)
                {
                    if (this.isPlayingAttack)
                        member.Target();
                    else if (this.isCharacterReset) // character has been selected to reset state, so just target
                    {
                        this.isCharacterReset = false; // reset this flag
                        member.Target();
                    }
                    else
                        member.TargetAndFollow();
                }

                else
                    member.ToggleTargeted();

                if (this.isPlayingAttack)
                {
                    if (member.Name != this.attackingCharacter.Name && member.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && member.Name != Constants.DEFAULT_CHARACTER_NAME)
                    {
                        if (!isPlayingAreaEffect)
                        {
                            if (this.targetCharacters.FirstOrDefault(tc => tc.Name == member.Name) == null)
                                this.targetCharacters.Add(member);
                            ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                            attackConfig.AttackMode = AttackMode.Defend;
                            attackConfig.AttackEffectOption = AttackEffectOption.None;
                            member.ActiveAttackConfiguration = attackConfig;
                            member.ActiveAttackConfiguration.IsCenterTarget = true;
                            if (PopupService.IsOpen("ActiveAttackView") == false)
                                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
                        }
                    }
                }
            }
        }
        public void TargetAndFollow()
        {
            if (this.CanTargetAndFollow(null))
            {
                this.TargetAndFollow(null);
            }
        }

        #endregion

        #region Move Target to Camera

        private bool CanMoveTargetToCamera(object arg)
        {
            bool canMoveTargetToCamera = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToCamera = false;
                return canMoveTargetToCamera;
            }
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.HasBeenSpawned)
                {
                    canMoveTargetToCamera = false;
                    break;
                }
            }
            return canMoveTargetToCamera;
        }

        private void MoveTargetToCamera(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.MoveToCamera();
            }
        }

        #endregion

        #region ToggleManeuverWithCamera


        private bool CanToggleManeuverWithCamera(object arg)
        {
            bool canManeuverWithCamera = false;
            if (this.SelectedParticipants != null && SelectedParticipants.Count == 1 && ((SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned || (SelectedParticipants[0] as CrowdMemberModel).ManeuveringWithCamera))
            {
                canManeuverWithCamera = true;
            }
            return canManeuverWithCamera;
        }

        private void ToggleManeuverWithCamera(object obj)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                member.ToggleManueveringWithCamera();
            }
            Commands_RaiseCanExecuteChanged();
        }

        public void ToggleManueverWithCamera()
        {
            if (this.CanToggleManeuverWithCamera(null))
            {
                this.ToggleManeuverWithCamera(null);
            }
        }

        #endregion

        #region Edit Character

        private bool CanEditCharacter(object state)
        {
            return this.SelectedParticipants != null && this.SelectedParticipants.Count == 1 && !this.isPlayingAttack;
        }

        private void EditCharacter(object state)
        {
            CrowdMemberModel c = this.SelectedParticipants[0] as CrowdMemberModel;
            this.eventAggregator.GetEvent<EditCharacterEvent>().Publish(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(c, null));
        }

        #endregion

        #region Activate Character

        private bool CanActivateCharacter(object state)
        {
            //return CanToggleTargeted(state) && (SelectedParticipants[0] as Character).HasBeenSpawned ;
            return !this.isPlayingAttack;
        }

        private void ActivateCharacter(object state)
        {
            ActivateCharacter();
        }

        private void ActivateCharacter(Character character = null, string selectedOptionGroupName = null, string selectedOptionName = null)
        {
            Action action = delegate ()
            {
                if (character == null)
                    if (SelectedParticipants.Count == 0)
                        return;
                    else
                        character = SelectedParticipants[0] as CrowdMemberModel;
                this.ActiveCharacter = character as CrowdMemberModel;
                this.eventAggregator.GetEvent<ActivateCharacterEvent>().Publish(new Tuple<Character, string, string>(character, selectedOptionGroupName, selectedOptionName));
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        #endregion

        #region Attack / Area Attack


        private void InitializeAttackEventHandlers(Attack attack)
        {
            attack.AttackInitiated -= this.Ability_AttackInitiated;
            attack.AttackInitiated += Ability_AttackInitiated;
        }

        private void InitializeAttackEventHandlers(Character character)
        {
            foreach (var ability in character.AnimatedAbilities)
            {
                var attack = ability as Attack;
                InitializeAttackEventHandlers(attack);
            }
        }

        private void Ability_AttackInitiated(object sender, EventArgs e)
        {
            Character targetCharacter = sender as Character;
            CustomEventArgs<Attack> customEventArgs = e as CustomEventArgs<Attack>;
            if (targetCharacter != null && customEventArgs != null)
            {
                Helper.GlobalVariables_IsPlayingAttack = true;
                // Change mouse pointer to bulls eye
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Mouse.OverrideCursor = cursor;
                // Inform Roster to update attacker
                this.eventAggregator.GetEvent<AttackInitiatedEvent>().Publish(new Tuple<Character, Attack>(targetCharacter, customEventArgs.Value));
            }
        }

        private void InitiateRosterCharacterAttack(Tuple<Character, Attack> attackInitiatedEventTuple)
        {
            this.targetCharacters = new List<Character>();
            Character attackingCharacter = attackInitiatedEventTuple.Item1;
            Attack attack = attackInitiatedEventTuple.Item2;
            CrowdMemberModel rosterCharacter = this.Participants.FirstOrDefault(p => p.Name == attackingCharacter.Name) as CrowdMemberModel;
            if (rosterCharacter != null && attack != null)
            {
                this.isPlayingAttack = true;
                if (attack.IsAreaEffect)
                {
                    this.isPlayingAreaEffect = true;
                    CreateBindSaveFiles();
                    this.fileSystemWatcher.EnableRaisingEvents = true;
                }
                Commands_RaiseCanExecuteChanged();
                targetObserver.TargetChanged += AttackTargetUpdated;
                this.currentAttack = attack;
                this.attackingCharacter = attackingCharacter;
                // Update character properties - icons in roster should show
                rosterCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None };
            }
        }

        private void AttackTargetUpdated(object sender, EventArgs e)
        {
            uint currentTargetPointer = targetObserver.CurrentTargetPointer;
            CrowdMemberModel currentTarget = (CrowdMemberModel)Participants.DefaultIfEmpty(null).Where(
                (p) =>
                {
                    Character c = p as Character;
                    return c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                }).FirstOrDefault();
            Action action = delegate ()
            {
                if (this.isPlayingAttack && currentTarget != null)
                {
                    if (currentTarget.Name != this.attackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                    {
                        if (!isPlayingAreaEffect)
                        {
                            if(targetCharacters.Count == 0)// choose only one character for vanilla attack
                            {
                                this.targetCharacters.Add(currentTarget);
                                currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                                attackConfig.AttackMode = AttackMode.Defend;
                                attackConfig.AttackEffectOption = AttackEffectOption.None;
                                currentTarget.ActiveAttackConfiguration = attackConfig;
                                currentTarget.ActiveAttackConfiguration.IsCenterTarget = true;
                                if (PopupService.IsOpen("ActiveAttackView") == false)
                                    this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
                            }
                        }
                    }
                }
                else if (currentTarget == null) // Cancel attack
                {
                    this.CancelActiveAttack(this.currentAttack);
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void CancelActiveAttack(object state)
        {
            Action action = delegate()
            {
                if (this.isPlayingAttack)
                {
                    Helper.GlobalVariables_IsPlayingAttack = false;
                    Commands_RaiseCanExecuteChanged();
                    List<Character> defendingCharacters = new List<Character>();
                    if(state is List<Character>)
                        defendingCharacters = state as List<Character>;
                    this.CancelAttack(defendingCharacters); 
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void HandleCharacterOptionAddition(ICharacterOption option)
        {
            if (option is Attack)
                this.InitializeAttackEventHandlers(option as Attack);
        }

        private void LaunchActiveAttack(Tuple<List<Character>, Attack> tuple)
        {
            List<Character> defendingCharacters = tuple.Item1;
            Attack attack = tuple.Item2;
            foreach (var defender in defendingCharacters)
            {
                defender.Deactivate(); // restore original costume
            }
            attack.AnimateAttackSequence(attackingCharacter, defendingCharacters);
            this.ResetAttack(defendingCharacters);
        }

        private void CancelAttack(List<Character> defendingCharacters)
        {
            if (this.attackingCharacter != null)
                this.attackingCharacter.Deactivate();
            foreach (var defender in defendingCharacters)
            {
                defender.Deactivate(); // restore original costume
            }
            this.ResetAttack(defendingCharacters);
        }

        private void ResetAttack(List<Character> defenders)
        {
            this.isPlayingAttack = false;
            this.isPlayingAreaEffect = false;
            targetObserver.TargetChanged -= AttackTargetUpdated;
            //this.currentAttack.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(); });
            this.currentAttack.Stop();

            // Hide attack icon from attacking character
            if (this.attackingCharacter != null && this.attackingCharacter.ActiveAttackConfiguration != null)
                this.attackingCharacter.ActiveAttackConfiguration.AttackMode = AttackMode.None;
            this.attackingCharacter = null;

            Helper.GlobalVariables_IsPlayingAttack = false;
            this.eventAggregator.GetEvent<CloseActiveAttackEvent>().Publish(this.currentAttack); 
            foreach (var defender in defenders)
            {
                defender.ActiveAttackConfiguration.AttackMode = AttackMode.None;
                //if (!this.currentAttack.OnHitAnimation.Persistent)
                //    this.currentAttack.OnHitAnimation.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(defender); });
                //defender.Deactivate(); // restore original costume
            }

            // Update Mouse cursor
            Mouse.OverrideCursor = Cursors.Arrow;

            this.currentAttack = null;
            this.fileSystemWatcher.EnableRaisingEvents = false;
            this.Commands_RaiseCanExecuteChanged();
        }

        private void ResetCharacterState(object state)
        {
            if (state != null && this.Participants != null)
            {
                string charName = state.ToString();
                Character defendingCharacter = this.Participants.FirstOrDefault(p => p.Name == charName) as Character;
                if (defendingCharacter != null && defendingCharacter.ActiveAttackConfiguration != null)
                {
                    // If he is just stunned make him normal
                    if(defendingCharacter.ActiveAttackConfiguration.AttackEffectOption == AttackEffectOption.Stunned)
                    {
                        KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                        defendingCharacter.Target(false);
                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        keyBindsGenerator.CompleteEvent();
                    }
                    // Else make him stand up 
                    else if (Helper.GlobalDefaultAbilities != null && Helper.GlobalDefaultAbilities.Count > 0)
                    {
                        var globalStandUpAbility = Helper.GlobalDefaultAbilities.FirstOrDefault(a => a.Name == Constants.STANDUP_ABILITY_NAME);
                        if (globalStandUpAbility != null && globalStandUpAbility.AnimationElements != null && globalStandUpAbility.AnimationElements.Count > 0)
                        {
                            globalStandUpAbility.Play(false, defendingCharacter);
                        }
                    }
                    // Update icons in Roster
                    defendingCharacter.ActiveAttackConfiguration = new ActiveAttackConfiguration { AttackEffectOption = AttackEffectOption.None, AttackMode = AttackMode.None };
                    this.isCharacterReset = true;
                }
            }
        }

        private void TargetCharacterForAreaAttack(object state)
        {
            foreach (var participant in this.SelectedParticipants)
            {
                Character currentTarget = participant as Character;
                if (currentTarget.Name != this.attackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                {
                    if (this.targetCharacters.FirstOrDefault(tc => tc.Name == currentTarget.Name) == null)
                        this.targetCharacters.Add(currentTarget);
                    currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    currentTarget.ActiveAttackConfiguration = attackConfig;
                }
            }
        }

        private void TargetAndExecuteAreaAttack(object state)
        {
            foreach (var participant in this.SelectedParticipants)
            {
                Character currentTarget = participant as Character;
                if (currentTarget.Name != this.attackingCharacter.Name && currentTarget.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && currentTarget.Name != Constants.DEFAULT_CHARACTER_NAME)
                {
                    if (this.targetCharacters.FirstOrDefault(tc => tc.Name == currentTarget.Name) == null)
                        this.targetCharacters.Add(currentTarget);
                    currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    currentTarget.ActiveAttackConfiguration = attackConfig;
                }
            }
            if (PopupService.IsOpen("ActiveAttackView") == false)
                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Action action = delegate ()
            {
                this.isMenuDisplayed = false;
                if (this.isPlayingAreaEffect)
                {
                    
                    if (e.Name == Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_FILENAME)
                    {
                        TargetCharacterForAreaAttack(null);
                    }
                    else if (e.Name == Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_FILENAME)
                    {
                        TargetAndExecuteAreaAttack(null);
                    }
                }
                else
                {
                    fileSystemWatcher.EnableRaisingEvents = false;
                    switch (e.Name)
                    {
                        case Constants.GAME_CHARACTER_BINDSAVE_SPAWN_FILENAME:
                            Spawn(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_PLACE_FILENAME:
                            Place(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_SAVEPOSITION_FILENAME:
                            SavePostion(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_MOVECAMERATOTARGET_FILENAME:
                            TargetAndFollow(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_MOVETARGETTOCAMERA_FILENAME:
                            MoveTargetToCamera(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_MANUEVERWITHCAMERA_FILENAME:
                            ToggleManeuverWithCamera(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_ACTIVATE_FILENAME:
                            ActivateCharacter(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_CLEARFROMDESKTOP_FILENAME:
                            ClearFromDesktop(null);
                            break;
                        case Constants.GAME_CHARACTER_BINDSAVE_CLONEANDLINK_FILENAME:
                            {
                                Character character = this.SelectedParticipants != null && this.SelectedParticipants.Count == 1 ? this.SelectedParticipants[0] as Character : null;
                                this.eventAggregator.GetEvent<CloneLinkCrowdMemberEvent>().Publish(character as CrowdMemberModel);
                                break;
                            }
                        default:
                            {
                                Character character = this.SelectedParticipants != null && this.SelectedParticipants.Count == 1 ? this.SelectedParticipants[0] as Character : null;
                                int index = e.Name.IndexOf(Constants.DEFAULT_DELIMITING_CHARACTER);
                                if (index > 0 && character != null)
                                {
                                    string whiteSpaceReplacedOptionGroupName = e.Name.Substring(0, index - 1); // The special characters are translated to two characters, so need to subtract one additional character
                                    string whiteSpceReplacedOptionName = e.Name.Substring(index + 1, e.Name.Length - index - 5); // to get rid of the .txt part
                                    string optionGroupName = whiteSpaceReplacedOptionGroupName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                    string optionName = whiteSpceReplacedOptionName.Replace(Constants.SPACE_REPLACEMENT_CHARACTER_TRANSLATION, " ");
                                    ActivateCharacter(character, optionGroupName, optionName);
                                }
                                break;
                            }
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void CreateBindSaveFiles()
        {
            string filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_FILENAME);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            filePath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_AREA_ATTACK_BINDSAVE_TARGET_EXECUTE_FILENAME);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
        }

        #endregion

        #endregion
    }
}
