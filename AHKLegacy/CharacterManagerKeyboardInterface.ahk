#SingleInstance force
#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahk
#Include Yunit\StdOut.ahk

#Include CrowdManager.ahk
#Include String-object-file.ahk
#Include TargetedModel.ahk


class CharacterKeyboardInterface{
	Path:=""
	Directory:=""
	static Interface:=""
	HandlingCharacterInput:=false
	characterBeingControlled:=""
	CharacterManager:=""
	GetInstance(){
	
			interface:= new CharacterKeyboardInterface
			interface.CharacterManager:= CharacterManager.GetInstance()
			interface.Directory:=interface.CharacterManager.Directory
			return interface
	}

	HandleCharacterKeyboardsInput(){
		Hotkey, IfWinActive, ahk_exe cityofheroes.exe
		Hotkey, h, HomeIn
	}
}


HomeIn:
	characterManager:=CharacterManager.GetInstance()
	SoundPlay sound\N_CharPageScroll.wav
	character:=characterManager.ActivateTargetedCharacter
	characterManager.TargetAndFollow(character)
	return