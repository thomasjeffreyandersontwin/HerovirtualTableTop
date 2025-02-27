#SingleInstance force

#Include HeroVirtualDesktopGraphicalInterface.ahk
#Include String-object-file.ahk
#Include TargetedModel.ahk
#Include CharacterManager.ahk
#Include AnimationManager.ahk
#Include CrowdManager.ahk
#Include CommandParser.ahk
#Include 3DPositioner.ahk


Class MoveInterface{
	instance:=""
	GetInstance(){
		if(this.instance==null){
			this.instance:=new MoveInterface()
		}
		return this.instance
	}
	Ground:=0
  	ActiveCharacterMovement:=""
	DirectionKeyMaps:={"x":"Still","w":"Forward", "a":"Left", "s":"Back", "d":"Right","z":"Down", " ":"Up","Space":"Up", "Left":"TurnLeft", "Right":"TurnRight", "Down":"TurnUp", "Up":"TurnDown"}
	CharacterManager:=CharacterManager.GetInstance()
	HandleTeleportMode(){
		IF(this.TeleportMode==true){
			SoundPlay sound\N_MenuExpand.wav
			loop{
				Input key, I L3 V
				If(key=="000"){				
					SoundPlay sound\N_Undo.wav
					this.TeleportMode:=false
					break
				}
				StringLeft, directionKey, key,1
				StringRight, distance, key,2
				this.HandleMove(directionKey,errorlevel, distance * 9, this.ground)

			}
		}
	}
	
	GrabbedCharacter:=""
	HandlePointMode(){
		if(this.PointMode==true){
			SoundPlay sound\N_MenuExpand.wav
			loop{
				Input key, I L1 V
				If(key==""){				
					SoundPlay sound\N_Undo.wav
					 
					this.PointMode:= false
					break
				}
				if (key=="g")
				{
					SoundPlay sound\N_Select.wav
					WinGetActiveTitle, current
					this.GrabbedCharacter:=this.CharacterManager.ActivateTargetedCharacter
					WinActivate %current%
					continue
				}
				if (key=="r")
				{
					
					destination:= this.CharacterManager.ActivateTargetedCharacter.location
					
					character:=this.GrabbedCharacter
					this.ActiveCharacterMovement:= character.ActiveMovement
					
					this.ActiveCharacterMovement.TravelToLocation(destination)
				}
			}
		}
	}

	
	
	MoveCharacter(key){
		this.DisableKeys()
		character:=this.CharacterManager.ActivateTargetedCharacter
		character.ActiveMovement.LastDirection:=""
		loop
		{
			moving1:=this.MoveCharacterDirection("w") 
			moving2:=this.MoveCharacterDirection("a") 
			moving3:=this.MoveCharacterDirection("s") 
			moving4:=this.MoveCharacterDirection("d")
			moving5:=this.MoveCharacterDirection("Space")
			moving6:=this.MoveCharacterDirection("Left")
			moving7:=this.MoveCharacterDirection("Right")
			moving8:=this.MoveCharacterDirection("Up")
			moving9:=this.MoveCharacterDirection("Down")
			moving10:=this.MoveCharacterDirection("x")
			moving11:=this.MoveCharacterDirection("z")
				
			if not (moving1 or moving2 or moving3 or moving4 or moving5 or moving6 or moving7 or moving8 or moving9 or moving10 or moving11){
				Movement.SwitchToCameraControl()
				break
			}
		}
		this.EnableKeys()
		return		
	}
	MoveCharacterDirection(key){
		GetKeyState state, %key%, P
		if(state=="D"){
			if (Movement.CameraDisabled== false){
				Movement.DisableCameraControl()
			}
			character:=this.CharacterManager.ActivateTargetedCharacter
			if(key =="Down" or key =="Up" or key =="Left" or key =="Right"){
				character.ActiveMovement.Distance:=.30
			}
			else{
				character.ActiveMovement.Distance:=.30/9
			}
			character.ActiveMovement.Direction:=this.DirectionKeyMaps[key]
			character.ActiveMovement.Ground:=this.ground
			this.crowdMode:=CrowdManager.GetInstance().CrowdMode
			if(this.crowdMode == false){
				character.ActiveMovement.MoveCharacter()
			}
			else{
				character.ActiveMovement.MoveAllCharactersInCrowd()
			}
			return true
		}
		else{
			return false
		}
	}
	DisableKeys(){
		Hotkey, !w, Off
		Hotkey, !a, Off
		Hotkey, !s, Off
		Hotkey, !d, Off
		Hotkey, !Space, Off
		Hotkey, !Left, Off
		Hotkey, !Right, Off
		Hotkey, !Up, Off
		Hotkey, !Down, Off
		Hotkey, !x, Off
		Hotkey, !z, Off
	}
	EnableKeys(){
		Hotkey, !w, On
		Hotkey, !a, On
		Hotkey, !s, On
		Hotkey, !d, On
		Hotkey, !Space, On
		Hotkey, !Left, On
		Hotkey, !Right, On
		Hotkey, !Up, On
		Hotkey, !Down, On
		Hotkey, !x, On
		Hotkey, !z, On
	}
	
	ActivateMovementForTargetCharacter(key){
		character:=this.CharacterManager.ActivateTargetedCharacter
		mode:= Movement.MovementMode[key]
		character.ActiveMovementMode:=mode
		this.ActiveCharacterMovement:= character.ActiveMovement
	}
	
	ActivateMoveModeForSingleCharacterBasedOnInfo(characterInfo, key){
		if(characterInfo <> ""){
			character:=this.CharacterManager.UpdateCharacterInfoIfItIsAlreadyBeingManaged(characterInfo)
		}
		else
		{
			character:=this.CharacterManager.ActivateTargetedCharacter
		}
		mode:= Movement.MovementMode[key]
		character.ActiveMovementMode:=mode
		this.ActiveCharacterMovement:= character.ActiveMovement
	}
}

Class Movement{
	fast:=false
	static Movements
	Ground:=""
	Mode:=""
	CharactersBeingMoved:=1
	CameraDisabled:=false
	MovementMode[KeyID]{
		Get{
			if(this.Movements==""){
				this.Movements:={}
				this.LoadMovements()
				this.movements:=this.BuildMovements()
			}
			for key, movement in THIS.Movements{
				StringUpper, UKeyId, KeyId
				if(movement.ActivationKey== KeyId or movement.ActivationKey== UKeyId ){
					return Key
				}
			}
		}
	}
	Directions:={Right:{animation:"",coord:"x", Forward:true},Left:{animation:"",coord:"x", Forward:false}, Up:{animation:" ",coord:"y", Forward:true}, Still:{animation:"",coord:"y", Forward:true},	Down:{animation:"",coord:"y", Forward:false},  Back:{animation:"",coord:"z", Forward:false},Forward:{animation:"",coord:"z", Forward:true}}
	Modes:=["fly", "beast", "run", "jump", "knockback", "hover"]
	
	BuildMovementForCharacter(characterInfo, character){
		movementsList:=this.BuildMovements()
		movements:={}
		for key, movementInfo in characterInfo.MovementInfos{
 			movement:= movementsList[movementInfo.Type]
			movement.Range:= movementInfo.Range
			movements[movementInfo.Type]:=movement
		}
		if(movements["Run"]==""){
			movements["Run"]:=movementsList["Run"]
			movements["Run"].range:=6
		}
		if(movements["Walk"]==""){
			movements["Walk"]:=movementsList["Walk"]
			movements["Walk"].range:=3
		}
		if(movements["Swim"]==""){
			movements["Swim"]:=movementsList["Swim"]
			movements["Swim"].range:=2
		}if(movements["Knockback"]==""){
			movements["Knockback"]:=movementsList["Knockback"]
			movements["Knockback"].range:=99
		}
		character.base.base:= new MoveManager().base
		character.movements:= movements
		if(characterInfo.DefaultMovement==""){
			character.DefaultMovement:=movementsList["Run"]
		}
		else{
			character.DefaultMovement:= movementsList[characterInfo.DefaultMovement]
			movements[characterInfo.DefaultMovement]:=character.DefaultMovement
		}
		return movements
	}
	LastDirection:=""
	Type:=""
	_activeCharacter:=""
	MoveAnimations{
		Get{
			animations:=[]
			
			for key, direction in this.Directions{
				counter++
				animations[counter]:= this[key]
			}
			return animations
		}
	}
	MaxDistance:=0
	BuildMovements(){
		movementCOnfigs:=StrObj("movements\movement.config")
		movements:={}
		for mode, movementConfig in movementCOnfigs{
			movement:= new Movement()
			movement.Type:=movementConfig.MovementType
			movement.Left:={NumKey:"a", Sequence:"AND",  movs:[movementConfig.Left]}
			movement.Right:={NumKey:"d", Sequence:"AND",movs:[movementConfig.Right]}
			movement.Forward:= {NumKey:"w", Sequence:"AND",movs:[movementConfig.Forward]}
			movement.Back:={NumKey:"s", Sequence:"AND",movs:[movementConfig.Back]}
			movement.Still:={NumKey:"x", Sequence:"AND",movs:[movementConfig.Still]}
			movement.Up:={NumKey:"Space", Sequence:"AND",movs:[movementConfig.Up]}
			movement.Down:={NumKey:"z", Sequence:"AND",movs:[movementConfig.Down]}
			movement.ActivationKey:=movementConfig.ActivationKey
			movement.Mode:= mode
			movements[mode]:= movement
		}
		return movements
	}	
	ActiveCharacter{
		set{
			;animManager:=AnimationManager.GetInstance()
			;animations:=this.MoveAnimations
			
			;animManager.GenerateBindFilesWithActiveCharacterUsingAnimations(value, animations, "Move")
			
			this._activeCharacter:=value
		}
		Get{
			return this._activeCharacter
		}
	}
	
	convertDistanceToCoord(distance){
		return distance * 8
	}
	
	MoveAllCharactersTogether(characters){
		AnimationManager.GetInstance().CommandParser.QueueCommands:=true	
		if (this.direction <> "TurnLeft" and this.direction <> "TurnRight" and this.direction <> "TurnUp" and this.direction <> "TurnDown"){
			if( this.Direction <> this.LastDirection){     ;this.LastDirection == ""
				AnimationManager.GetInstance().QueueAnimations:=true
				for name, character in characters{
					AnimationManager.GetInstance().CommandParser.Build("TargetName",[character.Name])
					this.PlayAnimationForDirection(character)
				}
				this.SubmitAnimationForDirection()
				
			}
		}
		for name, character in characters{
			this.CharactersBeingMoved:= this.CharactersBeingMoved+1
		}
		
		for name, character in characters{
			this.IncrementDirection(character)
		}
		
		this.CharactersBeingMoved:=1
	}
	MoveAllCharactersInCrowd(){	
		facing:=this.ActiveCharacter.memoryInstance.facing
		pitch:=this.ActiveCharacter.memoryInstance.pitch
		characters:=CrowdManager.GetInstance().CharactersInCrowd
		for name, character in characters{
			character.memoryInstance.Facing:=facing
			character.memoryInstance.Pitch:=Pitch
		}
		this.MoveAllCharactersTogether(characters)
	}
	MoveCharacter(){
		this.PlayAnimationForDirection()
		this.IncrementDirection()
	}
	MoveCharacterAccordingToInstructions(direction, distance, ground){
		this.distance:=distance
		this.ground:=ground
		this.direction:=direction
		this.MoveCharacter()
	}
	PlayAnimationForDirection(character:=""){
		if(this.Direction <> this.LastDirection){
			if(this.direction <> "TurnUp" and this.direction <> "TurnDown" and  this.direction <> "TurnLeft" and this.direction <> "TurnRight"){ 
				if(character==""){
					mov_animation:=this[this.direction]
				}
				else{
					mov:=character.ActiveMovement
					mov_animation:=mov[this.direction]
				}
				AnimationManager.GetInstance().PlayAnimationMovs(mov_animation)
				AnimationManager.GetInstance().CommandParser.MarkCommandComplete()
			}
		}
	}
	SubmitAnimationForDirection(){
		if(this.direction <> this.LastDirection){
			if(this.direction  <> "TurnUp" and this.direction <> "TurnDown" and  this.direction <> "TurnLeft" and this.direction <> "TurnRight"){ 
				AnimationManager.GetInstance().SubmitQueuedAnimations()
			}
		}
	}
	IncrementDirection( character:=""){
		if(this.direction == "TurnLeft" or this.direction =="TurnRight"){
			this.IncrementFacing(character)
		}
		else
		{
			if(this.direction == "TurnUp" or this.direction == "TurnDown"){
				this.IncrementPitch(character)
			}
			else
			{
				this.Increment(character)
			}
		}
	}
	
	Increment( character:=""){
		positioner:= new 3DPositioner()
		if(this.direction <> "Still"){
			if(character==""){
				character:=this.ActiveCharacter
			}
			memoryInstance:=character.MemoryInstance
			
			if(this.direction == "Up"){
				locationInstructions:={x:0,y:1, z:0}
			}
			else{
				if(this.direction="Down"){
					locationInstructions:={x:0,y:-1, z:0}
				}
				else{
					if(this.direction <> character.LastDirection or this.FacingChanged:=true){
						adjustedFacing:=positioner.AdjustDirectionBasedOnFacingAndDirectionTravelling(memoryInstance, this.direction)	
						character.locationInstructions:=positioner.UpdateLocationInstructionsBasedOnFacing(adjustedFacing)
						this.FacingChanged:=false
					}
				}
			}
			if(this.direction <> character.LastDirection or this.PitchChanged:=true){
				character.locationInstructions:=positioner.UpdateLocationInstructionsBasedOnPitch(character.locationInstructions, memoryInstance.Pitch,this.direction, memoryInstance)
				this.PitchChanged:=false
			}
			positioner.IncrementAccordingToLocationInstructions(memoryInstance, character.locationInstructions, this.distance, this.ground,"",this.CharactersBeingMoved, this.fast)
			
			if (this.direction <> "TurnLeft" and this.direction <> "TurnRight" and this.direction <> "TurnUp" and this.direction <> "TurnDown"){
				if(this.direction <> character.LastDirection){
					character.LastDirection:=this.direction
				}
				if(this.direction <> this.LastDirection){
					this.LastDirection:=this.direction
				}
				
			}
		}
	} 
	IncrementPitch(character:=""){
		if(character==""){
			character:=this.ActiveCharacter
		}
		t:= character.MemoryInstance
		
		counter:=1
		while(counter <= this.distance * 9 ){
			counter++
			t.IncrementPitch(this.direction)
			sleep 300 / (this.CharactersBeingMoved *3)
		}
		this.PitchChanged:=true
		
	}
	IncrementFacing(character:=""){
		if(character==""){
			character:=this.ActiveCharacter
		}
		t:= character.MemoryInstance
		while(counter <= this.distance ){
			counter++
			t.IncrementFacing(this.direction)
			sleep 300 / (this.CharactersBeingMoved *3)
		}
		this.FacingChanged:=true
	}
	
	getDistanceFromDestinationToActiveCharacter(destination){
		characterLocation:=this.ActiveCharacter.Location
		positioner:= new 3DPositioner()
		distance:= positioner.CalculateRangeToLocation(characterLocation, destination)
		distance:=distance/9
		return distance
	}
	LowestPointBetween(start, end){
		if(start.Y > end.Y){
			ground:= end.Y
		}
		else{
			ground:=start.Y
		}
		return ground
	}
	
	playForwardAnimationForActiveCharacter(){
		CharacterManager.GetInstance().TargetCharacter(this.ActiveCharacter)
		this.Direction:="Forward"
		this.LastDirection:=""
		this.PlayAnimationForDirection()
	}
	playForwardAnimationForCharactersInActiveCrowd(){
		this.Direction:="Forward"
		this.LastDirection:=""
		characters:=CrowdManager.GetInstance().CharactersInCrowd
		AnimationManager.GetInstance().QueueAnimations:=true
		for name, character in characters{
			AnimationManager.GetInstance().CommandParser.Build("TargetName",[character.Name])
			this.PlayAnimationForDirection(character)
		}
		this.SubmitAnimationForDirection()
		
	}
	moveActiveCharacterToDestination(destination, distance){
		positioner:= new 3DPositioner()
		characterLocation:=this.ActiveCharacter.Location
		locationInstructions:= positioner.UpdateLocationInstructionsBasedOnPositionOfStartAndDestination(characterLocation, destination)
		ground:= this.LowestPointBetween(characterLocation, destination)
		positioner.IncrementAccordingToLocationInstructions(this.ActiveCharacter.MemoryInstance, locationInstructions, distance, ground, destination )
	}
	moveCharactersInActiveCrowdToDestination(primeDestination, distance){
		positioner:= new 3DPositioner()
		primeOrigin:=this.ActiveCharacter.Location
		locationInstructions:= positioner.UpdateLocationInstructionsBasedOnPositionOfStartAndDestination(primeOrigin, primeDestination)
		
		memoryInstanceDestinationMap:= this.buildRelativeDestinationsForCharactersInActiveCrowd(primeOrigin,primeDestination)
		
		this.CharactersBeingMoved:=CrowdManager.GetInstance().CountOfCharactersInCrowd	
		
		positioner.IncrementCrowdAccordingToLocationInstructions(memoryInstanceDestinationMap, locationInstructions, distance, this.CharactersBeingMoved )
		this.CharactersBeingMoved:=1
	}
	buildRelativeDestinationsForCharactersInActiveCrowd(primeOrigin,primeDestination){
		positioner:= new 3DPositioner()
		facing:=this.ActiveCharacter.memoryInstance.Facing
		pitch:=this.ActiveCharacter.memoryInstance.Pitch
		memoryInstanceDestinationMap:=[]
		counter:=0
		characters:=CrowdManager.GetInstance().CharactersInCrowd
		for name, character in characters{
			character.memoryInstance.Facing:=facing
			character.memoryInstance.Pitch:=Pitch
			
			characterOrigin:= character.Location
			delta:=positioner.CalculateDelta(primeOrigin, characterOrigin)
			characterDestination:=positioner.LocationAdd(primeDestination, delta)
			
			ground:= this.LowestPointBetween(characterOrigin, characterDestination)
			counter++
			memoryInstanceDestinationMap.Insert(counter, {instance:character.memoryInstance, destination:characterDestination, ground:ground})
		}
		return memoryInstanceDestinationMap
		
	}
	playStillAnimationForActiveCharacter(){
		this.Direction:="Still"
		this.PlayAnimationForDirection()
	}
	playStillAnimationForCharactersInActiveCrowd(){
		characters:=CrowdManager.GetInstance().CharactersInCrowd
		AnimationManager.GetInstance().QueueAnimations:=true
		this.Direction:="Still"
		for name, character in characters{
			AnimationManager.GetInstance().CommandParser.Build("TargetName",[character.Name])
			this.PlayAnimationForDirection(character)
		}
			
		this.SubmitAnimationForDirection()
	}
	
	TravelToLocation(destination, ground:=0, toCam:=""){
		this.playForwardAnimationForActiveCharacter()
		distance:= this.getDistanceFromDestinationToActiveCharacter(destination)
		this.moveActiveCharacterToDestination(destination, distance)
		this.playStillAnimationForActiveCharacter()
	}
	TravelCharactersInCrowdToLocation(primeDestination, ground:=0, toCam:=""){
		this.playForwardAnimationForCharactersInActiveCrowd()
		distance:= this.getDistanceFromDestinationToActiveCharacter(primeDestination)
		this.moveCharactersInActiveCrowdToDestination(primeDestination, distance)

	}
		
	SwitchToCameraControl(){
		this.ActiveMovement:=""
		parser:=CommandParserFactory.NewTempKeybindFileParser()
		parser.build("BindLoadFile",["camera_control.txt"])
		parser.SubmitCommand()
		Movement.CameraDisabled:= false
	}
	DisableCameraControl(){
		parser:=CommandParserFactory.NewTempKeybindFileParser()
		parser.build("BindLoadFile",["disable_camera_control.txt"])
		parser.SubmitCommand()
		
		Movement.CameraDisabled:= true
	}
		
}

Class MoveManager
{
	Movements:={}
	DefaultMovement:=""
	_ActiveMovement:=""
	ActiveMovement{
		Get{
			if(this._ActiveMovement==""){
				this.ActiveMovementMode:=this.DefaultMovement.Type
			}
			return this._ActiveMovement
		}
		Set{
			this._ActiveMovement:= value
			this._ActiveMovement.ActiveCharacter:=this
		}
	}
	
	ActiveMovementMode{
		set{
			aMovement:=this.Movements[value]
			if(aMovement == ""){
				movements:= Movement.BuildMovements()
				aMovement:=movements[value]
			}
			this.ActiveMovement:=aMovement
		}
	}
	
	MoveDelta(delta){
		
		characterManager.GetInstance().TargetCharacterBasedOnName(this._name)
		this.CommandParser.SubmitCommands()
		
		pos:= new 3dPositioner()
		p:= new Player()
		facing:=player.Facing
		facing:= facing - delta.SourceFacing
		
		t:= this.MemoryInstance
		rotatedDelta:=pos.RotateVector(0, facing, delta)
		LocationInfo:=pos.LocationAdd(t,rotatedDelta)
		t.MoveTo(LocationInfo)
		
		
		
		t.Facing:= player.facing 
		;'\ delta.facing - delta.SourceFacing +player.facing
	}
		
	MoveToLocation(LocationInfo, dontUseMemory:=false){
		characterManager.GetInstance().TargetCharacterBasedOnName(this._name, dontUseMemory)
		this.CommandParser.SubmitCommands()
		t:= this.MemoryInstance
		t.MoveTo(LocationInfo)
	}
}


	 