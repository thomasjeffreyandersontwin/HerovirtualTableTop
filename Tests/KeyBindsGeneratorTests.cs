using NUnit.Framework;
using HVT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HVT.Tests
{
    public struct sCharacter
    {
        public string Name;
        public string Model;
        public string Costume;

        public sCharacter(string name, string model, string costume)
        {
            Name = name;
            Model = model;
            Costume = costume;
        }
    }

    public class TestGeneratorHelper
    {
        public sCharacter TestCharacter;

        public TestGeneratorHelper()
        {
            this.TestCharacter = GenerateTestCharacter();
        }

        public sCharacter GenerateTestCharacter()
        {
            return new sCharacter(name: "Statesman", model: "model_Statesman", costume: "Spyder");
        }

        public void GenerateKeyBindsForEventTestSpawnKeybind()
        {
            KeyBindsGenerator.GenerateKeyBindsForEvent(CohEvent.SpawnNpc, this.TestCharacter.Model, this.TestCharacter.Name);
        }

        public void GenerateKeyBindsForEventTargetFollowKeybind()
        {
            KeyBindsGenerator.GenerateKeyBindsForEvent(CohEvent.TargetName, this.TestCharacter.Name);
            KeyBindsGenerator.GenerateKeyBindsForEvent(CohEvent.Follow);
            //KeyBindsGenerator.CompleteEvent();
        }

        public void GenerateKeyBindsForEventLoadCostumeAndBindFileKeybind()
        {
            KeyBindsGenerator.GenerateKeyBindsForEvent(CohEvent.LoadCostume, this.TestCharacter.Costume);
            KeyBindsGenerator.GenerateKeyBindsForEvent(CohEvent.BindLoadFile, "c:\\test\\testfile.txt");
            //KeyBindsGenerator.CompleteEvent();
        }
    }

    [TestFixture()]
    public class KeyBindsGeneratorTests
    {
        public TestGeneratorHelper Helper;

        [OneTimeSetUp()]
        public void Begin()
        {
            this.Helper = new TestGeneratorHelper();
        }

        [Test()]
        public void TestGenerateKeyBindsForEvent_CreatesProperKeybindString()
        {
            this.Helper.GenerateKeyBindsForEventTestSpawnKeybind();
            string valid = "spawn_npc " + this.Helper.TestCharacter.Model + " " + this.Helper.TestCharacter.Name;
            string actual = KeyBindsGenerator.GeneratedKeybindText;
            KeyBindsGenerator.CompleteEvent();
            Assert.AreEqual(valid, actual);
        }

        [Test()]
        public void TestSubmit_SurroundsWithQuotes()
        {
            this.Helper.GenerateKeyBindsForEventTestSpawnKeybind();
            string valid = "\"spawn_npc" + " " + this.Helper.TestCharacter.Model + " " + this.Helper.TestCharacter.Name + "\"";
            string actual = KeyBindsGenerator.CompleteEvent();
            Assert.AreEqual(valid, actual);
        }

        [Test()]
        public void TestSubmit_MultipleKeybindsAppend()
        {
            this.Helper.GenerateKeyBindsForEventTargetFollowKeybind();
            string valid = "\"target_name" + " " + this.Helper.TestCharacter.Name + "$$follow \"";
            string actual = KeyBindsGenerator.CompleteEvent();
            Assert.AreEqual(valid, actual);
        }

        [Test()]
        public void TestSubmit_PlacesKeybindInTempBindFile()
        {
            this.Helper.GenerateKeyBindsForEventTestSpawnKeybind();
            KeyBindsGenerator.CompleteEvent();
            StreamReader actualFile = new StreamReader("C:\\Users\\user\\Documents\\Tequila\\data\\B.txt");
            string actual = actualFile.ReadLine();
            actualFile.Close();
            string valid = "Y \"spawn_npc" + " " + this.Helper.TestCharacter.Model + " " + this.Helper.TestCharacter.Name + "\"";
            Assert.AreEqual(valid, actual);
        }

    }
}