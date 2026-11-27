#if LEGATO_EARLY_VR_UI
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace Legato.LocalizationSupport;

/// <summary>Provides the embedded English UI strings on early game versions.</summary>
public static class EarlyLocalization
{
    private static readonly Lazy<Dictionary<string, string>> Text = new(Load);

    /// <summary>Looks up an embedded UI string by key.</summary>
    /// <param name="key">The localization key.</param>
    /// <returns>The translation, or the original key when no translation is available.</returns>
    public static string Get(string key)
    {
        return !string.IsNullOrEmpty(key) && Text.Value.TryGetValue(key, out string text) ? text : key;
    }

    /// <summary>Adds the English column of a Polyglot CSV table to the embedded UI strings.</summary>
    /// <param name="csv">The CSV table content.</param>
    public static void AddCsv(string csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return;
        }

        lock (Text.Value)
        {
            AddRows(Text.Value, new StringReader(csv));
        }
    }

    private static Dictionary<string, string> Load()
    {
        Dictionary<string, string> text = new(StringComparer.Ordinal)
        {
            ["BUTTON_CANCEL"] = "Cancel",
            ["BUTTON_OK"] = "OK",
        };
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("BeatSaberMarkupLanguage.Resources.beat-saber-markup-language.csv");
        if (stream == null)
        {
            return text;
        }

        using StreamReader reader = new(stream);
        AddRows(text, reader);
        return text;
    }

    private static void AddRows(Dictionary<string, string> text, TextReader reader)
    {
        foreach (List<string> row in ReadRows(reader))
        {
            if (row.Count > 2 && row[0] != "Polyglot" && !string.IsNullOrEmpty(row[0]) && !string.IsNullOrEmpty(row[2]))
            {
                text[row[0]] = row[2];
            }
        }
    }

    private static IEnumerable<List<string>> ReadRows(TextReader reader)
    {
        List<string> row = new();
        StringBuilder field = new();
        bool quoted = false;
        for (int value = reader.Read(); value != -1; value = reader.Read())
        {
            char character = (char)value;
            if (character == '"' && quoted && reader.Peek() == '"')
            {
                reader.Read();
                field.Append('"');
            }
            else if (character == '"' && (quoted || field.Length == 0))
            {
                quoted = !quoted;
            }
            else if (!quoted && (character == ',' || character == '\r' || character == '\n'))
            {
                row.Add(field.ToString());
                field.Clear();
                if (character != ',')
                {
                    if (character == '\r' && reader.Peek() == '\n')
                    {
                        reader.Read();
                    }

                    yield return row;
                    row = new List<string>();
                }
            }
            else
            {
                field.Append(character);
            }
        }

        if (row.Count > 0 || field.Length > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }
}

public sealed class EarlyLocalizedText : MonoBehaviour
{
    public TextMeshProUGUI localizedComponent { get; set; }

    public bool maintainTextAlignment { get; set; }

    public string key { get; set; }

    public string Key
    {
        get => key;
        set
        {
            key = value;
            if (isActiveAndEnabled)
            {
                Apply();
            }
        }
    }

    private void OnEnable() => Apply();

    private void Apply()
    {
        if (localizedComponent != null && !string.IsNullOrEmpty(key))
        {
            localizedComponent.text = EarlyLocalization.Get(key);
        }
    }
}
#endif
