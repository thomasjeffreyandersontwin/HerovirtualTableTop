#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahK
#Include Yunit\StdOut.ahk
#Include TestHelper.ahk
#Include CrowdRepository.ahk

Class CrowdRepositoryTestSuite{
	Class CrowdRetrievalTest extends CrowdTestHelper{
		Begin(){
			this.Repository:= this.NewTestCrowdRepository
		}
		TestBuildsCrowdWithCharactersFromDataIfNotAlreadyBuilt(){
			actualCrowd:= this.Repository.Crowds["TestCrowd"]
			this.AssertCrowd(actualCrowd, this.NewValidCrowd)
		}
		End(){
			 this.Repository.Clear()
			 FileDelete , data\TestCrowds.data
		 }
	}
	Class CrowdSaveTest extends CrowdTestHelper{
		Begin(){
			this.Repository:= this.NewTestCrowdRepository
		}
		AddsCharacterToCrowdIfNotCrowd(){
			newCharacter:= this.NewUnsavedCharacter
			name:=newCharacter.Name
			this.Repository.CharacterRepository.Characters[newCharacter.Name]:=newCharacter
			
			testCrowd:= this.Repository.Crowds["TestCrowd"]
			testCrowd.AddMember(newCharacter)
			this.Repository.SaveCrowd(testCrowd)
			this.Repository.Clear()
			
			this.Repository.LoadCrowdData()
			actual:= this.Repository.Crowds["TestCrowd"].Members[name]
			valid:= this.NewUnsavedCharacter
			this.AssertCharacter(actual, valid)
			
		}
		TestBuildsAndSavesCrowdIfItDoesNotExistInRepository(){
			newCrowd:= this.NewUnsavedCrowd
			newCharacter:=newCrowd.Members["NewTestCharacter"]
			this.Repository.CharacterRepository.Characters[newCharacter.Name]:=newCharacter
			
			name:=newCrowd.Name
			this.Repository.Crowds[newCrowd.Name]:=newCrowd
			this.Repository.Clear()
			
			this.Repository.LoadCrowdData()
			actual:= this.Repository.Crowds[name]
			valid:= this.NewUnsavedCrowd
			this.AssertCrowd(actual, valid)	
		}
		End(){
			 this.Repository.Clear()
			 FileDelete , data\TestCrowds.data
		 }
	}
	Class CrowdTargetingTest{
		Begin(){
			this.Repository:= this.NewTestCrowdRepository
		}
		TestRepositoryReturnsCrowdOfTargetedCharacter(){
			this.Repository.CharacterRepository._targeter:=new CrowdRepositoryTestSuite.TargeterStub()
			newCrowd:= this.NewValidTargetedCrowd
			newCharacter:=newCrowd.Members["TargetedCharacter"]
			this.Repository.CharacterRepository.Characters[newCharacter.Name]:=newCharacter
			this.Repository.Crowds[newCrowd.Name]:=newCrowd
			
			actual:=this.Repository.Targeted
			this.AssertCrowd(actual, newCrowd)
		}
		TestTargetBuildsAndSavesCrowdAndCharacterIfNotExistInRepository(){
			this.Repository.CharacterRepository._targeter:=new CrowdRepositoryTestSuite.TargeterStub()
			actual:=this.Repository.Targeted
			
			valid:= this.NewValidTargetedCrowd
			this.AssertCrowd(actual, valid)
		}
		TestTargetBuildsAndSavesCharactersBelongingToSameCrowdIfNotExistInRepository(){
			this.Repository.CharacterRepository._targeter:=new CrowdRepositoryTestSuite.TargeterStub()
			actual:=this.Repository.Targeted
			
			this.Repository.CharacterRepository._targeter.Label:= "TargetedCharacter 2 [TargetedCrowd]"
			actual:=this.Repository.Targeted
			
			valid:= this.NewValidTargetedCrowdWithTwoCharacters
			this.AssertCrowd(actual, actual)
			
		}
		End(){
			 this.Repository.Clear()
			 FileDelete , data\TestCCrowds.data
		}
	}
		
	Class TargeterStub{
		Label:="TargetedCharacter [TargetedCrowd]" 
		InitFromCurrentlyTargetedModel(){

		}
	}
}

Yunit.Use(YunitStdOut).Test(CrowdRepositoryTestSuite)
