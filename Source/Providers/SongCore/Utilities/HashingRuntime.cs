using System;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System.Collections.Concurrent;
using System.Threading;
#endif
namespace SongCore.Utilities
{
    internal static class HashingRuntime
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private static readonly ConcurrentBag<byte[]> Buffers = new();
        private static int _retained;
        internal static byte[] Rent()
        {
            if (!Buffers.TryTake(out var buffer)) return new byte[131072];
            Interlocked.Decrement(ref _retained);
            return buffer;
        }
        internal static void Return(byte[] buffer)
        {
            if (Interlocked.Increment(ref _retained) <= Math.Max(2, Environment.ProcessorCount)) Buffers.Add(buffer);
            else Interlocked.Decrement(ref _retained);
        }
#else
        internal static byte[] Rent() => System.Buffers.ArrayPool<byte>.Shared.Rent(131072);
        internal static void Return(byte[] buffer) => System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
#endif
        internal static string ToHexString(byte[] bytes)
        {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var chars = new char[bytes.Length * 2];
            Fill(chars, bytes);
            return new string(chars);
#else
            return string.Create(bytes.Length * 2, bytes, (chars, state) => Fill(chars, state));
#endif
        }
        private static void Fill(Span<char> chars, byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                int high = bytes[i] >> 4, low = bytes[i] & 15;
                chars[2 * i] = (char)(high < 10 ? '0' + high : 'A' - 10 + high);
                chars[2 * i + 1] = (char)(low < 10 ? '0' + low : 'A' - 10 + low);
            }
        }
    }
}
