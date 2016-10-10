using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HVT.Tests
{
    public class SpawnTestHelper
    {
        public Character SpawnTestCharacter()
        {
            KeyBindsGenerator.GenerateKeyBindsForEvent(CohEvent.SpawnNpc, "model_Statesman", "TestCharacter [TestCrowd]");
            KeyBindsGenerator.CompleteEvent();
            Character testCharacter = new Character("TestCharacter", "model_Statesman", SkinType.Model);
            new Crowd("TestCrowd").Add(testCharacter);
            return testCharacter;
        }
    }

    public class KeyBindsGeneratorTestHelper
    {
        public void NeuterTheKeyBindsGenerator(Character character)
        {
            KeyBindsGenerator.TriggerKey = string.Empty;
            KeyBindsGenerator.LoaderKey = string.Empty;
            KeyBindsGenerator.GeneratedKeybindText = string.Empty;
        }

        public void ActivateTheKeyBindsGenerator(Character character)
        {
            KeyBindsGenerator.TriggerKey = "Y";
            KeyBindsGenerator.LoaderKey = "B";
        }
    }

    public class CharacterTestHelper
    {
        public void AssertCharacter(Character actual, Character valid)
        {
            if (actual == null || valid == null)
            {
                Assert.Fail();
                return;
            }
            Assert.AreEqual(actual.Name, valid.Name);
            Assert.AreEqual(actual.Identity.Surface, valid.Identity.Surface);
            Assert.AreEqual(actual.Identity.Type, valid.Identity.Type);
        }

        public Character NewValidCharacter
        {
            get
            {
                return new Character("TestCharacter", "model_Statesman", SkinType.Model);
            }
        }

        public Character NewUpdatedCharacter
        {
            get
            {
                return new Character("UpdatedTestCharacter", "updated_model_Statesman", SkinType.UpdatedModel);
            }
        }

        public Character NewUnsavedCharacter
        {
            get
            {
                return new Character("NewTestCharacter", "new_model_Statesman", SkinType.NewModel);
            }
        }

        public Character NewSecondUnsavedCharacter
        {
            get
            {
                return new Character("NewTestCharacter 2", "new_model_Statesman", SkinType.NewModel);
            }
        }

        public Character NewValidTargetedCharacter
        {
            get
            {
                return new Character("TargetedCharacter");
            }
        }

        public Character NewSecondValidTargetedCharacter
        {
            get
            {
                return new Character("TargetedCharacter 2");
            }
        }

        public CharactersRepository NewTestCharacterRepository
        {
            get
            {
                CharactersRepository repo = CharactersRepository.Instance;

                repo.Call("Dispose");

                Character testCharacter1 = new Character("TestCharacter", "model_Statesman", SkinType.Model);
                repo["TestCharacter"] = testCharacter1;

                Character testCharacter2 = new Character("TestCharacter 2", "model_Statesman 2", SkinType.Model);
                repo["TestCharacter 2"] = testCharacter2;

                return repo;
            }
        }
    }

    public class CrowdTestHelper : CharacterTestHelper
    {
        public void AssertPositions(Position actual, Position valid)
        {
            Assert.AreEqual(actual.X, valid.X);
            Assert.AreEqual(actual.Y, valid.Y);
            Assert.AreEqual(actual.Z, valid.Z);
        }

        public Crowd NewUnsavedCrowd
        {
            get
            {
                Crowd newCrowd = new Crowd("NewTestCrowd");
                newCrowd.Add(NewUnsavedCharacter);
                return newCrowd;
            }
        }

        public Crowd NewValidTargetedCrowd
        {
            get
            {
                Crowd newCrowd = new Crowd("TargetedCrowd");
                newCrowd.Add(NewValidTargetedCharacter);
                return newCrowd;
            }
        }

        public Crowd NewValidTargetedCrowdWithTwoCharacters
        {
            get
            {
                Crowd newCrowd = new Crowd("TargetedCrowd");
                newCrowd.Add(NewValidTargetedCharacter);
                newCrowd.Add(NewSecondValidTargetedCharacter);
                return newCrowd;
            }
        }

        public void AssertCrowd(Crowd actual, Crowd valid)
        {
            Assert.AreEqual(actual.Name, valid.Name);
            foreach (Character validIter in valid)
            {
                Character actualMember = actual[validIter.Name];
                Assert.AreEqual(actualMember.Name, validIter.Name);
                AssertCharacter(actualMember, validIter);
                if (actualMember.Position != null && validIter.Position != null)
                {
                    Position actualPos = actualMember.Position;
                    Position validPos = validIter.Position;
                    AssertPositions(actualPos, validPos);
                }
            }
        }

        public Crowd NewValidCrowd
        {
            get
            {
                Crowd validCrowd = new Crowd("TestCrowd");
                validCrowd.Add(new Character("TestCharacter", "model_Statesman", SkinType.Model));
                validCrowd.Add(new Character("TestCharacter 2", "model_Statesman 2", SkinType.Model));
                return validCrowd;
            }
        }
    }
}
