using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

using static Utilities;

internal partial class Patcher
{
    private static readonly IReadOnlyDictionary<FormKey, uint> SoulGemValues = new Dictionary<FormKey, uint>
    {
        { FormKeys.SLGM.Petty, 10 },
        { FormKeys.SLGM.PettyFilled, 40 },
        { FormKeys.SLGM.Lesser, 25 },
        { FormKeys.SLGM.LesserFilledPetty, 55 },
        { FormKeys.SLGM.LesserFilled, 80 },
        { FormKeys.SLGM.Common, 45 },
        { FormKeys.SLGM.CommonFilledPetty, 75 },
        { FormKeys.SLGM.CommonFilledLesser, 100 },
        { FormKeys.SLGM.CommonFilled, 135 },
        { FormKeys.SLGM.Greater, 75 },
        { FormKeys.SLGM.GreaterFilledPetty, 105 },
        { FormKeys.SLGM.GreaterFilledLesser, 130 },
        { FormKeys.SLGM.GreaterFilledCommon, 165 },
        { FormKeys.SLGM.GreaterFilled, 265 },
        { FormKeys.SLGM.Grand, 160 },
        { FormKeys.SLGM.GrandFilledPetty, 190 },
        { FormKeys.SLGM.GrandFilledLesser, 215 },
        { FormKeys.SLGM.GrandFilledCommon, 250 },
        { FormKeys.SLGM.GrandFilledGreater, 350 },
        { FormKeys.SLGM.GrandFilled, 400 },
        { FormKeys.SLGM.Black, 240 },
        { FormKeys.SLGM.BlackFilledPetty, 270 },
        { FormKeys.SLGM.BlackFilledLesser, 295 },
        { FormKeys.SLGM.BlackFilledCommon, 330 },
        { FormKeys.SLGM.BlackFilledGreater, 430 },
        { FormKeys.SLGM.BlackFilledWhite, 480 },
        { FormKeys.SLGM.BlackFilled, 600 }
    };

    private static readonly IReadOnlySet<ModKey> ExcludedStaffMods = new HashSet<ModKey>
    {
        Dragonborn.ModKey
    };

    private static readonly IReadOnlySet<FormKey> ExcludedStaffKeys = new HashSet<FormKey>
    {
        Skyrim.Weapon.MQ303DragonPriestStaff.FormKey,                  // Nahkriin's Dragon Priest Staff
        Skyrim.Weapon.dunForelhostDragonPriestStaff.FormKey,           // Rahgot's Dragon Priest Staff
        Skyrim.Weapon.dunBlindCliffStaffReward.FormKey,                // Eye of Melka
        Skyrim.Weapon.dunCrystalDriftCaveStaff.FormKey,                // Gadnor's Staff of Charming
        Skyrim.Weapon.dunHalldirsCairnHalldirsStaff.FormKey,           // Halldir's Staff
        Skyrim.Weapon.dunValthumeDragonPriestStaff.FormKey,            // Hevnoraak's Staff
        Skyrim.Weapon.DA14SanguineRose.FormKey,                        // Sanguine Rose
        Skyrim.Weapon.DA16SkullofCorruption.FormKey,                   // Skull of Corruption
        Skyrim.Weapon.dunMarkarthWizardSpiderControlStaff.FormKey,     // Spider Control Rod
        Skyrim.Weapon.dunMarkarthWizardSpiderControlStaffFake.FormKey, // Spider Control Rod
        Skyrim.Weapon.FavorNelacarStaffFear.FormKey,                   // Staff of Arcane Authority
        Skyrim.Weapon.dunDarklightSilviaStaff.FormKey,                 // Staff of Hag's Wrath
        Skyrim.Weapon.dunSaarthalStaffJyrikStaff.FormKey,              // Staff of Jyrik Gauldurson
        Skyrim.Weapon.MG07StaffofMagnus.FormKey,                       // Staff of Magnus
        Skyrim.Weapon.MGRArniel02Staff.FormKey,                        // Staff of Tandil
        Skyrim.Weapon.DA15Wabbajack.FormKey,                           // Wabbajack
        Skyrim.Weapon.dunBluePalaceWabbajack.FormKey,                  // Wabbajack
        Skyrim.Weapon.dunRannveigSildsStaff.FormKey,                   // Sild's Staff
        Dawnguard.Weapon.DLC1LD_AetherialStaff.FormKey,                // Aetherial Staff
        Dawnguard.Weapon.DLC1RuunvaldStaff.FormKey,                    // Staff of Ruunvald
        MakeFormKey("ccbgssse040-advobgobs.esl",            0x000805), // Goblin Totem Staff
        MakeFormKey("ccbgssse067-daedinv.esm",              0x147D9F), // Staff of Ehlno Ede
        MakeFormKey("ccbgssse019-staffofsheogorath.esl",    0x000D62), // Staff of Sheogorath
        MakeFormKey("ECSS - Staff of Sheogorath Patch.esp", 0x000828), // Staff of Sheogorath
        MakeFormKey("BSHeartland.esm",                      0x086813), // Flamelight Spire
        MakeFormKey("BSHeartland.esm",                      0x070144), // Staff of Awesome Conflagration
        MakeFormKey("BSHeartland.esm",                      0x070558), // Rod of Potency
        MakeFormKey("BSHeartland.esm",                      0x0705F1), // Staff of Titan Summoning
        MakeFormKey("BSHeartland.esm",                      0x07062B), // Sceptre of Frosty Entombment
        MakeFormKey("BSHeartland.esm",                      0x0BF7E9), // Nelan Heroloth's Sheepstaff
        MakeFormKey("Wyrmstooth.esp",                       0x1CF784), // Dwarven Paralysis Rod
        MakeFormKey("Wyrmstooth.esp",                       0x3060D5), // Vulom's Staff
        MakeFormKey("Wyrmstooth.esp",                       0x78F3A2), // Staff of Malentis
        MakeFormKey("Wyrmstooth.esp",                       0x8F793C)  // Alka's Staff
    };

    private static ushort StaffEnchantAmounts(uint skillLevel) =>
        skillLevel switch
        {
            < 25  => 500,
            < 50  => 750,
            < 75  => 1500,
            < 100 => 3000,
            _     => 5000
        };

    private static ushort StaffEnchantCosts(uint skillLevel) =>
        skillLevel switch
        {
            < 25  => 20,
            < 50  => 30,
            < 75  => 60,
            < 100 => 120,
            _     => 250
        };

    private static FormKey StaffEnchantMarkerEffects(IObjectEffectGetter staffEnchant) =>
        staffEnchant switch
        {
            { CastType: CastType.Concentration, TargetType: TargetType.Aimed }       => FormKeys.MGEF.StaffEnchConcAimed,
            { CastType: CastType.Concentration, TargetType: TargetType.TargetActor } => FormKeys.MGEF.StaffEnchConcActor,
            { CastType: CastType.FireAndForget, TargetType: TargetType.Aimed }       => FormKeys.MGEF.StaffEnchFFAimed,
            { CastType: CastType.FireAndForget, TargetType: TargetType.TargetActor } => FormKeys.MGEF.StaffEnchFFActor,
            { CastType: CastType.FireAndForget, TargetType: TargetType.TargetLocation } =>
                staffEnchant.EditorID?.Contains("Rune") != true
                    ? FormKeys.MGEF.StaffEnchFFLocation
                    : FormKeys.MGEF.StaffEnchFFLocationRune,
            _ => FormKey.Null
        };

    private static (FormKey SoulGemType, int SoulGemQuantity) StaffRecipeDetails(uint skillLevel) =>
        skillLevel switch
        {
            < 25  => (FormKeys.SLGM.CommonFilled, 1),
            < 50  => (FormKeys.SLGM.GreaterFilled, 1),
            < 75  => (FormKeys.SLGM.GrandFilled, 1),
            < 100 => (FormKeys.SLGM.GrandFilled, 2),
            _     => (FormKeys.SLGM.GrandFilled, 3)
        };

    private static readonly IReadOnlySet<ModKey> ExcludedScrollMods = new HashSet<ModKey>
    {
        Dawnguard.ModKey,
        Dragonborn.ModKey,
        ModKey.FromNameAndExtension("Arachnomancy.esp"),
        ModKey.FromNameAndExtension("ShowRaceMenuAlternative.esp")
    };

    private static readonly IReadOnlySet<FormKey> ExcludedScrollKeys = new HashSet<FormKey>
    {
        Skyrim.Scroll.MGRJzargo1Scroll.FormKey,
        Skyrim.Scroll.MGR21ScrollDestruction.FormKey,
        Skyrim.Scroll.MGR21ScrollIllusion.FormKey,
        Skyrim.Scroll.MGR21ScrollAlteration.FormKey,
        Skyrim.Scroll.MGR21ScrollRestoration.FormKey,
        Skyrim.Scroll.MGR21ScrollConjuration.FormKey,
        Skyrim.Scroll.MGR21ScrollMagicka.FormKey,
        // Skill calculation for these items is broken - Mysticism uses MGEF costs in an unusual way.
        Skyrim.Scroll.RallyScroll.FormKey,
        Skyrim.Scroll.CallToArmsScroll.FormKey
    };

    private static uint ScrollValues(uint skillLevel) =>
        skillLevel switch
        {
            < 25  => 15,
            < 50  => 30,
            < 75  => 55,
            < 100 => 90,
            _     => 160
        };

    private static FormKey ScrollSkills(ActorValue magicSkill) =>
        magicSkill switch
        {
            // Injected records from Mysticism.esp into Update.esm
            ActorValue.Alteration  => Update.ModKey.MakeFormKey(0xADA151),
            ActorValue.Conjuration => Update.ModKey.MakeFormKey(0xADA152),
            ActorValue.Destruction => Update.ModKey.MakeFormKey(0xADA153),
            ActorValue.Illusion    => Update.ModKey.MakeFormKey(0xADA154),
            ActorValue.Restoration => Update.ModKey.MakeFormKey(0xADA155),
            _                      => FormKey.Null
        };

    private static uint ScrollResearchNotesValues(uint skillLevel) =>
        skillLevel switch
        {
            < 25  => 100,
            < 50  => 200,
            < 75  => 400,
            < 100 => 600,
            _     => 1000
        };

    private static (int PaperQuantity, int InkQuantity, ushort ScrollQuantity) ScrollRecipeDetails(uint skillLevel) =>
        skillLevel switch
        {
            < 25  => (5, 2, 5),
            < 50  => (4, 3, 4),
            < 75  => (3, 4, 3),
            < 100 => (2, 5, 2),
            _     => (2, 8, 2)
        };
}
