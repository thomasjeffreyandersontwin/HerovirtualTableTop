CLass AAA{
	_Name:="AAAName"
	AProp:="AAA"
	BBB:="bbb"
	
	Name{
		get{
			return this._name . this.BBB.Name
		}
	}
	__New(bbb){
		this.BBB:=bbb
	}
	__Call(method , params*){
		
		if(this.BBB[method] <>"" and this[property] == ""){
			return this.BBB[method] (params)
		}
	}
	
	__Set(property, aValue){
		if(this.BBB[property] <>"" and this[property] == ""){
			return this.BBB[property]:= aValue
		}
	}
	
	 __Get(property){
		if(property <> "BBB"){
			bbb:=this.BBB
			if (ObjHasKey(bbb, property) == true and ObjHasKey(this, property) == false){
				
				return this.BBB[property]
			}
		}
		
	}
}

Class BBB{
	Name:="BBBName"
	Bname:="AAA"
}

a:=new AAA(new BBB())


Msgbox % a.AProp

msgbox % a.BName

