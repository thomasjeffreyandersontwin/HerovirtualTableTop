#SingleInstance force
#Include TargetedModel.ahk
#include AnimationManager.ahk
#include CharacterManager.ahk
#include CrowdManager.ahk

class TargetInterface{
	CurrentTarget:=""
	characterManager:= CharacterManager.GetInstance()
	crowdManager:= CrowdManager.GetInstance()
	HandlePotentialTargetingEvent(){
		
		lastTargetedCharacter:= this.characterManager.LastTargetedCharacter
		targetedCharacter:=this.characterManager.ActivateTargetedCharacter
		if (lastTargetedCharacter.Name <> targetedCharacter.Name)
		{
			this.CrowdManager.LoadCrowdForCharacter(targetedCharacter)
		}
		return
	}
}

	