#region Copyright & License Information
/*
 * Copyright 2019-2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using OpenRA.Mods.OpenSA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.AudioLoaders
{
	public class SdfLoader : ISoundLoader
	{
		bool IsSdf(Stream s)
		{
			return s is SdfPackageLoader.SdfSegmentStream;
		}

		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			if (IsSdf(stream))
			{
				sound = new SdfFormat((SdfPackageLoader.SdfSegmentStream)stream);
				return true;
			}

			sound = null;
			return false;
		}
	}

	public sealed class SdfFormat : ISoundFormat
	{
		public int Channels { get { return channels; } }
		public int SampleBits { get { return sampleBits; } }
		public int SampleRate { get { return sampleRate; } }
		public float LengthInSeconds { get { return pcmStream.Length / (Channels * SampleRate * SampleBits); } }
		public Stream GetPCMInputStream() { return pcmStream; }
		public void Dispose() { sourceStream.Dispose(); }

		readonly Stream sourceStream;
		readonly SegmentStream pcmStream;
		readonly int channels;
		readonly int sampleBits;
		readonly int sampleRate;

		public SdfFormat(SdfPackageLoader.SdfSegmentStream stream)
		{
			sourceStream = stream;

			stream.Position = stream.SdfPosition;

			var fileOffset = stream.ReadUInt32();
			var fileSize = stream.ReadUInt32();
			stream.ReadUInt16(); // 01
			channels = stream.ReadInt16();
			sampleRate = stream.ReadInt32();
			stream.ReadUInt32(); // sampleRate * sampleSizeInBytes
			stream.ReadUInt16(); // sampleSizeInBytes
			sampleBits = stream.ReadUInt16();
			stream.ReadUInt32(); // 00

			pcmStream = new SegmentStream(stream, fileOffset, fileSize);
		}
	}
}
