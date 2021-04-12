
using System;

public static class ByteTools
{
    public static bool IsIdentical(in byte[] first, in byte[] second)
    {
        if (first.Length != second.Length) return false;
        return IsIdentical(first, 0, second, 0, (uint)first.Length);
    }

    public static bool IsIdentical(in byte[] first, uint firstOffset, in byte[] second, uint secondOffset, uint bytesLength)
    {
        if (first.Length - firstOffset < bytesLength) throw new IndexOutOfRangeException();
        if (second.Length - secondOffset < bytesLength) throw new IndexOutOfRangeException();

        for (int i = 0; i < bytesLength; i++)
        {
            if (first[i + firstOffset] != second[i + secondOffset]) return false;
        }

        return true;
    }
}
