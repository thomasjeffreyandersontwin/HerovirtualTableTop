using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ploeh.AutoFixture;

using Ploeh.AutoFixture.AutoMoq;
using Moq;



namespace HeroVirtualTableTop.Desktop
{
    [TestClass]
    public class KeybindGeneratorTestSuite
    {

        KeyBindCommandGenerator generator;
        public MockDesktopFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockDesktopFactory(); 
        }


        [TestMethod]
        public void ExecuteCmd_SendsCommandToIconUtility()
        {
            IconInteractionUtility utility = Factory.GetMockInteractionUtilityForCommand("spawn_npc MODEL_STATESMAN TESTMODEL");
            generator = new KeyBindCommandGeneratorImpl(utility);
            generator.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, "MODEL_STATESMAN", "TESTMODEL");
            generator.CompleteEvent();

            Mock.Get<IconInteractionUtility>(utility).VerifyAll();

        }

        [TestMethod]
        public void ExecuteCmd_SendsMultipleParametersAsTextDividecBySpaces()
        {
            IconInteractionUtility utility = Factory.GetMockInteractionUtilityForCommand("benpc param1 param2");
            generator = new KeyBindCommandGeneratorImpl(utility);
            string[] parameters = new string[2] { "param1", "param2" };
            generator.GenerateDesktopCommandText(DesktopCommand.BeNPC, parameters);
            generator.CompleteEvent();

            Mock.Get<IconInteractionUtility>(utility).VerifyAll();
        }

        [TestMethod]
        public void GenerateKeyBindsForCommand_ConnectsMultipleCommandsBeforeExecuting()
        {
            IconInteractionUtility utility = Factory.GetMockInteractionUtilityForCommand("spawn_npc MODEL_STATESMAN TESTMODEL$$load_costume Spyder");
            generator = new KeyBindCommandGeneratorImpl(utility);
            
            string[] parameters = new string[2] { "MODEL_STATESMAN", "TESTMODEL" };
            generator.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, parameters);

            parameters = new string[1] { "Spyder" };
            generator.GenerateDesktopCommandText(DesktopCommand.LoadCostume, parameters);
            generator.CompleteEvent();

            Mock.Get<IconInteractionUtility>(utility).VerifyAll();
        }
    }

    public class MockDesktopFactory
    {


        public IFixture StandardardaFixture;
        public IFixture MockFixture;
        public IFixture CustomizedMockFixture;


        public MockDesktopFactory()
        {
            MockFixture = new Fixture();
            MockFixture.Customize(new AutoMoqCustomization());

            CustomizedMockFixture = new Fixture();
            CustomizedMockFixture.Customize(new AutoConfiguredMoqCustomization());
            CustomizedMockFixture.Customizations.Add(new NumericSequenceGenerator());
        }

        public IconInteractionUtility GetMockInteractionUtilityForCommand(string command)
        {
            var mock = MockFixture.Freeze<Mock<IconInteractionUtility>>()
                .Setup(t => t.ExecuteCmd(It.Is<string>(p => p.Equals(command))));
            return MockFixture.Create<IconInteractionUtility>();

        }
        public DesktopCharacterTargeter MockDesktopCharacterTargeter
        {
            get
            {
                var mock = MockFixture.Create<DesktopCharacterTargeter>();
               
                mock = MockFixture.Create<DesktopCharacterTargeter>();
                mock.TargetedInstance = MockMemoryInstance;
                return mock;

            }
        }
        public DesktopCharacterMemoryInstance MockMemoryInstance
        {
            get
            {
                var mock= CustomizedMockFixture.Create<DesktopCharacterMemoryInstance>();
                Mock.Get(mock).SetupAllProperties();
                mock = CustomizedMockFixture.Create<DesktopCharacterMemoryInstance>();
                return mock;
            }
        }
        public KeyBindCommandGenerator MockKeybindGenerator
        {
            get
            {
                var mock = CustomizedMockFixture.Create<KeyBindCommandGenerator>();
                return mock;
            }
        }
        public KeyBindCommandGenerator GetMockKeyBindCommandGeneratorForCommand(DesktopCommand command, string[] paramters)
        {
            var mock = MockFixture.Freeze<Mock<KeyBindCommandGenerator>>();
            if (paramters == null || paramters.Length == 0)
                mock.Setup(t => t.GenerateDesktopCommandText(It.Is<DesktopCommand>(p => p.Equals(command))));
            if (paramters.Length==1)
                mock.Setup(t => t.GenerateDesktopCommandText(It.Is<DesktopCommand>(p => p.Equals(command)), It.Is<string>(p => p.Equals(paramters[0]))));
            if (paramters.Length == 2)
                mock.Setup(t => t.GenerateDesktopCommandText(It.Is<DesktopCommand>(p => p.Equals(command)), It.Is<string>(p => p.Equals(paramters[0])), It.Is<string>(p => p.Equals(paramters[1]))));


            return MockFixture.Create<KeyBindCommandGenerator>(); ;
        }
    }
}