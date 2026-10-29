using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModComponent.SDK.Editor.Authoring
{
    /// <summary>
    /// Round-trips a <see cref="LocalizationTable"/> through CSV (RFC 4180) so translators can work in a
    /// spreadsheet. The header row is "Key" followed by the game's language names.
    /// </summary>
    public static class LocalizationCsv
    {
        public const string KeyColumn = "Key";

        public static string Export(LocalizationTable table, IReadOnlyList<string> languages)
        {
            var sb = new StringBuilder();
            WriteRow(sb, new[] { KeyColumn }.Concat(languages));
            foreach (var entry in table.Entries.Where(e => !string.IsNullOrEmpty(e.Key)))
                WriteRow(sb, new[] { entry.Key }.Concat(languages.Select(l => entry.Get(l) ?? string.Empty)));
            return sb.ToString();
        }

        public sealed class ImportResult
        {
            public int Added;
            public int Updated;
            public readonly List<string> UnknownLanguages = new();
        }

        /// <summary>
        /// Merges CSV text into the table: new keys are added, non-empty cells overwrite, empty cells leave
        /// existing text alone. Columns whose header isn't a known language are skipped and reported.
        /// </summary>
        public static ImportResult Import(LocalizationTable table, string csv, IReadOnlyCollection<string> knownLanguages)
        {
            var rows = Parse(csv);
            var result = new ImportResult();
            if (rows.Count == 0) return result;

            var header = rows[0];
            if (header.Count == 0 || !string.Equals(header[0].Trim(), KeyColumn, StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"The first column must be \"{KeyColumn}\".");

            var columns = new List<(int index, string language)>();
            for (var i = 1; i < header.Count; i++)
            {
                var language = header[i].Trim();
                if (knownLanguages.Contains(language)) columns.Add((i, language));
                else if (language.Length > 0) result.UnknownLanguages.Add(language);
            }

            foreach (var row in rows.Skip(1))
            {
                var key = row.Count > 0 ? row[0].Trim() : string.Empty;
                if (key.Length == 0) continue;
                var existed = table.Contains(key);
                var entry = table.GetOrAdd(key);
                var changed = false;
                foreach (var (index, language) in columns)
                {
                    if (index >= row.Count || string.IsNullOrEmpty(row[index])) continue;
                    if (entry.Get(language) == row[index]) continue;
                    entry.Set(language, row[index]);
                    changed = true;
                }

                if (!existed) result.Added++;
                else if (changed) result.Updated++;
            }

            return result;
        }

        public static List<List<string>> Parse(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var quoted = false;
            csv ??= string.Empty;
            if (csv.Length > 0 && csv[0] == '﻿') csv = csv.Substring(1);

            for (var i = 0; i < csv.Length; i++)
            {
                var c = csv[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { cell.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else cell.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"': quoted = true; break;
                    case ',': row.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default: cell.Append(c); break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static void WriteRow(StringBuilder sb, IEnumerable<string> cells)
        {
            sb.Append(string.Join(",", cells.Select(Escape))).Append("\r\n");
        }

        private static string Escape(string cell)
        {
            cell ??= string.Empty;
            return cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + cell.Replace("\"", "\"\"") + "\"" : cell;
        }
    }
}
