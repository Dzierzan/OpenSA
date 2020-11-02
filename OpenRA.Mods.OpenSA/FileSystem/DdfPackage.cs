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
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.FileSystem
{
	public class DdfPackageLoader : IPackageLoader
	{
		public class UndisposingSegmentStream : SegmentStream
		{
			public UndisposingSegmentStream(Stream stream, long offset, long count)
				: base(stream, offset, count)
			{
			}

			protected override void Dispose(bool disposing)
			{
			}
		}

		public sealed class DdfSegmentStream : UndisposingSegmentStream
		{
			public long DdfPosition { get; private set; }
			public bool IsTile { get; private set; }

			public DdfSegmentStream(Stream ddfStream, long ddfOffset, bool isTile)
				: base(ddfStream, 0, ddfStream.Length)
			{
				DdfPosition = ddfOffset;
				IsTile = isTile;
			}
		}

		public sealed class AniSegmentStream : UndisposingSegmentStream
		{
			public long AniPosition { get; private set; }
			public Stream DdfStream { get; private set; }
			public Dictionary<string, long> DdfIndex { get; private set; }
			public Dictionary<string, long> MetaIndex { get; private set; }

			public AniSegmentStream(Stream aniStream, long aniOffset, Stream ddfStream, Dictionary<string, long> ddfIndex, Dictionary<string, long> metaIndex)
				: base(aniStream, 0, aniStream.Length)
			{
				AniPosition = aniOffset;
				DdfStream = ddfStream;
				DdfIndex = ddfIndex;
				MetaIndex = metaIndex;
			}
		}

		public sealed class DdfPackage : IReadOnlyPackage
		{
			public string Name { get; private set; }
			public IEnumerable<string> Contents { get { return filesList; } }

			readonly Dictionary<string, long> ddfIndex = new Dictionary<string, long>();
			readonly Dictionary<string, long> aniIndex = new Dictionary<string, long>();
			readonly Dictionary<string, long> metaIndex = new Dictionary<string, long>();
			readonly List<string> filesList = new List<string>();
			readonly Stream ddfStream;
			readonly Stream aniStream;
			readonly long palettePosition;

			public DdfPackage(Stream ddfBase, Stream aniBase, string filename)
			{
				ddfStream = ddfBase;
				aniStream = aniBase;
				Name = filename;

				var numFiles = ddfStream.ReadUInt32();
				ddfStream.ReadUInt16(); // 768
				ddfStream.ReadUInt16(); // 256
				palettePosition = ddfStream.Position;
				filesList.Add("OpenSA.PAL");
				ddfStream.Position += 256 * 4; // Palette

				for (var i = 0; i < numFiles; i++)
				{
					var fileName = ddfStream.ReadASCII(32).Replace("\0", string.Empty);

					if (!ddfIndex.ContainsKey(fileName))
					{
						ddfIndex.Add(fileName, ddfStream.Position);
						filesList.Add(fileName + ".DDF");
					}

					ddfStream.Position += 20;
				}

				var numImages = aniStream.ReadUInt32();

				for (var i = 0; i < numImages; i++)
				{
					var fileName = aniStream.ReadASCII(32).Replace("\0", string.Empty);

					if (!metaIndex.ContainsKey(fileName))
						metaIndex.Add(fileName, aniStream.Position);

					aniStream.Position += 32 + 4 * 5;
					var numUnkB = aniStream.ReadUInt32();
					var numPoints = aniStream.ReadUInt32();
					aniStream.Position += numUnkB * (32 + 32 + 4 * 4);
					aniStream.Position += numPoints * (32 + 32 + 4 * 2);
				}

				var numAnimations = aniStream.ReadUInt32();

				for (var i = 0; i < numAnimations; i++)
				{
					var fileName = aniStream.ReadASCII(32).Replace("\0", string.Empty);

					if (!aniIndex.ContainsKey(fileName))
					{
						aniIndex.Add(fileName, aniStream.Position);
						filesList.Add(fileName + ".ANI");
					}

					var numFrames = aniStream.ReadUInt32();

					for (var j = 0; j < numFrames; j++)
					{
						aniStream.Position += 32 + 4 * 2;
						var numScripts = aniStream.ReadUInt32();
						aniStream.Position += 32 * numScripts;
					}
				}
			}

			public Stream GetStream(string filename)
			{
				long e;

				if (filename.Equals("OpenSA.PAL"))
					return new UndisposingSegmentStream(ddfStream, palettePosition, 256 * 4);

				if (aniIndex.TryGetValue(filename.Replace(".ANI", ""), out e))
					return new AniSegmentStream(aniStream, e, ddfStream, ddfIndex, metaIndex);

				if (ddfIndex.TryGetValue(filename.Replace(".DDF", ""), out e))
					return new DdfSegmentStream(ddfStream, e, filename.StartsWith("TE"));

				return null;
			}

			public IReadOnlyPackage OpenPackage(string filename, OpenRA.FileSystem.FileSystem context)
			{
				return null;
			}

			public bool Contains(string filename)
			{
				return filesList.Contains(filename);
			}

			public void Dispose()
			{
				ddfStream.Dispose();
				aniStream.Dispose();
			}
		}

		public bool TryParsePackage(Stream s, string filename, OpenRA.FileSystem.FileSystem context, out IReadOnlyPackage package)
		{
			if (!filename.EndsWith("DDF"))
			{
				package = null;
				return false;
			}

			package = new DdfPackage(s, context.Open(filename.Replace(".DDF", ".ANI")), filename);
			return true;
		}
	}
}
