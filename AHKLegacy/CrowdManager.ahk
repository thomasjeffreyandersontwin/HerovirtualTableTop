#SingleInstance force
#Include CommandParser.ahk
#Include InstanceManager.ahk
#Include TargetedModel.ahk
#Include Logger.ahk
#Include CharacterManager.ahk

Class CrowdManager extends InstanceManager{
	static manager
	;SpawnedModels:={} test
	CharacterManager:=CharacterManager.GetInstance()
			
	
	CrowdCounter:=0
	Directory:="crowds\\"
	
	LastSpawnedModel{
		Set
		{
			ErrorLevel := StrObj("LastSpawnedModel:" . value, "LastSpawnedModel")
		}
		Get
		{
			last:= StrObj("LastSpawnedModel")
			last:=last.LastSpawnedModel
			return last
		}
	}
	DataType{
		Get{
			return "crowd"
		}
	}
	
	NewInstance{
		Get{
			return new CrowdManager
		}
	}
	Init(){
		 	
	}
	UniqueNameFor(name){
		crowdNumPos := RegExMatch(name, "\(\d+\)" , mobIdentifier = "")
		if(crowdNumPos> 0){
			name:= Trim(Substr(name, 1, crowdNumPos -1 ))
		}
		coreName:=name
		while(this._instanceData[name]<>""){
			counter++
			name:= coreName " (" . counter . ")"
		}
		return name
	}
	Crowd{
		get{
		 return this._instanceData
		}
	}
	
	Crowds[key]{
		get{
			this.LoadData()
			return this._instanceData[key]
		}
		set{
			this.LoadData()
			this._instanceData[key]:=value
			this.WriteToFile()
		}
	}	
	CurrentCrowdName{
		get{
			name := StrReplace(this.Path, this.Directory, "")
			pos := InStr(name, "\",0)
			name:= StrReplace(name, ".crowd" "")
			name:=SubStr(name, pos+1, StrLen(name) - pos)
			return name 
		}
	}
	CountOfCharactersInCrowd{
	Get{
			countc:=0
			for key, val in this._instanceData{
				countc++
			}
			return countc
		}
	}
	CharactersInCrowd{
		Get{
			charactersInCrowd:={}
			for key, val in this._instanceData{
				name:=val.characterName
				charactersInCrowd[name]:= this.CharacterManager.Characters[name]
			}
			return charactersInCrowd
		}
	}
	LoadModelsForCrowdBasedOnInfo(crowdInfo){
		if(crowdInfo.Name <> "Unnamed"){
			this.file:=crowdInfo.Name
			this.LoadData()
			this.UpdateCrowdBasedOnCrowdInfo(crowdInfo)
			
			this.WriteToFile()
			return this.LoadModelsForCrowd(crowdInfo.Name)
		}
	}
	
	LoadModelsForCrowd(crowd:=""){
		if(crowd<>""){
			this.file:=crowd
		}
		this.LoadData()
		this.CrowdCounter:=0
		charactersInCrowd:={}
		for key, val in this._instanceData{
			name:=val.characterName
			character:= this.CharacterManager.Characters[name]
			if(character==""){
				charactersInCrowd[name]:=this.CharacterManager.LoadCharacter[name]
			}
			else{
				charactersInCrowd[name]:=character
			}
			this.CrowdCounter++
		}
		roster:={name:crowd, characters:charactersInCrowd}
		this.charactermanager.GenerateChainedPlacementBindFilesForRoster(roster)
		return charactersInCrowd
	}
	
	UpdateCrowdBasedOnCrowdInfo(crowdInfo){
		if(character.Crowd.Name <> "Unnamed"){
			for characterName, characterInfo in crowdInfo.Characters{
				if(this._Instancedata[characterName]==""){
					this._Instancedata[characterName]:={ characterName:characterName , crowd:crowdInfo.Name}
				}
			}
		}
	}
	LoadCrowdForCharacter(character){
		lastCrowd:= this.CurrentCrowdName
		;generate bind files for the entire crowd when selecting a target who is part of a different crowd than the 
		;previously selected target
		if(character.Crowd.Name <> "Unnamed"){
			if(lastCrowd <>  character.Crowd.Name )
			{
				this.LoadModelsForCrowd(character.Crowd.Name)
				;this.crowdmanager.GenerateBindFilesForCharacter(character.Crowd)
			}
			;update CM state to support crowd manipulation based on model selected
			this.LastSpawnedModel:=character._name
			this.File:= character.Crowd.Name
		}
	}

	SpawnNextModelFromCrowd(){
		nextPlacement:=this.getNextCrowdPlacementInfo()
		this.CrowdCounter++
		this.LastSpawnedModel:=nextPlacement.CharacterName
		
		this.CharacterManager.SpawnCharacterBasedOnPlacementToDesktop(nextPlacement)
		return nextPlacement
	}
	getNextCrowdPlacementInfo(){
		this.LoadData()
		for key, val in this._instanceData
			max++
		if(this.CrowdCounter > max)
			this.CrowdCounter:=1
		for key, val in this._instanceData{
			runner++
			if(runner == this.CrowdCounter){
				return nextModel:=val
				break
			}
				
		} 
	}
	CrowdMode{
		get{
			mode:= StrObj("CrowdMode")
			mode:=mode[""]
			return mode
		}
		Set{
			ErrorLevel := StrObj(value, "CrowdMode")
			if(value== true){
				this.LoadModelsForCrowd(this.CurrentCrowdName)
			}
		}
	}
		
	GenerateBindFilesForCharacter(character){
		mode:=this.CrowdMode
		if(mode==1){
			if(this.CurrentCrowdName <> character.Crowd.Name){
				charactersInCrowd:=this.LoadModelsForCrowd(character.Crowd.Name)
			}
			roster:={name:this.CurrentCrowdName, characters:charactersInCrowd}
			this.charactermanager.GenerateChainedPlacementBindFilesForRoster(roster)
		}
		else{
			this.CharacterManager.GenerateBindFilesWithActiveCharacter(character)
		}
	}
	RemoveModelFromDesktop(name){
		this.characterManager.RemoveCharacterFromDesktop()
		this._instanceData.Remove(name)
		this.WriteToFile()
	}
	SpawnCloneOfLastSpawnedCharacterAndAddToCurrentCrowd(){
		targetedCharacter:= this.CharacterManager.ActivateTargetedCharacter
		lastName:= targetedCharacter._Name
		
		;lastName:=this.LastSpawnedModel
		;lastCharacter:= this.CharacterManager.Characters[lastName]
		
		cloneName:=this.UniqueNameFor(lastName)
		clonedCharacterInfo:={ name:cloneName, costume: targetedCharacter.costume, crowd:targetedCharacter.crowd}
		clonedCharacter:= this.CharacterManager.SpawnCharacterBasedOnInfoToDesktop(clonedCharacterInfo)
		
		
		this._InstanceData[clonedCharacter._Name]:= { name: clonedCharacter._Name, crowd: clonedCharacter.crowd.name }
		this.WriteToFile()
		return clonedCharacter
	}	

	SpawnAndPlaceNextModelFromCrowd(dontUseMemory:=false){
		if(this.CrowdMode==true){
			for key, model in this._InstanceData{
				placementInfo:=this.SpawnNextModelFromCrowd()
				sleep 800
				this.PlaceModelAtLocation(placementInfo, dontUseMemory)
			}
		}
		else{
			placementInfo:=this.SpawnNextModelFromCrowd()
			sleep 800
			this.PlaceModelAtLocation(placementInfo)
		}
	}
	PlaceModelAtLocation(placementInfo, dontUseMemory:=false){
		character:=this.CharacterManager.Characters[placementInfo.CharacterName]
		if(placementInfo.Relative== false){
			character.MoveToLocation(placementInfo , dontUseMemory)
		}
		else{
			character.MoveDelta(placementInfo , dontUseMemory)
		}
		this.CharacterManager.CommandParser.MarkCommandComplete()
	}
	PlaceCharacterBasedOnInfo(characterInfo){
		crowd:=this.crowd
		placementInfo:=crowd[characterInfo.Name]
		if(placementInfo == ""){
			character:=CharacterManager.GetInstance().Characters[characterInfo.Name]
			placementInfo:=this.AddCharacterToCrowd(character)
		}
		this.CharacterManager.SpawnCharacterBasedOnPlacementToDesktop(placementInfo)
		this.PlaceModelAtLocation(placementInfo, true)
	}
	
	SpawnNewCharacterAndAddToCurrentCrowd(model,costume){
		if(model==""){
			name := this.UniqueNameFor(costume)
		}
		else{
			name := this.UniqueNameFor(model)
		}
		
		;write placement info
		crowd:=this.CurrentCrowdName
		crowdPlacementInfo:= {characterName:name, crowd:crowd}
		this._instanceData[name]:= crowdPlacementInfo
		this.WriteToFile()
		
		;spawn the new character
		characterInfo:= {name:name , crowd:{name:crowd}, model:model, costume:costume}
		this.CharacterManager.SpawnCharacterBasedOnInfoToDesktop(characterInfo)
		
	}
	
	AddTargetedCharacterToCrowd(){
		character:= this.charactermanager.ActivateTargetedCharacter
		this.AddCharacterToCrowd(character)
	}
	
	AddCharacterToCrowd(character){
		crowdPlacementInfo:= {characterName:character._name, crowd:this.CurrentCrowdName}
		t:= new Target()
		crowdPlacementInfo:=t.UpdateLocation(crowdPlacementInfo)
		this._InstanceData[character._name]:=crowdPlacementInfo
		this.WriteToFile()
		return crowdPlacementInfo
	}
	SaveLocationOfSelectedModel(relative:=false){
		if(this.CrowdMode==true){
			data:={}
			for key, model in this._instanceData{
				name:= model.CharacterName
				this.CharacterManager.TargetCharacterBasedOnName(name)
				targetedCharacter:= this.CharacterManager.ActivateTargetedCharacter
				crowdPlacementInfo:=this.crowds[targetedCharacter._name] 
				t:= new Target()
				if(relative==false){
					t.UpdateLocation(crowdPlacementInfo)
				}
				else{
					player:= new Player()
					delta:=t.CalculateRelativeLocation(player)
					crowdPlacementInfo.X:=delta.x
					crowdPlacementInfo.y:=delta.y
					crowdPlacementInfo.z:=delta.z
					crowdPlacementInfo.Facing:=target.facing
					crowdPlacementInfo.SourceFacing:= p.facing
					crowdPlacementInfo.Relative:= "true"
				}
				data[name]:=crowdPlacementInfo
				
			}
			this._InstanceData:=data
			this.WriteToFile()
		}
		else{
			targetedCharacter:= this.CharacterManager.ActivateTargetedCharacter
			this.LoadCrowdForCharacter()
			sleep 300
			crowdPlacementInfo:=this.crowds[targetedCharacter._name] 
			if(crowdPlacementInfo==""){
				crowdPlacementInfo:=this.AddCharacterToCrowd(targetedCharacter)
			}
			t:= new Target()
			if(relative==false){
				t.UpdateLocation(crowdPlacementInfo)
			}
			else{
				p:= new Player()
				delta:=t.CalculateRelativeLocation(player)
				crowdPlacementInfo.X:=delta.x
				crowdPlacementInfo.y:=delta.y
				crowdPlacementInfo.z:=delta.z
				crowdPlacementInfo.Facing:=target.facing
				crowdPlacementInfo.SourceFacing:= p.facing
				
				crowdPlacementInfo.Relative:= "true"
			}
			this._InstanceData[targetedCharacter._name]:=crowdPlacementInfo
			this.WriteToFile()
		}
		this.CharacterManager.CommandParser.MarkCommandComplete()
	}
	SaveLocationBasedOnCharacterInfo(characterInfo){
		relative:= characterInfo.Relative

		characterManager.GetInstance().TargetCharacterBasedOnInfo(characterInfo)
		this.SaveLocationOfSelectedModel(relative)
	}
	WriteToFile(){
		ErrorLevel := StrObj(this._instanceData, this.Path)
		if (errorLevel >0)
			MsgBox % "did not write "
		
	}
}