using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.Shared.Events;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Module.UnitTest.AnimatedAbilities
{
    [TestClass]
    public class AnimatedAbilityTestSuite : BaseTest
    {
        private AbilityEditorViewModel abilityEditorViewModel;
        protected Mock<IResourceRepository> resourceRepositoryMock = new Mock<IResourceRepository>();
        private CrowdMemberModel character;
        [TestInitialize]
        public void TestInitialize()
        {
            abilityEditorViewModel = new AbilityEditorViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, resourceRepositoryMock.Object, eventAggregatorMock.Object);
            character = new CrowdMemberModel("Spyder");
            //this.abilityEditorViewModel.CurrentAbility = new AnimatedAbility("Ability");
            this.abilityEditorViewModel.Owner = character;

            this.abilityEditorViewModel.AnimationAdded += (delegate(object state, CustomEventArgs<bool> e) 
            { 
                this.abilityEditorViewModel.SelectedAnimationElement = state as IAnimationElement;
            });
        }

        [TestMethod]
        public void AddAnimationElement_AddsAnimationToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.CurrentAbility.AnimationElements != null && this.abilityEditorViewModel.CurrentAbility.AnimationElements.Count == 1);
        }
        [TestMethod]
        public void AddAnimationElement_AddsAnimationWithProperNumberSuffix()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
            // and so on...
        }
        [TestMethod]
        public void AddAnimationElement_AddsAnimationWithProperType()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is MOVElement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX); 
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is FXEffectElement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is SoundElement);
            // and so on...
        }
        [TestMethod]
        public void AddAnimationElement_AddsAnimationWithProperOrder()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 1);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 2);
            this.abilityEditorViewModel.SelectedAnimationElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements["Mov Element 1"];
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 2);
        }
        [TestMethod]
        public void RemoveAnimationElement_RemovesAnimationElement()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements["Mov Element 1"];
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            var deletedElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements.Where(a => a.Name == "Mov Element 1").FirstOrDefault();
            Assert.IsNull(deletedElement);
        }
        [TestMethod]
        public void RemoveAnimationElement_UpdatesOrder()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements["Mov Element 1"];
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            var updatedElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements.Where(a => a.Name == "FX Element 1").FirstOrDefault();
            Assert.AreEqual(updatedElement.Order, 1);
        }
        [TestMethod]
        public void AssignSequenceToAbility_AddsSequenceElement()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sequence);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is SequenceElement);
        }
        [TestMethod]
        public void AssignSequenceToAbility_AddsSequenceElementWithDefaultSequenceAnd()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sequence);
            Assert.IsTrue((this.abilityEditorViewModel.SelectedAnimationElement as SequenceElement).SequenceType == AnimationSequenceType.And);
        }
        [TestMethod]
        public void AddAnimationElementToParentSequence_NestsAnimationInParent()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 1);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements.Count == 1);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements[0].Name == "Mov Element 2");
        }
        [TestMethod]
        public void AddAnimationElementToParentSequence_AddsChildrenInProperOrder()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);           
            this.abilityEditorViewModel.SelectedAnimationParent = (this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = (this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements[0];
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements["Sound Element 1"].Order == 2);
        }
        [TestMethod]
        public void RemoveAnimationElementFromParentSequence_RemovesAnimationElementFromParent()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            this.abilityEditorViewModel.SelectedAnimationParent = (this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements.Count == 1);
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements.Count == 0);
        }
        [TestMethod]
        public void RemoveAnimationElementFromParentSequence_UpdatesOrderInNestedElements()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            this.abilityEditorViewModel.SelectedAnimationParent = (this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements["Sound Element 1"].Order == 3);
            this.abilityEditorViewModel.SelectedAnimationElement = (this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements["FX Element 1"];
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            Assert.IsTrue((this.abilityEditorViewModel.CurrentAbility.AnimationElements["Seq Element 1"] as SequenceElement).AnimationElements["Sound Element 1"].Order == 2);
        }
        [TestMethod]
        public void AssignPauseElementToAbility_AddsPauseElement()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Pause);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is PauseElement);
        }
        [TestMethod]
        public void AssignPauseElementToAbility_SetsDefaultPauseTimeToOne()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Pause);
            Assert.IsTrue((this.abilityEditorViewModel.SelectedAnimationElement as PauseElement).Time == 1);
        }
        [TestMethod]
        public void AssignMovToAbility_AddsMovElementToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Type == AnimationType.Movement);
        }
        [TestMethod]
        public void AssignMovToAbility_AddsMovWithCorrectName()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
        }
        [TestMethod]
        public void AssignFXToAbility_AddsFXElementToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Type == AnimationType.FX);
        }
        [TestMethod]
        public void AssignFXToAbility_AddsFXWithCorrectName()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
        }
        [TestMethod]
        public void AssignSoundToAbility_AddsSoundElementToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Type == AnimationType.Sound);
        }
        [TestMethod]
        public void AssignSoundToAbility_AddsSoundWithCorrectName()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
        }

    }
}
