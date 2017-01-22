using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moq;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture.Kernel;
using System.Linq;
using HeroVirtualTableTop.Desktop;
using HeroVirtualTableTop.ManagedCharacter;
using HeroVirtualTableTop.Crowd;
using IrrKlang;

namespace HeroVirtualTableTop.AnimatedAbility
{
    [TestClass]
    public class AnimatedElementTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));

        [TestMethod]
        public void PlayMultipleTargets_PlayElementOnEachTarget()
        {
            //arrange
            List<AnimatedCharacter> characters = Factory.MockAnimatedCharacterList;
            AnimationElement element = Factory.FakeAnimationElementUnderTest;
            //act
            element.Play(characters);

            //arrange
            foreach (AnimatedCharacter character in characters)
            {
                Mock.Get<AnimatedCharacter>(character).Verify(x => x.AddState(null, true));
            }
        }

        [TestMethod]
        public void PlayTarget_TargetsIfCharacterNotAlreadyTargeted()
        {
            //arrange
            AnimatedCharacter character = Factory.MockAnimatedCharacter;
            character.IsTargeted = false;
            AnimationElement element = Factory.FakeAnimationElementUnderTest;
            //act
            element.Play(character);
            //assert
            Mock.Get<AnimatedCharacter>(character).Verify(x => x.Target(false));

            //arrange
            character = Factory.MockAnimatedCharacter;
            character.IsTargeted = true;
            //act
            element.Play(character);
            //assert
            Mock.Get<AnimatedCharacter>(character).Verify(x => x.Target(false), Times.Never);

        }

        public void Flattened_AddsElementToEndOfList()
        {

        }

    }
    [TestClass]
    public class MovElementTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));

        [TestMethod]
        public void MovElement_PlaysMovBasedOnOwnerAndMovResource()
        {
            //arrange
            MovElement element = Factory.MovElementUnderTest;
            AnimatedCharacter character = element.Target;
            character.IsTargeted = false;
            //act
            element.Play();
            //assert
            Mock.Get<AnimatedCharacter>(character).Verify(x => x.Target(false));
            string[] para2 = { element.Mov.FullResourcePath };
            Mock.Get<KeyBindCommandGenerator>(character.Generator).Verify(x => x.GenerateDesktopCommandText(DesktopCommand.Move, para2));
        }
        [TestMethod]
        public void MovOrFxMarkedPlaywithNext_DoesNotExecuteCommand()
        {
            //arrange
            MovElement element = Factory.MovElementUnderTest;
            KeyBindCommandGenerator generator = element.Target.Generator;
            //act
            element.PlayWithNext = true;
            element.Play();
            //assert
            Mock.Get<KeyBindCommandGenerator>(generator).Verify(x => x.CompleteEvent(), Times.Never);


            //arrange
            FXElement element2 = Factory.FXElementUnderTestWithMockAnimatedCharacter;
            element2.PlayWithNext = true;
            element2.Play();
            //assert
            Mock.Get<KeyBindCommandGenerator>(generator).Verify(x => x.CompleteEvent(), Times.Never);
        }
    }
    [TestClass]
    public class FXAnimatedElementTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));
        [TestMethod]
        public void FXElement_CreatesCostumeForOwnerBasedOnFXResource()
        {
            //arrange
            FXElement element = Factory.FXElementUnderTestWithMockAnimatedCharacter;
            KeyBindCommandGenerator generator = element.Target.Generator;
            element.PlayWithNext = true;
            element.Play();
            //assert
            Mock.Get<KeyBindCommandGenerator>(generator).Verify(x => x.CompleteEvent(), Times.Never);
        }

        [TestMethod]
        public void FXElement_LoadsCostumeOnOwnerWithFXResourceBasedOnActiveIdentity()
        {
            //arrange
            FXElement element = Factory.FXElementUnderTestWithAnimatedCharacter;
            File.Create(element.CostumeFilePath).Close();   
            KeyBindCommandGenerator generator = element.Target.Generator;
            element.Target.Identities.Active = element.Target.Identities.FirstOrDefault().Value;
            //act
            element.Play();

            //assert
            string[] para = { element.ModifiedCostumeFilePath };
            Mock.Get<KeyBindCommandGenerator>(generator).Verify(x => x.GenerateDesktopCommandText(DesktopCommand.LoadCostume, para));
            Assert.IsTrue(element.ModifiedCostumeContainsFX);
            File.Delete(element.CostumeFilePath);
        }

        [TestMethod]
        public void FXElement_LoadsCostumeOnOwnerWithFXResourceBasedOnCharacterName()
        {
            //arrange
            FXElement element = Factory.FXElementUnderTestWithAnimatedCharacter;
            File.Create(element.CostumeFilePath).Close();
            KeyBindCommandGenerator generator = element.Target.Generator;

            //act
            element.Play();

            //assert
            string[] para = { element.ModifiedCostumeFilePath };
            Mock.Get<KeyBindCommandGenerator>(generator).Verify(x => x.GenerateDesktopCommandText(DesktopCommand.LoadCostume, para));
            Assert.IsTrue(element.ModifiedCostumeContainsFX);
            File.Delete(element.CostumeFilePath);
        }

        [TestMethod]
        public void FXElement_IsNotInCostumeFileIfSecondFXElementIsRenderedForSameOwner()
        {
            //arrange
            FXElement element = Factory.FXElementUnderTestWithAnimatedCharacter;
            element.Persistent = false;
            File.Create(element.CostumeFilePath).Close();
            
            //act
            element.Play();

            FXElement element2 = Factory.FXElementUnderTestWithMockAnimatedCharacter;
            element2.Target = element.Target;
            element2.Play();

            Assert.IsFalse(element.ModifiedCostumeContainsFX);
            Assert.IsTrue(element2.ModifiedCostumeContainsFX);
        }

        [TestMethod]
        public void PersistentFXElement_StayInCostumeFileIfSecondFXElementIsRenderedForSameOwner()
        {
            //arrange
            FXElement element = Factory.FXElementUnderTestWithAnimatedCharacter;
            element.Persistent = true;
            File.Create(element.CostumeFilePath).Close();

            //act
            element.Play();

            FXElement element2 = Factory.FXElementUnderTestWithMockAnimatedCharacter;
            element2.Target = element.Target;
            element2.Play();

            Assert.IsTrue(element.ModifiedCostumeContainsFX);
            Assert.IsTrue(element2.ModifiedCostumeContainsFX);

        }
    }
    [TestClass]
    public class SoundElementTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));
        [TestMethod]
        public void SoundElement_PlaysSoundResource()
        {
            //arrange
            SoundElement element = Factory.SoundElementUnderTest;  

            //act
            element.Play();

            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x=>x.Play3D(element.SoundFileName, It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>()));
        }

        [TestMethod]
        public void SoundElement_PlaysSoundAtPositionOfOwnerFromCameraPosition()
        {
            //arrange
            SoundElement element = Factory.SoundElementUnderTest;
            element.Target.MemoryInstance = Factory.CrowdFactory.CharacterFactory.DesktopFactory.MockMemoryInstance;

            //act

            element.Play();
            Position characterPos = element.Target.Position;
            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x => x.Play3D(element.SoundFileName, characterPos.X, characterPos.Y, characterPos.Z, false));

            Position cameraPos = element.Target.Camera.Position;
            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x => x.SetListenerPosition(cameraPos.X, cameraPos.Y, cameraPos.Z, 0,0,1));

        }

        [TestMethod]
        public void PersistentSoundElement_PlaysSoundInALoop()
        {
            //arrange
            SoundElement element = Factory.SoundElementUnderTest;

            //act
            element.Persistent = true;
            element.Play();

            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x => x.Play3D(element.SoundFileName, It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(), true));
        }

        [TestMethod]
        public void PersistentSoundElement_ChangesPositionPlayingFromWhenOwnerChangesPosition()
        {
            //arrange
            SoundElement element = Factory.SoundElementUnderTest;
            element.Target.MemoryInstance = Factory.CrowdFactory.CharacterFactory.DesktopFactory.MockMemoryInstance;
            element.Persistent = true;
            //act

            element.Play();

            //assert
            Position cameraPos = element.Target.Camera.Position;
            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x => x.SetListenerPosition(cameraPos.X, cameraPos.Y, cameraPos.Z, 0, 0, 1));

            //act
            element.Target.Camera.Position.X+=20;
            element.Target.Camera.Position.Y+=10;
            element.Target.Camera.Position.Z += 20;
            cameraPos = element.Target.Camera.Position;

            //assert
            System.Threading.Thread.Sleep(500);
            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x => x.SetListenerPosition(cameraPos.X, cameraPos.Y, cameraPos.Z, 0, 0, 1));

        }

        [TestMethod]
        public void PersistentSoundElement_StopsPlayingInLoopWhenTurnedOff()
        {
            //arrange
            SoundElement element = Factory.SoundElementUnderTest;

            //act
            element.Persistent = true;
            element.Play();
            element.Active = false;

            //assert
            Mock.Get<SoundEngineWrapper>(element.SoundEngine).Verify(
                x => x.StopAllSounds());

        }
    }
    [TestClass]
    public class PauseElementTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));

       
        public void PauseElement_PausesDuration()
        {
            //arrange
            PauseElement element = Factory.PauseElementUnderTest;
            DateTime now = DateTime.Now;
            //act
            element.Duration = 1000;
            element.Play();

            DateTime after = DateTime.Now;
            //assert

            DateTime expected = now.AddMilliseconds(element.Duration);
            Assert.AreEqual(expected.Second, after.Second);
        }
        
        public void PauseElementWithUnitPause_PausesBasedOnDistanceOfTarget()
        {
            //arrange
            PauseElement element = Factory.PauseElementUnderTest;

            Position targetPos = Factory.CrowdFactory.CharacterFactory.DesktopFactory.MockPosition;
            targetPos.X = element.Target.Position.X + 20;
            targetPos.Y = element.Target.Position.Y + 10;
            targetPos.Z = element.Target.Position.Z + 20;
            DateTime now = DateTime.Now;

            //act
            element.IsUnitPause = true;
            element.TargetPosition = targetPos;
            element.Play();

            //assert
            DateTime after = DateTime.Now;
            DateTime expected = now.AddMilliseconds(element.Duration);
            Assert.AreEqual(expected.Second, after.Second);

        }

    }
    [TestClass]
    public class SequenceElementTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));

        [TestMethod]
        public void AndSequenceElement_PlaysAllElementChildrenInOrder()
        {
            //arrange
            SequenceElement element = Factory.SequenceElementUnderTestWithMockChildren;

            //act
            element.Play();

            //assert

            List<AnimationElement> elements = element.AnimationElements;
            
            foreach (AnimationElement e in elements)
            {
                Mock.Get<AnimationElement>(e).Verify(x => x.Play(element.Target));   
            }

        }

        [TestMethod]
        public void OrSequenceElement_PlaysOneElementAtRandom()
        {
            //arrange
            SequenceElement element = Factory.SequenceElementUnderTestWithMockChildren;

            //act
            element.Type = SequenceType.Or; 
            element.Play();

            //assert

            List<AnimationElement> elements = element.AnimationElements;

            //verify only one element called;
            int timesCalled = 0;
            foreach (AnimationElement e in elements)
            {
                try
                {
                    Mock.Get<AnimationElement>(e).Verify(x => x.Play(element.Target));
                    timesCalled++;
                }
                catch
                {

                }
            }

            Assert.AreEqual(1, timesCalled);
        }

        [TestMethod]
        public void MoveElementinSequence_AllOrdersUpdatedCorrectly()
        {
            //arrange
            SequenceElement element = Factory.SequenceElementUnderTestWithMockChildren;
            AnimationElement toInsert = element.AnimationElements[1];
            AnimationElement insertAfter = element.AnimationElements[2];

            int insertAfterOrder = insertAfter.Order;
            int maxOrder = element.AnimationElements.Last().Order;
            int toInsertOrder = toInsert.Order;

            //act
            element.InsertAnimationElementAfter(toInsert, insertAfter);


            //assert
            Assert.AreEqual(insertAfter.Order, insertAfterOrder-1);
            Assert.AreEqual(toInsert.Order, insertAfterOrder);
        }

        [TestMethod]
        public void MoveElementsAcrossTwoSequences_UpdatesParentOfItemAndUpdatesOrderOfAllItemsInBothSequence()
        {
            //arrange
            SequenceElement elementsource = Factory.SequenceElementUnderTestWithMockChildren;
            SequenceElement elementDestination = Factory.SequenceElementUnderTestWithMockChildren;

            //record source list data before move
            AnimationElement toInsert = elementsource.AnimationElements.First();
            int sourceCount = elementsource.AnimationElements.Count;
            int sourceMaxOrder = elementsource.AnimationElements.Last().Order;
            int toInsertOrder = toInsert.Order;

            //record destination list data after move
            AnimationElement insertAfter = elementDestination.AnimationElements[1];
            int destinationCount = elementDestination.AnimationElements.Count;
            int destinationMaxOrder = elementDestination.AnimationElements.Last().Order;
            int insertAfterOrder = insertAfter.Order;

            //act
            elementDestination.InsertAnimationElementAfter(toInsert, insertAfter);

            //assert

            Assert.AreEqual(sourceCount - 1, elementsource.AnimationElements.Count);
            //do all the source elements still have a valid order?
            int counter = 0;
            foreach (AnimationElement sourceElement in from element in elementsource.AnimationElements
                                                       where element.Order > toInsertOrder select element)
            {
                counter++;
                Assert.AreEqual(counter, sourceElement.Order);
            }

            Assert.AreEqual(destinationCount + 1, elementDestination.AnimationElements.Count);         
            //have all the destination elements ather the insert order moved up one?
            int newInsertAfterOrder = insertAfterOrder+1;
            foreach (AnimationElement destinationElement in from element in elementDestination.AnimationElements
                                                       where element.Order > insertAfterOrder
                                                            select element)
            {
                
                Assert.AreEqual(newInsertAfterOrder, destinationElement.Order);
                newInsertAfterOrder++;
            }
            Assert.AreEqual(destinationCount + 1, elementDestination.AnimationElements.Count);
        }

        [TestMethod]
        public void RemoveElement_UpdatesOrderCorrectly()
        {
            //arrange
            SequenceElement element = Factory.SequenceElementUnderTestWithMockChildren;
            AnimationElement toRemove = element.AnimationElements[1];
            int orderOfElementAfterRemove = element.AnimationElements[2].Order;

            int countBeforeRemove = element.AnimationElements.Count;
            int maxOrderBeforeRemove = element.AnimationElements.Last().Order;

            //act
            element.RemoveAnimationElement(toRemove);


            //assert
            Assert.AreEqual(countBeforeRemove -1, element.AnimationElements.Count);

            //have all the source elements order breen decremented where after the removed item?
            int counter = orderOfElementAfterRemove-1;
            foreach (AnimationElement sourceElement in from srcelement in element.AnimationElements
                                                       where srcelement.Order >= orderOfElementAfterRemove
                                                       select srcelement)
            {
                counter++;
                Assert.AreEqual(counter, sourceElement.Order);
            }
        }
    }

    //to do
    class ColorElementTestSuite
    {
        public void ColorElement_UpdatesColorOfCostumeFileCorrectly()
        { }

        public void ColorElement_LoadsCorrectCostumefile()
        { }
        public void PersistentColorElement_DoesNotGetRemovedIfOtherFXLoaded() { }

        public void PersistentColorElement_IsRemovedFromCotumeFileWhenTurnedOff() { }

    }

    [TestClass]
    public class AnimatedAbilityTestSuite
    {
        public FakeAnimatedAbilityFactory Factory = new FakeAnimatedAbilityFactory(new FakeCrowdFactory(new FakeManagedCustomerFactory(new FakeDesktopFactory())));

        [TestMethod]
        public void PlayPersistentAbility_AddsCharacterStateWithAssociatedAbilityToCharacter()
        {
            //arrange
            AnimatedAbility ability = Factory.AnimatedAbilityUnderTestWithPersistentElements;

            //act
            ability.Play();

            //assert
            AnimatableCharacterState state = ability.Target.ActiveStates.First();
            Assert.IsNotNull(state);
            Assert.AreEqual(state.StateName, ability.Name);
            Assert.AreEqual(state.Ability, ability);
        }


        [TestMethod]
        public void StoppingPersistentAbility_RemovesStateFromCharacter()
        {
            //arrange
            AnimatedAbility ability = Factory.AnimatedAbilityUnderTestWithPersistentElements;

            //act
            ability.Play();
            ability.Stop();
            //assert
            AnimatableCharacterState state = ability.Target.ActiveStates.FirstOrDefault();

            //arrange
            Assert.IsNull(state);
        }

        [TestMethod]
        public void StoppingPersistentAbility_RemovesExistingPersistentAbilities()
        {
            //arrange
            AnimatedAbility ability = Factory.AnimatedAbilityUnderTestWithPersistentElements;

            //act
            ability.Play();
            ability.Stop();

            //assert
            foreach (AnimationElement e in ability.AnimationElements)
            {
                Mock.Get<AnimationElement>(e).Verify(x => x.Stop(ability.Target));
            }
        }

        //to do
        public void PlayOnMultipleTargets_PlaysElementOnEachTarget() { }
        public void StopOnMultipleTargets_PlaysElementOnEachTarget() { }
        public void StoppingORPersistentAbilityAlreadyPlayedOnTarget_StopsThePreviousElementThatWasSelectedTheLastTimeItWasPlayed() { }
        public void StoppingORPersistentAbility_StopsTheElementThatWasSelectedOnTheLastPlay() { }


    }
    public class FakeAnimatedAbilityFactory
    {
        public FakeCrowdFactory CrowdFactory;
        public IFixture MockFixture;
        public IFixture CustomizedMockFixture;
        public IFixture StandardizedFixture;
       
        public FakeAnimatedAbilityFactory(FakeCrowdFactory factory)
        {
            CrowdFactory = factory;
            MockFixture = factory.MockFixture;
            CustomizedMockFixture = factory.CustomizedMockFixture;
            StandardizedFixture = factory.StandardizedFixture;
            setupStandardFixture();

        }
        private void setupStandardFixture()
        {

            //map all interfaces to classes
           
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(AnimatedCharacter),
                        typeof(AnimatedCharacterImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(MovElement),
                        typeof(MovElementImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(FXElement),
                        typeof(FXElementImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(SoundElement),
                        typeof(SoundElementImpl)));
            StandardizedFixture.Customizations.Add(
                new TypeRelay(
                    typeof(SequenceElement),
                        typeof(SequenceElementImpl)));
            StandardizedFixture.Customizations.Add(
               new TypeRelay(
                   typeof(AnimatedAbility),
                       typeof(AnimatedAbilityImpl)));

            StandardizedFixture.Customize<AnimatedCharacter>(c => c
                .Without(x => x.ActiveAttackCycle)
                
                );
        }


        public List<AnimatedCharacter> MockAnimatedCharacterList
        {
            get
            {
                return CustomizedMockFixture.CreateMany<AnimatedCharacter>().ToList();
            }
        }
        public AnimatedCharacter MockAnimatedCharacter
        {
            get
            {
                {
                    return CustomizedMockFixture.Create<AnimatedCharacter>();
                }
            }
        }
        public AnimatedCharacter AnimatedCharacterUndertestWithIdentities
        {
            get
            {
                AnimatedCharacter a =  StandardizedFixture.Create<AnimatedCharacterImpl>();
                List<Identity> i = StandardizedFixture.CreateMany<Identity>().ToList();
                a.Identities.AddMany(i);
                return a;

            }
        }
        public AnimatedCharacter AnimatedCharacterUnderTest
        {
            get
            {
                AnimatedCharacter a = StandardizedFixture.Create<AnimatedCharacterImpl>();
                return a;

            }
        }
        public AnimationElement FakeAnimationElementUnderTest
        {
            get
            {
                return new FakeAnimatedElement(MockAnimatedCharacter);
            }
        }
        public List<AnimationElement> MockAnimationElementList
        {
            get
            {
                return CustomizedMockFixture.CreateMany<AnimationElement>(4).ToList();
            }
        }
        public MovElement MovElementUnderTest
        {
            get
            {
                MovElement mov = StandardizedFixture.Build<MovElementImpl>()
                    .With(x => x.Target, MockAnimatedCharacter)
                    .With(x => x.Mov, MockMovResource)
                    .With(x => x.ParentSequence , MockAnimatedAbility).Create();
                return mov;
            }
        }
        public MovResource MockMovResource
        {
            get
            {
                return CustomizedMockFixture.Create<MovResource>();
            }
        }
        public AnimatedAbility MockAnimatedAbility
        {
            get
            {
                return CustomizedMockFixture.Create<AnimatedAbility>();
            }
        }

        public FXElement FXElementUnderTestWithMockAnimatedCharacter
        {
            get
            {
                FXElement fx = StandardizedFixture.Build<FXElementImpl>()
                   .With(x => x.Target, MockAnimatedCharacter)
                   .With(x => x.FX, MockFXResource)
                   .With(x => x.ParentSequence, MockAnimatedAbility)
                   .Without(x => x.Color1)
                   .Without(x => x.Color2)
                   .Without(x => x.Color3)
                   .Without(x => x.Color4)
                   .Without(x => x.AttackDirection)
                   .Create();
                return fx;

            }
        }
        public FXElement FXElementUnderTestWithAnimatedCharacter
        {
            get
            {
                FXElement fx = StandardizedFixture.Build<FXElementImpl>()
                   .With(x => x.Target, AnimatedCharacterUndertestWithIdentities)
                   .With(x => x.FX, MockFXResource)
                   .With(x => x.ParentSequence, MockAnimatedAbility)
                   .Without(x => x.Color1)
                   .Without(x => x.Color2)
                   .Without(x => x.Color3)
                   .Without(x => x.Color4)
                   .Without(x => x.AttackDirection)
                   .Create();
                return fx;

            }
        }
        public object MockFXResource
        {
            get
            {
                return CustomizedMockFixture.Create<FXResource>();
            }
        }


        public SoundElement SoundElementUnderTest {
            get
            {
                SoundElement s = StandardizedFixture.Build<SoundElementImpl>()
                    .With(x => x.Target, AnimatedCharacterUndertestWithIdentities)
                    .With(x => x.SoundEngine, MockSoundEngine)
                    .With(x => x.Sound, MockSoundResource)
                    .With(x => x.ParentSequence, MockAnimatedAbility)              
                    .Create();
                return s;
            }
        }
        public object MockSoundResource
        {
            get
            {
                return CustomizedMockFixture.Create<SoundResource>();
            }
        }
        public SoundEngineWrapper MockSoundEngine
        {
            get
            {
                
                return CustomizedMockFixture.Create<SoundEngineWrapper>();
            }

        }

        public PauseElement PauseElementUnderTest {

            get
            {
                PauseElement p = StandardizedFixture.Build<PauseElementImpl>()
                   .With(x => x.Target, AnimatedCharacterUndertestWithIdentities)
                   .With(x => x.ParentSequence, MockAnimatedAbility)
                   .With(x=>x.Duration , 100)
                   .Create();

                return p;
            }
        }
        public SequenceElement SequenceElementUnderTestWithMockChildren {
            get
            {
                SequenceElement element = StandardizedFixture.Build<SequenceElementImpl>()
                    
                   .With(x => x.Target, MockAnimatedCharacter)
                   .With(x => x.ParentSequence, MockAnimatedAbility)
                   .Create();

                List<AnimationElement> list = MockAnimationElementList;
                foreach (AnimationElement e in list)
                {
                    element.InsertAnimationElement(e);
                }
                return element;
            }
        }

        public AnimatedAbility AnimatedAbilityUnderTestWithPersistentElements
        {
            get
            {
                AnimatedAbility ability = StandardizedFixture.Build<AnimatedAbilityImpl>()
                  .With(x => x.Target, AnimatedCharacterUnderTest)
                  .With(x=>x.Persistent , true)
                  .Without(x => x.Owner)
                  .Create();

                List<AnimationElement> list = MockAnimationElementList;
                foreach (AnimationElement e in list)
                {
                    ability.InsertAnimationElement(e);
                    e.Persistent = true;
                }
                return ability;
            }
        }
        public class FakeAnimatedElement : AnimationElementImpl
        {
            public FakeAnimatedElement(AnimatedCharacter owner) : base(owner)
            { }
            public override void PlayResource(AnimatedCharacter target)
            {
                target.AddState(null, true);
            }

            public override void StopResource(AnimatedCharacter target)
            {
               
            }
        }
    }
}