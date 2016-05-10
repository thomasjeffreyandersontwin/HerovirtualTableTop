using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Roster;

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
            this.numberOfItemsFound = 0;

        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterInPartecipants()
        {
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            rosterExplorerViewModel = new RosterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, eventAggregatorMock.Object);

            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMember));
        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterWithProperParentCrowd()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMember));

            Assert.AreEqual(rosterExplorerViewModel.Partecipants[0].RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterFromAllCharactersAddsWithoutCrowd()
        {
            Assert.Fail(); //Need to ask for correct behaviour
        }

        [TestMethod]
        public void AddCharacterToRoster_AddCharacterUseFirstParentCrowdFromNestedCrowds()
        {
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel;
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1].CrowdMemberCollection[0] as CrowdMemberModel;
            characterExplorerViewModel.AddToRosterCommand.Execute(null);

            Assert.IsTrue(rosterExplorerViewModel.Partecipants.Contains(characterExplorerViewModel.SelectedCrowdMember));

            Assert.AreEqual(rosterExplorerViewModel.Partecipants[0].RosterCrowd, characterExplorerViewModel.SelectedCrowdModel);
        }

        [TestMethod]
        public void AddCrowdToRoster_AddAllCharactersInPartecipants()
        {
        }

        [TestMethod]
        public void AddCrowdToRoster_AddAllCharactersWithProperParentCrowd()
        {
        }
    }
}
