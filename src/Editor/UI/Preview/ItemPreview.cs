using System;
using System.IO;
using System.Linq;
using ModComponent.SDK.Editor.Authoring;
using ModComponent.SDK.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModComponent.SDK.Editor.UI.Preview
{
    /// <summary>
    /// Renders an item's prefab off-screen, either the way the inspect view frames it (camera straight ahead at
    /// the inspect distance, model rotated, offset and scaled by the component's inspect settings) or framed to
    /// fit for an inventory icon. The inspect view is an approximation of the game's, close enough to line it up.
    /// </summary>
    public sealed class ItemPreview : IDisposable
    {
        private readonly PreviewRenderUtility _utility;
        private readonly GameObject _instance;

        public ItemPreview(GameObject prefab)
        {
            _utility = new PreviewRenderUtility();
            var camera = _utility.camera;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.14f, 0.15f, 1f);
            _utility.lights[0].intensity = 1.2f;
            _utility.lights[0].transform.rotation = Quaternion.Euler(40f, 30f, 0f);
            _utility.lights[1].intensity = 0.6f;
            _utility.ambientColor = new Color(0.35f, 0.35f, 0.38f);

            _instance = _utility.InstantiatePrefabInScene(prefab);
            foreach (var behaviour in _instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        }

        public void Dispose() => _utility.Cleanup();

        /// <summary>Shows only the named direct child (NormalModel or InspectModel) when the item swaps models.</summary>
        public void ShowOnly(string childName)
        {
            foreach (Transform child in _instance.transform)
                child.gameObject.SetActive(string.IsNullOrEmpty(childName) || child.name == childName);
        }

        /// <summary>The inspect view: camera at the origin looking forward, model at <c>distance</c> plus offset.</summary>
        public void PoseForInspect(ComponentModule component, Vector3? anglesOverride = null)
        {
            var useInspectModel = !component.NormalModel.IsEmpty && !component.InspectModel.IsEmpty;
            ShowOnly(useInspectModel ? component.InspectModel.Id : null);
            _instance.transform.SetPositionAndRotation(
                new Vector3(0, 0, Mathf.Max(0.05f, component.InspectDistance)) + component.InspectOffset,
                Quaternion.Euler(anglesOverride ?? component.InspectAngles));
            _instance.transform.localScale = component.InspectScale;
            _utility.camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        /// <summary>Frames the whole model from a three-quarter view, filling the image.</summary>
        public void PoseForIcon(Vector3 angles, string onlyChild = null)
        {
            ShowOnly(onlyChild);
            _instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(angles));
            _instance.transform.localScale = Vector3.one;
            var renderers = _instance.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
            if (renderers.Count == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            var radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
            var distance = radius / Mathf.Sin(_utility.camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _utility.camera.transform.SetPositionAndRotation(bounds.center - Vector3.forward * distance, Quaternion.identity);
        }

        public void SetBackground(Color color) => _utility.camera.backgroundColor = color;

        /// <summary>Draws into the current IMGUI rect. Call from OnGUI.</summary>
        public void Draw(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            _utility.BeginPreview(rect, GUIStyle.none);
            _utility.Render(true);
            _utility.EndAndDrawPreview(rect);
        }

        /// <summary>Renders to a new texture of the given size. The caller owns the texture.</summary>
        public Texture2D RenderTexture(int size)
        {
            _utility.BeginStaticPreview(new Rect(0, 0, size, size));
            _utility.Render(true);
            return _utility.EndStaticPreview();
        }

        public static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
    }

    /// <summary>
    /// Makes an item's inventory icon (ico_GearItem__Name.png) or crafting icon (ico_CraftItem__Name.png), saves it
    /// next to the item and assigns it.
    /// </summary>
    public static class IconRenderer
    {
        public static readonly Vector3 DefaultAngles = new(20f, -35f, 0f);

        /// <summary>Renders the model.</summary>
        public static Texture2D RenderAndAssign(GearItem item, int size = 256, bool crafting = false)
        {
            if (item.Prefab == null) throw new InvalidOperationException("The item has no prefab to render.");
            if (!ItemPreview.CanRender) throw new InvalidOperationException("Icons can't be rendered without a graphics device.");

            Texture2D rendered;
            using (var preview = new ItemPreview(item.Prefab))
            {
                preview.SetBackground(Color.clear);
                preview.PoseForIcon(DefaultAngles, item.Component?.NormalModel.Id);
                rendered = preview.RenderTexture(size);
            }

            var pixels = rendered.GetPixels();
            var renderedSize = rendered.width;
            UnityEngine.Object.DestroyImmediate(rendered);
            return SaveAndAssign(item, renderedSize, pixels, crafting);
        }

        /// <summary>A plain coloured square, for when nothing can be rendered (e.g. the example mod in batch mode).</summary>
        public static Texture2D PlaceholderAndAssign(GearItem item, Color color, bool crafting = false, int size = 64)
        {
            var border = Color.Lerp(color, Color.black, 0.35f);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                pixels[y * size + x] = x < 4 || y < 4 || x >= size - 4 || y >= size - 4 ? border : color;
            return SaveAndAssign(item, size, pixels, crafting);
        }

        private static Texture2D SaveAndAssign(GearItem item, int size, Color[] pixels, bool crafting)
        {
            var itemPath = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrEmpty(itemPath)) throw new InvalidOperationException("Save the item as an asset before making its icon.");
            var folder = Path.GetDirectoryName(itemPath)?.Replace('\\', '/') ?? AssetFactory.DefaultModsFolder;
            var path = $"{folder}/{(crafting ? item.CraftingIconAddress : item.InventoryIconAddress)}.png";
            var readable = new Texture2D(size, size, TextureFormat.RGBA32, false);
            readable.SetPixels(pixels);
            readable.Apply();
            File.WriteAllBytes(path, readable.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(readable);

            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Undo.RecordObject(item, "Set icon");
            if (crafting) item.CraftingIcon = icon;
            else item.Icon = icon;
            EditorUtility.SetDirty(item);
            return icon;
        }
    }
}
