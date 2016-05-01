#SingleInstance force
#Include Yunit\Yunit.ahk
#Include CityOfHeroesKeybindGenerator.ahk 
class TestGeneratorHelper{
	Generator:=""
	TestCharacter:=""
	Init(generator){
		this.TestCharacter:= this.GenerateKeyBindsForEventTestCharacter()
		this.generator:=generator
		
	}
	GenerateKeyBindsForEventTestCharacter(){
		return {name:"Statesman", model:"model_Statesman", costume:"TestCostume"}
	}
	
	GenerateKeyBindsForEventTestSpawnKeybind(){
		this.generator.GenerateKeyBindsForEvent("SpawnNPC", this.TestCharacter.Model, this.TestCharacter.Name)
	}
	GenerateKeyBindsForEventTargetFollowKeybind(){
		this.generator.GenerateKeyBindsForEvent("TargetName", this.TestCharacter.Name)
		this.generator.GenerateKeyBindsForEvent("Follow")
		this.generator.CompleteEvent()
	}
	GenerateKeyBindsForEventLoadCostumeAndBindFileKeybind(){
		this.generator.GenerateKeyBindsForEvent("LoadCostume", this.TestCharacter.Costume)
		this.generator.GenerateKeyBindsForEvent("Bindloadfile","c:\test\testfile.txt")
		this.generator.CompleteEvent()
	}
}
Class BaseCityOfHeroesKeybindGeneratorTest{
	
	Begin(){
		this.generator:= new BaseCityOfHeroesKeybindGenerator()
		this.Helper:=new TestGeneratorHelper()
		this.Helper.Init(this.generator)
	}
	
	
	
	TestGenerateKeyBindsForEvent_CreatesProperKeybindString()
	{
		this.Helper.GenerateKeyBindsForEventTestSpawnKeybind()
		valid:="spawn_npc " . this.helper.TestCharacter.Model . " " . this.helper.TestCharacter.Name 
		actual:=this.generator.GeneratedKeyBindText
		Yunit.AssertEquals(valid, actual)
	}
	TestSubmit_SurroundsWithQuotes(){
		this.Helper.GenerateKeyBindsForEventTestSpawnKeybind()
		valid:="""spawn_npc" . " " . this.helper.TestCharacter.Model . " " . this.helper.TestCharacter.Name . """"
		actual:=this.generator.CompleteEvent()
		Yunit.AssertEquals(valid, actual)
	}	
	TestSubmit_MultipleKeybindsAppend(){
		this.helper.GenerateKeyBindsForEventTargetFollowKeybind()
		actual:=this.generator.GeneratedKeyBindText
		valid:= """target_name" . this.TestCharacter.Name . "$$follow"""
	}
}

Class ImmediateLoadingKeyBindGeneratorTest{
	Begin(){
		this.Helper:=new TestGeneratorHelper()
		this.generator:= new ImmediateLoadingKeyBindGenerator()
		this.Helper.Init(this.generator) 
	}
	End(){
		this.Generator.GeneratedKeybindText:=""
		this.Generator.GenerateKeyBindsForEvent("ClearNPC")
		this.Generator.CompleteEvent()
	}
	
	TestSubmit_PlacesKeybindInTempBindFile(){
		this.Helper.GenerateKeyBindsForEventTestSpawnKeybind()
		this.generator.CompleteEvent()
		actualFile:= FileOpen("..\data\B.txt","r")
		actual:= actualFile.ReadLine()
		valid:="Y ""spawn_npc" . " " . this.helper.TestCharacter.Model . " " . this.helper.TestCharacter.Name . """"
		Yunit.AssertEquals(valid, actual)
	}
}

Yunit.Use(YunitStdOut).Test(ImmediateLoadingKeyBindGeneratorTest, BaseCityOfHeroesKeybindGeneratorTest )
		