using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVTTests.Classes
{
    [TestClass]
    public class MoveSingleCharacterTest
    {
        [TestMethod]
        public void MoveCharacterADirectionAndDistance_UpdatesCharacterPositionCorrectly()
        {
        }

        public void IncrementCharacterADirection_UpdatesPositionIncrementAMount()
        {
        }

        public void IncrementingFacing_UpdatesRotationMatrixToResultInCorrectFacing()
        {

        }

        public void IncrementingFacingAndIncrementingADirection_UpdatesPositionIncrementAmountBasedOnFacing()
        {

        }

        public void MoveCharacterToADestination_UpdatesCharacterFacingTowardsDestinationAndUpdatesPositionCorrectly()
        {
        }


        public void TryingToDescendCharacterBelowFloor_CharacterDoesNotMoveBelowGroundLevel()
        {
        }

        public void SetCustomFLoorToCameraPositionAndTryingDescendCharacterBelowFloor_CharacterDoesNotMoveBelowCustomGroundLevel()
        {
        }

        public void MoveActiveCharacterToCamera_UpdatesCharacterFacingToCameraAndMovesCharacterUntilNextToCameraLocation()
        {

        }

        public void MoveActiveCharacterToTarget_UpdatesCharacterFacingToTargetAndMovesCharacterUntilNextToTargetLocation()
        {

        }



    }
    public class ChangeCharacterMovementTest
    {
        public void AssignMovement_IsAddedToCharacter()
        {

        }

        public void RemoveMovement_IsRemovedFromCharacter()
        {

        }

        public void SpecifyDefaultMovementAndMoveCharacter_SetsActiveMovementToDefaultIfNoActiveMovement()
        {
        }

        public void MoveCharacter_MovesWithActiveMovement()
        {

        }
    }

    public class ChangeMovementTest {
        public void AssignAnimatedAbilityToMovementAndMovingCharacter_RunsAssociatedAnimatedAbility() {
        }

        public void AddMovement_SavesToMovementRepository()
        {

        }

        public void RemoveMovement_RemovesFromMovementRepository()
        {

        }


    }

    public class MoveCrowdTest {
        public void MoveCrowdADirectionAndDistance_UpdatesAllCharactersInCrowdToFacingOfFirstCharacterInCrowdAndUpdatesAllCrowdMembersPositionCorrectly()
        {

        }

        public void MoveCrowdToCamera_UpdatesCharacterFacingToCameraAndMovesCrowdUntilNextToCameraLocation()
        {

        }

        public void MoveCrowdToTarget_UpdatesCharacterFacingToTargetAndMovesCrowdUntilNextToTargetLocation()
        {

        }

        public void MoveCrowdADirection_UsesUniqueAnimationsForEachCharacterBasedOnActiveMovements()
        {

        }

        public void IncrementCrowdDirection_UpdatesPositionOfAllCrowdMemebersIncrementAMount()
        {
        }

        public void IncrementingCrowdFacing_UpdatesRotationMatrixOfAllCrowdMembersToSameFacingAndResultInCorrectFacingForAllCrowdMembers()
        {

        }

    }

    public class MoveRosterTest
    {
        public void MoveSelectedRosterEntriesADirectionAndDistance_UpdatesAllEntriesToFacingOfFirstCharacterInActivatedAndSelectedCharacterInRosterAndUpdatesAllEntriesPositionCorrectly()
        {

        }

        public void MoveSelectedRosterEntriesToCamera_UpdatesCharacterFacingToCameraAndMovesEntryUntilTheyAreNextToCameraLocation()
        {

        }

        public void MoveSelectedRosterEntriesToTarget_UpdatesCharacterFacingToTargetAndMovesEntriesUntilTheyAreNextToTargetLocation()
        {

        }

        public void MoveSelectedRosterEntriesADirection_UsesUniqueAnimationsForEachCharacterBasedOnActiveMovements()
        {

        }

        public void IncrementSelectedRosterEntriesDirection_UpdatesPositionOfAllEntriesIncrementAMount()
        {
        }

        public void IncrementingSelectedRosterEntriesFacing_UpdatesRotationMatrixOfAllEntriesToSameFacingAndResultInCorrectFacingForAll()
        {

        }

    }








}
}
