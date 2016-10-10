#include charactermanager.ahk


Class DesktopGui
{
	static instance:=""
	
	GetInstance(){
		if(this.instance==""){
			this.instance:= new DesktopGui()
		}
		return this.instance
	}
	__New(){
		xPos:= 0
		
		Gui, VTTBack: New
		Gui VTTBack:  Color, Black
		Gui VTTBack: +LastFound +AlwaysOnTop +ToolWindow
		Gui VTTBack: -Caption
		Gui VTTBack: +Disabled
		WinSet, Transparent, 190
		
		Gui	VTTBack: Show , x%xPos% y100  w355 h500 NoActivate 
		
		
		global GuiID
		Gui, VTT: New
		Gui VTT:  Color, Black
		Gui, Font, s11 CWhite Bold, mont_hvbold
		
		Gui, Add, Tab, x2 y7 w350 h500 , Controls|Character|Memory|Events|Errors
		
		
		Gui, Font, S9 CWhite Bold, mont_hvbold
		this.InitControlsTab()
		this.InitCharacterTab()
		this.InitTargetTab()
		this.InitPlayerTab()
		this.InitErrorsTab()
		this.InitEventsTab()
		
		;this.InitEventsTab()
		Gui +LastFound +AlwaysOnTop +ToolWindow
		Gui -Caption
		WinSet, TransColor, Black
		;WinSet, Transparent, 190
		
		Gui VTT: Show , x%xPos% y100  w355 h500 NoActivate 
		WinGet, GuiID, ID, A
		;WinSet, AlwaysOnTop, On, A
	}
	
	
	InitControlsTab(){
		Gui, Tab, Control
		inc:=13
		sep:=20
		pos:= 44
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Move Targeted character - Alt +
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans , W Forward
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans , A Left
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans , S Back 
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans , D Right
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, X  Ready
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Up TurnDown
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Left TurnLeft
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Down TurnUp
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Space Elevate
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Z Descend
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Y Limit character descent to current Camera Y Pos
		pos+=sep
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Character Management Commands
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, P resPawn targeted character
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, H Home in on last target character
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, T reTarget last targeted character
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, J Jump targeted character to camera
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, I move active crowd to camera In formation
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, C Toggle player between Camera and targeted Character
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, V saVe location of targeted character to crowd file
		pos+=inc
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, L spawn and pLace next character from crowd file 
		pos+=sep
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, Alt+M Toggle Crowd Command Mode
		pos+=sep
		Gui, Add, Text, x05 y%pos% +BackgroundTrans, <1 - 9> Play Character Animation 1 - 9
		
	}
	InitEventsTab(){
		local dummy
		Gui, Tab, Events
		Gui, Font, S8 CWhite Bold, mont_demibold
		Gui, Add, Text, x05 y55 w200 h28 +BackgroundTrans, Command History
		Gui, Color, Black
		Gui, Add, Edit, vCommandHistory x05 y73 w350 h400 +BackgroundTrans +ReadOnly, No Events
	}		
	InitPlayerTab(){
		global TargetName
		global tx 
		global ty
		global tz 
		global tpitch
		global tfacing 
		Gui, Tab, Memory
		Gui, Add, Text, x05 y55 w96 h28 , Target:
		Gui, Add, Text, x05 y73 w96 h28 , Name:
		Gui, Add, Text, vTargetName x133 y73 w230 h28 +BackgroundTrans, Nothing Targeted
		Gui, Add, Text, x05 y95 w96 h28 +BackgroundTrans, Location
		Gui, Add, Text, x05 y117 w57 h28 +BackgroundTrans, X:
		Gui, Add, Text, vtX x20 y117 w30 h28 +BackgroundTrans, 000
		Gui, Add, Text, x50 y117 w57 h28 +BackgroundTrans, Y:
		Gui, Add, Text, vtY x65 y117 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x100 y117 w57 h28 +BackgroundTrans, Z:
		Gui, Add, Text, vtZ x115 y117 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x150 y117 w5105 h28 +BackgroundTrans, Facing:
		Gui, Add, Text, vtFacing x205 y117 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x240 y117 w96 h28 +BackgroundTrans, Pitch:
		Gui, Add, Text, vtPitch x280 y117 w33 h28 +BackgroundTrans, 000
		
		global px 
		global py
		global pz 
		global ppitch
		global pfacing
		
		Gui, Add, Text, x05 y139 w96 h28 , Player:
		Gui, Add, Text, x05 y161 w96 h28 +BackgroundTrans, Location
		Gui, Add, Text, x05 y183 w57 h28 +BackgroundTrans, X:
		Gui, Add, Text, vpX x20 y183 w30 h28 +BackgroundTrans, 000
		Gui, Add, Text, x50 y183 w57 h28 +BackgroundTrans, Y:
		Gui, Add, Text, vpY x65 y183 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x100 y183 w57 h28 +BackgroundTrans, Z:
		Gui, Add, Text, vpZ x115 y183 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x150 y183 w5105 h28 +BackgroundTrans, Facing:
		Gui, Add, Text, vpFacing x205 y183 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x240 y183 w96 h28 +BackgroundTrans, Pitch:
		Gui, Add, Text, vpPitch x280 y183 w33 h28 +BackgroundTrans, 000
	}
	InitTargetTab(){
		
	}
	InitCharacterTab(){
		local dummy
		
		Gui, Tab, Character
		Gui, Add, Text, x05 y55 w96 h28 +BackgroundTrans, Name:
		Gui, Add, Text, vName x133 y55 w230 h28 +BackgroundTrans, No Character Selected
		Gui, Add, Text, x05 y73 w192 h28 +BackgroundTrans, Costume / Model:
		Gui, Add, Text, vCostumeModel x133 y73 w220 h28 +BackgroundTrans, No Mod / Costume

		Gui, Add, Text, x05 y103 w57 h19 +BackgroundTrans, Stun:
		Gui, Add, Text, x133 y103 w57 h19 +BackgroundTrans, End:
		Gui, Add, Text, vStun x45 y103 w57 h19 +BackgroundTrans, 00/00
		Gui, Add, Text, vEndurance x168 y103 w57 h19 +BackgroundTrans, 00/00
		
		Gui, Add, Text, x05 y133 w96 h28 +BackgroundTrans, Location
		Gui, Add, Text, x05 y155 w57 h28 +BackgroundTrans, X:
		Gui, Add, Text, vX x20 y155 w30 h28 +BackgroundTrans, 000
		Gui, Add, Text, x50 y155 w57 h28 +BackgroundTrans, Y:
		Gui, Add, Text, vY x65 y155 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x100 y155 w57 h28 +BackgroundTrans, Z:
		Gui, Add, Text, vZ x115 y155 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x150 y155 w105 h28 +BackgroundTrans, Facing:
		Gui, Add, Text, vFacing x205 y155 w33 h28 +BackgroundTrans, 000
		Gui, Add, Text, x240 y155 w96 h28 +BackgroundTrans, Pitch:
		Gui, Add, Text, vPitch x280 y155 w33 h28 +BackgroundTrans, 000
		
		this.InitAnimationWidgets()
		this.InitMovementWidgets()
	}
	
	InitMovementWidgets(){
		local dummy
		Gui, Add, Text, x200 y185  w50  +BackgroundTrans, Movements
		local i=0
		loop{
			i++
			if(i > 8)
				break
			vMovement = Movement%i%
			
			y:=185+i*15
			Gui, Add, Text, v%vMovement% x200 y%y% +BackgroundTrans, No Char Selected No Char Selected
		}
	}
	InitAnimationWidgets(){
		local dummy
		Gui, Add, Text, x05 y185  w50  +BackgroundTrans, Animations
		
		loop{
			i++
			if(i > 20)
				break
			vanim = Animation%i%
			
			y:=185+i*15
			Gui, Add, Text, v%vanim% x05 y%y% +BackgroundTrans, No Char Selected No Char Selected
		}
	}
	InitErrorsTab(){
		local dummy
		Gui, Tab, Errors
		Gui, Add, Text, x05 y55 w96 h28 +BackgroundTrans, Errors:
		Gui, Add, Text, vErrors x05 y73 w400 h300 +BackgroundTrans, No Character Selected
	}
	
	UpdateEventsTab(){
		parser:= CharacterManager.GetInstance().CommandParser
		if(parser.UnreadHistory == true){
			commandHistory:= parser.History
			GuiControl ,VTT:, commandHistory, %CommandHistory%
		}
	}
	UpdateErrorsTab(){
		local t := new Target()
		local character:= CharacterManager.GetInstance().ActivateTargetedCharacter()
		local errors:=""
		local characterName:=character.Name
		local tName:=target.Name
		if(characterName  <> tName){
			errors.= characterName . "\" . tName . "`r `n"
		}
		local characterLocation:=  Format("x:{1:3.3} y:{2:3.3} z:{3:3.3} Facing:{4:3.3}",character.location.x, character.location.y, character.location.z, character.location.Facing)
		local targetLocation:= Format("x:{1:3.3} y:{2:3.3} z:{3:3.3} Facing:{4:3.3}",t.x, t.y, t.z, t.Facing)
		if( targetLocation <> characterLocation){
			if(errors == ""){
				errors.= tName . "`r `n"
			}
			errors.= "Character Location `r `n" . characterLocation . "`r `n Does not equal Target Location `r `n" . targetLocation . "`r `n"
		}
		GuiControlGet, oldErrors,VTT:, Errors
		if (oldErrors <> errors){
			GuiControl ,VTT:, Errors, %errors%
		}
	}
	UpdateCharacterTab(){
		
		local character:=CharacterManager.GetInstance().LastTargetedCharacter
		
		local name:= character.Name
		local modelCostume:= character.Costume . "/" . character.Model
		 
		local stun:= character.CurrStun . "/" . character.MaxStun
		local end:= character.CurrEndurance . "/" . character.MaxEndurance
		
		local lx:= Format("{1:3.3}",character.location.x)
		local ly:= Format("{1:3.3}",character.location.y)
		local lz:= Format("{1:3.3}",character.location.z)
		local lPitch:= Format("{1:3.3}",character.location.Pitch)
		local lFacing:= Format("{1:3.3}",character.location.Facing)
		
		GuiControlGet, oldName,VTT:, Name
		if (oldName <> name){
			GuiControl ,VTT:, Name, %Name%
		}
		GuiControlGet, oldmodelCostume,VTT:, CostumeModel
		if (oldmodelCostume <> modelCostume){
			GuiControl ,VTT:, CostumeModel, %modelCostume%
		}
		GuiControlGet, oldStun,VTT:, Stun
		if (oldStun <> Stun){
			GuiControl ,VTT:, Stun, %stun%
		}
		GuiControlGet, oldEnd,VTT:, Endurance
		if (oldEnd <> End){
			GuiControl ,VTT:, Endurance, %End%
		}
		GuiControlGet, oldX,VTT:, X
		if (oldX <> lx){
			GuiControl ,VTT:, X, %lx%
		}
		GuiControlGet, oldY,VTT:, Y
		if (oldY <> ly){
			GuiControl ,VTT:, Y, %ly%
		}
		GuiControlGet, oldZ,VTT:, Z
		if (oldz <> lz){
			GuiControl ,VTT:, Z, %lz%
		}	
		GuiControlGet, oldFacing,VTT:, Facing
		if (oldFacing <> lFacing){	
			GuiControl ,VTT:, Facing, %lFacing%
		}
		GuiControlGet, oldPitch,VTT:, Pitch
		if (oldPitch <> lPitch){
			GuiControl ,VTT:, Pitch, %lPitch%
		}
		this.UpdateAnimationWidgets(character)
		this.UpdateMovementWidgets(character)
	}
	
	UpdateAnimationWidgets(character){
		local i=0
		animations:= animationManager.GetInstance().GetCharacterAnimations(character)
		for key, animation in animations {
			i++
			if(i > 20)
				break
			GuiControlGet, oldAnimation,VTT:, Animation%i%
			if (oldAnimation <> key){
				vanim = Animation%i%
				GuiControl ,VTT:, Animation%i% , %key%
			}
		}
		loop{
			i++
			if(i > 20)
				break
			GuiControlGet, oldAnimation,VTT:, Animation%i%
			if (oldAnimation <> ""){
				GuiControl ,VTT:, Animation%i%, 
			}
		}
	}
	
	UpdateMovementWidgets(character){
		local i=0
		movements:= character.Movements
		for key, movement in movements {
			i++
			if(i > 8)
				break
			moveOutput:=movement.Type
			if(movement.Type == character.DefaultMovement.Type){
				moveOutput.= " (D)"
			}
			if(movement.Type == character.ActiveMovement.Type){
				moveOutput.= "(A)"
			}
			GuiControlGet, oldMovement,VTT:, Movement%i%
			if (oldMovement <> moveOutput){
				
				GuiControl ,VTT:, Movement%i%, %moveOutput%
			}
		}
		loop{
			i++
			if(i > 8)
				break
			GuiControlGet, oldMovement,VTT:, Movement%i%
			if (oldMovement <> ""){
				GuiControl ,VTT:, Movement%i%, 
			}
		}
	}
	UpdateTargetTab(){
		target := New Target()
		tName:= target.Name
		
		tx:= Format("{1:3.3}",target.x)
		ty:= Format("{1:3.3}",target.y)
		tz:= Format("{1:3.3}",target.z)
		tFacing:= Format("{1:3.3}",target.Facing)
		tPitch:= Format("{1:3.3}",target.Pitch	)
		
		GuiControlGet, oldtName,VTT:, TargetName
		if (oldtName <> tName)
			GuiControl ,VTT:, TargetName, %TName%
		GuiControlGet, oldTx,VTT:, tX
		if (oldTx <> tX)
			GuiControl ,VTT:, tX, %tx%
		GuiControlGet, oldTy,VTT:, ty
		if (oldTy <> ty)
			GuiControl ,VTT:, tY, %ty%
		GuiControlGet, oldTz,VTT:, tz
		if (oldTz <> tz)
			GuiControl ,VTT:, tZ, %tz%
		GuiControlGet, oldTFacing,VTT:, tFacing
		if (oldTFacing <> tFacing)
			GuiControl ,VTT:, tFacing, %tFacing%
		GuiControlGet, oldTPitch,VTT:, tPitch
		if (oldTPitch <> tPitch)
			GuiControl ,VTT:, tPitch, %tPitch%
	}
	UpdatePlayerTab(){
		global px 
		global py
		global pz 
		global ppitch
		global pfacing
		player := New Player()
		px:= Format("{1:3.3}",player.x)
		py:= Format("{1:3.3}",player.y)
		pz:= Format("{1:3.3}",player.z)
		;pFacing:= Format("{1:3.3}",player.Facing)
		pPitch:= Format("{1:3.3}",player.Pitch	)
		
		GuiControlGet, oldpx,VTT:, px
		if (oldpx <> px)
			GuiControl ,VTT:, pX, %px%
		GuiControlGet, oldpy,VTT:, py
		if (oldpy <> py)
			GuiControl ,VTT:, pY, %py%
		GuiControlGet, oldpz,VTT:, pz
		if (oldpz <> pz)
			GuiControl ,VTT:, pZ, %pz%
		GuiControlGet, oldpFacing,VTT:, pFacing
		if (oldpFacing <> pFacing)
			GuiControl ,VTT:, pFacing, %pFacing%
		GuiControlGet, oldpPitch,VTT:, pPitch
		if (oldpPitch <> pPitch)
			GuiControl ,VTT:, pPitch, %pPitch%
	}
	UpdateCharacter(){
		this.UpdateCharacterTab()
		this.UpdateTargetTab()
		this.UpdatePlayerTab()
		this.UpdateErrorsTab()
		this.UpdateEventsTab()
	}


	EnableDragMode(){
		global GuiID
		CoordMode, Mouse
		MouseGetPos, MouseStartX, MouseStartY, MouseWin
		if(MouseWin <> GuiID){
			return false 
		}	
		else{
			startX:= MouseStartX
			starty:= MouseStartY
			this.StartMouse:={ MouseStartX: StartX, MouseStartY: Starty}
			return this.StartMouse
		}
	}

	DragWindow(){
		global GuiID
		GetKeyState, LButtonState, LButton, P
		if (LButtonState== "U")  ; Button has been released, so drag is complete.
		{
			
			return false
		}
		CoordMode, Mouse
		MouseGetPos, MouseX, MouseY
		
		
		MouseStartX:=d.StartMouse.MouseStartX
		MouseStartY:=d.StartMouse.MouseStartY
		DeltaX := MouseX
		DeltaX := DeltaX - MouseStartX
		DeltaY := MouseY
		DeltaY := MouseStartY - DeltaY
		d.StartMouse.MouseStartX := MouseX
		d.StartMouse.MouseStartY := MouseY
		WinGetPos, GuiX, GuiY,,, ahk_id %GuiID%
		GuiX += %DeltaX%
		GuiY += %DeltaY%
		SetWinDelay, -1   ; Makes the below move faster/smoother.
		WinMove, ahk_id %GuiID%,, %GuiX%, %GuiY%
		return true
	}
}

