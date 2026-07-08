using System;
using System.Collections.Generic;
using System.Linq;

namespace MomodoraArchipelago
{
    /**
     * Convenient class to track the progress of the run.
     */
    public class ProgressTracker
    {
        public List<bool> hasChargeItem;
        public List<bool> hasSavedChargeItem;
        public bool hasFoundGreenLeaf;
        public bool hasSavedFoundGreenLeaf;
        public bool hasBathedLeaf;
        public bool hasSavedBathedLeaf;
        public bool hasCatSphere;
        public bool hasSavedCatSphere;
        public double unlocked;
        public List<bool> hasSavedKey;
        public List<bool> hasKey;
        public bool hasWarp;
        public bool hasSavedWarp;
        public List<List<bool>> hasBoughtItem;
        public List<List<bool>> hasSavedBoughtItem;

        public ProgressTracker()
        {
            hasChargeItem = Enumerable.Repeat(false, GameInfo.chargeItems.Count).ToList();
            hasSavedChargeItem = new List<bool>(hasChargeItem);

            hasKey = Enumerable.Repeat(false, GameInfo.doorKeys.Count).ToList();
            hasSavedKey = new List<bool>(hasKey);
            
            hasBathedLeaf = false;
            hasSavedBathedLeaf = false;
            hasFoundGreenLeaf = false;
            hasSavedFoundGreenLeaf = false;
            hasWarp = false;
            hasSavedWarp = false;
            hasCatSphere = false;
            hasSavedCatSphere = false;

            hasBoughtItem = new List<List<bool>>
            {
                new List<bool> { false, false, false },
                new List<bool> { false },
                new List<bool> { false, false },
                new List<bool> { false },
                new List<bool> { false, false },
                new List<bool> { false, false, false },
                new List<bool> { false, false, false } // Pocket Incensory is banned from the shop to avoid accidental duplicates
            };
            hasSavedBoughtItem = new List<List<bool>>
            {
                new List<bool> { false, false, false },
                new List<bool> { false },
                new List<bool> { false, false },
                new List<bool> { false },
                new List<bool> { false, false },
                new List<bool> { false, false, false },
                new List<bool> { false, false, false } // Pocket Incensory is banned from the shop to avoid accidental duplicates
            };
        }

        /*
         * Load saved state data from archipelago connection
         */
        public void LoadRandomizerState(Momo4Archipelago archipelago, Action<string> log)
        {
            hasChargeItem = archipelago.HasItems(GameInfo.chargeItems);
            hasKey = archipelago.HasItems(GameInfo.doorKeys);
            hasBathedLeaf = archipelago.HasLocation(GameInfo.sourceIdMapping.First(pair => pair.Value == (int)Items.FreshSpringLeaf).Key);
            hasFoundGreenLeaf = archipelago.HasItem((int) Items.FreshSpringLeaf);
            hasWarp = archipelago.HasItem((int) Items.FragmentWarp);
            hasCatSphere = archipelago.HasItem((int) Items.CatSphere);
            hasBoughtItem = archipelago.GetBoughtItems(GameInfo.originalShopItems);
            SaveVariables(log);
        }

        /*
         * Mark pending state updates as "saved" as the game is saved
         */
        public void SaveVariables(Action<string> log)
        {
            log("\nSaving variables\n");
            log("Item Charges & keys:\n");
            for (int i = 0; i < hasChargeItem.Count; i++)
            {
                log("SavedChargeItem[" + i+"]: " + hasSavedChargeItem[i] + " = ChargeItem["+i+"]: " + hasChargeItem[i] + "\n");
                hasSavedChargeItem[i] = hasChargeItem[i];
            }
            for (int i = 0; i < hasSavedKey.Count; i++)
            {
                log("SavedKey["+i+"]: " + hasSavedKey[i] + " = Key["+i+"]: " + hasKey[i] + "\n");
                hasSavedKey[i] = hasKey[i];
            }
            log("\nShop items:\n");
            for (int i = 0; i < hasBoughtItem.Count; i++)
            {
                log("\n" + Enum.GetName(typeof(Shops), i) + " shop:\n");
                for (int j = 0; j < hasBoughtItem[i].Count; j++)
                {
                    log("SavedItemBought["+i+"]["+j+"]: " + hasSavedBoughtItem[i][j] +
                               " = ItemBought["+i+"]["+j+"]: " + hasBoughtItem[i][j] + "\n");
                    hasSavedBoughtItem[i][j] = hasBoughtItem[i][j];
                }
            }
            log("\nSavedLeafBathed: " + hasSavedBathedLeaf + " = LeafBathed: " + hasBathedLeaf + "\n");
            hasSavedBathedLeaf = hasBathedLeaf;
            log("SavedLeafFound: " + hasSavedFoundGreenLeaf + " = LeafFound: " + hasFoundGreenLeaf + "\n");
            hasSavedFoundGreenLeaf = hasFoundGreenLeaf;
            log("SavedhasWarp: " + hasSavedWarp + " = hasWarp: " + hasWarp + "\n");
            hasSavedWarp = hasWarp;
            log("SavedhasCatSphere: " + hasSavedCatSphere + " = hasCatSphere: " + hasCatSphere + "\n");
            hasSavedCatSphere = hasCatSphere;
            log("\nFinished saving variables\n");
        }

        /*
         * Load the "saved" values into the current ones, such as when we die and lose unsaved progress
         */
        public void LoadVariables(Action<string> log)
        {
            log("\nLoading saved variables\n");
            log("Item Charges & keys:\n");
            for (int i = 0; i < hasChargeItem.Count(); i++)
            {
                log("ChargeItem["+i+"]: " + hasChargeItem[i] + " = SavedChargeItem["+i+"]: " + hasSavedChargeItem[i] + "\n");
                log("Key["+i+"]: " + hasKey[i] +  " = SavedKey["+i+"]: " + hasSavedKey[i] + "\n");
                hasChargeItem[i] = hasSavedChargeItem[i];
                hasKey[i] = hasSavedKey[i];
            }
            log("\nShop items:\n");
            for (int i = 0; i < hasBoughtItem.Count(); i++)
            {
                log("\n" + Enum.GetName(typeof(Shops), i) + " shop:\n");
                for (int j = 0; j < hasBoughtItem[i].Count(); j++)
                {
                    log("hasBoughtItem["+i+"]["+j+"]: " + hasBoughtItem[i][j] + 
                               " = Saved["+i+"]["+j+"]: " + hasSavedBoughtItem[i][j] + "\n");
                    hasBoughtItem[i][j] = hasSavedBoughtItem[i][j];
                }
            }
            log("LeafBathed: " + hasBathedLeaf + " = SavedLeafBathed: " + hasSavedFoundGreenLeaf + "\n");
            hasBathedLeaf = hasSavedBathedLeaf;
            log("LeafFound: " + hasFoundGreenLeaf + " = SavedLeafFound: " + hasSavedWarp + "\n");
            hasFoundGreenLeaf = hasSavedFoundGreenLeaf;
            log("hasWarp: " + hasWarp + " = SavedSavedhasWarp: " + hasSavedBathedLeaf + "\n");
            hasWarp = hasSavedWarp;
            log("hasCatSphere: " + hasCatSphere + " = SavedhasCatSphere: " + hasSavedCatSphere + "\n");
            hasCatSphere = hasSavedCatSphere;
            log("\nFinished loading variables\n");
        }
    }
}