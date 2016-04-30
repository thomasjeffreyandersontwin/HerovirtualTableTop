using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Module.HeroVirtualTabletop.DomainModels;
using Module.Shared.Models.GameCommunicator;
using System.IO;
using Module.HeroVirtualTabletop.Enumerations;
using Module.Shared.Enumerations;

namespace Module.UnitTest.GameCommunicator
{
    [TestClass]
    public class KeyBindsGeneratorUnitTest
    {
        CrowdMember testCrowdMember;
        KeyBindsGenerator keyBindsGenerator;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            testCrowdMember = new Mock<CrowdMember>().Object;
            keyBindsGenerator = new Mock<KeyBindsGenerator>().Object;
            testCrowdMember.Name = "Statesman";
            testCrowdMember.ActiveIdentity = new Identity("model_Statesman", IdentityType.Model);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_CreatesProperKeybindStringTest()
        {
            //Act

            string actual = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);

            //Assert
            string valid = "spawn_npc " + testCrowdMember.ActiveIdentity.Surface + " " + testCrowdMember.Name;
            Assert.AreEqual(valid, actual);

            //Clean up
            keyBindsGenerator.CompleteEvent();
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_SurroundsWithQuotesTest()
        {
            //Act
            string actual = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);

            //Assert
            string valid = "\"spawn_npc" + " " + testCrowdMember.ActiveIdentity.Surface + " " + testCrowdMember.Name + "\"";
            Assert.AreEqual(valid, actual);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_MultipleKeybindsAppendTest()
        {
            //Act
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetName, testCrowdMember.Name);
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow);
            string actual = keyBindsGenerator.CompleteEvent();

            //Assert
            string valid = "\"target_name" + " " + testCrowdMember.Name + "$$follow \"";
            Assert.AreEqual(valid, actual);
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_PlacesKeybindInTempBindFileTest()
        {
            // This cannot be tested with mock, should rather be tested with actual keybindgenerator instance
            //Act
            //keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);
            //keyBindsGenerator.CompleteEvent();
            //StreamReader actualFile = new StreamReader(keyBindsGenerator.bindFile);
            //string actual = actualFile.ReadLine();
            //actualFile.Close();

            ////Assert
            //string valid = "Y \"spawn_npc" + " " + testCrowdMember.ActiveIdentity.Surface + " " + testCrowdMember.Name + "\"";
            //Assert.AreEqual(valid, actual);
        }
    }
}
