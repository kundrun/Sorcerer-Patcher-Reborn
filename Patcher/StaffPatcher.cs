using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

using Noggog;

internal partial class Patcher
{
    private void PatchStaffRecords(
        out IDictionary<FormKey, StaffInfo> staffInfoLookup,
        out IEnumerable<StaffEnchantInfo> staffEnchantInfoList)
    {
        Console.WriteLine("Processing staves.");

        staffInfoLookup = new Dictionary<FormKey, StaffInfo>();
        var staffEnchantInfoLookup = new Dictionary<FormKey, StaffEnchantInfo>();

        var staves = _state.LoadOrder.PriorityOrder.Weapon().WinningOverrides()
            .WhereIf(x => _config.ModsToPatch.Contains(x.FormKey.ModKey),
                     () => _config.ModsToPatch.Count > 0)
            .Where(x => _config.ExcludedStaves.All(y => x.FormKey != y.FormKey))
            .Where(x => !ExcludedStaffMods.Contains(x.FormKey.ModKey))
            .Where(x => !ExcludedStaffKeys.Contains(x.FormKey))
            .Where(x => x.EditorID is { } editorId &&
                        !editorId.StartsWith("MAG_") &&
                        !editorId.Contains("Template"))
            .Where(x => !x.MajorFlags.HasFlag(Weapon.MajorFlag.NonPlayable))
            .Where(x => x.HasKeyword(FormKeys.KYWD.WeapTypeStaff))
            .Where(x => !x.HasAnyKeyword([
                FormKeys.KYWD.MagicDisallowEnchanting,
                FormKeys.KYWD.DaedricArtifact
            ]));

        foreach (var staff in staves)
        {
            if (!staffEnchantInfoLookup.TryGetValue(staff.ObjectEffect.FormKey, out var staffEnchantInfo))
            {
                staffEnchantInfo = GetStaffEnchantInfo(staff.ObjectEffect.FormKey);
                if (staffEnchantInfo == null)
                {
                    continue;
                }
                staffEnchantInfoLookup.Add(staff.ObjectEffect.FormKey, staffEnchantInfo);
            }
            staffInfoLookup.Add(staff.FormKey, new StaffInfo(staff.Name?.String ?? string.Empty, staffEnchantInfo.SkillLevel));

            Weapon? patchedStaff = null;

            var expectedEnchantAmount = StaffEnchantAmounts(staffEnchantInfo.SkillLevel);

            if (expectedEnchantAmount != staff.EnchantmentAmount)
            {
                patchedStaff ??= _state.PatchMod.Weapons.GetOrAddAsOverride(staff);
                patchedStaff.EnchantmentAmount = expectedEnchantAmount;
            }

            if (patchedStaff != null)
            {
                Console.WriteLine($">>> Patched staff {staff.EditorID}.");
            }
        }

        staffEnchantInfoList = staffEnchantInfoLookup.Values.ToArray();
    }

    private StaffEnchantInfo? GetStaffEnchantInfo(FormKey staffEnchantKey)
    {
        var staffEnchant = TryResolve<IObjectEffectGetter>(staffEnchantKey);

        var primaryEffect = staffEnchant?.Effects
            .Select(x => TryResolve(x.BaseEffect))
            .WhereNotNull()
            .Where(x => x.EditorID == null || !(x.EditorID.Contains("Dummy") || x.EditorID.Contains("XP")))
            .MaxBy(x => x.BaseCost);

        return primaryEffect != null
            ? new StaffEnchantInfo(staffEnchant!, primaryEffect.MinimumSkillLevel)
            : null;
    }

    private record StaffInfo(string Name, uint SkillLevel);

    private record StaffEnchantInfo(IObjectEffectGetter Enchant, uint SkillLevel);
}
