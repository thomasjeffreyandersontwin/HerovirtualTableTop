using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnimatedCharacter
{
    [TestClass]
    public class AddEditDeleteAnimatedAbility
    {
        [TestMethod]
        public void AddAbility_AddsToAnimatedCharcter()
        {
        }

        [TestMethod]
        public void EditAbility_UpdatesAnimation()
        {
        }

        [TestMethod]
        public void DeleteAbility_RemovesAnimationFromCharacter()
        {
        }
        [TestMethod]
        public void AddAnimationElement_AddsToAbility()
        {
        }
        [TestMethod]
        public void RemoveAnimationElement_AddsToAbility()
        {
        }
        public void PlayAnimationWithMultipleElements_AnimatesElementsInOrder()
        {
        }
    }

    [TestClass]
    public class ReferenceAnimationElementTest
    {
        public void Filter_ReturnsFilteredListOfReferences()
        {
        }

        public void Browse_ReturnsAllAnimations()
        {
        }

        public void AssignReftoAbilityAndAnimate_RunsReferencedAnimation()
        {
        }

        public void CopyReftoAbility_AssignsSequenceAnimationToCharacter()
        {
        }

        public void LinkReftoAbilityAndChangeReferencedAnimationAndAnimate_RunsChangedReferenceAnimation()
        {
        }
    }

    [TestClass]
    public class MovAnimationElementTest
    {

        public void Filter_ReturnsFilteredListOfMovs()
        {
        }

        public void Browse_ReturnsAllMovs()
        {
        }

        public void ChangeRecord_GeneratesTargetAndMovKeybind()
        {
        }

        public void AssignMovetoAbilityAndAnimate_GeneratesTargetAndMovKeybind()
        {
        }
    }

    [TestClass]
    public class SoundAnimationElementTest
    {

        public void Filter_ReturnsFilteredListOfSounds()
        {
        }

        public void Browse_ReturnsAllSOundFilesInSoundDirectory()
        {
        }

        public void ChangeRecord_PlaysSound()
        {
        }

        public void AssignSoundtoAbilityAndAnimate_GeneratesTargetKeybindAndPlaysSoundFile()
        {
        }

        public void AssignPersistentAbilityWithSoundAndAnimate_GeneratesTargetKeybindAndPlaysSoundFileInLoop()
        {
        }
        public void ClearPersistenAbilityWithSoundAndAnimate_StopsLoopingSoundFile()
        {
        }
        public void MovingCameraAwayFromCharacterWithPeristentSound_ReducesVolumeOfLoopingSound()
        {
        }
        public void MovingCameraTowardsFromCharacterWithPeristentSound_IncreasesVolumeOfLoopingSound()
        {
        }

    }

    [TestClass]
    public class FXAnimationElementTest
    {

        public void Filter_ReturnsFilteredListOfFX()
        {
        }

        public void Browse_ReturnsAllFXEntries()
        {
        }

        public void ChangeRecord_CreatesNewCostumeFileAndInsertsFXIntoCostumeFileAndGeneratesTargetAndLoadCostumeKeybind()
        {
        }

        public void AssignSoundtoAbilityAndAnimate_CreatesNewCostumeFileAndInsertsFXIntoCostumeFileAndGeneratesTargetAndLoadCostumeKeybind()
        {
        }

        public void AssignPersisteAbilityWithFXAndAnimate_ArchivesCostumeFileInsertsFXIntoOriginalCostumeFileAndGeneratesTargetAndLoadCostumeKeybind()
        {
        }

        public void ClearPersistenAbilityWithFXAndAnimate_CopiesArchiveCostumeFileToOriginalAndGeneratesTargetAndLoadCostumeKeybind()
        {
        }

        public void ChangeColorOfFxAbilitie_InsertsColorChangesIntoCOstumeFile()
        {
        }
    }

    public class PauseElementTest
    {
        public void AssignPausetoAbilityAndAnimate_PausesExecutionOfAbilityCorrectTime()
        {
        }
    }

    public class PlayWithNextElementTest
    {
        public void AssignPlayWithNextToAbilityElementAndAnimate_GeneratesKeybindsForBothElementsAndFiresTogether()
        {
        }
    }

    public class SequenceElementTest
    {
        public void AddAnimatonToSequence_CreatesNestedElement()
        {
        }

        public void AssignSequencetoAbilityAndAnimate_AnimatesNestedElement()
        {
        }

        public void AnimateAbilityWithSequenceAndTypeWithMultipleElements_AnimatesInOrder()
        {
        }

        public void AnimateAbilityWithSequenceOrTypeWithMultipleElements_AnimatesRandomElement()
        {
        }

        public void CloneAndPasteNestedElementInSequence_CopiesElementsAcrossSequences()
        {
        }

        public void ClonePaste_ChangesOrderOfNestedElementsOfTargetSequence()
        {
        }

    }
}
