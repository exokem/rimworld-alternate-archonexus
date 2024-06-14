using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AlternateArchonexus
{
    public class QuestPart_SubquestGenerator_AlternateArchonexusVictory : QuestPart_SubquestGenerator
    {
        public Faction civilOutlander, empire, roughTribe, roughOutlander;

        protected override Slate InitSlate()
        {
            Slate slate = new Slate();
            slate.Set("civilOutlander", civilOutlander);
            slate.Set("empire", empire);
            slate.Set("roughTribe", roughTribe);
            slate.Set("roughOutlander", roughOutlander);
            return slate;
        }

        protected override QuestScriptDef GetNextSubquestDef()
        {
            int index = quest.GetSubquests(QuestState.EndedSuccess).Count() % subquestDefs.Count;
            QuestScriptDef questScriptDef = subquestDefs[index];
            if (!questScriptDef.CanRun(InitSlate()))
            {
                return null;
            }
            return questScriptDef;
        }

        public override void Notify_FactionRemoved(Faction faction)
        {
            if (civilOutlander == faction)
                civilOutlander = null;
            else if (empire == faction)
                empire = null;
            else if (roughTribe == faction)
                roughTribe = null;
            else if (roughOutlander == faction)
                roughOutlander = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref civilOutlander, "civilOutlander");
            Scribe_References.Look(ref empire, "empire");
            Scribe_References.Look(ref roughTribe, "roughTribe");
            Scribe_References.Look(ref roughOutlander, "roughOutlander");
        }
    }

    public class QuestPart_AlternateNewColony : QuestPart
    {
        public string inSignal, outSignalCompleted, outSignalCancelled;

        public Faction otherFaction;
        public WorldObjectDef worldObjectDef;

        public bool isFinal;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref otherFaction, "otherFaction");
            Scribe_Values.Look(ref outSignalCompleted, "outSignalCompleted");
            Scribe_Values.Look(ref outSignalCancelled, "outSignalCancelled");
            Scribe_Defs.Look(ref worldObjectDef, "worldObjecctDef");
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            Log.Message("[AX] Signal Received");
            base.Notify_QuestSignalReceived(signal);

            Log.Message($"[AX] {inSignal} : {signal.tag}");

            if (signal.tag != inSignal)
                return;

            Log.Message("[AX] Processing Signal");

            Find.MainTabsRoot.EscapeCurrentTab(playSound: false);
            Find.World.renderer.RegenerateLayersIfDirtyInLongEvent(); // ?

            // Find all maps that belong to the player
            List<Map> playerMaps = new List<Map>();

            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                    playerMaps.Add(map);
            }

            // Display dialog to select map
            Find.WindowStack.Add(new Dialog_ChooseSoldSettlement(PostSellSelection, delegate
            {
                if (!outSignalCancelled.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(outSignalCancelled));
            }){isFinal = isFinal});
        }

        protected virtual void PostSellSelection(Map map)
        {
            Find.WindowStack.Add(new Screen_ArchonexusSettlementCinematics(delegate
            {
                CameraJumper.TryJump(CameraJumper.GetWorldTargetOfMap(map));
            }, delegate
            {
                if (!isFinal)
                {
                    MoveColonyUtility.PickNewColonyTile(TileChosen, delegate
                    {
                        if (!outSignalCancelled.NullOrEmpty())
                        {
                            Find.SignalManager.SendSignal(new Signal(outSignalCancelled));
                        }
                    });
                }
                
                SellSettlement(map);
                ScreenFader.StartFade(Color.clear, 2f);
            }));
        }

        protected virtual void TileChosen(int chosenTile)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.MusicManagerPlay.ForceFadeoutAndSilenceFor(120f);

                WorldObjectDef worldObjectClarified = worldObjectDef ?? WorldObjectDefOf.Settlement;

                // Establish the new settlement
                Settlement settlement = WorldObjectMaker.MakeWorldObject(worldObjectClarified) as Settlement;
                settlement.SetFaction(Faction.OfPlayer);
                settlement.Tile = chosenTile;
                settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, Faction.OfPlayer.def.playerInitialSettlementNameMaker);
                Find.WorldObjects.Add(settlement);
                Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, worldObjectClarified);

                CameraJumper.TryJump(MapGenerator.PlayerStartSpot, settlement.Map);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                if (!outSignalCompleted.NullOrEmpty())
                {
                    Find.SignalManager.SendSignal(new Signal(outSignalCompleted));
                }
            }, "GeneratingMap", doAsynchronously: false, null);
        }

        protected void SellSettlement(Map map)
        {
            // End quests that should probably end at this point
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest.IsEndOnNewArchonexusSettlement())
                {
                    quest.hidden = true;
                    quest.End(QuestEndOutcome.Unknown, sendLetter: false);
                }
            }
            
            List<Pawn> pawnsToRemove = new List<Pawn>();

            foreach (Pawn pawn in map.mapPawns.SpawnedColonyAnimals)
                pawnsToRemove.Add(pawn);

            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                // Clear colonists and prisoners that are not within any caravans
                if ((pawn.IsColonist || pawn.IsPrisonerOfColony) && !pawn.IsCaravanMember())
                    pawnsToRemove.Add(pawn);
            }

            foreach (Pawn pawn in pawnsToRemove)
            {
                if (pawn.Spawned)
                    pawn.DeSpawn();
                    
                if (!pawn.IsWorldPawn())
                    Find.WorldPawns.PassToWorld(pawn);
            }
            
            map.Parent.SetFaction(null);
            Current.Game.DeinitAndRemoveMap(map);
            map.Parent.Destroy();

            if (otherFaction != null)
                SettleUtility.AddNewHome(map.Tile, otherFaction);
        }
    }
}
