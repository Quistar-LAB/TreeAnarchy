# Tree Anarchy 
### __(AKA Unlimited Trees: Rebooted)__
<p>
<a href="LICENSE">
	<img src="https://img.shields.io/badge/license-MIT-green" />
</a>
</p>
<br>&nbsp;
#### Requires: Harmony 2.0.x ported over to Cities Skylines by [Bofomer](https://github.com/boformer) -- [Github](https://github.com/boformer/CitiesHarmony)
#### Requires: MoveIt by [Quboid](https://github.com/Quboid) -- [Github](https://github.com/Quboid/CS-MoveIt)
TreeAnarchy is a reboot of the original Unlimited Trees Mod with focus on performance and added functionality of tree random rotation and tree snapping.

This mod is a complete rewrite, taking out bloats, fixing bugs and streamlining the serialization process. The concept is almost the same, and I wouldn't have been able to write this code this fast without the original Unlimited Trees mod creators. Thanks to
[Knighth](https://github.com/Knighth/TreeUnlimiter), 
[BloodyPenguin](https://github.com/bloodypenguin), 
[DRen72](https://steamcommunity.com/id/DRen72/myworkshopfiles/?appid=255710).

This mod is currently __BETA__, with the following functions:
- [x] Support for more than 262144 trees
- [x] Support Tree Snapping with functionality just like Prop Snapping
- [x] Support for tree resizing by pressing hotkeys
- [x] Support for random tree rotation
- [x] Lock Forestry (especially useful if you want to control where forestry resources are created)
- [ ] Performance tuning to attempt to increase fps to support more trees

This mod fixes a couple issues that existed in the old unlimited trees mod:
- When enabling the old Unlimited Trees mod, forestry resources could not be created. This mod fixes that issue.
- In the old Unlimited Trees mod, the core framework of rendering trees were detoured, and was never updated to reflect the current status of the game for 3 years. This caused poor rendering performance of trees.
- Trees would appear below the terrain if tree assets are missing when loading a map with more than 262144 trees. This is due to the old mod never associating its infoindex with PrefabCollection.
- Old mod was not friendly with ULOD due to its use of detours.

Reasons for including random tree rotation, lock forestry and tree snapping in to this mod:
- This mod utilizes the default in-game fixed height variable to realize tree snapping behavior, thus removes the random bugs of trees flying in the old tree snapping mod, when used with the old unlimited trees mod.
- Tree Movement Control and Random Tree Rotation mods both utilize Eular calculation in the function that renders all trees on the map. This causes the rendering of each tree to increase approximately 0.03ms per tree on my computer, and if you have lots of trees in your camera view, the rendering time will increase drastically reducing your FPS. I implemented a different framework for random tree rotation effect so that FPS would not be hit when rotating trees.

This mod is incompatible with the following mods:
- All previous versions of Unlimited Trees and Unlimited Trees: Revisited mods.
- Tree Snapping mod

The following mods can be used along with this mod, but is not recommended:
- Tree Movement Control and Random Tree Rotation mod. Both these mods will override the effects rotation effect of this mod.

This mod requires:
- MoveIt mod
- Harmony

I need supporters/volunteers to help debug/code to make this mod even better. If you want to contribute, please contact me anytime.

Anyways, these codes are open to the public, as its a hobby of mine. If you wish to contribute to the codes, please join in.

IMPORTANT!! As always, create a new save!!! This mod creates a new version of saved datas. Original mod formats loading are supported, but then are saved into the new format.

