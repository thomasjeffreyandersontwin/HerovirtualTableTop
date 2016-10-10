#SingleInstance Force
#NoEnv

Gui Add, Button, gX-Y x50 w200 Center, &1   x y (base)
Gui Add, Button, gXP-YP x50 w200 Center, &2   xp yp (top-left)
Gui Add, Button, gXM-YM x50 w200 Center, &3   xm ym (margin)
Gui Add, Button, gXS-YS x50 w200 Center, &4   xs ys (section)

Gui Show, w300, Control Position Reference
Return


X-Y:

Gui 2:Add, Button, g2X, Default first control placement
Gui 2:Add, Button, g2X, Default next placement

Gui 2:Add, Text, x50 y150 Center, 0:   (Absolute: x50 y150)`nMoves relative to`nbottom or right`nof previously`nadded control

Gui 2:Add, Button, g2X y+150, 1: v`ny+150
Gui 2:Add, Button, g2X x+150, 2: >`nx+150
Gui 2:Add, Button, g2X y+-150, 3: ^`ny+-150
Gui 2:Add, Button, g2X x+-150, 4: <`nx+-150

Gui 2:Add, Button, g2X x100 y100, 5: Absolute: x100 y100
Gui 2:Add, Button, g2X x100, 6: Absolute: x100 (no y)`nBeneath all previously added controls
Gui 2:Add, Button, g2X y100, 7: Absolute: y100 (no x)`nRight of all previously added controls

Gui 2:Show, w500 h500, Default and base positions
Return


XP-YP:

Gui 2:Add, Text, x150 y50 Center, 0:   (Absolute: x150 y50)`nMoves relative to`ntop-left corner`nof previously`nadded control

Gui 2:Add, Button, g2X xp+200, 1: >`nxp+200
Gui 2:Add, Button, g2X yp+150, 2: v`nyp+150
Gui 2:Add, Button, g2X xp-250, 3: <`nxp-250
Gui 2:Add, Button, g2X yp-100, 4: ^`nyp-100

Gui 2:Show, w500 h500, Top-left based positions
Return


XM-YM:

Gui 2:Add, Text, x50 y50 Center, 0:   (Absolute: x50 y50)`nMargin relative moves

Gui 2:Add, Button, g2X xm, 1: xm`nleft margin,`nbelow all previous controls`n(new row)
Gui 2:Add, Button, g2X ym, 2: ym`ntop margin,`non right of all previous controls`n(new column)
Gui 2:Add, Button, g2X xm+50, 3: xm+50`nleft margin + offset,`nbelow all previous controls
Gui 2:Add, Button, g2X ym+50, 4: ym+50`ntop margin + offset,`non right of all previous controls
Gui 2:Add, Button, g2X xm ym, 5: xm ym
Gui 2:Add, Button, g2X xm+50 ym+20, 6: xm+50 ym+20

Gui 2:Show, w500 h500, Margin based positions
Return


XS-YS:

n = 0
Gui 2:Add, Text, x20 y20 Center Section, %n%:   (Absolute:`nx20 y20, Section)`nSection relative moves
n++
Gui 2:Add, Button, g2X, %n%: Default`nsecond`ncontrol`nplacement
n++
Gui 2:Add, Button, g2X, %n%: Default`nnext`nplacement
n++

Gui 2:Add, Button, g2X ys, %n%: ys`nStart a new column`n(relative to previous`nSection declaration)
n++
Gui 2:Add, Button, g2X, %n%: Default
n++
Gui 2:Add, Button, g2X, %n%: Default _______
n++

Gui 2:Add, Button, g2X ys x+50, %n%: ys x+50`nNew column`nand move a bit on the right`n(relative to 5)
n++
Gui 2:Add, Button, g2X, %n%: Default
n++
Gui 2:Add, Button, g2X, %n%: Default
n++

Gui 2:Add, Button, g2X xs, %n%: xs`nNew row`nrelative to previous Section declaration
n++
Gui 2:Add, Button, g2X xs+100 Section, %n%: xs+100 Section`nNew row`ndeclare new Section
n++
Gui 2:Add, Button, g2X ys, %n%: ys
n++
Gui 2:Add, Button, g2X, %n%: Default
n++
Gui 2:Add, Button, g2X ys, %n%: ys
n++
Gui 2:Add, Button, g2X, %n%: Default
n++
Gui 2:Add, Button, g2X xs Section, %n%: xs Section`nNew row`nrelative to previous Section declaration,`ndeclare new Section
n++
Gui 2:Add, Button, g2X ys+20, %n%: ys+20
n++
Gui 2:Add, Button, g2X, %n%: Default
n++
Gui 2:Add, Button, g2X ys+30, %n%: ys+30
n++
Gui 2:Add, Button, g2X, %n%: Default
n++
Gui 2:Add, Button, g2X xs+100 ys+100, %n%: xs+100 ys+100
n++
Gui 2:Add, Button, g2X, %n%: Default
n++

Gui 2:Show, w500 h500, Section based positions
Return

2X:
2GuiClose:
2GuiEscape:
Gui 2:Destroy
Return

GuiClose:
GuiEscape:
ExitApp