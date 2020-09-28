using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.OpenSA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.SpriteLoaders
{
	public class DdfSpriteFrame : ISpriteFrame
	{
		public SpriteFrameType Type { get { return SpriteFrameType.Indexed; } }
		public Size Size { get; private set; }
		public Size FrameSize { get; private set; }
		public float2 Offset { get; private set; }
		public byte[] Data { get; private set; }

		public bool DisableExportPadding { get { return true; } }

		public DdfSpriteFrame(Stream s, float2 offset)
		{
			var fileOffset = s.ReadUInt32();
			var format = s.ReadUInt32();
			var fileSize = s.ReadInt32();
			var width = s.ReadInt32();
			var height = s.ReadInt32();

			Size = new Size(width, height);
			FrameSize = new Size(width, height);
			Offset = new float2(width / 2 + offset.X, height / 2 + offset.Y);

			if (format == 0)
			{
				s.Position = fileOffset;
				Data = s.ReadBytes(fileSize);
			}
			else
			{
				Data = new byte[width * height];

				for (var y = 0; y < height; y++)
				{
					s.Position = fileOffset + y * 4;
					var rowOffset = s.ReadUInt32();
					s.Position = fileOffset + height * 4 + rowOffset;
					var x = 0;

					while (true)
					{
						var length = s.ReadInt16();

						if (length == 0)
							break;

						if (length < 0)
							for (var i = 0; i < -length; i++)
								Data[y * width + x++] = 0x00;
						else
							for (var i = 0; i < length; i++)
								Data[y * width + x++] = s.ReadUInt8();
					}
				}
			}
		}
	}

	public class QuarterDdfTile : ISpriteFrame
	{
		public SpriteFrameType Type { get { return SpriteFrameType.Indexed; } }
		public Size Size { get; private set; }
		public Size FrameSize { get; private set; }
		public float2 Offset { get; private set; }
		public byte[] Data { get; private set; }

		public bool DisableExportPadding { get { return true; } }

		public QuarterDdfTile(DdfSpriteFrame fullTile, int tileX, int tileY)
		{
			Size = new Size(fullTile.Size.Width / 2, fullTile.Size.Height / 2);
			FrameSize = new Size(fullTile.Size.Width / 2, fullTile.Size.Height / 2);
			Offset = new float2(fullTile.Offset.X / 2, fullTile.Offset.Y / 2);

			Data = new byte[Size.Width * Size.Height];

			for (var y = 0; y < Size.Height; y++)
				for (var x = 0; x < Size.Width; x++)
					Data[y * Size.Width + x] = fullTile.Data[(y + tileY * Size.Height) * fullTile.Size.Width + x + tileX * Size.Width];
		}
	}

	public class DdfLoader : ISpriteLoader
	{
		protected virtual bool IsDdf(Stream s)
		{
			return s is DdfPackageLoader.DdfSegmentStream;
		}

		public virtual bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;

			if (!IsDdf(s))
			{
				frames = null;
				return false;
			}

			var ddfStream = s as DdfPackageLoader.DdfSegmentStream;
			ddfStream.Position = ddfStream.DdfPosition;

			if (ddfStream.IsTile)
			{
				var fullTile = new DdfSpriteFrame(ddfStream, new float2(0, 0));
				frames = new ISpriteFrame[]
				{
					new QuarterDdfTile(fullTile, 0, 0),
					new QuarterDdfTile(fullTile, 1, 0),
					new QuarterDdfTile(fullTile, 0, 1),
					new QuarterDdfTile(fullTile, 1, 1)
				};
			}
			else
				frames = new ISpriteFrame[]
				{
					new DdfSpriteFrame(ddfStream, new float2(0, 0))
				};

			return true;
		}
	}

	public class AniLoader : DdfLoader
	{
		protected override bool IsDdf(Stream s)
		{
			return s is DdfPackageLoader.AniSegmentStream;
		}

		public override bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;

			if (!IsDdf(s))
			{
				frames = null;
				return false;
			}

			var aniStream = s as DdfPackageLoader.AniSegmentStream;
			aniStream.Position = aniStream.AniPosition;
			frames = new ISpriteFrame[aniStream.ReadUInt32()];

			for (var i = 0; i < frames.Length; i++)
			{
				var metaName = aniStream.ReadASCII(32).Replace("\0", string.Empty);

				aniStream.Position += 4 * 2;
				var numScripts = aniStream.ReadUInt32();
				aniStream.Position += 32 * numScripts;
				var returnPosition = aniStream.Position;

				long metaPosition;
				if (aniStream.MetaIndex.TryGetValue(metaName, out metaPosition))
				{
					var metaStream = new SegmentStream(aniStream, 0, aniStream.Length);
					metaStream.Position = metaPosition;

					var ddfName = metaStream.ReadASCII(32).Replace("\0", string.Empty);
					metaStream.Position += 4 * 3;
					var offset = new float2(metaStream.ReadInt32(), metaStream.ReadInt32());

					long ddfPosition;
					if (aniStream.DdfIndex.TryGetValue(ddfName, out ddfPosition))
					{
						var ddfStream = aniStream.DdfStream;
						ddfStream.Position = ddfPosition;
						frames[i] = new DdfSpriteFrame(ddfStream, offset);
					}
				}

				aniStream.Position = returnPosition;
			}

			return true;
		}
	}
}
