#Include String-object-file.ahk
#Include CommandParser.ahk
#Include CharacterManager.ahk
#Include CrowdManager.ahk
#Include AnimationManager.ahk
#Include Logger.ahk
#include TargetedModel.ahk
#include Movement.ahk



class FileListeningEventHandler
{
	animationManager:=""
	characterManager:=""
	crowdManager:=""
	moveInterface:=""
	directory:="EventInfo\\"
	Running:=false
	GetInstance(){
		if (this._instance==""){
			this._instance:= new FileListeningEventHandler()
		}
		return this._instance
	}
	
	__New(){
		this.characterManager:= CharacterManager.GetInstance()
		this.animationManager:= AnimationManager.GetInstance()
		this.crowdManager:= CrowdManager.GetInstance()
		this.MoveInterface:= MoveInterface.GetInstance()
	}
	
	EventMap:= {"Play Ability":{manager:"AnimationManager", method:"PlayAbilityCycle"}
		, "Play Other":{manager:"AnimationManager", method:"PlayAbilityBasedOnCharInfo"}
		, "Remove Effects":{manager:"AnimationManager", method:"RemoveEffectsEvent"}
		, "Play Mov":{manager:"AnimationManager", method:"PlayStructuredMov"}
		, "Play Provided Ability":{manager:"AnimationManager", method:"PlayStructuredMovBasedOnCharacterInfo"}
		
		, "Target":{manager:"CharacterManager", method:"TargetCharacterBasedOnInfo"}
		, "Activate Character":{manager:"CharacterManager", method:"TargetAndFollowBasedOnInfo"}
		, "Node Double CLick":{manager:"CharacterManager", method:"TargetAndFollowBasedOnCharacterInfo"}
		, "Target Active Character":{manager:"CharacterManager", method:"TargetCharacterBasedOnInfo"}
		, "Expand Character Node":{manager:"CharacterManager", method:"TargetCharacterBasedOnInfoBasedOnInfo"}
		, "LoadCostume":{manager:"CharacterManager", method:"LoadCostumeBasedOnInfo"}
		
		, "Spawn Character":{manager:"CharacterManager", method:"SpawnCharacterBasedOnInfoToDesktop"}
		, "Open Character":{manager:"CharacterManager", method:"SpawnCharacterBasedOnInfoToDesktop"}
		, "New Character":{manager:"CharacterManager", method:"SpawnCharacterBasedOnInfoToDesktop"}
		, "Spawn Model For Active Character":{manager:"CharacterManager", method:"SpawnCharacterBasedOnInfo"}
		
		, "Manuever with Camera":{manager:"CharacterManager", method:"TurnCameraIntoCharacterBasedOnInfo"}
		
		, "Show Camera":{manager:"CharacterManager", method:"TurnCharacterIntoCamera"}
		, "End Manuever With Camera":{manager:"CharacterManager", method:"TurnCharacterIntoCameraBasedOnInfo"}
		
		, "Move To Camera":{manager:"CharacterManager", method:"JumpModelToCameraBasedOnInfo"}
		, "Jump to Camera":{manager:"CharacterManager", method:"JumpModelToCameraBasedOnInfo"}
		, "CharacterRenamed":{manager:"CharacterManager", method:"RenameCharacterBasedOnInfo"}
		, "RosterRemove":{manager:"CharacterManager", method:"DeleteModel"}
		, "Delete Character":{manager:"CharacterManager", method:"DeleteModel"}
		
		, "Move To Target":{manager:"CharacterManager", method:"MoveToTargetBasedOnInfo"}
		
		, "Move Mode":{manager:"MoveInterface", method:"ActivateMoveModeForSingleCharacterBasedOnInfo"}
		
		, "Place Crowd":{manager:"CrowdManager", method:"LoadModelsForCrowdBasedOnInfo" , crowdMode:true}
		, "Open Roster":{manager:"CrowdManager", method:"LoadModelsForCrowdBasedOnInfo", crowdMode:true}
		, "Collapse Roster Node":{manager:"CrowdManager", method:"LoadModelsForCrowdBasedOnInfo", crowdMode:true}
		, "MobMode":{manager:"CrowdManager", method:"LoadModelsForCrowdBasedOnInfo", crowdMode:true}
		
		, "Place Crowd":{manager:"CrowdManager", method:"SpawnAndPlaceNextModelFromCrowd",crowdMode:true}
		, "Place Character":{manager:"CrowdManager", method:"PlaceCharacterBasedOnInfo",crowdMode:false}
		, "Save Location":{manager:"CrowdManager", method:"SaveLocationBasedOnCharacterInfo",crowdMode:false }}
	
	
	
	ListenForAndHandleFileEvent(){
		WinGetActiveTitle, current
		
		event:= this.loadEventFromFileSystem()
		if(event == ""){
			this.deleteEventFile()
			return
		}
		eventMapEntry:= this.EventMap[event.Name]
		if(eventMapEntry.manager=="CharacterManager"){
			this.HandleCharacterEvent(eventMapEntry.Method,event.Character)
		}
		if(eventMapEntry.manager=="AnimationManager"){
			this.HandleAnimationEvent(eventMapEntry.Method,event.Animation, event.Character)
		}
		if(eventMapEntry.manager=="CrowdManager"){
			this.HandleCrowdEvent(eventMapEntry.Method, event.Crowd, eventMapEntry.CrowdMode, event.Character)
		}
		if(eventMapEntry.manager=="MoveInterface"){
			this.HandleMoveEvent(eventMapEntry.Method,event.Character,event.MovementTriggerKey)
		}
		this.characterManager.CommandParser.MarkCommandComplete()
		this.deleteEventFile()
		
		if(event.ActivateCallingApplication== "True"){
			WinActivate %current%
		}
	}
	loadEventFromFileSystem(){
		event:= this.LoadFromFile("Ability")
		if(event.Character.Name==""){
			name:=event.Character
			event.Character:={name:name}
		}
		if(event.Ability<>""){
			event.Name:="Play Ability"
			if(event.Ability <> ""){
				event.Animation:=event.Ability
			}
			if(event.Animation.Name==""){
				name:=event.Animation
				event.Animation:={name:name}
			}
			if(event.target<>""){
				;event.Animation:=event.Ability
				event.Animation.Ability:={name:event.Animation.Name}
				event.Animation.Target:= event.target
				if(event.Animation.Target.Name==""){
					event.Animation.Target:={name:event.Animation.Target}
				}
				event.Animation.Result:=event.Result
				event.Animation.Effects:=event.Effects
				event.Animation.Knockbac:=event.Knockbac
			}
			else{
				if(event.IndividualAttack <>""){
					;event.Animation:={}
					event.Animation.Ability:=event.Animation
					event.Animation.Knockbac:= event.Knockbac
					event.Animation.IndividualAttack:=event.IndividualAttack
					event.Animation.Type:=event.Type
				}	
			}
			return event
		}
		else{
			if(event.MiscItem == "True"){
				event.Name:="Play Other"
				event.Animation:=event.Item
				name:=event.Character
				event.Character:={Name:Name}
				return event
			}
		}
		event:= this.LoadFromFile("Activity")
		if(event.Activate.Crowd<>""){
			event.Character.Crowd:={Name: event.Activate.Crowd.Name}
			event.Name:="Activate Character"
			return event
		}
		event:=this.LoadFromFile("HCSEvent.info")
		if(event.character.name == "Generic Object"){
			return ""
		}
		if (event.Event<> ""){
			event.Name:= event.Event.Name
			if(event.Name ==""){
				event.Name:= event.Event
			}
			if(Event.event.character<>""){
				event.Character:=event.Event.Character
			}
			if(event.Character.Name==""){
				name:=event.Character
				event.Character:={name:name}
			}
				
			if(event.Movement.TriggerKey<>""){
				event.MovementTriggerKey:=event.Movement.TriggerKey
			}
			if(event.Event.Mov <> ""){
				event.Animation:=event.Event.Mov
			}
			if(event.Mov <> ""){
				event.Animation:=event.Mov
			}
			if(event.Event.Crowd <>""){
				event.Crowd:=event.Event.Crowd 
			}
			if(event.character.Roster <>""){
				event.Crowd:=event.character.Roster
			}
			
			return event
		}
		event:=this.LoadFromFile("DesktopAction")
		if(event.ActionSent=="True"){
			event.Name:= event.Action
			return event
		}
		event:=this.LoadFromFile("ExecuteAnimation")
		if(event.Sequence <>""){
			event.Character:= {name:event.Character}
			event.Animation:= event
			event.Name:= "Play Provided Ability"
			return event
		}
	}
	loadFromFile(file){
		this.path:=this.directory . file . ".info"
		return StrObj(this.path) 
	}
	

	deleteEventFile(){
		FileDelete % this.directory . "HCSEvent.info.info"
		FileDelete % this.directory . "Ability.info"
		FileDelete % this.directory . "Activity.info"
		FileDelete % this.directory . "DesktopAction.info"
		FileDelete % this.directory . "ExecuteAnimation.info"
}
	HandleCharacterEvent(method, characterInfo){
		this.Running:=true
		SoundPlay sound\N_CharPageScroll.wav
		m:=this.characterManager
		m[method](characterInfo)
		this.Running:=false
	}
	HandleCrowdEvent(method, crowdInfo, crowdMode, characterInfo=""){
		this.Running:=true
		SoundPlay sound\N_CharPageScroll.wav
		m:=this.crowdManager
		m.CrowdMode:=crowdMode
		m.LoadModelsForCrowd(crowdInfo.Name)
		if(method <>"LoadModelsForCrowd"){
			if(characterInfo==""){
				m[method](crowdInfo)
			}
			else
			{
				m[method](characterInfo)
			}
		}
		this.Running:=false
	}
	HandleAnimationEvent(method, animation, characterInfo=""){
		this.Running:=true
		m:=this.animationManager
		if(characterInfo<>""){
			m[method](animation , characterInfo)
		}
		else
		{
			m[method](animation)
		}
		this.Running:=false
	}
	HandleMoveEvent(method, character,triggerKey){
		this.Running:=true
		m:=this.MoveInterface
		m[method](character, triggerKey)
		this.Running:=false
	}	
	
}


	