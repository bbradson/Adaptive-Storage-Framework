# Adaptive Storage Framework
![](About/Preview.png?raw=true)  
  
![](https://i.imgur.com/yMY2n8Q.png)  
Adaptive Storage Framework contains various features to allow modders to better shape the way they want to make and change their own storage mods. It has added functionalities to change graphics depending on contents, tweak how items get displayed, how many graphics get displayed, how they get colored, how to display text and so on. A couple other features like more fine grained storage capacity control and temperature controls are included too.

Visible to users, the framework itself always adds a contents tab, a group tab for buildings in a storage group and enhances selection behaviour for storage buildings. A collection containing mods using this framework is available [here](https://steamcommunity.com/sharedfiles/filedetails/?id=3381351248). Let us know if you believe another mod to belong there.  
  
You can also include this banner in your mod page to signify that your mod requires ASF to work.  
  
[![](https://i.imgur.com/lwmy11p.png)](https://steamcommunity.com/sharedfiles/filedetails/?id=3033901359)  
  
![](https://i.imgur.com/sZlny9g.png)  
  
Q: Where can I read about these functionalities? How can I use them for my mod?  
A: Documentation can be found here: https://github.com/bbradson/Adaptive-Storage-Framework/blob/main/Docs/modules/ROOT/pages/index.adoc  
  
Q: Is this compatible with LWM Deep Storage?  
A: Yes! LWM is compatible and will not interfere with any of our features, in fact, the two compliment each other. Additionally, there are mod settings in ASF for you to choose between some systems.  
  
Q: Is this compatible with [insert storage mod here]?  
A: ASF only adds extra functionalities to be used. It doesn’t alter already existing features by itself, especially not making any changes to haul AI or similar. Almost all of its code is about rendering and UI. It should be compatible with most mods out there.  
Multiplayer, Combat Extended, Vanilla Expanded, Pick Up And Haul are definitely compatible. Performance mods are compatible too.  
  
Q: How do I tweak storage stats?  
A: Unlike deep storage, this doesn't have a custom made settings menu only for buildings made with the mod. It is instead compatible with [RIMMSqol](https://steamcommunity.com/sharedfiles/filedetails/?id=1084452457) however. Every storage building can have the majority of its stats freely adjusted there.  
Quick edits for modding purposes can additionally be made with the edit graphics button that shows up on buildings when having god mode active. These god mode changes don't get saved though.  
  
Q: How does this impact tps and fps? Can this cause lag?  
A: This framework's custom rendering and various caches result in buildings implemented using it outperforming basic vanilla shelves, improving fps. Those shelves are not automatically patched, but do benefit when set to use this framework as well. Phaneron's and Reel's Storage currently include relevant configs. Putting items inside of Adaptive Storage buildings reduces their tps overhead compared to having them on the floor, regardless of buildings hiding contents or keeping them visible with custom offsets, scaling and optionally a transparent lid on top. Details on some of what's been done to achieve that can be read up on here: https://github.com/bbradson/Adaptive-Storage-Framework/blob/main/Docs/modules/ROOT/pages/Performance.adoc  
There should never be situations where this negatively affects performance, compared to vanilla alternatives. If something does come up, please report it with a minimal modlist, Dub's Analyzer screenshots comparing framework and an alternative implementation, like a shelf for example, and a log. That kinda cases are definitely bugs, and very likely mod conflicts.  
  
Q: Do you accept suggestions?  
A: Yes! If you have a well thought out idea, please comment it down below or open a GitHub issue to let us know what you think the mod is missing.  
  
![](https://i.imgur.com/wR1GLB9.png)  
  
Hard incompatibilities:  
- Stockpile Stack Limit (Continued) has been reported to break the vanilla multiple items per cell functionality, which this relies on  
  
Soft Incompatibilities:  
- Designators from mods like Recycle This may appear inside containers with wrong offsets and no direct UI support  
  
![](https://i.imgur.com/cjnms8R.png)  
  
Got a bug? Report it at our Github Issues page or in the PINNED bug report thread, under discussions. Make sure to verify that it's only happening with this mod active, write down as much information as you can about the bug AND include a hugslib log. Bugs must be reproducible with exact known mod combinations for a reasonable chance of them getting fixed.  
Without a log, or with a modlist that takes hours just to download, your bug report will be ignored.  
  
![](https://i.imgur.com/HURdXkE.png)  
![](https://i.imgur.com/1Q93im9.gif)  
  
If you would like to support the team, please click on our images!  
  
[![](https://i.imgur.com/fdgGtLf.png)](https://ko-fi.com/thesoulknitter)  
[![](https://i.imgur.com/VL2IQrU.png)](https://steamcommunity.com/profiles/76561198105726482/myworkshopfiles/?appid=294100)  
[![](https://i.imgur.com/UVTnPfV.png)](https://ko-fi.com/bbradson)  
  
![](https://i.imgur.com/kbCDL4b.png)  
  
ASF and all of its modules are fully open source!  
That means you are free to use the source material however you please. The only thing we ask of you is to credit the original authors.  