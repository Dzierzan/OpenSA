#region Copyright & License Information
/*
 * Copyright 2019-2020 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	class PaletteFromDdfInfo : TraitInfo, IProvidesCursorPaletteInfo
	{
		[FieldLoader.Require]
		[PaletteDefinition]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Filename to load")]
		public readonly string Filename = null;

		public readonly bool AllowModifiers = true;

		[Desc("Whether this palette is available for cursors.")]
		public readonly bool CursorPalette = false;

		public override object Create(ActorInitializer init) { return new PaletteFromDdf(init.World, this); }

		string IProvidesCursorPaletteInfo.Palette { get { return CursorPalette ? Name : null; } }

		ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
		{
			var colors = new uint[Palette.Size];

			using (var s = fileSystem.Open(Filename))
			{
				for (var i = 0; i < 256; i++)
				{
					var r = s.ReadUInt8();
					var g = s.ReadUInt8();
					var b = s.ReadUInt8();
					var a = s.ReadUInt8();
					colors[i] = (uint)Color.FromArgb(a == 4 ? 0xff : 0x00, r, g, b).ToArgb();
				}
			}

			return new ImmutablePalette(colors);
		}
	}

	class PaletteFromDdf : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly World world;
		readonly PaletteFromDdfInfo info;

		public PaletteFromDdf(World world, PaletteFromDdfInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			wr.AddPalette(info.Name, ((IProvidesCursorPaletteInfo)info).ReadPalette(world.Map), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
