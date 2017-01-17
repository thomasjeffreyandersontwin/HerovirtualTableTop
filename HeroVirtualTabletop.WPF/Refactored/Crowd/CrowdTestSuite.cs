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
using System;

namespace HeroVirtualTableTop.Crowd
{
    [TestClass]
    public class CrowdRepositoryTestSuite
    {
        public FakeCrowdFactory Factory;

        public CrowdRepositoryTestSuite()
        {
            Factory = new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory()));

        }

        [TestMethod]
        public void NewCrowd_IsAddedToParent()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTest;

            //act
            Crowd actual = repo.NewCrowd(parent);

            //assert
            bool isPresent = parent.Members.Contains(actual);
            Assert.IsTrue(isPresent);

        }
        [TestMethod]
        public void NewCharacterCrowdMember_IsAddedToAllMembersAndParent()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTest;

            //act
            CharacterCrowdMember actual = repo.NewCharacterCrowdMember(parent);

            //assert
            bool isPresent = repo.AllMembersCrowd.Members.Contains(actual);
            Assert.IsTrue(isPresent);

            isPresent = parent.Members.Contains(actual);
            Assert.IsTrue(isPresent);

        }
        [TestMethod]
        public void NewCharacterMember_CreatesAUniqueNameAcrossCrowds() 
        {
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTest;

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
            Crowd parent2 = Factory.CrowdUnderTest;
            ((CrowdRepositoryImpl)repo).NewCharacterCrowdMemberInstance = Factory.MockCharacterCrowdMember;

            //act
            CharacterCrowdMember brotherFromAnotherMother = repo.NewCharacterCrowdMember(parent2);


            //assert
            Assert.AreEqual("Character (3)", brotherFromAnotherMother.Name);
            Assert.AreNotEqual(brotherFromAnotherMother.Parent, nextActual.Parent);
        }
        [TestMethod]
        public void NewCrowdMember_CreatesAUniqueNameWithinSameCrowdOnly() 
        {
            CrowdRepository repo = Factory.RepositoryUnderTest;
            Crowd parent = Factory.CrowdUnderTest;

            //act
            Crowd actual = repo.NewCrowd(parent);

            //((CrowdRepositoryImpl)repo).NewCrowdInstance = Factory.MockCrowd;
            Crowd nextActual = repo.NewCrowd(parent);

            //assert

            Assert.AreEqual("Crowd (1)", nextActual.Name);

            //arrange
            ((CrowdRepositoryImpl)repo).NewCrowdInstance = Factory.MockCrowd;

            //act
            nextActual = repo.NewCrowd(parent);

            //assert
            Assert.AreEqual("Crowd (2)", nextActual.Name);
        }
    }

    [TestClass]
    public class CrowdTestSuite
    {
        public FakeCrowdFactory Factory;

        public CrowdTestSuite()
        {
            Factory = new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory()));


        }

        [TestMethod]
        public void ChangingCrowdMemberChildInOneCrowd_ChangesTheSameMemberThatIaPartOfAnotherCrowd()
        {
            Crowd parent1 = Factory.CrowdUnderTest;
            Crowd parent2 = Factory.CrowdUnderTest;
            CharacterCrowdMember child = Factory.CharacterCrowdMemberUnderTest;

            //act
            parent1.AddCrowdMember(child);
            parent2.AddCrowdMember(child);

            child.Name = "New Name";

            Assert.AreEqual(parent1.MembersByName["New Name"], parent2.MembersByName["New Name"]);


        }
        [TestMethod]
        public void ChangeCrowdName_FailsIfDuplicateNameExsistsWithinParent() {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTestWithCrowdsOnly;
            Crowd crowdToChange = (Crowd)repo.Crowds[0].Members[0];

            DuplicateKeyException ex = null;

            //act
            try { crowdToChange.Name = "0.0.1"; } catch (DuplicateKeyException e) { ex = e; }

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
        public void ChangeCharacterName_FailsIfDuplicateNameExistsInEntireRepository()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTestWithLabeledCrowdChildrenAndcharacterGrandChildren;
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
        public void MoveMemberToNewPositionWithinParent_UpdatesOrderCorrectlyOfAllChildren()
        {

            List<Crowd> crowdList = Factory.MockRepositoryWithCrowdsOnlyUnderTest.Crowds;

            Crowd parent = crowdList[0];
            parent.AddCrowdMember(Factory.CharacterCrowdMemberUnderTest);

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
        public void MoveMemberFromOneParentToNewParent_RemovesFromOriginalAndPlacesInNewCrowdAndUpdatesOrderForMembersInOldAndNewparent()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTestWithLabeledCrowdChildrenAndcharacterGrandChildren;
            Crowd parent0, parent1;
            CharacterCrowdMember child0_0, child0_1, child0_2, child0_3;
            CharacterCrowdMember child1_0, child1_1, child1_2, child1_3;
            AddCrowdwMemberHierarchyWithTwoParentsANdFourChildrenEach(repo, out parent0, out parent1, out child0_0, out child0_1, out child0_2, out child0_3, out child1_0, out child1_1, out child1_2, out child1_3);

            int parent0Count = parent0.Members.Count;
            int parent1Count = parent1.Members.Count;

            int child0_0Order = child0_0.Order;
            int child0_1Order = child0_1.Order;
            int child0_2Order = child0_2.Order;
            int child0_3Order = child0_3.Order;

            int child1_0Order = child1_0.Order;
            int child1_1Order = child1_1.Order;
            int child1_2Order = child1_2.Order;
            int child1_3Order = child1_3.Order;

            //act
            CrowdMember memberToMove = child0_1;
            CrowdMember destination = child1_1;
            parent1.MoveCrowdMemberAfter(destination, memberToMove);

            //assert - did orders update on children
            Assert.AreEqual(child0_0Order, child0_0.Order); //unchanged
            Assert.AreEqual(child0_2Order - 1, child0_2.Order); //element moved down
            Assert.AreEqual(child0_3Order - 1, child0_3.Order); //element moved down

            Assert.AreEqual(child1_0Order, child1_0.Order); //unchanged
            Assert.AreEqual(child1_1Order, child1_1.Order); //unchanged
            Assert.AreEqual(child1_2Order + 1, child1_2.Order); //element moved up
            Assert.AreEqual(child1_3Order + 1, child1_3.Order); //element moved up

            //assert did parent members update
            Assert.AreEqual(destination.Order + 1, memberToMove.Order); //element now destination +1
            Assert.AreEqual(parent1.Members[2], memberToMove); //movedMember is a meber of new parent
            Assert.IsFalse(parent0.Members.Contains(memberToMove)); //movedMember is not a  member of the old parent
            Assert.AreEqual(memberToMove.Parent, parent1); //movedmember is connected to the parent

            //did child lose old membership and get new one
            int oldParentCount = memberToMove.AllCrowdMembershipParents.Where(x => x.ParentCrowd.Name == parent0.Name).ToList().Count;
            Assert.IsFalse(oldParentCount > 0);
            int newParentCount = memberToMove.AllCrowdMembershipParents.Where(x => x.ParentCrowd.Name == parent1.Name).ToList().Count;
            Assert.IsTrue(newParentCount == 1);

        }
        [TestMethod]
        public void MembersByName_returnsDictionaryOfMembersBasedOnUnderlyingMembershipList() {
            Crowd parent = Factory.CrowdUnderTest;
            Crowd child1 = Factory.CrowdUnderTest;
            CharacterCrowdMember child2 = Factory.CharacterCrowdMemberUnderTest;

            parent.AddCrowdMember(child1);
            parent.AddCrowdMember(child2);

            Assert.AreEqual(child1, parent.MembersByName[child1.Name]);
            Assert.AreEqual(child2, parent.MembersByName[child2.Name]);

            Assert.AreEqual(child1.Parent, parent);
            Assert.AreEqual(child2.Parent, parent);

        }
        [TestMethod]
        public void Parent_ChildReturnsParentBasedOnWhatParentChildWasRetrievedFromLast()
        {
            Crowd parent1 = Factory.CrowdUnderTest;
            Crowd parent2 = Factory.CrowdUnderTest;
            CharacterCrowdMember child = Factory.CharacterCrowdMemberUnderTest;

            parent1.AddCrowdMember(child);
            parent2.AddCrowdMember(child);

            CrowdMember crowdChild = parent1.Members[0];

            Assert.AreEqual(crowdChild.Parent, parent1);
            Assert.AreNotEqual(crowdChild.Parent, parent2);

            crowdChild = parent2.Members[0];

            Assert.AreEqual(crowdChild.Parent, parent2);
            Assert.AreNotEqual(crowdChild.Parent, parent1);


        }      
        [TestMethod]
        public void RemoveMember_UpdatesOrderCorrectly() {
            CrowdRepository repo = Factory.RepositoryUnderTestWithLabeledCrowdChildrenAndcharacterGrandChildren;
           
            Crowd parent = repo.Crowds[0];
            parent.AddCrowdMember(Factory.CharacterCrowdMemberUnderTest);
            
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
        public void RemoveCrowd_RemovingChildFromLastParentCrowdDeletesChildAndAnyNestedChildrenThatHaveNoOtherParents()
        {
            //arrange
            CrowdRepository repo = Factory.RepositoryUnderTestWithLabeledCrowdChildrenAndcharacterGrandChildren;
            Crowd parent, grandParent1, grandParent2;
            CharacterCrowdMember grandChild1, grandChild2;
            AddCrowdwMemberHierarchyWithParentSharedAcrossTwoGranParentsAndTwoGrandChildrenToRepo(repo, out parent, out grandParent1, out grandParent2, out grandChild1, out grandChild2);

            int parentCount = parent.Members.Count;
            int AllMembersCountBeforeDeletes = repo.AllMembersCrowd.Members.Count;

            //act-assert
            grandParent1.RemoveMember(parent);
            Assert.AreEqual(parent.Members.Count, parentCount);

            //act-assert
            grandParent2.RemoveMember(parent);
            Assert.AreEqual(parent.AllCrowdMembershipParents.Count, 0);
            Assert.AreEqual(grandChild1.AllCrowdMembershipParents.Count, 0);
            Assert.AreEqual(grandChild2.AllCrowdMembershipParents.Count, 0);
           
        }
        private void AddCrowdwMemberHierarchyWithParentSharedAcrossTwoGranParentsAndTwoGrandChildrenToRepo(CrowdRepository repo, out Crowd parent, out Crowd grandParent1, out Crowd grandParent2, out CharacterCrowdMember grandChild1, out CharacterCrowdMember grandChild2)
        {
            parent = Factory.CrowdUnderTest;
            parent.Name = "Parent";

            grandParent1 = repo.Crowds[1];
            grandParent2 = repo.Crowds[2];
            grandParent1.AddCrowdMember(parent);
            grandParent2.AddCrowdMember(parent);

            grandChild1 = Factory.CharacterCrowdMemberUnderTestWithNoParent;
            grandChild1.Name = "gran 1";
           // repo.AllMembersCrowd.AddCrowdMember(grandChild1);
            grandChild2 = Factory.CharacterCrowdMemberUnderTestWithNoParent;
            grandChild2.Name = "gran 2";
           // repo.AllMembersCrowd.AddCrowdMember(grandChild2);

            parent.AddCrowdMember(grandChild1);
            parent.AddCrowdMember(grandChild2);
        }
        private void AddCrowdwMemberHierarchyWithTwoParentsANdFourChildrenEach(CrowdRepository repo, out Crowd parent0, out Crowd parent1,  out CharacterCrowdMember child0_0, out CharacterCrowdMember child0_1, out CharacterCrowdMember child0_2, out CharacterCrowdMember child0_3, out CharacterCrowdMember child1_0, out CharacterCrowdMember child1_1, out CharacterCrowdMember child1_2, out CharacterCrowdMember child1_3)
        {
            parent0 = Factory.CrowdUnderTest;
            parent0.Name = "Parent0";
            parent1 = Factory.CrowdUnderTest;
            parent1.Name = "Parent1";
            repo.Crowds.Add(parent0);
            repo.Crowds.Add(parent1);

            AddChildCrowdMemberToParent(repo, parent0, out child0_0, "child0_0");
            AddChildCrowdMemberToParent(repo, parent0, out child0_1, "child0_1");
            AddChildCrowdMemberToParent(repo, parent0, out child0_2, "child0_2");
            AddChildCrowdMemberToParent(repo, parent0, out child0_3, "child0_3");

            AddChildCrowdMemberToParent(repo, parent1, out child1_0, "child1_0");
            AddChildCrowdMemberToParent(repo, parent1, out child1_1, "child1_1");
            AddChildCrowdMemberToParent(repo, parent1, out child1_2, "child1_2");
            AddChildCrowdMemberToParent(repo, parent1, out child1_3, "child1_3");
        }
        private void AddChildCrowdMemberToParent(CrowdRepository repo, Crowd parent, out CharacterCrowdMember child, string name)
        {
            child = Factory.GetCharacterUnderTestWithMockDependenciesAnddOrphanedWithRepo(repo);
            child.Name = name;
            parent.AddCrowdMember(child);
            repo.AllMembersCrowd.AddCrowdMember(child);
        }

        [TestMethod]
        public void AddMember_CreatesMembershipAndUpdatesParentAndOrderOfMember()
        {
            Crowd crowd = Factory.CrowdUnderTest;
            CharacterCrowdMember character = Factory.CharacterCrowdMemberUnderTest;
            crowd.AddCrowdMember(character);

            character = Factory.CharacterCrowdMemberUnderTest;
            crowd.AddCrowdMember(character);

            character = Factory.CharacterCrowdMemberUnderTest;
            crowd.AddCrowdMember(character);

            Assert.AreEqual(crowd.Members.Count, 3);
            Assert.AreEqual(crowd.Members[0].Order, 1);
            Assert.AreEqual(crowd.Members[1].Order, 2);
            Assert.AreEqual(crowd.Members[2].Order, 3);

            Assert.AreEqual(crowd.Members[0].Parent, crowd);
            Assert.AreEqual(crowd.Members[1].Parent, crowd);
            Assert.AreEqual(crowd.Members[2].Parent, crowd);


            CrowdMemberShip membershipAttachedToParent = crowd.MemberShips.Where(x => x.Child.Name == crowd.Members[0].Name).FirstOrDefault();
            CrowdMemberShip membershipAttachedToChild = crowd.Members[0].AllCrowdMembershipParents.Where(x => x.ParentCrowd.Name == crowd.Name).FirstOrDefault();
            Assert.AreEqual(membershipAttachedToParent.ParentCrowd.Name, membershipAttachedToChild.ParentCrowd.Name);
            Assert.AreEqual(membershipAttachedToParent.Child.Name, membershipAttachedToChild.Child.Name);

             membershipAttachedToParent = crowd.MemberShips.Where(x => x.Child.Name == crowd.Members[1].Name).FirstOrDefault();
             membershipAttachedToChild = crowd.Members[1].AllCrowdMembershipParents.Where(x => x.ParentCrowd.Name == crowd.Name).FirstOrDefault();
            Assert.AreEqual(membershipAttachedToParent.ParentCrowd.Name, membershipAttachedToChild.ParentCrowd.Name);
            Assert.AreEqual(membershipAttachedToParent.Child.Name, membershipAttachedToChild.Child.Name);

             membershipAttachedToParent = crowd.MemberShips.Where(x => x.Child.Name == crowd.Members[1].Name).FirstOrDefault();
             membershipAttachedToChild = crowd.Members[1].AllCrowdMembershipParents.Where(x => x.ParentCrowd.Name == crowd.Name).FirstOrDefault();
            Assert.AreEqual(membershipAttachedToParent.ParentCrowd.Name, membershipAttachedToChild.ParentCrowd.Name);
            Assert.AreEqual(membershipAttachedToParent.Child.Name, membershipAttachedToChild.Child.Name);


        }
        [TestMethod]
        public void ExecutingAnyManagedCharacterCommandOncrowd_RunsMethodOnAllCharactersInCrowd()
        {
            Crowd crowd = Factory.CrowdUnderTestWithThreeMockCrowdmembers;
            crowd.SpawnToDesktop();
            foreach (CrowdMember member in crowd.Members)
            {
                Mock.Get<CrowdMember>(member).Verify(x => x.SpawnToDesktop(true));
            }
            
        }
        [TestMethod]
        public void ExecutingSaveCurrentTableTopPositionOnCrowd_RunsSavePosOnAllCharactersInCrowd()
        {
            Crowd crowd = Factory.CrowdUnderTestWithThreeMockCharacters;
            crowd.SaveCurrentTableTopPosition();
            foreach (CharacterCrowdMember member in crowd.Members)
            {
                Mock.Get<CrowdMember>(member).Verify(x => x.SaveCurrentTableTopPosition());
            }
        }
        [TestMethod]
        public void ExecutingPlaceOnTableTopOnCrowd__RunsPlaceOnTableTopOnAllchsractersInCrowd()
        {
            Crowd crowd = Factory.CrowdUnderTestWithThreeMockCharacters;
            crowd.PlaceOnTableTop();
            foreach (CharacterCrowdMember member in crowd.Members)
            {
                Mock.Get<CrowdMember>(member).Verify(x => x.PlaceOnTableTop(null));
            }
        }
        [TestMethod]
        public void CloneNestedCrowd_CopiesCrowdAndChildrenAndNestedChildrenAndCreatesUniqueNamesForAllClonedChildren()
        {
            List<Crowd> nested = Factory.RepositoryUnderTestWithNestedgraphOfCharactersAndCrowds.Crowds;
            Crowd original = nested[0];
            Crowd clone = (Crowd) original.Clone();

            string expected = original.Name + " (1)";
            Assert.AreEqual(expected, clone.Name);
            Assert.AreEqual(clone.Order, original.Order);
            Assert.AreEqual(clone.Members.Count, original.Members.Count);

            CrowdMember cloneMember = null;
            foreach (CrowdMember originalmember in original.Members)
            {
                cloneMember = clone.Members[originalmember.Order - 1];
                Assert.AreEqual(cloneMember.Name, originalmember.Name + " (1)");
            }

            original = (Crowd)((Crowd)original.Members[0]);
            cloneMember = null;
            clone = (Crowd)clone.Members[0];
            foreach (CrowdMember originalmember in original.Members)
            {
                cloneMember = clone.Members[originalmember.Order - 1];
                Assert.AreEqual(originalmember.Name + " (1)", cloneMember.Name);

            }

        }
       
        public void ApplyFIlter_IncludesAllMatchedCrowdsAndCharactersAsWellAsCharactersPartOfMatchedCrowds() // need to verify
        { }

        public void resetFilter_clearsFilterForCrowdAndAllNestedCrowdMembers()
        { }

    }

    class CharacterCrowdMemberTestSuite
    {
        public FakeCrowdFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory()));


        }

        public void DeleteCharacter_AllMembersCrowdRemovesItFromAllOtherParents()
        {

        }

        public void SaveCurrentTableTopPosition_savesCurrentMemoryInstancePositionToCrowdMembershipOfCrowdParent()
        { }

        public void PlaceOnTableTop_SetsurrentMemoryInstancePositionFromSavedPositionOfCrowdMembershipBelongingToCrowdParent()
        { }
    }
    class CrowdClipboardTestSuite
    {
        public FakeCrowdFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory()));


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

   public class FakeCrowdFactory {
        public FakeManagedCustomerFactory MockManagedCustomerFactory;
        public IFixture MockFixture;
        public IFixture CustomizedMockFixture;
        public IFixture StandardizedFixture;

        public FakeCrowdFactory(FakeManagedCustomerFactory characterFactory)
        {
            MockManagedCustomerFactory = characterFactory;

            MockFixture = MockManagedCustomerFactory.MockFixture;
            CustomizedMockFixture = MockManagedCustomerFactory.CustomizedMockFixture;
            setupStandardFixture();
        }
        private void setupStandardFixture()
        {
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

            setupFixtureToBuildCrowdRepositories();
        }
        private void setupFixtureToBuildCrowdRepositories()
        {
            //setup with the child dependencies that are circular removed
            StandardizedFixture.Customize<CrowdRepositoryImpl>(c => c
                .Without(x => x.NewCrowdInstance)
                .Without(x => x.NewCharacterCrowdMemberInstance)
                .Without(x => x.Crowds)
                .With(x=>x.UsingDependencyInjection, false)
            );
            

            List<Crowd> crowds = StandardizedFixture.CreateMany<Crowd>().ToList();
            //now setup again with clildren dependencies as they are referring to a parent with child dependencies removed so it is safe
            StandardizedFixture.Customize<CrowdRepositoryImpl>(c => c
                .With(x => x.NewCrowdInstance, StandardizedFixture.Create<Crowd>())
                .With(x => x.NewCharacterCrowdMemberInstance, StandardizedFixture.Create<CharacterCrowdMember>())
                .Do(x=>x.Crowds.AddRange(crowds))
                );
            
            //now add the circular relationship
            CrowdRepositoryImpl repo = StandardizedFixture.Create<CrowdRepositoryImpl>();
            repo.NewCharacterCrowdMemberInstance.CrowdRepository = repo;
            repo.NewCharacterCrowdMemberInstance.CrowdRepository = repo;

            StandardizedFixture.Inject<CrowdRepository>(repo);
        }

        public CrowdRepository RepositoryUnderTest
        {
            get
            {
                return StandardizedFixture.Create<CrowdRepository>();

            }
        }
        public CrowdRepository MockRepositoryWithCrowdsOnlyUnderTest
        {
            get
            {
                var mock = CustomizedMockFixture.Build<CrowdRepositoryImpl>().With(
                        x => x.Crowds,
                            ThreeCrowdsWithThreeCrowdChildrenInTheFirstTwoCrowdsAllLabeledByOrder
                        ).Create();
                 StandardizedFixture.Inject<CrowdRepository>(mock);
                return mock;
            }
        }
        public CrowdRepository RepositoryUnderTestWithCrowdsOnly
        {
            get
            {
                CrowdRepository repo = StandardizedFixture.Create<CrowdRepository>();
                repo.Crowds.AddRange (ThreeCrowdsWithThreeCrowdChildrenInTheFirstTwoCrowdsAllLabeledByOrder);
                return repo;
            }
        }
        public CrowdRepository RepositoryUnderTestWithLabeledCrowdChildrenAndcharacterGrandChildren
        {
            get
            {
                CrowdRepository repo = StandardizedFixture.Create<CrowdRepository>();
                addChildCrowdsLabeledByOrder(repo);
                addCharacterChildrenLabeledByOrderToChildCrowd(repo, "0.0", (Crowd)repo.Crowds[0]);
                addCharacterChildrenLabeledByOrderToChildCrowd(repo, "0.1", (Crowd)repo.Crowds[1]);
                return repo;
            }
        }
        private void addChildCrowdsLabeledByOrder(CrowdRepository repo)
        {
            repo.Crowds.AddRange(StandardizedFixture.CreateMany<Crowd>().ToList());
            string counter = "0";
            int count = 0;
            foreach (Crowd c in repo.Crowds)
            {
                c.Name = counter + "." + count.ToString();
                c.Order = count;
                count++;
                c.CrowdRepository = repo;
            }
        }
        private void addCharacterChildrenLabeledByOrderToChildCrowd(CrowdRepository repo, string nestedName, Crowd parent)
        {
            int count = 0;
            foreach (CharacterCrowdMember grandchild in StandardizedFixture.CreateMany<CharacterCrowdMember>().ToList())
            {
                parent.AddCrowdMember(grandchild);

                grandchild.Name = nestedName + "." + count.ToString();
                count++;
                grandchild.Order = count;
                repo.AllMembersCrowd.AddCrowdMember(grandchild);
            }
        }

        public CrowdRepository RepositoryUnderTestWithNestedgraphOfCharactersAndCrowds
        {
            get
            {
                CrowdRepository repo = StandardizedFixture.Create<CrowdRepository>();
                addChildCrowdsLabeledByOrder(repo);
                addCrowdChildrenLabeledByOrderToChildCrowd(repo, "0.0",(Crowd)repo.Crowds[0]);
                addCharacterChildrenLabeledByOrderToChildCrowd(repo, "0.0.0", (Crowd)repo.Crowds[0].Members[0]);
                return repo;
            }
        }
        private void addCrowdChildrenLabeledByOrderToChildCrowd(CrowdRepository repo, string nestedChildName, Crowd parent)
        {
            int count = 0;
            foreach (Crowd child in StandardizedFixture.CreateMany<Crowd>().ToList())
            {
                parent.AddCrowdMember(child);

                child.Name = nestedChildName + "." + count.ToString();
                count++;
                child.Order = count;
                repo.Crowds.Add(child);
            }
        }

        public CharacterCrowdMember CharacterCrowdMemberUnderTest
        {
            get
            {
                return StandardizedFixture.Create<CharacterCrowdMember>();
            }
        }

        public CharacterCrowdMember CharacterCrowdMemberUnderTestWithNoParent
        {
            get
            {
                CharacterCrowdMember chara=  StandardizedFixture.Create<CharacterCrowdMember>();
                chara.AllCrowdMembershipParents.Clear();
                chara.Parent = null;
                return chara;
            }
        }
        public CharacterCrowdMember GetCharacterUnderTestWithMockDependenciesAnddOrphanedWithRepo(CrowdRepository repo)
        {
            CharacterCrowdMember characterUnderTest = StandardizedFixture.Create<CharacterCrowdMember>();

            return characterUnderTest;
        }
        public CharacterCrowdMember MockCharacterCrowdMember
        {
            get
            {
                return CustomizedMockFixture.Create<CharacterCrowdMember>();
            }
        }

        public Crowd MockCrowd
        {
            get
            {
                return CustomizedMockFixture.Create<Crowd>();

            }
        }
        public Crowd CrowdUnderTest
        {
            get
            {
                return StandardizedFixture.Create<CrowdImpl>();
            }
        }
        public List<Crowd> ThreeCrowdsWithThreeCrowdChildrenInTheFirstTwoCrowdsAllLabeledByOrder
        {
            get
            {
                CrowdRepository repo = StandardizedFixture.Create<CrowdRepository>();
                //StandardizedFixture.Inject<CrowdRepository>(repo);
                addChildCrowdsLabeledByOrder(repo);
                addCrowdChildrenLabeledByOrderToChildCrowd(repo, "0.0", (Crowd)repo.Crowds[0]);
                addCrowdChildrenLabeledByOrderToChildCrowd(repo, "0.1", (Crowd)repo.Crowds[1]);
                return repo.Crowds;
            }
        }
        public Crowd CrowdUnderTestWithThreeMockCrowdmembers
        {
            get
            {
                Crowd crowd = CrowdUnderTest;
                foreach (CrowdMember member in MockFixture.CreateMany<CrowdMember>())
                {
                    crowd.AddCrowdMember(member);
                 }
                return crowd;
            }
        }
        public Crowd CrowdUnderTestWithThreeMockCharacters
        {
            get
            {
                Crowd crowd = CrowdUnderTest;
                foreach (CharacterCrowdMember member in MockFixture.CreateMany<CharacterCrowdMember>())
                {
                    crowd.AddCrowdMember(member);
                }
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
                return new CrowdMemberShipImpl(CrowdUnderTest, CharacterCrowdMemberUnderTest);
            }
        }

    }



}
