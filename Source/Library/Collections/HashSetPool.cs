using System;
using System.Collections.Generic;

namespace Legato.Collections
{
    internal static class HashSetPool<T>
    {
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
        private const int MaximumRetained = 10000;
        private static readonly Stack<HashSet<T>> Inactive = new Stack<HashSet<T>>(10);
#endif

        internal static HashSet<T> Get()
        {
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            return Inactive.Count == 0 ? new HashSet<T>() : Inactive.Pop();
#else
            return UnityEngine.Pool.HashSetPool<T>.Get();
#endif
        }

        internal static void Release(HashSet<T> collection)
        {
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (Inactive.Contains(collection))
            {
                throw new InvalidOperationException("The collection has already been returned to the pool.");
            }

            collection.Clear();
            if (Inactive.Count < MaximumRetained)
            {
                Inactive.Push(collection);
            }
#else
            UnityEngine.Pool.HashSetPool<T>.Release(collection);
#endif
        }
    }
}
