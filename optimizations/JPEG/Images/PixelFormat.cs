namespace JPEG.Images;

public class PixelFormat
{
	private readonly string _format;

	private PixelFormat(string format)
	{
		_format = format;
	}

	public static PixelFormat RGB = new(nameof(RGB));
	public static PixelFormat YCbCr = new(nameof(YCbCr));

	protected bool Equals(PixelFormat other)
	{
		return string.Equals(_format, other._format);
	}

	public override bool Equals(object obj)
	{
		if (obj is PixelFormat other)
			return _format == other._format;
		return false;
	}

	public override int GetHashCode()
	{
		return _format?.GetHashCode() ?? 0;
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
		return _format;
	}
}