using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using System.Collections;
using Verse;

namespace AlternateArchonexus
{
    public class AscendantPawnData : WorldComponent
    {
        private HashSet<Pawn> archivedPawns = new HashSet<Pawn>();

        public AscendantPawnData(World world) : base(world)
        {

        }

        public void AscendPawn(Pawn pawn) => archivedPawns.Add(pawn);

        public override void ExposeData()
        {
            base.ExposeData();
            foreach (var entry in archivedPawns)
                Scribe_Collections.Look(ref archivedPawns, "archivedPawns", LookMode.Deep);
        }
    }
}
