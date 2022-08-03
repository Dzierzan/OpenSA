# About

This repository contains a recreated Swarm Assault game in OpenRA engine. The goal of this project is to bring modern controls and quality life improvements to Swarm Assault game released in 1999 while still being faithful as much as possible to the original source material.

# Current status

At the moment the project is highly complete, nearly all required features are there and what's left is purely cosmetical like HP bars or video support. Moreover all missions have been recreated. Feel free to give any feedback or even help to progress further this project.

# Swarm Assault vs OpenSA

Since OpenSA project is based on the OpenRA Mod SDK, it carries on a lot of quality life improvements plus additional stuff has been added to the project. Here are the key features:

- open source,
- multiplayer,
- support of HD resolutions,
- seamless zooming in and out,
- ability to queue units,
- rally points,
- units controled by player are less likely to wander than units controled by AI,
- more advanced map editor with the ability of creating own missions with objectives,
- ability of adding own units to the game,
- there's no random map generator in OpenSA, however the project is able to convert legacy maps to ORA format, so nothing is lost.

# How to compile/play

To launch your project from the development environment you must first compile the project by running `make.cmd` (Windows), or opening a terminal in the SDK directory and running `make` (Linux / macOS).  You can then run `launch-game.cmd` (Windows) or `launch-game.sh` (Linux / macOS) to run your game.

For more detailed instructions, I recommend reading this: https://github.com/OpenRA/OpenRA/blob/bleed/INSTALL.md

Otherwise, go to releases and get the latest stable version.

# Swarm Assault assets status

This project does not include any libre assets. It still requires vanilla Swarm Assault. The game itself hasn't been sold for more than 20 years, the developers are unreachable and the last publisher Mountain King Studios confirmed they do not possess the IP nor they have any contact to the original developers. It's safe to assume the game is abandonware. For this reason, when you launch OpenSA project for the first time, the assets will be downloaded and only assets. No exe files, so the vanilla game cannot be run.

# Legal disclaimers

* This project is not affiliated with or endorsed by Gate 5 Software or Mountain King Studios in any way.
* This project is non-commercial. The source code is available for free and always will be.
* You are free contribute to this repository, however your contribution must be either your own original code, or open source code with a
  clear acknowledgement of its origin.

# Special thanks

I would love to thank:

* Andre Mohren (IceReaper), his expertise in reverse engineering game asset formats was very helpful. Without this knowledge, this project wouldn't be possible.

* Matthias Mailänder (Mailaender) for helping out with writing a lot of custom traits which pushed this project forward.

* Zimmermann Gyula (Graion Dilach) for using his traits written for his Attacque Superior project (https://github.com/AttacqueSuperior/Engine).

* MikillRosen (sayedmamdouh) for creating extra cursors and Ant Hole variants.

* phredreeke for upscaling logos which are used for mission preview images.

* OpenRA developers for creating this open source engine.

* The original team who created Swarm Assault game.

# Screenshots

![](https://media.moddb.com/cache/images/mods/1/42/41459/thumb_620x2000/OpenRA-2020-05-31T094932231Z.png)
![](https://media.moddb.com/cache/images/mods/1/42/41459/thumb_620x2000/OpenRA-2020-05-31T085512814Z.png)
![](https://media.moddb.com/cache/images/mods/1/42/41459/thumb_620x2000/OpenRA-2020-05-31T085338835Z.png)
![](https://media.moddb.com/cache/images/mods/1/42/41459/thumb_620x2000/OpenRA-2020-05-31T085934410Z.png)
![](https://media.moddb.com/cache/images/mods/1/42/41459/thumb_620x2000/OpenRA-2020-05-31T090120130Z.png)
