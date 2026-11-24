using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Helpers;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Packets;
using System.Collections.Concurrent;

namespace Momo4Randomizer
{

    public enum Goal : int
    {
        AnyPercent = 0,
        TrueEnding = 1,
        AllItems = 2
    }

    // Used for both fastkill and deathlink
    public enum Kill : int
    {
        Never = 0,
        BossOnly = 1,
        Always = 2
    }

    /// <summary>
    /// Class to manage the archipelago state for the momodora randomizer
    /// </summary>
    public class ArchipelagoClient
    {
        // Constants
        public static readonly int FINAL_BOSS_LOCATION = GameInfo.RANDOMIZER_SOURCE_AMOUNT;
        public const int PROTOCOL_VERISON = 10400; // 1.04.00

        // Delegates
        public delegate void SetupLocationDelegate(int locationID, int itemID, string name);

        // Fields
        private long apBaseIDOffset; // Archipelago server items/locations will start at this number instead of 0
        private int savedItemCount = 0; // Track how many items our game has saved
        private Kill deathLinkSetting = Kill.Never;
        private int deathLinkAmount = 1;
        private int deathLinkCounter = 1;
        private Kill fastKillSetting = Kill.Never;
        private Goal goalSetting;
        private readonly ConcurrentQueue<ItemInfo> itemsGivenSinceLastSave = new ConcurrentQueue<ItemInfo>(); // Items we recieved since the last time we saved.
        private readonly List<long> offlineLocations = new List<long>(); // place to store locations scouted while offline that the server needs know about
        private readonly ArchipelagoSession apSession;
        private readonly Action<string> SendMessage;

        // Properties
        private DeathLinkService DeathLink { get; set; }
        public bool HasSaveData { get => savedItemCount != 0; }
        public bool IgnoreNextDeath { get; set; } = false; // tell archipelago not to deathlink the next time the player dies
        public bool Bugs { get; private set; } = false;
        public bool Vitality { get; private set; } = false;
        public bool HardMode { get; private set; } = false;

        /// <summary>
        /// Constructor for an AP client. (wish i could use primary constructors...)
        /// </summary>
        /// <param name="apSession">An active connection to the archipealgo server.</param>
        /// <param name="SendMessage">Delegate method to send a message to the player.</param>
        private ArchipelagoClient(ArchipelagoSession apSession, Action<string> SendMessage)
        {
            this.apSession = apSession;
            this.SendMessage = SendMessage;
        }

        /// <summary>
        /// Connect to the Archipelago Server using the provided connection info.
        /// </summary>
        /// <returns>The Archipelago client object if successfull, or null otherwise.</returns>
        public static ArchipelagoClient Connect(string url, int port, string slot, string password, Action<string> SendMessage)
        {
            var (login, session) = TryToConnect(url, port, slot, password, SendMessage);

            if (login is LoginFailure)
            {
                // Try connecting twice (this works surprisingly often (the library is not good))
                (login, session) = TryToConnect(url, port, slot, password, SendMessage);

                if (login is LoginFailure failure)
                {
                    //MessageBox.Show($"Could not connect to archipelago server {url}:{port}:\n" + string.Join("\n", failure.Errors));
                    SendMessage($"Could not connect to archipelago server {url}:{port}:\n" + string.Join("\n", failure.Errors));
                    SendMessage(failure.Errors[0]);
                }
            }

            if (!(login is LoginSuccessful success) || session == null)
                return null;

            var client = new ArchipelagoClient(session, SendMessage);
            client.InitializeSlotData(success.SlotData);

            return client;
        }

        /// <summary>
        /// Read the slot data from the server to extract the randomizer settings.
        /// </summary>
        /// <param name="slotData">Dictionary of settings from the archipelago server.</param>
        private void InitializeSlotData(Dictionary<string, object> slotData)
        {
            // Get settings and protocol info from the server
            goalSetting = (Goal)Convert.ToInt32(slotData["goal"]);
            fastKillSetting = (Kill)Convert.ToInt32(slotData["fast_kill"]);
            apBaseIDOffset = Convert.ToInt64(slotData["base_offset"]);

            // Check if the version matches
            int serverVersion = 0;
            if (slotData.TryGetValue("version", out object ver))
                serverVersion = Convert.ToInt32(ver);

            // This is fine, we are designed to be backwards compatible
            if (serverVersion <= PROTOCOL_VERISON)
                SendMessage("INFO: Apworld can be updated.");

            // This is potentially breaking
            else if (serverVersion >= PROTOCOL_VERISON)
                SendMessage("WARNING: Game Mod is out of date!");

            // Check if we are in hard mode (must use ContainsKey since old server versions dont have this)
            if (slotData.TryGetValue("hard_mode", out object value))
                HardMode = Convert.ToBoolean(value);

            // If the setting doesn't exist (because of an old apworld), check if we included a boss location
            else
                HardMode = apSession.Locations.AllLocations.Any(
                    loc => loc - apBaseIDOffset == GameInfo.BOSS_LOCATION_START);

            // Setup the deathlink
            deathLinkSetting = (Kill)Convert.ToInt32(slotData["deathlink"]);
            if (deathLinkSetting != Kill.Never)
            {
                deathLinkAmount = Convert.ToInt32(slotData["dl_amount"]);
                deathLinkCounter = deathLinkAmount;
                DeathLink = apSession.CreateDeathLinkService();
                DeathLink.EnableDeathLink();
            }

            // Read the item count from the data storage
            apSession.DataStorage[Scope.Slot, "item_count"].Initialize(0); // only sets to 0 if this doesnt yet exist
            savedItemCount = apSession.DataStorage[Scope.Slot, "item_count"];
        }

        /// <summary>
        /// Try to to the archipelago server, using the given credentials.
        /// </summary>
        /// <param name="log">An optional method to log connections status.</param>
        /// <returns>The login result, and the session (or null if it failed to connect).</returns>
        private static (LoginResult, ArchipelagoSession) TryToConnect(string url, int port, string slot, string password, Action<string> status)
        {
            try
            {
                status($"Connecting to Archipelago Server {url}:{port}...");
                var apSession = ArchipelagoSessionFactory.CreateSession(url, port);

                // Login to the server
                status($"Logging in as {slot}...");
                var result = apSession.TryConnectAndLogin("Momodora 4 - Reverie Under the Moonlight", slot,
                        ItemsHandlingFlags.AllItems, password: password);
                return (result, apSession);

            }
            catch (Exception e)
            {
                return (new LoginFailure(e.Message), null);
            }
        }

        /// <summary>
        /// Async method to Initialize the randomizer.
        /// </summary>
        /// <param name="KillPlayer">Delegate to kill the player (with a cause of death string).</param>
        /// <param name="GiveItem">Delegate to give an item to the player.</param>
        /// <param name="SetupLocation">Delegate to set a location's item.</param>
        public async Task InitializeRandomizer(Action<string> KillPlayer, Action<int> GiveItem, SetupLocationDelegate SetupLocation)
        {
            // Get the locations from the server and setup our replacements
            SendMessage("Initializing Randomizer...");
            await SetupLocations(SetupLocation, apSession.Locations.ScoutLocationsAsync(false, apSession.Locations.AllLocations.ToArray()));

            // Initialize Items
            GetNewArchipelagoItems(savedItemCount, GiveItem);
            apSession.Items.ItemReceived += helper => NewItemRecieved(helper, GiveItem);

            // Setup Deathlink if enabled
            if (DeathLink != null && deathLinkSetting != Kill.Never)
                DeathLink.OnDeathLinkReceived += dl => KillPlayer(dl.Cause);
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public async void Disconnect()
        {
            // Would prefer to synchronously wait on this but for some reason it takes almost 5 mins.
            await apSession.Socket.DisconnectAsync();
        }

        /// <summary>
        /// Async Method to process the list of item replacements reported by the archipelago server.
        /// </summary>
        /// <param name="SetupLocation">Delegate method to set the item at a given location</param>
        /// <param name="scouterInfoTask">A list of scouted locations and the items at each.</param>
        private async Task SetupLocations(SetupLocationDelegate SetupLocation, Task<Dictionary<long, ScoutedItemInfo>> scouterInfoTask)
        {
            var infoReport = await scouterInfoTask; // wait for the task to finish

            foreach (var serverlocationID in infoReport.Keys)
            {
                int gameLocationID = (int)(serverlocationID - apBaseIDOffset);

                if (gameLocationID >= GameInfo.RANDOMIZER_SOURCE_AMOUNT)
                    continue; // Ignore "Special" archipelago locations

                // Extract the relevant info
                var scoutedItem = infoReport[serverlocationID];
                int gameItemID = (int)(scoutedItem.ItemId - apBaseIDOffset);
                string name = scoutedItem.ItemDisplayName ?? scoutedItem.ItemName;

                // Set ivory bug and vitality fragment flags
                if (gameItemID == (int)Items.IvoryBug) Bugs = true;
                if (gameItemID == (int)Items.VitalityFragment) Vitality = true;

                // Handle other player's items by making them an Archipelago item with a relevant name
                if (scoutedItem.Player.Slot != apSession.ConnectionInfo.Slot)
                {
                    gameItemID = (int)Items.ArchipelagoItem;
                    name = scoutedItem.Player.Name + "'s " + name;
                }

                // Setup the item replacement in the game's data.
                SetupLocation(gameLocationID, gameItemID, name);
            }
        }

        /// <summary>
        /// Verify that every Location in the game has been checked
        /// </summary>
        private bool CheckAllItemsDone()
        {
            foreach (long locationID in apSession.Locations.AllMissingLocations)
                if ((int)(locationID - apBaseIDOffset) != FINAL_BOSS_LOCATION)
                    return false;

            return true;
        }

        /// <summary>
        /// Check if we have beaten the game and report to archipelago if we have.
        /// </summary>
        /// <param name="haveGreenLeaf">does the player have the green leaf item.</param>
        public void CheckVictoryConditionOnBossKill(bool haveGreenLeaf)
        {
            // 100% Needs every item
            if (goalSetting == Goal.AllItems)
                if (CheckAllItemsDone())
                    CompleteGame();
                else
                    SendMessage($"{apSession.Locations.AllMissingLocations.Count() - 1} Locations To Go");

            // True ending needs the green leaf
            else if (goalSetting == Goal.TrueEnding)
                if (haveGreenLeaf)
                    CompleteGame();
                else
                    SendMessage("Missing Green Leaf");

            // Any% need only beat the boss
            else CompleteGame();
        }

        /// <summary>
        /// Tell Archipelago we beat the game
        /// </summary>
        public void CompleteGame()
        {
            NotifyLocationChecked(FINAL_BOSS_LOCATION);
            apSession.Socket.SendPacket(new StatusUpdatePacket
            {
                Status = ArchipelagoClientState.ClientGoal
            });
        }

        /// <summary>
        /// Reconstruct what items have been purchased based on what locations the server knows about.
        /// </summary>
        /// <param name="shopLocationIDs">the original location # of each shop item</param>
        /// <returns>a list of shops, with true/false for each item in the shop</returns>
        public List<List<bool>> GetBoughtItems(List<List<int>> shopLocationIDs)
        {
            var boughtItems = new List<List<bool>>();
            var checkedItems = apSession.Locations.AllLocationsChecked;

            foreach (var shopContents in shopLocationIDs)
                boughtItems.Add(shopContents.Select(HasLocation).ToList());

            return boughtItems;
        }

        /// <summary>
        /// Give an archipelago item to the player
        /// Is asynchronous in case GiveItem is blocked so as to not softlock the archipelago client.
        /// </summary>
        /// <param name="GiveItem">A method delegate to grant the item to the player in the game.</param>
        /// <param name="item">The archipealgo item to give to the player.</param>
        private async void GiveArchipelagoItem(ItemInfo item, Action<int> GiveItem)
        {
            // Only give items that didnt come from this game
            if (item.LocationGame != apSession.ConnectionInfo.Game)
            {
                SendMessage($"Got {item.ItemName} from {item.Player.Name}!");
                await Task.Run(() => GiveItem((int)(item.ItemId - apBaseIDOffset)));
            }
        }

        /// <summary>
        /// Check archipelago to see if an item has been obtained by the player
        /// </summary>
        /// <param name="itemToCheck">the item to check</param>
        /// <returns>true/false indicating if the item has been obtained by the player</returns>
        public bool HasItem(int itemToCheck)
        {
            return apSession.Items.AllItemsReceived.Any(item => (item.ItemId - apBaseIDOffset) == itemToCheck);
        }

        /// <summary>
        /// Check archipelago to see which of the items in a list have been obtained by the player
        /// </summary>
        /// <param name="itemsToCheck">the items to check</param>
        /// <returns>a list of bools corresponding to each item in the list (true if obtained, false otherwise)</returns>
        public List<bool> HasItems(List<int> itemsToCheck)
        {
            return itemsToCheck.Select(HasItem).ToList();
        }

        /// <summary>
        /// Check archipelago to see if a location has been checked by the player
        /// </summary>
        /// <param name="gameLocationToCheck">the item to check</param>
        /// <returns>true/false indicating if the location has been checked by the player</returns>
        public bool HasLocation(int gameLocationToCheck)
        {
            return apSession.Locations.AllLocationsChecked.Contains(gameLocationToCheck + apBaseIDOffset);
        }

        /// <summary>
        /// Callback to process an item received event from the server.
        /// I'm assuming these are always sent in the order issued by the server (they seem to be).
        /// </summary>
        /// <param name="helper">Archipealgo.net event helper object with information about the item.</param>
        /// <param name="GiveItem">Delegate method to give an item to the player in game.</param>
        private void NewItemRecieved(ReceivedItemsHelper helper, Action<int> GiveItem)
        {
            GiveArchipelagoItem(helper.PeekItem(), GiveItem);
            itemsGivenSinceLastSave.Enqueue(helper.DequeueItem());
        }

        /// <summary>
        /// Tell the Archipelago Server that we checked a location
        /// </summary>
        /// <param name="location">the randomizer id of the location that was checked</param>
        public void NotifyLocationChecked(int location)
        {
            if (apSession.Socket.Connected)
                apSession.Locations.CompleteLocationChecks(location + apBaseIDOffset);
            else
            {
                offlineLocations.Add(location + apBaseIDOffset);
                apSession.Socket.ConnectAsync(); // try to reconnect
            }
        }

        /*
         * Tell archipelago that the player died (in case of deatlink games)
         * levelID = the id of the level where the player died.
         */
        public void NotifyPlayerDeath(int levelID)
        {
            if (IgnoreNextDeath)
            {
                IgnoreNextDeath = false;

            }
            else if (ShouldDeathLink(levelID))
            {
                // Check which (if any) boss killed us
                GameInfo.bossNameDictionary.TryGetValue(levelID, out string cause);

                // Check if we have hit the requisite number of deaths
                // If we are disconnected, dont send the deathlink, but dont reset the counter either
                if (--deathLinkCounter <= 0 && apSession.Socket.Connected)
                {
                    SendMessage($"Sending Death: {cause ?? "overworld"}");

                    string name = apSession.Players.ActivePlayer.Name;
                    string reason = $"{name} died to {cause ?? "the overworld"}";
                    DeathLink.SendDeathLink(new DeathLink(name, reason));

                    deathLinkCounter = deathLinkAmount;

                }
                else if (apSession.Socket.Connected)
                {
                    if (deathLinkCounter > 1)
                        SendMessage?.Invoke($"{deathLinkCounter} Lives Till Deathlink");
                    else
                        SendMessage?.Invoke($"Next Death Will Deathlink.");
                }
            }
        }

        /// <summary>
        /// Issue the player all items which were lost on death.
        /// </summary>
        /// <param name="GiveItem">Delegate to give an item to the player in game.</param>
        public void ResyncArchipeligoItemsOnDeath(Action<int> GiveItem)
        {
            // Re-issue the missing items
            while (itemsGivenSinceLastSave.TryDequeue(out var item))
                GiveArchipelagoItem(item, GiveItem);
        }

        /// <summary>
        /// Issue the player all items which they received while the client wasn't running.
        /// </summary>
        /// <param name="itemsKnown">the number of items we already know about</param>
        /// <param name="GiveItem">Delegate to give an item to the player in game.</param>
        private void GetNewArchipelagoItems(int itemsKnown, Action<int> GiveItem)
        {
            // Re-issue the missing items
            foreach (var item in apSession.Items.AllItemsReceived
                .Where(i => i.LocationGame != apSession.ConnectionInfo.Game)
                .Skip(itemsKnown))
            {
                GiveArchipelagoItem(item, GiveItem);
                itemsGivenSinceLastSave.Enqueue(item);
            }

            // Clear the item queue
            while (apSession.Items.Any())
                apSession.Items.DequeueItem();
        }

        /// <summary>
        /// Save local data to the server and update the saved item counter.
        /// </summary>
        public void Save()
        {
            // Have to clear the list one at a time due to api limits
            while (itemsGivenSinceLastSave.TryDequeue(out var _))
                savedItemCount++;

            if (apSession.Socket.Connected)
                apSession.DataStorage[Scope.Slot, "item_count"] = savedItemCount;
            else
                apSession.Socket.ConnectAsync(); // try to reconnect
        }

        /// <summary>
        /// Send a message in the archipealgo chat.
        /// </summary>
        public void Say(string message)
        {
            apSession.Socket.SendPacket(new SayPacket
            {
                Text = message
            });
        }

        /// <summary>
        /// Check if the settings say a player should be killed per the level id.
        /// </summary>
        /// <param name="setting">the kill setting to check (fastkill or deathlink)</param>
        /// <param name="levelID">the id of the room the player is currently in</param>
        /// <returns></returns>
        private static bool ShouldKill(Kill setting, int levelID)
        {
            if (setting == Kill.Always)
                return true;
            else if (setting == Kill.BossOnly)
            {
                return GameInfo.bossNameDictionary.ContainsKey(levelID);
            }
            else
                return false;
        }

        // Should the player be deathlinked at the specific level id
        public bool ShouldDeathLink(int levelID) => DeathLink != null && ShouldKill(deathLinkSetting, levelID);

        // Should the player be fast-killed at the specific level id
        public bool ShouldFastKill(int levelID) => ShouldKill(fastKillSetting, levelID);
        public bool ShouldSpiderMerchantKill() => true; // We dont actually de-prioritize these locations, so it should always kill. //goalSetting == Goal.AllItems;
    }
}