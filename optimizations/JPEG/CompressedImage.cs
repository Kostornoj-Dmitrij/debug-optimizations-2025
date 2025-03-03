using System;
using System.Collections.Generic;
using System.IO;

namespace JPEG;

public class CompressedImage
{
	public int Width { get; set; }
	public int Height { get; set; }

	public int Quality { get; set; }

	public Dictionary<BitsWithLength, byte> DecodeTable { get; set; }

	public long BitsCount { get; set; }
	public byte[] CompressedBytes { get; set; }

	public void Save(string path)
	{
		using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
		using var bw = new BinaryWriter(new BufferedStream(fs, 4096));

		bw.Write(Width);
		bw.Write(Height);
		bw.Write(Quality);
		bw.Write(DecodeTable.Count);

		foreach (var kvp in DecodeTable)
		{
			bw.Write(kvp.Key.Bits);
			bw.Write(kvp.Key.BitsCount);
			bw.Write(kvp.Value);
		}

		bw.Write(BitsCount);
		bw.Write(CompressedBytes.Length);
		bw.Write(CompressedBytes);
	}

	public static CompressedImage Load(string path)
	{
		var result = new CompressedImage();

		using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
		using var br = new BinaryReader(new BufferedStream(fs, 4096));

		result.Width = br.ReadInt32();
		result.Height = br.ReadInt32();
		result.Quality = br.ReadInt32();

		int decodeTableSize = br.ReadInt32();
		result.DecodeTable = new Dictionary<BitsWithLength, byte>(decodeTableSize);

		for (int i = 0; i < decodeTableSize; i++)
		{
			int bits = br.ReadInt32();
			int bitsCount = br.ReadInt32();
			byte mappedByte = br.ReadByte();

			result.DecodeTable[new BitsWithLength { Bits = bits, BitsCount = bitsCount }] = mappedByte;
		}

		result.BitsCount = br.ReadInt64();
		int compressedBytesCount = br.ReadInt32();
		result.CompressedBytes = br.ReadBytes(compressedBytesCount);

		return result;
	}
}