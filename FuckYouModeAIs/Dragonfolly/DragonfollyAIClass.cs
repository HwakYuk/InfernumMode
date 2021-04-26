﻿using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.Twins;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Dragonfolly
{
	public class DragonfollyAIClass
    {
        #region Enumerations
        public enum DragonfollyAttackType
        {
            SpawnEffects,
            FeatherSpreadRelease,
            OrdinaryCharge,
            FakeoutCharge,
            ThunderCharge,
            SummonSwarmers,
            NormalLightningAura,
            PlasmaBursts,
            LightningSupercharge
        }

        public enum DragonfollyFrameDrawingType
        {
            FlapWings,
            Screm
        }
        #endregion

        #region AI

        #region Main Boss

        public const int TransitionTime = ScreamTime + 15;

		[OverrideAppliesTo("Bumblefuck", typeof(DragonfollyAIClass), "DragonfollyAI", EntityOverrideContext.NPCAI)]
        public static bool DragonfollyAI(NPC npc)
		{
            DetermineTarget(npc, out bool despawning);
            if (despawning)
                return false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < 0.75f;
            bool phase3 = lifeRatio < 0.25f;
            Player target = Main.player[npc.target];
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float chargeType = ref npc.ai[2];
            ref float lastChargeType = ref npc.ai[3];
            ref float frameType = ref npc.localAI[0];
            ref float flapRate = ref npc.localAI[1];
            ref float fadeToRed = ref npc.localAI[2];
            ref float previousPhase = ref npc.Infernum().ExtraAI[5];
            ref float phaseTransitionCountdown = ref npc.Infernum().ExtraAI[6];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[7];
            ref float backgroundFadeToRed = ref npc.Infernum().ExtraAI[8];
            ref float phase2TransitionCountdown = ref npc.Infernum().ExtraAI[9];

            // Go to the next phases.
            if (previousPhase == 0f && phase2)
			{
                phaseTransitionCountdown = TransitionTime;
                phase2TransitionCountdown = 600f;
                previousPhase = 1f;

                // Piss off any remaining swarmers.
                int swarmerType = ModContent.NPCType<Bumblefuck2>();
                for (int i = 0; i < Main.maxNPCs; i++)
				{
                    if (!Main.npc[i].active || Main.npc[i].type != swarmerType)
                        continue;

                    Main.npc[i].ai[0] = 3f;
                    Main.npc[i].ai[1] = 0f;
                    Main.npc[i].ai[2] = 0f;
                    Main.npc[i].ai[3] = 0f;
                    Main.npc[i].netUpdate = true;
                }

                npc.netUpdate = true;
            }
            if (previousPhase == 1f && phase3)
            {
                chargeCounter = 0f;
                phaseTransitionCountdown = TransitionTime;
                previousPhase = 2f;

                // Piss off any remaining swarmers.
                int swarmerType = ModContent.NPCType<Bumblefuck2>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].type != swarmerType)
                        continue;

                    Main.npc[i].ai[0] = 3f;
                    Main.npc[i].ai[1] = 0f;
                    Main.npc[i].ai[2] = 0f;
                    Main.npc[i].ai[3] = 2f;
                    Main.npc[i].netUpdate = true;
                }

                npc.netUpdate = true;
            }

            if (phase2TransitionCountdown > 0)
			{
                npc.Calamity().DR = MathHelper.SmoothStep(0.1f, 0.35f, phase2TransitionCountdown / 600f);
                npc.defense = (int)MathHelper.SmoothStep(npc.defDefense, 100f, phase2TransitionCountdown / 600f);
                if (phase2)
                    npc.damage = (int)MathHelper.SmoothStep(npc.defDamage * 1.1f, npc.defDamage * 1.4f, phase2TransitionCountdown / 600f);
            }

            if (phaseTransitionCountdown > 0)
            {
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.185f);
                npc.velocity *= 0.95f;
                npc.rotation *= 0.95f;
                frameType = (int)DragonfollyFrameDrawingType.Screm;
                phaseTransitionCountdown--;
                return false;
            }

            npc.damage = npc.defDamage;

            switch ((DragonfollyAttackType)(int)attackType)
			{
                case DragonfollyAttackType.SpawnEffects:
                    DoAttack_SpawnEffects(npc, target, attackTimer, ref fadeToRed, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.OrdinaryCharge:
                case DragonfollyAttackType.FakeoutCharge:
                case DragonfollyAttackType.ThunderCharge:
                    DoAttack_Charge(npc, target, (DragonfollyAttackType)(int)attackType, phase2, phase3, ref fadeToRed, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.SummonSwarmers:
                    DoAttack_SummonSwarmers(npc, target, phase2, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.NormalLightningAura:
                    DoAttack_CreateNormalLightningAura(npc, target, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.FeatherSpreadRelease:
                    DoAttack_ReleaseSpreadOfFeathers(npc, target, ref attackTimer, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.PlasmaBursts:
                    DoAttack_ReleasePlasmaBursts(npc, target, ref attackTimer, ref fadeToRed, ref frameType, ref flapRate);
                    break;
                case DragonfollyAttackType.LightningSupercharge:
                    DoAttack_LightningSupercharge(npc, target, ref attackTimer, ref frameType, ref flapRate);
                    break;
            }

            // Cause the background red to wane when not doing a supercharge.
            if ((DragonfollyAttackType)(int)attackType != DragonfollyAttackType.LightningSupercharge)
			{
                backgroundFadeToRed *= 0.98f;
                backgroundFadeToRed = MathHelper.Clamp(backgroundFadeToRed - 0.025f, 0f, 1f);
			}

            attackTimer++;
            return false;
		}

        internal static void DetermineTarget(NPC npc, out bool despawning)
		{
            despawning = false;

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found, fly away.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.velocity.Y -= 0.5f;
                    npc.rotation = npc.rotation.AngleLerp(0f, 0.25f);
                    if (npc.timeLeft > 240)
                        npc.timeLeft = 240;
                    despawning = true;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;
        }

        internal static void GoToNextAttackState(NPC npc)
        {
            npc.alpha = 0;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float lastChargeType = ref npc.ai[3];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[7];
            DragonfollyAttackType currentAttackState = (DragonfollyAttackType)(int)npc.ai[0];
            DragonfollyAttackType newAttackState = DragonfollyAttackType.OrdinaryCharge;
            switch (currentAttackState)
            {
                case DragonfollyAttackType.SpawnEffects:
                    newAttackState = DragonfollyAttackType.OrdinaryCharge;
                    break;
                case DragonfollyAttackType.OrdinaryCharge:
                case DragonfollyAttackType.FakeoutCharge:
                case DragonfollyAttackType.ThunderCharge:
                    newAttackState = Main.rand.NextBool(2) ? DragonfollyAttackType.NormalLightningAura : DragonfollyAttackType.SummonSwarmers;
                    break;
                case DragonfollyAttackType.SummonSwarmers:
                    newAttackState = Main.rand.NextBool(2) ? DragonfollyAttackType.FeatherSpreadRelease : DragonfollyAttackType.NormalLightningAura;
                    break;
                case DragonfollyAttackType.NormalLightningAura:
                    newAttackState = Main.rand.NextBool(2) ? DragonfollyAttackType.OrdinaryCharge : DragonfollyAttackType.SummonSwarmers;
                    if (lifeRatio < 0.7f)
                        newAttackState = DragonfollyAttackType.PlasmaBursts;
                    break;
                case DragonfollyAttackType.FeatherSpreadRelease:
                    newAttackState = DragonfollyAttackType.NormalLightningAura;
                    break;
                case DragonfollyAttackType.PlasmaBursts:
                    newAttackState = DragonfollyAttackType.OrdinaryCharge;
                    break;
                case DragonfollyAttackType.LightningSupercharge:
                    newAttackState = DragonfollyAttackType.FeatherSpreadRelease;
                    break;
            }

            if (lifeRatio < 0.2f && Main.rand.NextBool(2))
                newAttackState = DragonfollyAttackType.OrdinaryCharge;

            if (newAttackState == DragonfollyAttackType.OrdinaryCharge)
            {
                int chargeOverrideRate = (int)Math.Round(MathHelper.Lerp(8, 3, Utils.InverseLerp(0.25f, 0.05f, npc.life / (float)npc.lifeMax)));
                chargeCounter++;
                if (npc.life < npc.lifeMax * 0.25f && chargeCounter > chargeOverrideRate)
                {
                    newAttackState = DragonfollyAttackType.LightningSupercharge;
                    chargeCounter = 0f;
                }
                else
                {
                    do
                        newAttackState = Utils.SelectRandom(Main.rand, DragonfollyAttackType.OrdinaryCharge, DragonfollyAttackType.FakeoutCharge, DragonfollyAttackType.ThunderCharge);
                    while (newAttackState == (DragonfollyAttackType)(int)lastChargeType);
                }

                lastChargeType = (int)newAttackState;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.noTileCollide = true;
            npc.netUpdate = true;
		}

		#region Specific Attacks
        internal static void DoAttack_SpawnEffects(NPC npc, Player target, float attackTimer, ref float fadeToRed, ref float frameType, ref float flapRate)
		{
            int chargeDelay = 30;
            if (attackTimer <= 1f)
                npc.Opacity = 0f;

            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = (int)Utils.Clamp(8f - npc.velocity.Length() * 0.125f, 4f, 8f);

            if (attackTimer <= 45f)
            {
                npc.Opacity = Utils.InverseLerp(25f, 45f, attackTimer, true);
                npc.Center = Vector2.SmoothStep(npc.Center, target.Center - Vector2.UnitY * 1350f, (float)Math.Pow(attackTimer / 45f, 3D));
                npc.spriteDirection = (npc.Center.X - target.Center.X < 0).ToDirectionInt();
                flapRate = 7;
            }

            // Release a bunch of feathers that aim towards the player.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 45f)
			{
                for (float offsetAngle = -1.21f; offsetAngle <= 1.21f; offsetAngle += 0.1f)
                {
                    Vector2 spawnPosition = target.Center - Vector2.UnitY.RotatedBy(offsetAngle) * 1500f;
                    Vector2 shootDirection = target.DirectionFrom(spawnPosition) * 0.001f;
                    Utilities.NewProjectileBetter(spawnPosition, shootDirection, ModContent.ProjectileType<RedLightningSnipeFeather>(), 300, 0f);
                }
            }
            if (attackTimer >= 150f)
			{
                // Teleport to a side of the player.
                if (attackTimer == 150f)
				{
                    npc.Center = target.Center + Vector2.UnitX * (npc.Center.X > target.Center.X).ToDirectionInt() * 1750f;
                    npc.netUpdate = true;
				}

                // Charge.
                if (attackTimer == 150f + chargeDelay)
				{
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 33f;
                    npc.netUpdate = true;
				}

                // And do specific things after charging.
                if (attackTimer >= 150f + chargeDelay)
                {
                    npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;

                    // Fade to red and back depending on how much time is left in the charge.
                    fadeToRed = MathHelper.Lerp(fadeToRed, attackTimer >= 205f + chargeDelay ? 0f : 1f, 0.15f);

                    // Release lightning clouds from time to time while charging.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 7f == 6f)
                        Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<LightningCloud>(), 0, 0f);
                }
                if (attackTimer >= 230f + chargeDelay)
                {
                    npc.velocity *= 0.96f;
                    if (attackTimer >= 305f + chargeDelay)
                        GoToNextAttackState(npc);
                }

                npc.alpha = Utils.Clamp(npc.alpha - 25, 0, 255);
			}

            if (Math.Abs(npc.velocity.X) > 0.8f)
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.noTileCollide = true;
        }
        internal static void DoAttack_Charge(NPC npc, Player target, DragonfollyAttackType chargeType, bool phase2, bool phase3, ref float fadeToRed, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.noTileCollide = true;
            float horizontalOffset;
            switch (chargeType)
            {
                default:
                case DragonfollyAttackType.OrdinaryCharge:
                    horizontalOffset = 660f;
                    break;
                case DragonfollyAttackType.FakeoutCharge:
                    horizontalOffset = 870f;
                    break;
                case DragonfollyAttackType.ThunderCharge:
                    horizontalOffset = 1060f;
                    break;
            }

            ref float chargeState = ref npc.Infernum().ExtraAI[0];
            ref float accumulatedSpeed = ref npc.Infernum().ExtraAI[1];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[2];
            ref float hasDoneFakeoutFlag = ref npc.Infernum().ExtraAI[3];

            // Phase 2-3 exclusive.
            int totalRedirects = phase3 ? 2 : 1;
            ref float redirectCounter = ref npc.Infernum().ExtraAI[4];

            // Line up for charge.
            if (chargeState == 0f)
			{
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                float verticalOffsetLeniance = 65f;
                float flySpeed = 18.5f + accumulatedSpeed;
                float flyInertia = 4f;
                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
				{
                    chargeState = 1f;
                    accumulatedSpeed = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
				}

                // Become more and more fast the more time has passed.
                // (Why does this comment sound funny to me?)
                accumulatedSpeed += 0.035f;
            }

            // Prepare for the charge.
            else if (chargeState == 1f)
			{
                int chargeDelay = chargeType == DragonfollyAttackType.ThunderCharge ? 45 : 20;
                if (chargeType == DragonfollyAttackType.OrdinaryCharge && phase2 && redirectCounter > 0f)
                    chargeDelay = 6;

                float flySpeed = 25f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity * (chargeType == DragonfollyAttackType.ThunderCharge ? 0.5f : 1f)) / flyInertia;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                if (attackTimer >= chargeDelay)
				{
                    attackTimer = 0f;
                    chargeState = 2f;
                    npc.velocity = chargeVelocity;
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    chargeDirection = npc.spriteDirection;
                    npc.netUpdate = true;

                    // Make a diving sound.
                    Main.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);

                    // Release some feathers into the air.
                    for (int i = 0; i < Main.rand.Next(4, 8 + 1); i++)
					{
                        Vector2 featherVelocity = Main.rand.NextVector2Circular(12f, 3f);
                        featherVelocity.Y = -Math.Abs(featherVelocity.Y);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(npc.Center + Main.rand.NextVector2CircularEdge(50f, 50f), featherVelocity, ModContent.ProjectileType<FollyFeather>(), 0, 0f);
					}

                    // If in phase 2 and doing a lightning attack, release an aura from the mouth that goes towards the player.
                    if (chargeType == DragonfollyAttackType.ThunderCharge && phase2)
                    {
                        SoundEffectInstance sound = Main.PlaySound(SoundID.DD2_BetsyScream, npc.Center);
                        if (sound != null)
                            sound.Pitch = 0.25f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 36f);
                            if (phase3)
                            {
                                Vector2 baseShootVelocity = npc.DirectionTo(mouthPosition) * 7f;
                                Projectile.NewProjectile(mouthPosition, baseShootVelocity.RotatedBy(-0.36f), ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                                Projectile.NewProjectile(mouthPosition, baseShootVelocity.RotatedBy(0.36f), ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                            }
                            else
                                Projectile.NewProjectile(mouthPosition, Vector2.Zero, ModContent.ProjectileType<BirbThunderAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                        }
                    }
                }
            }

            // Do the actual charge.
            else if (chargeState == 2f)
			{
                float horizontalSpeed = 25f;

                // Fly faster than usual after a fakeout.
                if (hasDoneFakeoutFlag == 1f && chargeType == DragonfollyAttackType.FakeoutCharge)
                    horizontalSpeed += 6f;

                accumulatedSpeed += phase3 ? 0.08f : 0.04f;
                npc.velocity.X = chargeDirection * (horizontalSpeed + accumulatedSpeed);

                float offsetRemoval = chargeType == DragonfollyAttackType.ThunderCharge ? -80f : 210f;
                bool farEnoughAwayFromPlayer = chargeDirection > 0f && npc.Center.X > target.Center.X + (horizontalOffset - offsetRemoval);
                farEnoughAwayFromPlayer |= chargeDirection < 0f && npc.Center.X < target.Center.X - (horizontalOffset - offsetRemoval);

                if (farEnoughAwayFromPlayer)
                {
                    if (redirectCounter < totalRedirects && chargeType == DragonfollyAttackType.OrdinaryCharge)
					{
                        chargeState = 1f;
                        attackTimer = 0f;
                        redirectCounter++;
                        npc.netUpdate = true;
					}
                    GoToNextAttackState(npc);
                }

                // Release lightning clouds from time to time while charging if doing a lightning charge.
                int cloudSpawnRate = (int)MathHelper.Lerp(16f, 9f, 1f - npc.life / (float)npc.lifeMax);
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % cloudSpawnRate == cloudSpawnRate - 1f && chargeType == DragonfollyAttackType.ThunderCharge)
                    Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<LightningCloud>(), 0, 0f);

                if (hasDoneFakeoutFlag == 0f && chargeType == DragonfollyAttackType.FakeoutCharge)
				{
                    // Fade out for the fake out.
                    if (npc.alpha < 255)
                    {
                        // Turn red as a telegraph for a short moment.
                        fadeToRed = (float)Math.Sin(Utils.InverseLerp(0f, 10f, attackTimer, true) * MathHelper.Pi);

                        npc.alpha = Utils.Clamp(npc.alpha + 14, 0, 255);
                        if (npc.Hitbox.Intersects(target.Hitbox))
                            target.AddBuff(BuffID.Confused, 150);
                        npc.damage = 0;
                    }

                    // After completely fading out, teleport to the other side of the player,
                    else
                    {
                        fadeToRed = 0f;
                        chargeState = 1f;
                        attackTimer = 0f;
                        hasDoneFakeoutFlag = 1f;
                        npc.Center = target.Center + Vector2.UnitX * horizontalOffset * (target.Center.X < npc.Center.X).ToDirectionInt();

                        // Charge diagonally in phase 2.
                        if (phase2)
                            npc.position.Y -= 475f;

                        npc.netUpdate = true;
                    }
                }

                // Rapidly fade in and slow down a bit if doing the fakeout charge.
                if (hasDoneFakeoutFlag == 1f && chargeType == DragonfollyAttackType.FakeoutCharge)
                {
                    npc.alpha = Utils.Clamp(npc.alpha - 45, 0, 255);
                    npc.velocity *= 0.985f;
                }
            }

            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = (int)Utils.Clamp(8f - npc.velocity.Length() * 0.125f, 4f, 8f);
            npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;
        }
        
        internal static void DoAttack_SummonSwarmers(NPC npc, Player target, bool phase2, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.rotation = npc.rotation.AngleLerp(0f, 0.125f);
            npc.rotation = npc.rotation.AngleTowards(0f, 0.125f);
            npc.noTileCollide = true;

            int maxSwarmersAtOnce = phase2 ? 2 : 1;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float originalSwamerCount = ref npc.Infernum().ExtraAI[1];
            if (originalSwamerCount == 0f)
            {
                originalSwamerCount = NPC.CountNPCS(ModContent.NPCType<Bumblefuck2>());

                // Don't bother doing this attack if the swarmer count is already at the limit.
                if (originalSwamerCount >= maxSwarmersAtOnce)
                    GoToNextAttackState(npc);

                npc.netUpdate = true;
            }

            // Fly near the target.
            if (attackState == 0f)
            {
                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 5f;

                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 200f, -Vector2.UnitY) * 21f, 0.15f);
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // If somewhat close to the target or enough time has passed, begin summoning swarmers.
                if (npc.WithinRange(target.Center, 600f) || attackTimer >= 180f)
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Scream and summon swarmers.
            else if (attackState == 1f)
            {
                frameType = (int)DragonfollyFrameDrawingType.Screm;

                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.04f);
                if (npc.velocity.Length() < 0.8f)
                    npc.velocity = Vector2.Zero;

                // Create swarmers around the dragonfolly.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == ScreamSoundDelay)
                {
                    int remainingPossibleSummons = maxSwarmersAtOnce - (int)originalSwamerCount;
                    int totalSwarmersToSummon = 1;
                    if (!phase2 && remainingPossibleSummons >= 2 && Main.rand.NextBool(2))
                        totalSwarmersToSummon = 2;

                    for (int i = 0; i < totalSwarmersToSummon; i++)
                    {
                        Vector2 potentialSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * new Vector2(100f, 85f) * Main.rand.NextFloat(0.6f, 1f);

                        // Ensure that the spawn position is not near the target, to prevent potentially unfair hits.
                        if (!target.WithinRange(potentialSpawnPosition, 160f))
                        {
                            int swarmer = NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y, ModContent.NPCType<Bumblefuck2>(), npc.whoAmI);
                            Main.npc[swarmer].ai[3] = phase2.ToInt();
                            Main.npc[swarmer].velocity = Vector2.UnitY * -12f;
                        }
                    }
                }

                if (attackTimer > ScreamTime + 8f)
                {
                    frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                    flapRate = 5f;
                }

                if (attackTimer >= ScreamTime + 25f)
                    GoToNextAttackState(npc);
            }
        }

        internal static void DoAttack_CreateNormalLightningAura(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            npc.velocity *= 0.96f;
            npc.rotation *= 0.95f;

            int shootDelay = 75;

            if (attackTimer >= shootDelay - ScreamSoundDelay)
                frameType = (int)DragonfollyFrameDrawingType.Screm;

            // Terminate the attack early if an aura or flare already exists.
            if (attackTimer < shootDelay)
            {
                if (Utilities.AnyProjectiles(ModContent.ProjectileType<BirbAuraFlare>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<BirbAura>()))
                    GoToNextAttackState(npc);
                npc.spriteDirection = (npc.SafeDirectionTo(target.Center).X > 0f).ToDirectionInt();
            }

            if (attackTimer == shootDelay)
            {
                Main.PlaySound(SoundID.DD2_BetsyFireballShot, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 36f);
                    Projectile.NewProjectile(mouthPosition, Vector2.Zero, ModContent.ProjectileType<BirbAuraFlare>(), 0, 0f, Main.myPlayer, 2f, npc.target + 1);
                }
            }

            if (attackTimer > shootDelay + 12f)
            {
                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 5f;
            }

            if (attackTimer == shootDelay + 35f)
                GoToNextAttackState(npc);
        }

        internal static void DoAttack_ReleaseSpreadOfFeathers(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float flapRate)
        {
            int totalWaves = (int)MathHelper.Lerp(1f, 3.5f, 1f - npc.life / (float)npc.lifeMax);
            int flyTime = 60;
            int waveDelay = 30;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float waveCounter = ref npc.Infernum().ExtraAI[0];
            ref float screamTimer = ref npc.localAI[3];

            if (attackTimer < flyTime)
            {
                float flyInertia = 9f;
                float flySpeed = 16f;

                if (!npc.WithinRange(target.Center, 400f))
                    npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(target.Center) * flySpeed) / flyInertia;
            }
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

            frameType = (int)DragonfollyFrameDrawingType.FlapWings;
            flapRate = 6f;

            if (attackTimer >= flyTime)
            {
                if (attackTimer <= flyTime + 65f)
                {
                    if (screamTimer < 30f)
                        screamTimer = 30f;
                    if (waveCounter > 0f)
                        screamTimer++;
                    frameType = (int)DragonfollyFrameDrawingType.Screm;
                }

                if (waveCounter > 0f)
                    attackTimer++;
            }

            if (attackTimer == flyTime + waveDelay)
            {
                // Release a burst of feathers into the air.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int featherType = ModContent.ProjectileType<RedLightningRedirectingFeather>();
                    int totalFeathers = (int)MathHelper.Lerp(8, 26, 1f - lifeRatio);
                    for (int i = 0; i < totalFeathers; i++)
                    {
                        Vector2 shootVelocity = Vector2.UnitY.RotatedBy(MathHelper.Lerp(-1.08f, 1.08f, i / (float)totalFeathers)) * -8f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 9f, shootVelocity, featherType, 240, 0f);
                    }
                }

                // As well as a burst of dust.
                if (!Main.dedServ)
                {
                    for (float speed = 3f; speed <= 10f; speed += 1.2f)
                    {
                        float lifePersistance = Main.rand.NextFloat(1.5f, 2f);
                        for (int i = 0; i < 60; i++)
                        {
                            Dust energy = Dust.NewDustPerfect(npc.Center, 267);
                            energy.velocity = (MathHelper.TwoPi * i / 60f).ToRotationVector2() * speed;
                            energy.noGravity = true;
                            energy.color = Main.hslToRgb(Main.rand.NextFloat(0f, 0.08f), 0.85f, 0.6f);
                            energy.fadeIn = lifePersistance;
                            energy.scale = 1.56f;
                        }
                    }
                }
            }

            if (attackTimer >= flyTime)
                npc.velocity *= 0.975f;
            npc.rotation *= 0.96f;

            if (attackTimer >= flyTime + waveDelay + 90f)
            {
                if (waveCounter >= totalWaves - 1f)
                    GoToNextAttackState(npc);
                else
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 34f;
                    attackTimer = flyTime;
                    waveCounter++;
                    npc.netUpdate = true;

                    // Make a diving sound.
                    Main.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                }
            }
        }

        internal static void DoAttack_ReleasePlasmaBursts(NPC npc, Player target, ref float attackTimer, ref float fadeToRed, ref float frameType, ref float flapRate)
        {
            if (NPC.CountNPCS(ModContent.NPCType<RedPlasmaEnergy>()) >= 3)
                GoToNextAttackState(npc);
            ref float chargeTime = ref npc.Infernum().ExtraAI[0];

            frameType = (int)DragonfollyFrameDrawingType.Screm;

            for (int delay = 0; delay < 60; delay += 20)
            {
                Vector2 mouthPosition = npc.Center + Vector2.UnitX * npc.direction * (float)Math.Cos(npc.rotation) * (npc.width * 0.5f + 27f);
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == ScreamTime - 30f + delay)
                {
                    int plasmaBall = NPC.NewNPC((int)mouthPosition.X, (int)mouthPosition.Y, ModContent.NPCType<RedPlasmaEnergy>());
                    if (Main.npc.IndexInRange(plasmaBall))
                        Main.npc[plasmaBall].velocity = Vector2.UnitX.RotatedByRandom(0.4f) * npc.direction * 7f;
                }
            }

            npc.rotation *= 0.975f;
            npc.velocity *= 0.975f;
            if (attackTimer >= ScreamTime + 45f)
                GoToNextAttackState(npc);
        }

        internal static void DoAttack_LightningSupercharge(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float flapRate)
		{
			float horizontalOffset = 750f;
			ref float chargeState = ref npc.Infernum().ExtraAI[0];
			ref float accumulatedSpeed = ref npc.Infernum().ExtraAI[1];
			ref float backgroundFadeToRed = ref npc.Infernum().ExtraAI[8];

			// Line up for charge.
			if (chargeState == 0f)
			{
				float verticalOffsetLeniance = 75f;
				float flySpeed = 18.5f + accumulatedSpeed;
				float flyInertia = 4f;
				Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

				// Fly towards the destination beside the player.
				npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;
				npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

				// If within a good approximation of the player's position, scream loudly.
				if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
				{
					chargeState = 1f;
					accumulatedSpeed = 0f;
					attackTimer = 0f;
					npc.netUpdate = true;
				}

				// Become more and more fast the more time has passed.
				// (Why does this comment sound funny to me?)
				accumulatedSpeed += 0.055f;

                frameType = (int)DragonfollyFrameDrawingType.FlapWings;
                flapRate = 6f;
            }

			// Scream and create a red shockwave/background.
			else if (chargeState == 1f)
			{
				npc.rotation *= 0.96f;
				npc.velocity *= 0.98f;
				backgroundFadeToRed = MathHelper.Lerp(backgroundFadeToRed, 1f, 0.1f);
				if (attackTimer < ScreamTime + 30f)
				{
					frameType = (int)DragonfollyFrameDrawingType.Screm;
					if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == ScreamTime + 10f)
						Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);
				}
				else
				{
					frameType = (int)DragonfollyFrameDrawingType.FlapWings;
					flapRate = 6f;
				}

				// Reel back.
				if (attackTimer == ScreamTime + 75f)
				{
					npc.velocity = npc.SafeDirectionTo(target.Center) * -8f;
					npc.velocity.X *= 0.3f;
					chargeState = 2f;
					accumulatedSpeed = 0f;
					attackTimer = 0f;
					npc.netUpdate = true;
				}
			}

			// Prepare for the charge.
			else if (chargeState == 2f)
			{
				backgroundFadeToRed = 1f;

				float flySpeed = 32.5f;
				float flyInertia = 26f;
				Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
				npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity * 0.8f) / flyInertia;
				npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
				npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;

				if (attackTimer >= 15f)
				{
					npc.velocity = chargeVelocity;
					chargeState = 3f;
					accumulatedSpeed = 0f;
					attackTimer = 0f;
					npc.netUpdate = true;

                    // Make a diving sound.
                    Main.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                }
			}

			// Do the charge and release lightning everywhere.
			else if (chargeState == 3f)
			{
				npc.velocity *= 0.99f;
				if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 16f == 15f)
				{
					Vector2 spawnPosition = target.Center + Vector2.UnitX * Main.rand.NextFloat(60f, 900f) * Main.rand.NextBool(2).ToDirectionInt();
					int cloud = Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<LightningCloud2>(), 0, 0f);
					if (Main.projectile.IndexInRange(cloud))
					{
						Main.projectile[cloud].timeLeft = 10 + (110 - (int)attackTimer);
						Main.projectile[cloud].netUpdate = true;
					}
				}

				npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
				npc.rotation = (npc.rotation * 7f + npc.velocity.X * 0.01f) / 8f;
				if (attackTimer >= 60f)
				{
					float flyInertia = 6f;
					float flySpeed = 25f;
					if (!npc.WithinRange(target.Center, 315f))
						npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(target.Center) * flySpeed) / flyInertia;
				}

				if (attackTimer >= 120f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);
                    GoToNextAttackState(npc);
				}
			}
		}

        #endregion

        #endregion Main Boss

        #region Minions

        [OverrideAppliesTo("Bumblefuck2", typeof(DragonfollyAIClass), "DragonSwarmerAI", EntityOverrideContext.NPCAI)]
        public static bool DragonSwarmerAI(NPC npc)
        {
            npc.damage = CalamityPlayer.areThereAnyDamnBosses ? npc.defDamage : (int)(npc.defDamage * 0.8);

            Player target = Main.player[npc.target];

            bool duringFollyFight = CalamityPlayer.areThereAnyDamnBosses;

            bool inPhase2 = npc.ai[3] == 1f;
            bool inPhase3 = npc.ai[3] == 2f;
            if (inPhase3)
                npc.damage = (int)(npc.defDamage * 1.4);
            else if (inPhase2)
                npc.damage = (int)(npc.defDamage * 1.25);

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float fadeToRed = ref npc.localAI[0];
            ref float deathTimer = ref npc.Infernum().ExtraAI[0];

            // Begin the redshift phase in phase 2 if 15 seconds have passed.
            if (inPhase2)
			{
                deathTimer++;
                if (npc.ai[0] != 3f && deathTimer >= 900f)
                {
                    npc.ai[0] = 3f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
				}
			}

            // Despawn immediately if super far from the target.
            if (!npc.WithinRange(target.Center, 5600f))
            {
                if (npc.timeLeft > 5)
                    npc.timeLeft = 5;
            }

            npc.noTileCollide = false;
            npc.noGravity = true;

            npc.rotation = (npc.rotation * 4f + npc.velocity.X * 0.04f * 1.25f) / 10f;

            // Repel from other swarmers.
            if (attackState == 0f || attackState == 1f)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (i == npc.whoAmI || !Main.npc[i].active || Main.npc[i].type != npc.type)
                        continue;

                    if (Main.npc[i].WithinRange(npc.Center, npc.width + npc.height))
                    {
                        Vector2 repelVelocity = npc.SafeDirectionTo(Main.npc[i].Center) * -0.1f;

                        npc.velocity += repelVelocity;
                        Main.npc[i].velocity -= repelVelocity;
                    }
                }
            }

            // Attempt to find a new target. If no target is found or it's too far away, fly away.
            if (npc.target < 0 || target.dead || !target.active)
            {
                npc.TargetClosest(true);
                target = Main.player[npc.target];
                if (target.dead || !npc.WithinRange(target.Center, duringFollyFight ? 4600f : 2800f))
                    attackState = -1f;
            }
            else
            {
                if (attackState > 1f && !npc.WithinRange(target.Center, 3600f))
                    attackState = 1f;
            }

            // Fly upward and despawn.
            if (attackState == -1f)
            {
                npc.velocity = (npc.velocity * 9f + Vector2.UnitY * -16f) / 10f;
                npc.noTileCollide = true;
                npc.dontTakeDamage = true;
                if (npc.timeLeft > 240)
                    npc.timeLeft = 240;
                return false;
            }

            // Search for a player to target.
            if (attackState == 0f)
            {
                npc.TargetClosest(true);
                target = Main.player[npc.target];
                npc.spriteDirection = npc.direction;
                
                // Rebound and clamp movement on tile collision.
                if (npc.collideX)
                {
                    npc.velocity.X *= -npc.oldVelocity.X * 0.5f;
                    npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -4f, 4f);
                }
                if (npc.collideY)
                {
                    npc.velocity.Y *= -npc.oldVelocity.Y * 0.5f;
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -4f, 4f);
                }

                // If the player is very far away, go to a different attack.
                if (!npc.WithinRange(target.Center, 2800f))
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.ai[2] = 0f;
                }
                // Otherwise fly towards the destination if relatively far from it.
                else if (!npc.WithinRange(target.Center, 400f))
                {
                    float distanceFromPlayer = npc.Distance(target.Center);
                    float flySpeed = (duringFollyFight ? 9f : 7f) + distanceFromPlayer / 100f + attackTimer / 15f;
                    float flyInertia = 30f;
                    npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(target.Center) * flySpeed) / flyInertia;
                }
                else if (npc.velocity.Length() > 2f)
                    npc.velocity *= 0.95f;
                else if (npc.velocity.Length() < 1f)
                    npc.velocity *= 1.05f;

                attackTimer++;
                if (attackTimer >= (duringFollyFight ? 90f : 105f))
                {
                    attackTimer = 0f;
                    attackState = 2f;
                }
            }
            else if (npc.ai[0] == 1f)
            {
                npc.collideX = false;
                npc.collideY = false;
                npc.noTileCollide = true;
                if (npc.target < 0 || !Main.player[npc.target].active || Main.player[npc.target].dead)
                {
                    npc.TargetClosest(true);
                    target = Main.player[npc.target];
                }

                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.rotation = (npc.rotation * 4f + npc.velocity.X * 0.04f) / 10f;

                // If somewhat close to the player and not stuck, go back to picking an attack.
                if (npc.WithinRange(target.Center, 800f) && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
                npc.ai[2] += 0.0166666675f;
                float flyInertia = 25f;
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * ((duringFollyFight ? 12f : 9f) + npc.ai[2] + npc.Distance(target.Center) / 150f);
                npc.velocity = (npc.velocity * (flyInertia - 1f) + idealVelocity) / flyInertia;
                return false;
            }
            else if (attackState == 2f)
            {
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.rotation = (npc.rotation * 4f * 0.75f + npc.velocity.X * 0.04f * 1.25f) / 8f;
                npc.noTileCollide = true;

                // Line up for a charge for a short amount of time.
                float flyInertia = 8f;
                Vector2 idealFlyVelocity = npc.SafeDirectionTo(target.Center) * (duringFollyFight ? 16f : 12f);
                npc.velocity = (npc.velocity * (flyInertia - 1f) + idealFlyVelocity) / flyInertia;
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;

                attackTimer++;
                int chargeDelay = inPhase2 ? 17 : 10;

                // And perform the charge.
                if (attackTimer > chargeDelay)
                {
                    npc.velocity = idealFlyVelocity;
                    npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                    attackState = 2.1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (attackState == 2.1f)
            {
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.velocity *= 1.01f;
                npc.noTileCollide = true;
                if (attackTimer > 30f)
                {
                    // If not stuck, just go back to picking a different attack.
                    if (!Collision.SolidCollision(npc.position, npc.width, npc.height))
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        npc.ai[2] = 0f;
                        return false;
                    }

                    // Otherwise, if stuck, wait for a little more time. If still stuck after that, 
                    // do the idle search attack.
                    if (attackTimer > 60f)
                    {
                        attackState = 1f;
                        attackTimer = 0f;
                        npc.ai[2] = 0f;
                    }

                    npc.netUpdate = true;
                }
                attackTimer++;
            }
            else if (attackState == 3f)
            {
                npc.noTileCollide = true;
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;

                int fadeTime = 35;
                int chargeDelay = 15;
                int chargeTime = 90;
                float flyInertia = 21f;
                float chargeSpeed = 29f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                // Try to go towards the player and charge, while fading red.
                fadeToRed = MathHelper.SmoothStep(0f, 1f, Utils.InverseLerp(0f, fadeTime, attackTimer, true));
                if (attackTimer >= fadeTime && attackTimer <= fadeTime + chargeDelay)
				{
                    npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;
                    if (attackTimer == fadeTime + chargeDelay)
                    {
                        npc.velocity = chargeVelocity;
                        npc.netUpdate = true;
                    }
                }

                // After charging for a certain amount of time, fade out of existance.
                if (attackTimer >= fadeTime + chargeDelay + chargeTime)
                    npc.Opacity = MathHelper.Lerp(npc.Opacity, 0f, 0.08f);

                if (npc.Opacity < 0.425f)
                {
                    npc.active = false;
                    npc.netUpdate = true;
                }
                // Release lightning clouds when charging if in phase 3.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > fadeTime + chargeDelay && attackTimer % 7f == 6f && inPhase3)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<VolatileLightning>(), 0, 0f);

                attackTimer++;
            }
            return false;
		}
        #endregion Minions

        #endregion AI

        #region Frames and Drawcode

        internal const int ScreamTime = 60;
        internal const int ScreamSoundDelay = ScreamTime - 20;

        [OverrideAppliesTo("Bumblefuck", typeof(DragonfollyAIClass), "DragonfollyFindFrame", EntityOverrideContext.NPCFindFrame)]
        public static void DragonfollyFindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            ref float frameType = ref npc.localAI[0];
            ref float flapRate = ref npc.localAI[1];
            ref float scremTimer = ref npc.localAI[3];

            switch ((DragonfollyFrameDrawingType)(int)frameType)
			{
                case DragonfollyFrameDrawingType.FlapWings:
                    if (npc.frameCounter >= flapRate)
                    {
                        npc.frameCounter = 0D;
                        npc.frame.Y += frameHeight;
                    }
                    if (npc.frame.Y >= frameHeight * 5)
                        npc.frame.Y = 0;
                    scremTimer = 0f;
                    break;
                case DragonfollyFrameDrawingType.Screm:
                    scremTimer++;
                    if (npc.frameCounter >= 5f)
                    {
                        npc.frameCounter = 0D;
                        npc.frame.Y += frameHeight;
                    }
                    if (npc.frame.Y >= frameHeight * 5)
                        npc.frame.Y = 0;

                    if (scremTimer >= ScreamSoundDelay - 15f)
                    {
                        npc.frame.Y = frameHeight * 5;
                        if (scremTimer == ScreamSoundDelay)
                        {
                            SoundEffectInstance sound = Main.PlaySound(SoundID.DD2_BetsyScream, npc.Center);
                            if (sound != null)
                                sound.Pitch = 0.25f;
                        }
                    }
                    break;
            }
		}

        [OverrideAppliesTo("Bumblefuck", typeof(DragonfollyAIClass), "DragonfollyPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool DragonfollyPreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float fadeToRed = npc.localAI[2];
            float phaseTransitionCountdown = npc.Infernum().ExtraAI[6];
            Texture2D texture = Main.npcTexture[npc.type];
            int drawInstances = (int)MathHelper.Lerp(1f, 4f, fadeToRed);
            Color drawColor = Color.Lerp(lightColor, Color.Red * 0.9f, fadeToRed);
            drawColor *= MathHelper.Lerp(1f, 0.4f, fadeToRed);
            if (fadeToRed > 0.4f)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);
                drawColor.A = 0;
            }
            
            Vector2 origin = npc.frame.Size() * 0.5f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;
            
            void drawInstance(Vector2 baseDrawPosition, float scale, float opacity)
            {
                if (phaseTransitionCountdown > 0f)
                {
                    float outwardnessFactor = 1f - (float)Math.Cos(phaseTransitionCountdown * MathHelper.TwoPi / TransitionTime);
                    outwardnessFactor /= 3f;
                    for (int i = 0; i < 6; i++)
                    {
                        Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.Red, 0.5f));
                        afterimageColor *= 1f - outwardnessFactor;

                        Vector2 drawPosition = npc.Center + (i / 6f * MathHelper.TwoPi + npc.rotation).ToRotationVector2() * outwardnessFactor * 42f - Main.screenPosition;
                        spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, spriteEffects, 0f);
                    }
                }

                for (int i = 0; i < drawInstances; i++)
                {
                    Vector2 drawPosition = baseDrawPosition - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    if (fadeToRed > 0.4f)
                        drawPosition += (MathHelper.TwoPi * i / drawInstances + Main.GlobalTime * 5f).ToRotationVector2() * 5f;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor) * opacity, npc.rotation, origin, scale, spriteEffects, 0f);
                }
            }

            drawInstance(npc.Center, npc.scale, 1f);
            return false;
        }


        [OverrideAppliesTo("Bumblefuck2", typeof(DragonfollyAIClass), "DragonSwarmerPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool DragonSwarmerPreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float fadeToRed = npc.localAI[0];
            float backgroundFadeToRed = 0f;
            int follyIndex = NPC.FindFirstNPC(ModContent.NPCType<Bumblefuck>());
            if (Main.npc.IndexInRange(follyIndex))
                backgroundFadeToRed = Main.npc[follyIndex].Infernum().ExtraAI[8];
            Texture2D texture = Main.npcTexture[npc.type];
            int drawInstances = (int)MathHelper.Lerp(1f, 4f, fadeToRed);
            Color drawColor = Color.Lerp(lightColor, Color.Red * 0.9f, fadeToRed);
            drawColor = Color.Lerp(drawColor, Color.White, backgroundFadeToRed * 0.9f);
            drawColor *= MathHelper.Lerp(1f, 0.4f, fadeToRed);
            if (fadeToRed > 0.4f)
                drawColor.A = 0;

            Vector2 origin = npc.frame.Size() * 0.5f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            void drawInstance(Vector2 baseDrawPosition, float scale, float opacity)
            {
                for (int i = 0; i < drawInstances; i++)
                {
                    Vector2 drawPosition = baseDrawPosition - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    if (fadeToRed > 0.4f)
                        drawPosition += (MathHelper.TwoPi * i / drawInstances + Main.GlobalTime * 5f).ToRotationVector2() * 2.5f;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor) * opacity, npc.rotation, origin, scale, spriteEffects, 0f);
                }
            }

            drawInstance(npc.Center, npc.scale, npc.Opacity);
            return false;
        }
        #endregion
    }
}
