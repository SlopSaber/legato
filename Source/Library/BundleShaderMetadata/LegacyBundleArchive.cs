#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System;
using System.IO;

namespace Legato.Rendering;

internal static class LegacyBundleArchive
{
    private const int CompressionMask = 0x3F;
    private const int CombinedDirectory = 0x40;
    private const int DirectoryAtEnd = 0x80;
    private const int AlignData = 0x200;
    private const int BlockEntrySize = 10;
    private const int BlockCountOffset = 16;

    internal static byte[]? Convert(string path)
    {
        byte[] source = File.ReadAllBytes(path);
        BundleBinaryReader header = new(source, true);
        if (source.Length < 12 || header.CString() != "UnityFS" || header.Int32() != 7)
        {
            return null;
        }

        header.CString();
        if (!header.CString().StartsWith("2019.4.", StringComparison.Ordinal))
        {
            return null;
        }

        int sizeOffset = header.Position;
        long size = header.Int64();
        int compressedDirectorySize = header.Int32();
        int directorySize = header.Int32();
        int archiveFlags = header.Int32();
        int headerEnd = header.Position;
        const int knownFlags = CompressionMask | CombinedDirectory | DirectoryAtEnd | AlignData;
        if ((archiveFlags & CombinedDirectory) == 0 || (archiveFlags & ~knownFlags) != 0)
        {
            throw new InvalidDataException("Unsupported UnityFS archive flags.");
        }

        header.Align(16);
        if (size != source.Length || compressedDirectorySize <= 0 ||
            compressedDirectorySize > source.Length - header.Position || directorySize < 24)
        {
            throw new InvalidDataException("Invalid UnityFS archive directory size.");
        }

        bool directoryAtEnd = (archiveFlags & DirectoryAtEnd) != 0;
        int directoryStart = directoryAtEnd ? source.Length - compressedDirectorySize : header.Position;
        int dataEnd = directoryAtEnd ? directoryStart : source.Length;
        byte[] directory = UnityBundleCompression.Decompress(
            source, directoryStart, compressedDirectorySize, directorySize, archiveFlags & CompressionMask, directorySize);

        if (!directoryAtEnd)
        {
            header.Position = directoryStart + compressedDirectorySize;
        }

        if ((archiveFlags & AlignData) != 0)
        {
            header.Align(16);
        }

        int dataStart = header.Position;
        BundleBinaryReader table = new(directory, true) { Position = BlockCountOffset };
        int blockCount = table.Int32();
        if (blockCount <= 0 || blockCount > (directory.Length - table.Position - sizeof(int)) / BlockEntrySize)
        {
            throw new InvalidDataException("Invalid UnityFS archive block count.");
        }

        int outputDataStart = checked(headerEnd + directory.Length);
        long expandedSize = outputDataStart;
        long packedEnd = dataStart;
        for (int i = 0; i < blockCount; i++)
        {
            int unpacked = table.Int32();
            int packed = table.Int32();
            table.Int16();
            if (unpacked <= 0 || packed <= 0)
            {
                throw new InvalidDataException("Invalid UnityFS archive block size.");
            }

            expandedSize += unpacked;
            packedEnd += packed;
        }

        if (packedEnd != dataEnd)
        {
            throw new InvalidDataException("UnityFS archive blocks do not match the file size.");
        }

        // Unity 2019.3 reads a v6 header with a front directory and no alignment padding.
        // Uncompressed blocks preserve the asset bytes and the CRC stored by the map.
        byte[] output = new byte[checked((int)expandedSize)];
        Buffer.BlockCopy(source, 0, output, 0, sizeOffset);
        BundleBinaryReader outputHeader = new(output, true) { Position = 8 };
        outputHeader.WriteInt32(6);
        outputHeader.Position = sizeOffset;
        outputHeader.WriteInt64(output.Length);
        outputHeader.WriteInt32(directory.Length);
        outputHeader.WriteInt32(directory.Length);
        outputHeader.WriteInt32(CombinedDirectory);

        table.Position = BlockCountOffset + sizeof(int);
        for (int i = 0; i < blockCount; i++)
        {
            int unpacked = table.Int32();
            int packed = table.Int32();
            int blockFlags = (ushort)table.Int16();
            byte[] block = UnityBundleCompression.Decompress(source, dataStart, packed, unpacked, blockFlags & CompressionMask, unpacked);
            Buffer.BlockCopy(block, 0, output, outputDataStart, unpacked);
            dataStart += packed;
            outputDataStart += unpacked;
            table.Position -= sizeof(int) + sizeof(short);
            table.WriteInt32(unpacked);
            table.WriteInt16(blockFlags & ~CompressionMask);
        }

        Buffer.BlockCopy(directory, 0, output, headerEnd, directory.Length);
        return output;
    }
}
#endif
