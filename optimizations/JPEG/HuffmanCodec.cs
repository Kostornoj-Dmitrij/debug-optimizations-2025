using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JPEG;

class HuffmanNode
{
	public byte? LeafLabel { get; set; }
	public int Frequency { get; set; }
	public HuffmanNode Left { get; set; }
	public HuffmanNode Right { get; set; }
}

public record struct BitsWithLength
{
	public int Bits { get; set; }
	public int BitsCount { get; set; }
}

class BitsBuffer
{
	private byte[] _buffer;
	private int _bufferIndex;
	private BitsWithLength _unfinishedBits;

	public BitsBuffer(int capacity = 1024)
	{
		_buffer = ArrayPool<byte>.Shared.Rent(capacity);
	}

	public void Add(BitsWithLength bitsWithLength)
	{
		int bitsCount = bitsWithLength.BitsCount;
		int bits = bitsWithLength.Bits;

		while (bitsCount > 0)
		{
			int neededBits = 8 - _unfinishedBits.BitsCount;
			if (bitsCount >= neededBits)
			{
				if (_bufferIndex >= _buffer.Length)
					ExpandBuffer();

				_buffer[_bufferIndex++] = (byte)((_unfinishedBits.Bits << neededBits) | (bits >> (bitsCount - neededBits)));
				bitsCount -= neededBits;
				bits &= (1 << bitsCount) - 1;
				_unfinishedBits.Bits = 0;
				_unfinishedBits.BitsCount = 0;
			}
			else
			{
				_unfinishedBits.Bits = (_unfinishedBits.Bits << bitsCount) | bits;
				_unfinishedBits.BitsCount += bitsCount;
				bitsCount = 0;
			}
		}
	}

	private void ExpandBuffer()
	{
		byte[] newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
		Array.Copy(_buffer, newBuffer, _buffer.Length);
		ArrayPool<byte>.Shared.Return(_buffer);
		_buffer = newBuffer;
	}

	public byte[] ToArray(out long bitsCount)
	{
		bitsCount = _bufferIndex * 8L + _unfinishedBits.BitsCount;
		var result = new byte[(bitsCount + 7) / 8];
		Array.Copy(_buffer, result, _bufferIndex);
		if (_unfinishedBits.BitsCount > 0)
			result[_bufferIndex] = (byte)(_unfinishedBits.Bits << (8 - _unfinishedBits.BitsCount));
		ArrayPool<byte>.Shared.Return(_buffer);
		return result;
	}
}

class HuffmanCodec
{
	public static byte[] Encode(IEnumerable<byte> data, out Dictionary<BitsWithLength, byte> decodeTable,
		out long bitsCount)
	{
		var enumerable = data as byte[] ?? data.ToArray();
		var frequences = CalcFrequences(enumerable);

		var root = BuildHuffmanTree(frequences);

		var encodeTable = new BitsWithLength[byte.MaxValue + 1];
		FillEncodeTable(root, encodeTable);

		var bitsBuffer = new BitsBuffer();
		foreach (var b in enumerable)
			bitsBuffer.Add(encodeTable[b]);

		decodeTable = CreateDecodeTable(encodeTable);

		return bitsBuffer.ToArray(out bitsCount);
	}

	public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, long bitsCount)
	{
		var result = new List<byte>();

		byte decodedByte;
		var sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
		for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
		{
			var b = encodedData[byteNum];
			for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < bitsCount; bitNum++)
			{
				sample.Bits = (sample.Bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
				sample.BitsCount++;

				if (decodeTable.TryGetValue(sample, out decodedByte))
				{
					result.Add(decodedByte);

					sample.BitsCount = 0;
					sample.Bits = 0;
				}
			}
		}

		return result.ToArray();
	}

	private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
	{
		var result = new Dictionary<BitsWithLength, byte>();
		for (int b = 0; b < encodeTable.Length; b++)
		{
			var bitsWithLength = encodeTable[b];
			if (bitsWithLength == null)
				continue;

			result[bitsWithLength] = (byte)b;
		}

		return result;
	}

	private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable,
		int bitvector = 0, int depth = 0)
	{
		if (node.LeafLabel != null)
			encodeSubstitutionTable[node.LeafLabel.Value] =
				new BitsWithLength { Bits = bitvector, BitsCount = depth };
		else
		{
			if (node.Left != null)
			{
				FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
				FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
			}
		}
	}

	private static HuffmanNode BuildHuffmanTree(int[] frequences)
	{
		var nodes = new List<HuffmanNode>();

		for (int i = 0; i < 256; i++)
			if (frequences[i] > 0)
				nodes.Add(new HuffmanNode { Frequency = frequences[i], LeafLabel = (byte)i });

		while (nodes.Count > 1)
		{
			nodes.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));

			var firstMin = nodes[0];
			var secondMin = nodes[1];
			nodes.RemoveRange(0, 2);

			nodes.Add(new HuffmanNode { Frequency = firstMin.Frequency + secondMin.Frequency, Left = secondMin, Right = firstMin });
		}

		return nodes[0];
	}

	private static int[] CalcFrequences(IEnumerable<byte> data)
	{
		var result = new int[256];
		foreach (var b in data)
			result[b]++;
		return result;
	}
}