#Include CityOfHeroesKeybindgenerator.ahk
#Include Targeter.ahk


Class Camera{
	static _instance:=""
	Generator:=""
	__New(){
		this.DefaultSkin:= new CharacterSkin(this, "V_Arachnos_Security_Camera", "Model")
		this.Generator:=ImmediateLoadingKeyBindGenerator.GetInstance()
	}
	MoveToCharacter(completedEvent:=true){
		this.Generator.GenerateKeyBindsForEvent("Follow", "")
		if(completedEvent==true){
			return this.Generator.CompleteEvent()
		}
	}
	ManueveringCharacter{
		Set{
			keybinds:=[]
			if( value <>""){
				this._character:=value
				this._character.Target(false)
				
				this.MoveToCharacter(false)
				keybinds[0]:= this.Generator.CompleteEvent()
				
				this._character.ClearFromDesktop(false)
				this.Skin:=value.Skin
				keybinds[1]:= this.Skin.Render()
				
				while(this.Location.IsWithin(2, this._character.Location)==false){
					sleep 10
				}
			}
			else{
				keybinds[0]:= this.ActivateCameraSkin()
				keybinds[1]:= this._character.Spawn()
				this._character:=""
			}
			return keybinds
		}
	}
	ActivateCameraSkin(){
		this.Skin:=this.DefaultSkin
		return this.Skin.Render()
	}
	GetInstance(){
		if(this._instance==""){
			this._instance:= new Camera()
			
		}
		return this._instance
	}
}
CLass CharacterSkin{
	_surface:=""
	Character:=""
	Generator:=""
	Type:=""
	__New(character, surface, type){
		this.Character:=character
		this.Type:=type
		
		this.Generator:= ImmediateLoadingKeyBindGenerator.GetInstance()
		this._Surface:=surface
	}
		
	Surface{
		Set{
			this._surface:=value
			if(this.character<>""){
				if(this.Character.Targeted==false){
					this.Character.Target()
				}	
				this.Render()
			}
		}
		Get{
			return this._surface
		}
	}
	Render(){
		if(this.Type=="Model"){
			this.Generator.GenerateKeyBindsForEvent("BeNPC", this.Surface)
		}
		if(this.Type=="Costume"){
			this.Generator.GenerateKeyBindsForEvent("LoadCostume", this.Surface)
		}
		return this.Generator.CompleteEvent()
	}
}

Class Character{
	_Name:=""
	COHPlayer:=""
	originalName:=""
	Label{
		Get{
			return this.Name . " [" . this.Crowd.Name . "]"
		}
	}
	Name{
		Get{
			return this._name
		}
		Set{
			this.UpdateOriginalName()
			this._Name:= value
		}
	}
	UpdateOriginalName(){
		if(this.originalName=="" and this._name <> ""){
			this.originalName:=this._name
		}
	}
	Position{
		Get{
			return this.COHPlayer.Position
		}
	}
	addCOHPlayerUntilTargetRegisters(){
		while(this.Label <> this.COHPlayer.Label){
			this.COHPlayer:= new Targeter()
			this.COHPLayer.Target()
			sleep 10
			counter++
			if(counter > 10){
				this.COHPLayer:=""
				break
			}
		}	
	}
	waitUntilTargetRegisters(){
		while(this.Label <> this.COHPlayer.Label){
			this.COHPlayer.Target()
			counter++
			sleep 10
			if(counter > 10){
				break
			}
		}
	}
}
	
	
Class ManagedCharacter extends Character{
	Generator:= ImmediateLoadingKeyBindGenerator.GetInstance()
	_skin:=""
	__New(name, surface:="", skinType:=""){
		this.Init(name, surface,skinType)
	}
	
	init(name, surface, skinType){
		this._Name:=name
		this._skin:=new CharacterSkin(this, surface, skinType)
	
	}
	Skin{
		Set{
			this._skin:=value
			this._skin.Character:=this
			this.Target()
			;to do test swapping skin across characters
			return this._skin.Render()
		}
		Get{
			return this._Skin
		}
	}
	ToggleTargeted(){
		this.Targeted:= not this.Targeted
	}
	
	Target(completeEvent=true){
		if(this.COHPlayer==""){
			keybind:=this.Generator.GenerateKeyBindsForEvent("TargetName", this.label)
			if(completeEvent==true){
				this.Generator.CompleteEvent()
				this.AddCOHPlayerUntilTargetRegisters()
			}
		}
		else{
			this.COHPlayer.Target()
			this.waitUntilTargetRegisters()
		}
		return keybind
	}
	Targeted{
		Get{
			currentTarget:=new Targeter()
			if(currentTarget.Label == this.Label){
				return true
			}
			return false
		}
		Set{
			if(value==true){
				this.Target()
			}
			else{
				if(value==false){
					this.Untarget()
				}
			}
		}
	}
	UnTarget(){
		this.Generator.GenerateKeyBindsForEvent("TargetEnemyNear", [])
		keybind:=this.Generator.CompleteEvent()
		currentTarget:=new Targeter()
		while(currentTarget.Label <>""){
			currentTarget:=new Targeter()
		}
		return keybind
	}
	TargetAndMoveCameraToCharacter(){
		keybind:= this.Target()
		camera:= Camera.GetInstance()
		if(keybind== ""){
			return camera.MoveToCharacter()
		}
		else
		{
			return keybind . "$$" . camera.MoveToCharacter()
		}
	}
	MoveToCamera(){
		this.Target()
		this.Generator.GenerateKeyBindsForEvent("MoveNPC", [])
		keybind:=this.Generator.CompleteEvent()
	}
	ToggleManueveringWithCamera(){
		this.ManueveringWithCamera:= not this.ManueveringWithCamera
	}
	ManueveringWithCamera{
		Get{
			return this._manueverWithCamera
		}
		Set{
			this._manueverWithCamera:=value
			camera:=Camera.GetInstance()
			if(this._manueverWithCamera==true){
				keybinds:=camera.ManueveringCharacter:=this
			}
			else
			{
				if(this._manueverWithCamera==false){
					keybinds:=camera.ManueveringCharacter:=""
				}
			}
			return keybinds
		}
	}
	Spawn(){
		this.Target()
		if(this.COHPlayer.Label == this.Label){
			this.Generator.GenerateKeyBindsForEvent("DeleteNPC", [])
		}
		this.COHPLayer:=""
		this.Generator.GenerateKeyBindsForEvent("SpawnNPC", "model_Statesman", this.label)
		this.Target(false)
		
		skin:=this.Skin
		keybind:=this.Skin.Render()
		
		this.Target()
		return keyBind
	}
	ClearFromDesktop(completedEvent:=true){
		this.Target(false)
		
		keybind:=this.Generator.GenerateKeyBindsForEvent("DeleteNPC", [])
		if(completedEvent==true){
			this.Generator.CompleteEvent()
		}
		this.COHPlayer:=""
	}
}