using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace AlternateArchonexus
{
    public static class Extensions
    {
        public static QuestPart_Filter SetSignals(this QuestPart_Filter filter, string inSignal = null, string outSignal = null, string outSignalElse = null)
        {
            if (inSignal != null)
                filter.inSignal = inSignal;
            if (outSignal != null)
                filter.outSignal = outSignal;
            if (outSignalElse != null)
                filter.outSignalElse = outSignalElse;

            return filter;
        }

        public static V SetSignals<V>(this V filter, string inSignal = null, string outSignal = null, string outSignalElse = null) where V : QuestPart_Filter
        {
            if (inSignal != null)
                filter.inSignal = inSignal;
            if (outSignal != null)
                filter.outSignal = outSignal;
            if (outSignalElse != null)
                filter.outSignalElse = outSignalElse;

            return filter;
        }

        public static QuestPartActivable SetSignals(this QuestPartActivable activable, string inSignalEnable = null, string inSignalDisable = null, params string[] outSignalsCompleted)
        {
            if (inSignalEnable != null)
                activable.inSignalEnable = inSignalEnable;
            if (inSignalDisable != null)
                activable.inSignalDisable = inSignalDisable;

            foreach (string signal in outSignalsCompleted)
                activable.outSignalsCompleted.Add(signal);

            return activable;
        }

        public static V SetSignals<V>(this V activable, string inSignalEnable = null, string inSignalDisable = null, params string[] outSignalsCompleted) where V : QuestPartActivable
        {
            if (inSignalEnable != null)
                activable.inSignalEnable = inSignalEnable;
            if (inSignalDisable != null)
                activable.inSignalDisable = inSignalDisable;

            foreach (string signal in outSignalsCompleted)
                activable.outSignalsCompleted.Add(signal);

            return activable;
        }
    }
}
