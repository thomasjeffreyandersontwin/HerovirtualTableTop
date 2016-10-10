
#Include Logger.ahk
class CommandParserFactory
{
	static _TempKeyBindParser:= new TempKeyBindParser()
	NewKeybindFileParser(character,purpose){
		parser:=new KeybindFileParser
		parser.Character:=character
		parser.Purpose:=purpose
		return parser
	}
	
	NewRosterModeCommandParserParser(roster){
		parser:=new RosterModeCommandParser
		parser.Roster:=roster
		return parser
	}
	NewChainedKeybindFileParser(character,purpose){
		parser:=new ChainKeybindParser
		parser.Character:=character
		parser.Purpose:=purpose
		return parser
	}
	NewTempKeybindFileParser(character=""){
		return this._TempKeyBindParser
	}
	NewMacroCommandParser(character=""){
		return new MacroCommandParser
	}
	NewPopMenuParser(character=""){
		root:= new MenuItem
		root.Name:="Champions"
		
		model:= new MenuItem
		model.Name:="Model"
		
		animations:= new MenuItem
		animations.Name:="Animations"
		
		Root.Child:= model
		root.Child:= Animations
		
		parser:= new PopMenuParser
		parser.RootMenutItem:=root
		return parser
	}

}

class MenuItem{
	Name:=""
	_children:={}
	Command:=""
	
	Child[Name=""]{
		Set{
			if(Name==""){
				Name:=value.Name
			}
			this._children[Name]:=value
		}
		Get{
			return this._children[Name]
		}
	}
}
class PopMenuParser extends BaseCommandParser{
	RootMenutItem:= ""
	MenuItem:=""
	MenuName:=""			
	MenuContent:=""
	MenuFile:="..\data\texts\English\Menus\Champions.mnu"
	SubmitCommand(){	
		if (this.MenuItem=="" or this.MenuName==""){
			MsgBox "Set the Name of the menu Name and Item first"
		}
		if(this.MenuItem.Child[this.MenuName]<>""){
			prev:=this.MenuItem.Child[this.MenuName]
			if(prev._children[1].Name==""){
				parent:= new MenuItem
				parent.Name:= this.MenuName
				prev.Name:= 1
				parent.Child:= prev
				this.MenuItem.Child:=parent
				this.MenuName:=parent._children.MaxIndex()+1
			}
			else{
				parent:= prev
				this.MenuName:=prev._children.MaxIndex()+1
			}
		}
		else{
			parent:=this.MenuItem
		}
		option:= new MenuItem
		option.Command:= this.SubmitIt()
		option.Name:= this.MenuName
		parent.Child:=option
	}
	Publish(character){
		this.PublishMenuItemAndTraverseChildrenItems(this.RootMenutItem,0)
		this.WriteMenuContentToFileAndLoad(character)
	}
	PublishMenuItemAndTraverseChildrenItems(parentMenuItem, indent){
		count:=0
		while (count < indent){
			tab:= tab . "`t"
			count++
		}
		if(parentMenuItem.Command){
			this.MenuContent:= this.MenuContent . tab . "Option " . """" . parentMenuItem.Name . """ " . parentMenuItem.Command . "`n"
		}
		else{
			this.MenuContent:= this.MenuContent . tab . "Menu """ . parentMenuItem.Name . """`n"
			this.MenuContent:= this.MenuContent . tab "{`n"
			indent++
			for name, menuItem in parentMenuItem._children{
				this.PublishMenuItemAndTraverseChildrenItems(menuItem,indent )
			}
			indent--
			count:=0
			tab:=""
			while (count < indent){
				tab:= tab . "`t"
				count++
			}
			this.MenuContent:=this.MenuContent . tab . "}`n"
		}
	}
	WriteMenuContentToFileAndLoad(character){
		FileDelete % this.MenuFile
		FileAppend % this.MenuContent, % this.MenuFile
		
		keyParser:= new KeybindFileParser
		keyParser.Character:=character
		keyParser.Key:="P"
		keyParser.Purpose:="Champions_PopMenu"
		keyParser.Build("PopMenu", [ this.MenuFile ])
		keyParser.SubmitCommand()
		keyParser.Publish()
	}
}

class ChainKeybindParser extends KeybindFileParser
{
	SubmitCommand(){
		chainedCommands:=this.KeyBinds[this.Key]
		if(chainedCommands == ""){
			chainedCommands:=[]
			chainNum:= 1
		}
		else{
			chainNum:= chainedCommands.MaxIndex()+1
		}
		chainedCommands[chainNum]:= this.UnprocessedCommands
		this.KeyBinds[this.Key]:=chainedCommands
		this.UnprocessedCommands:=""
	}
	initChain(chainedCommands){
		if(chainedCommands == ""){
			chainedCommands:=[]
			return 1
		}
		else{
			return chainedCommands.MaxIndex()+1
		}
	}
	Publish(){
		
		this.BuildFile()
		this.WriteKeybindContentToFileAndLoad()
	}
	BuildFile(){
		this.BindFileContent:=""
		for key, bind in this.KeyBinds{
				for chainNum, chainedBind in bind{
					if(chainNum < bind.Maxindex()){
						chainedBind:=this.AddCommandToLoadNextKeybindInChain(chainNum, chainedBind, key)
					}
					else{
						if(chainNum== bind.Maxindex() and chainNum <> 1){
							chainedBind:=this.AddToCommandToLoadFirstKeyBindInChain(chainedBind)
						}
						else{
							chainedBind:="""" . chainedBind . """"
						}
					}
					if(chainNum==1){
						this.PublishCommandToBindFile(key, chainedBind)
					}
					else{
						if(chainNum <> 1){
							this.PublishComandToChainedBindFile(key, chainNum, chainedBind)
						}
					}
				}
				
			}
		}
	PublishComandToChainedBindFile(key, chainNum, chainedBind){
		chainedBindFile:= this.KeyBindFullDir . this.KeyBindFile . key . "_" . chainNum . ".txt"
		try{
			FileDelete % chainedBindFile
		}
		catch
		{
		}
		FileAppend % key . " " . chainedBind, % chainedBindFile
	}
	
	AddCommandToLoadNextKeybindInChain(chainNum, chainedBind, key){
		nextChain:= chainNum+1
		nextChainBindFile:= this.KeyBindFile . key . "_" . nextChain . ".txt"
		chainedBind:="""" .  chainedBind . "$$bind_load_file " . this.KeyBindDir . nextChainBindFile . """"
		return chainedBind
	}
	AddToCommandToLoadFirstKeyBindInChain(chainedBind){
		chainedBind:= """" . chainedBind . "$$bind_load_file " . this.KeyBindDir . this.KeyBindFile . ".txt" . """"
		return chainedBind
	}
}
 class KeybindFileParser extends BaseCommandParser{  
	KeyBinds:={}
	Key:=""
	
	Character:=""
	Purpose:=""   
	BindFileContent:=""
	Directory:="data\"
	KeyBindDir{
		get{
			dir:= StrReplace(this.Character.Name, " ", "_") . "\"
			return dir
		}
	}
	KeyBindFile{
		Get{
			keyBindFile:= StrReplace(this.Character.Name, " ", "_") . "_" . StrReplace(this.Purpose, " ", "_") 
			return keyBindFile
		}
	}
	
	KeyBindFullDir{
		Get{
			dir:= this.Directory . this.KeyBindDir
			fullDir:="..\" .  dir
			return  fullDir
		}
	}
	SubmitCommand(){
		if(this.Key==""){
			msgbox "Set The Key First"
		}
		this.KeyBinds[this.Key]:=this.SubmitIt()
	}
	Publish(){
		if(this.Character=""){
			msgbox "no Character set"
		}
		for key, command in this.KeyBinds 
			this.PublishCommandToBindFile(key,command)
		this.WriteKeybindContentToFileAndLoad()
	}

	PublishCommandToBindFile(key,command){
		this.BindFileContent:= this.BindFileContent . key . " " . command . "`n"
	}
	WriteKeybindContentToFileAndLoad(){
		file:= this.KeyBindFullDir . this.KeyBindFile . ".txt"
		try{
			
			FileDelete % file
		}
		catch
		{}
			
		
		FileCreateDir, % this.KeyBindFullDir
		content:= this.BindFileContent
		FileAppend %content%, %file%
		
		keyParser:= new TempKeyBindParser
		
		
		keyParser.Build("BindLoadFile", [this.KeyBindDir . this.KeyBindFile . ".txt" ])
		keyParser.SubmitCommand()
	}
	
}
class RosterModeCommandParser extends ChainKeybindParser{
	RosterMemberKeybinds:={}
	_submitted:=1
	_key:=""
	KeyBindDir{
		get{
			return StrReplace(this.Roster.Name, " ", "_") . "\"
		}
	}
	KeyBindFile{
		Get{
			keyBindFile:= StrReplace(this.Roster.Name, " ", "_") . "_" . StrReplace(this.Purpose, " ", "_") 
			return keyBindFile
		}
	}
	Key{
		Set{
			this._submitted:=1
			this._key:=value
		}
		Get{
			return this._key
		}
	}
	SingleProcessor{
		Set{
			this.base:=value
		}
	}
	Roster{
		Set{
			this._roster:=value
			for key, rosterMember in this._roster.Characters{
				rosterMember.Roster:=value
				this._roster.Characters[key]:=CharacterModel.BuildValidCharacter(rosterMember)
			}
		}
		Get{
			return this._roster
		}
	}
	Purpose{
		Get{
			return:=this.roster.Name . "_" . "Roster_Cycle"
		}
	}
	IndividualChainSegments:=1
	BuildSpawn(character){
		chainNum:=this._submitted
		this.Character:=rosterMember.Name
		for key, character in this.Roster.Characters {
			base.BuildSpawn(character)
			this.StoreKeyBinds(chainNum)
			chainNum:= chainNum + this.IndividualChainSegments
		}
	}
	BuildLoadTheCostume(character) {
		chainNum:=this._submitted
		this.Character:=rosterMember.Name
		for key, character in this.Roster.Characters {
			base.BuildLoadTheCostume(character)
			this.StoreKeyBinds(chainNum)
			chainNum:= chainNum + this.IndividualChainSegments
		}
	}
	BuildLoadCharacterBinds(character){
		chainNum:=this._submitted
		this.Character:=rosterMember.Name
		for key, character in this.Roster.Characters {
			base.BuildLoadCharacterBinds(character)
			this.StoreKeyBinds(chainNum)
			chainNum:= chainNum + this.IndividualChainSegments
		}
	}
	
	BuildTransformCameraToCharacter(character){
		chainNum:=this._submitted
		this.Character:=rosterMember.Name
		for key, character in this.Roster.Characters {
			base.BuildTransformCameraToCharacter(character)
			this.StoreKeyBinds(chainNum)
			chainNum:= chainNum + this.IndividualChainSegments
		}
	}
	
	Build(commandKey, parameters=""){
		chainNum:=this._submitted
		this.Character:=rosterMember.Name
		if(this.KeyBinds[this.Key]=="")
			this.KeyBinds[this.Key]:=[]
		for key, character in this.Roster.Characters{
			newPara:=this.generateParaForCharacter(character,parameters)
			base.BuildIt(commandKey,newPara)
			this.StoreKeyBinds(chainNum)
			chainNum:= chainNum + this.IndividualChainSegments
		}
	}	
	generateParaForCharacter(character,parameters){
		newPara:={}
		for pkey,para in parameters{
			if(para == this.Roster.Name)
				newPara[pkey]:= character.Name
			else
				newPara[pkey]:=parameters[pkey]
		}
		return newPara
	}
	
	
	StoreKeyBinds(chainNum){
		chainedCommands:=this.KeyBinds[this.Key]
		if(chainedCommands==""){
			chainedCommands:={}
		}
		if(chainedCommands[chainNum]=="")
			chainedCommands[chainNum]:=this.UnprocessedCommands
		else
			chainedCommands[chainNum]:=chainedCommands[chainNum] . "$$" . this.UnprocessedCommands
		this.KeyBinds[this.Key]:=chainedCommands
		this.UnprocessedCommands:=""
	}
	
	SubmitCommand(){
		this._submitted++
		;this.Character:= this.roster
		;rosterMember:=CharacterModel.BuildValidCharacter(rosterMember)
		;base.SubmitCommand()
	}
}
	
class BaseCommandParser{
	pause:=true
	UnprocessedCommands:=""
	Commands:= { TargetName:"target_name",PrevSpawn:"prev_spawn", NextSpawn:"next_spawn", RandomSpawn:"random_spawn", Fly:"fly", EditPos:"edit_pos", DetachCamera:"detach_camera", NoClip:"no_clip", AccessLevel:"access_level", Command:"~", SpawnNpc:"spawn_npc" , Rename:"rename", LoadCostume:"load_costume", MoveNPC:"move_npc" , DeleteNPC:"delete_npc" , ClearNPC:"clear_npc", "Move":"mov", TargetEnemyNear:"target_enemy_near",LoadBind:"load_bind",BeNPC:"benpc" ,SaveBind:"save_bind", GetPos:"getpos", CamDist:"camdist", Follow:"follow", "LoadMap":"loadmap", BindLoadFile: "bind_load_file", Macro:"macro"}
	
	BuildSpawn(character){
		if(character.model=="none" or character.model==""){
			character.model:="Citizen_Biz_Male_01"
		}
		this.Buildit("SpawnNPC", [character.model, character.name])
	}
	BuildTransformCameraToCharacter(character){
		if(character.Costume==""){
			this.BuildIt("BeNPC",[character.Model])
		}
		else
		{
			this.BuildIt("LoadCostume",[character.Costume])
		}
	}	
	BuildLoadTheCostume(character) {
		Logger.Log("BuildLoadTheCostume", submittedeEvent)
		if(character.costume<>"")
		{
			this.BuildIt("TargetName", [character.name])
			this.BuildIt("LoadCostume",[character.costume])
		}
	}
	Build(commandKey, parameters=""){
		return this.BuildIt(commandKey, parameters)
	}
	BuildIt(commandKey, parameters){
		commandString:=this.Commands[commandKey]
		if (parameters <> "")
		{
			for k,p in parameters
			{
				if (p <>"")
					p:=trim p
					commandString:= commandString . " " . p
			}
		}
		if( this.UnprocessedCommands<>"")
			this.UnprocessedCommands:= this.UnprocessedCommands . "$$" . commandString
		else
			this.UnprocessedCommands:=commandString
		return commandString
	}
	SubmitCommand(){
		return this.SubmitIt()
		
	}
	
	SubmitIt(){
		retVal:=this.UnprocessedCommands
		this.UnprocessedCommands:=""
		return """" . retVal . """"
	}
}
class MacroCommandParser extends BaseCommandParser{
	Macros:={}
	Macro:=""
	
	SubmitCommand(){	
		if (this.Macro==""){
			MsgBox "Set the Name of the macro first"
		}
		this.Macros[this.Macro]:=this.SubmitIt()
	}
	Publish(){
		keyParser:= new AltTempKeyBindParser
		for name, command in this.Macros{
			keyParser.Build("Macro", [name, command])
			keyParser.SubmitCommand()
			sleep 200
		}
	}
}

Class TempKeyBindParser extends BaseCommandParser{
	TriggerKey:="Y"
	LoaderKey:="B"
	directory:="..\data\"
	QueueCommands:=false
	AppendHistory(text){
		history:=this.directory . this.LoaderKey . "_history.txt"
		FileAppend % text . "`n" ,  % history
	}
	MarkCommandComplete(){
		if(this.QueueCommands == false){
			history:=this.directory . this.LoaderKey . "_history.txt"
			FileAppend ___________________________________________ `r `n, % history
			this.UnreadHistory:= True
		}
	}
	DeleteHistory(){
		file:= this.directory . this.LoaderKey . "_history.txt"
		FileDelete, %file%
	}
	History{
		get{
			file:= this.directory . this.LoaderKey . "_history.txt"
			FileRead, out, %file%
			this.UnreadHistory:= false
			return out
		}
	}
	SubmitCommand(){	
		if(this.QueueCommands == true){
			return
		}
		command:=this.SubmitIt()
		this.UnprocessedCommands:=""
		if(StrLen(command) >255){
			a:=StrSplit(command,"$$")
			newbinds:=[]
			count:=1
			for key, val in a
			{
				if (Strlen(newbinds[count] . val . "$$") > 255){
					newbinds[count].=""""
					count+++
					newbinds[count].=""""
				}
				newbinds[count].= val . "$$"
			}
			for key , item in newbinds
				this.CreateTempBindFileAndSubmitContents(item)
				sleep 300
		}
		else{
			this.CreateTempBindFileAndSubmitContents(command)
			
		}
		return command
		
	}	

	CreateTempBindFileAndSubmitContents(command){
		bind:=this.directory . this.LoaderKey . ".txt"
		history:=this.directory . this.LoaderKey . "_history.txt"
		FileDelete %  bind
		
		keyBind:= this.TriggerKey . " " . command
		FileAppend % keyBind, % bind
		FileAppend % keyBind . "`n", % history
		
		WinGetActiveTitle, current
		WinActivate ahk_class CrypticWindow
		actual:=""
		sleep 50
		l:=this.LoaderKey
		t:=this.TriggerKey
		Send  %l%
		sleep 50
		Send  %t%
		;WinActivate %current%
	}
}	


Class AltTempKeyBindParser extends BaseCommandParser{
	TriggerKey:="Y"
	LoaderKey:="B"
	directory:="..\data\"
	
	SubmitCommand(){	
		try{
			bind:=this.directory . this.LoaderKey . ".txt"
			FileDelete %  bind
		}catch
		{
		}
		try
		{
			keyBind:= this.TriggerKey . " " . this.UnprocessedCommands
			this.UnprocessedCommands:=""
			FileAppend % keyBind, % bind
		}
		catch
		{
	
			;MsgBox % "InvalName File " . bind
		}
		WinActivate ahk_class CrypticWindow
		actual:=""
		while (actual<> keyBind){
			FileRead actual, %bind%
		}
		sleep 50
		l:=this.LoaderKey
		t:=this.TriggerKey
		Send  %l%
		sleep 50
		Send  %t%
	}					
}	
		