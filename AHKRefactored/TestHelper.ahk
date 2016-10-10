#include ManagedCharacter.ahk

Class SpawnTestHelper{
		SpawnTestCharacter(){
			generator:= ImmediateLoadingKeyBindGenerator.GetInstance()
			generator.GenerateKeyBindsForEvent("SpawnNPC", "model_Statesman", "TestCharacter [TestCrowd]")
			generator.CompleteEvent()
			testCharacter:= new ManagedCharacter("TestCharacter", "model_Statesman", "Model")
			testCharacter.Crowd:={Name:"TestCrowd"}
			return testCharacter
		}
}
Class AnimatedCharacterHelper extends CharacterTestHelper{
	NewValidCharacterWithKeyAssignedAnimation{
		Get{
			character:= new AnimatedCharacter("TestAnimatedCharacter")
			
			ability:=character.AddAnimatedAbility( new AnimatedAbility("TestAnimatedAbility", "1"))
			ability.AddAnimationElement( new AnimationElement())
			return character
		}
	}
	KeyPressed{
		Get{
			return "1"
		}
	}
	
	NewValidCharacterWithMovAnimation{
		Get{
			character:= new AnimatedCharacter("TestAnimatedCharacter")
			
			ability:=character.AddAnimatedAbility( new AnimatedAbility("TestMovAbility"))
			ability.AddAnimationElement( new MOV("DUALMGUNS_BURST"))
			return character
		}
	}
	ValidGeneratedKeybindFromMov{
		Get{
			return "target_name TestAnimatedCharacter []$$mov DUALMGUNS_BURST"
		}
	}
	
	NewValidCharacterWithSoundAnimation{
		Get{
			character:= new AnimatedCharacter("TestAnimatedCharacter")
			
			ability:=character.AddAnimatedAbility( new AnimatedAbility("TestSoundAbility"))
			ability.AddAnimationElement( new Sound("behavior\BaneBomb_DownHole.wav"))
			return character
		}
	}
	ValidPlayedSoundSCriptCommand{
		Get{
			return "SoundPlay, sound\action\behavior\BaneBomb_DownHole.wav, Wait"
		}
	}
	
	BuildTestCostumeFile(){
		testCostume=
		(
{
CostumeFilePrefix male
HeadScales  0,  0,  0
BrowScales  0,  0,  0
CheekScales  0,  0,  0
ChinScales  0,  0,  0
CraniumScales  0,  0,  0
JawScales  0,  0,  0
NoseScales  0,  0,  0
SkinColor  117,  68,  54
NumParts 28

CostumePart ""
{
	Fx AlreadySet.fx
	Geometry none
	Texture1 none
	Texture2 none
	DisplayName P3890352428
	RegionName Weapons
	BodySetName Weapons
	Color1  0,  0,  0
	Color2  135,  0,  204
	Color3  0,  0,  0
	Color4  135,  0,  204
}

CostumePart ""
{
	Geometry N_Male_Cyberpunk_01.geo/GEO_Neck_Cyberpunk_01
	Texture1 !X_Jaw_Cyberpunk_01
	Texture2 none
	DisplayName P2371314042
	RegionName Head
	BodySetName standard
	Color1  0,  0,  0
	Color2  135,  0,  204
	Color3  0,  0,  0
	Color4  135,  0,  204
}

CostumePart ""
{
	Fx none
	Geometry none
	Texture1 none
	Texture2 none
	Color1  0,  0,  0
	Color2  135,  0,  204
	Color3  0,  0,  0
	Color4  135,  0,  204
}

CostumePart ""
{
	Fx AlreadySet2.fx
	Geometry none
	Texture1 none
	Texture2 none
	Color1  0,  0,  0
	Color2  135,  0,  204
	Color3  0,  0,  0
	Color4  135,  0,  204
}
}
)
		FileDelete, ..\costumes\TestAnimatedCharacter.costume
		FileAppend, %testCostume% , ..\costumes\TestAnimatedCharacter.costume
	}
	NewValidCharacterWithFXAnimation{
		Get{
			character:= new AnimatedCharacter("TestAnimatedCharacter")
			
			ability:=character.AddAnimatedAbility( new AnimatedAbility("TestFXAbility"))
			anFx:=new FXEffect("/Explosions/medium/Expolsion_Child.fx")
			anFx.Colors[1].Red:= 1
			anFX.Colors[1].Green:=2
			anFX.Colors[1].Blue:=3
			anFX.Colors[2].Red:=4
			anFX.Colors[2].Green:=5
			anFX.Colors[2].Blue:=6
			anFX.Colors[3].Red:=7
			anFX.Colors[3].Green:=8
			anFX.Colors[3].Blue:=9
			anFX.Colors[4].Red:=10
			anFX.Colors[4].Green:=11
			anFX.Colors[4].Blue:=12
			ability.AddAnimationElement( anfx)
			return character
		}
	}
	NewValidCharacterWithPlayNextFXAnimation{
		Get{
			validCharacter:=this.NewValidCharacterWithFXAnimation
			validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].PlayWithNextElement:=true
			validCharacter.AnimatedAbilities["TestFXAbility"].AddAnimationElement( new MOV("DUALMGUNS_BURST"))
			return validCharacter
		}
	}
	ValidCostumeTextEffect{
		Get{
			return "Fx /Explosions/medium/Expolsion_Child.fx"
		}
	}
	ValidGeneratedKeybindFromFX{
		Get{
			return "target_name TestAnimatedCharacter []$$load_costume TestAnimatedCharacter\TestAnimatedCharacter_Expolsion_Child.fx"
			
		}
	}
	ValidGeneratedKeybindFromPlayWithNextFX{
		Get{
			return "load_costume TestAnimatedCharacter\TestAnimatedCharacter_Expolsion_Child.fx$$mov DUALMGUNS_BURST"
			
		}
	}
	ValidGeneratedKeybindFromDeactivateFX{
		Get{
			return "target_name TestAnimatedCharacter []$$load_costume TestAnimatedCharacter.fx"
		}
	}
	AssertColorOfFX(FXAnimation){
		aColor:=FXAnimation.Colors[1]
		actual:="	Color1 " . aColor.Red . ", " . aColor.Green . ", " . aColor.Blue
		Yunit.AssertEquals(actual, this.ValidFXColor1FromCostumeFile)
		
		aColor:=FXAnimation.Colors[2]
		actual:="	Color2 " . aColor.Red . ", " . aColor.Green . ", " . aColor.Blue
		Yunit.AssertEquals(actual, this.ValidFXColor2FromCostumeFile)
		
		aColor:=FXAnimation.Colors[3]
		actual:="	Color3 " . aColor.Red . ", " . aColor.Green . ", " . aColor.Blue
		Yunit.AssertEquals(actual, this.ValidFXColor3FromCostumeFile)
		
		aColor:=FXAnimation.Colors[4]
		actual:="	Color4 " . aColor.Red . ", " . aColor.Green . ", " . aColor.Blue
		Yunit.AssertEquals(actual, this.ValidFXColor4FromCostumeFile)
	}
	ValidFXColor2FromCostumeFile{
		get{
			return "	Color2 4, 5, 6"
		}
	}
	ValidFXColor3FromCostumeFile{
		get{
			return "	Color3 7, 8, 9"
		}
	}
	ValidFXColor1FromCostumeFile{
		get{
			return "	Color1 1, 2, 3"
		}
	}
	ValidFXColor4FromCostumeFile{
		get{
			return "	Color4 10, 11, 12"
		}
	}
	CostumOfCharacterWithFXAnimation{
		Get{
			costumeFile:="..\costumes\TestAnimatedCharacter.costume"
			FileRead costumeText, %costumeFile%
			return costumeText
		}
	}
	ArchiveCostumOfCharacterWithFXAnimation{
		Get{
			costumeFile:="..\costumes\TestAnimatedCharacter\TestAnimatedCharacter_original.costume"
			FileRead costumeText, %costumeFile%
			return costumeText
		}
	}
	
	AssertAnimationsAreEqual(actual, valid){
		this.AssertCharacter(actual.Character, valid.Character)
		Yunit.AssertEquals(actual.Name, valid.Name)
		Yunit.AssertEquals(actual.SequenceType, valid.SequenceType)
		actualAnimationElements:=actual.AnimationElements
		for name, actualElement in actualAnimationElements{
			validElement:= valid.AnimationElements[Name]
			this.AssertAnimationElementsAreEqual(actualElement, validElement)
		}
	}
	AssertAnimationElementsAreEqual(actualElement, validElement){
			Yunit.AssertEquals(actual.Order, valid.Order)
			this.AssertCharacter(actualElement.Character, validElement.Character)
			ctype:=validElement.__class
			assertMethod:="Assert" . ctype . "sAreEqual"
			if(ctype <> "AnimationElement"){
				this[assertMethod](actualElement, validElement)
			}
	}
	AssertMOVsAreEqual(actualElement, validElement){
		Yunit.AssertEquals(actualElement.MovResource, validElement.MovResource)
	}
	AssertSoundsAreEqual(actualElement, validElement){
	}
	AssertPausesAreEqual(actualElement, validElement){
	}
	AssertFXsAreEqual(actualElement, validElement){
	}
	AssertReferenceAbilitysAreEqual(actualElement, validElement){
	}
}
	
	
Class GeneratorTestHelper{
	NeuterTheGenerator(character){
		generator:=character.Generator
		generator.TriggerKey:=""
		generator.LoaderKey:=""
		generator.GeneratedKeybindText:=""
	}
	ActivateTheGenerator(character){
		generator:=character.Generator
		generator.TriggerKey:="Y"
		generator.LoaderKey:="B"
		
	}
}
Class CharacterTestHelper{
	AssertCharacter(actual, valid){
		Yunit.AssertEquals(actual.Name, valid.Name)
		Yunit.AssertEquals(actual.Skin.Surface,valid.Skin.Surface)
		Yunit.AssertEquals(actual.Skin.Type, valid.Skin.Type)
	}
	NewValidCharacter{
		Get{
			return new ManagedCharacter("TestCharacter", "model_Statesman", "Model")
		}
	}
	NewUpdatedCharacter{
		Get{
			return new ManagedCharacter("UpdatedTestCharacter",  "updated_model_Statesman", "updated_Model")
		}
	}
	NewUnsavedCharacter{
		Get{	
			return new CrowdMembership("NewTestCharacter",  "new_model_Statesman", "new_Model")
		}
	}
	NewValidTargetedCharacter{
		Get{	
			return new CrowdMembership("TargetedCharacter")
		}
	}
	NewSecondValidTargetedCharacter{
		Get{	
			return new CrowdMembership("TargetedCharacter 2")
		}
	}
	NewSecondUnsavedCharacter{
		Get{	
			return new CrowdMembership("NewTestCharacter 2",  "new_model_Statesman", "new_Model")
		}
	}
	NewTestCharacterRepository{
		Get{
			repository:=CharacterRepository.GetInstance("TestCharacters")
			repository.LoadCharacterData()
			
			testCharacter1 :={Name:"TestCharacter", _Skin:{_Surface:"model_Statesman",Type:"Model"}}
			testCharacter2:={Name:"TestCharacter 2", _Skin:{_Surface:"model_Statesman 2",Type:"Model"}}
			repository.Data:={"TestCharacter":TestCharacter1,"TestCharacter 2": testCharacter2}
			return repository
		}
	}
	NewTestCharacterInfo{
		Get{
			testInfo:={}
			testInfo.Name:="TestCharacter"
			testInfo._Skin:={}
			testInfo._skin._Surface:="model_Statesman"
			testInfo._skin.Type:="Model"
			return testinfo
		}
	}
}

Class CrowdTestHelper extends CharacterTestHelper{
	AssertPositions(actualPosition, validPosition){
		Yunit.AssertEquals(actualPosition.X, validPosition.X)
		Yunit.AssertEquals(actualPosition.y, validPosition.y)
		Yunit.AssertEquals(actualPosition.z, validPosition.z)
	}
	NewUnsavedCrowd{
		Get{
			newCrowd:= new CharacterCrowd("NewTestCrowd")
			newCrowd.AddMember(this.NewUnsavedCharacter)
			return newCrowd
		}
	}
	NewValidTargetedCrowd{
		Get{
			newCrowd:= new CharacterCrowd("TargetedCrowd")
			newCrowd.AddMember(this.NewValidTargetedCharacter)
			return newCrowd
		}
	}
	NewValidTargetedCrowdWithTwoCharacters{
		Get{
			newCrowd:= new CharacterCrowd("TargetedCrowd")
			newCrowd.AddMember(this.NewValidTargetedCharacter)
			newCrowd.AddMember(this.NewSecondValidTargetedCharacter)
			return newCrowd
		}
	}
	AssertCrowd(actual, valid){
		Yunit.AssertEquals(actual.Name, valid.Name)
		for validName, validMember in valid.AllMembers{
			actualMember:= actual.Members[validName]
			Yunit.AssertEquals(actualMember.Name, validMember.Name)
			this.AssertCharacter(actualMember, validMember)
			actualPos:=actualMember.Position
			validPos:=validmember.Position
			this.AssertPositions(actualPos, validPos)
		}
	}
	NewTestCrowdRepository{
		Get{
			repository:=CrowdRepository.GetInstance("TestCrowds")
			repository.LoadCrowdData()
			testInfo:=this.NewTestCrowdInfo
			repository.Data["TestCrowd"]:=testInfo
			repository.CharacterRepository:= this.NewTestCharacterRepository
			return repository
		}
	}
	NewTestCrowdInfo{
		Get{
			testInfo:={Name:"TestCrowd", Members:{"TestCharacter":{Name:"TestCharacter"}, "TestCharacter 2":{Name:"TestCharacter 2"}}}
			return testinfo
		}
	}
	NewValidCrowd{
		Get{
			validCrowd:= new CharacterCrowd("TestCrowd")
			validCrowd.AddMember( new CrowdMembership("TestCharacter", "model_Statesman", "Model"))
			validCrowd.AddMember( new CrowdMembership("TestCharacter 2", "model_Statesman 2", "Model"))
			return validCrowd
		}
		
	}
}

class CityOfHeroesKeybindTestHelper{
		
		BuildValidTargetDeleteSkinModelText(character){
			return this.BuildValidTargetText(character) . "$$delete_npc $$" . this.BuildValidSkinModelText(character)
		}
		BuildValidTargetFollowText2(character){
			return "target_name " . character.Label . "$$follow " 
		}
		BuildValidTargetFollowText(character){
			return "target_name " . character.Label . "$$""follow " 
		}
		BuildValidTargetText(character){
			return "target_name " . character.Label 
		}
		BuildValidSkinModelText(character){
			return "benpc " . character.Skin.Surface
		}
		BuildValidSkinCostumeText(character){
			return "Load_costume " . character.Skin.Surface
		}
		BuildValidSpawnText(character){
			return "spawn_npc " . character.Skin.Surface . " " . character.label .  "$$target_name " . character.label . "$$benpc " . character.Skin.Surface . """"""
		}
		BuildValidDeleteSpawnText(character){
			return "delete_npc $$" . this.BuildValidSpawnText(character)
		}
		AssertBindParametersMatchKeybindString(validKeybind, function, parameters*){
			actualKeybind:="Y """ . function
			for key,para in parameters{
				actualKeybind := actualKeybind . " " . para 
			}
			actualKeybind:= actualKeybind . """"
			Yunit.AssertEquals(validKeybind, actualKeybind)
		}
	}
