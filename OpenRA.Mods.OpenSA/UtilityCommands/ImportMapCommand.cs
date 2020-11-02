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

using System;
using System.IO;
using OpenRA.FileSystem;

namespace OpenRA.Mods.OpenSA.UtilityCommands
{
	class ImportMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--import-sa-map"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("FILENAME", "TILESET", "Convert a legacy Swarm Assault .lvl file to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var map = MapImporter.Import(args[1], utility.ModData.Manifest.Id, args[2]);
			if (map == null)
				return;

			var filename = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";
			map.Save(ZipFileLoader.Create(filename));
			Console.WriteLine(filename + " saved.");
		}
	}
}
