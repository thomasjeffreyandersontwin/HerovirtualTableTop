using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Crowds;
using System.Collections;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using System.IO;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Identities;
using Moq;

namespace Module.UnitTest.Characters
{
    [TestClass]
    public class CharacterTestSuite : BaseCrowdTest
    {
        private RosterExplorerViewModel rosterExplorerViewModel;


        [TestInitialize]
        public void TestInitialize()
        {
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            this.numberOfItemsFound = 0;

            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, targetObserverMock.Object, eventAggregatorMock.Object);
        }

        #region Spawn Tests
        [TestMethod]
        public void SpawnCharacter_CreatesCharacterInGame()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("spawn_npc model_Statesman Batman"));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }
        [TestMethod]
        public void SpawnCharacter_UntargetEveryOtherBeforeSpawning()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_enemy_near"));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }
        [TestMethod]
        public void SpawnCharacter_WithNoIdentityGeneratesSpawnKeybindUsingDefaultModel()
        {
            SpawnCharacter_CreatesCharacterInGame();
        }
        [TestMethod]
        public void SpawnCharacter_WithMultipleIdentitiesGeneratesSpawnKeybindUsingSpecifiedActiveIdentity()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("Panzer", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);
            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            string result = sr.ReadLine();
            Assert.IsTrue(result.Contains(string.Format("spawn_npc Model_Statesman {0} [{1}]$$target_name {0} [{1}]$$load_costume {2}", character.Name, character.RosterCrowd.Name, character.ActiveIdentity.Surface)));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }
        [TestMethod]
        public void SpawnCharacter_WithIdentityThatHasAModelGeneratesSpawnKeybindUsingModel()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("1stSigArcIssue4_Doctor_Female", HeroVirtualTabletop.Library.Enumerations.IdentityType.Model);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("spawn_npc 1stSigArcIssue4_Doctor_Female {0} [{1}]", character.Name, character.RosterCrowd.Name)));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }
        [TestMethod]
        public void SpawnCharacter_WithIdentityThatHasACostumeGeneratesSpawnKeybindUsingCostume()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            character.ActiveIdentity = new Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            string result = sr.ReadLine();
            string valid = string.Format("Y \"target_enemy_near$$nop$$spawn_npc Model_Statesman {0} [{1}]$$target_name {0} [{1}]$$load_costume {2}\"", character.Name, character.RosterCrowd.Name, character.ActiveIdentity.Surface);
            Assert.AreEqual(valid, result);

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }
        [TestMethod]
        public void SpawnCharacter_AssignsLabelWithBothCharacterAndCrowdName()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);
            string result = sr.ReadLine();

            Assert.IsTrue(result.Contains(string.Format("spawn_npc Model_Statesman {0} [{1}]", character.Name, character.RosterCrowd.Name)));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }

        #endregion

        #region Remove Tests
        [TestMethod]
        public void RemoveCharacterFromDesktop_GeneratesTargetAndDeleteKeybind()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            rosterExplorerViewModel.ClearFromDesktopCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_name Batman$$delete_npc"));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }

        #endregion

        #region Target Tests

        [TestMethod]
        public void TargetCharacter_TargetsCharacterUsingMemoryInstancesIfItExists()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);
            Mock<IMemoryElement> memoryElementMock = new Mock<IMemoryElement>();
            memoryElementMock.Setup(x => x.IsReal).Returns(true);
            character.gamePlayer = memoryElementMock.Object;
            rosterExplorerViewModel.ToggleTargetedCommand.Execute(null);
            memoryElementMock.Verify(x => x.Target(), Times.Once());
        }
        [TestMethod]
        public void TargetCharacter_GeneratesTargetKeybindIfNoMemoryInstance()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.ToggleTargetedCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("target_name {0}", character.Label)));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);

        }

        [TestMethod]
        public void TargetAndFollowCharacter_GeneratesTargetAndFollowKeybind()
        {
            CrowdMemberModel character = new CrowdMemberModel("Spyder");

            character.TargetAndFollow();

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("target_name {0}$$follow", character.Label)));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }

        #endregion

        #region Un Target Tests

        [TestMethod]
        public void UnTargetCharacter_GeneratesCorrectKeybinds()
        {
            CrowdMemberModel character = new CrowdMemberModel();

            character.UnTarget();

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_enemy_near"));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }

        #endregion

        #region Move To Camera

        [TestMethod]
        public void MoveCharacterToCamera_GeneratesCorrectKeyBinds()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Participants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.MoveTargetToCameraCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("target_name {0}$$move_npc", character.Label)));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);
        }

        #endregion

        #region Maneuver with Camera

        [TestMethod]
        public void ManeuverWithCameraToggleOn_WithCostumeBasedCharacter_TargetAndFollowsAndDeletesCharacterThenLoadsCharactersCostumeInCamera()
        {
            CrowdMemberModel character = new CrowdMemberModel("Spyder");
            character.ActiveIdentity = new Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);

            character.Position = new Mock<Position>(false, (uint)0).Object;
            //Camera.position = new Mock<Position>(false, (uint)0).Object;

            character.ToggleManueveringWithCamera();

            Assert.AreEqual(string.Format("target_name {0}$$follow", character.Label), KeyBindsGenerator.generatedKeybinds[0]);
            Assert.AreEqual(string.Format("target_name {0}$$delete_npc", character.Label), KeyBindsGenerator.generatedKeybinds[1]);
            Assert.AreEqual(string.Format("load_costume {0}", character.ActiveIdentity.Surface), KeyBindsGenerator.generatedKeybinds[2]);
        }

        [TestMethod]
        public void ManeuverWithCameraToggleOn_WithModelBasedCharacter_TargetAndFollowsAndDeletesCharacterThenCameraBecomesNPC()
        {
            CrowdMemberModel character = new CrowdMemberModel("Character");
            character.Position = new Mock<Position>(false, (uint)0).Object;
            //Camera.position = new Mock<Position>(false, (uint)0).Object;

            character.ToggleManueveringWithCamera();

            Assert.AreEqual(string.Format("target_name {0}$$follow", character.Label), KeyBindsGenerator.generatedKeybinds[0]);
            Assert.AreEqual(string.Format("target_name {0}$$delete_npc", character.Label), KeyBindsGenerator.generatedKeybinds[1]);
            Assert.AreEqual(string.Format("benpc {0}", character.ActiveIdentity.Surface), KeyBindsGenerator.generatedKeybinds[2]);
        }

        [TestMethod]
        public void ManeuverWithCameraToggleOff_ReloadsCameraSkinOnCameraThenSpawnsCharacter()
        {
            CrowdMemberModel character = new CrowdMemberModel("Spyder");

            character.Position = new Mock<Position>(false, (uint)0).Object;
            //Camera.position = new Mock<Position>(false, (uint)0).Object;

            character.ToggleManueveringWithCamera();
            character.ToggleManueveringWithCamera();

            Assert.AreEqual(string.Format("target_enemy_near$$benpc {0}", (new Camera()).Surface), KeyBindsGenerator.generatedKeybinds[3]);
            Assert.AreEqual(string.Format("target_enemy_near$$nop$$spawn_npc {0} {1}$$target_name {1}$$benpc {0}", character.ActiveIdentity.Surface, character.Label), KeyBindsGenerator.generatedKeybinds[4]);
            
        }

        #endregion
    }
}
