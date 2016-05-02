using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;
using Module.Shared.Enumerations;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.UnitTest.GameCommunicator
{
    [TestClass]
    public class KeyBindsGeneratorTest
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
        public void GenerateKeyBindsForEvent_CreatesProperKeybindString()
        {
            //Act
            // SHOULD TEST ALL EVENTS BY ITERATING THROUGH THE EVENT DICTIONARY
            // WE CAN KEEP A DICTIONARY IN THIS CLASS WITH EVENTS AND VALID KEYBIND STRINGS. WE CAN GENERATE KEYBINDS FOR EACH EVENT IN THE DICTIONARY AND ASSERT IF THE
            // GENERATED STRING MATCHES THE VALID STRING IN THE DICTIONARY
            string actual = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);

            //Assert
            string valid = "spawn_npc " + testCrowdMember.ActiveIdentity.Surface + " " + testCrowdMember.Name; // TRY TO USE STRING.FORMAT INSTEAD OF CONCATS WHEN MORE THAN 2 STRINGS ARE THERE
            Assert.AreEqual(valid, actual);

            //Clean up
            keyBindsGenerator.CompleteEvent();
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_SurroundsWithQuotes()
        {
            //Act
            string actual = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.SpawnNpc, testCrowdMember.ActiveIdentity.Surface, testCrowdMember.Name);

            //Assert
            string valid = "\"spawn_npc" + " " + testCrowdMember.ActiveIdentity.Surface + " " + testCrowdMember.Name + "\"";
            Assert.AreEqual(valid, actual); // DID NOT UNDERSTAND, AS THE FIRST TEST METHOD DOES NOT ASSERT WITH QUOTES
        }

        [TestMethod]
        public void GenerateKeyBindsForEvent_AppendsMultipleKeybinds()
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
        public void GenerateKeyBindsForEvent_PlacesKeybindInTempBindFile()
        {
            // THIS CANNOT POSSIBLY BE TESTED WITH MOCK AS IT CONTAINS ACTUAL FILE I/O. WE SHOULD USE AN ACTUAL INSTANCE OF KeyBindsGenerator
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
