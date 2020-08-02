using System;
using System.Collections.Generic;
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
		MapPlayers mapPlayers;

		int numberOfActors;

		MapImporter(string filename, string tileset)
		{
			tilesetName = tileset;
			clearTile = new TerrainTile(0, 0);

			try
			{
				stream = File.OpenRead(filename);

				if (stream.Length == 0)
					throw new InvalidDataException("File is empty.");

				var magic = stream.ReadASCII(82);
				var type = stream.ReadASCII(82);

				if (!(magic.Contains("GameA.DDF") || magic.Contains("Game.DDF")) || !type.Contains("Landscape"))
					throw new ArgumentException("The map is in an unrecognized format!", "filename");

				Initialize(filename);
				FillMap();
				LoadActors();

				map.PlayerDefinitions = mapPlayers.ToMiniYaml();
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
			var x = stream.ReadInt32();
			var y = stream.ReadInt32();
			mapSize = new Size(x * 2, y * 2);

			tileSet = Game.ModData.DefaultTileSets[tilesetName];

			map = new Map(Game.ModData, tileSet, mapSize.Width + 2 * MapCordonWidth, mapSize.Height + 2 * MapCordonWidth)
			{
				Title = Path.GetFileNameWithoutExtension(mapFile),
				Author = "Gate 5 Creations"
			};

			var tl = new PPos(MapCordonWidth, MapCordonWidth);
			var br = new PPos(MapCordonWidth + mapSize.Width - 1, MapCordonWidth + mapSize.Height - 1);
			map.SetBounds(tl, br);

			stream.Seek(424, SeekOrigin.Begin);
			numberOfActors = stream.ReadInt32();

			mapPlayers = new MapPlayers(map.Rules, 0);
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

		void LoadActors()
		{
			for (var a = 0; a < numberOfActors; a++)
			{
				var id = stream.ReadInt32();
				var type = stream.ReadInt32();
				var x = (int)Math.Round((float)stream.ReadInt32() / map.Grid.TileSize.Width);
				var y = (int)Math.Round((float)stream.ReadInt32() / map.Grid.TileSize.Height);

				var invalidLocation = false;
				if (x < 0 || x > mapSize.Width || y < 0 || y > mapSize.Height)
				{
					Console.WriteLine("Invalid coordinates {0},{1} for actor type {2}.".F(x, y, type));
					invalidLocation = true;
				}

				stream.Seek(4, SeekOrigin.Current);
				var numberOfProperties = stream.ReadInt32();
				stream.Seek(4, SeekOrigin.Current);
				if (numberOfProperties > 0)
				{
					for (var p = 0; p < numberOfProperties; p++)
					{
						var key = stream.ReadASCII(128);
						var value = stream.ReadInt32();
						Console.WriteLine(key + ": " + value);
					}
				}

				if (!ActorMap.ContainsKey(type))
				{
					Console.WriteLine("Ignoring unknown actor type: `{0}` @ {1},{2}".F(type, x, y));
					continue;
				}

				if (invalidLocation)
					continue;

				var actorInfo = ActorMap[type];
				var actorType = actorInfo.First.ToLowerInvariant();
				var actor = new ActorReference(actorType)
				{
					new LocationInit(new CPos(x + MapCordonWidth, y + MapCordonWidth)),
				};

				if (actorInfo.Second == Color.White)
					actor.Add(new OwnerInit("Neutral"));
				if (actorInfo.Second == Color.Green)
					actor.Add(new OwnerInit("Ants"));
				if (actorInfo.Second == Color.Blue)
					actor.Add(new OwnerInit("Beetles"));
				if (actorInfo.Second == Color.Red)
					actor.Add(new OwnerInit("Spiders"));
				if (actorInfo.Second == Color.Yellow)
					actor.Add(new OwnerInit("Wasps"));
				if (actorInfo.Second == Color.Black)
					actor.Add(new OwnerInit("Creeps"));

				AddPlayer(actorInfo.Second);

				var actorCount = map.ActorDefinitions.Count;

				if (!map.Rules.Actors.ContainsKey(actorType))
					Console.WriteLine("Ignoring unknown actor type: `{0}`".F(actorType));
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, actor.Save()));
			}
		}

		public static Dictionary<int, Pair<string, Color>> ActorMap = new Dictionary<int, Pair<string, Color>>
		{
			{ 7, Pair.New("ants_light", Color.Red) },
			{ 13, Pair.New("ants_light", Color.Yellow) },
			{ 15, Pair.New("ants_light", Color.Blue) },
			{ 8, Pair.New("ants_light", Color.Green) },
			{ 11, Pair.New("ants_medium", Color.Red) },
			{ 16, Pair.New("ants_medium", Color.Yellow) },
			{ 18, Pair.New("ants_medium", Color.Blue) },
			{ 17, Pair.New("ants_medium", Color.Green) },
			{ 70, Pair.New("ants_heavy", Color.Red) },
			{ 81, Pair.New("ants_heavy", Color.Yellow) },
			{ 79, Pair.New("ants_heavy", Color.Blue) },
			{ 80, Pair.New("ants_heavy", Color.Green) },
			{ 25, Pair.New("beetles_light", Color.Red) },
			{ 38, Pair.New("beetles_light", Color.Yellow) },
			{ 40, Pair.New("beetles_light", Color.Blue) },
			{ 39, Pair.New("beetles_light", Color.Green) },
			{ 29, Pair.New("beetles_medium", Color.Red) },
			{ 72, Pair.New("beetles_medium", Color.Yellow) },
			{ 73, Pair.New("beetles_medium", Color.Blue) },
			{ 74, Pair.New("beetles_medium", Color.Green) },
			{ 42, Pair.New("beetles_heavy", Color.Red) },
			{ 41, Pair.New("beetles_heavy", Color.Yellow) },
			{ 43, Pair.New("beetles_heavy", Color.Blue) },
			{ 19, Pair.New("beetles_heavy", Color.Green) },
			{ 27, Pair.New("spiders_light", Color.Red) },
			{ 91, Pair.New("spiders_light", Color.Yellow) },
			{ 89, Pair.New("spiders_light", Color.Blue) },
			{ 90, Pair.New("spiders_light", Color.Green) },
			{ 28, Pair.New("spiders_medium", Color.Red) },
			{ 86, Pair.New("spiders_medium", Color.Yellow) },
			{ 88, Pair.New("spiders_medium", Color.Blue) },
			{ 87, Pair.New("spiders_medium", Color.Green) },
			{ 68, Pair.New("spiders_heavy", Color.Red) },
			{ 118, Pair.New("spiders_heavy", Color.Yellow) },
			{ 120, Pair.New("spiders_heavy", Color.Blue) },
			{ 119, Pair.New("spiders_heavy", Color.Green) },
			{ 109, Pair.New("wasps_light", Color.Red) },
			{ 21, Pair.New("wasps_light", Color.Yellow) },
			{ 111, Pair.New("wasps_light", Color.Blue) },
			{ 110, Pair.New("wasps_light", Color.Green) },
			{ 112, Pair.New("wasps_medium", Color.Red) },
			{ 99, Pair.New("wasps_medium", Color.Yellow) },
			{ 114, Pair.New("wasps_medium", Color.Blue) },
			{ 113, Pair.New("wasps_medium", Color.Green) },
			{ 115, Pair.New("wasps_heavy", Color.Red) },
			{ 97, Pair.New("wasps_heavy", Color.Yellow) },
			{ 117, Pair.New("wasps_heavy", Color.Blue) },
			{ 116, Pair.New("wasps_heavy", Color.Green) },
			{ 103, Pair.New("scorpions_light", Color.Red) },
			{ 141, Pair.New("scorpions_light", Color.Yellow) },
			{ 139, Pair.New("scorpions_light", Color.Blue) },
			{ 140, Pair.New("scorpions_light", Color.Green) },
			{ 125, Pair.New("scorpions_medium", Color.Red) },
			{ 138, Pair.New("scorpions_medium", Color.Yellow) },
			{ 136, Pair.New("scorpions_medium", Color.Blue) },
			{ 137, Pair.New("scorpions_medium", Color.Green) },
			{ 126, Pair.New("scorpions_heavy", Color.Red) },
			{ 135, Pair.New("scorpions_medium", Color.Yellow) },
			{ 133, Pair.New("scorpions_medium", Color.Blue) },
			{ 134, Pair.New("scorpions_medium", Color.Green) },
			{ 30, Pair.New("ants_colony", Color.Red) },
			{ 34, Pair.New("ants_colony", Color.Yellow) },
			{ 36, Pair.New("ants_colony", Color.Blue) },
			{ 35, Pair.New("ants_colony", Color.Green) },
			{ 33, Pair.New("ants_colony", Color.White) },
			{ 78, Pair.New("beetles_colony", Color.Red) },
			{ 77, Pair.New("beetles_colony", Color.Yellow) },
			{ 76, Pair.New("beetles_colony", Color.Blue) },
			{ 51, Pair.New("beetles_colony", Color.Green) },
			{ 75, Pair.New("beetles_colony", Color.White) },
			{ 65, Pair.New("spiders_colony", Color.Red) },
			{ 83, Pair.New("spiders_colony", Color.Yellow) },
			{ 85, Pair.New("spiders_colony", Color.Blue) },
			{ 84, Pair.New("spiders_colony", Color.Green) },
			{ 82, Pair.New("spiders_colony", Color.White) },
			{ 106, Pair.New("wasps_colony", Color.Red) },
			{ 54, Pair.New("wasps_colony", Color.Yellow) },
			{ 104, Pair.New("wasps_colony", Color.Blue) },
			{ 105, Pair.New("wasps_colony", Color.Green) },
			{ 107, Pair.New("wasps_colony", Color.White) },
			{ 121, Pair.New("scorpions_colony", Color.Red) },
			{ 132, Pair.New("scorpions_colony", Color.Yellow) },
			{ 129, Pair.New("scorpions_colony", Color.Blue) },
			{ 130, Pair.New("scorpions_colony", Color.Green) },
			{ 131, Pair.New("scorpions_colony", Color.White) },
			{ 145, Pair.New("ant_bull_pirate", Color.Black) },
			{ 146, Pair.New("ants_grenadier_pirate", Color.Black) },
			{ 3, Pair.New("plant_toad_stool", Color.White) },
			{ 5, Pair.New("plant_brown_mushroom", Color.White) },
			{ 6, Pair.New("plant_broad_leaf_grass", Color.White) },
			{ 9, Pair.New("plant_flower", Color.White) },
			{ 159, Pair.New("plant_desert_flower", Color.White) },
			{ 160, Pair.New("plant_gumnut", Color.White) },
			{ 163, Pair.New("plant_serata", Color.White) },
			{ 164, Pair.New("plant_desert_grass", Color.White) },
			{ 165, Pair.New("plant_cherry_flower", Color.White) },
			{ 166, Pair.New("plant_moon_flower", Color.White) },
			{ 167, Pair.New("plant_clover", Color.White) },
			{ 168, Pair.New("plant_moon_mushroom", Color.White) },
			{ 174, Pair.New("plant_jelly_bean", Color.White) },
			{ 175, Pair.New("plant_smarty", Color.White) },
			{ 177, Pair.New("plant_choc_chip", Color.White) },
			{ 178, Pair.New("plant_jaffa", Color.White) },
		};

		void AddPlayer(Color color)
		{
			if (color == Color.White || color == Color.Black)
				return;

			var faction = "Random";
			if (color == Color.Green)
				faction = "ants";
			if (color == Color.Red)
				faction = "spiders";
			if (color == Color.Blue)
				faction = "beetles";
			if (color == Color.Yellow)
				faction = "wasps";

			if (color == Color.Green)
				color = Color.FromArgb(87, 172, 73);
			if (color == Color.Red)
				color = Color.FromArgb(202, 49, 49);
			if (color == Color.Blue)
				color = Color.FromArgb(96, 110, 165);
			if (color == Color.Yellow)
				color = Color.FromArgb(245, 211, 120);

			var playerReference = new PlayerReference
			{
				Name = faction.Substring(0, 1).ToUpper() + faction.Substring(1),
				Faction = faction,
				Color = color
			};

			if (!mapPlayers.Players.ContainsKey(playerReference.Name))
				mapPlayers.Players.Add(playerReference.Name, playerReference);
		}
	}
}
