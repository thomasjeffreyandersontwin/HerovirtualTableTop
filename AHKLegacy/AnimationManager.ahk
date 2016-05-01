#SingleInstance force
#Include CommandParser.ahk
#Include InstanceManager.ahk
#Include Logger.ahk

class AnimationListInterface{

	Path:=""
	Directory:=""
	static Interface:=""
	ManageAnimationMode:=false
	CharacterManager:=""
	GrabText(){
		act:=WinActive("ahk_class ahk_class CrypticWindow")
		if (act==0){
			sleep 500
			send {Escape}
			sleep 500
			Send ^c
			sleep 500
			var:=Clipboard
			var:=RegExReplace(var, "\r\n$","")
			return var
		}
		return ""
	}
	GetInstance(){
		if(this.interface=""){
			this.interface:= new AnimationListInterface
			this.interface.AnimationManager:= AnimationManager.GetInstance()
			this.interface.Directory:=interface.AnimationManager.Directory
		}
			return this.interface
	}
	HandleAnimationListLoading(){
		if(this.ManageAnimationMode== true){
			SoundPlay sound\enter.wav
			Loop{
				Input key, I L1 V
				SoundPlay sound\chimes.wav 
				If(key==""){				
					SoundPlay sound\chimes.wav 
					this.ManageAnimationMode:=false
					break
				}
				if (key=="g")
				{
					this.AnimationManager.ManageGlobalAnimations()
					
				}
				if (key=="n")
				{
					this.AnimationManager.Character:= charactermanager.getinstance().LastTargetedCharacter
					WinGetActiveTitle, current
					mov:=this.GrabText()
					this.AnimationManager.PlayMov(mov)
					WinActivate %current%
					
				}
				if (key=="l")
				{
					WinGetActiveTitle, current
					mov:=this.GrabText()
					this.AnimationManager.Character:= charactermanager.getinstance().LastTargetedCharacter
					this.AnimationManager.PlayAnimation(mov)
					WinActivate %current%
					
				}
			}
		}
	}
}

Class AnimationManager extends InstanceManager{
	static manager
	playedCommands:={}
	_defaultAnimations:={}
	_combatEffectAnimations:=[]
	_characterAnimations:=[]
	Directory:="animations\\"
	character:=""
	AnimimationListCounter:=0
	GetCharacterAnimations(character:=""){
		crowdNumPos := RegExMatch(character._name, "\(\d+\)" , mobIdentifier = "")
		if(crowdNumPos> 0){
			name:= Trim(Substr(character._name, 1, crowdNumPos -1 ))
		}
		else{
			name:=character._name
			if(name==""){
				name:= character.Name
			}
		}
		if(character<>"")
		{
			this._characterAnimations:=this.LoadLocalAnimations(name)
			this.character:=character
		}
		for key,val in this._characterAnimations
			this.AnimimationListCounter++
		this.AnimimationListCounter++
		return this._characterAnimations
	}
	CombatEffectAnimations{
		Get
		{
			this._combatEffectAnimations:=this.LoadLocalAnimations("combat_effects")
			return this._combatEffectAnimations
		}
	}
	DefaultAnimations{
		Get
		{
			this._defaultAnimations:=this.LoadLocalAnimations("Default")
			return this._defaultAnimations
		}
	}
	DataType{
		Get{
			return "animations"
		}
	}
	NewInstance{
		Get{
			return new AnimationManager
		}
	}
	Init(){
		
	}
	
	Animations[key]{
		Get{
			allAnimations:=[]
			characterAnimations:=this.GetCharacterAnimations()
			for key2, value in characterAnimations
				allAnimations[key2]:=value
			defaultAnimations:=this.LoadLocalAnimations("Default")
			for key2, value in defaultAnimations
				allAnimations[key2]:=value
			combatEffectAnimations:=this.LoadLocalAnimations("combat_effects")
			for key2, value in combatEffectAnimations
				allAnimations[key2]:=value
			
		return 	allAnimations[key]
		}
	}
	
	
	
	AnimationsByTrigger[triggerPressed]{
		Get{
			allAnimations:=[]
			characterAnimations:=this.GetCharacterAnimations()
			for key, value in characterAnimations{
				trigger:=value.NumKey
				if(trigger <> "")
					allAnimations[trigger]:=value
			}
			return 	allAnimations[triggerPressed]
		}
	}
	LoadLocalAnimations(localPath){
		FoundPos := RegExMatch(localPath, "(\d+) \(\d+\)")
		if(foundpos <> 0){
			localPath2:= Trim(SubStr(localPath, 1 , FoundPos-1) )
		}
		else	
			localPath2:=localpath
		this.file:=localPath2
		this.LoadData()
		localData:=this._instanceData
		this._instanceData:={}		
		return localData
	}	
	LoadAnimations(){
		this.DefaultAnimations:=this.LoadLocalAnimations("Default")
		this._combatEffectAnimations:=this.LoadLocalAnimations("combat_effects")
	}
	
	GenerateBindFilesWithActiveCharacter(character){
		characterAnimations:=this.GetCharacterAnimations(character)
		this.GenerateBindFilesWithActiveCharacterUsingAnimations(character, characterAnimations, "Animation")
	}
	
	GenerateBindFilesWithActiveCharacterUsingAnimations(character, animations, purpose){
		originalParser:=this.CommandParser
		this.CommandParser:=CommandParserFactory.NewChainedKeybindFileParser(character,purpose)
		this.CommandParser.pause:=false
		chainNum:=0
		;characterAnimations:=this.GetCharacterAnimations(character)		
		for key, mov in animations{
			if(mov.NumKey <> ""){
				this.CommandParser.Key:= mov.NumKey
				;this.PlayAnimation(key)
				this.PlayAnimationMovs(mov)
			}
		}
		this.CommandParser.Publish()
		this.CommandParser:= originalParser
	}
	
	
	
	BuildBadgesForDefaultAnimations(){
		originalParser:=this.CommandParser
		this.CommandParser:=CommandParserFactory.NewMacroCommandParser()
		for key, mov in this.DefaultAnimations{
			this.CommandParser.Macro:= key
			this.PlayStructuredMov(mov)
		}
		this.CommandParser.Publish()
		this.CommandParser:= originalParser
	}
	
	BuildBadgesForAnimations(animFile){
		originalParser:=this.CommandParser
		this.CommandParser:=CommandParserFactory.NewMacroCommandParser()
		anims:=this.GetCharacterAnimations({_name:animFile})
		for key, mov in anims{
			this.CommandParser.Macro:= key
			this.PlayStructuredMov(mov)
		}
		this.CommandParser.Publish()
		this.CommandParser:= originalParser
	}
	
	GeneratePopupMenuWithActiveCharacter(character){
		originalParser:=this.CommandParser
		this.CommandParser:=CommandParserFactory.NewPopMenuParser(character,"Animation")
		this.CommandParser.Pause:=false
		this.BuildPopupMenu(character)		
		this.CommandParser.Publish(character)
		this.CommandParser:= originalParser
	}
	BuildPopupMenu(character){
		root:= this.CommandParser.RootMenutItem
		animations:=root.Child["Animations"]
		defaultItem:= new MenuItem
		defaultItem.Name:="Default"
		animations.Child:=defaultItem
		this.CommandParser.MenuItem:=defaultItem
		for key, mov in this.DefaultAnimations{
			this.CommandParser.MenuName:= key
			this.PlayStructuredMov(mov)
		}
		
		characterItem:= new MenuItem
		characterItem.Name:=character.Name
		animations.Child:=characterItem
		this.CommandParser.MenuItem:=characterItem
		for key, mov in this.GetCharacterAnimations(character){
			this.CommandParser.MenuName:= key
			this.PlayStructuredMov(mov)
		}
		
		combatItem:= new MenuItem
		combatItem.Name:="Combat Effects"
		animations.Child:=combatItem
		this.CommandParser.MenuItem:=combatItem
		for key, mov in this.CombatEffectAnimations{
			this.CommandParser.MenuName:= key
			this.PlayStructuredMov(mov)
		}
	}
	
	ParseFXName(FX){
		FoundPos := RegExMatch(FX, "\w+\.fx" , fxName)
		return fxName
	}
	
	PlaySound(SoundFile) {
		FileDelete, %A_ScriptDIR%\Sound1.AHK
		FileAppend,
		(
			#NoTrayIcon
			SoundPlay, sound\action\%SoundFile%, Wait
		), %A_ScriptDir%\Sound1.AHK
		Run, %A_ScriptDIR%\Sound1.AHK
	}
	
	AddFxToModelAndSpawn(charName, Mov){
		fx:=mov.FX
		parser:= this.CommandParser
		character:=charName
		location:="..\costumes"
		file:=character . ".costume"
		origPath:=location . "\" . file
		newFolder:= location . "\" . character
		
		FXName:=this.ParseFXName(FX)
		newPath:= newFolder . "\" . character . "_" . FXName . ".costume"
		
		if(FileExist(newFolder)<> true){
			FileCreateDir % newFolder
		}
		
		if(FileExist(newPath)){
			FileDelete % newPath
		}
		if(FileExist( origPath)){
			;FileCopy %origPath%, %newPath%
			this.InsertFX(origPath,newPath, Mov)
			parser.Build("LoadCostume", [character . "\" . character . "_" . FXName])
			if(mov.PlayWithNext <> "True"){
				parser.SubmitCommand()
			}
			sleep 300
		}
		return newPath
	}
	
	RemoveEffects(characterInfo){
		character:=characterInfo.name
		origPath:= "..\costumes\" . character . ".costume"
		archivePath:= "..\costumes\" . character . "\" . character .  "_original.costume"
		if(archivePath <> ""){
			FileCopy  %archivePath% ,%origPath%, 1
			FileDelete, %archivePath%
		}
		
		parser:= this.CommandParser
		parser.Build("LoadCostume", [ character ])
		parser.SubmitCommand()
		sleep 300
	}

	RemoveEffectsEvent(Animation,characterInfo){
		this.RemoveEffects(characterInfo)
	}
	
	AddPersistentFxToModelAndSpawn(character, Mov){
		newpath:=this.AddFxToModelAndSpawn(character, Mov)
		
		origPath:=location . "..\costumes\" . character . ".costume"

		archivePath:= "..\costumes\" . character . "\" . character .  "_original.costume"
		;while(FileExist(archivePath) <> ""){
		;	mods++
		;	archivePath:= "..\costumes\" . character . "\" . character . "_original" . mods . ".costume"
		;}
		
		if( FileExist(archivePath) == ""){
			FileCopy %origPath%, %archivePath% , 1
		}
		FileCopy %newpath%, %origPath% , 1
	}
	
	
	InsertFX(origPath, newPath, Mov){
		FX:=Mov.fX
		FileRead fileStr, %origPath%
		
		fxNone:= "Fx none"
		fxNew:= "Fx " . FX
		output:=StrReplace(fileStr, fxNone , fxNew ,,1)
		fxPos := InStr(output, fxNew)
		partStart:= InStr(output,"Color1",, fxPos)
		partEnd:= InStr(output,"}",, fxPos)
		outputStart:=Substr(output, 1 , partStart -1)
		
		outputEnd:= SubStr(output, partEnd , Strlen(output) - partEnd)
		outputColors:= outputcolors . "Color1 " .  Mov.color1.red ", " . Mov .color1.green ", " . Mov .color1.blue . "`n"
		outputColors:= outputcolors . "`tColor2 " .  Mov.color2.red ", " . Mov .color2.green ", " . Mov .color2.blue . "`n"
		outputColors:= outputcolors . "`tColor3 " .  Mov.color3.red ", " . Mov .color3.green ", " . Mov .color3.blue . "`n"
		outputColors:= outputcolors . "`tColor4 " .  Mov.color4.red ", " . Mov .color4.green ", " . Mov .color4.blue . "`n"
		
		output:= outputStart . outputColors . outputEnd
	
		
		FileAppend %output%, %newPath%
	}
	
	PlayFXMov(mov, charName){
		if(charName<>""){
			if(mov.persistent=="True"){
				this.AddPersistentFxToModelAndSpawn(charName, Mov)
			}
			else
			{
				this.AddFxToModelAndSpawn(charName, mov)
			}
		}
	}
	PlayMov(mov, charName=""){
		if(charName==""){
			if (this.character._name <> ""){
				charName:=this.character._Name
			}
		}
		fxName:=this.ParseFXName(mov)
		if(fxName <> ""){
			if(charName<>""){
				if(mov.persistent==true){
					this.AddPersistentFxToModelAndSpawn(charName, Mov)
				}
				else
				{
					this.AddFxToModelAndSpawn(charName, mov)
				}
			}
		}
		else{
			isWav := RegExMatch(mov, "\w+\.wav" , fxName)
			if(isWav> 0){
				if(this.CommandParser.QueuedAnimations<> true){
					c:=this.CommandParser.__class
					if (c == "ChainKeybindParser" ){
						return
					}
					else{
						this.PlaySound(mov)
					}
				}
				else
				{
					this.QueuedSound:= mov
				}
			}
			else{
				;this.CommandParser.Build("SpawnNPC", [mov, mov])
				this.CommandParser.Build("Move", [mov])
				if(mov.PlayWithNext <> 1){
					return this.CommandParser.SubmitCommand()
				}
				else{
					return ""
				}
			}
		}
	}
	
	SubmitQueuedAnimations(){
		this.QueueAnimations:=false
		this.CommandParser.SubmitCommand()
		this.CommandParser.MarkCommandComplete()
	}
	QueueAnimations{
		Set{
			this.CommandParser.QueueCommands:=value
		}
		Get{
			return CommandParser.QueueCommands
		}
			
	}
	
	PlayAnimationMovs(mov,char=""){
		this.PlayStructuredMov(mov, char)
		commands:=this.playedCommands
		this.PlayedCommands:={}
		queue := this.QueueAnimations
		return commands
	}
	PlayAnimation(key, charName=""){
		Logger.log("PlayAnimation", key)
		mov:= this.Animations[key]
		this.PlayAnimationMovs(mov,charname)
	}
	PlayAnimationBasedOnTrigger(trigger, character){
		ca:=this.GetCharacterAnimations(character)
		anim:= this.AnimationsByTrigger[trigger]
		this.PlayAnimationMovs(anim, character._name)
		this.CommandParser.MarkCommandComplete()
	}
	PlayStructuredMov(mov, charName=""){
		if(charName==""){
			if (this.character._name <> ""){
				charName:=this.character._Name
			}
		}
		if(mov.FX <> ""){
			command:=this.PlayFXMov(mov, charName)
		}
		else{
			if(mov is alnum){
				command:=this.PlayMov(mov, charName)
				index:=this.playedCommands.MaxIndex()+1
				if(index==""){
					index=1
				}
				this.playedCommands[index]:= command
			}
			else{
				if(mov.pause <>""){
					if(this.CommandParser.pause==true){
						sleep % mov.pause
						;SoundPlay sound\button-3.wav
					}
					return
				}
				if(mov.delete <>""){
					if(this.CommandParser.pause==true){
						sleep 4000
					}
					this.CommandParser.Build("TargetName",[mov.delete])
					this.CommandParser.Build("DeleteNPC", [])
					if(mov.PlayWithNext <> 1){
						this.CommandParser.SubmitCommand()
					}
					return
				}
				if (mov.sequence<>""){
					if(mov.sequence=="AND"){
						this.PlayAllAnimations(mov.movs, charName)
					}
					else{
						if (mov.sequence=="OR"){
							this.PlayAnAnimationAtRandom(mov.movs, charName)
						}
					}
				}
				else{
					for key, child in mov{
						if(key <> "pause"){
							THIS.PlayStructuredMov(child, charName)
						}
					}
					
				}
			}
		}
		
	}
	PlayStructuredMovBasedOnCharacterInfo(animation, characterInfo){
		CharacterManager.GetInstance().TargetCharacterBasedOnInfo(characterInfo)
		this.PlayStructuredMov(animation, characterInfo.Name)
	}
	PlayAnimationFor(character, anim){
		ca:=this.GetCharacterAnimations(character)
		this.PlayAnimation(anim, character._name)
		
	}
	PlayAllAnimations(movs, charName){
		for key, mov in movs{
			this.PlayStructuredMov(mov, charName)
		}
	}
	PlayAnAnimationAtRandom(movs, charName){
		Random, chosen ,1, movs.MaxIndex()
		this.PlayStructuredMov(movs[chosen])
	}
	
	PlayAbilityBasedOnCharInfo(animation, characterInfo ){
		characterManager:=characterManager.GetInstance()
		characterManager.TargetCharacterBasedOnInfo(characterInfo)
		character:= characterManager.Characters[characterInfo.Name]
		this.PlayAnimationFor( character, animation)
	}
	PlayAbilityCycle(attack, characterInfo){
		if (Attack.Ability == ""){
			attack.ability:=attack
		}
		if(attack.ability.Name <>"" and attack.ability.Name <> "Hold Action"){	
			characterManager:=CharacterManager.GetInstance()
			character:=characterManager.UpdateCharacterInfoIfItIsAlreadyBeingManaged(characterInfo)
			
			
			
			characterManager.TargetCharacter(character)
			
			this.PlayAnimationFor(character, attack.ability.Name)
		}
		if(attack.Type=="single" or attack.Type==""){
			sleep 500
			this.PlayAnimationForDefender(attack)
		}
		else{
			sleep 500
			this.PlayHitOrMissForAreaEffect(attack)
			this.PlayKnockbackForAreaEffectAttack(attack)
		}
		;sleep 500
	}
	PlayHitOrMissForAreaEffect(attack){
		characterManager:=characterManager.GetInstance()
		for key, individualAttack in attack.IndividualAttack{
			if(individualAttack.Target<> ""){
				individualAttack.character:={name:individualAttack.Target}
			}
			if(individualAttack.character.name <> "Hex"){
				individualAttack.character:=characterManager.UpdateCharacterInfoIfItIsAlreadyBeingManaged(individualAttack.character)
			}
		}
		characterManager.DontUseMemoryWhenTargeting:=true
		this.CommandParser.QueueCommands:=true	
		for key, individualAttack in attack.IndividualAttack{
			character:=individualAttack.character
			if(individualAttack.character.name <> "Hex" and  individualAttack.character.name <> "WORKINGHEX"){
				
				charactermanager.TargetCharacter(character)
				this.PlayHitOrMissAnimations(attack.Ability, individualAttack.result, individualAttack.character)
			}
		}
		this.PlaySound(this.QueuedSound)
		this.QueuedSound:=""
		characterManager.DontUseMemoryWhenTargeting:=false
		this.COmmandParser.QueueCommands:=false
		this.COmmandParser.SubmitCommand()
		sleep 1000
	}
	
	PlayKnockbackForAreaEffectAttack(attack){
		knockedCharacters:={}
		KnockMap:={}
		for key, knockback in attack.Knockbac{
			if(knockback[1] <>""){
				knockBack:=knockback[1]
			}
			if(knockBack.effect="knockeddown"){
				knockBack.distance:=1
			}
			if(knockback.Target.Name== ""){
				knockback.Target:= {name:knockback.Target}
			}
			character:=charactermanager.Characters[knockback.Target.Name]
			knockedCharacters[knockback.Target.Name]:=character
			
			KnockMap[knockback.Target.Name]:=knockBack
			knockbackMovement:= character.Movements.Knockback
			character.ActiveMovement:= knockbackMovement
			
			knockbackMovement.Ground:= character.MemoryInstance.y
			
		}
		
		 
		knockbackMovement:=knockedCharacters[knockback.Target.Name].ActiveMovement
		
		knockbackMovement.Direction:="Back"
		knockbackMovement.Distance:= .30
		knockbackMovement.Fast:=true
		
		travelled:=0
		travellingCharacters:=""
		loop{
			charsMoved:=0
			for charName, knockback in KnockMap{
				if (knockback.distance > travelled){
					if(travellingCharacters==""){
						travellingCharacters:={}
					}
					travellingCharacters[charName]:=knockedCharacters[charName]
					charsMoved++
				}
			}
			if(travellingCharacters <>""){
				knockbackMovement.CharactersBeingMoved:= charsMoved
				knockbackMovement.MoveAllCharactersTogether(travellingCharacters)
				travelled:=travelled + knockbackMovement.Distance
				travellingCharacters:=""
			}
			else{
				break
			}
		}
		knockbackMovement.Direction:="Down"
		knockbackMovement.Distance:= 0
		knockbackMovement.MoveAllCharactersTogether(knockedCharacters)	
						
		knockbackMovement.Direction:="Still"
		knockbackMovement.Distance:= 0
		knockbackMovement.MoveAllCharactersTogether(knockedCharacters)	
		knockbackMovement.CharactersBeingMoved:= 1
		knockbackMovement.Fast:=false
	}	
		
	
	PlayAnimationForDefender(attack){
		;characterManager.DontUseMemoryWhenTargeting:=true
		if(attack.target<>""){
			character:=attack.target
		}
		if(character=="" and attack.result <>""){
			character:=attack.character
		}
		characterManager:=CharacterManager.GetInstance()
		if(character<>""){
			character:=characterManager.UpdateCharacterInfoIfItIsAlreadyBeingManaged(character)
			characterManager.TargetCharacter(character) ;optimize	
				
			this.PlayDefenderAnimations(attack.ability,attack.result, character, attack.effects, attack.Knockbac)

		}
		characterManager.DontUseMemoryWhenTargeting:=false
		this.COmmandParser.QueueCommands:=false
		this.COmmandParser.SubmitCommand()
	}
	PlayDefenderAnimations(ability, result,target, effects,knockback){  
		this.PlayHitOrMissAnimations(ability, result,target)
		knock:=this.PlayKnockbackAnimation(knockback, target)
		if(knock==false){
			this.PlayStunned(Effects)
			this.PlayEffects(Effects)
		}
		this.COmmandParser.QueueCommands:=false
		this.COmmandParser.SubmitCommand()
	}
		
	PlayHitOrMissAnimations(ability, result, target){
		Logger.log("PlayHitOrMissAnimations", {name:""})
		if(result=="hit"){
			if(ability.name<>""){
				customHitKey:= ability.Name . "Hit"
			}else
			{
				customHitKey:= ability . "Hit"
			}
			customHit:=this.Animations[customHitKey]
			if(customHit<>"")
				this.playAnimation(customHitKey, target._Name)
			else
			{
				this.PlayAnimationFor(target, result)
			}
		}
		else
		{
			this.PlayAnimationFor(target , "miss")
		}
	}
	PlayStunned(Effects){
		stunned:=false
		for key, effect in Effects{
			if(effect=="Unconscious" or effect="Unconsious" or effect=="dead" or effect=="dying" ){
				return
			}
			if(effect=="Stunned"){
				stunned:=true
			}
			
		}
		if(stunned){
			this.playAnimation("Stunned")
		}
	}
	PlayEffects(Effects){
		
		effectToPlay:=""
		for key, effect in Effects{
			if(effect=="dead"){
				effectToPlay:=effect
				this.playAnimation(effect)
				return
			}
		}
		for key, effect in Effects
		{
			if(effect=="dying" ){
				effectToPlay:=effect
				this.playAnimation(effect)
				return
			}
		}
		for key, effect in Effects
		{               
			if(effect=="Unconscious" or effect="Unconsious" ){
				effectToPlay:=effect
				this.playAnimation("Unconsious") ;test
				return
			}
		}
		

	}
	PlayKnockbackAnimation(knockBack, target){
		if(knockback[1] <>""){
			knockBack:=knockback[1]
		}
		player:= new Player()
		ground:= player.Y
		if(knockBack.effect="knockeddown"){
			;this.playAnimation("Knockback") ;optimize?
			target.ActiveMovement:= target.Movements.Knockback 
			knockbackMovement:=target.ActiveMovement
			knockbackMovement.Increment("Back", .1, ground)
			knockbackMovement.Increment("Down", 2, ground)
			knockbackMovement.Increment("Still", 1, ground)
			this.playAnimation("knockeddown")	
			return true
		}
		if(knockBack.effect="knockedback"){
			;this.playAnimation("Knockback") ;optimize?
			target.ActiveMovement:= target.Movements.Knockback 
			knockbackMovement:=target.ActiveMovement
			
			knockbackMovement.MoveCharacterAccordingToInstructions("Back", knockBack.distance, 0)
			knockbackMovement.MoveCharacterAccordingToInstructions("Down", 2, ground)
			knockbackMovement.MoveCharacterAccordingToInstructions("Still", 1, ground)
			return true
		}
	}
	

	
	WriteToFile(){
		ErrorLevel := StrObj(this._characterAnimations, this.Path)
		if (errorLevel >0)
			MsgBox % "did not write "
		
	}
	
	AddOrEditAndPlayAnimation(key, mov){
		movObj:=StrObj(mov)
		for key, val in movObj{
			if(val.Sequence <> ""){
				structured_mov := val
			}
		}
		if (structured_mov==""){
			structured_mov:= { Sequence:"", NumKey:0, movs:[mov]}
		}
		this._characterAnimations[key]:= structured_mov
		this.PlayStructuredMov(structured_mov)
		this.WriteToFile()
	}
	FinishEvaluatingMovAndAddAsAnimation(){
		this.AnimimationListCounter++
	}
	EvaluateMovAsAnAnimation(mov){
		if (this.Path=="")
			this.LoadLocalAnimations("Temporary")
		key:=Substr( this.Path, 1 ,2) . this.AnimimationListCounter
		this.AddOrEditAndPlayAnimation(key, mov)
		return key
	}
}











