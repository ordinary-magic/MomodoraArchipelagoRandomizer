using System;
using System.Windows.Forms;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Helpers;
using LiveSplit.UI.Components;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;

public enum Goal : int {
    AnyPercent = 0,
    TrueEnding = 1,
    AllItems = 2
}

// Used for both fastkill and deathlink
public enum Kill : int {
    Never = 0,
    BossOnly = 1,
    Always = 2
}

/*
 * Class to manage the archipelago state for the momodora randomizer
 */
public class Momo4Archipelago {

    // Constants
    public const int FINAL_BOSS_LOCATION = MomodoraArchipelagoRandomizer.RANDOMIZER_SOURCE_AMOUNT;

    // Delegates
    public delegate void GiveItemDelegate(int id);
    public delegate void KillPlayerDelegate(string cause);
    public delegate void SendMessageDelegate(string text);
    public delegate void SetupLocationDelegate(int locationID, int itemID, string name);

    // Fields
    private ArchipelagoSession apSession;
    private long apBaseIDOffset; // Archipelago server items/locations will start at this number instead of 0
    public int receivedItemCount; // Track how many items the server has sent us
    private int savedItemCount; // Track how many items our game has saved
    private Kill deathLinkSetting;
    private int deathLinkAmount;
    private int deathLinkCounter;
    private Kill fastKillSetting;
    private Goal goalSetting;
    private GiveItemDelegate GiveItem;
    private KillPlayerDelegate KillPlayer;
    private readonly SendMessageDelegate SendMessage;
    private string url; // Archipelago server url
    private int port; // Archipelago server port
    private string slot; // Archipelago server slot name
    private string password; // Archipelago server password
    private List<long> offlineLocations; // place to store locations scouted while offline that the server needs know about
    private bool connecting; // flag for if the server is trying to (re)connect

    // Properties
    private DeathLinkService DeathLink {get; set;}
    public bool HasSaveData { get => savedItemCount != 0; }
    public bool IgnoreNextDeath {get; set;} = false; // tell archipelago not to deathlink the next time the player dies
    

    /*
     * Construct the archipelago object
     * SendMessage: A delegate void(str) that can be used to send meesages to the player.
     */
    public Momo4Archipelago(SendMessageDelegate SendMessage) {
        this.SendMessage = SendMessage;
        offlineLocations = new List<long>();
        connecting = false;
        savedItemCount = 0;
    }

    /*
    * Connect to the Archipelago Server
    */
    public bool ConnectArchipelago(string url, int port, string slot, string password){
        connecting = true;        

        var login = TryToConnect(url, port, slot, password);
       
        if (login is LoginFailure) {
            // Try connecting twice (this works surprisingly often (the library is not good))
            login = TryToConnect(url, port, slot, password);

            if (login is LoginFailure failure) {
                MessageBox.Show($"Could not connect to archipelago server {url}:{port}:\n" + string.Join("\n", failure.Errors));
                SendMessage(failure.Errors[0]);
                return false;
            }
        }

        // Get settings and protocol info from the server
        var slotData = ((LoginSuccessful)login).SlotData;
        goalSetting = (Goal)Convert.ToInt32(slotData["goal"]);
        fastKillSetting = (Kill)Convert.ToInt32(slotData["fast_kill"]);
        apBaseIDOffset = Convert.ToInt64(slotData["base_offset"]);

        // Setup the deathlink
        deathLinkSetting = (Kill)Convert.ToInt32(slotData["deathlink"]);
        if(deathLinkSetting != Kill.Never){
            deathLinkAmount = Convert.ToInt32(slotData["dl_amount"]);
            deathLinkCounter = deathLinkAmount;
            DeathLink = apSession.CreateDeathLinkService();
            DeathLink.EnableDeathLink();
        }

        // Read the item count from the data storage
        apSession.DataStorage[Scope.Slot, "item_count"].Initialize(0); // only sets to 0 if this doesnt yet exist
        savedItemCount = apSession.DataStorage[Scope.Slot, "item_count"];
        receivedItemCount = savedItemCount;

        return true;
    }

    /*
    * Try to create a new apSession to the server using the provided address and login information.
    */
    private LoginResult TryToConnect(string url, int port, string slot, string password) {
        // Connect to the archipelago server
        LoginResult login = null;

        try {
            SendMessage($"Connecting to Archipelago Server {url}:{port}...");
            apSession = ArchipelagoSessionFactory.CreateSession(url, port);

            // Login to the server
            SendMessage($"Logging in as {slot}...");
            login = apSession.TryConnectAndLogin("Momodora 4 - Reverie Under the Moonlight", slot,
                    ItemsHandlingFlags.AllItems, password: password);

        } catch (Exception e) {
            return new LoginFailure(e.Message);
        }

        // Save the successful login info
        this.url = url;
        this.port = port;
        this.slot = slot;
        this.password = password;

        return login;
    }

    /*
     * Initialize the Randomizer
     */
    public async Task InitializeRandomizer(GiveItemDelegate GiveItem, KillPlayerDelegate KillPlayer, SetupLocationDelegate SetupLocation) {
        this.GiveItem = GiveItem;
        apSession.Items.ItemReceived += NewItemRecieved;
        ResyncArchipeligoItemsOnStartup(savedItemCount);

        // Setup Deathlink if enabled
        this.KillPlayer = KillPlayer;
        if (deathLinkSetting != Kill.Never)
            DeathLink.OnDeathLinkReceived += dl => KillPlayer(dl.Cause);

        // Get the locations from the server and setup our replacements
        SendMessage("Initializing Randomizer...");
        await SetupLocations(SetupLocation, apSession.Locations.ScoutLocationsAsync(false, apSession.Locations.AllLocations.ToArray()));

        connecting = false;
    }

    /*
     * Disconnect from the archipelago server
     */
    public async void Disconnect() {
        // Would prefer to synchronously wait on this but for some reason it takes almost 5 mins.
        await apSession.Socket.DisconnectAsync();
    }

    /*
     * Re-establish a lost connection to the archipelago server
     */
    public void Reconnect() {
        if (connecting)
            return; // dont reconnect if we are already connecting

        try {
            connecting = true;

            // Save this b4 reconnect to prevent race condition of getting new item btwn connection and start of resync
            int cachedItems = receivedItemCount;

            var login = TryToConnect(url, port, slot, password);

            if (login is LoginFailure failure) {
                SendMessage("Unable to Reconnect to Server");
                MessageBox.Show($"Could not reconnect to archipelago server:\n" + string.Join("\n", failure.Errors)); // Debug
                return;
            }

            // Re-initialize the deathlink service
            // Setup the deathlink
                if(deathLinkSetting != Kill.Never){
                    deathLinkCounter = deathLinkAmount;
                    DeathLink = apSession.CreateDeathLinkService();
                    DeathLink.OnDeathLinkReceived += dl => KillPlayer(dl.Cause);
                    DeathLink.EnableDeathLink();                    
                }

            // Get any items we've missed
            // Note: after a descync, AllItemsRecieved be out of order with actual game item ordering, but because
            // we dont get updates while offline and we ignore local items in GiveArchipelagoItem, it doesnt matter
            ResyncArchipeligoItemsOnStartup(cachedItems);

            // Re-sync our saved item counter with the server        
            apSession.DataStorage[Scope.Slot, "item_count"] = savedItemCount;

            // Issue any missed location checks
            if (offlineLocations.Count > 0) {
                apSession.Locations.CompleteLocationChecks(offlineLocations.ToArray());
                offlineLocations.Clear();
            }

        } finally {
            // No matter how we exit this block, we are no longer "connecting"
            connecting = false;
        }
    }

    /*
    * Async Method to process the list of item replacements reported by the archipelago server.
    */
    private async Task SetupLocations(SetupLocationDelegate SetupLocation, Task<Dictionary<long, ScoutedItemInfo>> scouterInfoTask){
        var infoReport = await scouterInfoTask; // wait for the task to finish

        foreach (var serverlocationID in infoReport.Keys) {
            int gameLocationID = (int)(serverlocationID - apBaseIDOffset);

            if (gameLocationID >= MomodoraArchipelagoRandomizer.RANDOMIZER_SOURCE_AMOUNT)
                continue; // Ignore "Special" archipelago locations

            // Extract the relevant info
            var scoutedItem = infoReport[serverlocationID];
            int gameItemID = (int) (scoutedItem.ItemId - apBaseIDOffset);
            string name = scoutedItem.ItemDisplayName ?? scoutedItem.ItemName;

            // Handle other player's items by making them an Archipelago item with a relevant name
            if (scoutedItem.Player.Slot != apSession.ConnectionInfo.Slot){
                gameItemID = (int) MomodoraArchipelagoRandomizer.Items.ArchipelagoItem;
                name = scoutedItem.Player.Name + "'s " + name;
            }

            // Setup the item replacement in the game's data.
            SetupLocation(gameLocationID, gameItemID, name);
        }
    }

    /*
    * Verify that every Location in the game has been checked 
    */
    private bool CheckAllItemsDone() {
        foreach (long locationID in apSession.Locations.AllMissingLocations)
            if ((int) (locationID - apBaseIDOffset) != FINAL_BOSS_LOCATION)
                return false;

        return true;
    }

    /*
    * Check if we have beaten the game and report to archipelago if we have.
    * haveGreenLeaf = true/false does the player have the green leaf item.
    */
    public void CheckVictoryConditionOnBossKill(bool haveGreenLeaf) {
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

    /*
     * Tell Archipelago we beat the game.
     */
    public void CompleteGame() {
        NotifyLocationChecked(FINAL_BOSS_LOCATION);
        apSession.Socket.SendPacket(new StatusUpdatePacket {
            Status = ArchipelagoClientState.ClientGoal
        });
    }

    /*
     * Reconstruct what items have been purchased based on what locations the server knows about.
     * shopLocationIDs - the original location # of each shop item
     * returns a list of shops, with true/false for each item in the shop
     */
    public List<List<bool>> GetBoughtItems(List<List<int>> shopLocationIDs) {
        // for every location in locationids, see if its been checked yet
        var boughtItems = new List<List<bool>>();
        var checkedItems = apSession.Locations.AllLocationsChecked;

        foreach (var shopContents in shopLocationIDs) {
            var shopState = new List<bool>();
            foreach (var itemID in shopContents)
                shopState.Add(checkedItems.Contains(itemID + apBaseIDOffset));
            boughtItems.Add(shopState);
        }

        return boughtItems;
    }

    /*
     * Give an archipelago item to the player
     */
    private void GiveArchipelagoItem(ItemInfo item){
        // Only give items that didnt come from this game
        if (item.Player.Slot != apSession.ConnectionInfo.Slot) {
            SendMessage($"Got {item.ItemName} from {item.Player.Name}!");
            GiveItem((int) (item.ItemId - apBaseIDOffset));
        } 
    }

    /*
    * Callback to process an item received event from the server
    * Note: AFAIK these should always be sent in the order issued by the server
    */
    private void NewItemRecieved(ReceivedItemsHelper helper) {
        receivedItemCount++;
        GiveArchipelagoItem(helper.PeekItem());
        helper.DequeueItem();
    }

    /*
    * Tell the Archipelago Server that we checked a location
    * location - the randomizer id of the location that was checked
    */
    public void NotifyLocationChecked(int location) {
        if (apSession.Socket.Connected)
            apSession.Locations.CompleteLocationChecks(location + apBaseIDOffset);
        else {
            offlineLocations.Add(location + apBaseIDOffset);
            Reconnect();
        }
    }

    /*
     * Tell archipelago that the player died (in case of deatlink games)
     * levelID = the id of the level where the player died.
     */
    public void NotifyPlayerDeath(int levelID) {
        if (IgnoreNextDeath) {
            IgnoreNextDeath = false;
            
        } else if (ShouldDeathLink(levelID)) {
            // Check which (if any) boss killed us
            string cause = null;
            if (MomodoraArchipelagoRandomizer.bossNameDictionary.Keys.Contains(levelID))
                cause = MomodoraArchipelagoRandomizer.bossNameDictionary[levelID];

            // Check if we have hit the requisite number of deaths
            // If we are disconnected, dont send the deathlink, but dont reset the counter either
            if (--deathLinkCounter <= 0 && apSession.Socket.Connected) {
                SendMessage($"Sending Death: {cause ?? "overworld"}");

                string name = apSession.Players.ActivePlayer.Name;
                string reason = $"{name} died to {cause ?? "the overworld"}";
                DeathLink.SendDeathLink(new DeathLink(slot, reason));

                deathLinkCounter = deathLinkAmount;

            } else if (apSession.Socket.Connected) {
                if (deathLinkCounter > 1)
                    SendMessage($"{deathLinkCounter} Lives Till Deathlink");
                else
                    SendMessage($"Next Death Will Deathlink.");
            }
        }
    }

    /*
    * Issue the player all items which were lost on death.
    */
    public void ResyncArchipeligoItemsOnDeath() {
        // Note: Safe from desync because recievedItemCount cant increase untill we are synced again
        int amount =  receivedItemCount - savedItemCount;

        // Re-issue the missing items
        foreach(var item in apSession.Items.AllItemsReceived.Skip(savedItemCount).Take(amount))
            GiveArchipelagoItem(item);
    }

    /*
    * Issue the player all items which they received while the client wasn't running.
    * itemsKnown = the number of items we already know about
    */
    private void ResyncArchipeligoItemsOnStartup(int itemsKnown) {
        // Re-issue the missing items
        foreach(var item in apSession.Items.AllItemsReceived.Skip(itemsKnown)) {
            receivedItemCount++;
            GiveArchipelagoItem(item);
        }
    }

    /*
     * Save local data to the server and update the saved item counter.
     */
    public void Save() {
        savedItemCount = receivedItemCount;

        if (apSession.Socket.Connected)
            apSession.DataStorage[Scope.Slot, "item_count"] = savedItemCount;
        else
            Reconnect();
    }

    /*
     * Say something in the archipelago chat
     */
    public void Say(string message){
        apSession.Socket.SendPacket(new SayPacket {
            Text = message
        });
    }

    /*
    * Check if the settings say a player should be killed per the level id.
    * setting - the kill setting to check (fastkill or deathlink)
    * levelID - the id of the room the player is currently in
    */
    private bool ShouldKill(Kill setting, int levelID) {
        if (setting == Kill.Always)
            return true;
        else if (setting == Kill.BossOnly) {
            return MomodoraArchipelagoRandomizer.bossNameDictionary.Keys.Contains(levelID);
        } else
            return false;
    }

    // Should the player be deathlinked at the specific level id
    public bool ShouldDeathLink(int levelID) => ShouldKill(deathLinkSetting, levelID);

    // Should the player be fast-killed at the specific level id
    public bool ShouldFastKill(int levelID) => ShouldKill(fastKillSetting, levelID);
}