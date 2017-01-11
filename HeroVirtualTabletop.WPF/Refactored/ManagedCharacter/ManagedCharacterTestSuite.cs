using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moq;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture;
using HeroVirtualTableTop.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture.Kernel;

namespace HeroVirtualTableTop.ManagedCharacter
{
    [TestClass]
    public class ManagedCharacterTestSuite
    {
        ManagedCharacter CharacterUnderTest;
        public MockManagedCustomerFactory Factory;

        public ManagedCharacterTestSuite()
        {
            Factory = new MockManagedCustomerFactory(new MockDesktopFactory());
        }

        [TestMethod]
        public void Target_CallsGenerateCommandCorrectlyIfNoMemoryInstance() {
            string[] parameters = { "Moq Man!" };
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.TargetName, parameters);

            CharacterUnderTest.Target();
            KeyBindCommandGenerator generator = CharacterUnderTest.Generator;
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }

        [TestMethod]
        public void Target_AssignsCorrectMemoryInstanceIfNoMemoryInstance() {
            CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.Target();
            DesktopCharacterMemoryInstance instance = CharacterUnderTest.MemoryInstance;
            Assert.IsNotNull(instance);
        }
        [TestMethod]
        public void Target_UsesMemoryInstanceIfExists() {
            CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.MemoryInstance = CharacterUnderTest.Targeter.TargetedInstance;
            CharacterUnderTest.Target();
            DesktopCharacterMemoryInstance instance = CharacterUnderTest.MemoryInstance;
            Mock.Get<DesktopCharacterMemoryInstance>(instance).Verify(t => t.Target());

        }

        [TestMethod]
        public void IsTargeted_MatchesBasedMemoryInstance()
        {
            CharacterUnderTest = Factory.CharacterUnderTest;

            CharacterUnderTest.MemoryInstance = CharacterUnderTest.Targeter.TargetedInstance;
            Assert.AreEqual(CharacterUnderTest.IsTargeted, true);

            CharacterUnderTest.MemoryInstance = Factory.MockDesktopFactory.MockMemoryInstance;
            bool actual = CharacterUnderTest.IsTargeted;
            Assert.AreEqual(actual, false);

        }

        [TestMethod]
        public void Follows_GeneratesCorrectCommandText()
        {
            string[] para = { };
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.Follow, para);
            CharacterUnderTest.Follow();
            KeyBindCommandGenerator generator = CharacterUnderTest.Generator;
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }
        public void UnFollow_GeneratesCorrectCommandTextIfFollowedSetToFalse()
        {
            string[] para = { };
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.Follow, para);

            KeyBindCommandGenerator generator = CharacterUnderTest.Generator;
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }

        [TestMethod]
        public void TargetAndMoveCameraToCharacter_TellsCameraToMoveToCharacter()
        {
            CharacterUnderTest = Factory.CharacterUnderTest;
            Mock<Camera> mocker = Mock.Get<Camera>(CharacterUnderTest.Camera);
            mocker.Setup(p => p.MoveToTarget(true));

            CharacterUnderTest.TargetAndMoveCameraToCharacter();
            mocker.Verify();
        }

        [TestMethod]
        public void IsManueveringWithCamera_SettingToTrueSetsCameraCharacter()
        {
            CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.IsManueveringWithCamera = true;
            Assert.AreEqual(CharacterUnderTest, CharacterUnderTest.Camera.ManueveringCharacter);

            CharacterUnderTest.IsManueveringWithCamera = false;
            Assert.AreEqual(null, CharacterUnderTest.Camera.ManueveringCharacter);
        }

        [TestMethod]
        public void SpawnToDesktop_SpawnsDefaultModelThenRendersActiveIdentity() {
            //arrange
            CharacterUnderTest = Factory.CharacterUnderTest;

            //act
            CharacterUnderTest.SpawnToDesktop();

            //assert
            var mocker = Mock.Get<KeyBindCommandGenerator>(CharacterUnderTest.Generator);
            string[] para = { "model_statesmen", CharacterUnderTest.Name + " [" + CharacterUnderTest.Name +"]" };
            mocker.Verify(x => x.GenerateDesktopCommandText(DesktopCommand.SpawnNpc, para));

            Identity active = CharacterUnderTest.Identities.Active;
            Mock<Identity> idMocker = Mock.Get(active);
            idMocker.Verify(x => x.Render(true));

        }

        [TestMethod]
        public void SpawnToDesktop_CLearsFromDesktopIfAlreadySpawned()
        {
            string[] para = { };
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.DeleteNPC, para);

            CharacterUnderTest.SpawnToDesktop();
            CharacterUnderTest.SpawnToDesktop();

            Mock<KeyBindCommandGenerator> mocker = Mock.Get(CharacterUnderTest.Generator);
            mocker.VerifyAll();
        }

        [TestMethod]
        public void SpawnToDesktop_UnsetsManueveringWithDesktopIfSet()
        {
            CharacterUnderTest = Factory.CharacterUnderTest;

            CharacterUnderTest.IsManueveringWithCamera = true;
            CharacterUnderTest.SpawnToDesktop();

            Assert.AreEqual(CharacterUnderTest.IsManueveringWithCamera, false);
        }

        [TestMethod]
        public void SpawnToDesktop__WithNoIdentityRendersCostumeWithNameOfCharacterAndCreatesDefaultIdentity() {
            CharacterUnderTest = Factory.CharacterUnderTestWithNoIdentities;
            CharacterUnderTest.SpawnToDesktop();
            Identity newlyCreatedId = CharacterUnderTest.Identities.Active;

            Assert.AreEqual(CharacterUnderTest.Name, newlyCreatedId.Name);
            Assert.AreEqual(CharacterUnderTest.Name, newlyCreatedId.Surface);
            Assert.AreEqual(newlyCreatedId.Type, SurfaceType.Costume);
            Assert.AreEqual(newlyCreatedId.Owner, CharacterUnderTest);


            Mock<Identity> mocker = Mock.Get(newlyCreatedId);
            mocker.Verify(x => x.Render(true));


        }

        [TestMethod]
        public void ClearsFromDesktop_RemovesCharacterFromDesktop() {
            string[] para = { };
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.DeleteNPC, para);

            CharacterUnderTest.ClearFromDesktop();

            Mock<KeyBindCommandGenerator> mocker = Mock.Get(CharacterUnderTest.Generator);
            mocker.VerifyAll();

        }

        [TestMethod]
        public void ClearsFromDesktop_CLearsAllStateAndMemoryInstanceAndIdentity() {
            CharacterUnderTest = Factory.CharacterUnderTestWithNoIdentities;

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
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.MoveNPC, paras);
            CharacterUnderTest.MoveCharacterToCamera();

            Mock<KeyBindCommandGenerator> mocker = Mock.Get(CharacterUnderTest.Generator);
            mocker.VerifyAll();

        }

    }

    [TestClass]
    public class CameraTestSuite
    {
        public MockManagedCustomerFactory Factory;

        public CameraTestSuite()
        {
            Factory = new MockManagedCustomerFactory(new MockDesktopFactory());
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
            ManagedCharacter mockCharacter = Factory.MockCharacter;
            Camera cameraUnderTest = Factory.CameraUnderTest;

            //act
            cameraUnderTest.ManueveringCharacter = mockCharacter;

            //assert
            var charMocker = Mock.Get<ManagedCharacter>(mockCharacter);
            string[] para = { mockCharacter.Name };
            charMocker.Verify(x => x.ClearFromDesktop(true));

            var idMocker = Mock.Get<Identity>(cameraUnderTest.Identity);
            string[] para2 = { mockCharacter.Identities.Active.Name };
            idMocker.Verify(x => x.Render(true));

            Assert.AreEqual(cameraUnderTest.Identity, mockCharacter.Identities.Active);

        }
    }

    [TestClass]
    public class IdentityTestSuite
    {
        public MockManagedCustomerFactory Factory;

        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockManagedCustomerFactory(new MockDesktopFactory());
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
        public MockManagedCustomerFactory Factory;

        public CharacterActionListTestSuite()
        {
            Factory = new MockManagedCustomerFactory(new MockDesktopFactory());
        }

        [TestMethod]
        public void Active_ReturnsActiveIfSetOrDefalultIfNotSet()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            idList.Active = null;
            Assert.AreEqual(idList.Active, idList.Default);

            idList.Active = idList[2];
            Assert.AreEqual(idList.Active, idList[2]);

        }

        [TestMethod]
        public void ActiveAndDefault_ReturnsFIrstIfDefalultAMdActiveNotSet()
        {
            CharacterActionList<Identity> idList = Factory.IdentityListUnderTest;
            idList.Active = null;
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

            Assert.IsNull(idList[delId.Name]);
            Assert.AreEqual(lastId.Order, lastOrder--);
        }
    }
    public class MockManagedCustomerFactory
    {
        public MockDesktopFactory MockDesktopFactory;
        public IFixture MockFixture;
        public IFixture CustomizedMockFixture;
        public IFixture StandardizedFixture;
        public MockManagedCustomerFactory(MockDesktopFactory desktopFactory)
        {
            MockDesktopFactory = desktopFactory;
            MockFixture = MockDesktopFactory.MockFixture;

            //rescurive mocking
            CustomizedMockFixture = MockDesktopFactory.CustomizedMockFixture;
            CustomizedMockFixture.Behaviors.Add(new OmitOnRecursionBehavior());
            
            MockFixture.Customize(new MultipleCustomization());

            //always mock out core dependencies
            StandardizedFixture = new Fixture();
            StandardizedFixture.Inject<Position>(MockDesktopFactory.MockPosition);
            StandardizedFixture.Inject<KeyBindCommandGenerator>(MockDesktopFactory.MockKeybindGenerator);
            StandardizedFixture.Inject<DesktopCharacterTargeter>(MockDesktopFactory.MockDesktopCharacterTargeter);

            //mock managedCharacter dependencies by defalut
            StandardizedFixture.Inject<Camera>(MockCamera);
            StandardizedFixture.Inject<CharacterActionList<Identity>>(IdentityListUnderTest);
            StandardizedFixture.Inject<CharacterProgressBarStats>(MockCharacterProgressBarStats);

            //standardized interface mapping
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(DesktopCharacterMemoryInstance),
                        typeof(DesktopCharacterMemoryInstanceImpl)));
        }

        public ManagedCharacter CharacterUnderTestWithNoIdentities
        {
            get
            {
                ManagedCharacter character = CharacterUnderTest;
                foreach (Identity id in character.Identities.Values)
                {
                    character.Identities.Remove(id.Name);
                }
                character.Identities.Active = null;
                return character;
            }
        }
        public ManagedCharacter CharacterUnderTest
        {
            get
            {
                //set up constructor parameters
                DesktopCharacterTargeter targeter = MockDesktopFactory.MockDesktopCharacterTargeter;
                string characterName = "Moq Man!";
                KeyBindCommandGenerator generator = MockDesktopFactory.MockKeybindGenerator;
                Camera camera = MockCamera;
                CharacterActionList<Identity> identities = MockIdentities;

                //create the character
                ManagedCharacter characterUnderTest = new ManagedCharacterImpl(targeter, generator, camera, identities);
                characterUnderTest.Name = characterName;

                return characterUnderTest;
            }
        }
        public ManagedCharacter MockCharacter
        {
            get {
                Mock<ManagedCharacter> mocker = new Mock<ManagedCharacter>();
                ManagedCharacter character = CustomizedMockFixture.Create<ManagedCharacter>();

                mocker.SetupAllProperties();
                character.Identities.Active = MockIdentities.Active;

                return character;

            }
        }
        public ManagedCharacter GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand command, string[] parameters)
        {
            DesktopCharacterTargeter targeter = MockDesktopFactory.MockDesktopCharacterTargeter;
            string characterName = "Moq Man!";
            KeyBindCommandGenerator generator = MockDesktopFactory.GetMockKeyBindCommandGeneratorForCommand(command, parameters);
            Camera camera = MockCamera;
            CharacterActionList<Identity> identities = MockIdentities;

            ManagedCharacter character = new ManagedCharacterImpl(targeter, generator, camera, identities);
            character.Name = characterName;

            return character;
        }

        public CharacterActionList<Identity> MockIdentities
        {
            get
            {
                var identityActionList = CustomizedMockFixture.Create<CharacterActionList<Identity>>();
                Mock<CharacterActionList<Identity>> identityMocker = Mock.Get<CharacterActionList<Identity>>(identityActionList);
                identityMocker.SetupAllProperties();

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
                Camera cameraUnderTest = new CameraImpl(MockDesktopFactory.MockKeybindGenerator);

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
        public Identity CostumedIdentityUnderTest
        {
            get {
                Identity id = new IdentityImpl(MockCharacter, "aName","aCostume",SurfaceType.Costume, MockDesktopFactory.MockKeybindGenerator);
                return id;
            }
        }

        public Identity ModelIdentityUnderTest
        {
            get
            {
                Identity id = new IdentityImpl(MockCharacter, "aName", "aModel", SurfaceType.Model, MockDesktopFactory.MockKeybindGenerator);
                return id;
            }
        }

        public CharacterActionList<Identity> IdentityListUnderTest
        {
            get
            {
                KeyBindCommandGenerator generator = MockDesktopFactory.MockKeybindGenerator;
                StandardizedFixture.Register<KeyBindCommandGenerator>(() => generator);

               
                ManagedCharacter character = MockCharacter;
                StandardizedFixture.Register<ManagedCharacter>(() => character);
                StandardizedFixture.Register<Identity>(() => new IdentityImpl(MockCharacter, StandardizedFixture.Create("Name"), StandardizedFixture.Create("Surface"),SurfaceType.Costume, generator));

                CharacterActionListImpl<Identity> identityList = StandardizedFixture.Build<CharacterActionListImpl<Identity>>()
                    .With(x => x.Owner, MockCharacter)
                    .With(x => x.Type, CharacterActionType.Identity)
                    .With(x => x.Generator, generator)
                    .Create();

                var identites = new List<Identity>();
                StandardizedFixture.AddManyTo(identites);

                StandardizedFixture.Customizations.Add(
                new NumericSequenceGenerator());
                foreach (Identity i in identites)
                {
                    i.Order = StandardizedFixture.Create<int>();
                    identityList.Insert(i);
                }

                MockCharacter.Identities = identityList;
                return identityList;
            }

        }
    }


}

    
  
       