using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.World
{
	class PaletteFromDdfInfo : ITraitInfo, IProvidesCursorPaletteInfo
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

		public object Create(ActorInitializer init) { return new PaletteFromDdf(init.World, this); }

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
		readonly OpenRA.World world;
		readonly PaletteFromDdfInfo info;

		public PaletteFromDdf(OpenRA.World world, PaletteFromDdfInfo info)
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
