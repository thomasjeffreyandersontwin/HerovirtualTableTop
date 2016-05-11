using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using System.Collections;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using System.IO;

namespace Module.UnitTest.Roster
{
    [TestClass]
    public class RosterTestSuite : BaseCrowdTest
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

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterInPartecipants()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMemberModel));
        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterWithProperParentCrowd()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; //Can't use "All Characters" crowd
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMemberModel));

            Assert.AreEqual(rosterExplorerViewModel.Partecipants[0].RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterFromAllCharactersAddsWithoutCrowd()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; //Use "All Characters" crowd
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMemberModel));

            Assert.AreNotEqual(rosterExplorerViewModel.Partecipants[0].RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
            Assert.AreEqual(rosterExplorerViewModel.Partecipants[0].RosterCrowd.Name, Constants.NO_CROWD_CROWD_NAME);
        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterUseFirstParentCrowdFromNestedCrowds()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[2] as CrowdModel;
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMemberModel));

            Assert.AreEqual(rosterExplorerViewModel.Partecipants[0].RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
        }

        [TestMethod]
        public void AddCrowdToRoster_AddAllCharactersInPartecipants()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            foreach (ICrowdMemberModel x in characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection)
            {
                Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(x));
            }
        }

        [TestMethod]
        public void AddCrowdToRoster_AddAllCharactersWithProperParentCrowd()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[2];
            characterExplorerViewModel.SelectedCrowdMemberModel = null;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            foreach (ICrowdMemberModel x in characterExplorerViewModel.SelectedCrowdModel.CrowdMemberCollection)
            {
                Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(x));
                Assert.AreEqual(x.RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
            }
        }

        [TestMethod]
        public void RemoveCharacterFromDesktop_GeneratesTargetAndDeleteKeybindAndRemovesFromRoster()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMemberModel = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            CrowdMemberModel character = rosterExplorerViewModel.Partecipants[0] as CrowdMemberModel;

            rosterExplorerViewModel.SelectedPartecipants = new ArrayList { character };
            rosterExplorerViewModel.SpawnCommand.Execute(null);

            rosterExplorerViewModel.ClearFromDesktopCommand.Execute(null);

            StreamReader sr = File.OpenText(new KeyBindsGenerator().BindFile);

            Assert.IsTrue(sr.ReadLine().Contains("target_name Batman$$delete_npc"));

            sr.Close();
            File.Delete(new KeyBindsGenerator().BindFile);

            Assert.IsFalse(rosterExplorerViewModel.Partecipants.Contains(character));
        }
    }
}
