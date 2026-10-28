using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModComponent.SDK.Modules;
using ModComponent.SDK.References;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ModComponent.SDK.Editor.Build.Export
{
    /// <summary>
    /// Turns a module into the JSON object ModComponent reads. Every public field is written, base class
    /// fields first, because the loader replaces a missing key with null or zero rather than its own default.
    /// </summary>
    public static class ModuleSerializer
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();

        public static JObject Serialize(ModuleData module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var json = new JObject();
            foreach (var field in GetExportedFields(module.GetType()))
            {
                var split = field.GetCustomAttribute<SplitArraysAttribute>();
                if (split != null)
                {
                    WriteSplit(json, split, (IEnumerable)field.GetValue(module));
                    continue;
                }

                json[ExportNameOf(field)] = ToToken(field.GetValue(module), field.FieldType, $"{module.GetType().Name}.{field.Name}");
            }

            return json;
        }

        /// <summary>The JSON keys a module type produces, in order. Used by tests to pin the loader contract.</summary>
        public static IEnumerable<string> GetExportedKeys(Type moduleType)
        {
            foreach (var field in GetExportedFields(moduleType))
            {
                var split = field.GetCustomAttribute<SplitArraysAttribute>();
                if (split != null)
                {
                    yield return split.FirstKey;
                    yield return split.SecondKey;
                }
                else
                {
                    yield return ExportNameOf(field);
                }
            }
        }

        public static string ExportNameOf(FieldInfo field) =>
            field.GetCustomAttribute<ExportNameAttribute>()?.Name ?? field.Name;

        /// <summary>Public instance fields from the root base class down to the module type.</summary>
        public static FieldInfo[] GetExportedFields(Type type)
        {
            if (FieldCache.TryGetValue(type, out var cached)) return cached;

            var chain = new List<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType) chain.Add(t);
            chain.Reverse();

            var fields = chain
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(f => !f.IsNotSerialized && !f.IsInitOnly)
                .ToArray();
            FieldCache[type] = fields;
            return fields;
        }

        private static void WriteSplit(JObject json, SplitArraysAttribute split, IEnumerable entries)
        {
            var first = new JArray();
            var second = new JArray();
            foreach (var entry in entries ?? Array.Empty<object>())
            {
                if (entry is not GearCount pair)
                    throw new NotSupportedException($"[SplitArrays] only supports {nameof(GearCount)} entries.");
                first.Add(pair.Item.Id);
                second.Add(pair.Count);
            }

            json[split.FirstKey] = first;
            json[split.SecondKey] = second;
        }

        private static JToken ToToken(object value, Type type, string path)
        {
            if (type == typeof(string)) return new JValue((string)value ?? string.Empty);
            if (type == typeof(bool) || type == typeof(int) || type == typeof(float)) return new JValue(value);
            if (type.IsEnum) return new JValue(Enum.GetName(type, value) ?? value.ToString());
            if (type == typeof(Vector3))
            {
                var v = (Vector3)value;
                return new JArray(v.x, v.y, v.z);
            }

            if (typeof(IReference).IsAssignableFrom(type)) return new JValue(((IReference)value)?.Id ?? string.Empty);

            if (typeof(IList).IsAssignableFrom(type))
            {
                var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
                if (elementType == null) throw new NotSupportedException($"{path}: cannot export untyped list {type.Name}.");
                var array = new JArray();
                foreach (var item in (IList)value ?? Array.Empty<object>())
                    array.Add(ToToken(item, elementType, path));
                return array;
            }

            throw new NotSupportedException($"{path}: fields of type {type.Name} cannot be exported.");
        }
    }
}
