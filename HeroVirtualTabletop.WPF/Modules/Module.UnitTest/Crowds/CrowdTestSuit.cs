using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.Shared;
using Moq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.UnitTest.Crowd
{
    #region Crowd Repository Test
    [TestClass]
    public class CrowdRepositoryTest : BaseCrowdTest
    {
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

        [TestMethod]
        public void GetCrowdCollection_CreatesNewRepositoryIfNoPresentRepositoryFile()
        {
            string testRepoFileName = "test.data";
            string fullFilePath = Path.Combine(testRepoFileName);
            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);
            // Here we are directly testing the repository and need to verify file i/o. So, not mocking.
            CrowdRepository crowdRepository = new CrowdRepository();
            crowdRepository.CrowdRepositoryPath = fullFilePath;
            List<CrowdModel> retrievedCrowdList = null;
            AutoResetEvent terminateEvent = new AutoResetEvent(false);
            bool testPassed = false;
            ThreadStart ts = delegate
            {
                crowdRepository.GetCrowdCollection(
                (List<CrowdModel> crowdList) =>
                {
                    try
                    {
                        retrievedCrowdList = crowdList;
                        Assert.IsTrue((File.Exists(fullFilePath)));
                        File.Delete(fullFilePath);
                        testPassed = true;
                    }
                    catch (AssertFailedException ex)
                    {
                        terminateEvent.Set();
                    }
                    finally
                    {
                        terminateEvent.Set();
                    }
                }
                );
            };
            Thread t = new Thread(ts);
            t.Start();
            terminateEvent.WaitOne();
            Assert.IsTrue(testPassed);
        }

        /// <summary>
        /// This is actually the acceptance test for crowd repository. Although the method name suggests only SaveCrowdCollection related test, it actually performs tests on both GetCrowdCollection
        /// and SaveCrowdCollection for their basic functionalities.
        /// </summary>
        [TestMethod]
        public void SaveCrowdCollection_SavesCrowdCollectionsConsistently()
        {
            CrowdModel crowd = new CrowdModel { Name = "Test Crowd 1" };
            CrowdModel childCrowd = new CrowdModel { Name = "Child Crowd 1" };
            CrowdMember crowdMember1 = new CrowdMember { Name = "Test CrowdMember 1" };
            CrowdMember crowdMember2 = new CrowdMember { Name = "Test CrowdMember 1.1" };
            crowd.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember1, childCrowd };
            childCrowd.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember2 };
            string testRepoFileName = "test.data";
            CrowdRepository crowdRepository = new CrowdRepository();
            crowdRepository.CrowdRepositoryPath = testRepoFileName;
            List<CrowdModel> crowdCollection = new List<CrowdModel>() { crowd };
            AutoResetEvent terminateEvent = new AutoResetEvent(false);
            bool testPassed = false;

            ThreadStart ts = delegate
            {
                crowdRepository.SaveCrowdCollection(() =>
                {
                    // More crowd members being added, repository shouldn't know
                    crowdCollection.Add(new CrowdModel() { Name = "New Crowd 1" });
                    crowd.CrowdMemberCollection.Add(new CrowdMember() { Name = "New CrowdMember 1" });

                    List<CrowdModel> retrievedCrowdList = null;
                    crowdRepository.GetCrowdCollection((List<CrowdModel> crowdList) =>
                    {
                        retrievedCrowdList = crowdList;
                        try
                        {
                            CrowdModel cmodel1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                            Assert.IsNull(cmodel1);
                            CrowdMember cm1 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "New CrowdMember 1").FirstOrDefault() as CrowdMember;
                            Assert.IsNull(cm1);
                            CrowdModel cmodel2 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                            Assert.IsNotNull(cmodel2);
                            CrowdMember cm2 = cmodel2.CrowdMemberCollection.Where(c => c.Name == "Test CrowdMember 1.1").FirstOrDefault() as CrowdMember;
                            Assert.IsNotNull(cm2);
                        }
                        catch (AssertFailedException ex)
                        {
                            terminateEvent.Set();
                        }
                        // Now save the updated crowd and check if repsitory knows about them
                        crowdRepository.SaveCrowdCollection(() =>
                        {
                            crowdRepository.GetCrowdCollection((List<CrowdModel> crowdListAnother) =>
                            {
                                retrievedCrowdList = crowdListAnother;
                                try
                                {
                                    CrowdModel cmodel1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                                    Assert.IsNotNull(cmodel1);
                                    CrowdMember cm1 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "New CrowdMember 1").FirstOrDefault() as CrowdMember;
                                    Assert.IsNotNull(cm1);
                                    CrowdModel cmodel2 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                                    Assert.IsNotNull(cmodel2);
                                    CrowdMember cm2 = cmodel2.CrowdMemberCollection.Where(c => c.Name == "Test CrowdMember 1.1").FirstOrDefault() as CrowdMember;
                                    Assert.IsNotNull(cm2);
                                    File.Delete(testRepoFileName);
                                    testPassed = true;
                                }
                                catch (AssertFailedException ex)
                                {
                                    terminateEvent.Set();
                                }
                                finally
                                {
                                    terminateEvent.Set();
                                }
                            });
                        }, crowdCollection);
                    });
                }, crowdCollection);
            };
            Thread t = new Thread(ts);
            t.Start();
            terminateEvent.WaitOne();
            Assert.IsTrue(testPassed);
        }
    }
    #endregion

    #region Crowd Member Test
    [TestClass]
    public class CrowdMemberTest : BaseCrowdTest
    {
        private CharacterExplorerViewModel characterExplorerViewModel;

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
        
        #region Add Crowd Tests
        /// <summary>
        /// Adding a crowd should obviously call the repository to update the crowd collection in the repository. This is an acceptance test. 
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryTest
        /// to make sure that the repository can save to file what it is being passed. So, making sure that repository is indeed being called with correct parameters would suffice here.
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdInRepository() 
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());// Starting with an empty repository
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == "Crowd")));
        }

        /// <summary>
        /// When the repository is empty, Adding a crowd should add it as a stand alone crowd and the All Characters list should not be present at this time
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesOnlyOneNewCrowdWithDefaultNameIfRepositoryIsEmpty()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cmodel2 = crowdList.Where(cr => cr.Name == "Crowd").FirstOrDefault();
            Assert.IsTrue(cmodel1 == null && cmodel2 != null);

        }

        /// <summary>
        /// The name of an added crowd should be unique, and should be "Crowd" or "Crowd (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesCrowdsWithUniqueNames()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == "Crowd");
            var cr2 = crowdList.Where(cr => cr.Name == "Crowd (1)").FirstOrDefault();
            Assert.IsTrue(cr1.Count() == 1 && cr2 != null);
        }

        /// <summary>
        /// If no crowd or character is selected, the new crowd should just be added as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesStandAloneCrowdIfNoCrowdOrCharactersSelected()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new crowd should be added under it and also as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdUnderSelectedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Gotham City" is selected
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added crowd should be in a total of two places - one stand alone and one nested position
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm=>cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded); 
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new crowd should be added under all of them, as well as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdUnderAllOccurrancesOfSelectedCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Assuming "Child Gotham City.1" is selected
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 3); // The added crowd should be in a total of 3 places - one stand alone and two nested positions
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd1.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "League of Shadows").FirstOrDefault();
            crowdAdded = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
        }
        /// <summary>
        /// If the current selected member is a character under All Characters or the All Characters crowd itself, the new crowd should be added as just another crowd and not under All Characters
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdAsStandAloneIfSelectedCrowdIsAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            // "All Characters" is the selected crowd as selecting a character under All Characters would also result in the containing crowd being selected in view model
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; 
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added crowd should be added only as a stand alone crowd
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            // And so on...
        }
        /// <summary>
        /// If the current selected member is a Character not under All Characters Crowd but in another Crowd, the new crowd should be added as a sibling of that character in all 
        /// occurances of that crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdAsSiblingOfSelectedCharacterNotUnderAllCharacters()
        {
            // Since selecting a character would result in the containing crowd to be selected in the view model, here the containing Crowd would be selected and this case 
            // has already been covered in another test above.
            AddCrowd_CreatesNewCrowdUnderAllOccurrancesOfSelectedCrowd();
        }
        #endregion

        #region Add Character Tests

        /// <summary>
        /// Adding a character should add it in the crowd. Depending on the situation, it could be added to one or more crowds. 
        /// This test is a combination of several other test cases.
        /// This is an acceptance test.
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterInCrowd()
        {
            AddCharacter_CreatesNewCharacterUnderAllCharactersIfNoCrowdOrCharacterIsSelected();
            TestInitialize();
            AddCharacter_CreatesNewCharacterUnderSelectedCrowd();
            TestInitialize();
            AddCharacter_CreatesNewCharacterUnderAllOccurrancesOfSelectedCrowd();
        }
        /// <summary>
        /// Adding a character should obviously call the repository to insert the character in the repository.
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryTest
        /// to make sure that the repository can save to file what it is being passed. So, making sure that repository is indeed being called with correct parameters would suffice here.
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterInRepository()
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
        /// When the repository is empty, Adding a character should first create the default All Characters crowd and the new character should be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesAllCharactersCrowdAndAddsNewCharacterUnderAllCharactersIfRepositoryIsEmpty()
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
        /// The name of an added character should be unique, and should be "Character" or "Character (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesCharactersWithUniqueNames()
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
        public void AddCharacter_CreatesNewCharacterUnderAllCharactersIfNoCrowdOrCharacterIsSelected()
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
        public void AddCharacter_CreatesNewCharacterUnderSelectedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Gotham City" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added character should be in a total of two places - one under All Characters and one under Gotham City
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            cm1 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new character should be added under all of them, as well as under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderAllOccurrancesOfSelectedCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Assuming "The Narrows" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            // The added character should be in a total of 3 places - one under All Characters and twice under The Narrows as it appears in two places in the Crowd list
            Assert.IsTrue(this.numberOfItemsFound == 3);
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(c => c.Name == "Character");
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            var cm2 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm2);
            var cm3 = (crowd1.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm3);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "League of Shadows").FirstOrDefault();
            var cm4 = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm4);
            var cm5 = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm5);
        }
        /// <summary>
        /// If the current selected member is a Character under All Characters, the new character should be added as a sibling of that character under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterAsSiblingOfSelectedCharacter()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            // Assuming a character is selected under All Characters. Then All Characters is the selected crowd
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

        #endregion

        #region Delete Crowd Tests
        /// <summary>
        /// Here we just need to make sure that the repository is being called with updated collection that does not have the deleted crowd in it.
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdFromRepository() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            //characterExplorerViewModel.DeleteCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Where(cm=>cm.Name == "Gotham City").FirstOrDefault() == null)));
        }

        #endregion

        #region Delete Character Tests
        public void DeleteCrowdmemberFromCrowd_RemovesCrowdMemberFromCrowd() { }

        #endregion

        #region Filter Character Tests
        public void FilterCharacter_ReturnsFilteredListOfCrowdMemberAndCrowds() { }

        #endregion

        #region Rename Character Tests
        public void RenameCharacter_UpdatesRepoCorrectly() { }

        #endregion

        #region Rename Crowd Tests
        public void SpawnCharacterInCrowd_AssignsLabelWithBothCharacterAndCrowdName() { }

        #endregion

        #region Save Placement of Character Tests
        public void SavePlacementOfCharacter_AssignsLocationToCrowdmembershipBasedOnCurrentPositionAndSavesCrowdmembershipToCrowdRepo() { }

        #endregion

        #region Place Character Tests
        public void PlaceCharacter_MovesCharacterToPositionBasedOnSavedLocation() { }

        #endregion

        #region Link and Paste Character Tests
        public void LinkAndPasteCharacterAcrossCharacters_AddsNewCrowdMemberWithCopiedCharacterToPastedCrowd() { }

        #endregion

        #region Command Delegation in Characters from Crowd Tests
        public void ExecutingSpawnOrSaveOrPlaceOrRemoveOnACrowd_ActivatesTheCommandOnAllCrowdmembersInCrowd() { }

        #endregion
    }
#endregion
}
