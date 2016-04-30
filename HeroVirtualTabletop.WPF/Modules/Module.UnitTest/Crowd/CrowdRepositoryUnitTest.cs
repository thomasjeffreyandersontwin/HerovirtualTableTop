using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Repositories;
using Module.Shared;
using System.IO;
using Module.HeroVirtualTabletop.Models;
using System.Linq;
using Module.HeroVirtualTabletop.DomainModels;
using System.Threading;

namespace Module.UnitTest
{
    /// <summary>
    /// Summary description for CrowdRepositoryUnitTest
    /// </summary>
    [TestClass]
    public class CrowdRepositoryUnitTest : BaseCrowdUnitTest
    {
        public CrowdRepositoryUnitTest()
        {
            //
            // TODO: Add constructor logic here
            //
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

        [TestMethod]
        public void CrowdRepository_NoRepositoryFileTest()
        {
            //CrowdRepository crowdRepository = new CrowdRepository();
            Assert.IsTrue(!(string.IsNullOrWhiteSpace(Constants.GAME_DATA_FOLDERNAME) || string.IsNullOrWhiteSpace(Constants.GAME_CROWD_REPOSITORY_FILENAME)));
        }

        [TestMethod]
        public void GetCrowdCollection_NoExistingRepositoryFileTest()
        {
            string testRepoFileName = "test.data";
            string fullFilePath = Path.Combine(testRepoFileName);
            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);
            // Here we are directly testing the repository and need to access the internal fields. So, not mocking.
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

        [TestMethod]
        public void SaveCrowdCollection_SaveConsistencyTest()
        {
            CrowdModel crowd = new CrowdModel { Name = "Test Crowd 1", OriginalName = "Test Crowd Original" };
            CrowdModel childCrowd = new CrowdModel { Name = "Child Crowd 1", OriginalName = "Child Crowd Original" };
            Character character1 = new Character { Name = "Test Character 1", OriginalName = "Test Character Original" };
            Character character2 = new Character { Name = "Test Character 1.1", OriginalName = "Test Character Original" };
            crowd.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>() { character1, childCrowd};
            childCrowd.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>() { character2};
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
                    crowd.ChildCrowdCollection.Add(new Character() { Name = "New Character 1" });

                    List<CrowdModel> retrievedCrowdList = null;
                    crowdRepository.GetCrowdCollection((List<CrowdModel> crowdList) =>
                    {
                        retrievedCrowdList = crowdList;
                        try
                        {
                            CrowdModel cr1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                            Assert.IsNull(cr1);
                            Character ch1 = retrievedCrowdList[0].ChildCrowdCollection.Where(c => c.Name == "New Character 1").FirstOrDefault() as Character;
                            Assert.IsNull(ch1);
                            CrowdModel cr2 = retrievedCrowdList[0].ChildCrowdCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                            Assert.IsNotNull(cr2);
                            Character ch2 = cr2.ChildCrowdCollection.Where(c => c.Name == "Test Character 1.1").FirstOrDefault() as Character;
                            Assert.IsNotNull(ch2);
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
                                    CrowdModel cr1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                                    Assert.IsNotNull(cr1);
                                    Character ch1 = retrievedCrowdList[0].ChildCrowdCollection.Where(c => c.Name == "New Character 1").FirstOrDefault() as Character;
                                    Assert.IsNotNull(ch1);
                                    CrowdModel cr2 = retrievedCrowdList[0].ChildCrowdCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                                    Assert.IsNotNull(cr2);
                                    Character ch2 = cr2.ChildCrowdCollection.Where(c => c.Name == "Test Character 1.1").FirstOrDefault() as Character;
                                    Assert.IsNotNull(ch2);
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
}
