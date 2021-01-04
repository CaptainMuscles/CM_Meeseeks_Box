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
        private List<Bill> bills = new List<Bill>();
        // Original as key, duplicate as value
        private Dictionary<Bill, Bill> originalBills = new Dictionary<Bill, Bill>();
        // Duplicate as key, original as value
        private Dictionary<Bill, Bill> duplicateBills = new Dictionary<Bill, Bill>();
        //private Dictionary<Bill, List<CompMeeseeksMemory>> meeseeksOnBills = new Dictionary<Bill, List<CompMeeseeksMemory>>();

        public MeeseeksBillStorage(Game game)
        {

        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Remove finished bills before saving
                bills = bills.Where(bill => !bill.deleted).ToList();

                originalBills = originalBills.Where(kvp => !kvp.Key.deleted && !kvp.Value.DeletedOrDereferenced).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                duplicateBills = duplicateBills.Where(kvp => !kvp.Key.DeletedOrDereferenced && !kvp.Value.deleted).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            Scribe_Collections.Look(ref bills, "bills", LookMode.Deep);
            Scribe_Collections.Look(ref originalBills, "originalBills", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref duplicateBills, "duplicateBills", LookMode.Reference, LookMode.Reference);
            //Scribe_Collections.Look(ref meeseeksOnBills, "meeseeksOnBills", LookMode.Reference, LookMode.Reference);
        }

        public bool IsDuplicate(Bill bill)
        {
            return bills.Contains(bill);
        }

        // Make clone of original bill
        public void SaveBill(Bill bill)
        {
            if (originalBills.ContainsKey(bill))
                return;

            Bill newBill = bill.Clone();
            bills.Add(newBill);

            originalBills[newBill] = bill;
            duplicateBills[bill] = newBill;
        }

        // Make new "original" from given bill
        public void ReplaceBill(Bill bill)
        {
            if (duplicateBills.ContainsKey(bill))
                return;

            if (!bills.Contains(bill))
                bills.Add(bill);

            Bill newBill = bill.Clone();

            originalBills[bill] = newBill;
            duplicateBills[newBill] = bill;
        }

        public Bill GetBillCopy(Bill bill)
        {
            if (duplicateBills.ContainsKey(bill))
                return duplicateBills[bill];

            return null;
        }

        public Bill GetBillOriginal(Bill bill)
        {
            if (originalBills.ContainsKey(bill))
                return originalBills[bill];

            return null;
        }

        //public Bill AddMeeseeksToBill(Bill bill, CompMeeseeksMemory meeseeksMemory)
        //{
        //    Bill billCopy = bill;
        //    if (!bills.Contains(bill))
        //        billCopy = SaveBill(bill);

        //    if (!meeseeksOnBills.ContainsKey(billCopy))
        //        meeseeksOnBills.Add(billCopy, new List<CompMeeseeksMemory>());

        //    List<CompMeeseeksMemory> meeseeksList = meeseeksOnBills[billCopy];
        //    if (!meeseeksList.Contains(meeseeksMemory))
        //        meeseeksList.Add(meeseeksMemory);

        //    return billCopy;
        //}

        public void KillBill(Bill bill)
        {
            if (bills.Contains(bill))
                bills.Remove(bill);

            //if (meeseeksOnBills.ContainsKey())
        }

        
    }
}
