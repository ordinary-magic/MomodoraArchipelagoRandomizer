# MomodoraArchipelagoRandomizer
 A LiveSplit component that is a randomizer for Momodora: Reverie Under the Moonlight. Currently supporting game versions v1.05b and v1.07

 ## Known issues/quirks:
 Active items may have incorrect amount of uses when equipped, this is fixed when saving.
 Game may crash sometimes when opening key items menu.
 Fresh Spring Leaf may be in your inventory, but the green leaf effect is tied to a flag so unless the randomizer tells you that you have found Fresh Spring Leaf you shouldn't have it.
 Losing internet connection will desync you from archipelago.
 
 ## Installation
 First, make sure that you have a LiveSplit installation http://livesplit.org/downloads/.
 
 1. Download the "MomodoraArchipelagoRandomizer.dll" from releases. ("Releases" link over there --->)  
 2. Navigate to your LiveSplit install, and add all three .dll files into LiveSplitDir/Components  
 3. Have the server host add "momo4.apworld" to the "Custom Worlds" folder (or double-click it).  
 4. Launch LiveSplit and Edit Layout, add the Momodora Randomizer component from the "Other" category.  
 ![image](https://user-images.githubusercontent.com/26115597/154794577-4dd8d8fb-a589-4a48-b257-a73940f76956.png)
 5. Edit the layout settings to add your Archipelago Sever information. 
 ![Image](https://github.com/user-attachments/assets/f7091a6a-8951-48e1-a9bf-6a45597d9d2d)

 ## How to use
 It is inteded to start the randomizer from a fresh launch of the game on a fresh savefile. Starting a randomizer in other circumstances may lead to instabilities/crashes.
  
To start a randomizer, simply start the timer. This should be done when you have started a new file and the Momodora: Reverie Under the Moonlight autosplitter works well for this. Once a run has started, a text will appear on the component in LiveSplit saying "1.05b/1.07 randomizer ready to go!" if it correctly detected the game.
 
 During play picking up an item will only display the original item that you were supposed to get, you have to look at the randomizer component to see the display of what item you actually found.
 
 Whenever an active item is picked up it won't be useable until you save at a bell.

 ## Settings  
 All of the in game settings can be adjusted within your archipelago yaml file:
 1. Open archipalgo.  
 2. Click "Generate Template Options"  
 3. Make a copy of "Momodora 4 - Reverie Under the Moonlight.yaml", and open the copy with a text editor.  
 4. Follow instructions within the file to edit the settings.  
 5. Place it in your archipelgo players folder, or send it to whomever is running the multiplayer server.  

 ## How to build the livesplit client:
 You don't actually need visual studio (tho it might be easier), but you do need [visual studio build tools](https://visualstudio.microsoft.com/downloads/) from 2019 or later.
 You need [.NETFramework Version 4.6.1] (https://dotnet.microsoft.com/en-us/download/dotnet-framework/net461)
 
 Run the build.ps1 script (in powershell), which will create a "MomodoraArchipelagoRandomizer.dll" file in the build directory, and copy all relevant dlls.

 ## How to build the archipelago multiworld:
Run "Archipelago/build.py" which will package the multiworld and put it in the build directory.
 
 ## Credits
 The original randomizer published by [DreamFox](https://github.com/axelkarlsson/MomodoraRandomizer) that i used as the basis for this.
