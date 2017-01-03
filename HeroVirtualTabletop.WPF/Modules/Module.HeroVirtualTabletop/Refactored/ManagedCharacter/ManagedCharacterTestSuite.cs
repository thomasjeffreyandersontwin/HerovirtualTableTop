using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moq;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture;
using HeroVirtualTableTop.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVirtualTableTop.ManagedCharacter
{
    [TestClass]
    public class ManagedCharacterTestSuite
    {
        ManagedCharacter CharacterUnderTest;
        public MockManagedCustomerFactory Factory;
        [TestInitialize]
        public void TestInitialize()
        {
            Factory = new MockManagedCustomerFactory(new MockDesktopFactory());
           
        }

        [TestMethod]
        public void Target_CallsGenerateCommandCorrectlyIfNoMemoryInstance(){
            string[] parameters = { "Moq Man!" };
            CharacterUnderTest = Factory.GetCharactersWithDependenciesMockedAndCommandGenerationSetup(DesktopCommand.TargetName, parameters);
            
            CharacterUnderTest.Target();
            KeyBindCommandGenerator generator = CharacterUnderTest.Generator;
            Mock.Get<KeyBindCommandGenerator>(generator).VerifyAll();
        }

        [TestMethod]
        public void Target_AssignsCorrectMemoryInstanceIfNoMemoryInstance(){
            CharacterUnderTest = Factory.CharacterUnderTest;
            CharacterUnderTest.Target();
            DesktopCharacterMemoryInstance instance = CharacterUnderTest.MemoryInstance;
            Assert.IsNotNull(instance);
        }
        [TestMethod]
        public void Target_UsesMemoryInstanceIfExists(){
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
            mocker.Setup(p => p.MoveToCharacter(It.Is<ManagedCharacter>(x => x.Equals(CharacterUnderTest))));

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
        public void SpawnToDesktop_RendersActiveIdentity() {
            CharacterUnderTest = Factory.CharacterUnderTest;

            CharacterUnderTest.SpawnToDesktop();

            Identity active = CharacterUnderTest.Identities.Active;
            Mock<Identity> mocker = Mock.Get(active);
            mocker.Verify(x => x.Render(true));

        }

        [TestMethod]
        public void SpawnToDesktop_CLearsFromDesktopIfAlreadySpawned()
        {
            string [] para ={};
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
        public void SpawnToDesktop__WithNoIdentityRendersCostumeWithNameOfCharacterAndCreatesDefaultIdentity() {
            CharacterUnderTest = Factory.CharacterUnderTestWithNoIdentities;
            CharacterUnderTest.SpawnToDesktop();
            Identity newlyCreatedId = CharacterUnderTest.Identities.Active;

            Assert.AreEqual(CharacterUnderTest.Name, newlyCreatedId.Name);
            Assert.AreEqual(CharacterUnderTest.Name, newlyCreatedId.Surface);
            Assert.AreEqual(newlyCreatedId.Type, SurfaceType.Costume);

            Mock<Identity> mocker = Mock.Get(newlyCreatedId);
            mocker.Verify(x => x.Render(true));


        }

        public void ClearsFromDesktop_RemiovesCharacterFromDesktop() { }
        public void ClearsFromDesktop_CLearsAllStateAndMemoryInstanceAndIdentity() { }
        public void MoveCharacterToCamera_CharacterHasSamePositionAsCamera() { }

    }

    class CameraTestSuite
    {
        public void MoveToCharacter_MovesCameraToPositionCloseToCharacter(){ }
        public void ManueverCharacter_SettingMoveToCharacter_MovesCameraToPositionCloseToCharacterThenCLearsCharacterThenTakesSurfaceOfCharactersActiveIdentity(){ }
    }
    class IdentityTestSuite
    {
        public void Render_GeneratesCorrectCommandsForCostume(){ }
        public void Render_GeneratesCorrectCommandsForModel(){ }
        public void Render_SpawnsModelWithCorrectName(){ }
    }
    class CharacterActionListTestSuite
    {
        public void Default_ReturnsFirstIfNoDefalultSetOrReturnsDefalultIfSet() { }
        public void Active_ReturnsActiveIfSetOrDefalultIfNotSet() { }
        public void GetNewValidActionName_ReturnsGenericActionTypeWithNumberIfNoNamePassedInOrNamePlusUniqueNumberIfNamePassed() { }
        public void Insert_InsertsAtBottomOfListIsRetrieveableByActionName() { }
        public void InsertAfter_InsertsAfterPreviousActionsIsRetrieveableByActionNameAndItemNumber() { }
        public void InsertAfter_ReordersItemIfItAlreadyExistsInList() { }
        public void ItemByInt_GetsCorrectAction() { }
        public void ItemByActionName_GetsCorrectAction() { }
        public void RemoveAction_CantRetreiveActionFromList() { }
        public void CreateNew_InsertsAndGeneratesUniqueName() { }
    }
    public class MockManagedCustomerFactory
    {
        public MockDesktopFactory MockDesktopFactory;
        IFixture MockFixture;
        IFixture CustomizedMockFixture;
        public MockManagedCustomerFactory(MockDesktopFactory desktopFactory)
        {
            MockDesktopFactory = desktopFactory;
            MockFixture = MockDesktopFactory.MockFixture;
            CustomizedMockFixture = MockDesktopFactory.CustomizedMockFixture;
            CustomizedMockFixture.Behaviors.Add(new OmitOnRecursionBehavior());

            MockFixture.Customize(new MultipleCustomization());
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
                var identityActionList= CustomizedMockFixture.Create<CharacterActionList<Identity>>();
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
    }

}

    
  
       