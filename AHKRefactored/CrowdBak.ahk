#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahK
#Include Yunit\StdOut.ahk
#include ManagedCharacter.ahk

Class CrowdMembership {
	_crowd:=""
	SavedPosition:=""
	Character:=""
	__Call(method , params*){
		;DelegateManagedCharacterCalls()
		character:=this.Character
		if(ObjHasKey(character, method) == true and ObjHasKey(this, method) == false){
			return this.Character[method] (params)
		}
	}
	__Set(property, aValue){
		;DelegateManagedCharacterCalls()
		character:=this.Character
		if(ObjHasKey(character, property) == true and ObjHasKey(this, property) == false){
			return this.Character[property]:= aValue
		}
	}
	 __Get(property){
		if(property <> "Character"){
			;DelegateManagedCharacterCalls()
			character:=this.Character
			if(ObjHasKey(character, property) == true and ObjHasKey(this, property) == false)
				return this.Character[property]
			}
		}
	
	NewMemberShipForCharacter(character, crowd){
		member:= new CrowdMembership(character, crowd)
		
		return member
		
	}
	__New(character, crowd){
		this._crowd:=crowd
		name:=character.Name
		this._crowd._Members[name]:= this
		this.SavedPosition:= character.COHPlayer.Position
		this.Character:=character
	}
	Name{
		Get{
			return this.Character.Name
		}
		Set{
			this.Character.Name:= value
			crowd:=this.Crowd
			this.Crowd.AddMember(this.Character)
		}
	}	
	Place(){
		this.COHPlayer.Position:=this.SavedPosition
	}
	StoreCurrentPosition(){
		pos:=this.COHPlayer.Position
		this.SavedPosition:=pos.Duplicate
	}
	
	CrowdName{
		Get{
			return this.Crowd.Name
		}
	}
	CharacterName{
		Get{
			return this.Name
		}
	}
	Position{
		get{
			return base.Position
		}
	}
	Crowd{
		Set{
			this._crowd:=value
			this._crowd._Members[this.Name]:= this
		}
		Get{
			return this._Crowd
		}
	}
}
Class CharacterCrowd{
	_Members:={}
	Name:=""
	__New(name){
		this.Name:=name
	}
	__Call(method , params*){
		if(this[method]==""){
			members:=this._Members
			for name, member in members{
				if(member[method] <>""){
					member[method](params)
				}
			}
		}
	}
	Members[characterName]{
		Set{
			if(value.OriginalName <>""){
				memberToReplace:=this._members[value.OriginalName]
				if(memberToReplace <> ""){
					this._Members.Remove(value.OriginalName)
				}
			}
			membership:= CrowdMembership.NewMemberShipForCharacter(value, this)
		}
		Get{
			member:=this._Members[charactername]
			if(member<>""){
				return member
			}
			else{
				return null
			}
		}
	}
	RemoveMember(crowdMembership){
		this._Members.Remove(crowdMembership.Name)
		crowdMembership.Crowd:=null
	}
	AddMember(character){
		this.Members[character.Name]:=character
		return this.Members[character.Name]
	}
}
spawnlog:=0
MockSpawnMethod(name){
	global spawnLog
	spawnLog++
}
class CrowdTestSuite{
	class BaseCrowdTest{
		AssertPositions(actualPosition, validPosition){
			Yunit.AssertEquals(actualPosition.X, validPosition.X)
			Yunit.AssertEquals(actualPosition.y, validPosition.y)
			Yunit.AssertEquals(actualPosition.z, validPosition.z)
		}
	}
	class CrowdCharacterMethodDelegationTest  extends CrowdTestSuite.BaseCrowdTest{
		Begin(){
			this.TestCrowd:= new CharacterCrowd("TestCrowd")
			
			char:=new ManagedCharacter("Spyder", "Spyder", "Costume")
			char.base.MockMethod:= Func("MockSpawnMethod")
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			membership:= CrowdMembership.NewMemberShipForCharacter( char, this.TestCrowd)
			membership.SavedPosition:={x:10, y:20, z:30}
			
			char:=new ManagedCharacter("Ogun", "Ogun", "Costume")
			char.base.MockMethod:= Func("MockSpawnMethod")
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			membership2:=CrowdMembership.NewMemberShipForCharacter(char, this.TestCrowd)
			membership2.SavedPosition:={x:40, y:50, z:60}
			
			char:=new ManagedCharacter("TestCharacter", "model_Statesman", "Model")
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			membership:=CrowdMembership.NewMemberShipForCharacter(char, this.TestCrowd)
			char.base.MockMethod:= Func("MockSpawnMethod")
			membership.SavedPosition:={x:80, y:90, z:100}
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
	Class CrowdCharacterIntegrationTest extends CrowdTestSuite.BaseCrowdTest{
		Begin(){
			this.TestCrowd:= new CharacterCrowd("TestCrowd")
			
			char:=new ManagedCharacter("Spyder", "Spyder", "Costume")
			membership:= CrowdMembership.NewMemberShipForCharacter( char, this.TestCrowd)
			membership.SavedPosition:={x:10, y:20, z:30}
			
			char:=new ManagedCharacter("Ogun", "Ogun", "Costume")
			membership2:=CrowdMembership.NewMemberShipForCharacter(char, this.TestCrowd)
			membership2.SavedPosition:={x:40, y:50, z:60}
			
			char:=new ManagedCharacter("TestCharacter", "model_Statesman", "Model")
			membership:=CrowdMembership.NewMemberShipForCharacter(char, this.TestCrowd)
			membership.SavedPosition:={x:80, y:90, z:100}
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
			
	class CharacterPlacementTest extends CrowdTestSuite.BaseCrowdTest{
		Begin(){
			this.testCrowd:= new CharacterCrowd("TestCrowd")
			char:=new ManagedCharacter("Spyder", "Spyder", "Costume")
			char.COHPLayer:={Position:{x:0, y:0, z:0}}
			membership:=CrowdMembership.NewMemberShipForCharacter(char, testCrowd)
			membership.SavedPosition:={x:10, y:20, z:30}
			this.TestCharacter:= membership
		}
		TestCharacterIsPlaced(){
			this.TestCharacter.Place()
			actual:=this.TestCharacter.COHPlayer.Position
			valid:={x:10, y:20, z:30}
			this.AssertPositions( actual, valid)
		}
	}
	class AddMemberTestTest extends CrowdTestSuite.BaseCrowdTest{
		Begin(){
			this.TestCrowd:= new CharacterCrowd("TestCrowd")
		}
		TestPositionIsCaptured(){
			testCharacter:= new ManagedCharacter("TestCharacter 2", "model_Statesman", "Model")
			testCharacter.COHPLayer:={Position:{x:10, y:5, z:5}}

			testCrowd:=this.TestCrowd
			addedCharacter:=testCrowd.AddMember(testCharacter)

			actual:= addedCharacter.Position
			valid:= {x:10, y:5, z:5}
			this.AssertPositions(actual, valid)
			
		}
		TestChangeInCharacterNameUpdatesCrowd(){
			testCharacter:= new ManagedCharacter("TestCharacter 2", "model_Statesman", "Model")
			testCrowd:= this.testCrowd
			addedCharacter:=testCrowd.AddMember(testCharacter)
			origName:=testCharacter.Name
			
			addedCharacter.Name:="Test Character 3"
			
			valid:= addedCharacter.Name
			actualCharacter:=testCrowd.Members[addedCharacter.Name]
			actual:=actualCharacter.Name
			Yunit.AssertEquals(actual, valid)
			
			origCharacter:=testCrowd.Members[origName]
			Yunit.AssertEquals(origCharacter, null)
		}
		TestAddsCharacterToCrowdAndCrowdToCharacter(){
			testCharacter:= new ManagedCharacter("TestCharacter", "model_Statesman", "Model")
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
			baseCharacter:=new ManagedCharacter("TestCharacter2", "model_Statesman", "Model")
			decoratedCharacter:= this.TestCrowd.AddMember(baseCharacter)
			
			actual:=decoratedCharacter.Crowd.Name
			Yunit.AssertEquals(actual, "TestCrowd")
		}
		TestRemoveMember(){
			TestCrowd:=this.testCrowd
			this.testCrowd["TestCharacter"]:= new ManagedCharacter("TestCharacter", "model_Statesman", "Model")
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