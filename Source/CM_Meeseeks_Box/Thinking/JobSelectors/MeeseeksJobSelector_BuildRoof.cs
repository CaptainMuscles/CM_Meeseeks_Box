using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace CM_Meeseeks_Box
{
    public class MeeseeksJobSelector_BuildRoof : MeeseeksJobSelector
    {
        public override bool UseForJob(Pawn meeseeks, CompMeeseeksMemory memory, SavedJob savedJob)
        {
            return savedJob.UsesWorkGiver<WorkGiver_BuildRoof>();
        }

        public override Job GetJob(Pawn meeseeks, CompMeeseeksMemory memory, SavedJob savedJob, SavedTargetInfo jobTarget, ref JobAvailability jobAvailabilty)
        {
            Job job = null;
            bool wantToBuild = savedJob.UsesWorkGiver<WorkGiver_BuildRoof>();
            IntVec3 cell = jobTarget.Cell;
            Map map = meeseeks.MapHeld;

            if (jobTarget != null)
            {
                bool roofedHere = cell.Roofed(map);
                bool inNoRoofZone = map.areaManager.NoRoof[cell];
                bool inYesRoofZone = map.areaManager.BuildRoof[cell];

                if (wantToBuild && !inNoRoofZone)
                {
                    bool withinRoofRange = RoofCollapseUtility.WithinRangeOfRoofHolder(cell, map);
                    bool connectedToRoof = RoofCollapseUtility.ConnectedToRoofHolder(cell, map, assumeRoofAtRoot: true);

                    if (withinRoofRange && connectedToRoof && !roofedHere)
                        job = ScanForJob(meeseeks, memory, savedJob, jobTarget, ref jobAvailabilty);
                }
            }
            else
            {
                Logger.WarningFormat(this, "Unable to get scanner or target for job.");
            }

            if (job == null)
            {
                Logger.MessageFormat(this, "Checking for nearby roof tiles");
                foreach (IntVec3 nearCell in GenAdjFast.AdjacentCellsCardinal(cell))
                {
                    Logger.MessageFormat(this, "Checking cell {0}", nearCell);

                    if (wantToBuild && CellWantsRoofBuilt(nearCell, map))
                    {
                        memory.AddJobTarget(nearCell);
                        Logger.MessageFormat(this, "Marking cell {0} to build roof", cell);
                    }
                }
            }

            if (job != null)
                jobAvailabilty = JobAvailability.Available;

            return job;
        }

        private bool CellWantsRoofBuilt(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map))
                return false;

            bool roofedHere = cell.Roofed(map);
            bool inNoRoofZone = map.areaManager.NoRoof[cell];

            return !roofedHere && !inNoRoofZone;
        }

        private bool CellWantsRoofRemoved(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map))
                return false;

            bool roofedHere = cell.Roofed(map);
            bool inYesRoofZone = map.areaManager.BuildRoof[cell];

            return roofedHere && !inYesRoofZone;
        }
    }
}
