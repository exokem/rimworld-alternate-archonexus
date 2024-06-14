using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using RimWorld.QuestGen;

namespace AlternateArchonexus
{
    public static class QuestLimits
    {
        public const int AllowedDefault = 999;

        public const int ElementsAllowed = 999;
        public const float WealthRequired = 350000F;
        public const int Interval = 3600000;

        public static int AllowedColonists { get; set; } = AllowedDefault;
        public static int AllowedAnimals { get; set; } = AllowedDefault;
        public static int AllowedRelics { get; set; } = AllowedDefault;
        public static int AllowedItems { get; set; } = AllowedDefault;
    }

    public class QuestNode_Root_AlternateArchonexusVictory : QuestNode
    {
    	public Faction CivilOutlander => Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.def == FactionDefOf.OutlanderCivil).FirstOrDefault();

        public Faction Empire => Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.def == FactionDefOf.Empire).FirstOrDefault();

        public Faction RoughOutlander => Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.def == FactionDefOf.OutlanderRough).FirstOrDefault();

        public Faction RoughTribe => Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.def == FactionDefOf.TribeRough).FirstOrDefault();
        
        protected override void RunInt()
        {
            if (!ModLister.CheckIdeology("Archonexus victory"))
            {
                Log.Warning("[AX] Failed Ideology Check");
                return;
            }

            Log.Warning("[AX] Running Quest");

            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            quest.AddPart(new QuestPart_SubquestGenerator_AlternateArchonexusVictory
            {
                inSignalEnable = slate.Get<string>("inSignal"),
                interval = new IntRange(0, 0),
                maxSuccessfulSubquests = 3,
                maxActiveSubquests = 1,
                civilOutlander = CivilOutlander,
                empire = Empire,
                roughOutlander = RoughOutlander,
                roughTribe = RoughTribe,
                subquestDefs =
                {
                    DefDatabase<QuestScriptDef>.GetNamed("EndGame_AlternateArchonexusVictory_FirstCycle"),
                    DefDatabase<QuestScriptDef>.GetNamed("EndGame_AlternateArchonexusVictory_SecondCycle"),
                    DefDatabase<QuestScriptDef>.GetNamed("EndGame_AlternateArchonexusVictory_ThirdCycle")
                }
            });
        }

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }

    public abstract class QuestNode_Root_AlternateArchonexusVictory_Cycle : QuestNode
    {
        protected Map map;

        protected abstract int Cycle { get; }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            map = slate.Get("map", QuestGen_Get.GetMap());

            string signalWealthSatisfied = QuestGen.GenerateNewSignal("PlayerWealthSatisfied");
            string signalLetterReminder = QuestGen.GenerateNewSignal("SendLetterReminder");
            string signalOuterNodeCompleted = QuestGen.GenerateNewSignal("OuterNodeCompleted");

            // QuestGen.GenerateNewSignal("ActivateLetterReminderSignal");

            QuestPart_RequirementsToAcceptPlayerWealth partAcceptRequirements = new QuestPart_RequirementsToAcceptPlayerWealth()
            {
                requiredPlayerWealth = QuestLimits.WealthRequired
            };

            QuestPart_PlayerWealth partPlayerWealth = new QuestPart_PlayerWealth()
            {
                inSignalEnable = quest.AddedSignal,
                playerWealth = QuestLimits.WealthRequired,
                signalListenMode = QuestPart.SignalListenMode.NotYetAcceptedOnly
            };
            partPlayerWealth.outSignalsCompleted.Add(signalWealthSatisfied);
            
            QuestPart_PassOutInterval partReminder = new QuestPart_PassOutInterval()
            {
                signalListenMode = QuestPart.SignalListenMode.NotYetAcceptedOnly,
                inSignalEnable = signalWealthSatisfied,
                ticksInterval = new IntRange(3600000, 3600000)
            };
            partReminder.outSignals.Add(signalLetterReminder);
            
            QuestPart_Filter_PlayerWealth partWealthFilter = new QuestPart_Filter_PlayerWealth()
            {
                minPlayerWealth = QuestLimits.WealthRequired,
                inSignal = signalLetterReminder,
                outSignal = signalOuterNodeCompleted,
                signalListenMode = QuestPart.SignalListenMode.NotYetAcceptedOnly
            };

            quest.AddPart(partAcceptRequirements);
            quest.AddPart(partPlayerWealth);
            quest.AddPart(partReminder);
            quest.AddPart(partWealthFilter);

            quest.CanAcceptQuest(delegate
            {
                QuestNode_ResolveQuestName.Resolve();
                string name = slate.Get<string>("resolvedQuestName");

                quest.Letter
                (
                    LetterDefOf.PositiveEvent, null, null, null, null, 
                    useColonistsFromCaravanArg: false, 
                    QuestPart.SignalListenMode.NotYetAcceptedOnly, null, 
                    filterDeadPawnsFromLookTargets: false, 
                    label: "LetterLabelArchonexusWealthReached".Translate(name), 
                    text: "LetterTextArchonexusWealthReached".Translate(name)
                );
            }, null, partWealthFilter.outSignal, null, null, QuestPart.SignalListenMode.NotYetAcceptedOnly);

            quest.RewardChoice().choices.Add(new QuestPart_Choice.Choice
            {
                rewards = { new Reward_ArchonexusMap()
                {
                    currentPart = Cycle
                }}
            });

            List<Map> maps = Find.Maps;
            List<MapParent> list = Find.Maps.Where(map => map.IsPlayerHome).Select(map => map.Parent).ToList();

            // Store quest data
            slate.Set("playerSettlements", list);
            slate.Set("playerSettlementsCount", list.Count);
            slate.Set("colonistsAllowed", QuestLimits.AllowedColonists);
            slate.Set("animalsAllowed", QuestLimits.AllowedAnimals);
            slate.Set("requiredWealth", QuestLimits.WealthRequired);
            slate.Set("map", this.map);
            slate.Set("mapParent", this.map.Parent);
        }

        protected void PickNewColony(Faction takeOverFaction, WorldObjectDef worldObjectDef, SoundDef colonyStartSoundDef, bool isFinal = false)
        {
            Log.Message("[AX] Selection Triggered");

            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            string text = QuestGen.GenerateNewSignal("NewColonyCreated");
            string text2 = QuestGen.GenerateNewSignal("NewColonyCancelled");
            quest.AddPart(new QuestPart_AlternateNewColony
            {
                inSignal = slate.Get<string>("inSignal"),
                otherFaction = takeOverFaction,
                outSignalCompleted = text,
                outSignalCancelled = text2,
                worldObjectDef = worldObjectDef,
                isFinal = isFinal
            });
            quest.SetQuestNotYetAccepted(text2, revertOngoingQuestAfterLoad: true);
            quest.End(QuestEndOutcome.Success, 0, null, text);
        }

        protected void TryAddStudyRequirement(Quest quest, Slate slate, ThingDef buildingToStudyDef)
        {
            Thing thing = map.listerThings.ThingsOfDef(buildingToStudyDef).FirstOrDefault();
            if (thing != null)
            {
                slate.Set("archonexusMajorStructure", thing);
                slate.Set("studyRequirement", var: true);
                quest.Letter(LetterDefOf.PositiveEvent, QuestGenUtility.HardcodedSignalWithQuestID("archonexusMajorStructure.Researched"), null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.NotYetAcceptedOnly, null, filterDeadPawnsFromLookTargets: false, label: "LetterLabelArchonexusStructureResearched".Translate(thing), text: "LetterTextArchonexusStructureResearched".Translate(thing));
                QuestPart_RequirementsToAcceptThingStudied_ArchotechStructures questPart_RequirementsToAcceptThingStudied_ArchotechStructures = new QuestPart_RequirementsToAcceptThingStudied_ArchotechStructures();
                questPart_RequirementsToAcceptThingStudied_ArchotechStructures.thing = thing;
                quest.AddPart(questPart_RequirementsToAcceptThingStudied_ArchotechStructures);
            }
            else
            {
                slate.Set("studyRequirement", var: false);
            }
        }

        protected override bool TestRunInt(Slate slate)
        {
            if (QuestGen_Get.GetMap() == null)
            {
                Log.Message("Missing map");
                return false;
            }

            return true;
        }

        protected bool HasFaction(string faction, Slate slate)
        {
            return GetFaction(faction, slate) != null;
        }

        protected Faction GetFaction(string faction, Slate slate)
        {
            return slate.Get<Faction>(faction);
        }

        protected Faction TryGetNextFaction(string slateKey, Slate slate)
        {
            Faction faction = slate.Get<Faction>(slateKey);

            if (faction == null)
            {
                Faction outlander = slate.Get<Faction>("civilOutlander");
                if (outlander != null)
                    return outlander;
                Faction empire = slate.Get<Faction>("empire");
                if (empire != null)
                    return empire;
                Faction roughTribe = slate.Get<Faction>("roughTribe");
                if (roughTribe != null)
                    return roughTribe;
                Faction roughOutlander = slate.Get<Faction>("roughOutlander");
                if (roughOutlander != null)
                    return roughOutlander;
            }

            return faction;
        }

        protected void StoreFactionFlags(Slate slate)
        {
            slate.Set("hasCivilOutlander", HasFaction("civilOutlander", slate));
            slate.Set("hasEmpire", HasFaction("empire", slate));
            slate.Set("hasRoughOutlander", HasFaction("roughOutlander", slate));
            slate.Set("hasRoughTribe", HasFaction("roughTribe", slate));
        }
    }

    public class QuestNode_Root_AlternateArchonexusVictory_FirstCycle : QuestNode_Root_AlternateArchonexusVictory_Cycle
    {
        protected override int Cycle => 1;
        protected override void RunInt()
        {
            base.RunInt();
            Log.Message("[AX] Running First Cycle");
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Faction faction = TryGetNextFaction("civilOutlander", slate);
            if (faction != null)
            {
                quest.RequirementsToAcceptFactionRelation(faction, FactionRelationKind.Ally, acceptIfDefeated: true);
            }
            PickNewColony(faction, WorldObjectDefOf.Settlement_SecondArchonexusCycle, SoundDefOf.GameStartSting_FirstArchonexusCycle);
            slate.Set("factionless", faction == null);
            StoreFactionFlags(slate);
        }
    }

    public class QuestNode_Root_AlternateArchonexusVictory_SecondCycle : QuestNode_Root_AlternateArchonexusVictory_Cycle
    {
        protected override int Cycle => 2;
        protected override void RunInt()
        {
            base.RunInt();
            Log.Message("[AX] Running Second Cycle");
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            TryAddStudyRequirement(quest, slate, ThingDefOf.MajorArchotechStructureStudiable);
            quest.DialogWithCloseBehavior("[resolvedQuestDescription]", null, quest.AddedSignal, null, null, QuestPart.SignalListenMode.NotYetAcceptedOnly, QuestPartDialogCloseAction.CloseActionKey.ArchonexusVictorySound2nd);
            Faction faction = TryGetNextFaction("roughOutlander", slate);
            if (faction != null)
            {
                quest.RequirementsToAcceptFactionRelation(faction, FactionRelationKind.Ally, acceptIfDefeated: true);
            }
            PickNewColony(faction, WorldObjectDefOf.Settlement_ThirdArchonexusCycle, SoundDefOf.GameStartSting_SecondArchonexusCycle);
            slate.Set("factionless", faction == null);
            StoreFactionFlags(slate);
        }
    }

    public class QuestNode_Root_AlternateArchonexusVictory_ThirdCycle : QuestNode_Root_AlternateArchonexusVictory_Cycle
    {
        private const int MinDistanceFromColony = 10;

        private const int MaxDistanceFromColony = 40;

        private static float ThreatPointsFactor = 0.6f;

        protected override int Cycle => 3;

        protected override void RunInt()
        {
            base.RunInt();
            Log.Message("[AX] Running Third Cycle");
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            float num = slate.Get("points", 0f);
            Faction faction = TryGetNextFaction("empire", slate);
            TryFindSiteTile(out var tile);

            if (faction != null)
                quest.RequirementsToAcceptFactionRelation(faction, FactionRelationKind.Ally, acceptIfDefeated: true);

            TryAddStudyRequirement(quest, slate, ThingDefOf.GrandArchotechStructure);
            quest.DialogWithCloseBehavior("[questDescriptionBeforeAccepted]", null, quest.AddedSignal, null, null, QuestPart.SignalListenMode.NotYetAcceptedOnly, QuestPartDialogCloseAction.CloseActionKey.ArchonexusVictorySound3rd);
            quest.DescriptionPart("[questDescriptionBeforeAccepted]", quest.AddedSignal, quest.InitiateSignal, QuestPart.SignalListenMode.OngoingOrNotYetAccepted);
            quest.DescriptionPart("[questDescriptionAfterAccepted]", quest.InitiateSignal, null, QuestPart.SignalListenMode.OngoingOrNotYetAccepted);
            quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, null, filterDeadPawnsFromLookTargets: false, "[questAcceptedLetterText]", null, "[questAcceptedLetterLabel]");
            float threatPoints = (Find.Storyteller.difficulty.allowViolentQuests ? (num * ThreatPointsFactor) : 0f);
            SitePartParams parms = new SitePartParams
            {
                threatPoints = threatPoints
            };
            Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle(new SitePartDefWithParams(SitePartDefOf.Archonexus, parms)), tile, Faction.OfAncients);
            if (num <= 0f && Find.Storyteller.difficulty.allowViolentQuests)
            {
                quest.SetSitePartThreatPointsToCurrent(site, SitePartDefOf.Archonexus, map.Parent, null, ThreatPointsFactor);
            }
            quest.SpawnWorldObject(site);
            PickNewColony(faction, WorldObjectDefOf.Settlement_ThirdArchonexusCycle, SoundDefOf.GameStartSting, isFinal: true);
            slate.Set("factionless", faction == null);
            slate.Set("threatsEnabled", Find.Storyteller.difficulty.allowViolentQuests);
            StoreFactionFlags(slate);
        }

        private bool TryFindSiteTile(out int tile, bool exitOnFirstTileFound = false)
        {
            return TileFinder.TryFindNewSiteTile(out tile, 10, 40, allowCaravans: false, TileFinderMode.Near, -1, exitOnFirstTileFound);
        }

        protected override bool TestRunInt(Slate slate)
        {
            int tile;
            if (base.TestRunInt(slate))
            {
                return TryFindSiteTile(out tile, exitOnFirstTileFound: true);
            }
            return false;
        }
    }
}
