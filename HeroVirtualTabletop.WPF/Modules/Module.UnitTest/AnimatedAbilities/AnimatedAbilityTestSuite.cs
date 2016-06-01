using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.UnitTest.AnimatedAbilities
{
    [TestClass]
    public class AnimatedAbilityTestSuite : BaseTest
    {
        private AbilityEditorViewModel abilityEditorViewModel;
        private CrowdMemberModel character;
        [TestInitialize]
        public void TestInitialize()
        {
            abilityEditorViewModel = new AbilityEditorViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, eventAggregatorMock.Object);
            character = new CrowdMemberModel("Spyder");
            this.abilityEditorViewModel.CurrentAbility = new AnimatedAbility("Ability");
            this.abilityEditorViewModel.Owner = character;

            this.abilityEditorViewModel.AnimationAdded += (delegate(object state, EventArgs e) { this.abilityEditorViewModel.SelectedAnimationElement = state as IAnimationElement; });
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
    }
}
