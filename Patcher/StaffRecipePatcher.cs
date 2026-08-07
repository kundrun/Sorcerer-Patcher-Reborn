using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

using Noggog;

internal partial class Patcher
{
    private void PatchStaffRecipeRecords(IDictionary<FormKey, StaffInfo> staffInfoLookup)
    {
        Console.WriteLine("Processing staff recipes.");

        var staffKeysWithExistingAltRecipes = _state.LoadOrder.PriorityOrder.ConstructibleObject().WinningOverrides()
            .Where(x => x.WorkbenchKeyword.FormKey == FormKeys.KYWD.StaffEnchanterWorkbenchSorcerer)
            .Select(x => x.CreatedObject.FormKey);

        var originalRecipes = _state.LoadOrder.PriorityOrder.ConstructibleObject().WinningOverrides()
            .Where(x => x.WorkbenchKeyword.FormKey == FormKeys.KYWD.StaffEnchanterWorkbenchSkyrim)
            .ExceptBy(staffKeysWithExistingAltRecipes, x => x.CreatedObject.FormKey);

        foreach (var originalRecipe in originalRecipes)
        {
            var staffKey = originalRecipe.CreatedObject.FormKey;
            if (staffInfoLookup.TryGetValue(staffKey, out var staffInfo))
            {
                CreateStaffRecipe(staffKey, staffInfo, originalRecipe);
            }
        }
    }

    private void CreateStaffRecipe(FormKey staffKey, StaffInfo staffInfo, IConstructibleObjectGetter originalRecipe)
    {
        var extractedId = staffInfo.Name
            .Replace("Staff of the ", "")
            .Replace("Staff of ",     "")
            .Replace(" ",             "")
            .Replace("'",             "");

        var recipeEditorId = "MAG_RecipeStaff" + extractedId + "Alt";

        if (_state.LinkCache.TryResolve<IConstructibleObjectGetter>(recipeEditorId, out var duplicateRecipe))
        {
            if (duplicateRecipe.CreatedObject.FormKey == staffKey)
            {
                Console.WriteLine($">>> Skipped staff recipe {recipeEditorId} because it already exists.");
                return;
            }

            recipeEditorId += "_" + staffKey.ModKey.MakeUniqueModIdentifier();

            if (_state.LinkCache.TryResolve<IConstructibleObjectGetter>(recipeEditorId, out _))
            {
                Console.WriteLine($">>> Skipped staff recipe {recipeEditorId} because it already exists.");
                return;
            }
        }

        var recipeDetails = StaffRecipeDetails(staffInfo.SkillLevel);

        var recipe = _state.PatchMod.ConstructibleObjects.AddNew(recipeEditorId);
        recipe.WorkbenchKeyword = FormKeys.KYWD.StaffEnchanterWorkbenchSorcerer.ToNullableLink<IKeywordGetter>();
        recipe.CreatedObject = staffKey.ToNullableLink<IConstructibleGetter>();
        recipe.CreatedObjectCount = originalRecipe.CreatedObjectCount;
        recipe.Conditions.AddRange(originalRecipe.Conditions.Select(x => x.DeepCopy()));
        if (originalRecipe.Items != null)
        {
            recipe.Items = originalRecipe.Items
                .Where(x => !x.Item.Item.FormKey.Equals(FormKeys.MISC.HeartStone))
                .Select(x => x.DeepCopy())
                .Append(new ContainerEntry
                {
                    Item = new ContainerItem
                    {
                        Item = recipeDetails.SoulGemType.ToLink<IItemGetter>(),
                        Count = recipeDetails.SoulGemQuantity
                    }
                })
                .ToExtendedList();
        }

        Console.WriteLine($">>> Created staff recipe {recipe.EditorID}.");
    }
}
