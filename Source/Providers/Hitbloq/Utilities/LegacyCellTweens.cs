#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections;
using UnityEngine;

namespace Hitbloq.Utilities
{
	internal sealed class LegacyCellTweens
	{
		private int _generation;

		internal void Cancel()
		{
			_generation++;
		}

		internal void Animate(MonoBehaviour owner, float duration, Action<float> update)
		{
			if (!owner.isActiveAndEnabled)
			{
				update(1);
				return;
			}
			owner.StartCoroutine(Run(_generation, duration, update));
		}

		private IEnumerator Run(int generation, float duration, Action<float> update)
		{
			float start = Time.unscaledTime;
			while (generation == _generation)
			{
				float progress = Mathf.Clamp01((Time.unscaledTime - start) / duration);
				update(progress);
				if (progress >= 1)
					yield break;
				yield return null;
			}
		}
	}
}
#endif
