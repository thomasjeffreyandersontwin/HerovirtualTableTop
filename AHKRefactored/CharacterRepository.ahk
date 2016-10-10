#Include ManagedCharacter.ahk
#Include String-object-file.ahk
#Include Crowd.ahk
Class CharacterRepository{
	static _instance:=""
	_targeter:= new Targeter()
	_file:="data\CharacterProd.data"
	Data:={}
	_characters:={}
	Characters[Name]{
		Get{
			character:= this._characters[name]
			if(character==""){
				info:=this.Data[Name]
				if(info <>""){
					character:=this.BuildCharacterFromInfo(info)
					this._characters[name]:=character
				}
			}
			return character
		}
		Set{
			this._characters[Name]:=value
			characterInfo:=this.buildInfoFromCharacter(value)
			this.Data[characterInfo.Name]:=characterInfo
			this.SaveCharacterData()
		}				
	}
	AllCharacters{
		Get{
		
			for name, info in this.Data{
				character:=this.Characters[Name]
			}
			return this._characters
		}
	}
	
	buildInfoFromCharacter(character){
		characterInfo:={}
		characterInfo.Name:=character.Name
		characterInfo._Skin:={}
		characterInfo._Skin.Type:=character._Skin.Type
		characterInfo._Skin._Surface:=character._Skin._Surface
		return characterInfo
	}
	buildCharacterFromInfo(characterInfo){
		character:= new CrowdMembership(characterInfo.Name, characterInfo._Skin._Surface, characterInfo._Skin.Type)
		return character
	}
	File{
		Set{
			this._File:="data\" . value . ".data"
		}
		Get{
			return this._file
		}
	}
	GetInstance(file:=""){
		if(this._instance==""){
			this._instance:= new CharacterRepository(file)
		}
		return this._instance
	}
	
	__New(file:=""){
		if(file==""){
			this.File:="Characters"
		}
		this.File:=file
		this.LoadCharacterData()
	}
	UpdateCharacterFromInfo(characterInfo){
		character:=this.Characters[characterInfo.Name]
		changed:=false
		if(character <>""){
			if(character.Name <> characterInfo.Name and characterInfo.Name <>""){
				character.Name := characterInfo.Name 
				changed:=true
			}
			if(character.SKin.Surface <> characterInfo._Skin._Surface and characterInfo._Skin._Surface <>""){
				character._Skin._Surface := characterInfo._Skin._Surface 
				changed:=true
			}
			if(character.Skin.Type <> characterInfo._Skin.Type and characterInfo._Skin.Type <>""){
				character._Skin.Type := characterInfo._Skin.Type 
				changed:=true
			}
		}
		this.Characters[characterInfo.Name]:= character
		return character
	}			
	LoadCharacterData(){
		this.Data:= StrObj(this.File) 
		this._Characters:={}
	}
	SaveCharacterData(){
		ErrorLevel:= StrObj(this.Data, this.File)
		if (errorLevel >0)
			MsgBox % "did not write "
	}
	SaveCharacter(character){
		originalName:=character.OriginalName
		if(originalName <>""){
			originalCharacter:=this.Characters[originalName]
			if(originalCharacter <> ""){
				this._characters.Delete(originalName)
				this.Data.Delete(originalName)
			}
		}
		this.Characters[character.Name]:=character
			
	}
	DeleteCharacter(character){
		this._characters.Delete(character.Name)
		this.Data.Delete(character.Name)
		this.SaveCharacterData()
	}
	NewCharacter{
		Get{
			return new ManagedCharacter("","","","")
		}
	}
		
	Clear(){
		this._Characters:={}
		this.Data:={}
	}
	Targeted{
		Get{
			this._targeter.InitFromCurrentlyTargetedModel()
			label:= this._targeter.Label
			if(label <>""){
				name:=this.parseNameFromLabel(label)
				crowd:=this.parseCrowdFromLabel(label) 
				Targeted:=this.Characters[name]
				if(Targeted==""){
					Targeted:= new CrowdMembership(name,,, new CharacterCrowd(crowd))
					this.Characters[name]:= Targeted
				}
				else{
					Targeted.Crowd:= new CharacterCrowd(crowd)
				}
				this.LastTargeted:=Targeted
			}
			else {
				Targeted:=this.LastTargeted
			}
			return Targeted
		}
	}
	parseNameFromLabel(label){
		pos :=InStr(label, "[",0)
		if(pos >0){
			name:= SubStr(label, 1 , pos-2)
		}
		return name
	}
	parseCrowdFromLabel(label){
		pos :=InStr(label, "[",0)
		if(pos >0){
			crowd:=SubStr(label, pos+1, (StrLen(label) - pos)-1)
		}
		return crowd
	}
}
		
