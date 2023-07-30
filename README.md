# About

This repository contains a recreated Swarm Assault game in OpenRA engine. The goal of this project is to bring modern controls and quality life improvements to Swarm Assault game released in 1999 while still being faithful as much as possible to the original source material. Although the project does not hesitate to add new content which enriches the overall gameplay.

# Current status

At the moment the project is highly complete, all required features are there and what's left is purely cosmetical or very minor. Moreover all 100 missions have been recreated and the project adds several more additional levels to play.

# Swarm Assault vs OpenSA

Since OpenSA project is based on the OpenRA Mod SDK, it carries on a lot of quality life improvements. Here are the key features:

- open source,
- multiplayer,
- Windows, macOS and Linux support,
- support of HD resolutions,
- seamless zooming in and out,
- rally points,
- units controlled by player are less likely to wander than units controlled by AI,
- advanced map editor with the ability of creating own missions with objectives,
- ability of adding own units to the game,
- there's no random map generator in OpenSA, however the project is able to convert legacy maps to ORA format, so nothing is lost.

# How to compile/play

To launch your project from the development environment you must first compile the project by running `make.cmd` (Windows), or opening a terminal in the SDK directory and running `make` (Linux / macOS).  You can then run `launch-game.cmd` (Windows) or `launch-game.sh` (Linux / macOS) to run your game.

For more detailed instructions, I recommend reading this: https://github.com/OpenRA/OpenRA/blob/bleed/INSTALL.md

Otherwise, go to releases and get the latest stable version.

# Swarm Assault assets status

This project does not include any libre assets. It still requires original Swarm Assault. The game itself hasn't been sold for more than 20 years, the developers are unreachable and the last publisher Mountain King Studios confirmed they do not possess the IP nor they have any contact to the original developers. It's safe to assume the game is abandonware. For this reason, when you launch OpenSA for the first time, the assets will be downloaded and only assets. No exe files, so the original game cannot be run.

# Legal disclaimers

* This project is not affiliated with or endorsed by Gate 5 Software or Mountain King Studios in any way.
* This project is non-commercial. The source code is available for free and always will be.
* You are free contribute to this repository, however your contribution must be either your own original code, or open source code with a
  clear acknowledgement of its origin.

# Special thanks

I would love to thank:

* **Andre Mohren** ([IceReaper](https://github.com/IceReaper)) for his expertise in reverse engineering game asset formats. Without this knowledge, this project wouldn't be possible.

* **Matthias Mailänder** ([Mailaender](https://github.com/Mailaender)) for helping out with writing a lot of custom traits which pushed this project forward.

* **Zimmermann Gyula** ([GraionDilach](https://github.com/GraionDilach)) for using his traits written for his [Attacque Superior](https://github.com/AttacqueSuperior/Engine) project.

* **sayedmamdouh ([MikillRosen](https://github.com/MikillRosen))** for creating extra cursors and Ant Hole variants.

* **phredreeke** for upscaling logos which are used for mission preview images.

* **Hooet (교니체)** for creating new missions.

* OpenRA developers for creating this open source engine.

* The original team who created Swarm Assault game.

# Screenshots

![](https://imgur.com/Fiv7Kux.png)
![](https://imgur.com/3MVhFSn.png)
![](https://imgur.com/b8tsTs6.png)
![](https://imgur.com/C6QqtJS.png)
