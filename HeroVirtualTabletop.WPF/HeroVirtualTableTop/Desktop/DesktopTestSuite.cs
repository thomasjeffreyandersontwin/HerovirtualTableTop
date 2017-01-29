using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace HeroVirtualTableTop.Desktop
{
    [TestClass]
    public class KeybindGeneratorTestSuite
    {
        private KeyBindCommandGenerator _generator;
        public DesktopTestObjectsFactory TestObjectsFactory;

        public KeybindGeneratorTestSuite()
        {
            TestObjectsFactory = new DesktopTestObjectsFactory();
        }

        [TestMethod]
        public void ExecuteCmd_SendsCommandToIconUtility()
        {
            //arrange
            var utility =
                TestObjectsFactory.GetMockInteractionUtilityThatVerifiesCommand("spawn_npc MODEL_STATESMAN TESTMODEL");
            //act
            _generator = new KeyBindCommandGeneratorImpl(utility);
            _generator.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, "MODEL_STATESMAN", "TESTMODEL");
            _generator.CompleteEvent();
            //assert
            Mock.Get(utility).VerifyAll();
        }

        [TestMethod]
        public void ExecuteCmd_SendsMultipleParametersAsTextDividecBySpaces()
        {
            //arrange
            var utility = TestObjectsFactory.GetMockInteractionUtilityThatVerifiesCommand("benpc param1 param2");
            var parameters = new[] {"param1", "param2"};
            //act
            _generator = new KeyBindCommandGeneratorImpl(utility);
            _generator.GenerateDesktopCommandText(DesktopCommand.BeNPC, parameters);
            _generator.CompleteEvent();
            //assert
            Mock.Get(utility).VerifyAll();
        }

        [TestMethod]
        public void GenerateKeyBindsForCommand_ConnectsMultipleCommandsUntilExecuteCmdSent()
        {
            //arrange
            var utility =
                TestObjectsFactory.GetMockInteractionUtilityThatVerifiesCommand(
                    "spawn_npc MODEL_STATESMAN TESTMODEL$$load_costume Spyder");
            _generator = new KeyBindCommandGeneratorImpl(utility);

            //act
            var parameters = new[] {"MODEL_STATESMAN", "TESTMODEL"};
            _generator.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, parameters);

            parameters = new[] {"Spyder"};
            _generator.GenerateDesktopCommandText(DesktopCommand.LoadCostume, parameters);
            _generator.CompleteEvent();

            //assert
            Mock.Get(utility).VerifyAll();
        }

        public void
            GenerateLongKeyBindsForCommand_BreaksCommandStringIntoChunksAndSendsEachChunkToIconInteractionUtilitySeparately
            () //todo
        {
        }
    }

    //todo
    public class PostionTestSuite
    {
    }

    //todo
    public class WindowsUtilitiesTestSuite
    {
    }

    //todo
    public class DesktopMemoryInstanceTestSuite
    {
    }

    public class DesktopTestObjectsFactory
    {
        public IFixture CustomizedMockFixture;
        public IFixture MockFixture;
        public IFixture StandardizedFixture;

        public DesktopTestObjectsFactory()
        {
            MockFixture = new Fixture();
            MockFixture.Customize(new AutoMoqCustomization());

            CustomizedMockFixture = new Fixture();
            CustomizedMockFixture.Customize(new AutoConfiguredMoqCustomization());
            CustomizedMockFixture.Customizations.Add(new NumericSequenceGenerator());
            //handle recursion
            CustomizedMockFixture.Behaviors.Add(new OmitOnRecursionBehavior());

            SetupMockFixtureToReturnSinlgetonDesktopCharacterTargeterWithBlankLabel();
        }

        public DesktopCharacterTargeter MockDesktopCharacterTargeter => CustomizedMockFixture.Create<DesktopCharacterTargeter>();

        public DesktopCharacterMemoryInstance MockMemoryInstance
        {
            get
            {
                var instance = CustomizedMockFixture.Create<DesktopCharacterMemoryInstance>();
                instance.Position.JustMissedPosition = MockPosition;
                return instance;
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

        public Position MockPosition => CustomizedMockFixture.Create<Position>();

        private void SetupMockFixtureToReturnSinlgetonDesktopCharacterTargeterWithBlankLabel()
        {
            var mock = CustomizedMockFixture.Create<DesktopCharacterTargeter>();
            // mock.TargetedInstance = MockMemoryInstance;
            mock.TargetedInstance.Label = "";
            CustomizedMockFixture.Inject(mock);
        }

        public IconInteractionUtility GetMockInteractionUtilityThatVerifiesCommand(string command)
        {
           MockFixture.Freeze<Mock<IconInteractionUtility>>()
                .Setup(t => t.ExecuteCmd(It.Is<string>(p => p.Equals(command))));
            var mock = MockFixture.Create<IconInteractionUtility>();
            MockFixture.Inject(mock);
            return mock;
        }

        public KeyBindCommandGenerator GetMockKeyBindCommandGeneratorForCommand(DesktopCommand command,
            string[] paramters)
        {
            var mock = MockFixture.Freeze<Mock<KeyBindCommandGenerator>>();
            if (paramters == null || paramters.Length == 0)
                mock.Setup(t => t.GenerateDesktopCommandText(It.Is<DesktopCommand>(p => p.Equals(command))));
            if (paramters?.Length == 1)
                mock.Setup(
                    t =>
                        t.GenerateDesktopCommandText(It.Is<DesktopCommand>(p => p.Equals(command)),
                            It.Is<string>(p => p.Equals(paramters[0]))));
            if (paramters?.Length == 2)
                mock.Setup(
                    t =>
                        t.GenerateDesktopCommandText(It.Is<DesktopCommand>(p => p.Equals(command)),
                            It.Is<string>(p => p.Equals(paramters[0])), It.Is<string>(p => p.Equals(paramters[1]))));


            return MockFixture.Create<KeyBindCommandGenerator>();
            
        }
    }
}