#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahK
#Include Yunit\StdOut.ahk
#Include TestHelper.ahk
#Include Crowd.ahk


Class AnimatedCharacacterTestSuite{
	Class PlayAnimatedAbilityTest{
		Begin(){
			this.Helper:= new AnimatedCharacterHelper()
		}
		TestKeyPlaysCorrectAbility(){
			validCharacter:=this.Helper.NewValidCharacterWithKeyAssignedAnimation
			validAnimation:= validCharacter.AnimatedAbilitiesByKey[1]
			
			key:=this.Helper.KeyPressed
			actualAnimation:=validCharacter.PlayKey(key,false)
			
			this.Helper.AssertAnimationsAreEqual(actualAnimation, validAnimation)
			actualPlayString:= actualAnimation.AnimationElements[1].Played
			Yunit.AssertEquals(actualPlayString,"Playing 1 for TestAnimatedCharacter" )
		}
		TestNamePlaysCorrectAbility(){
			validCharacter:=this.Helper.NewValidCharacterWithKeyAssignedAnimation
			validAnimation:= validCharacter.AnimatedAbilities["TestAnimatedAbility"]
			
			actualAnimation:=validCharacter.Play("TestAnimatedAbility", false)
			
			this.Helper.AssertAnimationsAreEqual(actualAnimation, validAnimation)
			actualPlayString:= actualAnimation.AnimationElements[1].Played
			Yunit.AssertEquals(actualPlayString,"Playing 1 for TestAnimatedCharacter" )

		}
	}
	class AnimationElementTest{
		Begin(){
			this.Helper:= new AnimatedCharacterHelper()
		}
		TestMovGeneratesKeybind(){
			validCharacter:=this.Helper.NewValidCharacterWithMovAnimation
			validCharacter.Play("TestMovAbility",false)
			actualKeybind:= validCharacter.Generator.GeneratedKeybindText
			validKeybind:= this.Helper.ValidGeneratedKeybindFromMov
			Yunit.AssertEquals(actualKeybind, validKeybind)
		}
		TestSoundGeneratesSoundScript(){
			validCharacter:=this.Helper.NewValidCharacterWithSoundAnimation
			validCharacter.Play("TestSoundAbility", false)
			actualAnimation:= validCharacter.AnimatedAbilities["TestSoundAbility"].AnimationElements[1]
			actualSoundScript:= actualAnimation.LastPlaySoundScript
			validSoundScriptCommand:= this.Helper.ValidPlayedSoundScriptCommand
			Yunit.Assert(InStr(actualSoundScript,validSoundScriptCommand))
			}
	}
	class FXElementTest{
		Begin(){
			this.Helper:= new AnimatedCharacterHelper()
			this.Helper.BuildTestCostumeFile()
			
		}
		TestFXModifiesCostumeFileAndGeneratesLoadCostumeKeybind(){
			validCharacter:=this.Helper.NewValidCharacterWithFXAnimation
			new GeneratorTestHelper().NeuterTheGenerator(validCharacter)
			
			
			validCharacter.Play("TestFXAbility",false)
			
			FX:=validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].Effect
			actualCostumeText:=validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].CostumeText
			found:=InStr(actualCostumeText, FX)
			Yunit.Assert(found > 0)
			
			actualKeybind:= validCharacter.Generator.GeneratedKeybindText
			validKeybind:= this.Helper.ValidGeneratedKeybindFromFX
			
			Yunit.AssertEquals(actualKeybind, validKeybind)
			new GeneratorTestHelper().ActivateTheGenerator(validCharacter)
		}
		TestPersistentFXModifiesOriginalCostumeAndGeneratesLoadCostumeKeybind(){
			validCharacter:=this.Helper.NewValidCharacterWithFXAnimation
			validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].Persistent:=true
			validCharacter.Play("TestFXAbility",false)
			
			FX:=validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].Effect
			actualCostumeText:=this.helper.CostumOfCharacterWithFXAnimation
			
			found:=InStr(actualCostumeText, FX)
			Yunit.Assert(found)
			
			archiveCostume:=this.helper.ArchiveCostumOfCharacterWithFXAnimation
			found:=InStr(archiveCostume, fx)	
			Yunit.Assert(found == 0)
		}
		TestDeactivateEfectsResetsOriginalCostumeAndGeneratesLoadCostumeKeybind(){
			validCharacter:=this.Helper.NewValidCharacterWithFXAnimation
			validCharacter.Generator.GeneratedKeybindText:=""
			
			validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].Persistent:=true
			validCharacter.Play("TestFXAbility",false)
			validCharacter.DeactivateAllEffects()
			
			FX:=validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1].Effect
			actualCostume:=this.helper.CostumOfCharacterWithFXAnimation
			found:=InStr(actualCostumeText, FX)
			Yunit.Assert(found == 0)
			
			actualKeybind:= validCharacter.Generator.GeneratedKeybindText
			validKeybind:= this.Helper.ValidGeneratedKeybindFromDeactivateFX
			
		}
		TestSettingPlayWithNextElementPauseCostumeLoadUntilNextElementPlayed(){
			validCharacter:=this.Helper.NewValidCharacterWithPlayNextFXAnimation
			new GeneratorTestHelper().NeuterTheGenerator(validCharacter)
			
			validCharacter.Play("TestFXAbility")
			
			actualKeybind:= validCharacter.Generator.LastKeybindGenerated
			validKeybind:= this.Helper.ValidGeneratedKeybindFromPlayWithNextFX
			
			Yunit.AssertEquals(actualKeybind, validKeybind)
			new GeneratorTestHelper().ActivateTheGenerator(validCharacter)
		}
		TestAllColorsLoadedIntoCostumeFile(){
			validCharacter:=this.Helper.NewValidCharacterWithPlayNextFXAnimation
			new GeneratorTestHelper().NeuterTheGenerator(validCharacter)
			
			validCharacter.Play("TestFXAbility")
			FXAnimation:=validCharacter.AnimatedAbilities["TestFXAbility"].AnimationElements[1]
			
			this.helper.AssertColorOfFX(FXAnimation)
			
		}
	}
	end(){
		new GeneratorTestHelper().ActivateTheGenerator(validCharacter)
	}
}

class AnimatedCharacter extends CrowdMembership{
	AnimatedAbilities:={}
	AnimatedAbilitiesByKey:={}
	Play(animatedAbilityName,completedEvent=true){
		animatedAbility:=this.AnimatedAbilities[animatedAbilityName]
		animatedAbility.Play(completedEvent)
		return animatedAbility
	}
	PlayKey(activationKey, completedEvent=true){
		animatedAbility:=this.animatedAbilitiesByKey[activationKey]
		animatedAbility.Play(completedEvent)
		return animatedAbility
	}
	AddAnimatedAbility(animatedAbility){
		animatedAbility.Character:=this
		this.AnimatedAbilities[animatedAbility.Name]:=animatedAbility
		this.AnimatedAbilitiesByKey[animatedAbility.ActivateOnKey]:=animatedAbility
		return animatedAbility
	}
	DeactivateAllEffects(completedEvent:=true){
		name:=this.name
		origPath:= "..\costumes\" . name . ".costume"
		archivePath:= "..\costumes\" . name . "\" . name .  "_original.costume"
		if(archivePath <> ""){
			FileCopy  %archivePath% ,%origPath%, 1
			FileDelete, %archivePath%
		}
		
		generator= this.Generator
		generator.GenerateKeyBindsForEvent("LoadCostume", [ name ])
		if(completedEvent == true){
			generator.CompleteEvent()
		}
		;sleep 300
	}
}	
Class AnimatedAbility{
	Name:=""
	Character:=""
	SequenceType:="And"
	AnimationElements:={}
	ActivateOnKey:=""
	LastOrder:=0
	__New(name, activateOnKey:="", sequenceType:="And",animatedCharacter:=""){
		this.Name:=name
		this.Character:= animatedCharacter
		this.SequenceType:=sequenceType
		this.ActivateOnKey:=activateOnKey
	}
	AddAnimationElement(animationElement){
		this.LastOrder++
		animationElement.Character:=this.Character
		animationElement.order:=this.LastOrder
		
		this.AnimationElements[animationElement.order]:=animationElement
	}
	Play(completedEvent=true){
		this.Character.Target(completedEvent)
		elements:=this.AnimationElements
		if(this.SequenceType=="And"){
			for order, element in elements{
				element.Play(completedEvent)
			}
		}
		else{
			Random, chosen ,1, elements.MaxIndex()
			this.elements[chosen].Play(completedEvent)
		}			
	}
}
class AnimationElement{
	Character:=""
	Order:=1
	played:=""
	__New(order:=1, character:=""){
		this.Order:=order
		this.Character:=character
	}
	Play(completedEvent){
		this.Played:= "Playing " . this.Order . " for " . this.Character.Name
		return this.Played
	}
}
class MOV extends AnimationElement{
	MovResource:=""
	__New(movResource ,order:=1, character:=""){
		this.MovResource:=movResource
	}
	Play(completeEvent=true){
		keybind:=this.Character.Generator.GenerateKeyBindsForEvent("Move", this.MovResource)
		if(completeEvent==true){
			this.Character.Generator.CompleteEvent()
		}
	}
}
class Sound extends AnimationElement{
	SoundFile:=""
	__New(SoundFile){
		this.soundFile:=soundFile
	}
	Play(completeEvent=true){
		if(completeEvent==true){
			SoundFile:=this.SoundFile
			try{
				FileDelete, %A_ScriptDIR%\Sound1.AHK
			}
			catch e
			{}
			FileAppend,
			(
				#NoTrayIcon
				SoundPlay, sound\action\%SoundFile%, Wait
			), %A_ScriptDir%\Sound1.AHK
			Run, %A_ScriptDIR%\Sound1.AHK
		}
	}
	LastPlaySoundScript{
		Get{	

			FileRead , soundScript, %A_ScriptDIR%\Sound1.AHK
			return soundScript
		}
	}
			
}
Class Pause extends AnimationElement{
	Time:=0
	__New(time){
		this.Time:=time
	}
	Play(completeEvent=true){
		if(completeEvent==true){
			sleep % this.TIme
		}
	}
}
Class FXColor{
	Blue:=0
	Red:=0
	Green:=0
}
Class FXEffect extends AnimationElement{
	Persistent:= false
	Effect:=""
	PlayWithNextElement:=false
	Colors:={}
	__New(effect, persistent:=false, playWithNextElement:=false){
		this.Effect:=effect
		this.Persistent:=persistent
		this.PlayWithNextElement:=playWithNextElement
		this.Colors[1]:=new FXColor()
		this.Colors[2]:=new FXColor()
		this.Colors[3]:=new FXColor()
		this.Colors[4]:=new FXColor()
	}
	
	CostumeText{
		Get{
			name:=this.Character.Name
			location:="..\costumes"
			file:=name . ".costume"
			newFolder:= location . "\" . name
			FXName:=this.parseFXName(this.Effect)
			newPath:= newFolder . "\" . name . "_" . FXName . ".costume"
		
			FileRead costumeText, %newPath%
			return costumeText
		}
	}
		
	Play(completeEvent=true){
		name:=this.Character.Name
		location:="..\costumes"
		file:=name . ".costume"
		origPath:=location . "\" . file
		newFolder:= location . "\" . name
		
		FXName:=this.parseFXName(this.Effect)
		newPath:= newFolder . "\" . name . "_" . FXName . ".costume"
		
		if(FileExist(newFolder)<> true){
			FileCreateDir % newFolder
		}
		if(FileExist(newPath)){
			FileDelete % newPath
		}
		if(FileExist( origPath)){
			this.insertFXIntoCharacterCostumeFile(origPath,newPath, this.Effect)
			
			generator:=this.Character.Generator
			fxCostume:= name . "\" . name . "_" . FXName
			generator.GenerateKeyBindsForEvent("LoadCostume", fxCostume)
			if(this.PlayWithNextElement <> True and completeEvent == true){
				generator.CompleteEvent()
			}
			;sleep 300
		}
		if(this.Persistent==true){
			this.archiveOriginalCostumeFileAndSwapWithModifiedFile(name, newPath)
		}
		return newPath
	}
	
	archiveOriginalCostumeFileAndSwapWithModifiedFile( name, newPath){
		origPath:="..\costumes\" . name . ".costume"
		archivePath:= "..\costumes\" . name . "\" . name .  "_original.costume"
		if( FileExist(archivePath) == ""){
			FileCopy %origPath%, %archivePath% , 1
		}
		FileCopy %newpath%, %origPath% , 1
	}
	parseFXName(FX){
		FoundPos := RegExMatch(FX, "\w+\.fx" , fxName)
		return fxName
	}
	insertFXIntoCharacterCostumeFile(origPath, newPath){
		FileRead fileStr, %origPath%
		fxNone:= "Fx none"
		fxNew:= "Fx " . this.Effect
		output:=StrReplace(fileStr, fxNone , fxNew ,,1)
		fxPos := InStr(output, fxNew)
		partStart:= InStr(output,"Color1",, fxPos)
		partEnd:= InStr(output,"}",, fxPos)
		outputStart:=Substr(output, 1 , partStart -1)
		
		outputEnd:= SubStr(output, partEnd , Strlen(output) - partEnd)
		outputColors:= outputcolors . "Color1 " .  this.colors[1].red ", " . this.colors[1].green ", " . this.colors[1].blue . "`n"
		outputColors:= outputcolors . "`tColor2 " .  this.colors[2].red ", " . this.colors[2].green ", " . this.colors[2].blue . "`n"
		outputColors:= outputcolors . "`tColor3 " .  this.colors[3].red ", " . this.colors[3].green ", " . this.colors[3].blue . "`n"
		outputColors:= outputcolors . "`tColor4 " .  this.colors[4].red ", " . this.colors[4].green ", " . this.colors[4].blue . "`n"
		
		output:= outputStart . outputColors . outputEnd
		FileAppend %output%, %newPath%
	}
}
Class NestedAnimation extends AnimationElement{
	AnimationElements:={}
	SequenceType:="And"
	__New(SequenceType:="And"){
		this.SequenceType:=sequenceType
	}
	Play(completedEvent=true){
		elements:=this.AnimationElements
		if(this.SequenceType=="And"){
			for order, element in elements{
				element.Play(completedEvent)
			}
		}
		else{
			Random, chosen ,1, elements.MaxIndex()
			this.elements[chosen].Play()
		}
	}
}
Class ReferenceAbility extends AnimationElement{
	Reference:=""
	__New(reference){
		this.Reference:=reference
	}
	Play(completedEvent=true){
		this.Reference.Character:=this.Character
		this.Reference.Play(completedEvent)
	}
}

Yunit.Use(YunitStdOut).Test(AnimatedCharacacterTestSuite)
	
	
		
		
		
		
		




		


		
	
	
	
	
	
	
	
	
			
		
		