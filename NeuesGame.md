- Game mit Alchemy

- Neue Substanzen und Materialien sammeln durch Mobs oder aus der Natur

- Konsumieren um Effekte zu erfahren

- Kombinieren um neue Substanzen zu erstellen



** Fokus des Games mit den Entstandenen Potions und Materials staerker werden und neue Rezepte fuer Waffen freischalten oder diese Verkaufen und interessantes NPC interaktionssystem machen wo wenn der NPC an deinem Shit stirbt du nen schlechtes Review bekommst **

- So oder so simples Kampfsystem bei dem die Waffe um dich hovert und mit weissem Schwung/Trail angreifen kann


TODO als Basis:

x 2.5D Environment Ausprobieren
x Kessel zum Kombinieren von N Materialien, immer das was drinne liegt mit dem Was reingehauen wird
x Simples Movement mit Jump
x Simple Attack mit einem Dagger mit Trail
- Slime Mob mit Slime Drop
x Kann dann Kombiniert werden im Kessel zu Quecksilber II, welches den Spieler bei Use killt
- Item kann geused werden
x Simple Inventarleiste oder 3x3 Grid


# TODO vorher:
	
	x gameManager mit paar globals
	x camera based movenent
	x axe raycast fixen
	x lighting fixen bzw aendern
	x wasser shader mit cell noise simpel aber mit reflections
	x poof particles mit no depth test testen
	x kleine zoom ui die mir den aktuellen zoom anzeigt und dann wegfaded
	x falling leafes texturen aendern fuer baeume
	
	# Features
	
	## Player Anim
	- movement fixen indem alles sich nur in pixeln bewegen kann
	- blockartige anim mit low fps

	-> maybe die atzen mit baelle als haende und den grossen emojis als gesicht oder so
	
	
	## Mechanics
	- actually mechanics implementieren
	- fishing minigame
	- fishing minigame
	- color quantization und palettew
	- aus quecksilber II eine lupe die einem anzeigt was actually im cauldron drinne is

	## Heute SOFORT MACHEN JAAAAA
	--> kleine shrek ahh huette zum chillen drinne aus holz, wird hingebaut wenn der player 10 planks hat
	
	
	# right now
	
	mond der sich im wasser reflektieren kann, ozean im norden hin hauen
	x steg am wasser
	x grass spitzen color per noise
	x tag und nacht kleines script zum wechseln zum testen usw was env coloru und directional lights switch
	
	sound aendern mal wieder
	
	x cloud shadows
	x feuer mit trail wie ds1
	x grass fps
	x wind streaks anpassen
	x trees neue sachen ausprobiern
	
	
	## Engine Recompilen mit Changes
	
	-> Custom Prjection Matrix setten fuer oblique projection damit stuff im wasser ne mit reflected wird
	
	
	## Visuals
	x water shader mit reflections
	- regen mit splashes auf dem boden
	- water + regen = ripples im wass /auf dem wasser als particle

	- outline shader fixen
	- cloud shadows abgestuft machen/soft edges kp wie tho
	
	- reflections fuer wasser mit oblique projection aka second camera wie ich schon probiert hatte damit das wasser transparent sein kann
	
	- player outline wenn hinter objekten mit stencil buffer
	
	- neue sounds 
	
	## Foliage Upgrades
	- bessere noise fuer rolling grass und leaves und grass-colour	
	- grass noise textur res runter damit die framerate der animation runter geht oder sowas in die richtung
	- wind sound effekt mit grass wind kombinieeren -> 1 sfx der random spielt -> dann per korve den wind machen und im shader setzten
	
	# Fixes
	- nochmal nachschauen wie ich komplett harte schatten bekomm
	- axe raycast maybe mal so fixen, dass das mit nicht statischem zoom auch geht
	- bei bsp. pillars die leaves fixen die muessen ne irgendwo hinrotiert werden
		---> enteder andern shader um auch das movement zu haben oder anders
	
	
	- DIGGA MAYBE EINFACH MAL NICH ALS POST PROCESSING EFFECT MACHEN SONDERN ALS MATERIAL / MATERIAL OVERLAY ODER EXTRA MATERIAL PASS... ok nvm funkt ne
	https://godotforums.org/d/41442-make-a-shader-ignore-specific-objects
	===> depth-composite und grass oder so in separate viewports rendern
	- TODO wenn bock auf dogshit: FIX OUTLINE COMPUTE SHADER
	- fix random memory corruption
	- play testproject from the video to test if it hast similar problems
	- aaaaa
	- fix depth buffer being entirely misaligned and seemingly ignoring my camera snapping which perfectly works in the rendered image
	- maybe durch uv's quantizen aka steppen


    # Random Ideen

    - Intro Sequence wo player aus dem himmel gefallen kommt, faded langsam in die natur rein und dann kommt der atze an
	
	- lotus auf dem wasser
	
	- biom mit anderer grass colour -> 	dark	mid		light	extra?	mit AGX 1.5 exposure getestet
										F08787 	FFC7A7 	FEE2AD 	F8FAB4
										EAEBD0	DA6C6C	CD5656	AF3E3E
