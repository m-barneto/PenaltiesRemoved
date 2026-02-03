import { DependencyContainer } from "tsyringe";

import { jsonc } from "jsonc";
import path from "path";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { ItemHelper } from "@spt/helpers/ItemHelper";
import { BaseClasses } from "@spt/models/enums/BaseClasses";
import { FileSystemSync } from "@spt/utils/FileSystemSync";

// #region ModConfig 
interface ModConfig {
    Equipment: Equipment;
    Weapons: Weapons;
    Ammo: Ammo;
}

interface Weapons {
    Enabled: boolean;
    RemoveErgoPenalty: boolean;
    RemoveRecoilPenalty: boolean;
    RemoveAccuracyPenalty: boolean;
    RemoveVelocityPenalty: boolean;
    RemoveMuzzleOverheatingPenalty: boolean;
    RemoveDurabilityBurnPenalty: boolean;
}

interface Equipment {
    Enabled: boolean;
    RemoveErgoPenalty: boolean;
    RemoveTurnPenalty: boolean;
    RemoveMovePenalty: boolean;
    RemoveHearingPenalty: boolean;
    AudioDistortionModifier: number;
    AmbientNoiseOffsetAmount: number;
}

interface Ammo {
    Enabled: boolean;
    MisfireMultiplier: number;
    RemoveMuzzleOverheatingPenalty: boolean;
    RemoveDurabilityBurnPenalty: boolean;
    RemoveAmmoRecoilPenalty: boolean;
    RemoveAmmoAccuracyPenalty: boolean;
}
// #endregion

class PenaltiesRemoved implements IPostDBLoadMod {
    private modConfig: ModConfig;
    private logger: ILogger;

    public postDBLoad(container: DependencyContainer): void {
        const fileSystem = container.resolve<FileSystemSync>("FileSystemSync");
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const itemHelper = container.resolve<ItemHelper>("ItemHelper");

        this.logger = container.resolve<ILogger>("WinstonLogger");
        
        this.modConfig = jsonc.parse(fileSystem.read(path.resolve(__dirname, "../config/config.jsonc")));

        const items = databaseServer.getTables().templates.items;

        for (const itemId in items) {
            const item = items[itemId];
            if (this.modConfig.Weapons.Enabled && itemHelper.isOfBaseclass(itemId, BaseClasses.MOD)) {
                if (this.modConfig.Weapons.RemoveErgoPenalty && item._props.Ergonomics < 0) {
                    item._props.Ergonomics = 0;
                }
                if (this.modConfig.Weapons.RemoveRecoilPenalty && item._props.Recoil > 0) {
                    item._props.Recoil = 0;
                }
                if (this.modConfig.Weapons.RemoveAccuracyPenalty && item._props.Accuracy < 0) {
                    item._props.Accuracy = 0;
                }
                if (this.modConfig.Weapons.RemoveMuzzleOverheatingPenalty && item._props.HeatFactor > 1.0) {
                    item._props.HeatFactor = 1.0;
                }
                if (this.modConfig.Weapons.RemoveDurabilityBurnPenalty && item._props.DurabilityBurnModificator > 1.0) {
                    item._props.DurabilityBurnModificator = 1.0;
                }
            } else if (this.modConfig.Ammo.Enabled && itemHelper.isOfBaseclass(itemId, BaseClasses.AMMO)) {
                if (this.modConfig.Ammo.MisfireMultiplier && item._props.MisfireChance != 0) {
                    item._props.MisfireChance *= this.modConfig.Ammo.MisfireMultiplier;
                }
                if (this.modConfig.Ammo.RemoveAmmoAccuracyPenalty && item._props.ammoAccr < 0) {
                    item._props.ammoAccr = 0;
                }
                if (this.modConfig.Ammo.RemoveAmmoRecoilPenalty && item._props.ammoRec > 0) {
                    item._props.ammoRec = 0;
                }
                if (this.modConfig.Ammo.RemoveDurabilityBurnPenalty && item._props.DurabilityBurnModificator > 1.0) {
                    item._props.DurabilityBurnModificator = 1.0;
                }
                if (this.modConfig.Ammo.RemoveMuzzleOverheatingPenalty && item._props.HeatFactor > 1.0) {
                    item._props.HeatFactor = 1.0;
                }
            } else if (this.modConfig.Equipment.Enabled) {
                if (itemHelper.isOfBaseclass(itemId, BaseClasses.HEADPHONES)) {
                    if (this.modConfig.Equipment.AudioDistortionModifier != 1.0) {
                        item._props.Distortion *= this.modConfig.Equipment.AudioDistortionModifier;
                    }
                    if (this.modConfig.Equipment.AmbientNoiseOffsetAmount != 0) {
                        item._props.AmbientVolume += this.modConfig.Equipment.AmbientNoiseOffsetAmount;
                    }
                }
                if (itemHelper.isOfBaseclasses(itemId, [BaseClasses.ARMORED_EQUIPMENT, BaseClasses.VEST, BaseClasses.BACKPACK])) {
                    if (this.modConfig.Equipment.RemoveErgoPenalty) {
                        item._props.weaponErgonomicPenalty = 0;
                    }
                    if (this.modConfig.Equipment.RemoveTurnPenalty) {
                        item._props.mousePenalty = 0;
                    }
                    if (this.modConfig.Equipment.RemoveMovePenalty) {
                        item._props.speedPenaltyPercent = 0;
                    }
                    if (this.modConfig.Equipment.RemoveHearingPenalty) {
                        item._props.DeafStrength = "None";
                    }
                }
            }
        }
    }
}

module.exports = { mod: new PenaltiesRemoved() };
