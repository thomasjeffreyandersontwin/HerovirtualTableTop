
#SingleInstance force
#Include CommandParser.ahk
#Include CharacterManager.ahk
#Include AnimationManager.ahk
#Include CrowdManager.ahk
#Include String-object-file.ahk



class AbstractCOHInterface{
	
	static Interface:=""
	
	CharacterManager:=""
	RosterManager:=""
	AnimationManager:=""
	CrowdManager:=""
	
	GetInstance(file:="Temporary"){
		if (this.interface=="")
		{
			this.interface:= this.NewInstance()
			this.interface.Init(file)
			return this.interface
		}
		else
		{
			return this.interface
		}
	}
	ReleaseFromMemory(){
		this.Release()
	}
	Init(file){
		
		this.CharacterManager:=CharacterManager.GetInstance(file)
		this.CrowdManager:=CrowdManager.GetInstance(file)
		this.AnimationManager:=AnimationManager.GetInstance(file)
		this.AnimationManager.CommandParser:=this.charactermanager.CommandParser
	}

}