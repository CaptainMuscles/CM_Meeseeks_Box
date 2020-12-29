using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;
using Verse.Sound;

namespace CM_Meeseeks_Box
{
    public static class WorkerDefUtility
    {
        // Borrowed from Achtung: getting list of relevant WorkGiverDefs
        private static List<WorkGiverDef> AllWorkerDefs<T>() where T : class
        {
            try
            {
                List<WorkGiverDef> workGiverDefs = DefDatabase<WorkGiverDef>.AllDefsListForReading.Where(def => def.giverClass != null && (def.Worker as T) != null).ToList();
                Log.Message("Returning " + workGiverDefs.Count + " defs for " + typeof(T).ToString());
                return workGiverDefs;
            }
            catch (Exception ex)
            {
                Log.Error($"CM_Meeseeks_Box cannot fetch a list of WorkGiverDefs for {typeof(T).FullName}: {ex}");
                return new List<WorkGiverDef>();
            }
        }

        public static readonly List<WorkGiverDef> constructionDefs = AllWorkerDefs<WorkGiver_ConstructDeliverResources>().Concat(AllWorkerDefs<WorkGiver_ConstructFinishFrames>()).ToList();
        public static List<WorkGiverDef> GetCombinedDefs(WorkGiver baseWorkgiver)
        {
            if (constructionDefs.Contains(baseWorkgiver.def))
                return constructionDefs.ToList();

            if (baseWorkgiver.def.giverClass != null && (baseWorkgiver.def.Worker as WorkGiver_Warden) != null)
                return AllWorkerDefs<WorkGiver_Warden>();

            return new List<WorkGiverDef> { baseWorkgiver.def };
        }
        // ***
    }
}
