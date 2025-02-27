#Include CharacterRepository.ahk

Class Event{
	Name:=""
	SubjectType:=""
	Method:=""
	Repository:=""
	ActivateOnKey:=""
	__New(name, subjectType, method, repository, activateOnKey:=""){
		this.Name:=name
		this.SubjectType:=subjectType
		this.Method:= method
		this.Repository:= repository
		this.ActivateOnKey:=activateOnKey
	}
	Handle(subject, params*){
		subject[this.Method](params*)
	}
	HandleWithTargeted(params*){
		subject:= this.Repository.Targeted
		subject[this.Method](params*)
	}
	HandleWithSubmittedInfo(info,params*){
		updateMethod:="Update" . this.SubjectType . "FromInfo"
		repository:=this.Repository
		subject:= repository[updateMethod](info)
		subject[this.Method](params*)
	}
	
}
Class HeroVirtualTableTopEventHandler{
	static _instance
	EventsByKeyActivated:={}
	EventsByName:={}
	AddEvent(event){
		activationKey:=event.ActivateOnKey
		this.EventsByKeyActivated[activationKey]:= event
		name:=event.Name
		this.EventsByName[name]:= event
	}
	ListenForFileBasedEvent(){
		eventInfo:= StrObj("Event.Info")
		if(eventInfo<>""){
			name:=eventInfo.Name
			event:=this.EventsByName[name]
			subjectInfo:=eventInfo.Subject
			parameters:=[]
			for key, para in eventInfo.Parameters{
				parameters.Push(para)
			}
			event.HandleWithSubmittedInfo(subjectInfo, parameters*)
		}
	}
	ListenForKeyStrokes(){
		Hotkey, IfWinActive, ahk_class CrypticWindow
		for key, event in this.EventsByKeyActivated{
			func:=ObjBindMethod(this, "HandleKey",key)
			Hotkey, %key% , %func%, On
		}
	}
	StopListeningForKeyStrokes(){
		for key, event in this.EventsByKeyActivated{
			func:=ObjBindMethod(this, "HandleKey",key)
			Hotkey, %key% ,, Off
		}
	}
	HandleKey(key){
		event:= this.EventsByKeyActivated[key]
		event.HandleWithTargeted()
	}
	
	GetInstance(){
		if(this._instance==""){
			this._instance:= new HeroVirtualTableTopEventHandler()
		}
		return this._instance
	}
}