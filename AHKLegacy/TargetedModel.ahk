#include classMemory.ahk
#Include String-object-file.ahk

Class Target
{
	
	static _LastSelectedTarget=""
	static FacingByMatrixKey:={	"0.927183867":22, "0.707106769":45, "0.390731066":67, "-0.000000044":90, "-0.374606490":112, "-0.707106769":135, "-0.920504868":157, "-1.000000000":180, "-0.927183807":202, "-0.707106829":225, "-0.390731215":247, "0.000000012":270, "0.374606371":292, "0.707106650":315, "0.920504689":337, "1.000000000":360}


	static rotationMatrixForEachFacing:=""
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
	Pitch:=0
	
	Name{
		Set{
				this.SetTargetAttribute(12740,value,"string")
		}
		Get{
				value:= this.GetTargetAttribute(12740,"string")
				return value
		}
	}
	
	RotationMatrix{
		Set{
				
				this.SetTargetAttribute(56, value[1])
				this.SetTargetAttribute(64, value[2])
				this.SetTargetAttribute(80, value[3])
				this.SetTargetAttribute(88, value[4])
			}
			
		Get{
			first:= this.GetTargetAttribute( 56)
			second:= this.GetTargetAttribute( 64)
			third:= this.GetTargetAttribute( 80)
			fourth:= this.GetTargetAttribute( 88)
			return [first, second, third, fourth]
		
		}
			
	}
	RotationMatrixKey{
		Get{
			matrix:=this.RotationMatrix
			return matrix[4] 
		}
	}
	
	
	Facing{
		Get{
			matrixKey:=this.RotationMatrixKey
			facing:=this.FacingByMatrixKey[matrixKey]
			if(facing==""){
				facing:= this.findClosestFacing(matrixKey)
			}
			return facing
		}
		Set{
			matrix:=this.rotationMatrixForEachFacing[value]
			if(matrix <> ""){
				this.RotationMatrix:= matrix
			}
		}
		
	}
	findClosestFacing(matrixKey){
		minDifference:=1
		for key, candidateFacing in this.FacingByMatrixKey{
			difference:= abs(matrixKey - key)
			if(difference < minDifference){
				minDifference:=difference
				facing:= candidateFacing
			}
		}
		return facing
	}
	IncrementFacing(direction){
		facing:=this.Facing
		_enum := this.rotationMatrixForEachFacing._NewEnum()
		while _enum.Next(Key, matrix)
		{
			counter++
			if(key== facing){
				if(direction=="TurnLeft" or direction=="Left"){
					if(counter > 1){
						match:= lastMatrix
						break
					}
					else{
						match:= this.rotationMatrixForEachFacing[360]
						break
					}
				}
				if(direction=="TurnRight" or direction=="Right"){
					if(counter < 16){
						_enum.Next(Key, matrix)
						match:= matrix
						break
					}
					else{
						match:= this.rotationMatrixForEachFacing[22]
					break
					}
				}
			}
			lastMatrix:= matrix
		}
		this.RotationMatrix:=match
			
	}
	IncrementPitch(direction){
		if(direction=="TurnDown"){
			
			if (this.pitch > -90 ){
				this.pitch:=this.pitch - 5
			}
		}
		if(direction=="TurnUp")
		{
			if(this.pitch < 90){
				this.pitch:=this.pitch + 5
			}
		}
	}

	_name{
		get{
			return this.name
		}
	}

	__New(){
		
		this.InitRotationMatrix()
		
		this.cohMemory:= new _ClassMemory("ahk_exe cityofheroes.exe", "", hProcessCopy)
		this.targetMemoryAddress:=0x00F14FB0
		this.targetPointer := this.cohMemory.read(this.targetMemoryAddress, "UInt")
	}
	
	InitRotationMatrix(){
		
			this.rotationMatrixForEachFacing:={}
			matrixForFacing:= [ 0.927183867,	-0.37460658,	0.37460658,	0.927183867]
			this.rotationMatrixForEachFacing[22]:=matrixForFacing
			
			matrixForFacing:= [ 0.707106769,	-0.707106769,	0.707106769,	0.707106769]
			this.rotationMatrixForEachFacing[45]:=matrixForFacing
			
			matrixForFacing:=[ 0.390731067,	-0.920504868,	0.920504868,	0.390731067]
			this.rotationMatrixForEachFacing[67]:=matrixForFacing
			
			matrixForFacing:= [ -0.0000000437,	-1,	1,	-0.0000000437]
			this.rotationMatrixForEachFacing[90]:=matrixForFacing
			
			matrixForFacing:= [ -0.37460649,	-0.927183867,	0.927183867,	-0.37460649]
			this.rotationMatrixForEachFacing[112]:=matrixForFacing
			
			matrixForFacing:= [ -0.707106769,	-0.707106769,	0.707106769,	-0.707106769]
			this.rotationMatrixForEachFacing[135]:=matrixForFacing
			
			matrixForFacing:= [ -0.920504868,	-0.390731156,	0.390731156,	-0.920504868]
			this.rotationMatrixForEachFacing[157]:=matrixForFacing
			
			matrixForFacing:= [ -1,	0.0000000874,	-0.0000000874,	-1]
			this.rotationMatrixForEachFacing[180]:=matrixForFacing
			
			matrixForFacing:= [ -0.927183807,	0.374606639,	-0.374606639,	-0.927183807]
			this.rotationMatrixForEachFacing[202]:=matrixForFacing
			
			matrixForFacing:= [ -0.707106829,	0.70710671,	-0.70710671,	-0.707106829]
			this.rotationMatrixForEachFacing[225]:=matrixForFacing
			
			matrixForFacing:= [ -0.390731216,	0.920504808,	-0.920504808,	-0.390731216]
			this.rotationMatrixForEachFacing[247]:=matrixForFacing
			
			matrixForFacing:= [ 0.0000000119,	1,	-1,	0.0000000119]
			this.rotationMatrixForEachFacing[270]:=matrixForFacing
			
			matrixForFacing:= [ 0.374606371,	0.927183926,	-0.927183926,	0.374606371]
			this.rotationMatrixForEachFacing[292]:=matrixForFacing
			
			matrixForFacing:= [ 0.374606371,	0.707106888,	-0.707106888,	0.70710665]
			this.rotationMatrixForEachFacing[315]:=matrixForFacing
			
			matrixForFacing:= [ 0.920504689,	0.390731514,	-0.390731514,	0.920504689]
			this.rotationMatrixForEachFacing[337]:=matrixForFacing
			
			matrixForFacing:= [ 1,	-0.000000175,	0.000000175,	1]
			this.rotationMatrixForEachFacing[360]:=matrixForFacing

	}
	
	TargetMe(){
		this.cohMemory.write(this.targetMemoryAddress,this.targetPointer, "UInt")
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
		clone:= {name:this.Name,_name:this.name, X:this.X, Y:this.Y, Z:this.Z, pitch:this.pitch, roll:this.roll, yaw:this.yaw , Facing:this.Facing}
		return clone
	}
	MoveTo(location){
		clone:=this.GetValueObject()
		this.Z:=location.Z
		this.Y:=location.Y
		this.X:=location.X
		this.Pitch:= location.Pitch
		this.Facing:= location.Facing
	}
	
	UpdateLocation(locatable){
		locatable.x:=this.x
		locatable.y:=this.y
		locatable.z:=this.z
		locatable.Facing:= this.Facing
		locatable.Pitch:= this.Pitch
		return locatable
	}
	
	CalculateRelativeLocation(locatable){
		pos:= new 3dPositioner()
		return pos.CalculateAbsoluteDelta(this, locatable)
	}
}


   

Class Player extends Target
{
	__New(){
	
		this.cohMemory:= new _ClassMemory("ahk_exe cityofheroes.exe", "", hProcessCopy)
		this.InitRotationMatrix()
		entityAddress:=0x00CAF580
		
		;2160D020
		entityPointer := this.cohMemory.read(entityAddress, "UInt")
		entityPointer:=this.cohMemory.Fhex(entityPointer)
		;2160DE20
		entityStructureAddress:=entityPointer + 0x0e00
		entityStructureAddress:=this.cohMemory.Fhex(entityStructureAddress)
		;3F886CC0
		entityStructurePointer:= this.cohMemory.read(entityStructureAddress, "UInt")
		this.entityStructurePointer:=this.cohMemory.Fhex(entityStructurePointer)
		
		this.targetMemoryAddress:=0
		this.targetPointer := 0
	}
	
	GetStructureAttribute( offset,varType="Float"){
		targetAttributeAddress:=this.entityStructurePointer+offset
		value:= this.GetAttributeFromAddress(targetAttributeAddress,varType)
		return value
    }
	SetStructureAttribute( offset,value, varType="Float"){
		targetAttributeAddress:=this.entityStructurePointer+offset
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
	X{
		Set{
				this.cohMemory.write(0x1671544, value,"float")
				this.cohMemory.write(0x1671544, 47.149131775 ,"float")
		}
		Get{
				return this.GetTargetAttribute( 23532868)
		}
	}
	Facing{	
		Get{
		
			targetIcon:=characterManager.GetInstance().Characters["Targeter"]
			characterManager.GetInstance().TargetCharacter(targetIcon ,false)
			t:= targetIcon.MemoryInstance
			return t.facing
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
	CurrStun{
		Get{
			value:= this.GetStructureAttribute(244)
			return value
		}
		
		Set{
			if(value < 5)
				value:=5
			this.SetStructureAttribute(244,value)
		}
		
	}
	
	MaxStun{
		Get{
			value:= this.GetStructureAttribute(1164)
			return value
		}
		
		Set{
			if(value < 5)
				value:=5
			this.SetStructureAttribute(1164,value)
		}
		
	}
	
	CurrEndurance{
		Get{
			value:= this.GetStructureAttribute(252)
			return value
		}
		
		Set{
			if(value < 5)
				value:=5
			this.SetStructureAttribute(252,value)
		}
	}
	MaxEndurance{
		Get{
		if(value < 5)
				value:=5
			value:= this.GetStructureAttribute(1172)
			return value
		}
		
		Set{
			this.SetStructureAttribute(1172,value)
		}
	}
	

}

