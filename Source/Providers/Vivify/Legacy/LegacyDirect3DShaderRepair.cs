#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Legato.Rendering;
using LZ4ps;

namespace Vivify.Legacy;

internal static class LegacyDirect3DShaderRepair
{
    private const int Direct3D11 = 4;
    private const int ShaderProgramVersion = 201806140;
    private const int ProgramEntrySize = 3 * sizeof(int);
    private const int AddedHeaderBytes = 32;
    private const int NativeHeaderSize = 6;
    private const int ExtendedHeaderSize = NativeHeaderSize + AddedHeaderBytes;
    private const int DxbcSignature = 0x43425844;

    internal static byte[]? Rewrite(byte[] archive, out int programCount)
    {
        programCount = 0;
        AssetsManager manager = new();
        using MemoryStream input = new(archive, false);
        try
        {
            BundleFileInstance bundleFile = manager.LoadBundleFile(input, "legacy-map", false);
            AssetBundleFile bundle = bundleFile.file;
            List<AssetBundleDirectoryInfo> files = bundle.BlockAndDirInfo.DirectoryInfos;
            for (int i = 0; i < files.Count; i++)
            {
                if (!bundle.IsAssetsFile(i))
                {
                    continue;
                }

                AssetsFileInstance assetFile = manager.LoadAssetsFileFromBundle(bundleFile, i, false);
                int changed = 0;
                foreach (AssetFileInfo shader in assetFile.file.GetAssetsOfType(AssetClassID.Shader))
                {
                    AssetTypeValueField data = manager.GetBaseField(assetFile, shader);
                    int converted = RewriteShader(data);
                    if (converted > 0)
                    {
                        shader.SetNewData(data);
                        changed += converted;
                    }
                }

                if (changed > 0)
                {
                    files[i].SetNewData(assetFile.file);
                    programCount += changed;
                }
            }

            if (programCount == 0)
            {
                return null;
            }

            using MemoryStream output = new();
            using AssetsFileWriter writer = new(output);
            bundle.Write(writer);
            return output.ToArray();
        }
        finally
        {
            manager.UnloadAll(true);
        }
    }

    private static int RewriteShader(AssetTypeValueField shader)
    {
        List<AssetTypeValueField> platforms = shader["platforms"]["Array"].Children;
        List<AssetTypeValueField> offsets = shader["offsets"]["Array"].Children;
        List<AssetTypeValueField> packedSizes = shader["compressedLengths"]["Array"].Children;
        List<AssetTypeValueField> sizes = shader["decompressedLengths"]["Array"].Children;
        AssetTypeValueField blob = shader["compressedBlob"]["Array"];
        if (blob.IsDummy || blob.AsByteArray is not { Length: > 0 } source ||
            platforms.Count != offsets.Count || platforms.Count != packedSizes.Count ||
            platforms.Count != sizes.Count)
        {
            return 0;
        }

        int changed = 0;
        using MemoryStream output = new();
        for (int i = 0; i < platforms.Count; i++)
        {
            List<AssetTypeValueField> platformOffsets = offsets[i]["Array"].Children;
            List<AssetTypeValueField> platformPackedSizes = packedSizes[i]["Array"].Children;
            List<AssetTypeValueField> platformSizes = sizes[i]["Array"].Children;
            if (platformOffsets.Count != platformPackedSizes.Count || platformOffsets.Count != platformSizes.Count)
            {
                return 0;
            }

            byte[][]? repaired = null;
            if (platforms[i].AsUInt == Direct3D11 && platformOffsets.Count > 0)
            {
                byte[][] chunks = new byte[platformOffsets.Count][];
                for (int j = 0; j < chunks.Length; j++)
                {
                    int size = checked((int)platformSizes[j].AsUInt);
                    chunks[j] = UnityBundleCompression.Decompress(
                        source,
                        checked((int)platformOffsets[j].AsUInt),
                        checked((int)platformPackedSizes[j].AsUInt),
                        size,
                        2,
                        size);
                }

                int repairedCount = RewritePrograms(chunks);
                if (repairedCount > 0)
                {
                    repaired = chunks;
                    changed += repairedCount;
                }
            }

            for (int j = 0; j < platformOffsets.Count; j++)
            {
                int start = checked((int)platformOffsets[j].AsUInt);
                int length = checked((int)platformPackedSizes[j].AsUInt);
                platformOffsets[j].AsUInt = checked((uint)output.Position);
                if (repaired == null)
                {
                    output.Write(source, start, length);
                    continue;
                }

                byte[] chunk = repaired[j];
                byte[] packed = LZ4Codec.Encode32(chunk, 0, chunk.Length);
                platformPackedSizes[j].AsUInt = checked((uint)packed.Length);
                output.Write(packed, 0, packed.Length);
            }
        }

        if (changed > 0)
        {
            blob.AsByteArray = output.ToArray();
        }

        return changed;
    }

    private static int RewritePrograms(byte[][] chunks)
    {
        BundleBinaryReader index = new(chunks[0], false);
        int count = index.Int32();
        if (count < 0 || count > (chunks[0].Length - sizeof(int)) / ProgramEntrySize)
        {
            throw new InvalidDataException("Invalid shader program table.");
        }

        Dictionary<(int Segment, int Offset), bool> repaired = new();
        int changed = 0;
        for (int i = 0; i < count; i++)
        {
            int offset = index.Int32();
            int lengthPosition = index.Position;
            int length = index.Int32();
            int segment = index.Int32();
            int nextEntry = index.Position;
            if ((uint)segment >= (uint)chunks.Length)
            {
                throw new InvalidDataException("Invalid shader program segment.");
            }

            if (!repaired.TryGetValue((segment, offset), out bool edited))
            {
                edited = RewriteProgram(chunks[segment], offset, length);
                repaired.Add((segment, offset), edited);
            }

            if (edited)
            {
                index.Position = lengthPosition;
                index.WriteInt32(length - AddedHeaderBytes);
                changed++;
            }

            index.Position = nextEntry;
        }

        return changed;
    }

    private static bool RewriteProgram(byte[] chunk, int offset, int length)
    {
        if (offset < 0 || length < sizeof(int) || offset > chunk.Length - length)
        {
            throw new InvalidDataException("Invalid shader program size.");
        }

        int end = offset + length;
        BundleBinaryReader reader = new(chunk, false) { Position = offset };
        if (reader.Int32() != ShaderProgramVersion || length < 32)
        {
            return false;
        }

        reader.Position = offset + 24;
        SkipKeywords(reader, end);
        SkipKeywords(reader, end);
        if (reader.Position > end - sizeof(int))
        {
            throw new InvalidDataException("Missing shader bytecode size.");
        }

        int sizeOffset = reader.Position;
        int size = reader.Int32();
        int bytecode = reader.Position;
        if (size < ExtendedHeaderSize + 32 || size > end - bytecode || chunk[bytecode] != 2)
        {
            return false;
        }

        int dxbc = bytecode + ExtendedHeaderSize;
        reader.Position = dxbc;
        if (reader.Int32() != DxbcSignature)
        {
            return false;
        }

        reader.Position = dxbc + 24;
        if (reader.Int32() != size - ExtendedHeaderSize)
        {
            return false;
        }

        // The older wrapper uses the first six bytes and the same DXBC plus its parameter data.
        chunk[bytecode] = 1;
        Buffer.BlockCopy(chunk, dxbc, chunk, dxbc - AddedHeaderBytes, end - dxbc);
        Array.Clear(chunk, end - AddedHeaderBytes, AddedHeaderBytes);
        reader.Position = sizeOffset;
        reader.WriteInt32(size - AddedHeaderBytes);
        return true;
    }

    private static void SkipKeywords(BundleBinaryReader reader, int end)
    {
        if (reader.Position > end - sizeof(int))
        {
            throw new InvalidDataException("Missing shader keyword count.");
        }

        int count = reader.Int32();
        if (count < 0 || count > (end - reader.Position) / sizeof(int))
        {
            throw new InvalidDataException("Invalid shader keyword count.");
        }

        for (int i = 0; i < count; i++)
        {
            if (reader.Position > end - sizeof(int))
            {
                throw new InvalidDataException("Missing shader keyword size.");
            }

            int length = reader.Int32();
            if (length < 0 || length > end - reader.Position)
            {
                throw new InvalidDataException("Invalid shader keyword size.");
            }

            reader.Position += length;
            reader.Align(4);
        }
    }
}
#endif
