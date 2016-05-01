#Include String-object-file.ahk
#Include CommandParser.ahk
Class InstanceManager{
	Directory:="..\data\\"
	Path:=""
	_instanceData:=""
	_thename:=""
	static manager:=""
	CommandParser:= CommandParserFactory.NewTempKeybindFileParser()
	
	DeleteActiveData(){
		Try{
			FileDelete this.Path
		}
		catch
		{}
	}
	
	_dataType{
		Get{
			return this.DataType
		}
	}
	
	GetInstance(){
		if( this.manager==""){
			this.manager:= this.NewInstance
			this.Manager.Init()
			this.manager.LoadData()
		}
		return this.manager
	}
	BuildPath(file){
		path:=this.Directory . file . "." . this._dataType
		return path
	}
	ReleaseFromMemory(){
		this.KillInstance()
	}
	WriteToFile(){
		ErrorLevel := StrObj(this._instanceData, this.Path)
		if (errorLevel >0)
			MsgBox % "did not write "
		
	}
	LoadData(){
		this._instanceData:= StrObj(this.Path) 
		
	}
	File{
		Set
		{
			this.Path:= this.BuildPath(value)
			this._thename:=value
			ErrorLevel := StrObj(value, this.DataType . "File" )
		}
		Get 
		{
			return StrObj(this.Path, this.DataType . "File")
		}
	}
}	
