#Include Yunit\Yunit.ahk
#Include HeroVirtualTabletopEventLoader.ahk

class EventLoaderTest{
	Begin(){
		generator:= ImmediateLoadingKeyBindGenerator.GetInstance()
		generator.GenerateKeyBindsForEvent("SpawnNPC", "model_Statesman", "TestCharacter [TestCrowd]")
		generator.CompleteEvent()
		this.Character:= new ManagedCharacter("TestCharacter", "TestCrowd", "model_Statesman", "Model")
		this.repository:=CharacterRepository.GetInstance()
		this.repository.Characters["TestCharacter"]:=this.Character
		this.character.Target()
		targeted:=this.repository.Targeted
			
		loader:=new EventLoader()
		loader.LoadEvents()
		this.handler:=HeroVirtualTableTopEventHandler.GetInstance()
	}
	End(){
		this.Character.ClearFromDesktop()
	}
	
	
	TestSpawnKeyCommand(){
		this.Handler.HandleKey("P")
		helper:= new Targeter()
		Yunit.AssertEquals(this.Character.Label, helper.Label)
	}
	
	TestTargetedCommand(){
		this.Character.Targeted:=false
		this.Handler.HandleKey("T")
		
		valid:=this.Character.Label
		actual:= new Targeter().Label
		Yunit.AssertEquals(valid,actual)
		Yunit.AssertEquals(true,this.Character.Targeted)
		
		this.Handler.HandleKey("T")
		Yunit.AssertEquals(false,this.Character.Targeted)
	
	}
	
	TestClearFromDesktopKeyCommand(){
		this.Handler.HandleKey("Del")
		
		character:=this.Repository.Characters["TestCharacter"]
		character.Target()
		
		Yunit.AssertEquals(0,strlen(this.Character.COHPLayer.Label))
		actual:= new Targeter().Label
		Yunit.AssertEquals(0,StrLen(actual))
	}
	
	TestTargetAndMoveCameraToCharacterKeyCommand(){
		this.Handler.HandleKey("H")
		valid:=this.Character.Label
		actual:= new Targeter().Label
		Yunit.AssertEquals(valid,actual)
		;to do test location when it works
	}
	
	TestToggleManueveringWithCameraKeyCommand(){
		this.Handler.HandleKey("C")
		
		character:=this.Repository.Characters["TestCharacter"]
		character.Target()
		
		Yunit.AssertEquals(0,strlen(this.Character.COHPLayer.Label))
		actual:= new Targeter().Label
		Yunit.AssertEquals(0,StrLen(actual))
		
		this.Handler.HandleKey("C")
		
		valid:=this.Character.Label
		actual:= new Targeter().Label
		Yunit.AssertEquals(valid,actual)
		Yunit.AssertEquals(true,this.Character.Targeted)
	}
	
	
}

Yunit.Use(YunitStdOut).Test(EventLoaderTest)
