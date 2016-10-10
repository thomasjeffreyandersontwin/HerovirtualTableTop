Class ButtonCOntrol extends UIControl{
	__New(variable, eventTarget:="", ui:="", renderPosFunc:=""){
		this.Variable:=variable
		this._UI:=ui
		this.ControlType:="Button"
		this.Event:=variable
		this.EventTarget:= eventTarget
		if(renderPosFunc<>""){
			this.RenderPos:=renderPosFunc
		}
	}
	
}
Class UIControl{
	Variable:=""
	_UI:=""
	EventTarget:=""
	controlType:=""
	__New(variable, controlType, event:="", eventTarget:="", ui:="",renderPosFunc:=""){
		this.Variable:=variable
		this._UI:=ui
		this.ControlType:=controlType
		this.Event:=event
		this.EventTarget:= eventTarget
		if(renderPosFunc<>""){
			this.RenderPos:=renderPosFunc
		}
	}
	UI{
		Get{
			ui:=this._Ui 
			if(ui <>""){
				ui:=Ui . ":"
			}
			return ui
		}
		Set{
			this._ui:=ui
		}
	}
	Value{
		Set{
			local dummy
			variable:=this.Variable
			ui:= this.UI
			GuiControlGet, oldValue,%ui%, %variable%
			if (oldValue <> value){
				GuiControl ,%ui%, %variable%, %value%
			}
		}
		Get{
			local dummy
			variable:=this.Variable
			ui:= this.UI
			GuiControlGet, variable,%ui%, %variable%
			return variable
		}
	}
	RenderPos(groupStarting){
		if(groupStarting== true){
			pos:="yp+15 xp+10"
		}
		else{
			pos:="xs y+0"
		}
	}
	Render(value:="",GroupStarting:="" ,para*){
		local dummy
		pos:=this.RenderPos(groupStarting)
		if(value==""){
			value:=this.variable
		}

		variable:=this.Variable
		ui:=this.Ui 
		controlType:=this.controlType
		Gui, %ui%Add, %controlType%, v%variable% +BackgroundTrans %pos%, %value%
		event:=this.Event
		if(event <>""){
			fn:=ObjBindMethod(this.EventTarget, event, "")
			GuiControl +g, %variable%, % fn
		}
	}	
}
Class ReadWriteControl extends UIControl{
	Label:=""
	__New(variable, label:="",ui:=""){
		this.Variable:=variable
		this.Label:=label
		this._UI:=ui
		this.ControlType:=controlType
	}
	Render(GroupStarting:=""){
		local dummy
		if(GroupStarting== true){
			pos:="yp+15 xp+10"
		}
		else{
			pos:="xs y+0"
		}
		
		label:=this.label
		if(label==""){
			label := this.variable
		}
		
		variable:=this.Variable
		ui:=this.Ui 
		
		Gui, %ui%Add, Text, v%variable%Label %pos% Section +BackgroundTrans, %label%:
		Gui, %ui%Add, Text, v%variable% +BackgroundTrans x+5, No Character Selected
		Gui, %ui%Add, Edit, v%variable%Edit  xp +BackgroundTrans, No Character Selected
		GuiControl, %ui%Hide, %variable%Edit	
	}
	
	EditMode{
		Set{
			local dummy
			ui:=this.UI
			variable:=this.Variable
			if(value ==true){
				
				GuiControl ,  %UI%Hide, %variable%
				GuiControl , %UI%Show, %variable%Edit
			}
			else
			{
				GuiControl , %UI%Show, %variable%
				GuiControl , %UI%Hide, %variable%Edit
			}
		}
					
	}
	Value{
		Set{
			local dummy
			variable:=this.Variable
			ui:= this.UI
			GuiControlGet, oldValue,%ui%, %variable%
			if (oldValue <> value){
				GuiControl ,%ui%, %variable%, %value%
				GuiControl ,%ui%, %variable%Edit, %value%
			}
		}
		Get{
			local dummy
			variable:=this.Variable
			ui:= this.UI
			GuiControlGet, variable,%ui%, %variable%Edit
			return variable
		}
	}
}
Class ListBoxControl extends UIControl{		
	_List:=""
	__New(variable, event:="", eventTarget:="", ui:="",renderPosFunc:=""){
		this.Variable:=variable
		this.ControlType:="ListBox"
		this._UI:=ui
		this.Event:=event
		this.EventTarget:= eventTarget
		if(renderPosFunc<>""){
			this.RenderPos:=renderPosFunc
		}
	}
	
	getListString(list){
		for key, val in list{
			listString.= key . "|"
		}
		return listString
	}
	Render(list ,para*){
		this._list:=List
		listString:= this.getListString(list)
		base.Render(listString,para*)
	}
	
	List{
		Set{
			this._list:=value
			listString:= "|" . this.getListString(this._list)
			
			this.Value:=listString
		}
	}	
}