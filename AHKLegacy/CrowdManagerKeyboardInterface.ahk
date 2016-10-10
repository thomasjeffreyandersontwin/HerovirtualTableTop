#SingleInstance forcev
#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahk
#Include Yunit\StdOut.ahk

#Include CrowdManager.ahk
#Include String-object-file.ahk
#Include TargetedModel.ahk


class CrowdKeyboardInterface{
	Path:=""
	Directory:=""
	static Interface:=""
	HandlingBuildInput:=false
	HandlingPlacementInput:=false
	CrowdManager:=""
	GetInstance(){
	
			interface:= new CrowdKeyboardInterface
			interface.CrowdManager:= CrowdManager.GetInstance()
			interface.Directory:=interface.CrowdManager.Directory
			return interface
	}

	GrabText(){
		act:=WinActive("ahk_class ahk_class CrypticWindow")
		if (act==0){
			Send ^c
			sleep 1000
			var:=Clipboard
			var:=RegExReplace(var, "\r\n$","")
			return var
		}
		return ""
	}
	HandleCrowdBuildingKeyboardsInput(){
		if(this.HandlingBuildInput== true){
			Loop{
				SoundPlay sound\N_CharPageScroll.wav
				Input key, I L1
				
				If(key==""){	
					this.HandlingBuildInput:= false
					SoundPlay sound\N_Undo.wav
					break
				}
				if (key=="s")
				{
					SoundPlay sound\N_Select.wav
					crowd:=this.GrabText()
					this.CrowdManager.LoadModelsForCrowd(crowd)
					continue
				}
				if (key=="m")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					Model:=this.GrabText()
					costume:=""
					this.CrowdManager.SpawnNewCharacterAndAddToCurrentCrowd(model,costume)
					WinActivate %current%
					continue
				}
				if (key=="c")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					costume:=this.GrabText()
					model:=""
					this.CrowdManager.SpawnNewCharacterAndAddToCurrentCrowd(model,costume)
					WinActivate %current%
					continue
				}
				if(key=="t")
				{	
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					this.CrowdManager.AddTargetedCharacterToCrowd()
					WinActivate %current%
					continue
				}
				SoundPlay sound\N_Error.wavn
			}
		}
	}
	
	ToggleCrowdMode(){
		
		if(this.crowdManager.CrowdMode== true){
			SoundPlay sound\N_Undo.wav
			this.crowdManager.CrowdMode:=false
		}
		else{
			SoundPlay sound\N_Select.wav
			this.crowdManager.CrowdMode:=true
		}
		return
	}
	
	HandleCrowdPlacingKeyboardsInput(){
		if(this.HandlingPlacementInput==true){
			SoundPlay sound\N_MenuExpand.wav
			Loop{
				Input key, I L1 V
				If(key==""){	
					this.HandlingPlacementInput:=false
					SoundPlay sound\N_Undo.wav
					break
				}
				if (key=="v")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					this.CrowdManager.SaveLocationOfSelectedModel()
					WinActivate %current%l
					continue
				}
				if (key=="l")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					this.CrowdManager.SpawnAndPlaceNextModelFromCrowd()
					WinActivate %current%
					continue

				}
				if (key=="n")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					this.CrowdManager.SpawnNextModelFromCrowd()
					WinActivate %current%
					continue

				}
				
				if (key=="c")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					this.CrowdManager.SpawnCloneOfLastSpawnedCharacterAndAddToCurrentCrowd()
					this.CrowdManager.WriteToFile()
					WinActivate %current%
					continue
				}
			}
		}	
	}
}



