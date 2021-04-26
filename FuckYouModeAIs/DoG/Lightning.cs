﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.FuckYouModeAIs.DoG
{
    public class Lightning : ModProjectile
    {
        public int telegraphTimer = 80;
        public float angle = 0f;
        public Vector2 targetPosition;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning");
        }

        public override void SetDefaults()
        {
            projectile.width = 14;
            projectile.height = 14;
            projectile.aiStyle = 88;
            projectile.hostile = true;
            projectile.alpha = byte.MaxValue;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.extraUpdates = 4;
            projectile.timeLeft = 120 * (projectile.extraUpdates + 1);
            telegraphTimer *= projectile.extraUpdates;
        }

        public override void AI()
        {
            if (telegraphTimer == 79)
            {
                targetPosition = Main.player[Player.FindClosest(projectile.Center, 1, 1)].Center;
                angle = Main.rand.NextFloat(MathHelper.ToRadians(-15f), MathHelper.ToRadians(15f));
            }
            if (telegraphTimer == 1)
            {
                if (projectile.ai[0] == 0f)
                {
                    projectile.velocity = projectile.DirectionTo(targetPosition) * 9f;
                }
                else
                {
                    projectile.velocity = (projectile.DirectionTo(targetPosition) * 9f).RotatedBy(angle);
                }
                Main.projectile[Projectile.NewProjectile(projectile.Center, projectile.velocity, ProjectileID.CultistBossLightningOrbArc, 85, 0f, projectile.owner,
                    projectile.velocity.ToRotation(), Main.rand.Next(100))].tileCollide = false;
                projectile.Kill();
            }
            telegraphTimer--;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (var i = 0; i < projectile.oldPos.Length && (projectile.oldPos[i].X != 0.0 || projectile.oldPos[i].Y != 0.0); i++)
            {
                projHitbox.X = (int)projectile.oldPos[i].X;
                projHitbox.Y = (int)projectile.oldPos[i].Y;
                if (projHitbox.Intersects(targetHitbox))
                    return true;
            }
            return false;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (telegraphTimer < 79)
                spriteBatch.DrawLineBetter(projectile.Center, projectile.Center + (projectile.AngleTo(targetPosition) + angle).ToRotationVector2() * 5000f, Color.Cyan, 3f);
        }
    }
}
