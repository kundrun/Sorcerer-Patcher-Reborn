using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

internal partial class Patcher
{
    private void PatchScrollRecipeRecords(IDictionary<FormKey, ScrollInfo> scrollInfoLookup)
    {
        Console.WriteLine("Processing scroll recipes.");

        foreach (var (scrollKey, scrollInfo) in scrollInfoLookup)
        {
            var extractedName = scrollInfo.Name.Replace("Scroll of the ", "").Replace("Scroll of ", "");
            var extractedId = extractedName.Replace(" ", "").Replace("'", "");

            var recipe = CreateScrollRecipe(scrollKey, scrollInfo, ref extractedId);
            var researchPerk = CreateScrollResearchPerk(extractedName, extractedId);
            var researchNotes = CreateScrollResearchNotes(extractedName, extractedId, scrollInfo, researchPerk);
            CreateScrollBreakdownRecipe(scrollKey, extractedId, researchPerk, researchNotes);
            if (recipe != null)
            {
                AttachScrollRecipeResearchPerk(recipe, researchPerk);
            }
        }
    }

    private IPerkGetter CreateScrollResearchPerk(string extractedName, string extractedId)
    {
        var perkEditorId = "MAG_ResearchPerk" + extractedId;

        if (_state.LinkCache.TryResolve<IPerkGetter>(perkEditorId, out var existingPerk))
        {
            Console.WriteLine($">>> Skipped scroll perk {perkEditorId} because it already exists.");
            return existingPerk;
        }

        var perk = _state.PatchMod.Perks.AddNew(perkEditorId);
        perk.Name = extractedName + " Research Perk";
        perk.Playable = true;
        perk.Hidden = true;
        perk.Level = 0;
        perk.NumRanks = 1;

        Console.WriteLine($">>> Created scroll perk {perk.EditorID}.");
        return perk;
    }

    private IBookGetter CreateScrollResearchNotes(
        string extractedName, string extractedId, ScrollInfo scrollInfo, IPerkGetter perk)
    {
        var notesEditorId = "MAG_ResearchNotes" + extractedId;

        if (_state.LinkCache.TryResolve<IBookGetter>(notesEditorId, out var existingNotes))
        {
            Console.WriteLine($">>> Skipped scroll research notes {notesEditorId} because it already exists.");
            return existingNotes;
        }

        var notes = _state.PatchMod.Books.AddNew(notesEditorId);
        notes.Name = "Research Notes: " + extractedName;
        notes.Description = $"Allows you to craft {scrollInfo.Name.Replace("Scroll of ", "scrolls of ")}.";
        notes.BookText = notes.Name;
        notes.Weight = 0;
        notes.Value = ScrollResearchNotesValues(scrollInfo.SkillLevel);
        notes.Keywords =
        [
            FormKeys.KYWD.ScrollResearchNotes.ToLinkGetter<IKeywordGetter>()
        ];
        notes.Model = new Model
        {
            File = @"Clutter\Common\Scroll05.nif"
        };
        notes.InventoryArt = FormKeys.STAT.ResearchItemScroll.ToNullableLink<IStaticGetter>();
        notes.PickUpSound = FormKeys.SNDR.NotePickUp.ToNullableLink<ISoundDescriptorGetter>();
        notes.VirtualMachineAdapter = new VirtualMachineAdapter
        {
            Scripts =
            [
                new ScriptEntry
                {
                    Name = "MAG_ResearchItem_Script",
                    Properties =
                    [
                        new ScriptObjectProperty
                        {
                            Name = "AttachedBook",
                            Object = notes.ToNullableLink()
                        },
                        new ScriptObjectProperty
                        {
                            Name = "CraftingPerk",
                            Object = perk.ToNullableLink()
                        }
                    ]
                }
            ]
        };

        Console.WriteLine($">>> Created scroll research notes {notes.EditorID}.");
        return notes;
    }

    private void CreateScrollBreakdownRecipe(FormKey scrollKey, string extractedId, IPerkGetter perk, IBookGetter notes)
    {
        var recipeEditorId = "MAG_BreakdownRecipeScroll" + extractedId;

        if (_state.LinkCache.TryResolve<IConstructibleObjectGetter>(recipeEditorId, out _))
        {
            Console.WriteLine($">>> Skipped scroll breakdown recipe {recipeEditorId} because it already exists.");
            return;
        }

        var recipe = _state.PatchMod.ConstructibleObjects.AddNew(recipeEditorId);
        recipe.WorkbenchKeyword = FormKeys.KYWD.ScrollEnchanterWorkbenchSorcerer.ToNullableLink<IKeywordGetter>();
        recipe.CreatedObject = notes.ToNullableLink();
        recipe.CreatedObjectCount = 1;
        recipe.Items =
        [
            new ContainerEntry
            {
                Item = new ContainerItem
                {
                    Item = scrollKey.ToLink<IItemGetter>(),
                    Count = 1
                }
            }
        ];

        var hasPerkConditionData = new HasPerkConditionData();
        hasPerkConditionData.Perk.Link.SetTo(perk);
        recipe.Conditions.Add(new ConditionFloat
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 0.0f,
            Data = hasPerkConditionData
        });

        var hasScrollsConditionData = new GetItemCountConditionData();
        hasScrollsConditionData.ItemOrList.Link.SetTo(scrollKey);
        recipe.Conditions.Add(new ConditionFloat
        {
            CompareOperator = CompareOperator.GreaterThanOrEqualTo,
            ComparisonValue = 1.0f,
            Data = hasScrollsConditionData
        });

        Console.WriteLine($">>> Created scroll breakdown recipe {recipe.EditorID}.");
    }

    private ConstructibleObject? CreateScrollRecipe(FormKey scrollKey, ScrollInfo scrollInfo, ref string extractedId)
    {
        var recipeEditorId = "MAG_RecipeScroll" + extractedId;

        if (_state.LinkCache.TryResolve<IConstructibleObjectGetter>(recipeEditorId, out var duplicateRecipe))
        {
            if (duplicateRecipe.CreatedObject.FormKey == scrollKey)
            {
                Console.WriteLine($">>> Skipped scroll recipe {recipeEditorId} because it already exists.");
                return null;
            }

            extractedId += "DUP";
            recipeEditorId = "MAG_RecipeScroll" + extractedId;

            if (_state.LinkCache.TryResolve<IConstructibleObjectGetter>(recipeEditorId, out _))
            {
                Console.WriteLine($">>> Skipped staff recipe {recipeEditorId} because it already exists.");
                return null;
            }
        }

        var recipeIngredients = ScrollRecipeDetails(scrollInfo.SkillLevel);

        var recipe = _state.PatchMod.ConstructibleObjects.AddNew(recipeEditorId);
        recipe.WorkbenchKeyword = FormKeys.KYWD.ScrollEnchanterWorkbenchSorcerer.ToNullableLink<IKeywordGetter>();
        recipe.CreatedObject = scrollKey.ToNullableLink<IConstructibleGetter>();
        recipe.CreatedObjectCount = recipeIngredients.ScrollQuantity;
        recipe.Items =
        [
            new ContainerEntry
            {
                Item = new ContainerItem
                {
                    Item = FormKeys.MISC.ScrollPaper.ToLink<IItemGetter>(),
                    Count = recipeIngredients.PaperQuantity
                }
            },
            new ContainerEntry
            {
                Item = new ContainerItem
                {
                    Item = FormKeys.MISC.EnchantedInk.ToLink<IItemGetter>(),
                    Count = recipeIngredients.InkQuantity
                }
            }
        ];

        Console.WriteLine($">>> Created scroll recipe {recipe.EditorID}.");
        return recipe;
    }

    private static void AttachScrollRecipeResearchPerk(ConstructibleObject recipe, IPerkGetter perk)
    {
        var hasPerkConditionData = new HasPerkConditionData();
        hasPerkConditionData.Perk.Link.SetTo(perk);
        recipe.Conditions.Add(new ConditionFloat
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 1.0f,
            Data = hasPerkConditionData
        });
    }
}
