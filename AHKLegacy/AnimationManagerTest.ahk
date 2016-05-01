#Include CharacterManager.ahk
#Include AnimationManager.ahk
#Include Yunit\Yunit.ahk
#Include Yunit\Window.ahk
#Include Yunit\StdOut.ahk

Class AnimationTestHelper
{
	manager:=""
	LoadAnimations(){
		a:= AnimationManager.GetInstance()
		
		a._characterAnimations:={}
		realllySimple:= "mov0"
		simpleAnd:= { Sequence: "AND", movs:["mov1", "mov2" ,"mov3"]}
		simpleAnd2:= { Sequence: "AND", movs:["mov4", "mov5" ,"mov6"]}
		nestedChild1:={ Sequence: "AND", movs:["mov7", "mov8" ,"mov9"]}
		nestedChild2:={ Sequence: "AND", movs:["mov10", "mov11" ,"mov12"]}
		complexNested:= { Sequence: "AND", movs:[nestedChild1, "mov5" ,nestedChild2]}
		
		character:= CharacterModel.BuildValidCharacter({name:"TestCharacter"})

		charAnims:=a.GetCharacterAnimations(character)
		a._characterAnimations["reallySimple"]:= realllySimple
		a._characterAnimations["simpleAnd"]:= simpleAnd
		a._characterAnimations["simpleAnd2"]:= simpleAnd2
		a._characterAnimations["complexNested"]:= complexNested
		this.manager:=a
		return a
	}
		
	AssertAnimationsAreEqual(actual,valid){
		Yunit.AssertEquals(actual.Sequence , valid.Sequence)
		for key, mov in valid.movs{
			if(mov is alnum){
				Yunit.AssertEquals(actual.movs[key] , mov)
			}
			else{
				if(mov[1].pause<>""){
					this.AssertAnimationsAreEqual(actual.movs[key] , valid.mov)
				}
				else{
					Yunit.AssertEquals(mov[1].pause , actual.movs[key][1].pause)
				}
			}
		}
		Yunit.AssertEquals(actual.NumKey , valid.NumKey)
	}
		
	EvaluateAnimation(value){
		a:= this.manager
		a.EvaluateMovAsAnAnimation(mov)
		return key
	}

	PlayAnimationFor(charName, animation ){
		a:= this.manager
		char:= CharacterModel.BuildValidCharacter({name:charName})

		a.PlayAnimationFor(char, animation)
		return a.Animations[animation]
	}
	Assert_AnimationCommandStringCorrect(validCommands, actualCommands){
		for key, valid in validCommands{
			Yunit.AssertEquals(actualCommands[key] , valid)
		}	
	}
	
	PlayandAssertAnimationIsValid(animKey, validCommands){
		a:= this.manager
		actualCommands:=a.PlayAnimation(animKey)
		this.Assert_AnimationCommandStringCorrect(validCommands, actualCommands)
	}

}
Class AnimationManagerTest{
	helper:= ""
	Begin(){
		this.helper:=new AnimationTestHelper()
		this.helper.LoadAnimations()
	}
	
	
	TestAnimationManagerAnimations_GetsCharacterFile(){
		a:= this.helper.manager
		actual:=a.Animations["simpleAnd"]
		valid:= { Sequence: "AND", movs:["mov1", "mov2" ,"mov3"]}
		this.helper.AssertAnimationsAreEqual(actual,valid)
	}
	TestAnimationManagerPlayAnimationFor_PlayOneLinerAnimBuildsCorrectCommandString(){	
		animKey:= "reallySimple"
		validCommands:=[]
		validCommands[1]:="""spawn_npc mov1 mov0$$mov mov0"""		this.Helper.PlayandAssertAnimationIsValid(animKey, validCommands)
	}
	TestAnimationManagerPlayAnimationFor_PlaySimpleANdOrAnimBuildsCorrectCommandString(){	
		animKey:= "simpleAnd"
		validCommands:=[]
		validCommands[1]:="""spawn_npc mov1 mov1$$mov mov1"""
		validCommands[2]:="""spawn_npc mov2 mov2$$mov mov2"""
		validCommands[3]:="""spawn_npc mov3 mov3$$mov mov3"""
		this.Helper.PlayandAssertAnimationIsValid(animKey, validCommands, "mov0")
		
		validCommands:=[]
		validCommands[1]:="""spawn_npc mov4 mov4$$mov mov4"""
		validCommands[2]:="""spawn_npc mov5 mov5$$mov mov5"""
		validCommands[3]:="""spawn_npc mov6 mov6$$mov mov6"""
		animKey:= "simpleAnd2"
		this.Helper.PlayandAssertAnimationIsValid(animKey, validCommands)
	}
	TestAnimationManagerPlayAnimationFor_PlayNestedAnimBuildsCorrectCommandString(){	
		animKey:= "complexNested"
		validCommands:=[]
		validCommands[1]:="""spawn_npc mov4 mov1$$mov mov7"""
		validCommands[2]:="""spawn_npc mov5 mov1$$mov mov8"""
		validCommands[3]:="""spawn_npc mov6 mov1$$mov mov9"""
		validCommands[4]:="""spawn_npc mov4 mov1$$mov mov10"""
		validCommands[5]:="""spawn_npc mov5 mov1$$mov mov11"""
		validCommands[6]:="""spawn_npc mov6 mov1$$mov mov12"""
		this.Helper.PlayandAssertAnimationIsValid(animKey, "Y ""mov mov0""", "mov0")
	}
	
	
	TestCorrectPathAndAnimationAreSet(a, charRecovery, charName, globalRecovery){
		a:=this.manager
		valid:=a.Path
		actual:="..\data\\" . charName . ".animations"
		
		Yunit.AssertEquals( valid, actual)
	}
}


Yunit.Use(YunitStdOut).Test(AnimationManagerTest )