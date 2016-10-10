#include classMemory.ahk
#Include String-object-file.ahk

Class Target
{
	static _LastSelectedTarget=""
	
	LastSelectedTarget{
		get{
			value:=StrObj("SelectedTarget")
			value:=value.SelectedTarget
			return value
			
		}
		
		set{
			StrObj({SelectedTarget:value}, "SelectedTarget")
		}
	}
	TargetChanged{
		get{
			last:=this.LastSelectedTarget.Name
			current:=this.Name
			if(last <> current){
				this.LastSelectedTarget:=this.GetValueObject()
				return true
			}	
			else
			{
				return false
			}
			
		}
	}
	X{
		Set{
				this.SetTargetAttribute(92, value)
		}
		Get{
				return this.GetTargetAttribute( 92)
		}
	}
	Y{
		Set{
				this.SetTargetAttribute(96, value)
		}
		Get{
				return this.GetTargetAttribute( 96)
		}
	}
	Z{
		Set{
				this.SetTargetAttribute(100, value)
		}
		Get{
				return this.GetTargetAttribute( 100)
		}
	}
	Name{
		Set{
				this.SetTargetAttribute(12740,value,"string")
		}
		Get{
				value:= this.GetTargetAttribute(12740,"string")
				return value
		}
	}
	
	
	Perspective{
		Get{
			perspective:={yaw:0, pitch:0}
			yaw:=this.Yaw
			roll:=this.roll
			pitch:=this.pitch
			
			perspective.pitch:= pitch
			perspective.yaw:=yaw
				
			if(pitch==360)
				pitch=0
			if(pitch==270)
				pitch:= -90
			if(pitch >-1 and pitch < 89 and yaw > -1 and yaw <89){
				perspective.pitch:= pitch
				perspective.yaw:=yaw
			}
			else
			{
				if( pitch > -181 and  pitch <-90){
					perspective.pitch:= 180 + pitch
					perspective.yaw:= 180 - yaw
				}
				else
					if(pitch <1 and pitch > - 89){
						perspective.pitch:= 360 +pitch
						if(yaw < 89 and yaw >=0){
							perspective.yaw:=yaw
						}
						else {
							if(yaw < 0){
								perspective.yaw:=360+yaw
							}
						}
					}
				else{
					if(pitch => 0 and yaw <=0){
						perspective.pitch:= pitch
						perspective.yaw:= 360 + yaw
					}
				}
				if(pitch== 0 and roll==0 and yaw > 0){
					perspective.yaw:=yaw
				}
			}
			if(roll==180){
				perspective.pitch := 180 + pitch
				perspective.yaw:= 180 +(yaw*-1)
			}
 			return perspective
		}
		
		Set{
			this.yaw:=value.yaw
			this.roll:= 0
			this.pitch:=value.pitch
		}
		

	}
	Facing{
		Get{
			yaw:=this.Yaw
			roll:=this.roll
			if(yaw>0){
				if(roll==0){
					return yaw
				}
				else{
					if(roll==180){
						return roll - yaw
					}
				}
			}
			else{
				if(yaw<=0){
					if(roll==180){	
						return roll + (yaw*-1)
					}
					else{
						if(roll==0){
							return 360 + yaw
						}
					}
				}
			}
		}
	}
			
	_name{
		get{
			return this.name
		}
	}
	Roll{
		get{
			return this.cohMemory.readString(0x567B9658, 40)
		}
		set{
			return this.cohMemory.writeString(0x567B9658, value)
		}
	}
			
	Yaw{
		get{
			return this.cohMemory.readString(0x42AFDD66, 40)
		}
		set{
			return this.cohMemory.writeString(0x42AFDD66, value)
		}
	}
	Pitch{
		get{
			return this.cohMemory.readString(0x42AFE12E, 40)
		}
		set{
			return this.cohMemory.writeString(0x42AFE12E, value)
		}
	}
	UpdateYPR(){
		WinActivate ahk_class CrypticWindow
		WinGetPos, x, y, width, height, ahk_class CrypticWindow
		;PixelSearch,x,y,800,100,2000, 1000 ,0x000000 , 2,fast,RGB

		x:= (width * 0.977799228)
		if(height <1500) {
			y:= (height * 0.151) 
		}
		else{
			y:= (height * 0.125) 
	}





		;x:=1602 
		;y:=170 
		;MouseMove, x,y
		click %x%,%y%, 9

;sleep 100
	send {Enter}
	;click 1602,400, 9
	}
	__New(){
	
		this.cohMemory:= new _ClassMemory("ahk_exe cityofheroes.exe", "", hProcessCopy)
		this.targetMemoryAddress:=0x00F14FB0
		this.targetPointer := this.cohMemory.read(this.targetMemoryAddress, "UInt")
	}
	
	GetAttributeFromAddress(address, varType){
		address:=this.cohMemory.Fhex(address)
		if(varType=="Float"){
			value:= this.cohMemory.read(address, "float")
			return value
		}	
		else{
			if(varType=="string"){
				value:=  this.cohMemory.readString(address, 40)
				return value
			}
			
		}
	}
	GetTargetAttribute( offset,varType="Float"){
		targetAttributeAddress:=this.targetPointer+offset
		value:= this.GetAttributeFromAddress(targetAttributeAddress,varType)
		return value
    }
	SetTargetAttribute( offset,value, varType="Float"){
		targetAttributeAddress:=this.targetPointer+offset
		targetAttributeAddress:=this.cohMemory.Fhex(targetAttributeAddress)
		if(varType=="Float"){
			return this.cohMemory.write(targetAttributeAddress, value,"float")
		}	
		else{
			if(varType=="string"){
				return this.cohMemory.writeString(targetAttributeAddress,value)
			}
		}
	}
	GetValueObject(){
		clone:= {name:this.Name,_name:this.name, X:this.X, Y:this.Y, Z:this.Z, pitch:this.pitch, y:this.roll, yaw:this.yaw}
		return clone
	}
	MoveTo(location){
		this.X:=location.X
		this.Y:=location.Y
		this.Z:=location.Z
	}
	
	UpdateLocation(locatable){
		locatable.x:=this.x
		locatable.y:=this.y
		locatable.z:=this.z
	}
		
}






Class Player extends Target
{
	__New(){
	
		this.cohMemory:= new _ClassMemory("ahk_exe cityofheroes.exe", "", hProcessCopy)
		this.targetMemoryAddress:=0
		this.targetPointer := 0
	}
	
	X{
		Set{
				this.SetTargetAttribute(23532868, value)
		}
		Get{
				return this.GetTargetAttribute( 23532868)
		}
	}
	Y{
		Set{
				this.SetTargetAttribute(23532872, value)
		}
		Get{
				return this.GetTargetAttribute( 23532872)
		}
	}
	Z{
	Set{
				this.SetTargetAttribute(23532876, value)
		}
		Get{
				return this.GetTargetAttribute( 23532876)
		}
	}
	Name{
		Get{
			return ""
		}
	}
}

