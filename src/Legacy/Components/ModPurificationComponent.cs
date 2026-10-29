using ModComponent.Components;
using ModComponent.SDK.Components;
using UnityEngine;

[HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modpurificationcomponent")]
[AddComponentMenu("")]
public class ModPurificationComponent : ModGenericComponent
{
    [Tooltip("Amount of water (liters) this item can purify.")]
    public float LitersPurify;

    [Tooltip("Time for purification process (seconds).")]
    public float ProgressBarDurationSeconds;

    [Tooltip("Progress bar text key.")]
    public string ProgressBarLocalizationID;

    [Tooltip("Sound effect for purification.")]
    public DataSoundAsset PurifyAudio;
}