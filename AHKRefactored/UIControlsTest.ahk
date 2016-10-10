#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahK
#Include Yunit\StdOut.ahk
#Include UIControls.ahk

Class UIControlTestSuite{
	class MockUI{
		HandledValue:=""
		Name:="MockUI"
		RenderPos(){
			return "x01 y02 h300 w400"
		}
		HandleEvent(){
			global handledValue:= "Event Ran"
		}
		__New(){
			Gui, MockUI: New
			Gui, MockUI: Show, x0 y0 h100 w100
		}
	}
	class UIControlTest{
		Begin(){
			this.MockUI:=new UIControlTestSuite.MockUI()
			this.handleFunc:=ObjBindMethod(this.MockUI.HandleEvent,"")
			this.renderFunc:=ObjBindMethod(this.MockUI.RenderPos,"")
			
		}
		TestRendersCorrectControl(){
			this.TestUIControl:=new UIControl("TestUIControl", "Text", , , "MockUI")
			this.TestUIControl.Render("TestValue")
			
			GuiControlGet, actual,MockUI:,TestUIControl, 
			valid:= "TestValue"
			
			Yunit.AssertEquals(actual, valid)
			
		}
		TestCallsCustomFunction(){
			this.TestUIControl:=new UIControl("TestUIControl", "Edit","HandleEvent" , this.MockUI, "MockUI")
			this.TestUIControl.Render("TestValue")
			
			GuiControl, Focus, TestUIControl
			Send "new value"
			Gui,  MockUI:Submit 
			GuiControlGet, actual, MockUI:,TestUIControl
			
			global handledValue
			actual:=handledValue
			valid:="Event Ran"
			Yunit.AssertEquals(actual, valid)
		}
		TestChangesValue(){
			this.TestUIControl:=new UIControl("TestUIControl", "Edit","HandleEvent" , this.MockUI, "MockUI")
			this.TestUIControl.Render("TestValue")
			this.TestUIControl.Value:="changed value"
			
			GuiControlGet, actual, MockUI:,TestUIControl, 
			actual:=this.TestUIControl.Value
			Yunit.AssertEquals(actual, "changed value")
		}
		TestAccessessValue(){
			this.TestUIControl:=new UIControl("TestUIControl", "Edit","HandleEvent" , this.MockUI, "MockUI")
			this.TestUIControl.Render("TestValue")
			
			GuiControl ,MockUI:, TestUIControl, "changed value"
			
			GuiControlGet, actual, MockUI:,TestUIControl, 
			valid:=this.TestUIControl.Value
			Yunit.AssertEquals(actual, valid)
		}
		TestRendersPOSWithCustomFunction(){
			renderFunc:=ObjBindMethod(this.MockUI.RenderPos,"")
			this.TestUIControl:=new UIControl("TestUIControl", "Edit","HandleEvent" , this.MockUI, "MockUI", renderFunc)
			this.TestUIControl.Render("TestValue")
			
			GuiControlGet, actual, MockUI:Pos,TestUIControl
			Yunit.AssertEquals(actualx, 01)
			Yunit.AssertEquals(actualy, 02)
			Yunit.AssertEquals(actualh, 300)
			Yunit.AssertEquals(actualw, 400)
			
		}
		End(){
			GUi, MockUI:Destroy
		}
	}
	class ReadWriteControlTest{
		Begin(){
			this.MockUI:=new UIControlTestSuite.MockUI()
		}
		TestEditModeEnablesAndDisables(){
			testRWControl:= new ReadWriteControl("TestRWControl", "Test Label", "MockUI")
			testRWControl.Render()
			
			testRWControl.EditMode:=false
			GuiControlGet, actual, MockUI:Visible, TestRWControl
			Yunit.AssertEquals(actual, true)
			GuiControlGet, actual, MockUI:Visible, TestRWControlEdit
			Yunit.AssertEquals(actual, false)
			
			testRWControl.EditMode:=true
			GuiControlGet, actual, MockUI:Visible, TestRWControl
			Yunit.AssertEquals(actual, false)
			GuiControlGet, actual, MockUI:Visible, TestRWControlEdit
			Yunit.AssertEquals(actual, true)
		}
		TestLabel(){
			testRWControl:= new ReadWriteControl("TestRWControl", "Test Label", "MockUI")
			testRWControl.Render()
			
			GuiControlGet, actual, MockUI:, TestRWControlLabel
			Yunit.AssertEquals(actual, "Test Label:")
		}
		TestPositionsCorrectly(){
			testRWControl:= new ReadWriteControl("TestRWControl", "Test Label", "MockUI")
			testRWControl.Render(true)
			
			testSecondRWControl:= new ReadWriteControl("TestRWControl2", "Test Label2", "MockUI")
			testSecondRWControl.Render()
			
			
			
			GuiControlGet, firstLabel, MockUI:Pos, TestRWControlLabel
			GuiControlGet, firstText, MockUI:Pos, TestRWControl
			GuiControlGet, firstEdit, MockUI:Pos, TestRWControlEdit
			
			GuiControlGet, secondLabel, MockUI:Pos, TestRWControl2Label
			GuiControlGet, secondText, MockUI:Pos, TestRWControl2
			
			Yunit.Assert(firstTextX, firstLabelX + firstLabelw + 5)
			Yunit.Assert(firstEditX, firstLabelX + firstLabelw + 5)
			Yunit.Assert(secondTextX, secondLabelX + secondLabelw + 5)
			
			Yunit.Assert(secondTexty, firstTexty + 22)
		}
		TestUpdatesValue(){
			testRWControl:= new ReadWriteControl("TestUIControl", "Test Label", "MockUI")
			testRWControl.Render(true)
			testRWControl.Value:="changed value"
			
			GuiControlGet, actual,MockUI:,TestUIControl, 
			valid:= "changed value"
			
			GuiControlGet, actual,MockUI:,TestUIControlEdit, 
			valid:= "changed value"
	
		}
		TestGetsValue(){
			testRWControl:= new ReadWriteControl("TestUIControl", "Test Label", "MockUI")
			testRWControl.Render(true)
			
			GuiControl ,MockUI:, TestUIControlEdit, changed value
			actual:=testRWControl.Value
			
			Yunit.AssertEquals(actual, "changed value")
			
			
		}
		End(){
			GUi, MockUI:Destroy
		}
	}
	Class ListBoxControlTest{
		Begin(){
			this.MockUI:=new UIControlTestSuite.MockUI()
		}
		TestRendersAllItems(){
			testLB:= new ListBoxControl("TestListBoxControl",,,"MockUI")
			list:={a:"ContentA", b:"ContentB"}
			testLB.Render(list)
			valid:="a|b|c"
			
			;ControlGet, actual, List,
			;Yunit.AssertEquals(actual, valid)

		}
	}
}
	

		
Yunit.Use(YunitWindow).Test(UIControlTestSuite)