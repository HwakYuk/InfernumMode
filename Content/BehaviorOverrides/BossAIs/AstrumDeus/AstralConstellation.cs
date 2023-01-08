using CalamityMod.NPCs.AstrumDeus;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.Core.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralConstellation : ModProjectile
    {
        public ref float Index => ref Projectile.ai[0];

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LaserCircle";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Star");

        public override void SetDefaults()
        {
            Projectile.scale = Main.rand?.NextFloat(0.8f, 1f) ?? 1f;
            Projectile.width = Projectile.height = (int)(Projectile.scale * 64f);
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.timeLeft = 900;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()))
                Projectile.active = false;

            if (Projectile.timeLeft < 60)
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0.002f, 0.1f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile projectileToConnectTo = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != Projectile.type || !Main.projectile[i].active ||
                    Main.projectile[i].timeLeft < 25f || Main.projectile[i].ai[0] != Index - 1f ||
                    Main.projectile[i].ai[1] != Projectile.ai[1])
                {
                    continue;
                }

                projectileToConnectTo = Main.projectile[i];
                break;
            }

            float fadeToOrange = Utils.GetLerpValue(50f, 0f, Projectile.timeLeft, true);
            Color cyanColor = new(76, 255, 194);
            Color orangeColor = new(250, 90, 74);
            Color starColor = Color.Lerp(cyanColor, orangeColor, fadeToOrange);

            Texture2D starTexture = TextureAssets.Projectile[Projectile.type].Value;
            float scaleFactor = Utils.GetLerpValue(0f, 15f, Time, true) + Utils.GetLerpValue(30f, 0f, Projectile.timeLeft, true) * 2f;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            for (int i = 0; i < 16; i++)
            {
                float drawOffsetFactor = ((float)Math.Cos(Main.GlobalTimeWrappedHourly * 40f) * 0.5f + 0.5f) * scaleFactor * fadeToOrange * 8f + 1f;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * drawOffsetFactor;
                Main.spriteBatch.Draw(starTexture, drawPosition + drawOffset, null, starColor * 0.4f, 0f, starTexture.Size() * 0.5f, Projectile.scale * scaleFactor, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(starTexture, drawPosition, null, starColor * 4f, 0f, starTexture.Size() * 0.5f, Projectile.scale * scaleFactor, SpriteEffects.None, 0f);

            if (projectileToConnectTo != null)
            {
                Texture2D lineTexture = TextureAssets.Extra[47].Value;
                Vector2 connectionDirection = Projectile.SafeDirectionTo(projectileToConnectTo.Center);
                Vector2 start = Projectile.Center + connectionDirection * Projectile.scale * 24f;
                Vector2 end = projectileToConnectTo.Center - connectionDirection * Projectile.scale * 24f;
                Vector2 scale = new(scaleFactor * 1.5f, (start - end).Length() / lineTexture.Height);
                Vector2 origin = new(lineTexture.Width * 0.5f, 0f);
                Color drawColor = Color.White;
                float rotation = (end - start).ToRotation() - MathHelper.PiOver2;

                Main.spriteBatch.Draw(lineTexture, start - Main.screenPosition, null, drawColor, rotation, origin, scale, SpriteEffects.None, 0f);
            }

            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item91, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 initialVelocity = Vector2.UnitY * 6f;
            if (Projectile.identity % 2f == 1f)
                initialVelocity = initialVelocity.RotatedBy(MathHelper.PiOver2);

            Utilities.NewProjectileBetter(Projectile.Center, -initialVelocity, ModContent.ProjectileType<AstralPlasmaSpark>(), 200, 0f);
            Utilities.NewProjectileBetter(Projectile.Center, initialVelocity, ModContent.ProjectileType<AstralPlasmaSpark>(), 200, 0f);
            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
        }
    }
}