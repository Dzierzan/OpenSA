using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.UtilityCommands
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
				var actorType = actorInfo.ActorType.ToLowerInvariant();
				var actor = new ActorReference(actorType)
				{
					new LocationInit(new CPos(x + MapCordonWidth, y + MapCordonWidth)),
				};

				if (actorInfo.Color == Color.White)
					actor.Add(new OwnerInit("Neutral"));
				if (actorInfo.Color == Color.Green)
					actor.Add(new OwnerInit("Ants"));
				if (actorInfo.Color == Color.Blue)
					actor.Add(new OwnerInit("Beetles"));
				if (actorInfo.Color == Color.Red)
					actor.Add(new OwnerInit("Spiders"));
				if (actorInfo.Color == Color.Yellow)
					actor.Add(new OwnerInit("Wasps"));
				if (actorInfo.Color == Color.Black)
					actor.Add(new OwnerInit("Creeps"));

				AddPlayer(actorInfo.Color);

				var actorCount = map.ActorDefinitions.Count;

				if (!map.Rules.Actors.ContainsKey(actorType))
					Console.WriteLine("Ignoring unknown actor type: `{0}`".F(actorType));
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, actor.Save()));
			}
		}

		public static Dictionary<int, (string ActorType, Color Color)> ActorMap = new Dictionary<int, (string, Color)>
		{
			{ 7, ("ants_light", Color.Red) },
			{ 13, ("ants_light", Color.Yellow) },
			{ 15, ("ants_light", Color.Blue) },
			{ 8, ("ants_light", Color.Green) },
			{ 11, ("ants_medium", Color.Red) },
			{ 16, ("ants_medium", Color.Yellow) },
			{ 18, ("ants_medium", Color.Blue) },
			{ 17, ("ants_medium", Color.Green) },
			{ 70, ("ants_heavy", Color.Red) },
			{ 81, ("ants_heavy", Color.Yellow) },
			{ 79, ("ants_heavy", Color.Blue) },
			{ 80, ("ants_heavy", Color.Green) },
			{ 25, ("beetles_light", Color.Red) },
			{ 38, ("beetles_light", Color.Yellow) },
			{ 40, ("beetles_light", Color.Blue) },
			{ 39, ("beetles_light", Color.Green) },
			{ 29, ("beetles_medium", Color.Red) },
			{ 72, ("beetles_medium", Color.Yellow) },
			{ 73, ("beetles_medium", Color.Blue) },
			{ 74, ("beetles_medium", Color.Green) },
			{ 42, ("beetles_heavy", Color.Red) },
			{ 41, ("beetles_heavy", Color.Yellow) },
			{ 43, ("beetles_heavy", Color.Blue) },
			{ 19, ("beetles_heavy", Color.Green) },
			{ 27, ("spiders_light", Color.Red) },
			{ 91, ("spiders_light", Color.Yellow) },
			{ 89, ("spiders_light", Color.Blue) },
			{ 90, ("spiders_light", Color.Green) },
			{ 28, ("spiders_medium", Color.Red) },
			{ 86, ("spiders_medium", Color.Yellow) },
			{ 88, ("spiders_medium", Color.Blue) },
			{ 87, ("spiders_medium", Color.Green) },
			{ 68, ("spiders_heavy", Color.Red) },
			{ 118, ("spiders_heavy", Color.Yellow) },
			{ 120, ("spiders_heavy", Color.Blue) },
			{ 119, ("spiders_heavy", Color.Green) },
			{ 109, ("wasps_light", Color.Red) },
			{ 21, ("wasps_light", Color.Yellow) },
			{ 111, ("wasps_light", Color.Blue) },
			{ 110, ("wasps_light", Color.Green) },
			{ 112, ("wasps_medium", Color.Red) },
			{ 99, ("wasps_medium", Color.Yellow) },
			{ 114, ("wasps_medium", Color.Blue) },
			{ 113, ("wasps_medium", Color.Green) },
			{ 115, ("wasps_heavy", Color.Red) },
			{ 97, ("wasps_heavy", Color.Yellow) },
			{ 117, ("wasps_heavy", Color.Blue) },
			{ 116, ("wasps_heavy", Color.Green) },
			{ 103, ("scorpions_light", Color.Red) },
			{ 141, ("scorpions_light", Color.Yellow) },
			{ 139, ("scorpions_light", Color.Blue) },
			{ 140, ("scorpions_light", Color.Green) },
			{ 125, ("scorpions_medium", Color.Red) },
			{ 138, ("scorpions_medium", Color.Yellow) },
			{ 136, ("scorpions_medium", Color.Blue) },
			{ 137, ("scorpions_medium", Color.Green) },
			{ 126, ("scorpions_heavy", Color.Red) },
			{ 135, ("scorpions_medium", Color.Yellow) },
			{ 133, ("scorpions_medium", Color.Blue) },
			{ 134, ("scorpions_medium", Color.Green) },
			{ 30, ("ants_colony", Color.Red) },
			{ 34, ("ants_colony", Color.Yellow) },
			{ 36, ("ants_colony", Color.Blue) },
			{ 35, ("ants_colony", Color.Green) },
			{ 33, ("ants_colony", Color.White) },
			{ 78, ("beetles_colony", Color.Red) },
			{ 77, ("beetles_colony", Color.Yellow) },
			{ 76, ("beetles_colony", Color.Blue) },
			{ 51, ("beetles_colony", Color.Green) },
			{ 75, ("beetles_colony", Color.White) },
			{ 65, ("spiders_colony", Color.Red) },
			{ 83, ("spiders_colony", Color.Yellow) },
			{ 85, ("spiders_colony", Color.Blue) },
			{ 84, ("spiders_colony", Color.Green) },
			{ 82, ("spiders_colony", Color.White) },
			{ 106, ("wasps_colony", Color.Red) },
			{ 54, ("wasps_colony", Color.Yellow) },
			{ 104, ("wasps_colony", Color.Blue) },
			{ 105, ("wasps_colony", Color.Green) },
			{ 107, ("wasps_colony", Color.White) },
			{ 121, ("scorpions_colony", Color.Red) },
			{ 132, ("scorpions_colony", Color.Yellow) },
			{ 129, ("scorpions_colony", Color.Blue) },
			{ 130, ("scorpions_colony", Color.Green) },
			{ 131, ("scorpions_colony", Color.White) },
			{ 145, ("ant_bull_pirate", Color.Black) },
			{ 146, ("ants_grenadier_pirate", Color.Black) },
			{ 3, ("plant_toad_stool", Color.White) },
			{ 5, ("plant_brown_mushroom", Color.White) },
			{ 6, ("plant_broad_leaf_grass", Color.White) },
			{ 9, ("plant_flower", Color.White) },
			{ 159, ("plant_desert_flower", Color.White) },
			{ 160, ("plant_gumnut", Color.White) },
			{ 163, ("plant_serata", Color.White) },
			{ 164, ("plant_desert_grass", Color.White) },
			{ 165, ("plant_cherry_flower", Color.White) },
			{ 166, ("plant_moon_flower", Color.White) },
			{ 167, ("plant_clover", Color.White) },
			{ 168, ("plant_moon_mushroom", Color.White) },
			{ 174, ("plant_jelly_bean", Color.White) },
			{ 175, ("plant_smarty", Color.White) },
			{ 177, ("plant_choc_chip", Color.White) },
			{ 178, ("plant_jaffa", Color.White) },
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
