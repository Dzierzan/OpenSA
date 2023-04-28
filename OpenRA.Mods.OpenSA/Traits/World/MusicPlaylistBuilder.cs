#region Copyright & License Information

/*
 * Copyright The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System.Collections.Generic;
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.World
{
	public class MusicPlaylistBuilderInfo : TraitInfo
	{
		public readonly string Shellmap;
		public readonly string Extension;
		public readonly Dictionary<string, string> Tilesets = new Dictionary<string, string>();

		public override object Create(ActorInitializer init)
		{
			return new MusicPlaylistBuilder(init.World, this);
		}
	}

	public class MusicPlaylistBuilder
	{
		public MusicPlaylistBuilder(OpenRA.World world, MusicPlaylistBuilderInfo builderInfo)
		{
			string songFile;

			if (world.Map.Visibility == MapVisibility.Shellmap)
				songFile = builderInfo.Shellmap;
			else
				builderInfo.Tilesets.TryGetValue(world.Map.Tileset, out songFile);

			if (songFile == null)
				return;

			var music = new MusicInfo(songFile, new MiniYaml("Music")
			{
				Nodes = new List<MiniYamlNode>
				{
					new MiniYamlNode("Extension", builderInfo.Extension)
				}
			});

			music.Load(Game.ModData.ModFiles);

			typeof(Ruleset)
				.GetField(nameof(Ruleset.Music), BindingFlags.Instance | BindingFlags.Public)?
				.SetValue(world.Map.Rules, new Dictionary<string, MusicInfo> { { "Music", music } });
		}
	}
}
