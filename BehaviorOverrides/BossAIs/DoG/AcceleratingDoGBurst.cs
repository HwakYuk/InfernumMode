using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class AcceleratingDoGBurst : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Death Fire");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            // Play a shoot sound.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
            }

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 6 % Main.projFrames[Projectile.type];
            Projectile.Opacity = Utils.GetLerpValue(300f, 285f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 35f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 38f)
                Projectile.velocity *= 1.022f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 90);
            target.AddBuff(BuffID.Frostburn, 90, true);
            target.AddBuff(BuffID.Darkness, 90, true);
        }
        
        public override void Kill(int timeLeft) => SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
    }
}