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
        public void AddCrowdToRoster_MembersAvailableAsParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTest;
            Crowd.Crowd c = TestObjectsFactory.CrowdUnderTestWithThreeMockCharacters;
            r.CreateGroupFromCrowd(c);
            int counter=0;
            foreach (RosterParticipant p in r.Participants)
            {
                Assert.AreEqual(p,c.Members[counter]);
                counter++;
            }

            Assert.AreEqual(r.Participants.Count, c.Members.Count);

        }

        [TestMethod]
        public void AddNestedCrowdToRoster_AddsAllCAllrowdsInGraphWithCharacterMembersToRoster()
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
        public void AddCharacterToRoster_AddsCharacterAndCrowdParentToRoster()
        {
            Roster r = TestObjectsFactory.RosterUnderTest;
            CharacterCrowdMember p = TestObjectsFactory.CrowdUnderTestWithThreeMockCharacters.Members[0] as CharacterCrowdMember;
            r.AddCrowdMemberAsParticipant(p);

            Assert.AreEqual((p as CharacterCrowdMember)?.Parent.Name, r.Groups.Values.FirstOrDefault()?.Name);
            Assert.AreEqual(p , r.Participants.FirstOrDefault());

        }

        [TestMethod]
        public void GetActiveCharacter_ReturnsTheLastActivatedCharacter()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant activeParticipant = r.Participants[2];
            CharacterCrowdMemberImpl c = activeParticipant as CharacterCrowdMemberImpl;
            
            c.IsActive = true;

            Assert.AreEqual(c,r.ActiveCharacter);
        }
        [TestMethod]
        public void ActivatingCharacter_DeactivatesExsistingAtttackingCharacterInRoster()
        {
        }
        [TestMethod]
        public void GetAtttackingCharacter_ReturnsTheLastAtttackingCharacterInTheRoster()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeParticipantsUnderTest;
            RosterParticipant acttackingParticipant = r.Participants[2];
            CharacterCrowdMemberImpl c = acttackingParticipant as CharacterCrowdMemberImpl;
            c.Abilities.InsertElement(TestObjectsFactory.MockAttack);
            Attack a = (AnimatedAttack) c.Abilities[1] ;


            Assert.AreEqual(c, r.AttackingCharacter);
        }
        [TestMethod]
        public void ActivatingAttack_PutsExistingattackerOutOfAttackMode()
        {
        }

        [TestMethod]
        public void SelectParticipants_AddsToParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterParticipant selected = r.Participants[0];

            r.SelectParticipant(selected);
            Assert.AreEqual(r.SelectedParticipants[0],selected);

            selected = r.Participants[1];
            r.SelectParticipant(selected);
            Assert.AreEqual(r.SelectedParticipants[1], selected);


        }
        [TestMethod]
        public void UnsSelectParticipant_rermovesfromParticipants()
        {
            Roster r = TestObjectsFactory.RosterUnderTestWithThreeMockParticipants;
            RosterParticipant selected = r.Participants[0];

            r.SelectParticipant(r.Participants[0]);
            r.SelectParticipant(r.Participants[1]);

            r.UnsSelectParticipant(r.Participants[1]);
            Assert.IsFalse(r.SelectedParticipants.Contains(r.Participants[1]));
        }
        [TestMethod]
        public void SelectGroup_AddsAllParticipantsInGroupToSelected()
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
        public void UnsSelectGroup_rermovesAllParticipantsInGroupFromSelected()
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
        public void ClearParticipants_rermovesAllParticipantsInAllGroupFromSelected()
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
        public void SelectAll_AddsAllParticipantsacrossGroupToSelected()
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

        public void SaveRoster_SavesAsNestedCrowdMadeUpOfClonedCrowdMemberships() { }

    }

    public class RosterTestObjectsFactory : CrowdTestObjectsFactory
    {
        public Roster RosterUnderTest => StandardizedFixture.Build<RosterImpl>()
            .Without(x=>x.ActiveCharacter)
            .Without(x => x.AttackingCharacter)
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

        public Roster RosterUnderTestWithThreeMockParticipants {
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
