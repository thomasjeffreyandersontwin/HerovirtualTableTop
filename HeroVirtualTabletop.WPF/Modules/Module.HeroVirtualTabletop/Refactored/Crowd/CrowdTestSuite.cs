using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moq;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture;
using HeroVirtualTableTop.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeroVirtualTableTop.ManagedCharacter;
using System.Linq;
using Ploeh.AutoFixture.Kernel;
using Framework.WPF.Library;


namespace HeroVirtualTableTop.Crowd
{
    [TestClass]
    public class CrowdRepositoryTestSuite
    {
        public MockCrowdFactory Factory;

        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockCrowdFactory(new MockManagedCustomerFactory(new MockDesktopFactory()));

        }

        [TestMethod]
        public void NewCrowd_IsAddedToRootCrowdAndParent()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTestWithMockRepo;

            //act
            Crowd actual = repo.NewCrowd(parent);

            //assert
            bool isPresent = repo.Crowds.Contains(actual);
            Assert.IsTrue(isPresent);
            isPresent = parent.Members.Contains(actual);
            Assert.IsTrue(isPresent);

        }

        [TestMethod]
        public void NewCharacterCrowdMember_IsAddedToAllMembersAndParent()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTestWithMockRepo;

            //act
            CharacterCrowdMember actual = repo.NewCharacterCrowdMember(parent);

            //assert
            bool isPresent = repo.AllMembersCrowd.Members.Contains(actual);
            Assert.IsTrue(isPresent);

            isPresent = parent.Members.Contains(actual);
            Assert.IsTrue(isPresent);

        }

        [TestMethod]
        public void NewCharacterMember_CreatesAUniqueNameAcrossCrowds() //need to check
        {
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTestWithMockRepo;

            //act
            CharacterCrowdMember actual = repo.NewCharacterCrowdMember(parent);

            ((CrowdRepositoryImpl)repo).NewCharacterCrowdMemberInstance = Factory.MockCharacterCrowdMember;
            CharacterCrowdMember nextActual = repo.NewCharacterCrowdMember(parent);

            //assert

            Assert.AreEqual("Character (1)", nextActual.Name);

            //arrange
            ((CrowdRepositoryImpl)repo).NewCharacterCrowdMemberInstance = Factory.MockCharacterCrowdMember;

            //act
            nextActual = repo.NewCharacterCrowdMember(parent);

            //assert
            Assert.AreEqual("Character (2)", nextActual.Name);

            //arrange
            Crowd parent2 = Factory.CrowdUnderTestWithMockRepo;
            ((CrowdRepositoryImpl)repo).NewCharacterCrowdMemberInstance = Factory.MockCharacterCrowdMember;

            //act
            CharacterCrowdMember brotherFromAnotherMother = repo.NewCharacterCrowdMember(parent2);


            //assert
            Assert.AreEqual("Character (3)", brotherFromAnotherMother.Name);
            Assert.AreNotEqual(brotherFromAnotherMother.Parent, nextActual.Parent);
        }

        [TestMethod]
        public void NewCrowdMember_CreatesAUniqueNameAcrossCrowds() //need to check
        {
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTestWithMockRepo;

            //act
            Crowd actual = repo.NewCrowd(parent);

            ((CrowdRepositoryImpl)repo).NewCrowdInstance = Factory.MockCrowd;
            Crowd nextActual = repo.NewCrowd(parent);

            //assert

            Assert.AreEqual("Crowd (1)", nextActual.Name);

            //arrange
            ((CrowdRepositoryImpl)repo).NewCrowdInstance = Factory.MockCrowd;

            //act
            nextActual = repo.NewCrowd(parent);

            //assert
            Assert.AreEqual("Crowd (2)", nextActual.Name);

            //arrange
            Crowd parent2 = Factory.CrowdUnderTestWithMockRepo;
            ((CrowdRepositoryImpl)repo).NewCrowdInstance = Factory.MockCrowd;

            //act
            Crowd brotherFromAnotherMother = repo.NewCrowd(parent2);


            //assert
            Assert.AreEqual("Crowd (3)", brotherFromAnotherMother.Name);
        }
    }

    [TestClass]
    public class CrowdTestSuite
    {
        public MockCrowdFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockCrowdFactory(new MockManagedCustomerFactory(new MockDesktopFactory()));


        }

        [TestMethod]
        public void ChangingCrowdMemberChildInOneCrowd_ChangesTheSameMemberThatIaPartOfAnotherCrowd()
        {
            Crowd parent1 = Factory.CrowdUnderTestWithMockRepo;
            Crowd parent2 = Factory.CrowdUnderTestWithMockRepo;
            CharacterCrowdMember child = Factory.CharacterUnderTestWithMockDependencies;

            //act
            parent1.AddCrowdMember(child);
            parent2.AddCrowdMember(child);

            child.Name = "New Name";

            Assert.AreEqual(parent1.MembersByName["New Name"], parent2.MembersByName["New Name"]);


        }

        [TestMethod]
        public void ChangeCrowdName_FailsIfDuplicateNameExsistsAcrossAllCrowds() {
            //arrange
            CrowdRepository repo = Factory.MockRepositoryWIthCrowdsOnlyUnderTest;
            Crowd crowdToChange = repo.Crowds[1];

            DuplicateKeyException ex = null;

            //act
            try { crowdToChange.Name = "0.1.1"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNotNull(ex);
            ex = null;

            try { crowdToChange.Name = "0.0"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNotNull(ex);
            ex = null;

            try { crowdToChange.Name = "0.0.2"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNotNull(ex);
            ex = null;

            try { crowdToChange.Name = "new name"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNull(ex);

        }

        [TestMethod]
        public void ChangeCharacterName_FailsIfDuplicateNameExsistsAcrossAllCrowds()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTestWIthMixedCharactersAndCrowds;
            CharacterCrowdMember characterToChange = (CharacterCrowdMember)repo.Crowds[0].Members[1];

            DuplicateKeyException ex = null;

            //act
            try { characterToChange.Name = "0.0.1"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNotNull(ex);
            ex = null;

            try { characterToChange.Name = "0.0.2"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNotNull(ex);
            ex = null;

            try { characterToChange.Name = "new name"; } catch (DuplicateKeyException e) { ex = e; }

            // Assert
            Assert.IsNull(ex);

        }

        [TestMethod]
        public void MoveMemberToNewPosition_UpdatesOrderCorrectly()
        {

            List<Crowd> crowdList = Factory.MockRepositoryWIthCrowdsOnlyUnderTest.Crowds;

            Crowd parent = crowdList[0];
            parent.AddCrowdMember(Factory.CharacterUnderTestWithMockDependencies);

            CrowdMember movedDown = parent.Members[1];
            CrowdMember toMove = parent.Members[0];
            CrowdMember destination = parent.Members[1];

            int destinationOrder = destination.Order;
            int maxOrder = parent.Members[2].Order;
            int movedDownOrder = parent.Members[1].Order;

            parent.MoveCrowdMemberAfter(destination, toMove);

            Assert.AreEqual(toMove.Order, destinationOrder);
            Assert.AreEqual(parent.Members[2].Order, maxOrder);
            Assert.AreEqual(movedDown.Order, movedDownOrder - 1);

        }

        [TestMethod]
        public void Members_returnsProperListBasedOnCrowdMembershipDictionary() {
            Crowd parent = Factory.CrowdUnderTestWithMockRepo;
            Crowd child1 = Factory.CrowdUnderTestWithMockRepo;
            CharacterCrowdMember child2 = Factory.CharacterUnderTestWithMockDependencies;

            parent.AddCrowdMember(child1);
            parent.AddCrowdMember(child2);

            Assert.AreEqual(child1, parent.MembersByName[child1.Name]);
            Assert.AreEqual(child2, parent.MembersByName[child2.Name]);

            Assert.AreEqual(child1.Parent, parent);
            Assert.AreEqual(child2.Parent, parent);

        }

        [TestMethod]
        public void Parent_activatesParentMembershipBasedOnParentCalled()
        {
            Crowd parent1 = Factory.CrowdUnderTestWithMockRepo;
            Crowd parent2 = Factory.CrowdUnderTestWithMockRepo;
            CharacterCrowdMember child = Factory.CharacterUnderTestWithMockDependencies;

            parent1.AddCrowdMember(child);
            parent2.AddCrowdMember(child);

            CrowdMember crowdChild = parent1.Members[0];

            Assert.AreEqual(crowdChild.Parent, parent1);
            Assert.AreNotEqual(crowdChild.Parent, parent2);

            crowdChild = parent2.Members[0];

            Assert.AreEqual(crowdChild.Parent, parent2);
            Assert.AreNotEqual(crowdChild.Parent, parent1);


        }

        #region fure
        [TestMethod]
        public void RemoveMember_UpdatesOrderCorrectly() {
            CrowdRepository repo = Factory.RepositoryUnderTestWIthMixedCharactersAndCrowds;
           
            Crowd parent = repo.Crowds[0];
            parent.AddCrowdMember(Factory.CharacterUnderTestWithMockDependencies);


            
            CrowdMember first = parent.Members[0];
            int firstOrder = parent.Members[0].Order;

            CrowdMember second = parent.Members[1];
            int secondOrder = parent.Members[1].Order;

            CrowdMember third = parent.Members[2];
            int thirdOrder = parent.Members[2].Order;

            CrowdMember fourth = parent.Members[3];
            int fourthOrder = parent.Members[3].Order;

            CrowdMember toRemove = parent.Members[1];

            parent.RemoveMember(toRemove);

            Assert.AreEqual(first.Order, firstOrder);
            Assert.AreEqual(third.Order, secondOrder);
            Assert.AreEqual(fourth.Order, thirdOrder);

        }

       
        [TestMethod]
        public void RemoveCrowd_LastInstanceOfCrowdDeletesChildrenCharacterCrowdMembers()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTestWIthMixedCharactersAndCrowds;
            Crowd parent = Factory.CrowdUnderTestWithMockRepo;
            parent.Name = "Parent";
           
            Crowd grandParent1 = repo.Crowds[1];
            Crowd grandParent2 = repo.Crowds[2];

            grandParent1.AddCrowdMember(parent);
            grandParent2.AddCrowdMember(parent);

            CharacterCrowdMember grandChild1 = Factory.GetCharacterUnderTestWithMockDependenciesAnddOrphanedWithRepo(repo);
            grandChild1.Name = "gran 1";
            repo.AllMembersCrowd.AddCrowdMember(grandChild1);
            CharacterCrowdMember grandChild2 = Factory.GetCharacterUnderTestWithMockDependenciesAnddOrphanedWithRepo(repo);
            grandChild2.Name = "gran 2";
            repo.AllMembersCrowd.AddCrowdMember(grandChild2);

            parent.AddCrowdMember(grandChild1);
            parent.AddCrowdMember(grandChild2);
            int parentCount = parent.Members.Count;
            int AllMembersCountBeforeDeletes = repo.AllMembersCrowd.Members.Count;

            //act-assert
            grandParent1.RemoveMember(parent);
            Assert.AreEqual(parent.Members.Count, parentCount);

            //act-assert
            grandParent2.RemoveMember(parent);
            Assert.AreEqual(parent.Members.Count, 0);
            Assert.AreEqual(grandChild1.AllCrowdMembershipParents.Count, 0);
            Assert.AreEqual(grandChild2.AllCrowdMembershipParents.Count, 0);
            Assert.AreEqual(repo.AllMembersCrowd.Members.Count, AllMembersCountBeforeDeletes-2);
        }

        [TestMethod]
        public void AddMember_CreatesMembershipAndSetsParentAndOrder()
        {

        }

        [TestMethod]
        public void ManagedCharacterCommands_RunsONAllCharactersInCrowd()
        {

        }

        [TestMethod]
        public void SaveCurrentTableTopPosition_RunsSavePosOnAllCharactersInCrowd()
        {

        }
        public void PlaceOnTableTop__RunsSavePosOnAllCharactersInCrowd()
        {

        }
        public void Clone_CopiesCrowdAndChildrenAndNestedChildren()
        {

        }
        public void Clone_CreatesUniqueNamesForAllClonedChildrem() 
        {

        }
        public void ApplyFIlter_IncludesAllMatchedCrowdsAndCharacrersAsWellAsCharactersPartOfMatchedCrowds() // need to verify
        { }

        public void resetFilter_clearsFilterForCrowdAndAllNestedCrowdMembers()
        { }
#endregion

    }

    class CharacterCrowdMemberTestSuite
    {
        public MockCrowdFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockCrowdFactory(new MockManagedCustomerFactory(new MockDesktopFactory()));


        }

        public void DeleteCrowd_AllMembersCrowdRemovesItFromAllOtherParents()
        {

        }

        [TestMethod]
        public void ChangingCharacterMemberChildInOneCrowd_ChangesTheSameMeberThatIaPartOfAnotherCrowd()
        {

        }

        [TestMethod]
        public void ChangeName_FailsIfDuplicateNameExsistsAcrossAllCrowds() // need to check
        {

        }

        [TestMethod]
        public void MoveMemberToNewPosition_UpdatesOrderCorrectly()
        {

        }


        [TestMethod]
        public void LoadedParentMembership_isEqualToCrowdParentThatMemberWasObtainedFrom()
        { }

        public void SaveCurrentTableTopPosition_savesCurrentMemoryInstancePositionToCrowdMembershipOfCrowdParent()
        { }

        public void PlaceOnTableTop_SetsurrentMemoryInstancePositionFromSavedPositionOfCrowdMembershipBelongingToCrowdParent()
        { }

        public void Clone_CreatesUniqueNamesForClone() // need to verify
        {

        }
        public void ApplyFIlter_IncludesAllMatchedCharactersAsWellAsCrowdParents() // need to verify
        { }

        public void ApplyFIlter_IncludesAllMatchedCrowdsAsWellAsCharactersPartOfMatchedCrowds() // need to verify
        { }

        public void resetFilter_clearsFilterForMatchedCharacters() //redundant
        { }
    }
    class CrowdClipboardTestSuite
    {
        public MockCrowdFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockCrowdFactory(new MockManagedCustomerFactory(new MockDesktopFactory()));


        }

        [TestMethod]
        public void CutAndPaste_ToSameCrowdDoesNothing()
        {
        }

        [TestMethod]
        public void CutAndPaste_ToDifferentCrowdRemovesMembershipFromSourceCrowdANdAddsNewMembershipToDestination() //validate name
        {
        }

        [TestMethod]
        public void CutAndPaste_ToDifferentCrowdIsRetriveableInCorrectOrder()
        {
        }

        [TestMethod]
        public void CopyndPaste_ToSameCrowdCreatesACloneAndNewMembershipAndUniqueName()
        {
        }

        [TestMethod]
        public void CopyAndPasteCrowd_CLonesAllNestedCrowdChildren()
        {
        }

        [TestMethod]
        public void LinkPasteCrowd_ClonesAllNestedCrowdChildren()
        {
        }
    }

   public class MockCrowdFactory {
        public MockManagedCustomerFactory MockManagedCustomerFactory;
        public IFixture MockFixture;
        public IFixture CustomizedMockFixture;
        public IFixture StandardizedFixture;

        public MockCrowdFactory(MockManagedCustomerFactory characterFactory)
        {
            MockManagedCustomerFactory = characterFactory;
            MockFixture = MockManagedCustomerFactory.MockFixture;
            CustomizedMockFixture = MockManagedCustomerFactory.CustomizedMockFixture;

            StandardizedFixture = MockManagedCustomerFactory.StandardizedFixture;

            StandardizedFixture.Behaviors.Add(new OmitOnRecursionBehavior());

            //map all interfaces to classes
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(Crowd),
                        typeof(CrowdImpl)));
            StandardizedFixture.Customizations.Add(
               new TypeRelay(
                   typeof(CrowdMember),
                       typeof(CharacterCrowdMemberImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(CrowdMemberShip),
                        typeof(CrowdMemberShipImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(CharacterCrowdMember),
                        typeof(CharacterCrowdMemberImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(Crowd),
                        typeof(CrowdImpl)));
            StandardizedFixture.Customizations.Add(
               new TypeRelay(
                   typeof(CrowdRepository),
                       typeof(CrowdRepositoryImpl)));

        }

        public Crowd MockCrowd {
            get
            {
                Crowd crowd = CustomizedMockFixture.Create<Crowd>();
                return crowd;
            }
        }
        public Crowd CrowdUnderTestWithMockRepo
        {
            get
            {
                StandardizedFixture.Inject<CrowdRepository>(MockRepositoryWIthCrowdsOnlyUnderTest);
                return StandardizedFixture.Create<CrowdImpl>();
            }
        }
        public Crowd CrowdUnderTest
        {
            get
            {
                return StandardizedFixture.Create<CrowdImpl>();
            }
        }


        public CrowdRepository RepositoryUnderTest
        {
            get
            {
 
                CrowdRepositoryImpl repo = StandardizedFixture.Build<CrowdRepositoryImpl>()
                    .With(x=>x.Crowds,null)
                    .With(x => x.NewCrowdInstance, MockCrowd)
                    .With(x => x.NewCharacterCrowdMemberInstance, MockCharacterCrowdMember)
                    .Create();
                StandardizedFixture.Inject<CrowdRepository>(repo);

                var crowdList = StandardizedFixture.CreateMany<Crowd>();
                repo.Crowds = crowdList.ToList();

                Crowd allMembers = CrowdUnderTestWithMockRepo;
                allMembers.Name = CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME;
                repo.Crowds.Add(allMembers);

                return repo;
            }
        }
        public CrowdRepository MockRepositoryWIthCrowdsOnlyUnderTest
        {
            get
            {
                Mock<CrowdRepository> mocker = new Mock<CrowdRepository>();
                mocker.SetupAllProperties();
                CrowdRepository mock = mocker.Object;
                StandardizedFixture.Inject<CrowdRepository>(mock);

                
                mock.Crowds = NestedCrowdTreeUnderTest;
                return mock;
            }
        }

        public CrowdRepository RepositoryUnderTestWIthMixedCharactersAndCrowds
        {
            get
            {
                CrowdRepositoryImpl repo = StandardizedFixture.Build<CrowdRepositoryImpl>()
                     .With(x => x.Crowds, new List<Crowd>())
                     .With(x => x.NewCrowdInstance, MockCrowd)
                     .With(x => x.NewCharacterCrowdMemberInstance, MockCharacterCrowdMember)
                     .Create();
                StandardizedFixture.Inject<CrowdRepository>(repo);

                repo.Crowds = StandardizedFixture.CreateMany<Crowd>().ToList();
                string counter = "0";
                int count = 0;

                foreach (Crowd c in repo.Crowds)
                {
                    c.Name = counter + "." + count.ToString();
                    c.Order = count;
                    count++;
                    
                }
                Crowd allMembers = CrowdUnderTest;
                allMembers.Name = CROWD_CONSTANTS.ALL_CHARACTER_CROWD_NAME;
                repo.Crowds.Add(allMembers);

                counter = "0.0";
                count = 0;
                foreach (CharacterCrowdMember grandchild in StandardizedFixture.CreateMany<CharacterCrowdMember>().ToList())
                {
                    ((Crowd) repo.Crowds[0]).AddCrowdMember(grandchild);

                    grandchild.Name = counter + "." + count.ToString();
                    count++;
                    grandchild.Order = count;
                    repo.AllMembersCrowd.AddCrowdMember(grandchild);

                }

                count = 0;
                counter = "0.1";
                foreach (CharacterCrowdMember grandchild in StandardizedFixture.CreateMany<CharacterCrowdMember>().ToList())
                {
                    ((Crowd)repo.Crowds[1]).AddCrowdMember(grandchild);

                    grandchild.Name = counter + "." + count.ToString();
                    count++;
                    grandchild.Order = count;
                    repo.AllMembersCrowd.AddCrowdMember(grandchild);
                }              
                return repo;

            }


        }
  
        public List<Crowd> NestedCrowdTreeUnderTest
        {
            get
            {
                var crowdList = StandardizedFixture.CreateMany<Crowd>().ToList();
                string counter = "0";
                int count = 0;
                foreach (Crowd c in crowdList)
                {


                    c.Name = counter + "." + count.ToString();
                    c.Order = count;
                    count++;

                }

                counter = "0.0";
                count = 0;
                foreach (Crowd grandchild in StandardizedFixture.CreateMany<Crowd>().ToList())
                {
                    crowdList[0].AddCrowdMember(grandchild);

                    grandchild.Name = counter + "." + count.ToString();
                    count++;
                    grandchild.Order = count;

                }
                count = 0;
                counter = "0.1";
                foreach (Crowd grandchild in StandardizedFixture.CreateMany<Crowd>().ToList())
                {
                    crowdList[1].AddCrowdMember(grandchild);

                    grandchild.Name = counter + "." + count.ToString();
                    count++;
                    grandchild.Order = count;
                }
                return crowdList;
            }
        }

        public CharacterCrowdMember CharacterUnderTestWithMockDependencies
        {
            get
            {
                //create the character
                CharacterCrowdMember characterUnderTest = new CharacterCrowdMemberImpl(MockCrowd, MockManagedCustomerFactory.MockDesktopFactory.MockDesktopCharacterTargeter,
                    MockManagedCustomerFactory.MockDesktopFactory.MockKeybindGenerator,
                    MockManagedCustomerFactory.MockCamera, MockManagedCustomerFactory.MockIdentities, MockRepositoryWIthCrowdsOnlyUnderTest);
                characterUnderTest.Name = "MOQ MAN 2";
                return characterUnderTest;
            }
        }

        public CharacterCrowdMember GetCharacterUnderTestWithMockDependenciesAnddOrphanedWithRepo(CrowdRepository repo)
        {
            CharacterCrowdMember characterUnderTest = new CharacterCrowdMemberImpl(null, MockManagedCustomerFactory.MockDesktopFactory.MockDesktopCharacterTargeter,
               MockManagedCustomerFactory.MockDesktopFactory.MockKeybindGenerator,
               MockManagedCustomerFactory.MockCamera, MockManagedCustomerFactory.MockIdentities, repo);
            characterUnderTest.Name = "MOQ MAN 2";
            return characterUnderTest;
        }
        public CharacterCrowdMember MockCharacterCrowdMember
        {
            get
            {
                CharacterCrowdMember crowd = CustomizedMockFixture.Create<CharacterCrowdMember>();
                return crowd;
            }
        }
       
        public CrowdMemberShip MockCrowdMembership
        {
            get
            {
                return MockFixture.Create<CrowdMemberShip>();
            }
        }
        public CrowdMemberShip MemberShipWithCharacterUnderTest
        {
            get
            {
                return new CrowdMemberShipImpl(CrowdUnderTestWithMockRepo, CharacterUnderTestWithMockDependencies);
            }
        }

    }



}
