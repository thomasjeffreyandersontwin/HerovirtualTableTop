#Include HeroVirtualTabletopEventHandler.ahk
#Include Yunit\Yunit.ahk

;handler:= HeroVirtualTableTopEventHandler.GetInstance()
;SubjectStub:= new EventTestSuite.SubectStub(1,"BaseString")
;SubjectRepositoryStub:= new EventTestSuite.SubjectRepositoryStub(this.SubjectStub)
;TestEvent:= new Event("TestEvent", "SubjectStub", "IncrementState",this.SubjectRepositoryStub,"T")
;handler.AddEvent(TestEvent)
;TThandler.ListenForKeyStrokes()


Class EventTestSuite{
	class SubjectStub{
		StateCounter:=0
		StateString:=""
		__New(counter, string){
			this.StateCounter:=counter
			this.StateString:= string
		}
		ChangeState(counter, string){
			this.StateCounter+=counter
			this.StateString:= this.StateString . string
		}
		
		IncrementState(){
			this.StateCounter+=1
			this.StateString:= this.StateString . "AndIncrementString"
		}
	}
	Class SubjectRepositoryStub{
		SubjectStub:=""
		__New(SubjectStub){
			this.SubjectStub:= SubjectStub
		}
		Targeted{
			Get{
				return this.SubjectStub
			}
		}
		UpdateSubjectStubFromInfo(info){
			this.SubjectStub.StateCounter:=info.StateCounter
			this.SubjectStub.StateString:=info.StateString
			return this.SubjectStub
		}
	}
	Class  TestInfoPersisterHelper{
		WriteTestInfoEventFile(){
			testInfo:={StateCounter:5, StateString:"FileBasedInfoString"}
			eventInfo:={Name:"TestEvent" , Subject:testInfo, parameters:{counter:5, string:"FileBasedInfoString"}}
			ErrorLevel:= StrObj(eventInfo, "Event.info")
			if (errorLevel >0)
				MsgBox % "did not write "
		}
	}
	class HeroVirtualTableTopEventHandlerTest{
		Begin(){
			this.handler:= HeroVirtualTableTopEventHandler.GetInstance()
			this.SubjectStub:= new EventTestSuite.SubjectStub(1,"BaseString")
			this.SubjectRepositoryStub:= new EventTestSuite.SubjectRepositoryStub(this.SubjectStub)
			this.TestEvent:= new Event("TestEvent", "SubjectStub", "IncrementState",this.SubjectRepositoryStub,"T")
		}
		TestFiresCorrectEventOnKeyHandle(){
			this.handler.AddEvent(this.TestEvent)
			this.handler.HandleKey("T")
			
			;Send T
			Yunit.AssertEquals(this.SubjectStub.StateCounter, 2)
			Yunit.AssertEquals(this.SubjectStub.StateString, "BaseStringAndIncrementString")
		}
		
		TestListensToEventKeystrokeAndFiresOnPress(){
			;this.handler.AddEvent(this.TestEvent)
			;this.handler.ListenForKeyStrokes()
			;Send T
			;Yunit.AssertEquals(this.SubjectStub.StateCounter, 2)
			;Yunit.AssertEquals(this.SubjectStub.StateString, "BaseStringAndIncrementString")
		}
		
		TestLoadsInfoFromFileAndFiresCorrectInfoEvent(){
			this.TestEvent.Method:="ChangeState"
			this.handler.AddEvent(this.TestEvent)
			helper:= new EventTestSuite.TestInfoPersisterHelper()
			helper.WriteTestInfoEventFile()
			this.handler.ListenForFileBasedEvent()
			Yunit.AssertEquals(this.SubjectStub.StateCounter, 10)
			Yunit.AssertEquals(this.SubjectStub.StateString, "FileBasedInfoStringFileBasedInfoString")
		}
		
		End(){
			FileDelete % ErrorLevel:= StrObj(testInfo, "event.info")
			;this.handler.StopListeningForKeyStrokes()
		}
	}
	class EventHandleTest{
		Begin(){
			this.SubjectStub:= new EventTestSuite.SubjectStub(1,"BaseString")
			this.SubjectRepositoryStub:= new EventTestSuite.SubjectRepositoryStub(this.SubjectStub)
			this.TestEvent:= new Event("TestEvent", "SubjectStub", "ChangeState",this.SubjectRepositoryStub)
			this.TestInfo:={StateCounter:1, StateString:"InfoString"}
		}
		TestPassesParametersToRightMethod(){
			this.TestEvent.Handle(this.SubjectStub , 7, "AndEventString")
			Yunit.AssertEquals(this.SubjectStub.StateCounter, 8)
			Yunit.AssertEquals(this.SubjectStub.StateString, "BaseStringAndEventString")
			
		}
		TestRunsEventOnTargetedCharacter(){
			this.TestEvent.HandleWithTargeted(7, "AndEventString")
			Yunit.AssertEquals(this.SubjectStub.StateCounter, 8)
			Yunit.AssertEquals(this.SubjectStub.StateString, "BaseStringAndEventString")
		}
		TestUpdatesSubjectFromInfo(){
			this.TestEvent.HandleWithSubmittedInfo(this.TestInfo,7,"AndEventString")
			Yunit.AssertEquals(this.SubjectStub.StateCounter, 8)
			Yunit.AssertEquals(this.SubjectStub.StateString, "InfoStringAndEventString")
		}
	}
}

Yunit.Use(YunitStdOut).Test(EventTestSuite)