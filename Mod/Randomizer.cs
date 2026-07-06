using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System.Drawing.Drawing2D;
using System.IO;

namespace MomodoraArchipelago
{
    public class Randomizer : IComponent
    {
        // Controls and Connections //
        private SimpleLabel RandomizerLabel;
        private MomodoraArchipelagoRandomizerSettings settingsControl;
        public LiveSplitState state;
        private Momo4Archipelago archipelago;
        private readonly object inventoryLock;
        private GameMonitor gameProc;

        // Randomizer State //
        private bool randomizerRunning;
        private bool shopping = false;
        private readonly List<int> deferredItemQueue = new List<int>();
        private string deferredDeathCause = null;
        private ProgressTracker progress = new ProgressTracker();
        
        // Shop Randomization State //
        public string[] shopItemNames = new string[GameInfo.SHOP_SIZE];
        public List<List<int>> shopItems = new List<List<int>> {
            new List<int> {8, 9, 10},
            new List<int> {9},
            new List<int> {11, 12},
            new List<int> {13},
            new List<int> {14, 10},
            new List<int> {15, 10, 16},
            new List<int> {17, 18, 19}
        };
        public static readonly List<int>[] shopPointers = new List<int>[]
        {
            new List<int>(),
            new List<int>(),
            new List<int>()
        };

        // Logging //
        private readonly List<string> Events = new List<string> { };

        // Create the randomizer component
        public Randomizer(LiveSplitState state)
        {
            this.state = state;
            RandomizerLabel = new SimpleLabel();
            settingsControl = new MomodoraArchipelagoRandomizerSettings();
            inventoryLock = new object();

            // Create the archipelago object
            archipelago = new Momo4Archipelago(SendMessage);

            // Register the live split methods.
            state.OnStart += OnStart;
            state.OnReset += OnReset;
        }

        private void OnReset(object sender, TimerPhase value)
        {
            randomizerRunning = false;
            LogResults();

            // Reset the shops so as to not affect other runs
            if (gameProc != null && gameProc.VerifyProcessRunning())
                ResetShops();

            // Disconnect from and reset the archipealgo connection
            archipelago.Disconnect();

            // Strong argument to skip this and re-connect in onStart instead, 
            // but this matches the plugin's original design philosiphy where
            //  clock stopped = randomizer stopped
            archipelago = new Momo4Archipelago(SendMessage);
        }

        private void OnStart(object sender, EventArgs e)
        {
            gameProc = GameMonitor.TryConnectToGame();

            if (gameProc != null && gameProc.VerifyProcessRunning())
            {
                Events.Clear();

                // Register update functions on the special watchers
                SetupSepcialUpdateEvents();
                
                // Archipelago setup contains a lot of server connection and 
                // data retrieval waiting that is better done asynchronously.
                ConfigureArchipelagoAsync();
            }
        }

        /*
         * Configure the speical misc memory watchers referenced by the randomizer.
         */
        private void SetupSepcialUpdateEvents()
        {
            // Configure the update events for the old memory watchers used throught this randomizer (mostly unchanged)
            gameProc.levelIDWatcher.OnChanged += CheckRoom;

            gameProc.inGameWatcher.OnChanged += (old, current) =>
            {
                if (!Convert.ToBoolean(current))
                    progress.LoadVariables(Event);
            };

            gameProc.deathWatcher.OnChanged += (old, current) =>
            {
                // If the number of deaths has increased, we must have died
                if (current > old)
                {
                    progress.LoadVariables(Event);
                    archipelago.NotifyPlayerDeath(gameProc.ReadValue<int>(gameProc.levelIDPointer));
                }
            };

            gameProc.saveWatcher.OnChanged += (old, current) =>
            {
                // If the number of times the game has been saved has increased, we must have saved
                if (current > old)
                {
                    progress.SaveVariables(Event);
                    archipelago.Save();
                }
            };

            gameProc.convOpenWatcher.OnChanged += ConversationCheckIfShop;
            gameProc.playerYWatcher.OnChanged += PlayerYOnUpdate;

            gameProc.currentHealthWatcher.OnChanged += (old, current) =>
            {
                if(old == 0 && current != 0)
                {
                    Debug.WriteLine("Respawning");
                    gameProc.UpdateChargeItemWatchers(); // Update the watchers so they know about the inventory reset.
                    archipelago.ResyncArchipeligoItemsOnDeath();
                }

                // Check if HP loss should kill the player
                else if (current != 0 && current < old)
                    if(archipelago.ShouldFastKill(gameProc.ReadValue<int>(gameProc.levelIDPointer))){
                        gameProc.WriteValue<double>(gameProc.currentHealthPointer, 0);
                    }
            };

            // Setup the new Archipelago Watchers
            SetupFinalBossKillWatcher();
            SetupMeowWatcher();
            SetupSpiderWatcher();
        }

        private async void ConfigureArchipelagoAsync(){
            // Connect to the archipelago session
            if (archipelago.ConnectArchipelago(
                settingsControl.GetServerAddress(),
                settingsControl.GetServerPort(),
                settingsControl.GetSlot(),
                settingsControl.GetServerPassword()
            )) {
                // Does not seem to do anything ??
                progress.LoadRandomizerState(archipelago, Event);

                // Initialize the randomizer
                await archipelago.InitializeRandomizer(
                    ArchipelagoGrantItem,
                    KillPlayer,
                    CreateLocationMemoryWatcherEvent
                );

                // Check if we started the game in a shop room
                if(GameInfo.shopLocations.Contains(gameProc.ReadValue<int>(gameProc.levelIDPointer)))
                    ModifyShopInventory(gameProc.ReadValue<int>(gameProc.levelIDPointer));

                SendMessage("Randomization Complete!");
                randomizerRunning = true;
            }
        }

        /*
         * Function to setup a memory watcher to preform a specific location check.
         * locationID = the id of the location to setup the watcher at
         * newItemID = the ID of the item to give at the location
         * itemName = the name of the item to show to the user
         */
        private void CreateLocationMemoryWatcherEvent(int locationID, int newItemID, string itemName)
        {
            if (8 <= locationID && locationID <= 19)// If item is a shop item
            {
                SaveShopItem(locationID, newItemID, itemName);
            }
            else
            {
                // Save the mappings in the debug file
                if(GameInfo.sourceIdMapping.ContainsKey(locationID))
                    Debug.WriteLine("Item '" + itemName + "' generated at " + Enum.GetName(typeof(Items), GameInfo.sourceIdMapping[locationID]));
                else
                    Debug.WriteLine("Item '" + itemName + " generated at position " + locationID);
                
                // Setup a memory watcher for the item source
                gameProc.CreateRandomizerSourceWatcher(gameProc.potentialSourcesPointers[locationID], (old, current) =>
                    {
                        // current is making sure we only do this if the item is added, levelID is for ???
                        int levelID = gameProc.ReadValue<int>(gameProc.levelIDPointer);
                        if (current == 1 && GameInfo.sourceToLevelMapping[locationID].Contains(levelID))
                        {
                            // track the spring leaf event
                            if (locationID == 28) progress.hasBathedLeaf = true;
                            
                            // Perform the item replacement
                            ReplaceItem(newItemID, gameProc.potentialSourcesPointers[locationID]);
                            RandomizerLabel.Text = $"Got {itemName}!";

                            // Tell Archipelago that we checked a location
                            archipelago.NotifyLocationChecked(locationID);
                        }
                    });

                // Whenever we enter this item's room, preload its name so we can display it on screen.
                int stringIndex = GameInfo.sourceToStringMapping[locationID];
                //Shop items are handled elsewhere
                if(stringIndex != -1)
                {
                    // On screen transitions, check for this location and swap its name in memory
                    gameProc.levelIDWatcher.OnChanged += (old, current) =>
                    {
                        if (GameInfo.sourceToLevelMapping[locationID].Contains(current))
                        {
                            // Replace the old item name with the new one
                            byte[] bytes = Encoding.ASCII.GetBytes(itemName);
                            gameProc.WriteBytes(gameProc.itemGetStringPtrs[stringIndex], bytes);// Set name of placeholder to the one that will get added later
                            gameProc.WriteValue(gameProc.itemGetStringPtrs[stringIndex] + bytes.Length, 0x0);// Add end of string
                        }
                    };
                }
            }
        }

        /*
         * Adds a new item to the players inventory and removes the last item acquired
         * id = id of item to give out
         * addr = IntPtr to source of item given out
         */
        private bool ReplaceItem(int id, IntPtr addr)
        {
            lock(inventoryLock) {
                RemoveItem();

                // If it's our item, give it to the player
                if (id != (int)Items.ArchipelagoItem)
                    return GrantItem(id, addr);

                return true;
            }
        }

        /*
         * Archipelago wrapper for the grant item function
         * id = id of item to give
         */
        private void ArchipelagoGrantItem(int id)
        {
            lock(inventoryLock) {
                if (shopping)
                    deferredItemQueue.Add(id);
                else
                    GrantItem(id, IntPtr.Zero);
            }
        }

        /*
         * Grant all items in the deferred item queue.
         */
        private void GrantDeferredItems()
        {
            lock(inventoryLock) {
                foreach (int id in deferredItemQueue)
                    GrantItem(id, IntPtr.Zero);
                deferredItemQueue.Clear();
            }
        }

        /*
         * Adds a new item to the players inventory (with no replacement)
         * id = id of item to give out
         * addr = IntPtr to source of item given out
         */
        private bool GrantItem(int id, IntPtr addr)
        {
            bool res = true;
            Debug.WriteLine("Giving item id: " + id);
            
            int allocatedMemory = gameProc.ReadValue<int>(IntPtr.Subtract(gameProc.inventoryItemsStartPointer, 0x10));
            Debug.WriteLine("Allocated memory: " + allocatedMemory);
            int totalItems = gameProc.ReadValue<int>(gameProc.totalItemsPointer);
            Debug.WriteLine("Total items: " + totalItems);

            if (id == (int)Items.VitalityFragment)
            {
                AdjustVitalityFragments(1);
            }
            else if (totalItems * 16 != allocatedMemory)
            {
                if ((int)Items.FragmentWarp >= id && id >= (int)Items.FragmentBowPow)
                {
                    AddCrestFragment(id);
                    if (id == 53)
                    {
                        progress.hasWarp = true;
                    }
                }
                else if (id == (int)Items.Bellflower || id == (int)Items.Passiflora || id == (int)Items.TaintedMissive || id == (int)Items.IvoryBug)
                {
                    AddChargeItem(id);
                }
                else if (id == (int)Items.MonasteryKey || id == (int)Items.GardenKey || id == (int)Items.CinderKey)
                {
                    AddKey(id);
                }
                else if (id == (int)Items.FreshSpringLeaf)
                {
                    AddLeaf();
                }
                else
                {
                    AddItem(id);
                }
            }
            else
            {
                if (addr != IntPtr.Zero) gameProc.WriteValue<double>(addr, 0);
                res = false;
            }
            gameProc.UpdateChargeItemWatchers(); // Update the memeory watchers to clear this item's report before its checked elsewhere.
            if(res == true) Event("\nItem " + id + " given, total items: " + totalItems + "\n");
            else Event("\nUnable to give item " + id + ", total items: " + totalItems + "\n");
            return res;
        }
      
        private void AddLeaf()
        {
            gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[28], 1);
            progress.hasFoundGreenLeaf = true;
            AddItem((int)Items.FreshSpringLeaf);
        }

        /*
         * Remove the most recently obtained item and report which item was removed.
         */
        private Items RemoveItem()
        {
            gameProc.UpdateChargeItemWatchers(); // Update the watchers to check if any charge item values changed.

            if (gameProc.taintedMissiveWatcher.Changed)
            {
                Event("Removing missive\n");
                RemoveChargeItem((int)Items.TaintedMissive, gameProc.taintedMissiveWatcher.Current - gameProc.taintedMissiveWatcher.Old);
                return Items.TaintedMissive;
            }
            else if (gameProc.bellflowerWatcher.Changed)
            {
                Event("\nRemoving bellflower\n");
                RemoveChargeItem((int)Items.Bellflower, gameProc.bellflowerWatcher.Current - gameProc.bellflowerWatcher.Old);
                return Items.Bellflower;
            }
            else if (gameProc.passifloraWatcher.Changed)
            {
                Event("\nRemoving passiflora\n");
                RemoveChargeItem((int)Items.Passiflora, gameProc.passifloraWatcher.Current - gameProc.passifloraWatcher.Old);
                return Items.Passiflora;
            }
            else if (gameProc.ivoryBugWatcher.Changed && archipelago.Bugs)
            {
                Event("\nRemoving IB\n");
                RemoveChargeItem((int)Items.IvoryBug, 1);
                return Items.IvoryBug;
            }
            else if (gameProc.crestFragmentWatcher.Changed)
            {
                Event("\nRemoving Crest Frag\n");
                return RemoveCrestFragment();
            }
            else if (gameProc.vitalityFragmentWatcher.Changed && archipelago.Vitality)
            {
                Event("\nRemoving Vit Frag\n");
                AdjustVitalityFragments(-1);
                return Items.VitalityFragment;
            }
            else if (gameProc.levelIDWatcher.Current == 83)
            {
                Event("\nGreen leaf upgraded, do not remove stuff\n");
                return Items.None;
            }
            else
            {
                Event("\nRemoving last item in inventory\n");
                return RemoveLastItem();
            }
        }

        private void AddChargeItem(int id)
        {
            IntPtr maxValuePointer, saveValuePtr;
            switch (id)
            {
                case (int)Items.Bellflower:
                    maxValuePointer = gameProc.bellflowerMaxValuePointer;
                    saveValuePtr = gameProc.bellflowerSaveValuePointer;
                    break;
                case (int)Items.Passiflora:
                    maxValuePointer = gameProc.passifloraMaxValuePointer;
                    saveValuePtr = gameProc.passifloraSaveValuePointer;
                    break;
                case (int)Items.TaintedMissive:
                    maxValuePointer = gameProc.taintedMissiveMaxValuePointer;
                    saveValuePtr = gameProc.taintedMissiveSaveValuePointer;
                    break;
                case (int)Items.IvoryBug:
                    maxValuePointer = gameProc.ivoryBugCountPointer;
                    saveValuePtr = IntPtr.Zero;
                    break;
                // Cat Sphere is a charge item with infinite chages, should handle here, but i dont have the charge amount pointer
                default:
                    return;
            }
            int j = GameInfo.chargeItems.IndexOf(id);
            bool hasItem = progress.hasChargeItem[j];
            double currentMaxCharges = hasItem? gameProc.ReadValue<double>(maxValuePointer): 0;
            int increase = GetChargeItemIncrease(id, currentMaxCharges);

            gameProc.WriteValue(maxValuePointer, increase + currentMaxCharges);
            if (saveValuePtr != IntPtr.Zero)
                gameProc.WriteValue(saveValuePtr, increase + currentMaxCharges);

            if (currentMaxCharges == 0 && !hasItem)
            {
                progress.hasChargeItem[j] = true;
                AddItem(id);
            }
        }

        /*
         * Calculate the amount of additional charges to give for a specific item/current charge amount.
         * id = the item id we are adding charges to
         * currentCharges = the current amount of charges the item has.
         * Note: currentCharges is stored internally as a double, which i dont trust enough to compare using ==
         */
        private int GetChargeItemIncrease(int id, double currentCharges) {
            switch (id) {
                case (int)Items.IvoryBug:
                    return 1; // Ivory Bugs are always worth one

                case (int)Items.Bellflower:
                    if (currentCharges < 1) return 3; // First bellflower is worth 3
                    if (currentCharges > 8) return 1; // Last bellflower is worth 1
                    goto default;

                case (int)Items.Passiflora:
                    if (currentCharges < 0.5) return 1; // First passiflora is worth 1
                    if (currentCharges > 0.5) return 3; // Second passiflora is worth 3
                    goto default;

                default:
                    return 2; // Everything other amount/item type just adds 2 charges.
            }
        }

        private void RemoveChargeItem(int id, double charges)
        {
            double currentCharges;
            IntPtr maxValuePointer, saveValuePtr;
            switch (id)
            {
                case (int)Items.Bellflower:
                    maxValuePointer = gameProc.bellflowerMaxValuePointer;
                    saveValuePtr = gameProc.bellflowerSaveValuePointer;
                    break;
                case (int)Items.Passiflora:
                    maxValuePointer = gameProc.passifloraMaxValuePointer;
                    saveValuePtr = gameProc.passifloraSaveValuePointer;
                    break;
                case (int)Items.TaintedMissive:
                    maxValuePointer = gameProc.taintedMissiveMaxValuePointer;
                    saveValuePtr = gameProc.taintedMissiveSaveValuePointer;
                    break;
                case (int)Items.IvoryBug:
                    maxValuePointer = gameProc.ivoryBugCountPointer;
                    saveValuePtr = IntPtr.Zero;
                    break;
                default:
                    return;
            }
            int j = GameInfo.chargeItems.IndexOf(id);
            progress.hasChargeItem[j] = true;
            currentCharges = gameProc.ReadValue<double>(maxValuePointer);
            gameProc.WriteValue(maxValuePointer, currentCharges - charges);
            if (saveValuePtr != IntPtr.Zero)
                gameProc.WriteValue(saveValuePtr, currentCharges - charges);
            
            // Get rid of the item if it doesnt have any charges
            // TODO: figure out how to stop accumulating empty bellflowers
            // Likely need to check equipped items or look for and replace empty de-equipped bellflowers at later time
            //if((currentCharges - charges) <= 0) {
            //    RemoveLastItem(); // Didn't work on bellflower (because its equipped automatically?)
            //}
        }

        private void AddItem(int id)
        {
            //To add an item: Increase total item counter by one
            //and set the inventory value for the next item slot to the id of the item given
            var totalItemAmount = gameProc.ReadValue<int>(gameProc.totalItemsPointer);
            if (id == (int)Items.CatSphere) progress.hasCatSphere = true;
            gameProc.WriteValue<int>(gameProc.totalItemsPointer, (int)totalItemAmount + 1);
            gameProc.WriteValue<double>(IntPtr.Add(gameProc.inventoryItemsStartPointer, 0x10 * totalItemAmount), id);
        }

        private Items RemoveLastItem()
        {
            //To remove last item, decrease total item counter by one
            var totalItemAmount = gameProc.ReadValue<int>(gameProc.totalItemsPointer);

            // Save the removed item's type so we can report it later.
            Items removedItem = (Items) PeekLastItem();

            gameProc.WriteValue<int>(gameProc.totalItemsPointer, (int)totalItemAmount - 1);
            return removedItem;
        }

        private Items RemoveCrestFragment()
        {
            Items fragment = RemoveLastItem();
            CountCrestFragments();
            return fragment;
        }

        private void AddCrestFragment(int id)
        {
            AddItem(id);
            CountCrestFragments();
        }

        private void AdjustVitalityFragments(int change)
        {
            double fragments = gameProc.ReadValue<double>(gameProc.vitalityFragmentCountPointer);
            gameProc.WriteValue(gameProc.vitalityFragmentCountPointer, fragments + change);

            double hpPerFragment = GameInfo.GetHealthChangePerDifficulty(gameProc.Version, gameProc.Difficulty);
            AdjustMaxHp(change * hpPerFragment);
        }

        private void AdjustMaxHp(double change)
        {
            double health = gameProc.ReadValue<double>(gameProc.maxHealthPointer);
            gameProc.WriteValue(gameProc.maxHealthPointer, health + change);
        }

        private void AddKey(int id)
        {
            progress.hasKey[GameInfo.doorKeys.IndexOf(id)] = true;
            AddItem(id);
        }

        private void CountCrestFragments()
        {
            int totalItems = gameProc.ReadValue<int>(gameProc.totalItemsPointer);
            int fragments = 0;
            for (int i = 0; i < totalItems; i++)
            {
                double itemId = gameProc.ReadValue<double>(IntPtr.Add(gameProc.inventoryItemsStartPointer, 0x10 * i));
                if ((int)Items.FragmentWarp >= itemId && itemId >= (int)Items.FragmentBowPow)
                {
                    fragments++;
                }
            }
            Debug.WriteLine("Found a total of " + fragments + " crest fragments");
            fragments = Math.Max(Math.Min(fragments, 4), 0);
            Debug.WriteLine("Writing " + fragments + " crest fragments");
            gameProc.WriteValue<double>(gameProc.crestFragmentCountPointer, fragments);
        }

        private void CheckRoom(int old, int current)
        {
            int j;

            //Garden = 4,
            //Cinder = 5,
            //Monastery = 6
            for (int i = 0; i < 3; i++)
            {
                j = i + 4;//Id for keys is 4-6
                bool unlocked = false;

                if (GameInfo.lockedDoorLocations[i].Contains(current))//If the player is in a room with doors that pertaint to a key
                {
                    unlocked = gameProc.ReadValue<double>(gameProc.potentialSourcesPointers[j]) > 0;//Read value for key acquired

                    if (unlocked != progress.hasKey[i])//If the state of "key acquired" is different than it should be invert value
                    {
                        Event("\nApplying door logic\n");
                        Event("Door " + (unlocked ? "closed" : "opened") + "\n");
                        gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[j], unlocked? 0: 1); // flip the door state
                    }
                }
                else if (GameInfo.lockedDoorLocations[i].Contains(old) && !GameInfo.lockedDoorLocations[i].Contains(current))//If they just left the room revert state of flag
                {
                    gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[j], unlocked? 0: 1);
                }
            }
            
            gameProc.playerYWatcher.Enabled = ShouldEnableYWatcher(current);
            

            // Update/Reset the shop items
            if (GameInfo.shopLocations.Contains(current))
                ModifyShopInventory(current);
            else if (GameInfo.shopLocations.Contains(old))
                ResetShopInventory(old);

            // DEBUG - set munny to 1000 every time you walk into a room
            gameProc.WriteValue<double>(gameProc.munnyPointer, 1000);
        }


        /**
         * In some rooms, we track the player's Y value for various reasons.
         *   this onUpdate function combines those checks.
         */
        private void PlayerYOnUpdate(double old, double curernt)
        {
            int room = gameProc.ReadValue<int>(gameProc.levelIDPointer);

            // Lubella Anti-Softlock: if the player doesn't have the cat sphere in the escape room, turn them human
            if (room == 149 && gameProc.ReadValue<double>(gameProc.lubella2Pointer) > 0 && !progress.hasCatSphere)
            {
                // Turn the player into either a human or cat as appropriate
                double playerXPos = gameProc.ReadValue<double>(gameProc.playerXPointer);
                if (curernt > 195 && playerXPos > 120)
                {
                    gameProc.WriteValue<double>(gameProc.playerFormPointer, 1);
                }
                else
                {
                    gameProc.WriteValue<double>(gameProc.playerFormPointer, 0);
                }

            }

            // Check If we are in the green leaf room
            if (room == 82)
            {
                double pickedUpLeaf = gameProc.ReadValue<double>(gameProc.potentialSourcesPointers[28]);
                double playerXPos = gameProc.ReadValue<double>(gameProc.playerXPointer);
                if (gameProc.playerYWatcher.Current > 176 && playerXPos > 410)
                {
                    //If the state of "has picked green leaf source" is different from "found green leaf" invert
                    if (Convert.ToBoolean(pickedUpLeaf) != progress.hasBathedLeaf) 
                        gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[28], 1 - pickedUpLeaf);
                }
                else
                {
                    gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[28], progress.hasFoundGreenLeaf? 0: 1);
                }
            }
        }

        public bool ShouldEnableYWatcher(int room)
        {
            return (room == 82) || (room == 149 && gameProc.ReadValue<double>(gameProc.lubella2Pointer) > 0 && !progress.hasCatSphere);
        }

        private void ConversationCheckIfShop(double old, double current)
        {
            int room = gameProc.ReadValue<int>(gameProc.levelIDPointer);// Get current room

            if (GameInfo.shopLocations.Contains(room))// If player is in a shop room
            {
                if (current != 0) { // If player is in a conversation with an npc
                    Event("\nShopping...\n");
                    shopping = true;
                    AddBoughtItemPlaceholders(room);

                    // Before we shop, make sure to acknowledge any pending charge item updates.
                    gameProc.UpdateChargeItemWatchers();

                } else {
                    Event("\nShopping complete!\n");
                    ReplacePurchases(room);
                    shopping = false;
                    GrantDeferredItems();
                    if (deferredDeathCause != null) {
                        KillPlayer(deferredDeathCause);
                        deferredDeathCause = null;
                    }
                }
            }
        }

        /*
         * Check if the player's inventory contains any of the placeholder shop items
         * and swap them with the appropriate randomized item instead.
         * room = the level id of the current room.
         * Note: In order to better control this method's timings, it now happens exactly once when the player leaves the shop.
         *       As long as i properly lock and defer server items while shopping, this should be much safer.
         */
        private void ReplacePurchases(int room)
        {
            int currentShopLocation = GameInfo.shopLocations.IndexOf(room);
            List<int> currentShopItems = shopItems[currentShopLocation]; // get shop's randomized inventory

            lock (inventoryLock) {

                // Remove all purchased shop placeholder items from the player's inventory
                List<Items> purchasedItems = new List<Items>();
                while (GameInfo.shopPlaceholderItems.Contains(PeekLastItem())) {
                    purchasedItems.Add(RemoveItem());
                    Event("Last acquired item: " + purchasedItems.Last() + "\n");
                }

                // Check each placeholder to see if it is a newly purchased item
                foreach (var placeholderItem in purchasedItems) {
                    int listPosition = GameInfo.shopPlaceholderItems.IndexOf((int)placeholderItem);

                    // Ensure the item is newly purchased, and not one of the bought item placeholders.
                    if (!progress.hasBoughtItem[currentShopLocation][listPosition])
                    {
                        // For each purchased item, give the player the real item instead
                        int originalItem = GameInfo.originalShopItems[currentShopLocation][listPosition];
                        SendMessage($"Bought {shopItemNames[originalItem - GameInfo.originalShopItems[0][0]]}!");

                        // Update value of hasBoughtItem in all shops that "sell" originalItem
                        for (int i = 0; i < GameInfo.originalShopItems.Count(); i++) 
                            for (int j = 0; j < GameInfo.originalShopItems[i].Count(); j++)
                                if (GameInfo.originalShopItems[i][j] == originalItem)
                                {
                                    Event("Updating shop " + Enum.GetName(typeof(Shops), i) + ": position " + j + "\n");
                                    progress.hasBoughtItem[i][j] = true;
                                }

                        // Give the player the item if it's from this game
                        if (currentShopItems[listPosition] != (int)Items.ArchipelagoItem)
                            GrantItem(currentShopItems[listPosition], IntPtr.Zero);

                        // Tell Archipelago
                        archipelago.NotifyLocationChecked(originalItem);
                    }
                }
            }

            // Summarize the purchase
            Event("\nAt shop " + Enum.GetName(typeof(Shops), currentShopLocation) + "\n");
            foreach (var item in shopItems[currentShopLocation])
            {
                Event("Items sold: " + Enum.GetName(typeof(Items), item) +
                    (progress.hasBoughtItem[currentShopLocation][shopItems[currentShopLocation].IndexOf(item)] ? " was bought" : " wasn't bought") + "\n");
            }
        }

        /*
         * Add placeholders to the players inventory for any items already purchased.
         *  By doing this, the in game shop menu will show the item as already purchased.
         * room = the level id of the current room
         */
        private void AddBoughtItemPlaceholders(int room)
        {
            // Get list of what items have already been bought in the current shop
            List<bool> boughtItems = progress.hasBoughtItem[GameInfo.shopLocations.IndexOf(room)];

            for (int i = 0; i < boughtItems.Count(); i++)
                if (boughtItems[i]) // If item was bought add the placeholder
                {
                    AddItem(GameInfo.shopPlaceholderItems[i]);
                    Event($"Adding placholder id {GameInfo.shopPlaceholderItems[i]}\n");
                }
        }

        /* 
         * Get the id of the most recently acquired item
         */
        private int PeekLastItem(){
            int invSize = gameProc.ReadValue<int>(gameProc.totalItemsPointer);
            return (int)gameProc.ReadValue<double>(IntPtr.Add(gameProc.inventoryItemsStartPointer, 0x10 * (invSize - 1)));
        }

        /*
         * Save the information about the item to be displayed in the shop.
         */
        private void SaveShopItem(int origin, int swapped, string name)
        {
            int itemPos, listPos;
            foreach (var list in GameInfo.originalShopItems)
            {
                if (list.Contains(origin))
                {
                    listPos = GameInfo.originalShopItems.IndexOf(list);
                    itemPos = list.IndexOf(origin);
                    shopItems[listPos][itemPos] = swapped;
                    shopItemNames[origin - GameInfo.originalShopItems[0][0]] = name;
                }
            }
        }

        private void ModifyShopInventory(int room)
        {
            IntPtr pointer, itemPointer;
            int currentShopLocation = GameInfo.shopLocations.IndexOf(room);
            List<int> list = GameInfo.GetShopOffsets(gameProc.Version)[currentShopLocation];
            List<int> shopItemsAux = shopItems[currentShopLocation];// Get list of items of the current shop
            int id = 22, pValue;
            byte[] bytes;

            Event("\nSetting items for " + Enum.GetName(typeof(Shops), currentShopLocation) + " shop\n");
            for (int i = 0; i < list.Count(); i++)
            {
                id = GameInfo.shopPlaceholderItems[i];

                pointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.shopPointer).Deref<Int32>(gameProc.gameProc), 0x10 * list[i]);// Get pointer to shop item
                gameProc.WriteValue<double>(pointer, id);// Set shop placeholder to id

                // Vitality Fragment & AP Items dont have a name or effect so we have to write to memory
                if (shopItemsAux[i] == (int)Items.VitalityFragment || shopItemsAux[i] == (int)Items.ArchipelagoItem)
                {
                    // Determine the item name and description
                    string name, description;
                    if (shopItemsAux[i] == (int)Items.VitalityFragment) {
                        // (trying to write as few bytes as possible)
                        name = "Vit. Frag.";
                        description = "+ Max Hp";
                    } else {
                        name = "AP Item"; // Respecting short item name by storing the variable string in the description
                        // Get the replced item's id, then subtract it by id of the first shop item to get the index
                        description = shopItemNames[GameInfo.originalShopItems[currentShopLocation][i] - GameInfo.originalShopItems[0][0]];
                    }

                    bytes = Encoding.ASCII.GetBytes(name);
                    pointer = (IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * id) + 0x10), 0x0 }).Deref<Int32>(gameProc.gameProc);// Get pointer to item name
                    gameProc.WriteBytes(pointer, bytes);// Set name of placeholder to the one that will get added later
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string

                    bytes = Encoding.ASCII.GetBytes(description);
                    pointer = (IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * id) + 0x20), 0x0 }).Deref<Int32>(gameProc.gameProc);// Get pointer to item effect
                    gameProc.WriteBytes(pointer, bytes);// Set effect of placeholder to the one that will get added later
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string
                }
                else
                {
                    pointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * id) + 0x10) }).Deref<Int32>(gameProc.gameProc), 0x0);// Get pointer of placeholder
                    shopPointers[i].Add(gameProc.ReadValue<int>(pointer));// Save value of original pointer
                    itemPointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * shopItemsAux[i]) + 0x10) }).Deref<Int32>(gameProc.gameProc), 0x0);// Get pointer of real item
                    pValue = gameProc.ReadValue<int>(itemPointer);
                    gameProc.WriteValue(pointer, pValue);// Make placeholder pointer point to the real item's name

                    pointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * id) + 0x20) }).Deref<Int32>(gameProc.gameProc), 0x0);// Get pointer of placeholder
                    shopPointers[i].Add(gameProc.ReadValue<int>(pointer));// Save value of original pointer
                    itemPointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * shopItemsAux[i]) + 0x20) }).Deref<Int32>(gameProc.gameProc), 0x0);// Get pointer of real item
                    pValue = gameProc.ReadValue<int>(itemPointer);
                    gameProc.WriteValue(pointer, pValue);// Make placeholder pointer point to the real item's effect
                }
            }
        }

        private void ResetShopInventory(int room)
        {
            IntPtr pointer;
            int currentShopLocation = GameInfo.shopLocations.IndexOf(room);
            List<int> list = GameInfo.GetShopOffsets(gameProc.Version)[currentShopLocation];
            List<int> shopItemsAux = GameInfo.originalShopItems[currentShopLocation];// Get list of original items for the current shop
            List<int> shopItemsAux2 = shopItems[currentShopLocation];// Get list of items of the current shop
            int id, idAux = 22;
            byte[] bytes;

            Event("\nResetting items for " + Enum.GetName(typeof(Shops), currentShopLocation) + " shop\n");
            for (int i = 0; i < list.Count(); i++)
            {
                idAux = GameInfo.shopPlaceholderItems[i];
                id = shopItemsAux[i];

                pointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc.gameProc), 0x10 * list[i]);// Get pointer to shop item
                gameProc.WriteValue<double>(pointer, (double)GameInfo.sourceIdMapping[id]);// Set shop item id to original one

                // check pointerValues[i] so livesplit doesnt freeze on onReset()
                if (shopItemsAux2[i] == 54 && shopPointers[i].Count == 0)
                {
                    bytes = Encoding.ASCII.GetBytes(GameInfo.placeholderShopNames[i]);
                    pointer = (IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * idAux) + 0x10), 0x0 }).Deref<Int32>(gameProc.gameProc);// Get pointer to item name
                    gameProc.WriteBytes(pointer, bytes);// Restore name
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string

                    bytes = Encoding.ASCII.GetBytes(GameInfo.placeholderShopDescription[i]);
                    pointer = (IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * idAux) + 0x20), 0x0 }).Deref<Int32>(gameProc.gameProc);// Get pointer to item effect
                    gameProc.WriteBytes(pointer, bytes);// Restore effect
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string
                }
                else if (shopPointers[i].Count > 0)
                {
                    pointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * idAux) + 0x10) }).Deref<Int32>(gameProc.gameProc), 0x0);// Get pointer of placeholder
                    gameProc.WriteValue(pointer, shopPointers[i][0]);// Restore placeholder pointer value

                    pointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.itemInfoPointer, new int[] { ((0x60 * idAux) + 0x20) }).Deref<Int32>(gameProc.gameProc), 0x0);// Get pointer of placeholder
                    gameProc.WriteValue(pointer, shopPointers[i][1]);// Restore placeholder pointer value

                    shopPointers[i].Clear();// Remove placeholder pointer values
                }
            }
        }
        
        private void ResetShops() {
            foreach (var room in GameInfo.shopLocations)
                ResetShopInventory(room);
        }

        private void Event(string text)
        {
            if (settingsControl.logEnabled == true)
            {
                if (text.StartsWith("\n"))
                {
                    Events.Add("\n" + DateTime.Now.ToString("T") + ":" + text.Replace("\n", "") + "\n");
                }
                else Events.Add(DateTime.Now.ToString("T") + ":" + text);
            }
            Debug.WriteLine(text.Replace("\n", ""));
        }

        private void LogResults()
        {
            if(settingsControl.logEnabled == true)
            {
                Debug.WriteLine("Creating event log file");
                var path = "Components/MomodoraArchipelagoRandomizer.log";
                if (File.Exists(path))
                {
                    File.AppendAllText(path, "Running on version " + gameProc?.Version ?? 0 + "\n");
                    Debug.WriteLine("Writing run events");
                    foreach (var Event in Events)
                    {
                        File.AppendAllText(path, Event);
                    }
                }
                else Debug.WriteLine("Failed to create event log file");
            }
        }

        public string ComponentName => "Momodora Archipelago Randomizer";

        public float HorizontalWidth {get; set;}

        public float MinimumHeight => 10;

        public float VerticalHeight { get; set; }

        public float MinimumWidth => 200;

        public float PaddingTop => 1;

        public float PaddingBottom => 1;

        public float PaddingLeft => 1;

        public float PaddingRight => 1;

        public IDictionary<string, Action> ContextMenuControls => null;

        public void Dispose()
        {
        }

        public void DrawHorizontal(System.Drawing.Graphics g, LiveSplitState state, float height, System.Drawing.Region clipRegion)
        {
            throw new NotImplementedException();
        }

        public void DrawVertical(System.Drawing.Graphics g, LiveSplitState state, float width, System.Drawing.Region clipRegion)
        {
            var textHeight = g.MeasureString("A", state.LayoutSettings.TextFont).Height;
            VerticalHeight = textHeight * 1.5f;

            prepareDraw(state);
            RandomizerLabel.SetActualWidth(g);
            RandomizerLabel.Width = RandomizerLabel.ActualWidth;
            RandomizerLabel.Height = VerticalHeight;
            RandomizerLabel.X = width-PaddingRight-RandomizerLabel.Width;
            RandomizerLabel.Y = 3f;

            DrawBackground(g, width, VerticalHeight);

            RandomizerLabel.Draw(g);
        }

        public void prepareDraw(LiveSplitState state)
        {
            RandomizerLabel.Font = settingsControl.OverrideTextFont ? settingsControl.TextFont : state.LayoutSettings.TextFont;
            RandomizerLabel.ForeColor = settingsControl.OverrideTextColor ? settingsControl.TextColor : state.LayoutSettings.TextColor;
            RandomizerLabel.OutlineColor = settingsControl.OverrideTextColor ? settingsControl.OutlineColor : state.LayoutSettings.TextOutlineColor;
            RandomizerLabel.ShadowColor = settingsControl.OverrideTextColor ? settingsControl.ShadowColor : state.LayoutSettings.ShadowsColor;

            RandomizerLabel.VerticalAlignment = StringAlignment.Center;
            RandomizerLabel.HorizontalAlignment = StringAlignment.Center;
        }

        private void DrawBackground(Graphics g, float width, float height)
        {
            if (settingsControl.BackgroundColor.A > 0
                || settingsControl.BackgroundGradient != GradientType.Plain
                && settingsControl.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            settingsControl.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            settingsControl.BackgroundColor,
                            settingsControl.BackgroundGradient == GradientType.Plain
                            ? settingsControl.BackgroundColor
                            : settingsControl.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return settingsControl.GetSettings(document);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            return settingsControl;
        }

        public void SetSettings(XmlNode settings)
        {
            settingsControl.SetSettings(settings);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (gameProc != null && gameProc.VerifyProcessRunning() && randomizerRunning)
            {
                if (gameProc.InGame)
                {
                    // Unsure if this is actually required? it seems like it should happen automatically?
                    gameProc.UpdateSpecialWatchers();
                    gameProc.UpdateRandomizerSourceWatchers();
                }

                if (invalidator != null)
                {
                    invalidator.Invalidate(0, 0, width, height);
                }
            }
        }

        /*
         * Kill a player if requested (by deathlink or similar)
         * cause = the reason the player was killed
         */
        private void KillPlayer(string cause) {
            if (archipelago.ShouldDeathLink(gameProc.ReadValue<int>(gameProc.levelIDPointer))) {
                if (!shopping) {
                    SendMessage($"Killed by {cause}.");
                    archipelago.IgnoreNextDeath = true;
                    gameProc.WriteValue<double>(gameProc.currentHealthPointer, 0);
                } else deferredDeathCause = cause;
            }
        }

        /*
         * Send a message for logging and display
         */
        private void SendMessage(string message) {
            Event(message);
            RandomizerLabel.Text = message;
        }

        /*
        * Setup the memory watcher to detect a boss kill
        */
        private void SetupFinalBossKillWatcher() {
            var watcher = gameProc.CreateSpecialMemoryWatcher<double>(gameProc.cutsceneStatePointer, (old, current) =>
                {
                    // Cutscene State 1000 is triggered on boss death. We check that + room id to ensure the right state.
                    int levelID = gameProc.ReadValue<int>(gameProc.levelIDPointer);
                    if (current == 1000 && levelID == GameInfo.FINAL_BOSS_LEVEL_ID)
                        archipelago.CheckVictoryConditionOnBossKill(progress.hasFoundGreenLeaf);
                });
        }

        /*
         * Meow :3
         */
        private void SetupMeowWatcher() {
            // Setup the memory watcher to check the game's memory every 10 milliseconds 
            var watcher = gameProc.CreateSpecialMemoryWatcher<double>(gameProc.playerFormPointer);

            // Trigger when the player's form changes
            watcher.OnChanged += (old, current) =>
            {
                archipelago.Say("Meow :3");
                watcher.Enabled = false;
            };
        }
 
        /*
         * Setup the memory watcher to kill the player if the spider merchant dies. (anti-softlock)
         */
        private void SetupSpiderWatcher() {
            var watcher = gameProc.CreateSpecialMemoryWatcher<double>(gameProc.spiderMerchantPointer, (old, current) =>
                {
                    SendMessage($"Spider: ({old} -> {current})."); // DEBUG
                    if (archipelago.ShouldSpiderMerchantKill())
                    {
                        //SendMessage("Killed by Spider Merchant.");
                        gameProc.WriteValue<double>(gameProc.currentHealthPointer, 0);    
                    }
                });
        }
    }
}
