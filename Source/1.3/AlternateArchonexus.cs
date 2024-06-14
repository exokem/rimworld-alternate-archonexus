using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace AlternateArchonexus
{
    [StaticConstructorOnStartup]
    public static class AlternateArchonexus
    {
        static AlternateArchonexus()
        {
            Harmony harmony = new Harmony("rimworld.mod.exokem.alternatearchonexus");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class ArchonexusCountdown_Patch
    {
        [HarmonyPatch(typeof(ArchonexusCountdown), nameof(ArchonexusCountdown.ArchonexusCountdownUpdate))]
        public static bool Prefix(ref float ___timeLeft, ref Building_ArchonexusCore ___archonexusCoreRoot)
        {
            if (0F < ___timeLeft)
            {
                ___timeLeft -= Time.deltaTime;
                if (___timeLeft <= 0)
                    AlternateEndGame(___archonexusCoreRoot);
            }

            // Skip original
            return false;
        }

        private static void AlternateEndGame(Building_ArchonexusCore archonexusCoreRoot)
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<Pawn> list = (from p in archonexusCoreRoot.Map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                where p.RaceProps.Humanlike
                select p).ToList();
            foreach (Pawn item in list)
            {
                if (!item.Dead && !item.IsQuestLodger())
                {
                    stringBuilder.AppendLine("   " + item.LabelCap);
                    Find.StoryWatcher.statsRecord.colonistsLaunched++;
                }

                if (item.Spawned)
                    item.DeSpawn();

                item.GetUniqueLoadID();
                Find.World.GetComponent<AscendantPawnData>().AscendPawn(item);
                    
                // if (!item.IsWorldPawn())
                //     Find.WorldPawns.PassToWorld(item);
            }
            GameVictoryUtility.ShowCredits(GameVictoryUtility.MakeEndCredits("GameOverArchotechInvokedIntro".Translate(), "GameOverArchotechInvokedEnding".Translate(), stringBuilder.ToString(), "GameOverColonistsTranscended", list), SongDefOf.ArchonexusVictorySong, exitToMainMenu: false, 2.5f);
        }
    }
}