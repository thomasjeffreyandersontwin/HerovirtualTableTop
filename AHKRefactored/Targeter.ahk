#Include ClassMemory.ahk
class Targeter{
	__New(){
		this.InitFromCurrentlyTargetedModel()
	}
	InitFromCurrentlyTargetedModel(){
		this.cohMemory:= new _ClassMemory("ahk_exe cityofheroes.exe", "", hProcessCopy)
		this.targetMemoryAddress:=0x00F14FB0
		this.targetPointer := this.cohMemory.read(this.targetMemoryAddress, "UInt")
	}
	Label{
		Get{
				value:= this.GetTargetAttribute(12740,"string")
				return value
		}
	}
	Target(){
		this.cohMemory.write(this.targetMemoryAddress,this.targetPointer, "UInt")
	}
	GetTargetAttribute( offset,varType="Float"){
		targetAttributeAddress:=this.targetPointer+offset
		value:= this.GetAttributeFromAddress(targetAttributeAddress,varType)
		return value
    }
	GetAttributeFromAddress(address, varType){
		
		address:=this.cohMemory.Fhex(address)
		if(varType=="Float"){
			SetFormat, float, 4.9
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
	Position{
		Get{
			p:=new Position(this)
			return p
		}
		Set{
			p:=new Position(this)
			p.X:=value.x
			p.y:=value.y
			p.z:=value.z
		}
	}
}

Class Position{
	__New(targeter){
		this.Targeter:=targeter
	}
	X{
		Set{
				this.targeter.SetTargetAttribute(92, value)
		}
		Get{
				return this.targeter.GetTargetAttribute( 92)
		}
	}
	Y{
		Set{
				this.targeter.SetTargetAttribute(96, value)
		}
		Get{
				return this.targeter.GetTargetAttribute( 96)
		}
	}
	Z{
		Set{
				this.targeter.SetTargetAttribute(100, value)
		}
		Get{
				return this.targeter.GetTargetAttribute( 100)
		}
	}
	Duplicate{
		Get{
			clone:= {X:this.X, Y:this.Y, Z:this.Z}
			return clone
		}
	}
}
	