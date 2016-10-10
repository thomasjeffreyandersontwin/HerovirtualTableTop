#Include String-object-file.ahk
class Logger{
static items:=[]
static doLog:= false
	Log(identifier, data){
		if (Logger.doLog==true){
			data.key:=identifier
			Logger.items.Insert(data)
			StrObj(Logger.items, "log\logData.txt")
		}
	}

	category{
		set{
			if (Logger.doLog==true){
				if(Logger.category<>value){
				Logger.log(value, "______________")
				}
				Logger.method:=value
			}
		}
	}

	Reset()
	{
		Logger.items:=[]
		StrObj(Logger.items, "log\logData.txt")
	}
}

Logger.category:="done"
Logger.log("test1", {name:"aaa"})
Logger.log("test2", {name:"bbb"})
Logger.log("test3", {name:"ccc"})

Logger.Reset()


