using System;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Mods.SA.UtilityCommands
{
	public class MapImporter
	{
		const int MapCordonWidth = 2;

		readonly FileStream stream;
		readonly string tilesetName;
		readonly TerrainTile clearTile;

		Map map;
		Size mapSize;
		TileSet tileSet;

		MapImporter(string filename, string tileset)
		{
			tilesetName = tileset;

			try
			{
				clearTile = new TerrainTile(0, 0);
				stream = File.OpenRead(filename);

				if (stream.Length == 0)
					throw new InvalidDataException("File is empty.");

				var magic = stream.ReadASCII(8);
				stream.Seek(132, SeekOrigin.Begin);
				var type = stream.ReadASCII(9);

				if (magic != "Game.DDF" || type != "Landscape")
					throw new ArgumentException("The map is in an unrecognized format!", "filename");

				Initialize(filename);
				FillMap();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				map = null;
			}
			finally
			{
				stream.Close();
			}
		}

		public static Map Import(string filename, string mod, string tileset)
		{
			var importer = new MapImporter(filename, tileset);
			var map = importer.map;
			if (map == null)
				return null;

			map.RequiresMod = mod;

			return map;
		}

		void Initialize(string mapFile)
		{
			stream.Seek(400, SeekOrigin.Begin);
			var x = stream.ReadUInt16();
			stream.Seek(404, SeekOrigin.Begin);
			var y = stream.ReadUInt16();
			mapSize = new Size(x * 2, y * 2);

			tileSet = Game.ModData.DefaultTileSets[tilesetName];

			map = new Map(Game.ModData, tileSet, mapSize.Width + 2 * MapCordonWidth, mapSize.Height + 2 * MapCordonWidth)
			{
				Title = Path.GetFileNameWithoutExtension(mapFile),
				Author = "Mountain King Studios"
			};

			var tl = new PPos(MapCordonWidth, MapCordonWidth);
			var br = new PPos(MapCordonWidth + mapSize.Width - 1, MapCordonWidth + mapSize.Height - 1);
			map.SetBounds(tl, br);

			var players = new MapPlayers(map.Rules, 0); // TODO
			map.PlayerDefinitions = players.ToMiniYaml();
		}

		void FillMap()
		{
			stream.Seek(428, SeekOrigin.Begin);

			for (var y = MapCordonWidth; y < mapSize.Height + MapCordonWidth; y += 2)
			{
				for (var x = MapCordonWidth; x < mapSize.Width + MapCordonWidth; x += 2)
				{
					var rawTile = stream.ReadUInt8();

					byte offset = 0x0;
					switch (tilesetName)
					{
						case "NORMAL":
							offset = 47;
							break;
						case "SWAMP":
							offset = 201;
							if ((byte)(rawTile - offset) > 36)
								offset++;
							break;
						case "DESERT":
							offset = 100;
							if ((byte)(rawTile - offset) < 37)
								offset += 2;
							break;
						case "CANDY":
							if ((byte)(rawTile - offset) > 32)
								offset++;
							break;
					}

					var tileInfo = (byte)(rawTile - offset);

					var unknown = stream.ReadUInt8();

					for (int f = 0; f < 4; f++)
					{
						var tile = GetTile(tileInfo, (byte)f);
						var cell = new CPos(x + (f % 2), y + (f / 2));
						map.Tiles[cell] = tile;
					}
				}
			}
		}

		TerrainTile GetTile(ushort tileIndex, byte frame)
		{
			TerrainTemplateInfo template;
			if (!tileSet.Templates.TryGetValue(tileIndex, out template))
			{
				Console.WriteLine("Tile with Id {0} could not be found. Defaulting to clear.".F(tileIndex));
				return clearTile;
			}

			return new TerrainTile(template.Id, frame);
		}
	}
}
