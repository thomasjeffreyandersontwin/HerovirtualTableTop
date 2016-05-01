using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.ViewModels;
using Module.HeroVirtualTabletop.Models;
using Moq;
using Module.Shared;
using Module.HeroVirtualTabletop.DomainModels;
using System.Windows.Controls;

namespace Module.UnitTest.Crowd
{
    /// <summary>
    /// Summary description for AddCharacterUnitTest
    /// </summary>
    [TestClass]
    public class AddCharacterUnitTest : BaseCrowdUnitTest
    {
        private CharacterExplorerViewModel characterExplorerViewModel;
        public AddCharacterUnitTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        [TestInitialize]
        public void TestInitialize()
        {
            InitializeDefaultList();
            this.numberOfItemsFound = 0;
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        /// When the repository is empty, Adding a character should first create the default All Characters crowd and the new character should be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_EmptyRepositoryTest()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            Assert.IsNotNull(cmodel1);
            var char1 = cmodel1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(char1);
        }
        /// <summary>
        /// Adding a crowd should obviously call the repository to update the crowd collection in the repository. This is an acceptance test. 
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryUnitTest
        /// to make sure that the repository can save to file what it is being passed. So, here we just need to make sure that repository is indeed being called with correct parameters.
        /// </summary>
        [TestMethod]
        public void AddCharacter_RepositoryBeingUpdatedTest()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());// Starting with an empty repository
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == Constants.ALL_CHARACTER_CROWD_NAME && cmList[0].CrowdMemberCollection.Count == 1 && cmList[0].CrowdMemberCollection[0].Name == "Character")));
        }
        /// <summary>
        /// The name of an added character should be unique, and should be "Character" or "Character (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCharacter_NewCharacterUniqueNameTest()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            Assert.IsTrue(cr1 != null && cr1.CrowdMemberCollection.Count() == 2);
            var cm1 = cr1.CrowdMemberCollection.Where(c => c.Name == "Character");
            var cm2 = cr1.CrowdMemberCollection.Where(c => c.Name == "Character (1)").FirstOrDefault();
            Assert.IsTrue(cm1 != null && cm1.Count() == 1 && cm2 != null);
        }

        /// <summary>
        /// If no crowd or character is selected, the new character should just be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_NoCurrentSelectedCrowdTest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 1);
            var cm1 = crowdList.ToList()[0].CrowdMemberCollection.Where(c => c.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new character should be added under it and also under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CurrentSelectionIsCrowdTest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Crowd 1" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added character should be in a total of two places - one under All Characters and one under Crowd 1
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 1").FirstOrDefault();
            cm1 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new character should be added under all of them, as well as under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CurrentSelectionIsCrowdWithMultiplePresenceTest()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Assuming "Child Crowd 1.1" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            // The added character should be in a total of 3 places - one under All Characters and twice under Child Crowd 1.1 as it appears in two places in the Crowd list
            Assert.IsTrue(this.numberOfItemsFound == 3); 
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(c => c.Name == "Character");
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 1").FirstOrDefault();
            var cm2 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm2);
            var cm3 = (crowd1.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm3);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 2").FirstOrDefault();
            var cm4 = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm4);
            var cm5 = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm5);
        }
        /// <summary>
        /// If the current selected member is a Character under All Characters, the new character should be added as a sibling of that character under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CurrentSelectionIsCharacterUnderAllCharactersTest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            // "All Characters" is the selected crowd as selecting a character would result in the containing crowd being selected in view model
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; 
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added character should be added under All Characters
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Character not under All Characters Crowd but in another Crowd, the new character should be added as a sibling of that character in all occurances 
        /// of that crowd and also under all characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CurrentSelectionIsCharacterNotUnderAllCharactersTest()
        {
            // Since selecting a character in the UI would result in the containing crowd to be selected in the view model, here the containing Crowd would be selected and this case 
            // has already been covered in another test above.
            AddCharacter_CurrentSelectionIsCrowdWithMultiplePresenceTest();
        }
    }
}
