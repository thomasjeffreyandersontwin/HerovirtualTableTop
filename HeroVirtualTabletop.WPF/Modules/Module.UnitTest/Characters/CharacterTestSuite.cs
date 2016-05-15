using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Crowds;
using System.Collections;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using System.IO;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Moq;

namespace Module.UnitTest.Characters
{
    [TestClass]
    public class CharacterTestSuite : BaseCrowdTest
    {
        private CharacterExplorerViewModel characterExplorerViewModel;
        private RosterExplorerViewModel rosterExplorerViewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            InitializeDefaultList();
            InitializeCrowdRepositoryMockWithDefaultList();
            this.numberOfItemsFound = 0;

            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, eventAggregatorMock.Object);
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
            Assert.IsTrue(result.Contains(string.Format("spawn_npc model_Statesman {0} [{1}]$$target_name {0} [{1}]$$load_costume {2}", character.Name, character.RosterCrowd.Name, character.ActiveIdentity.Surface)));

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

            character.ActiveIdentity = new HeroVirtualTabletop.Identities.Identity("Spyder", HeroVirtualTabletop.Library.Enumerations.IdentityType.Costume);

            rosterExplorerViewModel.SelectedParticipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            string result = sr.ReadLine();
            Assert.IsTrue(result.Contains(string.Format("spawn_npc model_Statesman {0} [{1}]$$target_name {0} [{1}]$$load_costume {2}", character.Name, character.RosterCrowd.Name, character.ActiveIdentity.Surface)));

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

            Assert.IsTrue(sr.ReadLine().Contains(string.Format("spawn_npc model_Statesman {0} [{1}]", character.Name, character.RosterCrowd.Name)));

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
    }
}
