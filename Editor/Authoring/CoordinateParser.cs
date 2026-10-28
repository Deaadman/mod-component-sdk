using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModComponent.SDK.Editor.Authoring
{
    /// <summary>
    /// Reads a position (and optional rotation) from text copied out of the game or a mod's debug output, so
    /// spawn points don't have to be typed in by hand.
    /// </summary>
    public static class CoordinateParser
    {
        private static readonly Regex Labelled =
            new(@"\b([pr])\s*=\s*(-?[\d.]+(?:[eE]-?\d+)?)\s*,\s*(-?[\d.]+(?:[eE]-?\d+)?)\s*,\s*(-?[\d.]+(?:[eE]-?\d+)?)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Accepts "p=1.5,2,3 r=0,90,0" (GearSpawner lines), "(1.5, 2.0, 3.0)" (Unity's Vector3 text), or any three
        /// or six numbers. When the text has no '.', and splitting on spaces alone gives three or six numbers, commas
        /// are read as decimal points ("1,5 2 3,25").
        /// </summary>
        public static bool TryParse(string text, out Vector3 position, out Vector3? rotation)
        {
            position = default;
            rotation = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var labelled = Labelled.Matches(text).Cast<Match>().ToList();
            if (labelled.Count > 0)
            {
                var found = false;
                foreach (var m in labelled)
                {
                    if (!TryVector(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value, out var v)) continue;
                    if (char.ToLowerInvariant(m.Groups[1].Value[0]) == 'p') { position = v; found = true; }
                    else rotation = v;
                }

                return found;
            }

            var cleaned = Regex.Replace(text, @"[^\d.,eE\s-]", " ").Trim();
            string[] numbers;
            var bySpace = cleaned.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (!cleaned.Contains('.') && bySpace.Length is 3 or 6)
                numbers = bySpace.Select(n => n.Replace(',', '.')).ToArray();
            else
                numbers = Regex.Split(cleaned, @"[,\s]+").Where(s => s.Length > 0).ToArray();

            if (numbers.Length != 3 && numbers.Length != 6) return false;
            if (!TryVector(numbers[0], numbers[1], numbers[2], out position)) return false;
            if (numbers.Length == 6 && TryVector(numbers[3], numbers[4], numbers[5], out var r)) rotation = r;
            return true;
        }

        private static bool TryVector(string x, string y, string z, out Vector3 v)
        {
            v = default;
            if (!Parse(x, out var fx) || !Parse(y, out var fy) || !Parse(z, out var fz)) return false;
            v = new Vector3(fx, fy, fz);
            return true;
        }

        private static bool Parse(string s, out float value) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
