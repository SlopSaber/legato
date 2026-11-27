#if BEAT_SABER_1453 || LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System;
using System.Text;

namespace Legato.Rendering;

internal sealed class BundleBinaryReader
{
    private readonly bool _bigEndian;

    internal BundleBinaryReader(byte[] data, bool bigEndian)
    {
        Data = data;
        _bigEndian = bigEndian;
    }

    internal byte[] Data { get; }

    internal int Position { get; set; }

    internal byte Byte() => Data[Position++];

    internal short Int16() => (short)ReadInteger(2);

    internal int Int32() => (int)ReadInteger(4);

    internal uint UInt32() => (uint)ReadInteger(4);

    internal long Int64() => (long)ReadInteger(8);

    internal void WriteInt16(int value) => WriteInteger((ushort)value, 2);

    internal void WriteInt32(int value) => WriteInteger((uint)value, 4);

    internal void WriteInt64(long value) => WriteInteger((ulong)value, 8);

    internal void WriteAlignedString(string value)
    {
        WriteInt32(value.Length);
        Position += Encoding.ASCII.GetBytes(value, 0, value.Length, Data, Position);
        int end = (Position + 3) & ~3;
        Array.Clear(Data, Position, end - Position);
        Position = end;
    }

    internal void Align(int alignment) => Position = (Position + alignment - 1) / alignment * alignment;

    internal string CString()
    {
        int end = Array.IndexOf(Data, (byte)0, Position);
        string value = Encoding.UTF8.GetString(Data, Position, end - Position);
        Position = end + 1;
        return value;
    }

    private ulong ReadInteger(int size)
    {
        ulong value = 0;
        for (int i = 0; i < size; i++)
        {
            ulong part = Data[Position + i];
            value |= _bigEndian ? part << ((size - 1 - i) * 8) : part << (i * 8);
        }

        Position += size;
        return value;
    }

    private void WriteInteger(ulong value, int size)
    {
        for (int i = 0; i < size; i++)
        {
            Data[Position++] = (byte)(value >> ((_bigEndian ? size - 1 - i : i) * 8));
        }
    }
}
#endif
