﻿using CalamityMod.NPCs;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Luminance.Core.Hooking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using static InfernumMode.Core.ILEditingStuff.HookManager;

namespace InfernumMode.Core.ILEditingStuff
{
    public class OverrideSystemHooks : ICustomDetourProvider
    {
        void ICustomDetourProvider.ModifyMethods()
        {
            HookHelper.ModifyMethodWithDetour(SetDefaultMethod, OverrideSystemHooks.SetDefaultDetourMethod);
            HookHelper.ModifyMethodWithDetour(FindFrameMethod, OverrideSystemHooks.FindFrameDetourMethod);
            HookHelper.ModifyMethodWithDetour(CalPreAIMethod, OverrideSystemHooks.CalPreAIDetourMethod);
            HookHelper.ModifyMethodWithDetour(CalGlobalNPCPredrawMethod, OverrideSystemHooks.CalGlobalNPCPredrawDetourMethod);
        }

        // Don't let Calamity's PreAI run on vanilla bosses to avoid ai conflicts.
        internal static bool CalPreAIDetourMethod(Orig_CalPreAIDelegate orig, CalamityGlobalNPC self, NPC npc)
        {
            if (InfernumMode.CanUseCustomAIs && NPCBehaviorOverride.BehaviorOverrideSet[npc.type].HasPreAI && npc.ModNPC == null)
                return false;

            return orig(self, npc);
        }

        // Sets Infernum's defaults last.
        internal static void SetDefaultDetourMethod(Orig_SetDefaultDelegate orig, NPC npc, bool createModNPC)
        {
            orig(npc, createModNPC);

            // This exists to only set them once at the end, as opposed to inside orig as well.
            GlobalNPCOverrides.ShouldSetDefaults = true;

            if (InfernumMode.CanUseCustomAIs)
                npc.Infernum().SetDefaults(npc);

            GlobalNPCOverrides.ShouldSetDefaults = false;
        }

        // Only run Infernum's findframe if it exists.
        internal static void FindFrameDetourMethod(Orig_FindFrameDelegate orig, NPC npc, int frameHeight)
        {
            if (InfernumMode.CanUseCustomAIs && NPCBehaviorOverride.BehaviorOverrideSet[npc.type].HasFindFrame && !npc.IsABestiaryIconDummy)
            {
                npc.Infernum().FindFrame(npc, frameHeight);
                return;
            }

            orig(npc, frameHeight);
        }

        internal static bool CalGlobalNPCPredrawDetourMethod(Orig_CalGlobalNPCPredrawMethod orig, CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.type == NPCID.GolemHeadFree)
                return false;

            return orig(self, npc, spriteBatch, screenPos, drawColor);
        }
    }
}
