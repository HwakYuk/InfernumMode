using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class PlasmaBomb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Bomb");
            Main.projFrames[projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 36;
            projectile.height = 36;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 270;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(270f, 265f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 35f, projectile.timeLeft, true);

            // Emit light.
            Lighting.AddLight(projectile.Center, 0.1f * projectile.Opacity, 0.25f * projectile.Opacity, 0.25f * projectile.Opacity);

            // Handle frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            // Create a burst of dust on the first frame.
            if (projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 60; i++)
                {
                    Dust plasma = Dust.NewDustPerfect(projectile.Center, Main.rand.NextBool() ? 110 : 107);
                    plasma.position += Main.rand.NextVector2Circular(20f, 20f);
                    plasma.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 16f);
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Lime * 0.6f;
                    plasma.scale *= Main.rand.NextFloat(1.5f, 2f);
                    plasma.noGravity = true;
                }
                projectile.localAI[0] = 1f;
            }

            // Slow down over time.
            projectile.velocity *= 0.98f;
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.CursedInferno, 300);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], Color.White * projectile.Opacity, 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item93, projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into electric sparks on death.
            for (int i = 0; i < 7; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 7f).ToRotationVector2() * 6f;
                Utilities.NewProjectileBetter(projectile.Center, sparkVelocity, ModContent.ProjectileType<TypicalPlasmaSpark>(), 500, 0f);
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
