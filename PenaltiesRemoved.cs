using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace PenaltiesRemoved;


public record ModMetadata : AbstractModMetadata {
    public override string ModGuid { get; init; } = "com.mattdokn.penaltiesremoved";
    public override string Name { get; init; } = "PenaltiesRemoved";
    public override string Author { get; init; } = "Mattdokn";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.3.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/m-barneto/PenaltiesRemoved";
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 100)]
public class PenaltiesRemoved(
    DatabaseServer databaseServer,
    ModHelper modHelper,
    ItemHelper itemHelper,
    ISptLogger<PenaltiesRemoved> logger)
    : IOnLoad {

    Dictionary<MongoId, TemplateItem>? itemDatabase;
    ModConfig? config;

    public Task OnLoad() {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        config = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.jsonc");
        if (config == null) {
            logger.Error("Unable to locate mod config file!");
            return Task.CompletedTask;
        }

        itemDatabase = databaseServer.GetTables().Templates.Items;

        foreach (var (itemId, item) in itemDatabase) {
            var props = item.Properties;
            if (props == null) continue;

            if (config.Equipment.Enabled) {
                if (itemHelper.IsOfBaseclass(itemId, BaseClasses.HEADPHONES)) {
                    if (config.Equipment.AudioDistortionModifier != 1.0) {
                        props.Distortion *= config.Equipment.AudioDistortionModifier;
                    }
                    if (config.Equipment.AmbientNoiseOffsetAmount != 0.0) {
                        props.AmbientVolume += config.Equipment.AmbientNoiseOffsetAmount;
                    }
                }
                if (itemHelper.IsOfBaseclasses(itemId, [BaseClasses.ARMORED_EQUIPMENT, BaseClasses.VEST, BaseClasses.BACKPACK])) {
                    if (config.Equipment.RemoveErgoPenalty) {
                        props.WeaponErgonomicPenalty = 0;
                    }
                    if (config.Equipment.RemoveTurnPenalty) {
                        props.MousePenalty = 0;
                    }
                    if (config.Equipment.RemoveMovePenalty) {
                        props.SpeedPenaltyPercent = 0;
                    }
                    if (config.Equipment.RemoveHearingPenalty) {
                        props.DeafStrength = "None";
                    }
                }
            }
            if (config.Weapons.Enabled) {
                if (itemHelper.IsOfBaseclass(itemId, BaseClasses.MOD)) {
                    if (config.Weapons.RemoveErgoPenalty && props.Ergonomics < 0.0) {
                        props.Ergonomics = 0.0;
                    }
                    if (config.Weapons.RemoveRecoilPenalty && props.Recoil > 0.0) {
                        props.Recoil = 0.0;
                    }
                    if (config.Weapons.RemoveAccuracyPenalty && props.Accuracy < 0.0) {
                        props.Accuracy = 0.0;
                    }
                    if (config.Weapons.RemoveMuzzleOverheatingPenalty && props.HeatFactor > 1.0) {
                        props.HeatFactor = 1.0;
                    }
                    if (config.Weapons.RemoveDurabilityBurnPenalty && props.DurabilityBurnModificator > 1.0) {
                        props.DurabilityBurnModificator = 1.0;
                    }
                    if (config.Weapons.RemoveVelocityPenalty && props.Velocity < 0.0) {
                        props.Velocity = 0.0;
                    }
                    if (config.Weapons.RemoveCoolingPenalty && props.CoolFactor < 1.0) {
                        props.CoolFactor = 1.0;
                    }
                }
            }
            if (config.Ammo.Enabled) {
                if (itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO)) {
                    if (config.Ammo.MisfireMultiplier != 1.0 && props.MisfireChance != 0.0) {
                        props.MisfireChance *= config.Ammo.MisfireMultiplier;
                    }
                    if (config.Ammo.RemoveAmmoAccuracyPenalty && props.AmmoAccr < 0.0) {
                        props.AmmoAccr = 0.0;
                    }
                    if (config.Ammo.RemoveAmmoRecoilPenalty && props.AmmoRec > 0.0) {
                        props.AmmoRec = 0.0;
                    }
                    if (config.Ammo.RemoveDurabilityBurnPenalty && props.DurabilityBurnModificator > 1.0) {
                        props.DurabilityBurnModificator = 1.0;
                    }
                    if (config.Ammo.RemoveMuzzleOverheatingPenalty && props.HeatFactor > 1.0) {
                        props.HeatFactor = 1.0;
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}

public record ModConfig {
    public EquipmentConfig Equipment { get; set; } = new();
    public WeaponsConfig Weapons { get; set; } = new();
    public AmmoConfig Ammo { get; set; } = new();
}

public record EquipmentConfig {
    public bool Enabled { get; set; }

    // Armor Penalties
    public bool RemoveErgoPenalty { get; set; }
    public bool RemoveTurnPenalty { get; set; }
    public bool RemoveMovePenalty { get; set; }

    // Below only applies to helmets.
    public bool RemoveHearingPenalty { get; set; }

    // Set this to 0 to completely remove audio distortion from headphones
    // Set to 1 to disable
    public double AudioDistortionModifier { get; set; }

    // Lower values (like -8 < -4) reduce ambient noises
    // Set to 0 to disable
    public double AmbientNoiseOffsetAmount { get; set; }
}

public record WeaponsConfig {
    public bool Enabled { get; set; }

    // Remove negative ergo penalties
    public bool RemoveErgoPenalty { get; set; }

    // Remove recoil penalties
    public bool RemoveRecoilPenalty { get; set; }

    // Remove accuracy penalties
    public bool RemoveAccuracyPenalty { get; set; }

    // Remove velocity penalties
    public bool RemoveVelocityPenalty { get; set; }

    // Silencers and muzzle attachments don't affect how much heat is generated when firing
    public bool RemoveMuzzleOverheatingPenalty { get; set; }

    // All attachments no longer change how quickly durability is reduced on the weapon
    public bool RemoveDurabilityBurnPenalty { get; set; }

    // Barrels no longer apply a cooling penalty
    public bool RemoveCoolingPenalty { get; set; }
}

public record AmmoConfig {
    public bool Enabled { get; set; }

    // Set to 0.0 to disable ammo misfiring penalties
    public double MisfireMultiplier { get; set; }

    // Ammos don't affect how much heat is generated when firing
    public bool RemoveMuzzleOverheatingPenalty { get; set; }

    // Ammos don't affect durability loss
    public bool RemoveDurabilityBurnPenalty { get; set; }

    // Ammos don't affect recoil
    public bool RemoveAmmoRecoilPenalty { get; set; }

    // Ammos don't affect accuracy
    public bool RemoveAmmoAccuracyPenalty { get; set; }
}