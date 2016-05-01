using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedCharacter
{
    [TestClass]
    public class CharacterOptionsTest
    {
        [TestMethod]
        public void AddOptionToGroup_IsAccessibleByName()
        {
        }
        public void RemoveOptionFromGroup_RemovesGroupFromCollection()
        {
        }
        public void AddedOptionGroup_IsAccessibleByName()
        {
        }
        public void RemovedOptionGroup_RemoveOptionFromCollection()
        {
        }
    }

    [TestClass]
    public class ManagedCharacterTest
    {
        public void AddCharacter_CreatesNewCharacterInRepo (){ }
        public void DeleteCharacter_RemovesCharacterFromRepo() { }
        public void FilterCharacter_ReturnsFilteredListOfCharacters() { }
        public void RenameCharacter_UpdatesRepoCorrectly() { }
        public void CloneAndPasteCharacterAcrossCharacters_AddsNewCharacterWithUniqueSequenceNumber() { }
        public void SpawnCharacterWithNoIdentity_GeneratesSpawnKeybindUsingDefaultModelAndNameAsCostume() { }
        public void TargetCharacter_TargetsCharacterUsingMemoryInstancesIfItExists() { }
        public void TargetCharacter_GeneratesTargetKeybindIfNoMemoryInstance() { }
        public void TargetAndFollowCharacter_GeneratesTargetAndFollowKeybind() { }
        public void MoveCharacterToCamera_GeneratesTargetCharacterAndMoveNPCKeybind() { }
        public void HideCameraWithCostumedCharacter_TargetAndFollowsAndDeletesCharacterThenLoadsCharactersCostumeInCamera() { }
        public void HideCameraWithModelCharacter_TargetAndFollowsAndDeletesCharacterThenCameraBecomesNPC() { }
        public void ShowCamera_ReloadsCameraSkinOnCameraThenSpawnsCharacter() { }
    }

    public class IdentyTest
    {
        public void SpawnCharacter_WithMultipleIdentitiesGeneratesSpawnKeybindUsingSpecifiedDefaultIdentity() { }
        public void SpawnCharacter_WithIdentityThatHasAModelGeneratesSpawnKeybindUsingModel() { }
        public void SpawnCharacter_WithIdentityThatHasACostumeGeneratesSpawnKeybindUsingCostume() { }
    }

    [TestClass]
    public class CrowdMemberTest
    {
        public void AddCrowd_CreatesNewCrowdInRepo() { }
        public void AddCrowdMemberToCrowd_CreatesNewCrowdMemberInCrowd() { }
        public void DeleteCrowd_RemovesCrowdFromRepo() { }
        public void DeleteCrowdmemberFromCrowd_RemovesCrowdMemberFromCrowd() { }
        public void FilterCharacter_ReturnsFilteredListOfCrowdMemberAndCrowds() { }
        public void RenameCharacter_UpdatesRepoCorrectly() { }
        public void SpawnCharacterInCrowd_AssignsLabelWithBothCharacterAndCrowdName() { }
        public void SavePlacementOfCharacter_AssignsLocationToCrowdmembershipBasedOnCurrentPositionAndSavesCrowdmembershipToCrowdRepo() { }
        public void PlaceCharacter_MovesCharacterToPositionBasedOnSavedLocation() { }
        public void LinkAndPasteCharacterAcrossCharacters_AddsNewCrowdMemberWithCopiedCharacterToPastedCrowd() { }
        public void ExecutingSpawnOrSaveOrPlaceOrRemoveOnACrowd_ActivatesTheCommandOnAllCrowdmembersInCrowd() { }
    }

    public class RosterTest
    {
        public void SaveRoster_CreatesNestedCrowdWithRosterAndUniqueNumber() { }
        public void AddCharacterToRoster_AddsToRosterInNoCrowdSection() { }
        public void AddCharacterToRosterThatIsAlreadyInRoster_DoesNothing() { }
        public void AddCrowdmemberToRoster_CreatesCrowdTitleSectionInRosterAndAddsCharacterToRosterInSectionWithTitleOfCrowd() { }
        public void AddCrowdWithCharacterAlreadyinAnotherCrowdAddedToRoster_RaisesWarningAndDoesNotAddCHaracterTwice() { }
        public void AddMultipleSelectedCharacterExplorerEntriesToRoster_AddsAllEntries() { }
        public void AddCrowdToRoster_CreatesCrowdTitleSectionInRosterAndAddsAllCHaractersInCrowdToRosterInSectionWithTitleOfCrowd() { }
        public void RemoveCharacterFromDesktop_GeneratesTargetAndDeleteKeybindAndRemovesFromRoster() { }
        public void SingleClickingCharacterOnDesktop_SelectsCharacterInRoster() { }
        public void DoubleClickingCharacterOnDesktop_ActivatesCharacterAndSelectsInRoster() { }
        public void DoubleClickingCharacterInRoster_ActivatesCharacterAndTargetsAndFollowsInDesktop() { }
        public void SingleClickingCharacterInRoster_TargetsCharacterInDesktop() { }
        public void SelectingMultipleRosterEntriesAndExecutingSpawnOrSAveOrPlaceOrRemoveActivatesTheCommandOnAllSelectedEntries() { }
        public void AllCommands_CanBeActivatedUsingKeyboards() { }


    }
}
