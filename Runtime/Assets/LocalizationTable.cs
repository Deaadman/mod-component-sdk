using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModComponent.SDK
{
    /// <summary>
    /// Localization keys and their text per language. Only languages with text are stored;
    /// ModComponent falls back to English for the rest.
    /// </summary>
    [CreateAssetMenu(menuName = "ModComponent SDK/Localization Table", fileName = "Localization", order = 3)]
    public sealed class LocalizationTable : ScriptableObject
    {
        public const string DefaultLanguage = "English";

        [Serializable]
        public struct Translation
        {
            [Tooltip("Language name exactly as the game reports it, e.g. \"French (France)\".")]
            public string Language;

            [TextArea(1, 6)] public string Text;
        }

        [Serializable]
        public sealed class Entry
        {
            public string Key = string.Empty;
            public List<Translation> Translations = new();

            public string Get(string language) =>
                Translations.FirstOrDefault(t => t.Language == language).Text;

            public void Set(string language, string text)
            {
                var index = Translations.FindIndex(t => t.Language == language);
                if (string.IsNullOrEmpty(text))
                {
                    if (index >= 0) Translations.RemoveAt(index);
                    return;
                }

                var translation = new Translation { Language = language, Text = text };
                if (index >= 0) Translations[index] = translation;
                else Translations.Add(translation);
            }
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        public Entry Find(string key) => entries.FirstOrDefault(e => e.Key == key);

        public bool Contains(string key) => Find(key) != null;

        public Entry GetOrAdd(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key is empty.", nameof(key));
            var entry = Find(key);
            if (entry != null) return entry;
            entry = new Entry { Key = key };
            entries.Add(entry);
            return entry;
        }

        public bool Remove(string key) => entries.RemoveAll(e => e.Key == key) > 0;

        public string Get(string key, string language = DefaultLanguage) => Find(key)?.Get(language);

        public void Set(string key, string language, string text) => GetOrAdd(key).Set(language, text);
    }
}
