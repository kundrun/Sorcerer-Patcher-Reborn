using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

using Noggog;

internal partial class Patcher
{
    private void PatchScrollRecords(out IDictionary<FormKey, ScrollInfo> scrollInfoLookup)
    {
        Console.WriteLine("Processing scrolls.");

        scrollInfoLookup = new Dictionary<FormKey, ScrollInfo>();

        var scrolls = _state.LoadOrder.PriorityOrder.Scroll().WinningOverrides()
            .WhereIf(x => _config.ModsToPatch.Contains(x.FormKey.ModKey),
                     () => _config.ModsToPatch.Count > 0)
            .Where(x => _config.ExcludedScrolls.All(y => x.FormKey != y.FormKey))
            .Where(x => !ExcludedScrollMods.Contains(x.FormKey.ModKey))
            .Where(x => !ExcludedScrollKeys.Contains(x.FormKey))
            .Where(x => x.EditorID is { } editorId);

        foreach (var scroll in scrolls)
        {
            if (GetScrollInfo(scroll) is not { } scrollInfo)
            {
                continue;
            }
            scrollInfoLookup.Add(scroll.FormKey, scrollInfo);

            Scroll? patchedScroll = null;

            var expectedValue = ScrollValues(scrollInfo.SkillLevel);

            if (expectedValue != scroll.Value)
            {
                patchedScroll ??= _state.PatchMod.Scrolls.GetOrAddAsOverride(scroll);
                patchedScroll.Value = expectedValue;
            }

            var expectedSkill = ScrollSkills(scrollInfo.MagicSkill);

            if (!expectedSkill.IsNull && !scroll.HasKeyword(expectedSkill))
            {
                patchedScroll ??= _state.PatchMod.Scrolls.GetOrAddAsOverride(scroll);
                (patchedScroll.Keywords ??= []).Add(expectedSkill);
            }

            if (patchedScroll != null)
            {
                Console.WriteLine($">>> Patched scroll {scroll.EditorID}.");
            }
        }
    }

    private ScrollInfo? GetScrollInfo(IScrollGetter scroll)
    {
        var primaryEffect = scroll.Effects
            .Select(x => TryResolve(x.BaseEffect))
            .WhereNotNull()
            .Where(x => x.EditorID == null || !(x.EditorID.Contains("Dummy") || x.EditorID.Contains("XP")))
            .MaxBy(x => x.BaseCost);

        return primaryEffect != null
            ? new ScrollInfo(scroll.Name?.String ?? string.Empty, primaryEffect.MinimumSkillLevel, primaryEffect.MagicSkill)
            : null;
    }

    private record ScrollInfo(string Name, uint SkillLevel, ActorValue MagicSkill);
}
