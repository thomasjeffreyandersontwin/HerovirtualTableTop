#Include CharacterManager.ahk
#Include TargetedModel.ahk
#SingleInstance force

class KeyboardInterface
{
	characterManager:=CharacterManager.GetInstance()
	JumpToModel(){
		Target:=new Target()
		destination:=target.GetValueObject()
		;this.characterManager.TargetNoOne()
		player:=new Player()
		player.moveTo(destination)
	}
}

ki:= new KeyboardInterface()


!h::
{
	global ki
	ki.JumpToModel()
}


	