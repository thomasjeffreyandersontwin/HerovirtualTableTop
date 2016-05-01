#SingleInstance force
#Include CommandParser.ahk
#Include AnimationManager.ahk
#Include String-object-file.ahk
#Include TargetedModel.ahk
#include CharacterManager.ahk

ac:= AnimationManagerKeyboardInterface.GetInstance()

class AnimationManagerKeyboardInterface{
	Path:=""
	Directory:="data\\"
	static Interface:=""

	NewCharacter:=""

	
	GetInstance(){
	
			this.interface:= new AnimationManagerKeyboardInterface
			this.interface.AnimationManager:= AnimationManager.GetInstance()
			return this.interface
	}

	
	removeKeyPressedAndSelectText(){
		send ^z
		Send +{Home}
	}
	GrabText(){
		act:=WinActive("ahk_class ahk_class CrypticWindow")
		if (act==0){
			;this.removeKeyPressedAndSelectText()
			return this.GrabNPCFromExcel()
		}
	return ""
	}
	GrabNPCFromExcel(){
		Clipboard:=""
		Send ^c
		sleep 1000
		var:=Clipboard
		var:=RegExReplace(var, "\r\n$","")
		return var
	}
	GrabAndParseDots(){
		THIS.removeKeyPressedAndSelectText()
		Send ^c
		sleep 500
		var:=StrSplit(Clipboard , ".")
		return var
	}
	HandleAnimationListLoading(){
		SoundPlay sound\N_MenuExpand.wav
		Loop{
			Input key, I L1
			If(key==""){				
				SoundPlay sound\N_Undo.wav
				break
			}
			if (key=="s")
			{
				SoundPlay sound\N_Select.wav
				charName:=this.GrabText()
				char:={ _name:charName }
				this.AnimationManager.GetCharacterAnimations(char)
				continue
			}
			if (key=="f")
			{
				SoundPlay sound\N_Select.wav
				charName:=this.GrabText()
				this.AnimationManager.FinishEvaluatingMovAndAddAsAnimation()
				continue
			}
			if (key=="e")
			{
				SoundPlay sound\N_Select.wav
				WinGetActiveTitle, current
				mov:=this.GrabText()
				this.AnimationManager.EvaluateMovAsAnAnimation(mov)
				WinActivate %current%
				continue
			}
			if (key=="m")
			{
				SoundPlay sound\N_Select.wav
				WinGetActiveTitle, current
				mov:=this.GrabText()
				
				target:= new Target()
				modelObject:= ModelClass.Build(target.GetValueObject())
				this.AnimationManager.GetCharacterAnimations(modelObject)
				this.AnimationManager.PlayMov(mov)
				WinActivate %current%
				continue

			}
			if (key=="a")
			{
				target:= new Target()
				modelObject:= CharacterModel.BuildValidCharacter(target.GetValueObject())
				this.AnimationManager.GetCharacterAnimations(modelObject)
				
				SoundPlay sound\N_Select.wav
				WinGetActiveTitle, current
				anim:=this.GrabText()
				anim:=StrObj(anim)
				for key , val in anim
						anim:=val
				this.AnimationManager.PlayStructuredMov(val, modelObject._Name)
				WinActivate %current%
				continue
			}
			SoundPlay sound\N_Error.wav
		}
	}
}
$!a::
{
	global ac
	ac.HandleAnimationListLoading()
	return
}


!x::
{
	MouseGetPos X, Y	
	i=0
	while(i< 10){
		MouseClick, Right, x, y 
		sleep 50
		Mouseclick Right
		sleep 50
		Send r
		 
		x:=x+40
		
		i++
	}
	return
		
 }
 
 !^b::
 {
	
	SoundPlay sound\chimes.wav		
			
	manager:= AnimationManager.GetInstance()
	animFile:=GrabText()
	manager.BuildBadgesForAnimations(animFile)
	return
}

GrabText(){
		Clipboard:=""
		Send ^c
		sleep 1000
		var:=Clipboard
		var:=RegExReplace(var, "\r\n$","")
		return var
	}