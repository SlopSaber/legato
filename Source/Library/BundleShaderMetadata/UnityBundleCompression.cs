#if BEAT_SABER_1453 || LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System;
using System.IO;

namespace Legato.Rendering;

internal static class UnityBundleCompression
{
    internal static byte[] Decompress(byte[] source, int offset, int compressedSize, int size, int compression, int needed)
    {
        if (compressedSize < 0 || offset < 0 || offset > source.Length - compressedSize || size < 0 || needed < 0 || needed > size)
        {
            throw new InvalidDataException("Invalid UnityFS block bounds.");
        }

        byte[] result = new byte[size];
        switch (compression)
        {
            case 0:
                if (compressedSize != size)
                {
                    throw new InvalidDataException("Uncompressed UnityFS block size does not match.");
                }

                Buffer.BlockCopy(source, offset, result, 0, size);
                break;
            case 1:
                new LzmaDecoder(source, offset, compressedSize).Decode(result, needed);
                break;
            case 2:
            case 3:
                DecodeLz4(source, offset, compressedSize, result);
                break;
            default:
                throw new NotSupportedException($"Compression type {compression} is not supported.");
        }

        return result;
    }

    private static void DecodeLz4(byte[] source, int offset, int length, byte[] destination)
    {
        int end = offset + length;
        int written = 0;
        while (offset < end)
        {
            int token = source[offset++];
            int literals = ReadLength(token >> 4);
            if (literals > end - offset || literals > destination.Length - written)
            {
                throw new InvalidDataException("Invalid UnityFS LZ4 literals.");
            }

            Buffer.BlockCopy(source, offset, destination, written, literals);
            offset += literals;
            written += literals;
            if (offset == end)
            {
                break;
            }

            if (end - offset < 2)
            {
                throw new InvalidDataException("Missing UnityFS LZ4 match distance.");
            }

            int distance = source[offset] | (source[offset + 1] << 8);
            offset += 2;
            int match = checked(ReadLength(token & 15) + 4);
            if (distance == 0 || distance > written || match > destination.Length - written)
            {
                throw new InvalidDataException("Invalid UnityFS LZ4 match.");
            }

            for (int read = written - distance; match > 0; match--)
            {
                destination[written++] = destination[read++];
            }
        }

        if (written != destination.Length)
        {
            throw new InvalidDataException("UnityFS LZ4 block ended early.");
        }

        int ReadLength(int size)
        {
            if (size == 15)
            {
                int extra;
                do
                {
                    if (offset == end)
                    {
                        throw new InvalidDataException("Truncated UnityFS LZ4 length.");
                    }

                    extra = source[offset++];
                    size = checked(size + extra);
                }
                while (extra == 255);
            }

            return size;
        }
    }

    // Raw LZMA stream decoder (5-byte properties header, no size field) as written into bundle blocks.
    private sealed class LzmaDecoder
    {
        private const int StateCount = 12;
        private const int PosStatesMax = 16;

        private readonly byte[] _input;
        private readonly int _inputEnd;
        private readonly int _lc;
        private readonly int _lp;
        private readonly int _pb;
        private readonly ushort[] _literals;
        private readonly ushort[] _isMatch = Probabilities(StateCount * PosStatesMax);
        private readonly ushort[] _isRep = Probabilities(StateCount);
        private readonly ushort[] _isRepG0 = Probabilities(StateCount);
        private readonly ushort[] _isRepG1 = Probabilities(StateCount);
        private readonly ushort[] _isRepG2 = Probabilities(StateCount);
        private readonly ushort[] _isRep0Long = Probabilities(StateCount * PosStatesMax);
        private readonly ushort[] _posSlot = Probabilities(4 << 6);
        private readonly ushort[] _posDecoders = Probabilities(1 + 128 - 14);
        private readonly ushort[] _align = Probabilities(1 << 4);
        private readonly LengthDecoder _length = new();
        private readonly LengthDecoder _repLength = new();
        private int _inputPosition;
        private uint _range = 0xFFFFFFFF;
        private uint _code;

        internal LzmaDecoder(byte[] input, int offset, int length)
        {
            if (length < 10 || input[offset] >= 225)
            {
                throw new InvalidDataException("Invalid UnityFS LZMA header.");
            }

            _input = input;
            _inputEnd = offset + length;
            int properties = input[offset];
            _lc = properties % 9;
            properties /= 9;
            _lp = properties % 5;
            _pb = properties / 5;
            _literals = Probabilities(0x300 << (_lc + _lp));
            _inputPosition = offset + 6;
            for (int i = 0; i < 4; i++)
            {
                _code = (_code << 8) | _input[_inputPosition++];
            }
        }

        internal void Decode(byte[] output, int limit)
        {
            int state = 0;
            uint rep0 = 0;
            uint rep1 = 0;
            uint rep2 = 0;
            uint rep3 = 0;
            int position = 0;
            int pbMask = (1 << _pb) - 1;
            while (position < limit)
            {
                int posState = position & pbMask;
                if (Bit(_isMatch, (state << 4) + posState) == 0)
                {
                    output[position] = DecodeLiteral(output, position, state, rep0);
                    position++;
                    state = state < 4 ? 0 : state < 10 ? state - 3 : state - 6;
                    continue;
                }

                int length;
                if (Bit(_isRep, state) != 0)
                {
                    if (Bit(_isRepG0, state) == 0)
                    {
                        if (Bit(_isRep0Long, (state << 4) + posState) == 0)
                        {
                            state = state < 7 ? 9 : 11;
                            output[position] = output[position - rep0 - 1];
                            position++;
                            continue;
                        }
                    }
                    else
                    {
                        uint distance;
                        if (Bit(_isRepG1, state) == 0)
                        {
                            distance = rep1;
                        }
                        else
                        {
                            if (Bit(_isRepG2, state) == 0)
                            {
                                distance = rep2;
                            }
                            else
                            {
                                distance = rep3;
                                rep3 = rep2;
                            }

                            rep2 = rep1;
                        }

                        rep1 = rep0;
                        rep0 = distance;
                    }

                    length = _repLength.Decode(this, posState);
                    state = state < 7 ? 8 : 11;
                }
                else
                {
                    rep3 = rep2;
                    rep2 = rep1;
                    rep1 = rep0;
                    length = _length.Decode(this, posState);
                    state = state < 7 ? 7 : 10;
                    rep0 = DecodeDistance(length);
                    if (rep0 == 0xFFFFFFFF)
                    {
                        throw new InvalidDataException("UnityFS LZMA block ended early.");
                    }
                }

                for (length += 2; length > 0 && position < limit; length--)
                {
                    output[position] = output[position - rep0 - 1];
                    position++;
                }
            }
        }

        private static ushort[] Probabilities(int count)
        {
            ushort[] probabilities = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                probabilities[i] = 1024;
            }

            return probabilities;
        }

        private byte DecodeLiteral(byte[] output, int position, int state, uint rep0)
        {
            int previous = position > 0 ? output[position - 1] : 0;
            int literalState = ((position & ((1 << _lp) - 1)) << _lc) + (previous >> (8 - _lc));
            int baseIndex = 0x300 * literalState;
            int symbol = 1;
            if (state >= 7)
            {
                int matchByte = output[position - rep0 - 1];
                do
                {
                    int matchBit = (matchByte >> 7) & 1;
                    matchByte <<= 1;
                    int bit = Bit(_literals, baseIndex + ((1 + matchBit) << 8) + symbol);
                    symbol = (symbol << 1) | bit;
                    if (matchBit != bit)
                    {
                        break;
                    }
                }
                while (symbol < 0x100);
            }

            while (symbol < 0x100)
            {
                symbol = (symbol << 1) | Bit(_literals, baseIndex + symbol);
            }

            return (byte)(symbol - 0x100);
        }

        private uint DecodeDistance(int length)
        {
            int lengthState = Math.Min(length, 3);
            int posSlot = Tree(_posSlot, lengthState << 6, 6);
            if (posSlot < 4)
            {
                return (uint)posSlot;
            }

            int directBits = (posSlot >> 1) - 1;
            uint distance = (uint)((2 | (posSlot & 1)) << directBits);
            if (posSlot < 14)
            {
                return distance + ReverseTree(_posDecoders, (int)distance - posSlot, directBits);
            }

            distance += DirectBits(directBits - 4) << 4;
            return distance + ReverseTree(_align, 0, 4);
        }

        private int Bit(ushort[] probabilities, int index)
        {
            uint probability = probabilities[index];
            uint bound = (_range >> 11) * probability;
            int bit;
            if (_code < bound)
            {
                probability += (2048 - probability) >> 5;
                _range = bound;
                bit = 0;
            }
            else
            {
                probability -= probability >> 5;
                _code -= bound;
                _range -= bound;
                bit = 1;
            }

            probabilities[index] = (ushort)probability;
            Normalize();
            return bit;
        }

        private uint DirectBits(int count)
        {
            uint result = 0;
            for (; count > 0; count--)
            {
                _range >>= 1;
                _code -= _range;
                uint mask = 0 - (_code >> 31);
                _code += _range & mask;
                Normalize();
                result = (result << 1) + mask + 1;
            }

            return result;
        }

        private int Tree(ushort[] probabilities, int offset, int bits)
        {
            int m = 1;
            for (int i = 0; i < bits; i++)
            {
                m = (m << 1) + Bit(probabilities, offset + m);
            }

            return m - (1 << bits);
        }

        private uint ReverseTree(ushort[] probabilities, int offset, int bits)
        {
            int m = 1;
            uint symbol = 0;
            for (int i = 0; i < bits; i++)
            {
                int bit = Bit(probabilities, offset + m);
                m = (m << 1) + bit;
                symbol |= (uint)bit << i;
            }

            return symbol;
        }

        private void Normalize()
        {
            if (_range < 1u << 24)
            {
                if (_inputPosition == _inputEnd)
                {
                    throw new InvalidDataException("Truncated UnityFS LZMA block.");
                }

                _range <<= 8;
                _code = (_code << 8) | _input[_inputPosition++];
            }
        }

        private sealed class LengthDecoder
        {
            private readonly ushort[] _choice = Probabilities(2);
            private readonly ushort[] _low = Probabilities(PosStatesMax << 3);
            private readonly ushort[] _mid = Probabilities(PosStatesMax << 3);
            private readonly ushort[] _high = Probabilities(1 << 8);

            internal int Decode(LzmaDecoder decoder, int posState)
            {
                if (decoder.Bit(_choice, 0) == 0)
                {
                    return decoder.Tree(_low, posState << 3, 3);
                }

                return decoder.Bit(_choice, 1) == 0
                    ? 8 + decoder.Tree(_mid, posState << 3, 3)
                    : 16 + decoder.Tree(_high, 0, 8);
            }
        }
    }
}
#endif
