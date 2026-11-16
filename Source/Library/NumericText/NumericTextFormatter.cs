#nullable enable
using System;
using TMPro;

namespace Legato.Presentation {
    internal sealed class NumericTextFormatter {
#if BEAT_SABER_1_45_3
        private readonly char[] buffer = new char[128];
        private string? lastText;
#endif

        internal void Publish(TMP_Text target, float value, string format) {
#if BEAT_SABER_1_45_3
            if (value.TryFormat(buffer.AsSpan(), out int length, format.AsSpan(), provider: null)) {
                PublishCharacters(target, length);
                return;
            }
#endif
            string text = value.ToString(format);
            target.text = text;
#if BEAT_SABER_1_45_3
            lastText = text;
#endif
        }

        internal void Publish(TMP_Text target, float left, float right, string format) {
#if BEAT_SABER_1_45_3
            if (left.TryFormat(buffer.AsSpan(), out int leftLength, format.AsSpan(), provider: null)
                && buffer.Length - leftLength >= 3) {
                buffer[leftLength] = ' ';
                buffer[leftLength + 1] = '|';
                buffer[leftLength + 2] = ' ';
                int rightStart = leftLength + 3;
                if (right.TryFormat(buffer.AsSpan(rightStart), out int rightLength, format.AsSpan(), provider: null)) {
                    PublishCharacters(target, rightStart + rightLength);
                    return;
                }
            }
#endif
            string text = $"{left.ToString(format)} | {right.ToString(format)}";
            target.text = text;
#if BEAT_SABER_1_45_3
            lastText = text;
#endif
        }

#if BEAT_SABER_1_45_3
        private void PublishCharacters(TMP_Text target, int length) {
            bool unchanged = lastText != null && lastText.Length == length;
            for (int i = 0; unchanged && i < length; i++)
                unchanged = lastText![i] == buffer[i];
            if (unchanged) {
                if (target.text != lastText) target.text = lastText;
                return;
            }
            string text = new string(buffer, 0, length);
            target.text = text;
            lastText = text;
        }
#endif
    }
}
