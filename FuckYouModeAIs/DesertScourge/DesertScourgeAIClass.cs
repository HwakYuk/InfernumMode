﻿using CalamityMod.NPCs.DesertScourge;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.DesertScourge
{
	public class DesertScourgeAIClass
    {
		#region AI

		[OverrideAppliesTo("DesertScourgeHead", typeof(DesertScourgeAIClass), "DesertScourgeHeadAI", EntityOverrideContext.NPCAI)]
        public static bool DesertScourgeHeadAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            int digLungeTime = 420;
            ref float initializedFlag = ref npc.Infernum().ExtraAI[0];
            ref float inAirTime = ref npc.Infernum().ExtraAI[1];
            ref float fallTime = ref npc.Infernum().ExtraAI[2];
            ref float digPreparationTime = ref npc.Infernum().ExtraAI[3];
            ref float digAttackTime = ref npc.Infernum().ExtraAI[4];
            ref float lungeFallTimer = ref npc.Infernum().ExtraAI[5];
            ref float mandatoryLungeCount = ref npc.Infernum().ExtraAI[6];
            ref float mandatoryLungeCountdown = ref npc.Infernum().ExtraAI[7];
            ref float boneToothShootCounter = ref npc.Infernum().ExtraAI[8];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 28, ModContent.NPCType<DesertScourgeBody>(), ModContent.NPCType<DesertScourgeTail>());
                SummonSmallerWorms(npc);
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // If there still was no valid target, dig away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoAttack_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            bool inTiles = Collision.SolidCollision(npc.position, npc.width, npc.height);
            npc.defense = target.ZoneDesert ? npc.defDefense : 1000;
            npc.dontTakeDamage = mandatoryLungeCountdown > 120f || NPC.AnyNPCs(ModContent.NPCType<DesertScourgeHeadSmall>());

            // Idly release bone teeth.
            boneToothShootCounter++;
            if (!npc.dontTakeDamage && !inTiles && boneToothShootCounter % 180f == 179f)
            {
                Main.PlaySound(SoundID.Item92, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 spawnPosition = npc.Center;
                        Vector2 shootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.43f) * Main.rand.NextFloat(8f, 11f);
                        spawnPosition += shootVelocity * 2.5f;

                        int sand = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<BoneTooth>(), 52, 0f);
                        if (Main.projectile.IndexInRange(sand))
                            Main.projectile[sand].tileCollide = false;
                    }
                }
            }

            if (mandatoryLungeCountdown > 0)
                mandatoryLungeCountdown--;

            // Perform mandatory lunges at certain life intervals.
            if (mandatoryLungeCount == 0f && lifeRatio < 0.7f)
			{
                digAttackTime = mandatoryLungeCountdown = digLungeTime;
                mandatoryLungeCount++;
                npc.netUpdate = true;
            }
            if (mandatoryLungeCount == 1f && lifeRatio < 0.4f)
            {
                digAttackTime = mandatoryLungeCountdown = digLungeTime;
                mandatoryLungeCount++;
                npc.netUpdate = true;
            }

            // Do the dig lunge as necessary.
            if (digAttackTime > 0f)
            {
                DoAttack_DoDigLunge(npc, target, digLungeTime - digAttackTime, lifeRatio, ref lungeFallTimer);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                digAttackTime--;

                return false;
            }

            lungeFallTimer = 0f;

            if (lifeRatio < 0.1f)
			{
                // After an amount of time, begin the dig/lunge.
                digPreparationTime++;
                if (digPreparationTime >= digLungeTime)
                {
                    digPreparationTime = 0f;
                    digAttackTime = digLungeTime;
                    npc.netUpdate = true;
                }
            }

            // If the worm has been in air for too long, fall into the ground again.
            if (fallTime > 0f)
                DoAttack_FallIntoGround(npc, inTiles, ref fallTime, ref inAirTime);
            else
                DoAttack_FlyTowardsTarget(npc, target, lifeRatio, inTiles, ref inAirTime, ref fallTime);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            
            return false;
		}

        [OverrideAppliesTo("DesertScourgeBody", typeof(DesertScourgeAIClass), "DesertScourgeSegmentAI", EntityOverrideContext.NPCAI)]
        [OverrideAppliesTo("DesertScourgeTail", typeof(DesertScourgeAIClass), "DesertScourgeSegmentAI", EntityOverrideContext.NPCAI)]
        public static bool DesertScourgeSegmentAI(NPC npc)
        {
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC aheadSegment = Main.npc[(int)npc.ai[1]];

            npc.target = aheadSegment.target;
            Player target = Main.player[npc.target];

            if (aheadSegment.alpha < 128)
                npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

            npc.defense = aheadSegment.defense;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
            return false;
		}


        [OverrideAppliesTo("DesertScourgeHeadSmall", typeof(DesertScourgeAIClass), "DesertScourgeSmallHeadAI", EntityOverrideContext.NPCAI)]
        public static bool DesertScourgeSmallHeadAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Become more aggressive if the other worm is dead.
            bool onlyWormAlive = NPC.CountNPCS(npc.type) == 1;
            if (onlyWormAlive)
                lifeRatio = MathHelper.Clamp(lifeRatio + 0.5f, 0f, 1f);

            ref float initializedFlag = ref npc.Infernum().ExtraAI[1];
            ref float time = ref npc.Infernum().ExtraAI[2];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 16, ModContent.NPCType<DesertScourgeBodySmall>(), ModContent.NPCType<DesertScourgeTailSmall>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // If there still was no valid target, dig away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoAttack_Despawn(npc);
                return false;
            }

            time++;
            Player target = Main.player[npc.target];

            switch ((int)npc.Infernum().ExtraAI[0])
            {
                // Aggressive worm.
                case 0:
                    Vector2 destination = target.Center;

                    // If close to the target, determine the destination based on the current direction of the worm.
                    if (npc.WithinRange(target.Center, 140f))
                        destination += npc.velocity.SafeNormalize(Vector2.UnitY) * 180f;

                    float distanceFromDestination = npc.Distance(destination);
                    float turnSpeed = MathHelper.Lerp(0.008f, 0.055f, Utils.InverseLerp(175f, 475f, distanceFromDestination, true));

                    float newSpeed = npc.velocity.Length();
                    float idealSpeed = MathHelper.Lerp(6.7f, 13f, 1f - lifeRatio);

                    // Accelerate quickly if relatively far from the destination.
                    if (distanceFromDestination > 1250f)
                        newSpeed += 0.06f;

                    // Otherwise slow down if relatively close to the destination.
                    if (distanceFromDestination < 300f)
                        newSpeed -= 0.04f;

                    // Slowly regress back to the ideal speed over time.
                    newSpeed = MathHelper.Lerp(newSpeed, idealSpeed, 0.018f);
                    newSpeed = MathHelper.Clamp(newSpeed, 4f, 21.5f);

                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), turnSpeed, true) * newSpeed;
                    break;

                // Sand spewing worm.
                case 1:
                    destination = target.Center;
                    if (npc.WithinRange(destination, 360f))
                        destination += (time / 105f * MathHelper.TwoPi).ToRotationVector2() * 270f;

                    distanceFromDestination = npc.Distance(destination);
                    turnSpeed = MathHelper.Lerp(0.04f, 0.11f, Utils.InverseLerp(175f, 475f, distanceFromDestination, true));

                    newSpeed = npc.velocity.Length();
                    idealSpeed = MathHelper.Lerp(5.5f, 10f, 1f - lifeRatio);

                    // Accelerate quickly if relatively far from the destination.
                    if (distanceFromDestination > 1250f)
                        newSpeed += 0.04f;

                    // Otherwise slow down if relatively close to the destination.
                    if (distanceFromDestination < 300f)
                        newSpeed -= 0.04f;

                    // Slowly regress back to the ideal speed over time.
                    newSpeed = MathHelper.Lerp(newSpeed, idealSpeed, 0.02f);
                    newSpeed = MathHelper.Clamp(newSpeed, 5f, 18f);

                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), turnSpeed, true) * newSpeed;

                    // Release sand from the mouth every so often.
                    int sandShootRate = onlyWormAlive ? 90 : 130;
                    if (Main.netMode != NetmodeID.MultiplayerClient && time % sandShootRate == sandShootRate - 1f)
					{
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 spawnPosition = npc.Center;
                            Vector2 shootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.47f) * Main.rand.NextFloat(7f, 9f);
                            spawnPosition += shootVelocity * 2f;

                            int sand = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SandBlast>(), 52, 0f);
                            if (Main.projectile.IndexInRange(sand))
                                Main.projectile[sand].tileCollide = false;
                        }
                    }
                    break;
			}
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
		}

        #region Specific Attacks

        public static void DoAttack_Despawn(NPC npc)
		{
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 15f)
                npc.velocity.Y += 0.25f;

            if (npc.timeLeft > 200)
                npc.timeLeft = 200;
		}

        public static void DoAttack_DoDigLunge(NPC npc, Player target, float attackTimer, float lifeRatio, ref float lungeFallTimer)
		{
            int burrowTime = 150;
            if (lifeRatio < 0.4f)
                burrowTime = 125;
            if (lifeRatio < 0.1f)
                burrowTime = 105;

            if (attackTimer < burrowTime)
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 14f, 0.04f);
			else
			{
                if (MathHelper.Distance(target.Center.X, npc.Center.X) > 125f)
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, npc.DirectionTo(target.Center).X * 12f, 0.04f);
                if (lungeFallTimer > 145f || target.Center.Y - npc.Center.Y < -1020f)
                    npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -16f, 0.08f);

                // Fall.
                else if (npc.Center.Y < target.Top.Y - 100f && npc.velocity.Y < 21f)
                    npc.velocity.Y += 0.6f;

                // Prepare to fall and play a sound.
                if (lungeFallTimer == 0f && MathHelper.Distance(target.Center.Y, npc.Center.Y) < 825f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DesertScourgeRoar"), target.Center);

                    lungeFallTimer = 1f;
                    npc.netUpdate = true;
				}

                // If the fall timer has been initialized, increment it further.
                if (lungeFallTimer > 0f)
                {
                    // After a certain point, release a bunch of sand into the air.
                    if (Main.netMode != NetmodeID.MultiplayerClient && lungeFallTimer == 40f)
					{
                        for (int i = 0; i < (lifeRatio < 0.1f ? 28 : 16); i++)
						{
                            Vector2 spawnPosition = npc.Center;
                            Vector2 shootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(1.21f) * Main.rand.NextFloat(9f, 11f);
                            spawnPosition += shootVelocity * 2.6f;

                            int sand = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SandBlast>(), 52, 0f);
                            if (Main.projectile.IndexInRange(sand))
                                Main.projectile[sand].tileCollide = false;
						}
					}
                    lungeFallTimer++;
                }
			}
		}

        public static void DoAttack_FallIntoGround(NPC npc, bool inTiles, ref float fallTime, ref float inAirTime)
        {
            fallTime = Utils.Clamp(fallTime - (inTiles ? 3 : 1), 0, 300);
            inAirTime = Utils.Clamp(inAirTime - 2, 0, 180);
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 11f)
                npc.velocity.Y += 0.175f;
        }
        
        public static void DoAttack_FlyTowardsTarget(NPC npc, Player target, float lifeRatio, bool inTiles, ref float inAirTime, ref float fallTime)
        {
            Vector2 destination = target.Center;

            // If close to the target, determine the destination based on the current direction of the worm.
            if (npc.WithinRange(target.Center, 200f))
                destination += npc.velocity.SafeNormalize(Vector2.UnitY) * 250f;

            float distanceFromDestination = npc.Distance(destination);
            float turnSpeed = MathHelper.Lerp(0.007f, 0.065f, Utils.InverseLerp(175f, 475f, distanceFromDestination, true));

            float newSpeed = npc.velocity.Length();
            float idealSpeed = MathHelper.Lerp(5f, 10.5f, 1f - lifeRatio);

            // Accelerate quickly if relatively far from the destination.
            if (distanceFromDestination > 1250f)
                newSpeed += 0.05f;

            // Otherwise slow down if relatively close to the destination.
            if (distanceFromDestination < 300f)
                newSpeed -= 0.04f;

            // Slowly regress back to the ideal speed over time.
            newSpeed = MathHelper.Lerp(newSpeed, idealSpeed, 0.018f);
            newSpeed = MathHelper.Clamp(newSpeed, 4f, 21.5f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), turnSpeed, true) * newSpeed;

            if (!inTiles)
                inAirTime++;

            if (inAirTime >= 180f)
            {
                fallTime = 190f;
                npc.netUpdate = true;
            }
        }

		#endregion Specific Attacks

		#region AI Utility Methods
		public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
		{
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static void SummonSmallerWorms(NPC npc)
		{
            int headType = ModContent.NPCType<DesertScourgeHeadSmall>();
            int bodyType = ModContent.NPCType<DesertScourgeBodySmall>();
            int tailType = ModContent.NPCType<DesertScourgeTailSmall>();

            // Clear the initial worms spawned by the item.
            for (int i = 0; i < Main.maxNPCs; i++)
			{
                int npcType = Main.npc[i].type;
                if (npcType != headType || npcType != bodyType || npcType != tailType || !Main.npc[i].active)
                    continue;

                Main.npc[i].active = false;
                Main.npc[i].netUpdate = true;
			}

            // And respawn them again.
            Point spawnPosition = (npc.Center + Main.rand.NextVector2Circular(60f, 60f)).ToPoint();
            int highAggressionWorm = NPC.NewNPC(spawnPosition.X, spawnPosition.Y, headType, npc.whoAmI);
            if (Main.npc.IndexInRange(highAggressionWorm))
                Main.npc[highAggressionWorm].Infernum().ExtraAI[0] = 0f;

            spawnPosition = (npc.Center + Main.rand.NextVector2Circular(60f, 60f)).ToPoint();
            int sandSpewingWorm = NPC.NewNPC(spawnPosition.X, spawnPosition.Y, headType, highAggressionWorm);
            if (Main.npc.IndexInRange(sandSpewingWorm))
                Main.npc[sandSpewingWorm].Infernum().ExtraAI[0] = 1f;
        }
		#endregion AI Utility Methods

		#endregion AI
	}
}
