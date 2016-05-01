#Include AnimationManager.ahk



!x::
{
	MouseGetPos X, Y	
	i=0
	while(i< 10){
		MouseClick, Right, x, y 
		sleep 50
		Mouseclick Right
		sleep 50
		Send r
		 
		x:=x+40
		
		i++
	}
	return
		
 }
 
 !^b::
 {
	
	SoundPlay sound\chimes.wav		
			
	manager:= AnimationManager.GetInstance()
	animFile:=GrabText()
	manager.BuildBadgesForAnimations(animFile)
	return
}

GrabText(){
		Clipboard:=""
		Send ^c
		sleep 1000
		var:=Clipboard
		var:=RegExReplace(var, "\r\n$","")
		return var
	}