#Include CharacterRepository.ahk
#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahK
#Include Yunit\StdOut.ahk
#Include TestHelper.aHK


Class CharacterRepositoryTestSuite{
	Class CharacterRetrievalTest extends CharacterRepositoryTestSuite.BaseCharacterRepositoryTest{
		Begin(){
			base.Begin()
		}
		TestBuildsCharacterFromDataIfNotAlreadyBuilt(){
			actualCharacter:= this.Repository.Characters["TestCharacter"]
			validCharacter:=this.NewValidCharacter
			this.AssertCharacter(actualCharacter, validCharacter)
		}
		TestTargetedReturnsProperTargeted(){
			this.Repository._targeter:=new CharacterRepositoryTestSuite.TargeterStub()
			actual:= new ManagedCharacter("TestTargeted", "updated_model_Statesman", "updated_Model")
			this.Repository.Characters[actual.Name]:=actual
			actual.Target()
			
			valid:=this.Repository.Targeted
			this.AssertCharacter(actual, valid)
			
		}
		TestTargetedBuildsAndAddsCharacterIfDoesNotExistInRepository(){
			this.Repository._targeter:=new CharacterRepositoryTestSuite.TargeterStub()
			actual:= new ManagedCharacter("TestTargeted")
			actual.Target()
			
			valid:=this.Repository.Targeted
			this.AssertCharacter(actual, valid)
		}
		End(){
			 base.End()
		 }
	}
	Class CharacterSaveTest extends CharacterRepositoryTestSuite.BaseCharacterRepositoryTest{
		Begin(){
			base.Begin()
		}
		TestSavesNewCharacter(){
			character:= this.NewUnsavedCharacter
			name:=character.Name
			this.Repository.SaveCharacter(character)
			this.Repository.Clear()
			this.Repository.LoadCharacterData()
			
			actual:=this.Repository.Characters[name]
			valid:= this.NewUnsavedCharacter
			this.AssertCharacter(actual, valid)
		}
		TestSaveUpdatesExistingCharacter(){
			character:=this.Repository.Characters["TestCharacter"]
			character.Name:="UpdatedTestCharacter"
			character.Skin.Surface:="updated_model_Statesman"
			character.Skin.Type:="updated_Model"
			this.Repository.SaveCharacter(character)
			this.Repository.Clear()
			this.Repository.LoadCharacterData()
			
			actual:=this.Repository.Characters[character.Name]
			valid:= this.NewUpdatedCharacter
			this.AssertCharacter(actual, valid)
		}
		TestUpdateCharacterNameOverwritesExistingEntry(){
			character:=this.Repository.Characters["TestCharacter"]
			oldName:=character.Name
			newName:="UpdatedTestCharacter"
			character.Name:=newName
			character.Skin.Surface:="updated_model_Statesman"
			character.Skin.Type:="updated_Model"
			
			this.Repository.SaveCharacter(character)
			this.Repository.Clear()
			this.Repository.LoadCharacterData()
			
			actual:=this.Repository.Characters[oldName]
			valid:=""
			Yunit.AssertEquals(actual, valid)
			
			actual:=this.Repository.Characters[newName]
			valid:= this.NewUpdatedCharacter
			this.AssertCharacter(actual, valid)
	
		}
		End(){
			base.End()
			FileDelete , data\TestCharacters.data
		}
	}
	Class TargeterStub{
		InitFromCurrentlyTargetedModel(){
			this.Label:="TestTargeted []" 
		}
	}
	Class UpdateFromCharacterInfoTest extends CharacterRepositoryTestSuite.BaseCharacterRepositoryTest{
		Begin(){
			base.Begin()
		}
		TestUpdatesCrowdButNotEmptySkin(){
			valid:=this.NewValidCharacter
			
			characterInfo:={}
			characterInfo.Name:="TestCharacter"
			actual:=this.Repository.UpdateCharacterFromInfo(characterInfo)
			this.AssertCharacter(actual, valid)
			
		}
		TestUpdatesSkin(){
			valid:=this.NewValidCharacter
			valid.Skin.Surface:="updated_model_Statesman"
			characterInfo:={}
			characterInfo.Name:="TestCharacter"
			characterInfo._Skin:={}
			characterInfo._Skin._Surface:="updated_model_Statesman"
			
			actual:=this.Repository.UpdateCharacterFromInfo(characterInfo)
			this.AssertCharacter(actual, valid)
		
		}
		End(){
			base.End()
			FileDelete , data\TestCharacters.data
		}
	}
	class BaseCharacterRepositoryTest extends CharacterTestHelper {
		Begin(){
			this.Repository:= this.NewTestCharacterRepository
		}
		
		
		End(){
			this.Repository.Clear()
		}
	}	
}

Yunit.Use(YunitStdOut).Test(CharacterRepositoryTestSuite)

