using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Systems
{
    public class WorldSaveSystem : ModSystem
    {
        public static bool HasGeneratedProfanedShrine
        {
            get;
            set;
        } = false;

        public static bool HasBeatedInfernumProvRegularly
        {
            get;
            set;
        }

        public static bool HasBeatedInfernumNightProvBeforeDay
        {
            get;
            set;
        }

        public static bool InfernumMode
        {
            get;
            set;
        } = false;

        public static Rectangle ProvidenceArena
        {
            get;
            set;
        } = Rectangle.Empty;

        public static int ProvidenceDoorXPosition
        {
            get;
            set;
        }

        public static bool HasSepulcherAnimationBeenPlayed
        {
            get;
            set;
        }

        public static bool HasProvidenceDoorShattered
        {
            get;
            set;
        }

        // This value is only set to true in new worldgen code. All prior worlds will never naturally have this flag enabled.
        // This is done to allow backwards compatibility with old Abyss worldgen.
        public static bool InPostAEWUpdateWorld
        {
            get;
            set;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            var downed = new List<string>();
            if (InfernumMode)
                downed.Add("InfernumModeActive");
            if (HasGeneratedProfanedShrine)
                downed.Add("HasGeneratedProfanedShrine");
            if (HasBeatedInfernumProvRegularly)
                downed.Add("HasBeatedInfernumProvRegularly");
            if (HasBeatedInfernumNightProvBeforeDay)
                downed.Add("HasBeatedInfernumNightProvBeforeDay");
            if (HasProvidenceDoorShattered)
                downed.Add("HasProvidenceDoorShattered");
            if (HasSepulcherAnimationBeenPlayed)
                downed.Add("HasSepulcherAnimationBeenPlayed");
            if (InPostAEWUpdateWorld)
                downed.Add("InPostAEWUpdateWorld");

            tag["downed"] = downed;
            tag["ProvidenceArenaX"] = ProvidenceArena.X;
            tag["ProvidenceArenaY"] = ProvidenceArena.Y;
            tag["ProvidenceArenaWidth"] = ProvidenceArena.Width;
            tag["ProvidenceArenaHeight"] = ProvidenceArena.Height;
            tag["ProvidenceDoorXPosition"] = ProvidenceDoorXPosition;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            var downed = tag.GetList<string>("downed");
            InfernumMode = downed.Contains("InfernumModeActive");
            HasGeneratedProfanedShrine = downed.Contains("HasGeneratedProfanedShrine");
            HasBeatedInfernumProvRegularly = downed.Contains("HasBeatedInfernumProvRegularly");
            HasBeatedInfernumNightProvBeforeDay = downed.Contains("HasBeatedInfernumNightProvBeforeDay");
            HasProvidenceDoorShattered = downed.Contains("HasProvidenceDoorShattered");
            HasSepulcherAnimationBeenPlayed = downed.Contains("HasSepulcherAnimationBeenPlayed");
            InPostAEWUpdateWorld = downed.Contains("InPostAEWUpdateWorld");

            ProvidenceArena = new(tag.GetInt("ProvidenceArenaX"), tag.GetInt("ProvidenceArenaY"), tag.GetInt("ProvidenceArenaWidth"), tag.GetInt("ProvidenceArenaHeight"));
            ProvidenceDoorXPosition = tag.GetInt("ProvidenceDoorXPosition");
        }
    }
}