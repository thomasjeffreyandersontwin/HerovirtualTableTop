#SingleInstance force

class BaseCityOfHeroesKeybindGenerator{
	GeneratedKeybindText:=""
	KeyBinds:= { TargetName:"target_name",PrevSpawn:"prev_spawn", NextSpawn:"next_spawn", RandomSpawn:"random_spawn", Fly:"fly", EditPos:"edit_pos", DetachCamera:"detach_camera", NoClip:"no_clip", AccessLevel:"access_level", Command:"~", SpawnNpc:"spawn_npc" , Rename:"rename", LoadCostume:"load_costume", MoveNPC:"move_npc" , DeleteNPC:"delete_npc" , ClearNPC:"clear_npc", "Move":"mov", TargetEnemyNear:"target_enemy_near",LoadBind:"load_bind",BeNPC:"benpc" ,SaveBind:"save_bind", GetPos:"getpos", CamDist:"camdist", Follow:"follow", "LoadMap":"loadmap", BindLoadFile: "bind_load_file", Macro:"macro"}
	
	
	GenerateKeyBindsForEvent(function, parameters*){
		return this.notOverided_GenerateKeyBindForEvent(function, parameters*)
	}
	notOverided_GenerateKeyBindForEvent(function, parameters*){
		function:=this.Keybinds[function]
		for k,p in parameters
		{
			if (p <>"")
				p:=trim p
				generatedKeybindText:= generatedKeybindText . " " . p
				generatedKeybindText:= trim generatedKeybindText 
		}
		if( this.GeneratedKeybindText<>"")
			this.GeneratedKeybindText:= this.GeneratedKeybindText . "$$" . function . generatedKeybindText
		else
			this.GeneratedKeybindText:=function . generatedKeybindText
		this.LastFunction:=function
		return function . generatedKeybindText
	}
	CompleteEvent(){
		return this.notOverided_CompleteEvent()
	}
	
	notOverided_CompleteEvent(){
		generatedKeybindText:=this.GeneratedKeybindText
		this.GeneratedKeybindText:=""
		return """" . generatedKeybindText . """"
	}
}
Class ImmediateLoadingKeyBindGenerator extends BaseCityOfHeroesKeybindGenerator{
	TriggerKey:="Y"
	LoaderKey:="B"
	directory:="C:\champions\applications\coh\data\"
	_instance:=""
	GetInstance(){
		if(this._instance==""){
			this._instance:=new ImmediateLoadingKeyBindGenerator()
		}
		return this._instance
	}
	CompleteEvent(){	
		try{
			bindFile:=this.directory . this.LoaderKey . ".txt"
			FileDelete %  bindFile
		}catch
		{
		}
		try
		{
			this.LastKeybindGenerated:=this.GeneratedKeybindText
			command:=this.notOverided_CompleteEvent()
			generatedKeybindText:= this.TriggerKey . " " . command
			FileAppend % generatedKeybindText, % bindFile
		}
		catch
		{
			MsgBox % "InvalName File " . bindFile
		}
		WinActivate ahk_class CrypticWindow
		actual:=""
		while (actual<> generatedKeybindText){
			FileRead actual, %bindFile%
		}
		
		sleep 50
		l:=this.LoaderKey
		t:=this.TriggerKey
		Send  %l%
		sleep 50
		Send  %t%
		return command
	}					
}

