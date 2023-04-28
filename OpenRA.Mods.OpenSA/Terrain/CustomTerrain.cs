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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.OpenSA.Terrain
{
	public class CustomTerrainLoader : ITerrainLoader
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "IDE0060:Remove unused parameter", Justification = "Load game API")]
		public CustomTerrainLoader(ModData modData) { }

		public ITerrainInfo ParseTerrain(IReadOnlyFileSystem fileSystem, string path)
		{
			return new CustomTerrain(fileSystem, path);
		}
	}

	public class CustomTerrainTileInfo : TerrainTileInfo
	{
		public readonly float ZOffset = 0.0f;
		public readonly float ZRamp = 1.0f;
	}

	public class CustomTerrainTemplateInfo : TerrainTemplateInfo
	{
		public readonly string[] Images;
		public readonly int[] Frames;
		public readonly string Palette;

		public CustomTerrainTemplateInfo(ITerrainInfo terrainInfo, MiniYaml my)
			: base(terrainInfo, my) { }

		protected override TerrainTileInfo LoadTileInfo(ITerrainInfo terrainInfo, MiniYaml my)
		{
			var tile = new CustomTerrainTileInfo();
			FieldLoader.Load(tile, my);

			// Terrain type must be converted from a string to an index
			tile.GetType().GetField("TerrainType").SetValue(tile, terrainInfo.GetTerrainIndex(my.Value));

			// Fall back to the terrain-type color if necessary
			var overrideColor = terrainInfo.TerrainTypes[tile.TerrainType].Color;
			if (tile.MinColor == default)
				tile.GetType().GetField("MinColor").SetValue(tile, overrideColor);

			if (tile.MaxColor == default)
				tile.GetType().GetField("MaxColor").SetValue(tile, overrideColor);

			return tile;
		}
	}

	public class CustomTerrain : ITemplatedTerrainInfo, ITerrainInfoNotifyMapCreated
	{
		public readonly string Name;
		public readonly string Id;
		public readonly int SheetSize = 512;
		public readonly Color[] HeightDebugColors = { Color.Red };
		public readonly string[] EditorTemplateOrder;
		public readonly bool IgnoreTileSpriteOffsets;

		[FieldLoader.Ignore]
		public readonly IReadOnlyDictionary<ushort, TerrainTemplateInfo> Templates;

		[FieldLoader.Ignore]
		public readonly TerrainTypeInfo[] TerrainInfo;
		readonly Dictionary<string, byte> terrainIndexByType = new Dictionary<string, byte>();
		readonly byte defaultWalkableTerrainIndex;

		public CustomTerrain(IReadOnlyFileSystem fileSystem, string filepath)
		{
			var yaml = MiniYaml.FromStream(fileSystem.Open(filepath), filepath)
				.ToDictionary(x => x.Key, x => x.Value);

			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			TerrainInfo = yaml["Terrain"].ToDictionary().Values
				.Select(y => new TerrainTypeInfo(y))
				.OrderBy(tt => tt.Type)
				.ToArray();

			if (TerrainInfo.Length >= byte.MaxValue)
				throw new YamlException("Too many terrain types.");

			for (byte i = 0; i < TerrainInfo.Length; i++)
			{
				var tt = TerrainInfo[i].Type;

				if (terrainIndexByType.ContainsKey(tt))
					throw new YamlException("Duplicate terrain type '{0}' in '{1}'.".F(tt, filepath));

				terrainIndexByType.Add(tt, i);
			}

			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");

			// Templates
			Templates = yaml["Templates"].ToDictionary().Values
				.Select(y => (TerrainTemplateInfo)new CustomTerrainTemplateInfo(this, y)).ToDictionary(t => t.Id);
		}

		public TerrainTypeInfo this[byte index]
		{
			get { return TerrainInfo[index]; }
		}

		public byte GetTerrainIndex(string type)
		{
			if (terrainIndexByType.TryGetValue(type, out var index))
				return index;

			throw new InvalidDataException("Tileset '{0}' lacks terrain type '{1}'".F(Id, type));
		}

		public byte GetTerrainIndex(TerrainTile r)
		{
			if (!Templates.TryGetValue(r.Type, out var tpl))
				return defaultWalkableTerrainIndex;

			if (tpl.Contains(r.Index))
			{
				var tile = tpl[r.Index];
				if (tile != null && tile.TerrainType != byte.MaxValue)
					return tile.TerrainType;
			}

			return defaultWalkableTerrainIndex;
		}

		public TerrainTileInfo GetTileInfo(TerrainTile r)
		{
			if (!Templates.TryGetValue(r.Type, out var tpl))
				return null;

			return tpl.Contains(r.Index) ? tpl[r.Index] : null;
		}

		public bool TryGetTileInfo(TerrainTile r, out TerrainTileInfo info)
		{
			if (!Templates.TryGetValue(r.Type, out var tpl) || !tpl.Contains(r.Index))
			{
				info = null;
				return false;
			}

			info = tpl[r.Index];
			return info != null;
		}

		string ITerrainInfo.Id { get { return Id; } }
		TerrainTypeInfo[] ITerrainInfo.TerrainTypes { get { return TerrainInfo; } }
		TerrainTileInfo ITerrainInfo.GetTerrainInfo(TerrainTile r) { return GetTileInfo(r); }
		bool ITerrainInfo.TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info) { return TryGetTileInfo(r, out info); }
		Color[] ITerrainInfo.HeightDebugColors { get { return HeightDebugColors; } }
		IEnumerable<Color> ITerrainInfo.RestrictedPlayerColors { get { return TerrainInfo.Where(ti => ti.RestrictPlayerColor).Select(ti => ti.Color); } }
		float ITerrainInfo.MinHeightColorBrightness { get { return 1.0f; } }
		float ITerrainInfo.MaxHeightColorBrightness { get { return 1.0f; } }
		TerrainTile ITerrainInfo.DefaultTerrainTile { get { return new TerrainTile(Templates.First().Key, 0); } }

		string[] ITemplatedTerrainInfo.EditorTemplateOrder { get { return EditorTemplateOrder; } }
		IReadOnlyDictionary<ushort, TerrainTemplateInfo> ITemplatedTerrainInfo.Templates { get { return Templates; } }

		void ITerrainInfoNotifyMapCreated.MapCreated(Map map)
		{
			// Randomize PickAny variants across tiles
			var r = new MersenneTwister();
			for (var j = map.Bounds.Top; j < map.Bounds.Bottom; j += 2)
			{
				for (var i = map.Bounds.Left; i < map.Bounds.Right; i += 2)
				{
					var tile = map.Tiles[new MPos(i, j)];
					if (!Templates.TryGetValue(tile.Type, out var template) || !template.PickAny)
						continue;

					var terrainInfo = map.Rules.TerrainInfo;
					var terrainIndex = terrainInfo.GetTerrainIndex(tile);
					var similarTiles = Templates.Where(t => t.Value.PickAny
						&& terrainInfo.GetTerrainIndex(new TerrainTile(t.Key, 0x00)) == terrainIndex);

					var randomTile = similarTiles.Random(r);
					for (var f = 0; f < 4; f++)
					{
						var cell = new MPos(i + (f % 2), j + (f / 2));
						map.Tiles[cell] = new TerrainTile(randomTile.Key, (byte)f);
					}
				}
			}
		}
	}
}
