﻿using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using System;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    public class Designator_AreaWorkMeeseeks : Designator
    {
        private Pawn Meeseeks = null;
        private CompMeeseeksMemory Memory = null;

        public override int DraggableDimensions => 2;

        public static readonly Material DragHighlightCellMat = MaterialPool.MatFrom("UI/Overlays/DragHighlightCell", ShaderDatabase.MetaOverlay);
        public static readonly Material DragHighlightThingMat = MaterialPool.MatFrom("UI/Overlays/DragHighlightThing", ShaderDatabase.MetaOverlay);

        private Dictionary<IntVec3, bool> cachedCellResults = new Dictionary<IntVec3, bool>();
        private Dictionary<Thing, bool> cachedThingResults = new Dictionary<Thing, bool>();

        public Designator_AreaWorkMeeseeks(Pawn mrMeeseeksLookAtMe)
        {
            defaultLabel = "CM_Meeseeks_Box_SelectJobAreaLabel".Translate();
            defaultDesc = "CM_Meeseeks_Box_SelectJobAreaDescription".Translate();
            icon = ContentFinder<Texture2D>.Get("Icons/SelectTargets");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Harvest;
            Meeseeks = mrMeeseeksLookAtMe;
            Memory = Meeseeks.GetComp<CompMeeseeksMemory>();

            hasDesignateAllFloatMenuOption = true;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 cell)
        {
            if (!cell.InBounds(base.Map))
            {
                return false;
            }

            bool success = false;

            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    if (workGiverScanner != null)
                    {
                        bool prepared = false;
                        try
                        {
                            prepared = PrepareDesignations(savedJob, cell);

                            success = this.HasJobOnCell(workGiverScanner, cell);
                            cachedCellResults[cell] = success;

                            if (!success)
                            {
                                foreach (Thing thing in cell.GetThingList(base.Map))
                                {
                                    success = HasJobOnThing(workGiverScanner, thing);
                                    cachedThingResults[thing] = success;

                                    if (success)
                                        break;
                                }
                            }
                        }
                        finally
                        {
                            if (prepared)
                                RestoreDesignations(cell);
                        }
                    }
                }
            }

            return success;
        }

        private bool PrepareDesignations(SavedJob savedJob, IntVec3 cell)
        {
            bool result = false;

            // Holy shit the things I do for plants - checking for plant cutting jobs that match normal designations - chop wood, harvest plants, cut plants
            if (savedJob.def.driverClass.IsSubclassOf(typeof(JobDriver_PlantWork)))
            {
                if (Memory.jobTargets.Count > 0 && Memory.jobTargets[0].HasThing)
                {
                    Plant plant = Memory.jobTargets[0].Thing as Plant;
                    if (plant != null)
                    {
                        if (plant.HarvestableNow)
                        {
                            if (plant.def.plant.IsTree)
                            {
                                DesignatorUtility.ForceDesignationOnThingsInCell(cell, base.Map, DesignationDefOf.HarvestPlant, ((Func<Thing, bool>)((Thing thing) => thing.def.plant?.IsTree ?? false)));
                                //Logger.MessageFormat(this, "It's a tree harvest job...");
                            }
                            else
                            {
                                DesignatorUtility.ForceDesignationOnThingsInCell(cell, base.Map, DesignationDefOf.HarvestPlant, ((Func<Thing, bool>)((Thing thing) => !thing.def.plant?.IsTree ?? false)));
                                //Logger.MessageFormat(this, "It's a plant harvest job...");
                            }

                            result = true;
                        }
                        else
                        {
                            DesignatorUtility.ForceDesignationOnThingsInCell(cell, base.Map, DesignationDefOf.CutPlant, ((Func<Thing, bool>)((Thing thing) => thing.def.plant != null)));
                            //Logger.MessageFormat(this, "It's a plant cut job...");
                            result = true;
                        }
                    }
                }
            }
            else
            {
                DesignatorUtility.ForceAllDesignationsOnCell(cell, base.Map);
                result = true;
            }

            return result;
        }

        private void RestoreDesignations(IntVec3 cell)
        {
            DesignatorUtility.RestoreDesignationsOnCell(cell, base.Map);
        }

        public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            bool success = false;

            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    if (workGiverScanner != null)
                    {
                        bool prepared = false;
                        try
                        {
                            prepared = PrepareDesignations(savedJob, thing.PositionHeld);

                            if (HasJobOnThing(workGiverScanner, thing))
                                success = true;
                        }
                        finally
                        {
                            if (prepared)
                                RestoreDesignations(thing.PositionHeld);
                        }
                    }
                }
            }

            return success;
        }

        public override void DesignateSingleCell(IntVec3 cell)
        {
            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    if (workGiverScanner != null)
                    {
                        if (cachedCellResults.ContainsKey(cell) && cachedCellResults[cell] == true)
                            Memory.AddJobTarget(cell, workGiverScanner.def);

                        foreach (Thing thing in cell.GetThingList(base.Map))
                        {
                            if (cachedThingResults.ContainsKey(thing) && cachedThingResults[thing] == true)
                                DesignateThing(thing);
                        }
                    }
                }
            }
        }

        public override void DesignateThing(Thing thing)
        {
            bool didIt = false;
            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    if (workGiverScanner != null)
                    {
                        Memory.AddJobTarget(thing, workGiverScanner.def);
                        didIt = true;
                    }
                }
            }

            if (!didIt)
                Memory.AddJobTarget(thing);
        }

        private bool HasJobOnCell(WorkGiver_Scanner workGiverScanner, IntVec3 cell)
        {
            List<WorkGiver_Scanner> workGivers = WorkerDefUtility.GetCombinedWorkGiverScanners(workGiverScanner);

            foreach (WorkGiver_Scanner scanner in workGivers)
            {
                var potentialWorkCells = scanner.PotentialWorkCellsGlobal(Meeseeks);
                if ((potentialWorkCells == null || potentialWorkCells.Contains(cell)) && !Memory.jobTargets.Contains(cell) && scanner.HasJobOnCell(Meeseeks, cell, true))
                    return true;
            }

            return false;
        }

        private bool HasJobOnThing(WorkGiver_Scanner workGiverScanner, Thing thing)
        {
            List<WorkGiver_Scanner> workGivers = WorkerDefUtility.GetCombinedWorkGiverScanners(workGiverScanner);

            foreach (WorkGiver_Scanner scanner in workGivers)
            {
                var potentialWorkThings = scanner.PotentialWorkThingsGlobal(Meeseeks);
                if ((potentialWorkThings == null || potentialWorkThings.Contains(thing)) && !Memory.jobTargets.Contains(thing) && scanner.HasJobOnThing(Meeseeks, thing, true))
                    return true;
            }

            return false;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();

            cachedCellResults.Clear();
            cachedThingResults.Clear();
        }

        public override void RenderHighlight(List<IntVec3> dragCells)
        {

            RenderHighlightOverSelectableCells(dragCells);
        }

        private void RenderHighlightOverSelectableCells(List<IntVec3> dragCells)
        {
            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    foreach(IntVec3 cell in dragCells)
                    {
                        if (cachedCellResults.ContainsKey(cell) && cachedCellResults[cell] == true)
                        {
                            RenderHighlightOverCell(cell);
                        }
                        else
                        {
                            foreach (Thing thing in cell.GetThingList(base.Map))
                            {
                                if (cachedThingResults.ContainsKey(thing) && cachedThingResults[thing] == true)
                                {
                                    RenderHighlightOverThing(thing);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RenderHighlightOverCell(IntVec3 cell)
        {
            Vector3 position = cell.ToVector3Shifted();
            position.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, position, Quaternion.identity, DragHighlightCellMat, 0);
        }

        private void RenderHighlightOverThing(Thing thing)
        {
            Vector3 drawPos = thing.DrawPos;
            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, DragHighlightThingMat, 0);
        }
    }
}
