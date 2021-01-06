using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    public class MeeseeksBillStorage : GameComponent
    {
        List<MeeseeksBillKnowledge> bills = new List<MeeseeksBillKnowledge>();

        public MeeseeksBillStorage(Game game)
        {

        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref bills, "bills", LookMode.Deep);
        }

        public bool IsOriginal(Bill bill)
        {
            bool isDuplicate = bills.Find(b => b.duplicateBill == bill) != null;
            if (isDuplicate)
                return false;

            bool isOriginal = bills.Find(b => b.originalBill == bill) != null;
            return isOriginal;
        }

        public bool IsDuplicate(Bill bill)
        {
            bool isOriginal = bills.Find(b => b.originalBill == bill) != null;
            if (isOriginal)
                return false;
            
            bool isDuplicate = bills.Find(b => b.duplicateBill == bill) != null;
            return isDuplicate;
        }

        public bool IsTracked(Bill bill)
        {
            return bills.Find(b => b.originalBill == bill || b.duplicateBill == bill) != null;
        }

        public void SaveBill(Bill originalBill)
        {
            if (IsTracked(originalBill))
                return;

            Bill duplicateBill = originalBill.Clone();
            duplicateBill.InitializeAfterClone();

            BillStack billStack = new BillStack(originalBill.billStack.billGiver);
            billStack.AddBill(duplicateBill);

            MeeseeksBillKnowledge newBillKnowledge = new MeeseeksBillKnowledge();
            newBillKnowledge.originalBill = originalBill;
            newBillKnowledge.billStack = billStack;
            newBillKnowledge.billGiver = billStack.billGiver as Thing;

            bills.Add(newBillKnowledge);
        }

        public Bill GetOriginalBill(Bill duplicateBill)
        {
            Bill originalBill = bills.Find(b => b.duplicateBill == duplicateBill)?.originalBill;
            return originalBill;
        }

        public Bill GetDuplicateBill(Bill originalBill)
        {
            Bill duplicateBill = bills.Find(b => b.originalBill == originalBill)?.duplicateBill;
            return duplicateBill;
        }
    }
}
