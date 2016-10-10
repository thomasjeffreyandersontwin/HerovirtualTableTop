#Include CrowdManager.ahk
#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahk
#Include Yunit\StdOut.ahk

Class CrowdTestHelper
{
	manager:=""
	LoadCrowds(){
		c:= CrowdManager.GetInstance()
		
		c._characterCrowds:={}
		crowd:=c.LoadModelsForCrowd("TestCrowd")
		c.Crowds["testModel1"]:= { name: "testModel1", model:"Arachnos_Bane_Boss"}
		c.Crowds["testModel2"]:= { name: "testModel2", model:"FemaleNPC_64"}
		c.Crowds["testModel3"]:= { name: "testModel3", model:"Arachnos_Bane_Boss"}
		c.WriteToFile()
		

		
		this.manager:=c
		return c
	}
		
	AssertCrowdsAreEqual(actual,valid){
		Yunit.AssertEquals(actual._name , valid._name)
		Yunit.AssertEquals(actual.model , valid.model)
	}
		
	SpawnModelForCrowd(value){
		c:= this.manager
		c.SpawnModelForCrowd(Model)
		return name
	}

	SpawnNamedModel(name){
		c := this.manager
		c.SpawnNamedModel(name)
		return a.Crowds[name]
	}
	
	Assert_SpawnCommandStringCorrect(valid){
		commandFired:= this.manager.CommandParser.directory . this.manager.CommandParser.LoaderKey . ".txt"
		FileRead  actual, % commandFired
		valid:= "Y " . valid
		Yunit.AssertEquals(actual , valid)
	}
	
	SpawnAndAssertModelIsValid(validModel){
		c:= this.manager
		c.SpawnNamedModel(validModel.name)
		valid:="""spawn_npc " . validModel.model . " " . validModel.name . " " . "[" . validModel.crowd . "]" . """"	
		this.Assert_SpawnCommandStringCorrect(valid)Y
	}

}
Class CrowdManagerTest{
	helper:= ""
	Begin(){
		this.helper:=new CrowdTestHelper()
		this.helper.LoadCrowds()
	}
	
	TestSpawnModelForCrowd_AddsNewModelToRosterWithGeneratedName(){
		validModel:="FemaleNPC_56"
		c:=this.helper.manager
		c.SpawnModelWithCostumeForCrowd(validModel,"")
		c.SpawnModelWithCostumeForCrowd(validModel,"")
		validName:= "FemaleNPC_56" . 1
		actual:=c.Crowds[validName]
		this.Helper.SpawnAndAssertModelIsValid({name:validName , model:"FemaleNPC_56", crowd:"TestCrowd"})
		this.helper.AssertCrowdsAreEqual(actual, {_name:validName, model:validModel})
	}
		

	TestCrowdManagerLoadsCrowd_GetsCorrectCharacterFile(){
		c:= this.helper.manager
		actual:=c.Crowds["testModel1"]
		valid:= { name: "testModel1", model:"Arachnos_Bane_Boss"}
		this.helper.AssertCrowdsAreEqual(actual,valid)
	}
	TestCrowdManagerSpawnsModelInCrowdCrowd_BuildsCorrectCommandString(){	
		validModel1:= { name: "testModel1", model:"Arachnos_Bane_Boss", crowd:"TestCrowd"}
		this.Helper.SpawnAndAssertModelIsValid(validModel1)
		
		validModel2:= { name: "testModel2", model:"FemaleNPC_64" , crowd:"TestCrowd"}
		this.Helper.SpawnAndAssertModelIsValid(validModel2)
		
		validModel3:= { name: "testModel3", model:"Arachnos_Bane_Boss", crowd:"TestCrowd"}
		this.Helper.SpawnAndAssertModelIsValid(validModel3)
	}
	TestCorrectPathAndCrowdAreSet(a, charRecovery, charName, globalRecovery){
		a:=this.manager
		valid:=a.Path
		actual:="..\data\\" . charName . ".Crowds"
		
		Yunit.AssertEquals( valid, actual)
	}
}


Yunit.Use(YunitStdOut).Test(CrowdManagerTest )