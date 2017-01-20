using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moq;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture;
using HeroVirtualTableTop.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture.Kernel;
using System.Linq;
namespace HeroVirtualTableTop.ManagedCharacter
{
    [TestClass]
    public class ManagedCharacterTestSuite
    {
       
        public FakeManagedCustomerFactory Factory;

        public ManagedCharacterTestSuite()
        {
            Factory = new FakeManagedCustomerFactory(new FakeDesktopFactory());
        }

        [TestMethod]
        public void Target_CallsGenerateCommandCorrectlyIfNoMemoryInstance() {          
            ManagedCharacter character = Factory.CharacterUnderTest;
            string[] parameters = { character.Name + " ["+ character.Name +"]"};
            KeyBindCommandGenerator generator = Factory.DesktopFactory.GetMockKeyBindCommandGeneratorForCommand(DesktopCommand.TargetName, parameters);
            character.Generator = generator;
            character.MemoryInstance = null;

            character.Target();
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }

        [TestMethod]
        public void Target_AssignsCorrectMemoryInstanceIfNoMemoryInstance() {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;
            Factory.CharacterUnderTest.Target();
            DesktopCharacterMemoryInstance instance = Factory.CharacterUnderTest.MemoryInstance;
            Assert.IsNotNull(instance);
        }
        [TestMethod]
        public void Target_UsesMemoryInstanceIfExists() {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.MemoryInstance = CharacterUnderTest.Targeter.TargetedInstance;
            CharacterUnderTest.Target();
            DesktopCharacterMemoryInstance instance = CharacterUnderTest.MemoryInstance;
            Mock.Get<DesktopCharacterMemoryInstance>(instance).Verify(t => t.Target());

        }

        [TestMethod]
        public void IsTargeted_MatchesBasedMemoryInstance()
        {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;

            CharacterUnderTest.MemoryInstance = CharacterUnderTest.Targeter.TargetedInstance;
            Assert.AreEqual(CharacterUnderTest.IsTargeted, true);

            CharacterUnderTest.MemoryInstance = Factory.DesktopFactory.MockMemoryInstance;
            bool actual = CharacterUnderTest.IsTargeted;
            Assert.AreEqual(actual, false);

        }

        [TestMethod]
        public void Follows_GeneratesCorrectCommandText()
        {
            string[] para = { };
            ManagedCharacter CharacterUnderTest = Factory.GetCharacterUnderTestWithCommandGenerationSetupToCommand(DesktopCommand.Follow, para);
            CharacterUnderTest.Follow();
            KeyBindCommandGenerator generator = CharacterUnderTest.Generator;
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }
        public void UnFollow_GeneratesCorrectCommandTextIfFollowedSetToFalse()
        {
            string[] para = { };
            ManagedCharacter CharacterUnderTest = Factory.GetCharacterUnderTestWithCommandGenerationSetupToCommand(DesktopCommand.Follow, para);

            KeyBindCommandGenerator generator = CharacterUnderTest.Generator;
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }

        [TestMethod]
        public void TargetAndMoveCameraToCharacter_TellsCameraToMoveToCharacter()
        {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;
            Mock<Camera> mocker = Mock.Get<Camera>(CharacterUnderTest.Camera);
            mocker.Setup(p => p.MoveToTarget(true));

            CharacterUnderTest.TargetAndMoveCameraToCharacter();
            mocker.Verify();
        }

        [TestMethod]
        public void IsManueveringWithCamera_SettingToTrueSetsCameraCharacter()
        {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.IsManueveringWithCamera = true;
            Assert.AreEqual(CharacterUnderTest, CharacterUnderTest.Camera.ManueveringCharacter);

            CharacterUnderTest.IsManueveringWithCamera = false;
            Assert.AreEqual(null, CharacterUnderTest.Camera.ManueveringCharacter);
        }

        [TestMethod]
        public void SpawnToDesktop_SpawnsDefaultModelThenRendersActiveIdentity() {
            //arrange
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;

            //act
            CharacterUnderTest.SpawnToDesktop();

            //assert
            var mocker = Mock.Get<KeyBindCommandGenerator>(CharacterUnderTest.Generator);
            string[] para = { "model_statesmen", CharacterUnderTest.Name + " [" + CharacterUnderTest.DesktopLabel +"]" };
            mocker.Verify(x => x.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, para));
        }

        [TestMethod]
        public void SpawnToDesktop_CLearsFromDesktopIfAlreadySpawned()
        {
            string[] para = { };
            ManagedCharacter CharacterUnderTest = Factory.GetCharacterUnderTestWithCommandGenerationSetupToCommand(DesktopCommand.DeleteNPC, para);

            CharacterUnderTest.SpawnToDesktop();
            CharacterUnderTest.SpawnToDesktop();

            Mock<KeyBindCommandGenerator> mocker = Mock.Get(CharacterUnderTest.Generator);
            mocker.VerifyAll();
        }

        [TestMethod]
        public void SpawnToDesktop_UnsetsManueveringWithDesktopIfSet()
        {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;

            CharacterUnderTest.IsManueveringWithCamera = true;
            CharacterUnderTest.SpawnToDesktop();

            Assert.AreEqual(CharacterUnderTest.IsManueveringWithCamera, false);
        }

        [TestMethod]
        public void SpawnToDesktop__WithNoIdentityRendersCostumeWithNameOfCharacterAndCreatesDefaultIdentity() {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.SpawnToDesktop();
            Identity newlyCreatedId = CharacterUnderTest.Identities.Active;

            Assert.AreEqual(CharacterUnderTest.Name, newlyCreatedId.Name);
            Assert.AreEqual(CharacterUnderTest.Name, newlyCreatedId.Surface);
            Assert.AreEqual(newlyCreatedId.Type, SurfaceType.Costume);
            Assert.AreEqual(newlyCreatedId.Owner, CharacterUnderTest);
        }

        [TestMethod]
        public void ClearsFromDesktop_RemovesCharacterFromDesktop() {
            string[] para = { };
            ManagedCharacter CharacterUnderTest = Factory.GetCharacterUnderTestWithCommandGenerationSetupToCommand(DesktopCommand.DeleteNPC, para);

            CharacterUnderTest.ClearFromDesktop();

            Mock<KeyBindCommandGenerator> mocker = Mock.Get(CharacterUnderTest.Generator);
            mocker.VerifyAll();

        }

        [TestMethod]
        public void ClearsFromDesktop_CLearsAllStateAndMemoryInstanceAndIdentity() {
            ManagedCharacter CharacterUnderTest = Factory.CharacterUnderTest;

            CharacterUnderTest.ClearFromDesktop();

            Assert.IsNull(CharacterUnderTest.MemoryInstance);
            Assert.IsFalse(CharacterUnderTest.IsSpawned);
            Assert.IsFalse(CharacterUnderTest.IsTargeted);
            Assert.IsFalse(CharacterUnderTest.IsFollowed);
            Assert.IsFalse(CharacterUnderTest.IsManueveringWithCamera);
        }

        [TestMethod]
        public void MoveCharacterToCamera_GeneratesCorrectCommand() {
            string[] paras = { };
            ManagedCharacter CharacterUnderTest = Factory.GetCharacterUnderTestWithCommandGenerationSetupToCommand(DesktopCommand.MoveNPC, paras);
            CharacterUnderTest.MoveCharacterToCamera();

            Mock<KeyBindCommandGenerator> mocker = Mock.Get(CharacterUnderTest.Generator);
            mocker.VerifyAll();

        }

    }

    [TestClass]
    public class CameraTestSuite
    {
        public FakeManagedCustomerFactory Factory;

        public CameraTestSuite()
        {
            Factory = new FakeManagedCustomerFactory(new FakeDesktopFactory());
        }

        [TestMethod]
        public void MoveToCharacter_GenerateCommandToFollowCharacter() {
            Camera cameraUnderTest = Factory.CameraUnderTest;

            ManagedCharacter character = Factory.CharacterUnderTest;
            character.Target();

            cameraUnderTest.MoveToTarget();

            KeyBindCommandGenerator generator = cameraUnderTest.Generator;

            string[] para = { "" };
            Mock.Get<KeyBindCommandGenerator>(generator).Verify(x => x.GenerateDesktopCommandText(It.Is<DesktopCommand>(y => y.Equals(DesktopCommand.Follow)), para));
        }

        [TestMethod]
        public void ManueverCharacter_StopsWaitingToGetToDestinationIfdNoChangeInDistanceAfterSixChecks() {
            ManagedCharacter character = Factory.MockFixture.Create<ManagedCharacter>();
            Mock.Get<ManagedCharacter>(character).SetupAllProperties();

            Camera cameraUnderTest = Factory.CameraUnderTest;
            Mock<Position> mocker = Mock.Get<Position>(cameraUnderTest.Position);

            float distance;
            int counter = 0;
            mocker.Setup(x => x.IsWithin(It.IsAny<float>(), It.IsAny<Position>(), out distance)).Returns(false).Callback(() => counter++);

            cameraUnderTest.ManueveringCharacter = character;
            Assert.AreEqual(6, counter);
        }

        [TestMethod]

        public void ManueverCharacter_ContinuesToWaitForCameraToGetToDestinationUntilWithinMinimumDistance()
        {
            ManagedCharacter character = Factory.MockFixture.Create<ManagedCharacter>();
            Mock.Get<ManagedCharacter>(character).SetupAllProperties();

            Camera cameraUnderTest = Factory.CameraUnderTest;
            Mock<Position> mocker = Mock.Get<Position>(cameraUnderTest.Position);
            
            float distance = 3;
            mocker.Setup(x => x.IsWithin(It.IsAny<float>(), It.IsAny<Position>(), out distance)).Returns(
                    delegate ()
                    {
                        if (distance > 0)
                        {
                            distance--;
                            return false;
                        }
                        return true;
                    }
            );
            cameraUnderTest.ManueveringCharacter = character;
            Assert.AreEqual(distance, 0);
        }
        [TestMethod]
        public void ManueverCharacter_ClearsCharacterFromDesktopAndCameraBecomesCharacter()
        {
            //arrange
            ManagedCharacter characterUnderTest = Factory.CharacterUnderTest;
            
            characterUnderTest.Identities.Active = Factory.Mockidentity;
            Camera cameraUnderTest = Factory.CameraUnderTest;

            //act
            cameraUnderTest.ManueveringCharacter = characterUnderTest;

            //assert
            var keyMocker = Mock.Get<KeyBindCommandGenerator>(characterUnderTest.Generator);
            string[] para = {  };

            keyMocker.Verify(x => x.GenerateDesktopCommandText(DesktopCommand.DeleteNPC, para));

            var idMocker = Mock.Get<Identity>(cameraUnderTest.Identity);
            string[] para2 = { characterUnderTest.Identities.Active.Name };
            idMocker.Verify(x => x.Render(true));

            Assert.AreEqual(cameraUnderTest.Identity, characterUnderTest.Identities.Active);

        }
    }

    [TestClass]
    public class IdentityTestSuite
    {
        public FakeManagedCustomerFactory Factory;

        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new FakeManagedCustomerFactory(new FakeDesktopFactory());
        }

        [TestMethod]
        public void Render_GeneratesCorrectCommandsForCostume() {
            //arrange
            Identity id = Factory.CostumedIdentityUnderTest;

            //act
            id.Render();

            //assert
            var mocker = Mock.Get<KeyBindCommandGenerator>(id.Generator);
            string[] para = { id.Surface };
            mocker.Verify(x => x.GenerateDesktopCommandText(DesktopCommand.LoadCostume, para));
        }


        [TestMethod]
        public void Render_GeneratesCorrectCommandsForModel() {
            //arrange
            Identity id = Factory.ModelIdentityUnderTest;

            //act
            id.Render();

            //assert
            var mocker = Mock.Get<KeyBindCommandGenerator>(id.Generator);
            string[] para = { id.Surface };
            mocker.Verify(x => x.GenerateDesktopCommandText(DesktopCommand.BeNPC, para));
        }
    }

    [TestClass]
    public class CharacterActionListTestSuite
    {
        public FakeManagedCustomerFactory Factory;

        public CharacterActionListTestSuite()
        {
            Factory = new FakeManagedCustomerFactory(new FakeDesktopFactory());
        }

        [TestMethod]
        public void Active_ReturnsDefalultIfNotSet()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            idList.Deactivate();
            Assert.AreEqual(idList.Active, idList.Default);



        }

        [TestMethod]
        public void ActiveAndDefault_ReturnsFIrstIfDefalultAMdActiveNotSet()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            idList.Deactivate();
            idList.Default = null;
            Assert.AreEqual(idList.Default, idList[1]);
            Assert.AreEqual(idList.Active, idList[1]);
        }

        [TestMethod]
        public void GetNewValidActionName_ReturnsGenericActionTypeWithNumberIfNoNamePassedInOrNamePlusUniqueNumberIfNamePassed()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;

            string name = idList.GetNewValidActionName();
            Assert.AreEqual("Identity", name);

            idList[1].Name = name;
            name = idList.GetNewValidActionName();
            Assert.AreEqual("Identity (1)", name);

            idList[1].Name = "Spyder";
            name = idList.GetNewValidActionName(idList[1].Name);
            Assert.AreEqual("Spyder (1)", name);

        }

        [TestMethod]
        public void Insert_InsertsAtBottomOfListAndIsRetrieveableByActionName()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            Identity id = Factory.ModelIdentityUnderTest;
            idList.Insert(id);

            Assert.AreEqual(idList.Owner, id.Owner);
            Assert.AreEqual(idList[idList.Count], id);
            Assert.AreEqual(idList[id.Name], id);


        }

        [TestMethod]
        public void InsertAfter_InsertsAfterPreviousActionsIsRetrieveableByActionNameAndCorrectItemNumber()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            Identity prevId = idList[2];
            Identity afterId = idList[3];
            Identity idToAdd = Factory.ModelIdentityUnderTest;

            idList.InsertAfter(idToAdd, prevId);

            Assert.AreEqual(idToAdd.Order, prevId.Order + 1);
            Assert.AreEqual(idList[prevId.Order + 1], idToAdd);
            Assert.AreEqual(idList[idToAdd.Order + 1], afterId);
            Assert.AreEqual(idToAdd.Order + 1, afterId.Order);
            Assert.AreEqual(idList[idToAdd.Name], idToAdd);

        }
        [TestMethod]
        public void RemoveAction_CantRetreiveActionFromList()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            Identity delId = idList[2];
            Identity lastId = idList[idList.Count];
            int lastOrder = lastId.Order;
            idList.RemoveAction(delId);

            Assert.IsFalse(idList.ContainsKey(delId.Name));
            Assert.AreEqual(lastId.Order, lastOrder-1);
        }
    }
    public class FakeManagedCustomerFactory
    {
        public FakeDesktopFactory DesktopFactory;
        public IFixture MockFixture;
        public IFixture CustomizedMockFixture;
        public IFixture StandardizedFixture;
        public FakeManagedCustomerFactory(FakeDesktopFactory desktopFactory)
        {
            DesktopFactory = desktopFactory;
            MockFixture = DesktopFactory.MockFixture;
            setupMockFixture();
            setupStandardizedFixture();
        }
        private void setupMockFixture()
        {
            //rescurive mocking
            CustomizedMockFixture = DesktopFactory.CustomizedMockFixture;

            //handle recursion
            CustomizedMockFixture.Behaviors.Add(new OmitOnRecursionBehavior());

            //enable all collection<T>
            MockFixture.Customize(new MultipleCustomization());
        }
        private void setupStandardizedFixture()
        {
            //mock out core dependencies
            StandardizedFixture = new Fixture();
            StandardizedFixture.Inject<Position>(DesktopFactory.MockPosition);
            StandardizedFixture.Inject<KeyBindCommandGenerator>(DesktopFactory.MockKeybindGenerator);
            StandardizedFixture.Inject<DesktopCharacterTargeter>(DesktopFactory.MockDesktopCharacterTargeter);
            StandardizedFixture.Inject<DesktopCharacterMemoryInstance>(DesktopFactory.MockMemoryInstance);
            StandardizedFixture.Inject<Camera>(MockCamera);
            StandardizedFixture.Inject<CharacterProgressBarStats>(MockCharacterProgressBarStats);

            //map interfaces to classes 
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(DesktopCharacterMemoryInstance),
                        typeof(DesktopCharacterMemoryInstanceImpl)));
            StandardizedFixture.Customizations.Add(
               new TypeRelay(
                   typeof(ManagedCharacter),
                       typeof(ManagedCharacterImpl)));
            StandardizedFixture.Customizations.Add(
               new TypeRelay(
                    typeof(CharacterActionList<Identity>),
                        typeof(CharacterActionListImpl<Identity>)));
            StandardizedFixture.Customizations.Add(
               new TypeRelay(
                    typeof(Identity),
                        typeof(IdentityImpl)));

            //handle recursion
            StandardizedFixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        public ManagedCharacter CharacterUnderTest
        {
            get
            {
                return StandardizedFixture.Create<ManagedCharacter>();
            }
        }
        public ManagedCharacter CharacterUnderTestWithIdentities
        {
            get
            {
                return StandardizedFixture.Build<ManagedCharacterImpl>()
                    .Do(
                        c => c.Identities.AddMany(
                            StandardizedFixture.Build<Identity>()
                                .With(y => y.Owner, c).CreateMany().ToList()
                            )
                        ).Create();
            }
        }
        public ManagedCharacter MockCharacter
        {
            get
            {
                ManagedCharacter character = CustomizedMockFixture.Create<ManagedCharacter>();
                return character;
            }
        }
        public ManagedCharacter GetCharacterUnderTestWithCommandGenerationSetupToCommand(DesktopCommand command, string[] parameters)
        {
            ManagedCharacter character = CharacterUnderTest;
            KeyBindCommandGenerator generator = DesktopFactory.GetMockKeyBindCommandGeneratorForCommand(command, parameters);
            character.Generator = generator;
            return character;
        }

        public CharacterActionList<Identity> MockIdentities
        {
            get
            {

                var identityActionList = CustomizedMockFixture.Create<CharacterActionList<Identity>>();
                IEnumerable<Identity> identities = CustomizedMockFixture.Create<IEnumerable<Identity>>();

                foreach (Identity id in identities)
                {
                    identityActionList.Add(id.Name, id);
                }

                var e = identities.GetEnumerator();
                e.MoveNext();

                identityActionList.Active = e.Current;
                return identityActionList;
            }
        }

        public List<Identity> MockIdList
        {
            get
            {
                return CustomizedMockFixture.Create<IEnumerable<Identity>>().ToList();
            }
        }
        public Camera MockCamera
        {
            get
            {
                var mock = MockFixture.Freeze<Camera>();
                Mock<Camera> cameraMocker = Mock.Get<Camera>(mock);
                cameraMocker.SetupAllProperties();
                return mock;

            }
        }
        public Camera CameraUnderTest
        {
            get
            {
                Camera cameraUnderTest = new CameraImpl(DesktopFactory.MockKeybindGenerator);

                Position pos = MockFixture.Create<Position>();
                cameraUnderTest.Position = pos;

                return cameraUnderTest;
            }
        }

        public CharacterProgressBarStats MockCharacterProgressBarStats
        {
            get
            {
                return MockFixture.Create<CharacterProgressBarStats>();

            }
        }

        public Identity Mockidentity
        {
            get
            {
                Identity id = CustomizedMockFixture.Create<Identity>();
                return id;

            }
        }
        public Identity CostumedIdentityUnderTest
        {
            get
            {
                Identity id = new IdentityImpl(MockCharacter, "aName", "aCostume", SurfaceType.Costume, DesktopFactory.MockKeybindGenerator,null);
                return id;
            }
        }
        public Identity ModelIdentityUnderTest
        {
            get
            {
                Identity id = new IdentityImpl(MockCharacter, "aName", "aModel", SurfaceType.Model, DesktopFactory.MockKeybindGenerator,null);
                return id;
            }
        }

        public CharacterActionList<Identity> IdentityListUnderTest
        {
            get
            {
                return StandardizedFixture.Build<CharacterActionListImpl<Identity>>()
                    .With(x => x.Type, CharacterActionType.Identity)
                    .Do(x => x.AddMany(StandardizedFixture.CreateMany<Identity>().ToList()))
                    .Create();
            }
        }
        public CharacterActionList<Identity> MockIdentityList
        {
            get
            {
                return CustomizedMockFixture.Create<CharacterActionList<Identity>>();
            }
        }
    }


}

    
  
       