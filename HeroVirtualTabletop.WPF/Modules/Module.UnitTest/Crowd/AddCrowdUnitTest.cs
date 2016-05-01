using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.ViewModels;
using Module.HeroVirtualTabletop.Repositories;
using Moq;
using Module.HeroVirtualTabletop.Models;
using Module.HeroVirtualTabletop.DomainModels;
using System.Linq;

namespace Module.UnitTest.Crowd
{
    /// <summary>
    /// Summary description for AddCrowdUnitTest
    /// </summary>
    [TestClass]
    public class AddCrowdUnitTest : BaseCrowdUnitTest
    {
        private CharacterExplorerViewModel crowdMemberExplorerViewModel;
        public AddCrowdUnitTest()
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
        /// When the repository is empty, Adding a crowd should add it as a stand alone crowd and the All crowdMembers list should not be present at this time
        /// </summary>
        [TestMethod]
        public void AddCrowd_EmptyRepositoryTest()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = crowdMemberExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == "All Characters").FirstOrDefault();
            var cmodel2 = crowdList.Where(cr => cr.Name == "Crowd").FirstOrDefault();
            Assert.IsTrue(cmodel1 == null && cmodel2 != null);
            
        }
        /// <summary>
        /// Adding a crowd should obviously call the repository to update the crowd collection in the repository. This is an acceptance test. 
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryUnitTest
        /// to make sure that the repository can save to file what it is being passed. So, here we just need to make sure that repository is indeed being called with correct parameters.
        /// </summary>
        [TestMethod]
        public void AddCrowd_RepositoryBeingUpdatedTest()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());// Starting with an empty repository
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == "Crowd")));
        }
        /// <summary>
        /// The name of an added crowd should be unique, and should be "Crowd*" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCrowd_NewCrowdUniqueNameTest()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = crowdMemberExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == "Crowd");
            var cr2 = crowdList.Where(cr => cr.Name == "Crowd (1)").FirstOrDefault();
            Assert.IsTrue(cr1.Count() == 1 && cr2 != null);
        }
        /// <summary>
        /// If no crowd or crowdMember is selected, the new crowd should just be added as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_NoCurrentSelectedCrowdTest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.SelectedCrowdModel = null;
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = crowdMemberExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new crowd should be added under it and also as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CurrentSelectionIsCrowdTest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.SelectedCrowdModel = crowdMemberExplorerViewModel.CrowdCollection[1]; // Assuming "Crowd 1" is selected
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = crowdMemberExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added crowd should be in a total of two places - one stand alone and one nested position
            CrowdModel crowdAdded = crowdMemberExplorerViewModel.CrowdCollection.Where(cm=>cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = crowdMemberExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 1").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded); 
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new crowd should be added under all of them, as well as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CurrentSelectionIsCrowdWithMultiplePresenceTest()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.SelectedCrowdModel = crowdMemberExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Assuming "Child Crowd 1.1" is selected
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = crowdMemberExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 3); // The added crowd should be in a total of 3 places - one stand alone and two nested positions
            CrowdModel crowdAdded = crowdMemberExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = crowdMemberExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 1").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd1.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd2 = crowdMemberExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 2").FirstOrDefault();
            crowdAdded = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
        }
        /// <summary>
        /// If the current selected member is a CrowdMember under All Characters, the new crowd should be added as just another crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CurrentSelectionIsCrowdMemberUnderAllCrowdMembersTest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            crowdMemberExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            crowdMemberExplorerViewModel.SelectedCrowdModel = crowdMemberExplorerViewModel.CrowdCollection[0]; // Assuming "All Characters" is the selected crowd
            crowdMemberExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = crowdMemberExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added crowd should be added only as a stand alone crowd
            CrowdModel crowdAdded = crowdMemberExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = crowdMemberExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd 1").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            // And so on...
        }

        int numberOfItemsFound = 0;
        private void CountNumberOfCrowdMembersByName(List<ICrowdMember> collection, string name)
        {           
            foreach (ICrowdMember bcm in collection)
            {
                if (bcm.Name == name)
                    numberOfItemsFound++;
                if (bcm is CrowdModel)
                {
                    CrowdModel cm = bcm as CrowdModel;
                    if (cm.CrowdMemberCollection != null && cm.CrowdMemberCollection.Count > 0)
                    {
                        CountNumberOfCrowdMembersByName(cm.CrowdMemberCollection.ToList(), name);
                    }
                }
            }
        }
    }
}
