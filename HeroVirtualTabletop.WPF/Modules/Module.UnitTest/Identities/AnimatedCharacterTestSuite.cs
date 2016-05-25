using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using System.Collections;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using System.IO;
using System.Linq;
using Moq;
using System.Collections.Generic;

namespace Module.UnitTest.Identities
{
    [TestClass]
    public class AnimatedCharacterTestSuite : BaseTest
    {
        private CharacterEditorViewModel characterEditorVM;
        private CrowdMemberModel character;

        [TestInitialize]
        public void TestInitialize()
        {
            characterEditorVM = new CharacterEditorViewModel(busyServiceMock.Object, unityContainerMock.Object, eventAggregatorMock.Object);
            character = new CrowdMemberModel("Spyder");
            characterEditorVM.LoadCharacter(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(character, null));
        }

        [TestMethod]
        public void AddAbility_AddsToAnimatedCharcter()
        {
            characterEditorVM.AnimatedAbilitiesViewModel.AddOptionCommand.Execute(null);
            Assert.AreEqual(character.AnimatedAbilities.Count, 1);
        }

        [TestMethod]
        public void DeleteAbility_RemovesAnimationFromCharacter()
        {
            characterEditorVM.AnimatedAbilitiesViewModel.AddOptionCommand.Execute(null);
            characterEditorVM.AnimatedAbilitiesViewModel.SelectedOption = character.AnimatedAbilities[0];
            characterEditorVM.AnimatedAbilitiesViewModel.RemoveOptionCommand.Execute(null);
            Assert.AreEqual(character.AnimatedAbilities.Count, 0);
        }
    }
}
