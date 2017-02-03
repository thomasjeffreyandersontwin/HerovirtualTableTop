using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HeroVirtualTableTop.Crowd;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using HeroVirtualTableTop.AnimatedAbility;
using HeroVirtualTableTop.Attack;
namespace HeroVirtualTableTop.Roster
{
    [TestClass]
    public class RosterTestSuite
    {
        public RosterTestObjectsFactory TestObjectsFactory = new RosterTestObjectsFactory();
        [TestMethod]
        public void AddCrowdToRoster_CrowdMembersAvailableAsParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTest;
            Crowd.Crowd c = TestObjectsFactory.CrowdUnderTestWithThreeMockCharacters;
            r.CreateGroupFromCrowd(c);
            int counter = 0;
            foreach (RosterParticipant p in r.Participants)
            {
                Assert.AreEqual(p, c.Members[counter]);
                counter++;
            }

            Assert.AreEqual(r.Participants.Count, c.Members.Count);

        }
        [TestMethod]
        public void AddNestedCrowdToRoster_AddsAllCrowdsInGraphWithCharacterMembersToTheRoster()
        {
            Roster r = TestObjectsFactory.RosterUnderTest;
            Crowd.Crowd gran = TestObjectsFactory.NestedCrowdCharacterGraph;
            r.CreateGroupFromCrowd(gran);
            int counter = 0;

            foreach (CrowdMember parent in gran.Members)
            {
                Crowd.Crowd parentcrowd = parent as Crowd.Crowd;
                foreach (CrowdMember child in parentcrowd?.Members)
                {
                    Assert.AreEqual(r.Participants[counter], child);
                    counter++;
                }
            }
        }
        [TestMethod]
        public void AddCharacterToRoster_AddsTheCharacterAndCrowdParentOfCharacterToRoster()
        {
            Roster r = TestObjectsFactory.RosterUnderTest;
            CharacterCrowdMember p =
                TestObjectsFactory.CrowdUnderTestWithThreeMockCharacters.Members[0] as CharacterCrowdMember;
            r.AddCrowdMemberAsParticipant(p);

            Assert.AreEqual((p as CharacterCrowdMember)?.Parent.Name, r.Groups.Values.FirstOrDefault()?.Name);
            Assert.AreEqual(p, r.Participants.FirstOrDefault());

        }
        [TestMethod]
        public void ActivatingCharacter_RosterActiveCharacterWillReturnsTheActivatedCharacter()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;

            c.IsActive = true;

            Assert.AreEqual(c, r.ActiveCharacter);
        }
        [TestMethod]
        public void DeactivatingCharacter_RemovesExsistingAtttackingCharacterFromRoster()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;

            c.IsActive = true;
            c.IsActive = false;

            Assert.IsNull( r.ActiveCharacter);
        }
        [TestMethod]
        public void CharacterStartsAttack_RosterAttackingCharacterWillReturnTheAttackingCharacter()
        {
            Roster r = TestObjectsFactory.RosterUnderTest;
            AnimatedAttack a = TestObjectsFactory.AttackUnderTestWithCharacterUnderTest;
            a.Attacker = (AnimatedCharacter) TestObjectsFactory.CharacterCrowdMemberUnderTest;

            r.AddCrowdMemberAsParticipant((CharacterCrowdMember) a.Attacker);

            a.StartAttackCycle();
            Assert.AreEqual(a.Target, r.AttackingCharacter);
        }
        [TestMethod]
        public void ActivatingAttack_PutsPreviousAttackingCharacterOutOfAttackMode()
        {
            //arrange
            Roster r = TestObjectsFactory.RosterUnderTest;
            AnimatedAttack first = TestObjectsFactory.AttackUnderTestWithCharacterUnderTest;
            first.Attacker = (AnimatedCharacter) TestObjectsFactory.CharacterCrowdMemberUnderTest;

            r.AddCrowdMemberAsParticipant((CharacterCrowdMember) first.Attacker);
            first.StartAttackCycle();

            //arrange
            AnimatedAttack second = TestObjectsFactory.AttackUnderTestWithCharacterUnderTest;
            second.Attacker = (AnimatedCharacter) TestObjectsFactory.CharacterCrowdMemberUnderTest;

            //act
            r.AddCrowdMemberAsParticipant((CharacterCrowdMember) second.Attacker);
            second.StartAttackCycle();

            //Assert
            Assert.IsNull(first.Attacker.ActiveAttack);


        }
        [TestMethod]
        public void SelectParticipants_AddsParticipantToRosterSelectedParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterParticipant selected = r.Participants[0];

            r.SelectParticipant(selected);
            Assert.AreEqual(r.SelectedParticipants[0], selected);

            selected = r.Participants[1];
            r.SelectParticipant(selected);
            Assert.AreEqual(r.SelectedParticipants[1], selected);


        }
        [TestMethod]
        public void UnsSelectParticipant_RemovesYheParticipantfromSelectedParticipantsInRoster()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterParticipant selected = r.Participants[0];

            r.SelectParticipant(r.Participants[0]);
            r.SelectParticipant(r.Participants[1]);

            r.UnsSelectParticipant(r.Participants[1]);
            Assert.IsFalse(r.SelectedParticipants.Contains(r.Participants[1]));
        }
        [TestMethod]
        public void SelectGroup_AddsAllParticipantsInGroupToRosterSelectedParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterGroup selected = r.Groups[1];

            r.SelectGroup(selected);
            int counter = 0;
            foreach (var p in selected.Values)
            {
                Assert.AreEqual(p, r.SelectedParticipants[counter]);
                counter++;
            }
        }
        [TestMethod]
        public void UnsSelectGroup_RemovesAllParticipantsInGroupFromRosterSelectedParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterGroup selected = r.Groups[1];

            r.SelectGroup(selected);
            r.UnSelectGroup(selected);
            int counter = 0;
            foreach (var p in selected.Values)
            {
                Assert.IsFalse(r.SelectedParticipants.Contains(p));
                counter++;
            }
        }
        [TestMethod]
        public void ClearParticipants_RemovesAllParticipantsInAllGroupsFromRosterSelectedParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterGroup selected = r.Groups[1];

            r.SelectGroup(selected);
            r.ClearAllSelections();
            int counter = 0;
            foreach (var p in selected.Values)
            {
                Assert.IsFalse(r.SelectedParticipants.Contains(p));
                counter++;
            }
        }
        [TestMethod]
        public void SelectAll_AddsAllParticipantsAcrossAllGroupsInRosterToRosterSelectedParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            r.SelectAllParticipants();
            int counter = 0;
            foreach (var p in r.Participants)
            {
                Assert.AreEqual(p, r.SelectedParticipants[counter]);
                counter++;
            }
        }
        [TestMethod]
        public void SaveRoster_RosterIsSavedAsNestedCrowdMadeUpOfClonedCrowdMembershipsWithSameCharactersInSameOrderAsParticipantsInRosterGroups()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            Crowd.Crowd crowd = r.SaveAsCrowd();
            int crowdCounter = 0;
            foreach (var g in r.Groups.Values)
            {
                Crowd.Crowd c = crowd.Members[crowdCounter] as Crowd.Crowd;
                Assert.AreEqual(g.Name, c.Name);
                crowdCounter++;
                int charCounter = 0;
                foreach (var p in g.Values)
                {                 
                    Assert.AreEqual(p, c.Members[charCounter]);
                    charCounter++;
                }
            }

        }
        [TestMethod]
        public void TargetingCharacter_RosterReturnsLastTargetedCharacter()
        {
            //arrange
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];
            (activeParticipant as ManagedCharacter.ManagedCharacter).Targeter.TargetedInstance =
                (activeParticipant as ManagedCharacter.ManagedCharacter).MemoryInstance;
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;

            //act
            c.Target();

            //assert
            Assert.AreEqual(c, r.TargetedCharacter);
        }
        [TestMethod]
        public void UnTargetingCharacter_TargetedCharacterIsRemovedFromRoster()
        {
            //arange
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];        
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;
            c.Target();

            //act
            c.UnTarget();

            //assert
            Assert.IsNull(r.TargetedCharacter);
        }
        [TestMethod]
        public void SelectCharacter_UpdatesSelectedParticipantsInRoster()
        {
            //arrange
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;

            //act
            c.IsSelected= true;

            //assert
            Assert.IsTrue(r.SelectedParticipants.Contains(c));
        }
        [TestMethod]
        public void UnselectCharacter_CharacterIsRemovedFromSelectedParticipants()
        {
            //arrange
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;

            //act
            c.IsSelected = false;

            //assert
            Assert.IsFalse(r.SelectedParticipants.Contains(c));
        }
    }

    public class RosterTestObjectsFactory : CrowdTestObjectsFactory
    {
        public Roster RosterUnderTest => StandardizedFixture.Build<RosterImpl>()
            .Without(x => x.ActiveCharacter)
             .Without(x => x.TargetedCharacter)
            .Without(x => x.AttackingCharacter)
            .Without(x => x.LastSelectedCharacter)
            .Create();

        public Crowd.Crowd NestedCrowdCharacterGraph
        {
            get
            {
                Crowd.Crowd c = ThreeCrowdsWithThreeCrowdChildrenInTheFirstTwoCrowdsAllLabeledByOrder[0];

                (c?.Members?[0] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);
                (c?.Members?[0] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);
                (c?.Members?[0] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);

                (c?.Members?[1] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);
                (c?.Members?[1] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);
                (c?.Members?[1] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);

                (c?.Members?[2] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);
                (c?.Members?[2] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);
                (c?.Members?[2] as Crowd.Crowd)?.AddCrowdMember(MockCharacterCrowdMember);

                return c;
            }
        }
        public Roster RosterUnderTestWithThreeMockParticipants
        {
            get
            {
                Roster rosterUnderTest = RosterUnderTest;
                rosterUnderTest.AddCrowdMemberAsParticipant(MockCharacterCrowdMember);
                rosterUnderTest.AddCrowdMemberAsParticipant(MockCharacterCrowdMember);
                rosterUnderTest.AddCrowdMemberAsParticipant(MockCharacterCrowdMember);
                return rosterUnderTest;

            }
        }
        public Roster RosterUnderTestWithThreeParticipantsUnderTest
        {
            get
            {
                Roster rosterUnderTest = RosterUnderTest;
                rosterUnderTest.AddCrowdMemberAsParticipant(CharacterCrowdMemberUnderTest);
                rosterUnderTest.AddCrowdMemberAsParticipant(CharacterCrowdMemberUnderTest);
                rosterUnderTest.AddCrowdMemberAsParticipant(CharacterCrowdMemberUnderTest);
                return rosterUnderTest;
            }
        }
    }

}
