using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LiveSplit.ComponentUtil;
using System.Threading.Tasks;

namespace Momo4Randomizer
{
    public class Randomizer
    {
        // Communication Objects //
        private GameMonitor gameProc;
        private ArchipelagoClient archipelago;
        private Action<string> ShowMessage;
        private Action<string> LogMessage;

        // Randomizer State //
        private bool shopping = false;
        private readonly List<int> deferredItemQueue = new List<int>();
        private string deferredDeathCause = null;
        private readonly object inventoryLock = new object();
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

        private Randomizer(GameMonitor game, ArchipelagoClient ap, Action<string> show, Action<string> log)
        {
            gameProc = game;
            archipelago = ap;
            ShowMessage = show;
            LogMessage = log;
        }

        /// <summary>
        /// Configure the speical misc memory watchers referenced by the randomizer.
        /// </summary>
        private void SetupSepcialMemoryWatchers()
        {
            // Configure the update LogMessages for the old memory watchers used throught this randomizer (mostly unchanged)
            gameProc.levelIDWatcher.OnChanged += CheckRoom;

            gameProc.inGameWatcher.OnChanged += (old, current) =>
            {
                if (!Convert.ToBoolean(current))
                    progress.LoadVariables(LogMessage);
            };

            gameProc.deathWatcher.OnChanged += (old, current) =>
            {
                // If the number of deaths has increased, we must have died
                if (current > old)
                {
                    progress.LoadVariables(LogMessage);
                    archipelago.NotifyPlayerDeath(gameProc.ReadValue<int>(gameProc.levelIDPointer));
                }
            };

            gameProc.saveWatcher.OnChanged += (old, current) =>
            {
                // If the number of times the game has been saved has increased, we must have saved
                if (current > old)
                {
                    progress.SaveVariables(LogMessage);
                    archipelago.Save();
                }
            };

            gameProc.convOpenWatcher.OnChanged += ConversationCheckIfShop;

            gameProc.currentHealthWatcher.OnChanged += (old, current) =>
            {
                if (old == 0 && current != 0)
                {
                    Debug.WriteLine("Respawning");
                    gameProc.UpdateChargeItemWatchers(); // Update the watchers so they know about the inventory reset.
                    archipelago.ResyncArchipeligoItemsOnDeath(ArchipelagoGrantItem);
                }

                // Check if HP loss should kill the player
                else if (current != 0 && current < old)
                    if (archipelago.ShouldFastKill(gameProc.ReadValue<int>(gameProc.levelIDPointer)))
                    {
                        gameProc.WriteValue<double>(gameProc.currentHealthPointer, 0);
                    }
            };

            // Setup the new Archipelago Watchers
            SetupFinalBossKillWatcher();
            SetupMeowWatcher();
            SetupSpiderWatcher();
        }

        /// <summary>
        /// Factory to create and initialze the randomizer.
        /// </summary>
        /// <param name="gameProc">The momodora reverie under the moonlight game process monitor.</param>
        /// <param name="url">The url of the ap server.</param>
        /// <param name="port">The port for the ap server.</param>
        /// <param name="slot">The slot number for the ap connection.</param>
        /// <param name="password">The password for the ap conenction.</param>
        /// <param name="ShowMessage">A delegate to show a message to the player.</param>
        /// <param name="LogMessage">A delegate to log a message in a file.</param>
        /// <returns>An awaitable task with an initialzed randomizer object, or null if it was not completed successfully.</returns>
        public async static Task<Randomizer> InitializeRandomizer(GameMonitor gameProc, string url, int port, string slot, string password, Action<string> ShowMessage, Action<string> LogMessage)
        {
            // Connect to the archipelago session
            if (ArchipelagoClient.Connect(url, port, slot, password, ShowMessage) is ArchipelagoClient client)
            {

                // Initialize the randomizer
                var rando = new Randomizer(gameProc, client, ShowMessage, LogMessage);
                rando.progress.LoadRandomizerState(client, LogMessage);
                await client.InitializeRandomizer(
                    rando.KillPlayer,
                    rando.ArchipelagoGrantItem,
                    rando.CreateLocationMemoryWatcherLogMessage
                );

                rando.BanShopItems();
                rando.SetupSepcialMemoryWatchers();

                // Check if we started the game in a shop room
                if (GameInfo.shopLocations.Contains(gameProc.ReadValue<int>(gameProc.levelIDPointer)))
                    rando.ModifyShopInventory(gameProc.ReadValue<int>(gameProc.levelIDPointer));

                ShowMessage("Randomization Complete!");

                return rando;
            }
            return null;
        }

        /// <summary>
        /// Function to setup a memory watcher to preform a specific location check.
        /// </summary>
        /// <param name="locationID">the id of the location to setup the watcher at</param>
        /// <param name="newItemID">the ID of the item to give at the location</param>
        /// <param name="itemName">the name of the item to show to the user</param>
        private void CreateLocationMemoryWatcherLogMessage(int locationID, int newItemID, string itemName)
        {
            if (8 <= locationID && locationID <= 19)// If item is a shop item
            {
                SaveShopItem(locationID, newItemID, itemName);
            }
            else
            {
                // Save the mappings in the debug file
                if (GameInfo.sourceIdMapping.TryGetValue(locationID, out int value))
                    Debug.WriteLine("Item '" + itemName + "' generated at " + Enum.GetName(typeof(Items), value));
                else
                    Debug.WriteLine("Item '" + itemName + " generated at position " + locationID);

                // Setup a memory watcher for the item source
                gameProc.CreateRandomizerSourceWatcher(gameProc.potentialSourcesPointers[locationID], (old, current) =>
                    {
                        // current is making sure we only do this if the item is added, levelID is for ???
                        int levelID = gameProc.ReadValue<int>(gameProc.levelIDPointer);
                        if (current == 1 && GameInfo.sourceToLevelMapping[locationID].Contains(levelID))
                        {
                            // track the spring leaf the log
                            if (locationID == 28) progress.hasBathedLeaf = true;

                            // Perform the item replacement
                            ReplaceItem(newItemID, gameProc.potentialSourcesPointers[locationID]);
                            ShowMessage($"Got {itemName}!");

                            // Tell Archipelago that we checked a location
                            archipelago.NotifyLocationChecked(locationID);
                        }
                    });

                // Whenever we enter this item's room, preload its name so we can display it on screen.
                int stringIndex = GameInfo.sourceToStringMapping[locationID];
                //Shop items are handled elsewhere
                if (stringIndex != -1)
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

        /// <summary>
        /// Adds a new item to the players inventory and removes the last item acquired
        /// </summary>
        /// <param name="id">id of item to give out</param>
        /// <param name="addr">IntPtr to source of item given out</param>
        /// <returns></returns>
        private bool ReplaceItem(int id, IntPtr addr)
        {
            lock (inventoryLock)
            {
                RemoveItem();

                // If it's our item, give it to the player
                if (id != (int)Items.ArchipelagoItem)
                    return GrantItem(id, addr);

                return true;
            }
        }

        /// <summary>
        /// Archipelago wrapper for the grant item function
        /// </summary>
        /// <param name="id">id of item to give</param>
        private void ArchipelagoGrantItem(int id)
        {
            lock (inventoryLock)
            {
                if (shopping)
                    deferredItemQueue.Add(id);
                else
                    GrantItem(id, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Grant all items in the deferred item queue.
        /// </summary>
        private void GrantDeferredItems()
        {
            lock (inventoryLock)
            {
                foreach (int id in deferredItemQueue)
                    GrantItem(id, IntPtr.Zero);
                deferredItemQueue.Clear();
            }
        }

        /// <summary>
        /// Update method which should be called on a timer by parent classes to update the memory watchers
        /// </summary>
        public void Update()
        {
            if (gameProc.InGame && gameProc.VerifyProcessRunning())
            {
                // Unsure if this is actually required? it seems like it should happen automatically?
                gameProc.UpdateSpecialWatchers();
                gameProc.UpdateRandomizerSourceWatchers();
            }
        }

        /// <summary>
        /// Adds a new item to the players inventory (with no replacement)
        /// </summary>
        /// <param name="id">id of item to give out</param>
        /// <param name="addr">IntPtr to source of item given out</param>
        /// <returns>Was the item actually given to the player.</returns>
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
            else if (id == (int)Items.VitalityChunk)
            {
                double fragments = gameProc.ReadValue<double>(gameProc.vitalityFragmentCountPointer);
                AdjustVitalityFragments((fragments > 0) ? 4 : 5); // Since there are 17 total, the first chunk is worth an extra
            }
            else if (id >= (int)Items.Munny1 && id <= (int)Items.Munny999)
            {
                // Increase the munny amount
                double currentMunny = gameProc.ReadValue<double>(gameProc.munnyPointer);
                double newMunny = currentMunny + GameInfo.MunnyAmounts[id - (int)Items.Munny1];
                gameProc.WriteValue(gameProc.munnyPointer, newMunny);
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
                else if (id == (int)Items.Bellflower || id == (int)Items.Passiflora || id == (int)Items.TaintedMissive || id == (int)Items.IvoryBug || id == (int)Items.IvorySwarm)
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
            if (res == true) LogMessage("\nItem " + id + " given, total items: " + totalItems + "\n");
            else LogMessage("\nUnable to give item " + id + ", total items: " + totalItems + "\n");
            return res;
        }

        private void AddLeaf()
        {
            gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[28], 1);
            progress.hasFoundGreenLeaf = true;
            AddItem((int)Items.FreshSpringLeaf);
        }

        /// <summary>
        /// Remove the most recently obtained item.
        /// </summary>
        /// <returns>The id of the removed item.</returns>
        private Items RemoveItem()
        {
            gameProc.UpdateChargeItemWatchers(); // Update the watchers to check if any charge item values changed.

            if (gameProc.taintedMissiveWatcher.Changed)
            {
                LogMessage("Removing missive\n");
                RemoveChargeItem((int)Items.TaintedMissive, gameProc.taintedMissiveWatcher.Current - gameProc.taintedMissiveWatcher.Old);
                return Items.TaintedMissive;
            }
            else if (gameProc.bellflowerWatcher.Changed)
            {
                LogMessage("\nRemoving bellflower\n");
                RemoveChargeItem((int)Items.Bellflower, gameProc.bellflowerWatcher.Current - gameProc.bellflowerWatcher.Old);
                return Items.Bellflower;
            }
            else if (gameProc.passifloraWatcher.Changed)
            {
                LogMessage("\nRemoving passiflora\n");
                RemoveChargeItem((int)Items.Passiflora, gameProc.passifloraWatcher.Current - gameProc.passifloraWatcher.Old);
                return Items.Passiflora;
            }
            else if (gameProc.ivoryBugWatcher.Changed && archipelago.Bugs)
            {
                LogMessage("\nRemoving IB\n");
                RemoveChargeItem((int)Items.IvoryBug, 1);
                return Items.IvoryBug;
            }
            else if (gameProc.crestFragmentWatcher.Changed)
            {
                LogMessage("\nRemoving Crest Frag\n");
                return RemoveCrestFragment();
            }
            else if (gameProc.vitalityFragmentWatcher.Changed && archipelago.Vitality)
            {
                LogMessage("\nRemoving Vit Frag\n");
                AdjustVitalityFragments(-1);
                return Items.VitalityFragment;
            }
            else if (gameProc.levelIDWatcher.Current == 83)
            {
                LogMessage("\nGreen leaf upgraded, do not remove stuff\n");
                return Items.None;
            }
            else
            {
                LogMessage("\nRemoving last item in inventory\n");
                return RemoveLastItem();
            }
        }

        private void AddChargeItem(int id)
        {
            IntPtr maxValuePointer, saveValuePtr;
            int index = GameInfo.chargeItems.IndexOf(id);
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
                case (int)Items.IvorySwarm:
                    maxValuePointer = gameProc.ivoryBugCountPointer;
                    saveValuePtr = IntPtr.Zero;
                    index = GameInfo.chargeItems.IndexOf((int)Items.IvoryBug);
                    break;
                // Cat Sphere is a charge item with infinite chages, should handle here, but i dont have the charge amount pointer
                default:
                    return;
            }
            bool hasItem = progress.hasChargeItem[index];
            double currentMaxCharges = hasItem ? gameProc.ReadValue<double>(maxValuePointer) : 0;
            int increase = GetChargeItemIncrease(id, currentMaxCharges);

            gameProc.WriteValue(maxValuePointer, increase + currentMaxCharges);
            if (saveValuePtr != IntPtr.Zero)
                gameProc.WriteValue(saveValuePtr, increase + currentMaxCharges);

            if (currentMaxCharges == 0 && !hasItem)
            {
                progress.hasChargeItem[index] = true;
                AddItem(GameInfo.chargeItems[index]);
            }
        }

        /// <summary>
        /// Calculate the amount of additional charges to give for a specific item/current charge amount.
        /// </summary>
        /// <remarks>
        /// currentCharges is stored internally as a double, which i dont trust enough to compare using ==
        /// </remarks>
        /// <param name="id">the item id we are adding charges to</param>
        /// <param name="currentCharges" the current amount of charges the item has.</param>
        /// <returns></returns>
        private int GetChargeItemIncrease(int id, double currentCharges)
        {
            switch (id)
            {
                case (int)Items.IvoryBug:
                    return 1; // Ivory Bugs are always worth one

                case (int)Items.IvorySwarm:
                    return 5; // Ivory Swarms are always worth five

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
            Items removedItem = (Items)PeekLastItem();

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
                        LogMessage("\nApplying door logic\n");
                        LogMessage("Door " + (unlocked ? "closed" : "opened") + "\n");
                        gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[j], unlocked ? 0 : 1); // flip the door state
                    }
                }
                else if (GameInfo.lockedDoorLocations[i].Contains(old) && !GameInfo.lockedDoorLocations[i].Contains(current))//If they just left the room revert state of flag
                {
                    gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[j], unlocked ? 0 : 1);
                }
            }

            // Enable or disable the player Y Watcher as appropriate
            if (ShouldEnableYWatcher(current))
                EnablePlayerYWatcher();
            else if (gameProc.playerYWatcher != null)
                gameProc.playerYWatcher.Enabled = false;

            // Update/Reset the shop items
            if (GameInfo.shopLocations.Contains(current))
                ModifyShopInventory(current);
            else if (GameInfo.shopLocations.Contains(old))
                ResetShopInventory(old);

            // DEBUG - set munny to 1000 every time you walk into a room
            //gameProc.WriteValue<double>(gameProc.munnyPointer, 1000);
        }
        
        // In some rooms, we track the player's Y value for various reasons.
        //   this onUpdate function combines those checks.
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
                    gameProc.WriteValue<double>(gameProc.potentialSourcesPointers[28], progress.hasFoundGreenLeaf ? 0 : 1);
                }
            }
        }

        // We should enable the player y watcher if we are in the sealed wind room,
        //   or if we have defeated lubella without the cat sphere, and are in the room where it teaches you how to turn back to escape 
        public bool ShouldEnableYWatcher(int room)
        {
            return (room == 82) || (room == 149 && gameProc.ReadValue<double>(gameProc.lubella2Pointer) > 0 && !progress.hasCatSphere);
        }

        // Enable the player Y watcher, and possibly set it up if it hasn't yet been created.
        public void EnablePlayerYWatcher()
        {
            // If the watcher doesn't yet exist, setup the position monitor and our callback
            if (gameProc.playerYWatcher == null)
            {
                gameProc.SetupPlayerPositionMonitor();
                gameProc.playerYWatcher.OnChanged += PlayerYOnUpdate;
            }

            // Turn it on
            gameProc.playerYWatcher.Enabled = true;
        }

        private void ConversationCheckIfShop(double old, double current)
        {
            int room = gameProc.ReadValue<int>(gameProc.levelIDPointer);// Get current room

            if (GameInfo.shopLocations.Contains(room))// If player is in a shop room
            {
                if (current != 0)
                { // If player is in a conversation with an npc
                    LogMessage("\nShopping...\n");
                    shopping = true;
                    AddBoughtItemPlaceholders(room);

                    // Before we shop, make sure to acknowledge any pending charge item updates.
                    gameProc.UpdateChargeItemWatchers();

                }
                else
                {
                    LogMessage("\nShopping complete!\n");
                    ReplacePurchases(room);
                    shopping = false;
                    GrantDeferredItems();
                    if (deferredDeathCause != null)
                    {
                        KillPlayer(deferredDeathCause);
                        deferredDeathCause = null;
                    }
                }
            }
        }

        /// <summary>
        /// Check if the player's inventory contains any of the placeholder shop items
        ///  and swap them with the appropriate randomized item instead.
        /// </summary>
        /// <remarks>
        /// In order to better control this method's timings, it now happens exactly once when the player leaves the shop.
        /// As long as i properly lock and defer server items while shopping, this should be much safer.
        /// </remarks>
        /// <param name="room">the level id of the current room.</param>
        private void ReplacePurchases(int room)
        {
            int currentShopLocation = GameInfo.shopLocations.IndexOf(room);
            List<int> currentShopItems = shopItems[currentShopLocation]; // get shop's randomized inventory

            lock (inventoryLock)
            {

                // Remove all purchased shop placeholder items from the player's inventory
                List<Items> purchasedItems = new List<Items>();
                while (GameInfo.shopPlaceholderItems.Contains(PeekLastItem()))
                {
                    purchasedItems.Add(RemoveItem());
                    LogMessage("Last acquired item: " + purchasedItems.Last() + "\n");
                }

                // Check each placeholder to see if it is a newly purchased item
                foreach (var placeholderItem in purchasedItems)
                {
                    int listPosition = GameInfo.shopPlaceholderItems.IndexOf((int)placeholderItem);

                    // Ensure the item is newly purchased, and not one of the bought item placeholders.
                    if (!progress.hasBoughtItem[currentShopLocation][listPosition])
                    {
                        // For each purchased item, give the player the real item instead
                        int originalItem = GameInfo.originalShopItems[currentShopLocation][listPosition];
                        ShowMessage($"Bought {shopItemNames[originalItem - GameInfo.originalShopItems[0][0]]}!");

                        // Update value of hasBoughtItem in all shops that "sell" originalItem
                        for (int i = 0; i < GameInfo.originalShopItems.Count(); i++)
                            for (int j = 0; j < GameInfo.originalShopItems[i].Count(); j++)
                                if (GameInfo.originalShopItems[i][j] == originalItem)
                                {
                                    LogMessage("Updating shop " + Enum.GetName(typeof(Shops), i) + ": position " + j + "\n");
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
            LogMessage("\nAt shop " + Enum.GetName(typeof(Shops), currentShopLocation) + "\n");
            foreach (var item in shopItems[currentShopLocation])
            {
                LogMessage("Items sold: " + Enum.GetName(typeof(Items), item) +
                    (progress.hasBoughtItem[currentShopLocation][shopItems[currentShopLocation].IndexOf(item)] ? " was bought" : " wasn't bought") + "\n");
            }
        }

        /// <summary>
        /// Add placeholders to the players inventory for any items already purchased,
        ///   which will force the in game shop menu will show the item as already purchased.
        /// </summary>
        /// <param name="room">the level id of the current room</param>
        private void AddBoughtItemPlaceholders(int room)
        {
            // Get list of what items have already been bought in the current shop
            List<bool> boughtItems = progress.hasBoughtItem[GameInfo.shopLocations.IndexOf(room)];

            for (int i = 0; i < boughtItems.Count(); i++)
                if (boughtItems[i]) // If item was bought add the placeholder
                {
                    AddItem(GameInfo.shopPlaceholderItems[i]);
                    LogMessage($"Adding placholder id {GameInfo.shopPlaceholderItems[i]}\n");
                }
        }

        /// <summary>
        /// Get the id of the most recently acquired item
        /// </summary>
        private int PeekLastItem()
        {
            int invSize = gameProc.ReadValue<int>(gameProc.totalItemsPointer);
            return (int)gameProc.ReadValue<double>(IntPtr.Add(gameProc.inventoryItemsStartPointer, 0x10 * (invSize - 1)));
        }

        /// <summary>
        /// Save the information about the item to be displayed in the shop.
        /// </summary>
        /// <param name="origin">The id of the original item.</param>
        /// <param name="swapped">The id of the new item.</param>
        /// <param name="name">The display name of the new item.</param>
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

        /// <summary>
        /// Ban disallowed items from the shop
        /// </summary>
        private void BanShopItems()
        {
            // Ban the pocket incensory - because this only appears if you dont have one, we must ban it
            SaveShopItem(19, (int)Items.ShopPlaceholder4, "Sold Out");
            progress.hasBoughtItem[6][2] = true;
        }

        private void ModifyShopInventory(int room)
        {
            IntPtr pointer, itemPointer;
            int currentShopLocation = GameInfo.shopLocations.IndexOf(room);
            List<int> list = GameInfo.GetShopOffsets(gameProc.Version)[currentShopLocation];
            List<int> shopItemsAux = shopItems[currentShopLocation];// Get list of items of the current shop
            int id = 22, pValue;
            byte[] bytes;

            LogMessage("\nSetting items for " + Enum.GetName(typeof(Shops), currentShopLocation) + " shop\n");
            for (int i = 0; i < list.Count(); i++)
            {
                id = GameInfo.shopPlaceholderItems[i];

                pointer = IntPtr.Add((IntPtr)new DeepPointer(gameProc.shopPointer).Deref<Int32>(gameProc.gameProc), 0x10 * list[i]);// Get pointer to shop item
                gameProc.WriteValue<double>(pointer, id);// Set shop placeholder to id

                // Vitality fragment and every custom item that comes after it dont have shop names.
                //   We also want to handle the placeholders specially too
                if (shopItemsAux[i] >= (int)Items.VitalityFragment ||
                    GameInfo.shopPlaceholderItems.Contains(shopItemsAux[i]))
                {
                    // Determine the item name and description
                    string name, description;
                    if (shopItemsAux[i] == (int)Items.ArchipelagoItem)
                    {
                        name = "AP Item"; // Respecting short item name by storing the variable string in the description
                                          // Get the replced item's id, then subtract it by id of the first shop item to get the index
                        description = shopItemNames[GameInfo.originalShopItems[currentShopLocation][i] - GameInfo.originalShopItems[0][0]];

                    }
                    else if (GameInfo.shopPlaceholderItems.Contains(shopItemsAux[i]))
                    {
                        // If it's a placeholder item, use the placeholder info
                        int index = GameInfo.shopPlaceholderItems.IndexOf(shopItemsAux[i]);
                        name = GameInfo.placeholderShopNames[index];
                        description = GameInfo.placeholderShopDescription[index];

                    }
                    else
                    {
                        // Otherwise, its a sepcial item. Use the name we saved when constructing the shop
                        name = shopItemNames[GameInfo.originalShopItems[currentShopLocation][i] - GameInfo.originalShopItems[0][0]];

                        // Check if we have a custom description for it
                        if (GameInfo.customItemShopDescriptions.TryGetValue(shopItemsAux[i], out string cDescription))
                            description = cDescription;
                        else description = name; // Otherwise, just use the name as the description
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

            LogMessage("\nResetting items for " + Enum.GetName(typeof(Shops), currentShopLocation) + " shop\n");
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

        /// <summary>
        /// Undo the randomization, and disconnect from the server.
        /// Return the game to its vanilla state so that a normal run can be played after saving and quitting.
        /// </summary>
        public void Teardown()
        {
            archipelago.Disconnect();
            ResetShops();
        }

        // TODO: this is causing an error to be thrown on close, need to investigate why
        private void ResetShops()
        {
            foreach (var room in GameInfo.shopLocations)
                ResetShopInventory(room);
        }

        /// <summary>
        /// Kill a player if requested (by deathlink or similar)
        /// </summary>
        /// <param name="cause">String describing the cause of the death.</param>
        private void KillPlayer(string cause)
        {
            if (archipelago.ShouldDeathLink(gameProc.ReadValue<int>(gameProc.levelIDPointer)))
            {
                if (!shopping)
                {
                    ShowMessage($"Killed by {cause}.");
                    archipelago.IgnoreNextDeath = true;
                    gameProc.WriteValue<double>(gameProc.currentHealthPointer, 0);
                }
                else deferredDeathCause = cause;
            }
        }

        /// <summary>
        /// Setup the memory watcher to detect the final boss kill 
        /// </summary>
        private void SetupFinalBossKillWatcher()
        {
            var watcher = gameProc.CreateSpecialMemoryWatcher<double>(gameProc.cutsceneStatePointer, (old, current) =>
                {
                    // Cutscene State 1000 is triggered on boss death. We check that + room id to ensure the right state.
                    int levelID = gameProc.ReadValue<int>(gameProc.levelIDPointer);
                    if (current == 1000 && levelID == GameInfo.FINAL_BOSS_LEVEL_ID)
                        archipelago.CheckVictoryConditionOnBossKill(progress.hasFoundGreenLeaf);
                });
        }

        /// <summary>
        /// Meow :3
        /// </summary>
        private void SetupMeowWatcher()
        {
            // Setup the memory watcher to check the game's memory every 10 milliseconds 
            var watcher = gameProc.CreateSpecialMemoryWatcher<double>(gameProc.playerFormPointer);

            // Trigger when the player's form changes
            watcher.OnChanged += (old, current) =>
            {
                archipelago.Say("Meow :3");
                watcher.Enabled = false;
            };
        }

        /// <summary>
        /// Setup the memory watcher to kill the player if the spider merchant dies. (anti-softlock)
        /// </summary>
        private void SetupSpiderWatcher()
        {
            var watcher = gameProc.CreateSpecialMemoryWatcher<double>(gameProc.spiderMerchantPointer, (old, current) =>
                {
                    //ShowMessage($"Spider: ({old} -> {current})."); // DEBUG
                    if (archipelago.ShouldSpiderMerchantKill() && current > old)
                    {
                        ShowMessage("Killed a Spider and Died of Guilt");
                        gameProc.WriteValue<double>(gameProc.currentHealthPointer, 0);
                    }
                });
        }
    }

}