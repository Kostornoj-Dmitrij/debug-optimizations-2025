namespace JPEG.Images;

public class PixelFormat
{
	private readonly string Format;

	private PixelFormat(string format)
	{
		Format = format;
	}

	public static PixelFormat RGB = new(nameof(RGB));
	public static PixelFormat YCbCr = new(nameof(YCbCr));

	protected bool Equals(PixelFormat other)
	{
		return string.Equals(Format, other.Format);
	}

	public override bool Equals(object obj)
	{
		if (obj is PixelFormat other)
			return Format == other.Format;
		return false;
	}

	public override int GetHashCode()
	{
		return Format?.GetHashCode() ?? 0;
	}

	public static bool operator ==(PixelFormat a, PixelFormat b)
	{
		if (a is null) return b is null;
		return a.Equals(b);
	}

	public static bool operator !=(PixelFormat a, PixelFormat b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		return Format;
	}
}