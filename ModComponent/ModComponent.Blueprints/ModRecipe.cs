using ModComponent.SDK.Components;
using UnityEngine;

namespace ModComponent.Blueprints
{
    [HelpURL("https://github.com/Deaadman/ModComponentSDK/wiki/API#modrecipe")]
    public class ModRecipe : ModBlueprint
    {
        [Tooltip("The name of the recipe.")]
        public string RecipeName;

        [Tooltip("A description of the recipe.")]
        public string RecipeDescription;

        [Tooltip("Icon representing the recipe.")]
        public DataGearAsset RecipeIcon;

        [Range(0, 5)]
        [Tooltip("Minimum skill level required to use the recipe.")]
        public int RequiredSkillLevel;

        [Tooltip("Specific cooking pots allowed for this recipe.")]
        public DataGearAsset[] AllowedCookingPots;
    }
}