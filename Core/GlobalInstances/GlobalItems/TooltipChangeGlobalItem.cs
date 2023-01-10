using CalamityMod.Items.SummonItems;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.GlobalItems
{
    public class TooltipChangeGlobalItem : GlobalItem
    {
        public static Dictionary<int, string> EnrageTooltipReplacements => new()
        {
            [ModContent.ItemType<DecapoditaSprout>()] = "Enrages outside of the Mushroom biome",
            [ItemID.WormFood] = "Enrages outside of the Corruption",
            [ItemID.BloodySpine] = "Enrages outside of the Crimson",
            [ModContent.ItemType<Teratoma>()] = "Enrages outside of the Corruption",
            [ModContent.ItemType<BloodyWormFood>()] = "Enrages outside of the Crimson",
            [ItemID.MechanicalEye] = null,
            [ItemID.MechanicalSkull] = null,
            [ItemID.MechanicalWorm] = null,
            [ItemID.ClothierVoodooDoll] = null,
            [ModContent.ItemType<ExoticPheromones>()] = null,
            [ModContent.ItemType<NecroplasmicBeacon>()] = "Enrages outside of the Underground",
        };

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            void replaceTooltipText(string tooltipIdentifier, string replacementText)
            {
                var tooltip = tooltips.FirstOrDefault(x => x.Name == tooltipIdentifier && x.Mod == "Terraria");

                // Stop if the tooltip could not be identified.
                if (tooltip is null)
                    return;

                tooltip.Text = replacementText;
            }

            void addTooltipLineAfterLine(string tooltipIdentifier, TooltipLine line)
            {
                int tooltipIndex = tooltips.FindIndex(x => x.Name == tooltipIdentifier && x.Mod == "Terraria");

                // Stop if the tooltip could not be identified.
                if (tooltipIndex == -1)
                    return;

                tooltips.Insert(tooltipIndex + 1, line);
            }

            if (item.type == ItemID.CelestialSigil)
            {
                string summoningText = "Summons the Moon Lord immediately\n" +
                        "Creates an arena at the player's position\n" +
                        "Not consumable.";
                replaceTooltipText("Tooltip0", summoningText);
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedShard>())
            {
                string summoningText = "Summons the Profaned Guardians when used in the profaned garden at the far right of the underworld";
                replaceTooltipText("Tooltip1", summoningText);

                // Remove the next line about an enrage condition.
                tooltips.RemoveAll(x => x.Name == "Tooltip2" && x.Mod == "Terraria");

                // Add a warning about the lack of a garden if it hasn't been generated yet.
                if (!WorldSaveSystem.HasGeneratedProfanedShrine)
                {
                    TooltipLine warningTooltip = new(Mod, "Warning",
                        "Your world does not currently have a Profaned Garden. Kill the Moon Lord again to generate it\n" +
                        "Be sure to grab the Hell schematic first if you do this, as the garden might destroy the lab");
                    warningTooltip.OverrideColor = Color.Orange;
                    addTooltipLineAfterLine("Tooltip1", warningTooltip);
                }
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ModContent.ItemType<ProfanedCore>())
            {
                string summoningText = "Summons Providence when used at the alter in the profaned temple at the far right of the underworld";
                replaceTooltipText("Tooltip1", summoningText);
            }

            if (InfernumMode.CanUseCustomAIs && item.type == ItemID.LihzahrdPowerCell)
            {
                string summoningText = "Summons Golem when used at the Lihzhard Altar\n" +
                    "Golem summons rectangular arena around the altar\n" +
                    "If the altar is inside of the temple solid tiles within the arena are broken";
                replaceTooltipText("Tooltip0", summoningText);
            }

            if (item.type == ModContent.ItemType<SandstormsCore>())
            {
                var tooltip0 = tooltips.FirstOrDefault(x => x.Name == "Tooltip0" && x.Mod == "Terraria");
                if (tooltip0 != null)
                {
                    if (WorldSaveSystem.HasGeneratedColosseumEntrance || SubworldSystem.IsActive<LostColosseum>())
                    {
                        tooltip0.Text = "Opens a portal to the Lost Colosseum";
                        tooltip0.OverrideColor = Color.Lerp(Color.Orange, Color.Yellow, 0.55f);
                    }

                    // Warn the player about not having a colosseum entrance if it hasn't been generated yet.
                    else
                    {
                        tooltip0.Text = "Your world does not currently have a Lost Gateway. Kill the Lunatic Cultist again to generate it.";
                        tooltip0.OverrideColor = Color.Orange;
                    }
                }
                tooltips.RemoveAll(x => x.Name == "Tooltip1" && x.Mod == "Terraria");
            }

            // Remove a bunch of "Enrages in XYZ" tooltips from base Calamity because people keep getting confused by it.
            if (InfernumMode.CanUseCustomAIs)
                EditEnrageTooltips(item, tooltips);
        }

        public static void EditEnrageTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Don't do anything if the item doesn't call for a tooltip replacement.
            if (!EnrageTooltipReplacements.TryGetValue(item.type, out string tooltipReplacement))
                return;

            // Don't do anything if the item has no enrage tooltip to reference.
            var enrageTooltip = tooltips.FirstOrDefault(x => x.Text.Contains("enrage", StringComparison.OrdinalIgnoreCase));
            if (enrageTooltip is null)
                return;

            int enrageTextStart = enrageTooltip.Text.IndexOf("enrage", StringComparison.OrdinalIgnoreCase);
            int enrageTextEnd = enrageTextStart;

            // Find where the current line terminates following the instance of the word 'enrage'.
            while (enrageTextEnd < enrageTooltip.Text.Length && enrageTooltip.Text[enrageTextEnd] != '\n')
                enrageTextEnd++;
            
            enrageTooltip.Text = enrageTooltip.Text.Remove(enrageTextStart, Math.Min(enrageTextEnd - enrageTextStart + 1, enrageTooltip.Text.Length));

            // If a replacement exists, insert it into the enrage text instead.
            if (tooltipReplacement is not null)
                enrageTooltip.Text = enrageTooltip.Text.Insert(enrageTextStart, tooltipReplacement);
        }
    }
}