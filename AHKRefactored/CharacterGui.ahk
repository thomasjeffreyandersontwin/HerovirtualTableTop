#SingleInstance force
#Include CharacterRepository.Ahk
#Include UIControls.ahk
Class CharacterGui{
	static instance:=""
	_character:=""
	
	GetInstance(){
		if(this.instance==""){
			this.instance:= new DesktopGui()
		}
		return this.instance
	}
	DisplayBackGround(xpos){
		Gui, VTTBack: New
		Gui VTTBack:  Color, Black
		Gui VTTBack: +LastFound +AlwaysOnTop +ToolWindow
		Gui VTTBack: -Caption
		Gui VTTBack: +Disabled
		WinSet, Transparent, 190
		
		 
		Gui	VTTBack: Show , x0 y100 w390 h550 NoActivate 
	}
	DisplayForGround(xpos){
		global GuiID
		Gui, VTT: New
		Gui VTT:  Color, Black
		Gui, Font, s11 CWhite Bold, mont_hvbold
		Gui +LastFound +AlwaysOnTop +ToolWindow
		Gui -Caption
		;WinSet, TransColor, Black
		Gui VTT: Show , x0 y100  w390 h550 NoActivate 
		WinGet, GuiID, ID, A
		
	}
	__New(){
		this.Repository:=CharacterRepository.GetInstance()
		
		this.DisplayBackground(xpos)
		this.DisplayForGround(xpos)
		
		;Gui, Add, Tab2, v%hTab%  w375 h540, Character|Memory|Events|Errors|Controls
		Gui, Font, S9 CWhite Bold, mont_hvbold
		
		this.InitCharacterBox()
		this.InitCharacterList()
		
		Gui VTTBack: Show,
		Gui VTT: Show,

		WinGetActiveTitle, current
		this.Title:=current
	}
	
	InitCharacterList(){
		local dummy
		
		listString:=""
		
		Gui Color, , Black
		renderFunc:=ObjBindMethod(this.RenderCharacterListPos,"")
		this.CharacterListBox:=new ListboxControl("CharacterList","UpdateCharacter",this, "VTT", renderFunc)
		this.CharacterListBox.Render(this.Repository.AllCharacters)
		
	}
	RenderCharacterListPos(){
		return "w170 h300 xs-10 y+20"
	}
	InitCharacterBox(){
		local dummy
		Gui, Add, GroupBox, R7 W350 ,Character Details
		;Gui, Tab, Character
		
		this.CharacterName:=new ReadWriteControl("Name",, "VTT")
		this.CharacterName.Render(true)
		
		this.Crowd:= new ReadWriteControl("Crowd",,"VTT")
		this.Crowd.Render()
		this.SkinSurface:=new ReadWriteControl("SkinSurface", "Skin Surface","VTT")
		this.SkinSurface.Render()
		this.SkinType:=new ReadWriteControl("SkinType", "Skin Type")
		this.SkinType.Render()
		
		renderFunc:=ObjBindMethod(this.RenderButtonPos,"")
		new ButtonControl("Target",this,"VTT", renderFunc).Render(,true)
		
		
		
		new ButtonControl("Spawn",this,"VTT",renderFunc).Render()
		
		new ButtonControl("Camera",this,"VTT",renderFunc).Render()
		new ButtonControl("CameraToCharacter",this,"VTT",renderFunc).Render("> Cam")
		new ButtonControl("CharacterToCamera",this,"VTT",renderFunc).Render("> Char")
		new ButtonCOntrol("Clear",this,"VTT",renderFunc).Render()
		
		new ButtonControl("NewCharacter",this,"VTT", renderFunc).Render("New",true)
		new ButtonControl("Edit",this,"VTT", renderFunc).Render()
		new ButtonControl("Delete",this,"VTT", renderFunc).Render()
		new ButtonControl("Cancel",this,"VTT", renderFunc).Render()
		new ButtonControl("Save",this,"VTT", renderFunc).Render()
		
	}
	RenderButtonPos(groupStarting){
		if(groupStarting==true){
			pos:="Section xs"
		}
		else{
			pos:="x+5"
		}
		return pos
		}
	
	UpdateCharacter(){
		local dummy

		this.Character.UnTarget()
		
		selectedCharacterName:= this.CharacterListBox.Value
		character:=this.Repository.Characters[selectedCharacterName]
		character.Target()
		this.Character:= character
		
		current:=this.Title
		WinActivate %current%
	}
	UpdateCharacterIfTargetChanged(){
		targeter:= new Targeter()
		if(targeter.Label<>""){
			character:=this.Repository.Targeted
			existingLabel:=this.Character.Label
			if(character<>"" and character.label <> existingLabel){
				this.Character:= character
			}
		}
	}
		
	Character{
		Set{
			local dummy
			this._character:=value
			character:=value
			this.CharacterName.Value:=character.Name
			this.Crowd.value:=character.Crowd
			this.SkinSurface.value:=character.Skin.Surface
			this.SkinType.value:=character.Skin.Type
			
			name:= character.Name
			;StringLeft, name, name, 2
			
			GuiControl, VTT:ChooseString, CharacterList, % name
		}
		Get{
			return this._character
		}
	}
	Clear(){
		this.Character.ClearFromDesktop()
	}
	Spawn(){
		this.Character.Spawn()
	}
	Target(){
		this.Character.ToggleTargeted()
	}
	Camera(){
		this.Character.ToggleManueveringWithCamera()
	}
	CameraToCharacter(){
		this.Character.TargetAndMoveCameraToCharacter()
	}
	CharacterToCamera(){
		this.Character.MoveToCamera()
	}
	Edit(){
		this.CharacterName.EditMode:=true
		this.Crowd.EditMode:=true
		this.SkinSurface.EditMode:=true
		this.SkinType.EditMode:=true
	}
	Cancel(){
		local dummy
		this.CharacterName.EditMode:=false
		this.Crowd.EditMode:=false
		this.SkinSurface.EditMode:=false
		this.SkinType.EditMode:=false
	}
	Save(){
		local dummy
		
		character.Name:=this.CharacterName.Value
		character.Crowd:=this.Crowd.Value
		character.Skin.Surface:=this.SkinSurface.Value
		character.Skin.Type:=this.SkinType.Value
		
		this.Repository.SaveCharacter(character)
		this.CharacterListBox.List:=this.Repository.AllCharacters
		
		this.Cancel()
	}
	NewCharacter(){
		this.Character.UnTarget()
		this.Character:=this.Repository.NewCharacter
		this.Edit()
	}
	Delete(){
		this.Character.ClearFromDesktop()
		this.Repository.DeleteCharacter(this.character)
		this.CharacterListBox.List:=this.Repository.AllCharacters
		this.UpdateCharacter()
	}
	

}

d:= new CharacterGui()
SetTimer HandleAllInputs, 100	

HandleAllInputs:
d.UpdateCharacterIfTargetChanged()
