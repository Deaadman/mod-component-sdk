using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModComponent.SDK.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/Workspace-Information#mod-localization-assets")]
    public class ModLocalization : ScriptableObject
    {
        [Serializable]
        public class LocalizationEntry
        {
            [Tooltip("Key for this localization entry.")]
            public string localizationKey;

            [Tooltip("Localized text for various languages.")]
            public Languages languages = new();
        }

        [Serializable]
        public class Languages
        {
            [Tooltip("Text in English.")]
            public string English = "";

            [Tooltip("Text in German.")]
            public string German = "";

            [Tooltip("Text in Russian.")]
            public string Russian = "";

            [Tooltip("Text in French.")]
            public string French = "";

            [Tooltip("Text in Japanese.")]
            public string Japanese = "";

            [Tooltip("Text in Korean.")]
            public string Korean = "";

            [Tooltip("Text in Simplified Chinese.")]
            public string SimplifiedChinese = "";

            [Tooltip("Text in Swedish.")]
            public string Swedish = "";

            [Tooltip("Text in Traditional Chinese.")]
            public string TraditionalChinese = "";

            [Tooltip("Text in Turkish.")]
            public string Turkish = "";

            [Tooltip("Text in Norwegian.")]
            public string Norwegian = "";

            [Tooltip("Text in Spanish.")]
            public string Spanish = "";

            [Tooltip("Text in Portuguese (Portugal).")]
            public string PortuguesePortugal = "";

            [Tooltip("Text in Portuguese (Brazil).")]
            public string PortugueseBrazil = "";

            [Tooltip("Text in Dutch.")]
            public string Dutch = "";

            [Tooltip("Text in Finnish.")]
            public string Finnish = "";

            [Tooltip("Text in Italian.")]
            public string Italian = "";

            [Tooltip("Text in Polish.")]
            public string Polish = "";

            [Tooltip("Text in Ukrainian.")]
            public string Ukrainian = "";
        }

        [Tooltip("All localization entries.")]
        public List<LocalizationEntry> localizationEntries = new();
    }
}