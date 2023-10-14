﻿using System.IO;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Cutscene
{
    public class DoGProviCutsceneProjectile : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public ref float JawRotation => ref Projectile.ai[1];

        public static int DoGLifetime => 90;

        public static int TotalLifetime => StartTime + SlowddownTime + ChompTime + AfterHoldTime;

        public static int InitialPortalStartTime => 270;

        public static int InitialPortalEndTime => 360;

        public static int SecondPortalStartTime => 150;

        public static float PortalFadeTime => 20;

        public static int StartTime => InitialPortalStartTime + 60;

        public static int JawOpenTime => 60;

        public static int SlowddownTime => 70;

        public static int ChompTime => 15;

        public static int AfterHoldTime => 50;

        public static int WhiteningWait => 65;

        public static int ZoomInLength => 120;

        public static int RocksDelay => 90;

        public Vector2 DoGHeadPosition
        {
            get;
            set;
        }

        public Vector2 InitialPortalPosition => Projectile.Center + Vector2.UnitX * 650f;

        public Vector2 SecondPortalPosition => Projectile.Center - Vector2.UnitX * 650f;

        public float FirstPortalOpacity => Utils.GetLerpValue(InitialPortalStartTime, InitialPortalStartTime + PortalFadeTime, Timer, true) * Utils.GetLerpValue(InitialPortalEndTime, InitialPortalEndTime - PortalFadeTime, Timer, true);

        private static Projectile myself;

        public static Projectile Myself
        {
            get
            {
                if (myself == null || !myself.active)
                    return null;

                return myself;
            }
            private set => myself = value;
        }

        public static Color TimeColor
        {
            get
            {
                Color timeColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[2], 0.2f);
                if (ProvidenceBehaviorOverride.IsEnraged)
                    timeColor = Color.DeepSkyBlue;
                return timeColor;
            }
        }

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0;
            Projectile.timeLeft = TotalLifetime;
        }

        public override void AI()
        {
            Myself = Projectile;

            if (Timer == 0f)
                JawRotation = 0.05f;

            if (Timer > RocksDelay && Timer < StartTime + WhiteningWait)
            {
                int rockSpawnRate = (int)Lerp(10f, 1f, (Timer - RocksDelay) / (StartTime + WhiteningWait - RocksDelay));

                if (Timer % rockSpawnRate == 0)
                {
                    Vector2 position;
                    do
                    {
                        position = Projectile.Center + Main.rand.NextVector2Circular(600f, 600f);
                    }
                    while (position.WithinRange(Projectile.Center, 200f));

                    int lifeTime = 70;
                    Vector2 velocity = position.DirectionTo(Projectile.Center) * (position.Distance(Projectile.Center) / (lifeTime));
                    ProfanedRockParticle rock = new(position, velocity, Color.White, Main.rand.NextFloat(1.2f, 1.5f), lifeTime, gravity: false, fadeIn: true);
                    GeneralParticleHandler.SpawnParticle(rock);

                    for (int j = 0; j < 3; j++)
                    {
                        position = Projectile.Center + Main.rand.NextVector2Circular(40f, 40f);

                        var fire = new MediumMistParticle(position, Vector2.Zero, TimeColor, Color.Gray, Main.rand.NextFloat(0.8f, 1.2f), 210f);
                        GeneralParticleHandler.SpawnParticle(fire);
                    }
                }
            }

            //if (Timer > InitialPortalStartTime)
            //{
                // Perform zoom effects.
                float zoom = Lerp(0f, 0.45f, (Timer / InitialPortalStartTime).Saturate());
                ZoomSystem.SetZoomEffect(zoom);
            //}

            if (Timer == StartTime)
            {
                SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, Projectile.Center);

                for (int i = 0; i < 70; i++)
                {
                    float scale = Main.rand.NextFloat(0.8f, 1.16f);
                    Color particleColor = Color.Lerp(Color.Fuchsia, Color.Cyan, Main.rand.NextFloat(0.1f, 0.9f));
                    Vector2 particleSpawnOffset = Main.rand.NextVector2Circular(50, 50) * new Vector2(1.75f, 0.75f);
                    Vector2 particleVelocity = particleSpawnOffset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 3f);
                    SquishyLightParticle light = new(InitialPortalPosition + particleSpawnOffset, particleVelocity, scale, particleColor, 60, 1f, 3f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }
            if (Timer > StartTime)
            {
                DoGHeadPosition += Projectile.velocity;

                // Perform whitening effects
                if (Timer > StartTime + WhiteningWait)
                    CeaselessVoidWhiteningEffect.WhiteningInterpolant = Lerp(0f, 1f, ((Timer - StartTime - WhiteningWait) / 5).Saturate());

                // Initially, slow down and open the jaw.
                if (Timer < StartTime + SlowddownTime)
                {
                    Projectile.velocity *= 0.961f;
                    JawRotation = Lerp(0.05f, 0.75f, ((Timer - StartTime) / JawOpenTime).Saturate());

                }
                // Then, lunge forward and bite down.
                else if (Timer <= StartTime + SlowddownTime + ChompTime)
                {
                    float interpolant = (Timer - StartTime - SlowddownTime) / ChompTime;
                    Projectile.velocity = Vector2.Lerp(Vector2.UnitX * -13f, Vector2.Zero, interpolant);
                    JawRotation = Lerp(0.75f, -0.1f, interpolant.Saturate());
                }
            }

            if (Timer == StartTime + SlowddownTime + (int)(ChompTime * 0.5f))
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound, Projectile.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceScreamSound, Projectile.Center);

                if (Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss))
                {
                    Player target = Main.player[Main.npc[CalamityGlobalNPC.holyBoss].target];
                    target.Infernum_Camera().CurrentScreenShakePower = 20f;
                }

                ScreenEffectSystem.SetBlurEffect(Projectile.Center, 1f, 60);
                ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 60);
            }

            if (FirstPortalOpacity > 0 && Timer < StartTime)
            {
                // Spawn portal particles.
                for (int i = 0; i < 3; i++)
                {
                    if (Main.rand.NextFloat() > FirstPortalOpacity)
                        continue;

                    float scale = Main.rand.NextFloat(0.5f, 0.66f);
                    Color particleColor = Color.Lerp(Color.Fuchsia, Color.Cyan, Main.rand.NextFloat(0.1f, 0.9f));
                    Vector2 particleSpawnOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.15f, 1f) * Projectile.scale * 712f;
                    Vector2 particleVelocity = particleSpawnOffset * -0.05f;
                    SquishyLightParticle light = new(InitialPortalPosition + particleSpawnOffset, particleVelocity, scale, particleColor, 40, 1f, 4f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }

            Timer++;
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            DoGHeadPosition = reader.ReadVector2();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(DoGHeadPosition);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the crystal.
            DrawCrystal();

            // Draw DoG.
            if (Timer > InitialPortalStartTime + 60f)
                DrawSegments();

            // Draw the first portal.
            if (FirstPortalOpacity > 0f)
                DrawPortal(InitialPortalPosition - Main.screenPosition, FirstPortalOpacity);

            return false;
        }

        public void DrawBlackOverlays(float opacity)
        {
            DrawCrystal(Color.Black * opacity);

            if (Timer > InitialPortalStartTime + 60f)
            {
                int fullTime = StartTime + SlowddownTime + (int)(ChompTime * 0.5f);

                opacity *= Utils.GetLerpValue(fullTime + 10, fullTime, Timer, true);
                DrawSegments(Color.Black * opacity);
            }
        }

        public float GetSegmentOpacity(float xPosition) => CalamityUtils.Convert01To010(Utils.GetLerpValue(InitialPortalPosition.X + 50, SecondPortalPosition.X - 50, xPosition, true));

        public void DrawCrystal(Color? overrideColor = null)
        {
            float backstuffOpacity = 1f - CeaselessVoidWhiteningEffect.WhiteningInterpolant;

            Texture2D fatCrystalTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceCrystal").Value;
            Texture2D bloomTexture = InfernumTextureRegistry.BloomFlare.Value;

            Main.spriteBatch.Draw(bloomTexture, Projectile.Center - Main.screenPosition, null, TimeColor with { A = 0 } * Lerp(0.3f, 0.6f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly * 1.1f)) * 0.5f) *
                backstuffOpacity, Main.GlobalTimeWrappedHourly, bloomTexture.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(bloomTexture, Projectile.Center - Main.screenPosition, null, Color.Lerp(TimeColor, Color.White, 0.3f) with { A = 0 } *
                Lerp(0.3f, 0.6f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly * 1.4f)) * 0.5f) * backstuffOpacity,
                -Main.GlobalTimeWrappedHourly, bloomTexture.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);

            float crystalScale = Projectile.scale * Lerp(0.9f, 1.1f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly * 0.85f)) * 0.5f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (TwoPi * i / 8f + Main.GlobalTimeWrappedHourly).ToRotationVector2() * Lerp(4f, 10f, (1f + Sin(PI * Main.GlobalTimeWrappedHourly)) * 0.5f);
                Color glowColor = Color.LightPink with { A = 0 } * 0.5f;
                Main.spriteBatch.Draw(fatCrystalTexture, Projectile.Center + offset - Main.screenPosition, null, glowColor * backstuffOpacity, 0f, fatCrystalTexture.Size() * 0.5f, crystalScale, SpriteEffects.None, 0f);
            }

            //Main.spriteBatch.Draw(fatCrystalTexture, Projectile.Center - Main.screenPosition, null, Color.White * backstuffOpacity, 0f, fatCrystalTexture.Size() * 0.5f, crystalScale, SpriteEffects.None, 0f);
            //Main.spriteBatch.Draw(fatCrystalTexture, Projectile.Center - Main.screenPosition, null, timeColor with { A = 0 } * Sin(PI * Main.GlobalTimeWrappedHourly * 0.8f) * 0.7f * backstuffOpacity, 0f, fatCrystalTexture.Size() * 0.5f, crystalScale, SpriteEffects.None, 0f);

            // Draw with a shader if the override color is set.
            //if (overrideColor != null)
            //{
            float threshold = 0.65f;
            if (Timer >= StartTime + SlowddownTime + (int)(ChompTime * 0.5f))
            {
                threshold = 0.53f;
                if (Timer >= StartTime + SlowddownTime + (int)(ChompTime * 0.5f) + AfterHoldTime / 3)
                {
                    threshold = Lerp(threshold, 0.0f, Utilities.EaseInOutCubic(((Timer - StartTime - SlowddownTime - ChompTime / 2 - AfterHoldTime / 3) / (ChompTime / 2 + (AfterHoldTime / 3) * 2)).Saturate()));
                }
            }

            Main.spriteBatch.EnterShaderRegion();
            Effect crack = InfernumEffectsRegistry.CrystalCrackShader.GetShader().Shader;
            crack.Parameters["resolution"]?.SetValue(Utilities.CreatePixelationResolution(fatCrystalTexture.Size()));
            crack.Parameters["threshold"]?.SetValue(threshold);
            Utilities.SetTexture1(InfernumTextureRegistry.WavyNoise.Value);
            crack.CurrentTechnique.Passes[0].Apply();
                //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, crack, Main.GameViewMatrix.TransformationMatrix);
            //}

            Main.spriteBatch.Draw(fatCrystalTexture, Projectile.Center - Main.screenPosition, null, overrideColor ?? Color.White, 0f, fatCrystalTexture.Size() * 0.5f, crystalScale, SpriteEffects.None, 0f);

            //if (overrideColor != null)
                Main.spriteBatch.ExitShaderRegion();
        }

        public void DrawSegments(Color? overrideColor = null)
        {
            Texture2D headTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Head").Value;
            Texture2D glowTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlow").Value;
            Texture2D jawTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw").Value;

            Texture2D bodyTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Body").Value;
            Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow").Value;

            Texture2D tailTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2Tail").Value;
            Texture2D tailGlowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow").Value;

            int segmentCount = 81;
            Vector2 segmentDrawPosition = DoGHeadPosition + InitialPortalPosition + Vector2.UnitX * 250f;
            for (int i = 0; i < segmentCount; i++)
            {
                Texture2D textureToDraw = bodyTexture2Antimatter;
                Texture2D glowmaskToDraw = glowmaskTexture2Antimatter;
                if (i == segmentCount - 1)
                {
                    textureToDraw = tailTexture2Antimatter;
                    glowmaskToDraw = tailGlowmaskTexture2Antimatter;
                }

                segmentDrawPosition += Vector2.UnitX * textureToDraw.Width * 0.8f;
                float segmentOpacity = 1f; /*GetSegmentOpacity(segmentDrawPosition.X) * */ //Utils.GetLerpValue(InitialPortalStartTime + 60 + DoGLifetime, InitialPortalStartTime + DoGLifetime, Timer, true);
                if (segmentOpacity > 0)
                {
                    Main.spriteBatch.Draw(textureToDraw, segmentDrawPosition - Main.screenPosition, null, overrideColor ?? Color.White * segmentOpacity, -PiOver2, textureToDraw.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(glowmaskToDraw, segmentDrawPosition - Main.screenPosition, null, overrideColor ?? Color.White * segmentOpacity, -PiOver2, glowmaskToDraw.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }

            float headOpacity = 1f;//GetSegmentOpacity(DoGHeadPosition.X);
            Vector2 jawOrigin = jawTextureAntimatter.Size() * 0.5f;
            Vector2 jawPositionMain = DoGHeadPosition + InitialPortalPosition + Vector2.UnitX * 250f - Main.screenPosition;
            jawPositionMain -= headTextureAntimatter.Size() * Projectile.scale * 0.5f;
            jawPositionMain += headTextureAntimatter.Size() * 0.5f * Projectile.scale;
            // Draw each jaw.
            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 42f;
                SpriteEffects jawSpriteEffect = SpriteEffects.None;
                if (i == 1)
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;

                float rotation = (-Vector2.UnitY).ToRotation();
                Vector2 jawPosition = jawPositionMain;
                jawPosition += Vector2.UnitX.RotatedBy(rotation + JawRotation * i) * i * (jawBaseOffset + Sin(JawRotation) * 24f);
                jawPosition -= Vector2.UnitY.RotatedBy(rotation) * (58f + Sin(JawRotation) * 30f);
                Main.spriteBatch.Draw(jawTextureAntimatter, jawPosition, null, overrideColor ?? Color.White * 0.7f, rotation + JawRotation * i, jawOrigin, 1f, jawSpriteEffect, 0f);
            }

            Main.spriteBatch.Draw(headTextureAntimatter, DoGHeadPosition + InitialPortalPosition + Vector2.UnitX * 250f - Main.screenPosition, null, overrideColor ?? Color.White * 0.7f * headOpacity, -PiOver2, headTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTextureAntimatter, DoGHeadPosition + InitialPortalPosition + Vector2.UnitX * 250f - Main.screenPosition, null, overrideColor ?? Color.White * headOpacity, -PiOver2, glowTextureAntimatter.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
        }

        public static void DrawPortal(Vector2 portalPosition, float opacity)
        {
            Texture2D noiseTexture = InfernumTextureRegistry.VoronoiShapes.Value;
            Vector2 origin = noiseTexture.Size() * 0.5f;

            Main.spriteBatch.EnterShaderRegion();

            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(new Color(0.2f, 1f, 1f, 0f));
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(new Color(1f, 0.2f, 1f, 0f));
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            Main.spriteBatch.Draw(noiseTexture, portalPosition, null, Color.White, 0f, origin, new Vector2(1.25f, 2.75f) * opacity, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
