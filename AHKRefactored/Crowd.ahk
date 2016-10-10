#include ManagedCharacter.ahk

Class CrowdMembership extends ManagedCharacter {
	_crowd:=""
	SavedPosition:=""
	Character:=""
	
	__New(name, surface:="", skinType:="", crowd:=""){
		this.init(name, surface, skintype)
		this._crowd:=crowd
		this.name:=name
	}
	Name{
		Set{
			this.UpdateOriginalName()
			this._Name:= value
			this._crowd.AddMember(this)
		}
	}	
	Place(){
		this.COHPlayer.Position:=this.SavedPosition
	}
	StoreCurrentPosition(){
		pos:=this.COHPlayer.Position
		this.SavedPosition:=pos.Duplicate
	}
	
	CrowdName{
		Get{
			return this.Crowd.Name
		}
	}
	CharacterName{
		Get{
			return this.Name
		}
	}
	Crowd{
		Set{
			this._crowd:=value
			this._crowd._Members[this.Name]:= this
			this.SavedPosition:= this.COHPlayer.Position
		}
		Get{
			return this._Crowd
		}
	}
}
Class CharacterCrowd{
	_Members:={}
	Name:=""
	__New(name){
		this.Name:=name
	}
	__Call(method , params*){
		if(this[method]==""){
			members:=this._Members
			for name, member in members{
				if(member[method] <>""){
					member[method](params)
				}
			}
		}
	}
	Members[characterName]{
		Set{
			if(value.OriginalName <>""){
				memberToReplace:=this._members[value.OriginalName]
				if(memberToReplace <> ""){
					this._Members.Remove(value.OriginalName)
				}
			}
			this._Members[characterName]:=value
			value.SavedPosition:= value.COHPlayer.Position
		}
		Get{
			member:=this._Members[charactername]
			if(member<>""){
				return member
			}
			else{
				return null
			}
		}
	}
	RemoveMember(crowdMembership){
		this._Members.Remove(crowdMembership.Name)
		crowdMembership.Crowd:=null
	}
	AddMember(crowdMembership){
		crowdMembership.Crowd:=this
		return this.Members[crowdMembership.Name]
	}
	AllMembers{
		Get{
			return this._Members
		}
	}
}


