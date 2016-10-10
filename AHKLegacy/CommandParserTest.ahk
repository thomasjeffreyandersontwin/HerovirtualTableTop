#SingleInstance force
#Include Yunit\Yunit.ahk
;#Include Yunit\Window.ahk
#Include Yunit\StdOut.ahk
#Include CommandParser.ahk



class TestParserHelper{
	Parser:=""
	TestCharacter:=""
	Init(parser){
		this.TestCharacter:= this.BuildTestCharacter()
		this.Parser:=parser
		
	}
	BuildTestCharacter(){
		return {name:"TestCharacter", model:"TestModel", costume:"TestCostume"}
	}
	
	BuildTestSpawnCommand(){
		this.Parser.Build("SpawnNPC", [this.TestCharacter.Name, this.TestCharacter.Model])
	}
	BuildTargetFollowCommand(){
		this.parser.Build("TargetName", [this.TestCharacter.Name])
		this.parser.Build("Follow")
		this.Parser.SubmitCommand()
	}
	BuildLoadCostumeAndBindFileCommand(){
		this.parser.Build("LoadCostume", [this.TestCharacter.Costume])
		this.parser.Build("Bindloadfile",["c:\test\testfile.txt"])
		this.Parser.SubmitCommand()
	}
}

Class BaseParserTest{
	
	Begin(){
		this.parser:= new BaseCommandParser
		this.Helper:=new TestParserHelper
		this.Helper.Init(this.parser)
	}
	
	TestBuild_CreatesProperCommandString()
	{
		this.Helper.BuildTestSpawnCommand()
		valid:="spawn_npc " . this.helper.TestCharacter.Name . " " . this.helper.TestCharacter.Model
		actual:=this.Parser.UnprocessedCommands
		Yunit.AssertEquals(valid, actual)
	}
	TestSubmit_SurroundsWithQuotes(){
		this.Helper.BuildTestSpawnCommand()
		valid:="""spawn_npc " . this.helper.TestCharacter.Name . " " . this.helper.TestCharacter.Model . """"
		actual:=this.Parser.SubmitCommand()
		Yunit.AssertEquals(valid, actual)
	}	
	TestSubmit_MultipleCommandsAppend(){
		this.helper.BuildTargetFollowCommand()
		actual:=this.parser.UnprocessedCommands
		valid:= """target_name" . this.TestCharacter.Name . "$$follow"""
	}
}

Class KeyBindFileParserTest{
	Begin(){
		this.Helper:=new TestParserHelper
		this.parser:= CommandParserFactory.NewKeybindFileParser(TestParserHelper.BuildTestCharacter(),"TestPurpose")
		
		this.Helper.Init(this.parser) 
	}
		
	TestKeyBindFile_MatchesCharAndPurpose(){
		actual:= this.parser.KeyBindFile
		valid:= StrReplace(this.helper.TestCharacter.Name, " ", "_") . "_" . this.parser.Purpose
		Yunit.AssertEquals(valid, actual)


}
	TestSubmit_StoresKeySpecificCommands(){
		this.parser.Key:="A"
		this.helper.BuildTargetFollowCommand()
		this.parser.Key:="B"
		this.helper.BuildLoadCostumeAndBindFileCommand()
		
		actual:= this.Parser.KeyBinds["A"]
		valid:="""" . "target_name " . this.helper.TestCharacter.Name . "$$follow"""
		Yunit.AssertEquals(valid, actual)
		
		actual:= this.Parser.KeyBinds["B"]
		valid:="""" . "load_costume " . this.helper.TestCharacter.Costume . "$$bind_load_file c:\test\testfile.txt"""
		Yunit.AssertEquals(valid, actual)
	}
	TestPublish_CreatesValidFile(){
		this.parser.Key:="A"
		this.helper.BuildTargetFollowCommand()
		this.parser.Key:="B"
		this.helper.BuildLoadCostumeAndBindFileCommand()
		this.parser.Key:="C"
		this.helper.BuildTestSpawnCommand()
		this.Parser.SubmitCommand()
		this.parser.Publish()
		validFile:=  this.parser.KeyBindFile . ".txt"
		actualFile:= FileOpen(validFile,"r")
		Yunit.Assert(actualFile<>0)
		
		actualLines:=0
		validLines:=3
		loop{
			line:=actualFile.ReadLine()
			
			if(line==""){
				break
			}
			actualLines++
		}
		
		Yunit.Assert(actualLines, validLines)
		actualFile:= FileOpen(validFile,"r")
		
		actual:= trim actualFile.ReadLine()
		valid:="A """ . "target_name " . this.helper.TestCharacter.Name . "$$follow"""
		found:= InStr(actual, valid )
		pass:= (found <> 1)
		Yunit.Assert(found)
		
		actual:=actualFile.ReadLine()
		valid:="B """ . "load_costume " . this.helper.TestCharacter.Costume . "$$bind_load_file c:\test\testfile.txt"
		found:= InStr(actual, valid )
		pass:= (found <> 1)
		Yunit.Assert(found)
		
		actual:=actualFile.ReadLine()
		valid:="C """ . "spawn_npc " . this.helper.TestCharacter.Name . " " . this.helper.TestCharacter.Model
		found:= InStr(actual, valid )
		pass:= (found <> 1)
		Yunit.Assert(found)
	}
	TestPublish_CreatesTempKeybindFile(){	
		this.parser.Publish()
		
		actualFile:= FileOpen("..\data\B.txt","r")
		actual:= actualFile.ReadLine()
		valid:="Y """ . "bind_load_file " . this.helper.TestCharacter.Name . "\" BYBYBYBYthis.parser.KeyBindFile . ".txt"""
		Yunit.AssertEquals(valid, actual)BY
	}
}

Class TempKeyBindParserTest{
	Begin(){
		this.Helper:=new TestParserHelper
		this.parser:= CommandParserFactory.NewTempKeybindFileParser()
		this.Helper.Init(this.parser) 
	}
	
	TestSubmit_PlacesCommandInTempBindFile(){
		this.Helper.BuildTestSpawnCommand()
		this.Parser.SubmitCommand()
		actualFile:= FileOpen("C:\champions\applications\coh\data\B.txt","r")
		actual:= actualFile.ReadLine()
		valid:="Y ""spawn_npc " . this.helper.TestCharacter.Name . " " . this.helper.TestCharacter.Model . """"
		Yunit.AssertEquals(valid, actual)
	}
}

Class ChainKeybindParserTest{
	Begin(){
		this.Helper:=new TestParserHelper
		this.parser:= CommandParserFactory.NewChainedKeybindFileParser(TestParserHelper.BuildTestCharacter(),"TestPurpose")
		this.Helper.Init(this.parser)
	}
	TestSubmit_CreatesFirstChainedCommand(){
		this.parser.Key:="A"
		this.parser.Build("TargetName", [this.Helper.TestCharacter.Name])
		this.Parser.SubmitCommand()
		
		actual:=this.Parser.KeyBinds["A"][1]
		valid:="target_name " . this.helper.TestCharacter.Name 
		Yunit.AssertEquals(actual, valid)
		
	}
	
	BuildChainedCommand(){
		this.parser.Key:="A"
		this.parser.Build("TargetName", [this.Helper.TestCharacter.Name])
		this.parser.Build("Follow")
		this.Parser.SubmitCommand()
		this.parser.Build("LoadCostume", [this.Helper.TestCharacter.Costume])
		this.Parser.SubmitCommand()
		this.parser.Build("TargetEnemyNear")
		this.Parser.SubmitCommand()
	}
	BuildAnotherChainedCommand(){
		this.parser.Key:="B"
		this.parser.Build("TargetName", [this.Helper.TestCharacter.Name])
		this.parser.Build("Rename")
		this.Parser.SubmitCommand()
		this.parser.Build("SpawnNpc", [this.Helper.TestCharacter.Costume])
		this.parser.Build("TargetEnemyNear")
		this.Parser.SubmitCommand()
	}
	TestSubmit_CreatesAdditionalChainedLink(){
		this.BuildChainedCommand()
		
		actual:=this.Parser.KeyBinds["A"][1]
		valid:= "target_name " . this.helper.TestCharacter.Name . "$$follow"
		Yunit.AssertEquals(actual, valid)
		
		actual:=this.Parser.KeyBinds["A"][2]
		valid:="load_costume " . this.helper.TestCharacter.Costume
		Yunit.AssertEquals(actual, valid)
		
		actual:=this.Parser.KeyBinds["A"][3]
		valid:="target_enemy_near"
		Yunit.AssertEquals(actual, valid)
	}
	
	TestPublish_BuildBaseAndChainedBindFiles(){
		this.BuildChainedCommand()
		this.BuildAnotherChainedCommand()
		
		this.Parser.Publish()
		validFile1:=this.Parser.KeyBindFile . ".txt"
		validFile2a:=this.Parser.KeyBindFile . "A_2" .  ".txt"
		validFile2b:=this.Parser.KeyBindFile . "B_2" .  ".txt"
		
		
	
		valid:="A """ . "target_name " . this.helper.TestCharacter.Name . "$$follow$$bind_load_file " . validFile2a . """`n"
		FileRead actual, % validFile1
		pos:= InStr(actual, valid )
		Yunit.Assert(pos <> 1)
		
		validFile3a:=this.Parser.KeyBindFile . "A_3" .  ".txt"
		valid:="A """ . "load_costume " . this.helper.TestCharacter.Costume . "$$bind_load_file " . validFile3a . """"
		FileRead actual, % validFile2a
		Yunit.AssertEquals(actual, valid)
		
		valid:="A """ . "target_enemy_near$$bind_load_file " . validFile1 . """"
		FileRead actual, % validFile3a
		Yunit.AssertEquals(actual, valid)
		
		
		valid:= "B """ . "target_name " . this.helper.TestCharacter.Name . "$$rename$$bind_load_file " . validFile2b
		FileRead actual, % validFile1
		pos := InStr(actual, valid )
		Yunit.Assert(pos <> 1)
		
		validFile2b:=this.Parser.KeyBindFile . "B_2" .  ".txt"
		valid:="B """ . "spawn_npc " . this.helper.TestCharacter.Costume . "$$target_enemy_near$$bind_load_file " . validFile1 . """"
		FileRead actual, % validFile2b
		Yunit.AssertEquals(actual, valid)
		
		
		
		
	}
			
}

Yunit.Use(YunitStdOut).Test(TempKeyBindParserTest, ChainKeybindParserTest,BaseParserTest, KeyBindFileParserTest )


