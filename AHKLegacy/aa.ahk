
location:={x:0, y:0}
location.x:=10
location.Y:=10

rotation:=45

pi:=3.141592653589793
radians:= rotation * pi/180

rotated:={x:0, y:0}

rotated.x:=location.x * cos(radians) - location.y * sin(radians)
rotated.y:=location.y * cos(radians) + location.x * sin(radians)


MsgBox  % rotated.x . " "  . rotated.y