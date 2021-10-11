﻿using CalamityMod;
using CalamityMod.NPCs.StormWeaver;
using InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaverHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverHeadNaked>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum StormWeaverAttackType
        {
            NormalMove,
            SparkBurst,
            LightningCharge,
            StaticChargeup,
            StormWeave,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);

            // Create segments and start rain.
            if (npc.localAI[0] == 0f)
            {
                AquaticScourgeHeadBehaviorOverride.CreateSegments(npc, 15, ModContent.NPCType<StormWeaverBodyNaked>(), ModContent.NPCType<StormWeaverTailNaked>());

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CalamityUtils.StartRain(true);
                    Main.cloudBGActive = 1f;
                    Main.numCloudsTemp = Main.cloudLimit;
                    Main.numClouds = Main.numCloudsTemp;
                    Main.windSpeedTemp = 1.56f;
                    Main.windSpeedSet = Main.windSpeedTemp;
                    Main.maxRaining = 0.9f;
                }

                npc.localAI[0] = 1f;
            }

            float fadeToBlue = 0f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];

            switch ((StormWeaverAttackType)(int)attackState)
            {
                case StormWeaverAttackType.NormalMove:
                    DoAttack_NormalMove(npc, target, attackTimer);
                    break;
                case StormWeaverAttackType.SparkBurst:
                    DoAttack_SparkBurst(npc, target, lifeRatio, attackTimer);
                    break;
                case StormWeaverAttackType.LightningCharge:
                    DoAttack_LightningCharge(npc, target, lifeRatio, ref attackTimer, ref fadeToBlue);
                    break;
                case StormWeaverAttackType.StaticChargeup:
                    DoAttack_StaticChargeup(npc, target, ref attackTimer, ref fadeToBlue);
                    break;
                case StormWeaverAttackType.StormWeave:
                    DoAttack_StormWeave(npc, target, ref attackTimer, ref fadeToBlue);
                    break;
            }

            Main.rainTime = 480;

            // Determine rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Determine blue fade.
            npc.Calamity().newAI[0] = MathHelper.Lerp(280f, 400f, MathHelper.Clamp(fadeToBlue, 0f, 1f));

            attackTimer++;
            return false;
        }

        public static void DoAttack_NormalMove(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.039f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.015f;
            else if (npc.velocity.Length() > 19f + attackTimer / 36f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 15f, 29f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer >= 300f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_SparkBurst(NPC npc, Player target, float lifeRatio, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.054f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 13f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 13f, 25f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 40f == 39f && !npc.WithinRange(target.Center, 210f))
            {
                // Create some mouth dust.
                for (int i = 0; i < 20; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 30f, 229);
                    electricity.velocity = Main.rand.NextVector2Circular(5f, 5f) + npc.velocity;
                    electricity.scale = 1.9f;
                    electricity.noGravity = true;
                }

                Main.PlaySound(SoundID.Item94, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootSpeed = MathHelper.Lerp(8f, 12.5f, 1f - lifeRatio);
                    for (int i = 0; i < 9; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.51f, 0.51f, i / 8f);
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                    }
                }
            }

            if (attackTimer >= 450f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_LightningCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float fadeToBlue)
        {
            int hoverRedirectTime = 240;
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt(), (target.Center.Y < npc.Center.Y).ToDirectionInt()) * 485f;
            Vector2 hoverDestination = target.Center + hoverOffset;
            int chargeRedirectTime = 40;
            int chargeTime = 45;
            int chargeSlowdownTime = 25;
            int chargeCount = 3;
            ref float idealChargeVelocityX = ref npc.Infernum().ExtraAI[0];
            ref float idealChargeVelocityY = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(20.5f, 39f, attackTimer / hoverRedirectTime);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.064f, true) * idealVelocity.Length();

                // Stop hovering if close to the hover destination
                if (npc.WithinRange(hoverDestination, 40f))
                {
                    attackTimer = hoverRedirectTime;
                    if (npc.velocity.Length() > 24f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 24f;

                    npc.netUpdate = true;
                }
            }

            // Determine a charge velocity to adjust to.
            if (attackTimer == hoverRedirectTime)
            {
                Vector2 idealChargeVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f) * MathHelper.Lerp(24f, 31.5f, 1f - lifeRatio);
                idealChargeVelocityX = idealChargeVelocity.X;
                idealChargeVelocityY = idealChargeVelocity.Y;
                npc.netUpdate = true;
            }

            // Move into the charge.
            if (attackTimer > hoverRedirectTime && attackTimer <= hoverRedirectTime + chargeRedirectTime)
            {
                Vector2 idealChargeVelocity = new Vector2(idealChargeVelocityX, idealChargeVelocityY);
                npc.velocity = npc.velocity.RotateTowards(idealChargeVelocity.ToRotation(), 0.08f, true) * MathHelper.Lerp(npc.velocity.Length(), idealChargeVelocity.Length(), 0.15f);
                npc.velocity = npc.velocity.MoveTowards(idealChargeVelocity, 5f);
            }

            // Release lightning from behind the worm once the charge has begun.
            if (attackTimer == hoverRedirectTime + chargeRedirectTime / 2)
            {
                Main.PlaySound(SoundID.DD2_KoboldExplosion, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 lightningSpawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.92f) * 150f;
                        Vector2 lightningVelocity = (target.Center - lightningSpawnPosition).SafeNormalize(Vector2.UnitY) * 6.5f;
                        int arc = Utilities.NewProjectileBetter(lightningSpawnPosition, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 255, 0f);
                        if (Main.projectile.IndexInRange(arc))
                        {
                            Main.projectile[arc].ai[0] = lightningVelocity.ToRotation();
                            Main.projectile[arc].ai[1] = Main.rand.Next(100);
                            Main.projectile[arc].tileCollide = false;
                        }
                    }
                }
            }

            // Slow down after charging.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime)
                npc.velocity *= 0.95f;

            // Calculate fade to blue.
            fadeToBlue = Utils.InverseLerp(hoverRedirectTime, hoverRedirectTime + chargeRedirectTime, attackTimer, true) *
                Utils.InverseLerp(hoverRedirectTime + chargeRedirectTime + chargeTime + chargeSlowdownTime, hoverRedirectTime + chargeRedirectTime + chargeTime, attackTimer, true);

            // Prepare the next charge. If all charges are done, go to the next attack.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime + chargeSlowdownTime)
            {
                chargeCounter++;
                idealChargeVelocityX = 0f;
                idealChargeVelocityY = 0f;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNewAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoAttack_StaticChargeup(NPC npc, Player target, ref float attackTimer, ref float fadeToBlue)
        {
            int initialAttackWaitDelay = 10;
            float attackStartDistanceThreshold = 490f;
            int spinDelay = 30;
            int totalSpins = 3;
            int spinTime = 270;
            float spinSpeed = 18f;
            float angularSpinVelocity = MathHelper.TwoPi * totalSpins / spinTime;

            // Determine fade to blue.
            fadeToBlue = Utils.InverseLerp(spinDelay, spinDelay + initialAttackWaitDelay, attackTimer, true) *
                Utils.InverseLerp(spinDelay + initialAttackWaitDelay + spinTime, spinDelay + initialAttackWaitDelay + spinTime - 30f, attackTimer, true);

            // Attempt to move towards the target if far away from them.
            if (!npc.WithinRange(target.Center, attackStartDistanceThreshold) && attackTimer < initialAttackWaitDelay)
            {
                float idealMoventSpeed = (npc.Distance(target.Center) - attackStartDistanceThreshold) / 70f + 15f;
                npc.velocity = (npc.velocity * 39f + npc.SafeDirectionTo(target.Center) * idealMoventSpeed) / 40f;

                attackTimer = 0f;
            }

            // Attempt to get closer to the ideal spin speed once ready.
            if (attackTimer >= initialAttackWaitDelay && attackTimer < spinDelay + initialAttackWaitDelay && !npc.WithinRange(target.Center, 150f))
                npc.velocity = (npc.velocity * 14f + npc.SafeDirectionTo(target.Center) * spinSpeed) / 15f;

            // Prepare the spin movement and direction.
            if (attackTimer == spinDelay + initialAttackWaitDelay)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitY) * spinSpeed;
                npc.netUpdate = true;
            }

            // Do the spin, along with associated attacks from it.
            if (attackTimer > spinDelay + initialAttackWaitDelay)
            {
                npc.velocity = npc.velocity.RotatedBy(angularSpinVelocity);
                Vector2 spinCenter = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * spinTime / totalSpins / MathHelper.TwoPi;

                // Frequently release sparks.
                if (attackTimer % 8f == 7f)
                {
                    Main.PlaySound(SoundID.DD2_LightningAuraZap, spinCenter);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projectileCount = attackTimer % 24f == 23f ? 9 : 1;

                        for (int i = 0; i < projectileCount; i++)
                        {
                            float angularImprecision = Utils.InverseLerp(720f, 350f, npc.Distance(target.Center), true);
                            float predictivenessFactor = (float)Math.Pow(1f - angularImprecision, 2D);
                            float angularOffset = 0f;
                            if (projectileCount > 1)
                                angularOffset = MathHelper.Lerp(-0.87f, 0.87f, i / (float)(projectileCount - 1f));

                            Vector2 predictiveOffset = target.velocity * predictivenessFactor * 15f;
                            Vector2 shootVelocity = (target.Center - spinCenter + predictiveOffset).SafeNormalize(Vector2.UnitY).RotatedBy(angularOffset).RotatedByRandom(angularImprecision * 0.59f) * 5.6f;
                            Utilities.NewProjectileBetter(spinCenter, shootVelocity, ModContent.ProjectileType<WeaverSpark>(), 255, 0f);
                        }
                    }
                }

                // As well as some electric orbs that explode.
                if (attackTimer % 90f == 89f)
                {
                    Vector2 orbSpawnPosition = spinCenter + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f, 220f);
                    Vector2 orbShootVelocity = (target.Center - orbSpawnPosition).SafeNormalize(Vector2.UnitY) * 5f;

                    // Play a sound and create some electric dust.
                    for (int i = 0; i < 16; i++)
                    {
                        Dust electricity = Dust.NewDustPerfect(orbSpawnPosition + Main.rand.NextVector2Circular(45f, 45f), 264);
                        electricity.color = Color.Cyan;
                        electricity.velocity = (electricity.position - orbShootVelocity).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(5f, 12f);
                        electricity.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(orbSpawnPosition, orbShootVelocity, ModContent.ProjectileType<ElectricOrb>(), 255, 0f);
                }
            }

            if (attackTimer >= spinDelay + initialAttackWaitDelay + spinTime)
                SelectNewAttack(npc);
        }

        public static void DoAttack_StormWeave(NPC npc, Player target, ref float attackTimer, ref float fadeToBlue)
        {
            int hoverRedirectTime = 240;
            int chargeTime = 235;
            float cloudCoverArea = 4600f;
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * cloudCoverArea * 0.5f, -1350f);
            Vector2 hoverDestination = target.Center + hoverOffset;
            if (hoverDestination.Y < 300f)
                hoverDestination.Y = 300f;

            float chargeSpeed = cloudCoverArea / chargeTime;
            int chargeSlowdownTime = 180;
            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(26.5f, 45f, attackTimer / hoverRedirectTime);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.074f, true) * idealVelocity.Length();
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                // Stop hovering if close to the hover destination
                if (npc.WithinRange(hoverDestination, 40f))
                {
                    attackTimer = hoverRedirectTime;
                    npc.netUpdate = true;
                }
            }

            // Begin charging horizontally, releasing storm clouds while doing so.
            if (attackTimer > hoverRedirectTime && attackTimer < hoverRedirectTime + chargeTime)
            {
                Vector2 chargeVelocity = Vector2.UnitX * chargeSpeed * chargeDirection;
                npc.velocity = Vector2.Lerp(npc.velocity, chargeVelocity, 0.15f).MoveTowards(chargeVelocity, 2f);

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 6f == 5f)
                {
                    Vector2 cloudSpawnPosition = npc.Center + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 cloudVelocity = Main.rand.NextVector2Circular(5f, 5f);
                    Utilities.NewProjectileBetter(cloudSpawnPosition, cloudVelocity, ModContent.ProjectileType<StormWeaveCloud>(), 0, 0f);
                }
            }

            if (attackTimer > hoverRedirectTime + chargeTime + chargeSlowdownTime)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];
            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add((int)StormWeaverAttackType.NormalMove, 1.5);
            newStatePicker.Add((int)StormWeaverAttackType.LightningCharge);
            newStatePicker.Add((int)StormWeaverAttackType.StaticChargeup);

            if (lifeRatio < 0.5f)
                newStatePicker.Add((int)StormWeaverAttackType.StormWeave, 6.5);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
