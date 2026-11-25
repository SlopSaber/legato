using System;
using System.Collections.Generic;

namespace Legato.Collections
{
    internal static class ListPool<T>
    {
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
        private const int MaximumRetained = 10000;
        private static readonly Stack<List<T>> Inactive = new Stack<List<T>>(10);
#endif

        internal static List<T> Get()
        {
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            return Inactive.Count == 0 ? new List<T>() : Inactive.Pop();
#else
            return UnityEngine.Pool.ListPool<T>.Get();
#endif
        }

        internal static void Release(List<T> collection)
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
            UnityEngine.Pool.ListPool<T>.Release(collection);
#endif
        }
    }
}
