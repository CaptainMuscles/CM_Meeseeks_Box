using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

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
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 cell)
        {
            if (!cell.InBounds(base.Map))
            {
                return false;
            }

            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    if (workGiverScanner != null)
                    {
                        if (this.HasJobOnCell(workGiverScanner, cell))
                            return true;

                        foreach (Thing thing in cell.GetThingList(base.Map))
                        {
                            if (HasJobOnThing(workGiverScanner, thing))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            SavedJob savedJob = Memory.savedJob;
            if (savedJob != null)
            {
                if (savedJob.workGiverDef != null)
                {
                    WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;

                    if (workGiverScanner != null)
                    {
                        if (HasJobOnThing(workGiverScanner, thing))
                            return true;
                    }
                }
            }

            return false;
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
                        if (this.HasJobOnCell(workGiverScanner, cell))
                            Memory.AddJobTarget(cell);

                        foreach (Thing thing in cell.GetThingList(base.Map))
                        {
                            if (HasJobOnThing(workGiverScanner, thing))
                                DesignateThing(thing);
                        }
                    }
                }
            }
        }

        public override void DesignateThing(Thing thing)
        {
            Memory.AddJobTarget(thing);
        }

        private bool HasJobOnCell(WorkGiver_Scanner workGiverScanner, IntVec3 cell)
        {
            if (workGiverScanner != null)
            {
                var potentialWorkCells = workGiverScanner.PotentialWorkCellsGlobal(Meeseeks);
                if ((potentialWorkCells == null || potentialWorkCells.Contains(cell)) && workGiverScanner.HasJobOnCell(Meeseeks, cell, true))
                    return true;
            }

            return false;
        }

        private bool HasJobOnThing(WorkGiver_Scanner workGiverScanner, Thing thing)
        {
            if (workGiverScanner != null)
            {
                var potentialWorkThings = workGiverScanner.PotentialWorkThingsGlobal(Meeseeks);
                if ((potentialWorkThings == null || potentialWorkThings.Contains(thing)) && workGiverScanner.HasJobOnThing(Meeseeks, thing, true))
                    return true;
            }

            return false;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
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
                        if (this.HasJobOnCell(workGiverScanner, cell))
                        {
                            RenderHighlightOverCell(cell);
                        }
                        else
                        {
                            foreach (Thing thing in cell.GetThingList(base.Map))
                            {
                                if (HasJobOnThing(workGiverScanner, thing))
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
