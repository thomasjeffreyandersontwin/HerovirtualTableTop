using NUnit.Framework;
using HVT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HVT.Tests
{
    [TestFixture()]
    public class CrowdCharacterMethodDelegationTest : CrowdTestHelper
    {
        Crowd TestCrowd;

        [OneTimeSetUp()]
        public void Begin()
        {
            TestCrowd = new Crowd("TestCrowd");

            Character Char;

            Char = new Character("Spyder", "Spyder", SkinType.Costume);
            this.TestCrowd.Add(Char);

            Char.Position = new Position() { X = 10, Y = 20, Z = 30 };
            Char.SavePosition();
            Char.Position = new Position();

            Char.Spawn();

            
            Char = new Character("Ogun", "Ogun", SkinType.Costume);
            this.TestCrowd.Add(Char);

            Char.Position = new Position() { X =40, Y = 50, Z = 60 };
            Char.SavePosition();
            Char.Position = new Position();

            Char.Spawn();

            Char = new Character("TestCharacter", "model_Statesman", SkinType.Model);
            this.TestCrowd.Add(Char);

            Char.Position = new Position() { X = 80, Y = 90, Z = 100 };
            Char.SavePosition();
            Char.Position = new Position();

            Char.Spawn();

        }

        [Test()]
        public void TestCallsCrowdMemberMethodOnAllMembers()
        {
            TestCrowd.Call("Place");

            Position actual = TestCrowd["Spyder"].Position;
            Position valid = new Position() { X = 10, Y = 20, Z = 30 };
            AssertPositions(actual, valid);

            actual = TestCrowd["Ogun"].Position;
            valid = new Position() { X = 40, Y = 50, Z = 60 };
            AssertPositions(actual, valid);

            actual = TestCrowd["TestCharacter"].Position;
            valid = new Position() { X = 80, Y = 90, Z = 100 };
            AssertPositions(actual, valid);
        }

        [OneTimeTearDown()]
        public void End()
        {
            CharactersRepository.Instance.Clear();
            CrowdsRepository.Instance.Clear();
        }
    }

    [TestFixture()]
    public class CrowdCharacterIntegrationTest : CrowdTestHelper
    {
        Crowd TestCrowd;

        [OneTimeSetUp()]
        public void Begin()
        {
            TestCrowd = new Crowd("TestCrowd");

            Character Char;

            Char = new Character("Spyder", "Spyder", SkinType.Costume);
            this.TestCrowd.Add(Char);
            Char.Position = new Position() { X = 10, Y = 20, Z = 30 };
            Char.SavePosition();
            Char.Position = new Position();

            Char = new Character("Ogun", "Ogun", SkinType.Costume);
            this.TestCrowd.Add(Char);

            Char.Position = new Position() { X = 40, Y = 50, Z = 60 };
            Char.SavePosition();
            Char.Position = new Position();

            Char = new Character("TestCharacter", "model_Statesman", SkinType.Model);
            this.TestCrowd.Add(Char);

            Char.Position = new Position() { X = 80, Y = 90, Z = 100 };
            Char.SavePosition();
            Char.Position = new Position();

        }

        [Test()]
        public void TestSavePositionForSpawnedCharacter()
        {
            Character Spyder = TestCrowd["Spyder"];
            Spyder.Spawn();
            Spyder.SavePosition();

            Position targetedPosition = new Position() { cohMemoryManager = new COHMemoryManager()};

            AssertPositions(Spyder.Position, targetedPosition);
        }

        [Test()]
        public void TestPlacesCharacterInSavedPosition()
        {
            Character Spyder = TestCrowd["Spyder"];
            Spyder.Spawn();
                        
            Position originalPosition = Spyder.Position.Clone();

            Spyder.Position.X -= 10;
            Spyder.SavePosition();

            Spyder.Position = originalPosition.Clone(Spyder.Position.cohMemoryManager);

            Spyder.Place();

            Position targetedPosition = new Position() { cohMemoryManager = new COHMemoryManager() };

            AssertPositions(Spyder.Position, targetedPosition);

        }

        [OneTimeTearDown()]
        public void End()
        {
            CharactersRepository.Instance.Clear();
            CrowdsRepository.Instance.Clear();
        }
    }

    [TestFixture()]
    public class CharacterPlacementTest : CrowdTestHelper
    {
        Crowd TestCrowd;
        Character TestCharacter;

        [OneTimeSetUp()]
        public void Begin()
        {
            TestCrowd = new Crowd("TestCrowd");

            Character Char;

            Char = new Character("Spyder", "Spyder", SkinType.Costume);
            this.TestCrowd.Add(Char);
            Char.Position = new Position() { X = 10, Y = 20, Z = 30 };
            Char.SavePosition();
            Char.Position = new Position();
            Char.Spawn();
            TestCharacter = Char;
        }

        [Test()]
        public void TestCharacterIsPlaced()
        {
            TestCharacter.Place();
            Position actual = TestCharacter.Position;
            Position valid = new Position() { X = 10, Y = 20, Z = 30 };
            AssertPositions(actual, valid);

        }

        [Test()]
        public void TestPositionShouldNotBeTheSameForCharacterAcrossTwoCrowds()
        {
            throw new NotImplementedException();
        
        }

        [OneTimeTearDown()]
        public void End()
        {
            CharactersRepository.Instance.Clear();
            CrowdsRepository.Instance.Clear();
        }
    }

    [TestFixture()]
    public class AddMemberTestTest : CrowdTestHelper
    {
        Crowd TestCrowd;
        Character TestCharacter;

        [OneTimeSetUp()]
        public void Begin()
        {
            TestCrowd = new Crowd("TestCrowd");
        }

        [Test()]
        public void TestPositionIsCaptured()
        {
            TestCharacter = new Character("TestCharacter 2", "model_Statesman", SkinType.Model);
            TestCharacter.Spawn();
            TestCharacter.Position = new Position() { cohMemoryManager = TestCharacter.Position.cohMemoryManager, X = 10, Y = 5, Z = 5 };
            TestCrowd.Add(TestCharacter);
            Character addedCharacter = TestCrowd[TestCharacter.Name];
            Position actual = addedCharacter.Position;
            Position valid = new Position() { X = 10, Y = 5, Z = 5 };

            AssertPositions(actual, valid);

            TestCharacter.Dispose();
        }

        [Test()]
        public void TestAddsCharacterToCrowdAndCrowdToCharacter()
        {
            TestCharacter = new Character("TestCharacter", "model_Statesman", SkinType.Model);
            TestCrowd.Add(TestCharacter);

            Character addedCharacter = TestCrowd["TestCharacter"];
            string vname = addedCharacter.Name;
            Assert.AreEqual(TestCharacter.Name, vname);
            Assert.AreEqual(TestCharacter.Name, addedCharacter.Name);

            Crowd crowdFromCharacter = addedCharacter.Crowds[0];
            Assert.AreEqual(addedCharacter.Crowds[0].Name, crowdFromCharacter.Name);

            TestCharacter.Dispose();
        }

        [Test()]
        public void TestDecoratesExistingCharacterObject()
        {
            Character baseCharacter = new Character("TestCharacter2", "model_Statesman", SkinType.Model);
            TestCrowd.Add(baseCharacter);
            Character decoratedCharacter = TestCrowd[baseCharacter.Name];
            string actual = decoratedCharacter.Crowds[0].Name;
            Assert.AreEqual(actual, "TestCrowd");
            baseCharacter.Dispose();
        }

        [Test()]
        public void TestRemoveMember()
        {
            TestCharacter = new Character("TestCharacter", "model_Statesman", SkinType.Model);
            TestCrowd.Add(TestCharacter);

            Character character = TestCrowd["TestCharacter"];

            TestCrowd.Remove(character);
            bool test = TestCrowd.Contains(character);

            Assert.AreEqual(test, false);

            TestCharacter.Dispose();
        }

        [OneTimeTearDown()]
        public void End()
        {
            CharactersRepository.Instance.Clear();
            CrowdsRepository.Instance.Clear();
        }
    }
}