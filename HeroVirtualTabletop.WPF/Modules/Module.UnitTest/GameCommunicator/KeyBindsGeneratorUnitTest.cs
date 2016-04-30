using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Module.HeroVirtualTabletop.DomainModels;
using Module.Shared.Models.GameCommunicator;
using System.IO;
using Module.HeroVirtualTabletop.Enumerations;

namespace Module.UnitTest.GameCommunicator
{
    [TestClass]
    public class KeyBindsGeneratorUnitTest
    {
        ManagedCharacter TestCharacter;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            TestCharacter = new Mock<ManagedCharacter>().Object;
            TestCharacter.Name = "Statesman";
            TestCharacter.ActiveIdentity = new Identity("model_Statesman", IdentityType.Model);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_CreatesProperKeybindStringTest()
        {
            //Act
            KeyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, TestCharacter.ActiveIdentity.Surface, TestCharacter.Name);
            string actual = KeyBindsGenerator.GeneratedKeybindText;

            //Assert
            string valid = "spawn_npc " + TestCharacter.ActiveIdentity.Surface + " " + TestCharacter.Name;
            Assert.AreEqual(valid, actual);

            //Clean up
            KeyBindsGenerator.CompleteEvent();
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_SurroundsWithQuotesTest()
        {
            //Act
            KeyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, TestCharacter.ActiveIdentity.Surface, TestCharacter.Name);
            string actual = KeyBindsGenerator.CompleteEvent();

            //Assert
            string valid = "\"spawn_npc" + " " + TestCharacter.ActiveIdentity.Surface + " " + TestCharacter.Name + "\"";
            Assert.AreEqual(valid, actual);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_MultipleKeybindsAppendTest()
        {
            //Act
            KeyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, TestCharacter.Name);
            KeyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
            string actual = KeyBindsGenerator.CompleteEvent();

            //Assert
            string valid = "\"target_name" + " " + TestCharacter.Name + "$$follow \"";
            Assert.AreEqual(valid, actual);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_PlacesKeybindInTempBindFileTest()
        {
            //Act
            KeyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, TestCharacter.ActiveIdentity.Surface, TestCharacter.Name);
            KeyBindsGenerator.CompleteEvent();
            StreamReader actualFile = new StreamReader(KeyBindsGenerator.bindFile);
            string actual = actualFile.ReadLine();
            actualFile.Close();

            //Assert
            string valid = "Y \"spawn_npc" + " " + TestCharacter.ActiveIdentity.Surface + " " + TestCharacter.Name + "\"";
            Assert.AreEqual(valid, actual);
        }
    }
}
