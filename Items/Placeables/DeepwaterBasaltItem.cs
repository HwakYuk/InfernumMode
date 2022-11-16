﻿using InfernumMode.Tiles.Abyss;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.Placeables
{
    public class DeepwaterBasaltItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Deepwater Basalt");
            SacrificeTotal = 100;
        }

        public override void SetDefaults()
        {
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.width = 16;
            Item.height = 16;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.maxStack = 999;
            Item.createTile = ModContent.TileType<DeepwaterBasalt>();
            Item.useStyle = ItemUseStyleID.Swing;
        }
    }
}
