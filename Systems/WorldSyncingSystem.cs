using System.IO;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Systems.WorldSaveSystem;

namespace InfernumMode.Systems
{
    public class WorldSyncingSystem : ModSystem
    {
        public override void NetSend(BinaryWriter writer)
        {
            BitsByte flags = new();
            flags[0] = WorldSaveSystem.InfernumMode;
            flags[1] = HasBeatedInfernumNightProvBeforeDay;
            flags[2] = HasBeatedInfernumProvRegularly;
            flags[3] = HasProvidenceDoorShattered;
            flags[4] = HasSepulcherAnimationBeenPlayed;
            flags[5] = HasGeneratedProfanedShrine;
            flags[6] = HasGeneratedColosseumEntrance;
            writer.Write(flags);

            writer.Write(ProvidenceArena.X);
            writer.Write(ProvidenceArena.Y);
            writer.Write(ProvidenceArena.Width);
            writer.Write(ProvidenceArena.Height);
            writer.Write(WayfinderGateLocation.X);
            writer.Write(WayfinderGateLocation.Y);
            writer.Write(LostColosseumPortalAnimationTimer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            WorldSaveSystem.InfernumMode = flags[0];
            HasBeatedInfernumNightProvBeforeDay = flags[1];
            HasBeatedInfernumProvRegularly = flags[2];
            HasProvidenceDoorShattered = flags[3];
            HasSepulcherAnimationBeenPlayed = flags[4];
            HasGeneratedProfanedShrine = flags[5];
            HasGeneratedColosseumEntrance = flags[6];

            ProvidenceArena = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

            WayfinderGateLocation = new(reader.ReadSingle(), reader.ReadSingle());
            LostColosseumPortalAnimationTimer = reader.ReadInt32();
        }
    }
}