#Include Crowd.ahk
#Include CharacterRepository.ahk

#Include String-object-file.ahk





Class CrowdRepository{
	static _instance:=""
	_targeter:= new Targeter()
	_file:= "data\CrowdProd.data"
	Data:={}
	_Crowds:={}
	CharacterRepository:= CharacterRepository.GetInstance()
	Crowds[Name]{
		Get{
			Crowd:= this._Crowds[name]
			if(Crowd==""){
				info:=this.Data[Name]
				if(info <>""){
					Crowd:=this.BuildCrowdFromInfo(info)
					this._Crowds[name]:=Crowd
				}
			}
			return Crowd
		}
		Set{
			this._Crowds[Name]:=value
			crowdInfo:=this.buildInfoFromCrowd(value)
			this.Data[crowdInfo.Name]:=crowdInfo
			this.SaveCrowdData()
		}				
	}
	AllCrowds{
		Get{
		
			for name, info in this.Data{
				Crowd:=this.Crowds[Name]
			}
			return this._Crowds
		}
	}
	Targeted{
		Get{
			crowdMember:= this.CharacterRepository.Targeted
			crowd:=this.Crowds[crowdMember.Crowd.Name]
			if(crowd==""){
				this.Crowds[crowdMember.Crowd.Name]:=crowd
			}
			else{
				crowd.Members[crowdMember.Name]:= crowdMember
				crowdMember._Crowd:=crowd
			}
			this.SaveCrowd(crowd)
			return crowd
		}
		
	}
	buildInfoFromCrowd(Crowd){
		
		crowdInfo:={}
		crowdInfo.Members:={}
		crowdInfo.Name:=Crowd.Name
		members:=crowd.AllMembers
		for name, member in members{
			memberInfo:={}
			memberInfo.Name:=name
			memberInfo.Position:= member.SavedPosition
			crowdInfo.Members[name]:=memberInfo
		}
		return CrowdInfo
	}
	buildCrowdFromInfo(crowdInfo){
		crowd:= new CharacterCrowd(CrowdInfo.Name)
		repository:=this.CharacterRepository
		crowdMemberInfos:=crowdInfo.Members
		for name, memberInfo in crowdMemberInfos{
			character:=repository.Characters[name]
			crowd.Members[name]:=character
			character.SavedPosition:=memberInfo.Position
		}
		return crowd
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
			this._instance:= new CrowdRepository(file)
		}
		return this._instance
	}
	
	__New(file:=""){
		if(file==""){
			this.File:="Crowds"
		}
		this.File:=file
		this.LoadCrowdData()
	}
	UpdateCrowdFromInfo(CrowdInfo){
		Crowd:=this.Crowds[CrowdInfo.Name]
		changed:=false
		if(Crowd <>""){
			if(Crowd.Name <> CrowdInfo.Name and CrowdInfo.Name <>""){
				Crowd.Name := CrowdInfo.Name 
				changed:=true
			}
			if(Crowd.Crowd <> CrowdInfo.Crowd and CrowdInfo.Crowd <>""){
				Crowd.Crowd := CrowdInfo.Crowd 
				changed:=true
			}
			if(Crowd.SKin.Surface <> CrowdInfo._Skin._Surface and CrowdInfo._Skin._Surface <>""){
				Crowd._Skin._Surface := CrowdInfo._Skin._Surface 
				changed:=true
			}
			if(Crowd.Skin.Type <> CrowdInfo._Skin.Type and CrowdInfo._Skin.Type <>""){
				Crowd._Skin.Type := CrowdInfo._Skin.Type 
				changed:=true
			}
		}
		this.Crowds[CrowdInfo.Name]:= Crowd
		return Crowd
	}			
	LoadCrowdData(){
		this.Data:= StrObj(this.File) 
		this._Crowds:={}
	}
	SaveCrowdData(){
		ErrorLevel:= StrObj(this.Data, this.File)
		if (errorLevel >0)
			MsgBox % "did not write "
	}
	SaveCrowd(Crowd){
		originalName:=Crowd.OriginalName
		if(originalName <>""){
			originalCrowd:=this.Crowds[originalName]
			if(originalCrowd <> ""){
				this._Crowds.Delete(originalName)
				this.Data.Delete(originalName)
			}
		}
		this.Crowds[Crowd.Name]:=Crowd
			
	}
	DeleteCrowd(Crowd){
		this._Crowds.Delete(Crowd.Name)
		this.Data.Delete(Crowd.Name)
		this.SaveCrowdData()
	}
	NewCrowd{
		Get{
			return new CharacterCrowd("")
		}
	}
		
	Clear(){
		this._Crowds:={}
		this.Data:={}
	}
}


