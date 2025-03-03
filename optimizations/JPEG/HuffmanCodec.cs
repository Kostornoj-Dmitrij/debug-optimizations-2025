using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JPEG.Utilities;

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

	/*public class Comparer : IEqualityComparer<BitsWithLength>
	{
		public bool Equals(BitsWithLength x, BitsWithLength y)
		{
			if (x == y) return true;
			if (x == null || y == null)
				return false;
			return x.BitsCount == y.BitsCount && x.Bits == y.Bits;
		}

		public int GetHashCode(BitsWithLength obj)
		{
			if (obj == null)
				return 0;
			return ((397 * obj.Bits) << 5) ^ (17 * obj.BitsCount);
		}
	}
	*/
}

class BitsBuffer
{
	private byte[] buffer;
	private int bufferIndex;
	private BitsWithLength unfinishedBits = new BitsWithLength();

	public BitsBuffer(int capacity = 1024)
	{
		buffer = ArrayPool<byte>.Shared.Rent(capacity);
	}

	public void Add(BitsWithLength bitsWithLength)
	{
		int bitsCount = bitsWithLength.BitsCount;
		int bits = bitsWithLength.Bits;

		while (bitsCount > 0)
		{
			int neededBits = 8 - unfinishedBits.BitsCount;
			if (bitsCount >= neededBits)
			{
				if (bufferIndex >= buffer.Length)
					ExpandBuffer();

				buffer[bufferIndex++] = (byte)((unfinishedBits.Bits << neededBits) | (bits >> (bitsCount - neededBits)));
				bitsCount -= neededBits;
				bits &= (1 << bitsCount) - 1;
				unfinishedBits.Bits = 0;
				unfinishedBits.BitsCount = 0;
			}
			else
			{
				unfinishedBits.Bits = (unfinishedBits.Bits << bitsCount) | bits;
				unfinishedBits.BitsCount += bitsCount;
				bitsCount = 0;
			}
		}
	}

	private void ExpandBuffer()
	{
		byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
		Array.Copy(buffer, newBuffer, buffer.Length);
		ArrayPool<byte>.Shared.Return(buffer);
		buffer = newBuffer;
	}

	public byte[] ToArray(out long bitsCount)
	{
		bitsCount = bufferIndex * 8L + unfinishedBits.BitsCount;
		var result = new byte[(bitsCount + 7) / 8];
		Array.Copy(buffer, result, bufferIndex);
		if (unfinishedBits.BitsCount > 0)
			result[bufferIndex] = (byte)(unfinishedBits.Bits << (8 - unfinishedBits.BitsCount));
		ArrayPool<byte>.Shared.Return(buffer);
		return result;
	}
}

class HuffmanCodec
{
	public static byte[] Encode(IEnumerable<byte> data, out Dictionary<BitsWithLength, byte> decodeTable,
		out long bitsCount)
	{
		var frequences = CalcFrequences(data);

		var root = BuildHuffmanTree(frequences);

		var encodeTable = new BitsWithLength[byte.MaxValue + 1];
		FillEncodeTable(root, encodeTable);

		var bitsBuffer = new BitsBuffer();
		foreach (var b in data)
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

	private static PriorityQueue<HuffmanNode, int> GetNodes(int[] frequences)
	{
		var nodes = new PriorityQueue<HuffmanNode, int>();

		for (var i = 0; i < 256; i++)
			if (frequences[i] > 0)
				nodes.Enqueue(new HuffmanNode{Frequency = frequences[i], LeafLabel = (byte)i}, frequences[i]);

		return nodes;
	}

	private static int[] CalcFrequences(IEnumerable<byte> data)
	{
		var result = new int[256];
		foreach (var b in data)
			result[b]++;
		return result;
	}
}