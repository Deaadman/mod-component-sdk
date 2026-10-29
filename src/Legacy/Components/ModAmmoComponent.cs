using ModComponent.Utilities;
using UnityEngine;

namespace ModComponent.Components
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modammocomponent")]
    [AddComponentMenu("")]
    public class ModAmmoComponent : ModGenericComponent
    {
        [Tooltip("Specifies the gun type this ammo is compatible with.")]
        public AmmoForGunType AmmoForGunType;
    }
}