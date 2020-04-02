using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Swarm_Assault.FileSystem
{
	public class SdfPackageLoader : IPackageLoader
	{
		public sealed class SdfSegmentStream : SegmentStream
		{
			public long SdfPosition { get; private set; }

			public SdfSegmentStream(Stream stream, long offset)
				: base(stream, 0, stream.Length)
			{
				SdfPosition = offset;
			}

			protected override void Dispose(bool disposing)
			{
			}
		}

		public sealed class SdfPackage : IReadOnlyPackage
		{
			public string Name { get; private set; }
			public IEnumerable<string> Contents { get { return index.Keys; } }

			readonly Dictionary<string, long> index = new Dictionary<string, long>();
			readonly Stream s;

			public SdfPackage(Stream sBase, string filename)
			{
				s = sBase;
				Name = filename;

				var numFiles = sBase.ReadUInt32();

				for (var i = 0; i < numFiles; i++)
				{
					var fileName = sBase.ReadASCII(64).Replace("\0", string.Empty);
					sBase.Position += 28;

					while (index.ContainsKey(fileName + ".SDF"))
						fileName += "_Z";

					index.Add(fileName + ".SDF", sBase.Position - 28);
				}
			}

			public Stream GetStream(string filename)
			{
				long e;

				if (!index.TryGetValue(filename, out e))
					return null;

				return new SdfSegmentStream(s, e);
			}

			public IReadOnlyPackage OpenPackage(string filename, OpenRA.FileSystem.FileSystem context)
			{
				// Not implemented
				return null;
			}

			public bool Contains(string filename)
			{
				return index.ContainsKey(filename);
			}

			public void Dispose()
			{
				s.Dispose();
			}
		}

		public bool TryParsePackage(Stream s, string filename, OpenRA.FileSystem.FileSystem context,
			out IReadOnlyPackage package)
		{
			if (!filename.EndsWith("SDF"))
			{
				package = null;
				return false;
			}

			package = new SdfPackage(s, filename);
			return true;
		}
	}
}
