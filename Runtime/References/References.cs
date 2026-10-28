using System;
using UnityEngine;

namespace ModComponent.SDK.References
{
    /// <summary>
    /// A name ModComponent resolves at runtime. Stored as the exact string the loader expects,
    /// so references never depend on generated assets.
    /// </summary>
    public interface IReference
    {
        string Id { get; }
        bool IsEmpty { get; }
    }

    /// <summary>A gear prefab name such as <c>GEAR_Hatchet</c>, vanilla or from any mod.</summary>
    [Serializable]
    public struct GearRef : IReference, IEquatable<GearRef>
    {
        [SerializeField] private string id;

        public GearRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator GearRef(string id) => new(id);
        public bool Equals(GearRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is GearRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>A Wwise event name such as <c>Play_SndInvClothLight</c>.</summary>
    [Serializable]
    public struct SoundRef : IReference, IEquatable<SoundRef>
    {
        [SerializeField] private string id;

        public SoundRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator SoundRef(string id) => new(id);
        public bool Equals(SoundRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is SoundRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>An internal scene name such as <c>CoastalRegion</c>.</summary>
    [Serializable]
    public struct SceneRef : IReference, IEquatable<SceneRef>
    {
        [SerializeField] private string id;

        public SceneRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator SceneRef(string id) => new(id);
        public bool Equals(SceneRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is SceneRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>A loot table name such as <c>LootTableVehicleGloveBox</c>.</summary>
    [Serializable]
    public struct LootTableRef : IReference, IEquatable<LootTableRef>
    {
        [SerializeField] private string id;

        public LootTableRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator LootTableRef(string id) => new(id);
        public bool Equals(LootTableRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is LootTableRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>A liquid type such as <c>LIQUID_WaterPotable</c>.</summary>
    [Serializable]
    public struct LiquidRef : IReference, IEquatable<LiquidRef>
    {
        [SerializeField] private string id;

        public LiquidRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator LiquidRef(string id) => new(id);
        public bool Equals(LiquidRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is LiquidRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>A powder type such as <c>POWDER_Gunpowder</c>.</summary>
    [Serializable]
    public struct PowderRef : IReference, IEquatable<PowderRef>
    {
        [SerializeField] private string id;

        public PowderRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator PowderRef(string id) => new(id);
        public bool Equals(PowderRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is PowderRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>A localization key such as <c>GAMEPLAY_WoolBalaclava</c>.</summary>
    [Serializable]
    public struct LocalizationKey : IReference, IEquatable<LocalizationKey>
    {
        [SerializeField] private string id;

        public LocalizationKey(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator LocalizationKey(string id) => new(id);
        public bool Equals(LocalizationKey other) => Id == other.Id;
        public override bool Equals(object obj) => obj is LocalizationKey other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }

    /// <summary>
    /// The name of a direct child of the item's prefab. ModComponent looks children up
    /// with a non-recursive find, so grandchildren never resolve.
    /// </summary>
    [Serializable]
    public struct ChildRef : IReference, IEquatable<ChildRef>
    {
        [SerializeField] private string id;

        public ChildRef(string id) => this.id = id ?? string.Empty;
        public string Id => id ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(id);

        public static implicit operator ChildRef(string id) => new(id);
        public bool Equals(ChildRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is ChildRef other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id;
    }
}
