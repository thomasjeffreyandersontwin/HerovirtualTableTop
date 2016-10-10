#Include Crowd.ahk
#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahK
#Include Yunit\StdOut.ahk
#Include TestHelper.ahk

spawnlog:=0
MockSpawnMethod(name){
	global spawnLog
	spawnLog++
}
class CrowdTestSuite{

	class CrowdCharacterMethodDelegationTest  extends CrowdTestHelper{
		Begin(){
			this.TestCrowd:= new CharacterCrowd("TestCrowd")
			
			char:=new CrowdMembership("Spyder", "Spyder", "Costume", this.TestCrowd)
			char.base.MockMethod:= Func("MockSpawnMethod")
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			char.SavedPosition:={x:10, y:20, z:30}
			
			char:=new CrowdMembership("Ogun", "Ogun", "Costume", this.TestCrowd)
			char.base.MockMethod:= Func("MockSpawnMethod")
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			char.SavedPosition:={x:40, y:50, z:60}
			
			char:=new CrowdMembership("TestCharacter", "model_Statesman", "Model",this.TestCrowd)
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			char.base.MockMethod:= Func("MockSpawnMethod")
			char.SavedPosition:={x:80, y:90, z:100}
		}
		TestCallsCrowdMemberMethodOnAllMembers(){
			crowd:=this.TestCrowd
			crowd.Place()
			
			valid:={x:10, y:20, z:30}
			actual:=crowd.Members["Spyder"].COHPlayer.Position
			this.AssertPositions( actual, valid)
			
			valid:={x:40, y:50, z:60}
			actual:=crowd.Members["Ogun"].COHPlayer.Position
			this.AssertPositions( actual, valid)
			
			valid:={x:80, y:90, z:100}
			actual:=crowd.Members["TestCharacter"].COHPlayer.Position
			this.AssertPositions( actual, valid)
			
			
			
		}
		TestCallsManagedCharacterMethodOnAllMembers(){
			global spawnLog
			crowd:=this.TestCrowd
			crowd.MockMethod()
			valid:=3
			actual:= spawnLog
			Yuint.AssertEquals(actual, valid)
		}
	}
	Class CrowdCharacterIntegrationTest extends CrowdTestHelper{
		Begin(){
			this.TestCrowd:= new CharacterCrowd("TestCrowd")
			
			char:=new CrowdMember("Spyder", "Spyder", "Costume", this.TestCrowd)
			char.SavedPosition:={x:10, y:20, z:30}
			
			char:=new CrowdMember("Ogun", "Ogun", "Costume", this.TestCrowd)
			char.SavedPosition:={x:40, y:50, z:60}
			
			char:=new ManagedCharacter("TestCharacter", "model_Statesman", "Model", this.TestCrowd)
			char.SavedPosition:={x:80, y:90, z:100}
		}
		TestSavePositionForSpawnedCharacter(){
			Spyder:=this.TestCrowd.Members["Spyder"]
			Spyder.Spawn()
			Spyder.StoreCurrentPosition()
			
			charPosition:=Spyder.SavedPosition
			t:= new Targeter()
			targetedPosition:=t.Position.Duplicate
			
			this.AssertPositions(charPosition, targetedPosition)
		}
		TestPlacesCharacterInSavedPosition(){
			Spyder:=this.TestCrowd.Members["Spyder"]
			Spyder.Spawn()
			Spyder.StoreCurrentPosition()
			
			t:= new Targeter()
			originalPosition:=t.Position.Duplicate
			charPosition:=Spyder.SavedPosition
			
			charPosition.X:= charPosition.X - 10
			Spyder.Place()
			
			targetedPosition:=t.Position.Duplicate
			
			this.AssertPositions(charPosition, targetedPosition)
		}
	}
			
	class CharacterPlacementTest extends CrowdTestHelper{
		Begin(){
			this.testCrowd:= new CharacterCrowd("TestCrowd")
			char:=new CrowdMembership("Spyder", "Spyder", "Costume", this.TestCrowd)
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			char.SavedPosition:={x:10, y:20, z:30}
			this.TestCharacter:= char
		}
		TestCharacterIsPlaced(){
			this.TestCharacter.Place()
			actual:=this.TestCharacter.COHPlayer.Position
			valid:={x:10, y:20, z:30}
			this.AssertPositions( actual, valid)
		}
	}
	class AddMemberTestTest extends CrowdTestHelper{
		Begin(){
			this.TestCrowd:= new CharacterCrowd("TestCrowd")
		}
		TestPositionIsCaptured(){
			testCharacter:= new CrowdMembership("TestCharacter 2", "model_Statesman", "Model")
			testCharacter.COHPLayer:={Position:{x:10, y:5, z:5}}

			testCrowd:=this.TestCrowd
			addedCharacter:=testCrowd.AddMember(testCharacter)

			actual:= addedCharacter.Position
			valid:= {x:10, y:5, z:5}
			this.AssertPositions(actual, valid)
			
		}

		TestAddsCharacterToCrowdAndCrowdToCharacter(){
			testCharacter:= new CrowdMember("TestCharacter", "model_Statesman", "Model")
			testCrowd:=	this.TestCrowd
			testCrowd.Members["TestCharacter"]:= testCharacter
			
			addedCharacter:=testCrowd.Members["TestCharacter"]
			vname:=addedCharacter.Name
			Yunit.AssertEquals(testCharacter.Name, vname)
			Yunit.AssertEquals(testCharacter.Name, addedCharacter.CharacterName)
			
			crowdFromCharacter:= addedCharacter.Crowd
			Yunit.AssertEquals(addedCharacter.Crowd.Name	, crowdFromCharacter.Name)
		}
		TestDecoratesExistingCharacterObject(){
			baseCharacter:=new CrowdMembership("TestCharacter2", "model_Statesman", "Model")
			decoratedCharacter:= this.TestCrowd.AddMember(baseCharacter)
			
			actual:=decoratedCharacter.Crowd.Name
			Yunit.AssertEquals(actual, "TestCrowd")
		}
		TestRemoveMember(){
			TestCrowd:=this.testCrowd
			this.testCrowd["TestCharacter"]:= new CrowdMembership("TestCharacter", "model_Statesman", "Model")
			character:=testCrowd.Members["TestCharacter"]
			
			testCrowd.RemoveMember(character)
			character:=testCrowd.Members[character.Name]
			Yunit.AssertEquals(character, "")
			
			crowd:=character.Crowd
			Yunit.AssertEquals(crowd, null)
		}
	}
}

Yunit.Use(YunitStdOut).Test(CrowdTestSuite)