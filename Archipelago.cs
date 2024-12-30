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

public enum Goal : int {
    AnyPercent = 0,
    TrueEnding = 1,
    AllItems = 2
}

public enum FastKill : int {
    Never = 0,
    BossOnly = 1,
    Always = 2
}

/*
 * Class to manage the archipelago state for the momodora randomizer
 * TODO: Add connection state management tools (disconnect/reconnect)
 * TODO: Figure out how to make items/locations work around connection issues
 */
public class Momo4Archipelago {

    // Constants
    public const int FINAL_BOSS_LOCATION = MomodoraRandomizer.RANDOMIZER_SOURCE_AMOUNT;

    // Delegates
    public delegate void GiveItemDelegate(int id);
    public delegate void KillPlayerDelegate();
    public delegate void SendMessageDelegate(string text);
    public delegate void SetupLocationDelegate(int locationID, int itemID, string name);

    // Fields
    private ArchipelagoSession apSession;
    private long apBaseIDOffset; // Archipelago server items/locations will start at this number instead of 0
    public int receivedItemCount; // Track how many items the server has sent us
    private int savedItemCount; // Track how many items our game has saved
    private FastKill fastKillSetting;
    private Goal goalSetting;
    private GiveItemDelegate GiveItem;
    private readonly SendMessageDelegate SendMessage;

    // Properties
    public DeathLinkService DeathLink {get; private set;}
    public bool HasSaveData { get => savedItemCount != 0; }

    /*
     * Construct the archipelago object
     * SendMessage: A delegate void(str) that can be used to send meesages to the player.
     */
    public Momo4Archipelago(SendMessageDelegate SendMessage) {
        this.SendMessage = SendMessage;
    }

    /*
    * Connect to the Archipelago Server
    */
    public void ConnectArchipelago(string url, int port, string slot, string password){
        LoginResult login = null;

        // Connect to the archipelago server
        try {
            SendMessage($"Connecting to Archipelago Server {url}:{port}...");
            apSession = ArchipelagoSessionFactory.CreateSession(url, port);

            // Login to the server
            SendMessage($"Logging in as {slot}...");
            login = apSession.TryConnectAndLogin("Momodora 4 - Reverie Under the Moonlight", slot, 
                    ItemsHandlingFlags.AllItems, password: password);

        } catch (Exception e) {
            MessageBox.Show($"Could not connect to archipelago server {url}:{port}:\n" + e.Message);
            return;
        }
                
        if (login is LoginFailure failure) {
            MessageBox.Show($"Login Failed:\n" + string.Join("\n", failure.Errors));
            return;
        }

        var slotData = ((LoginSuccessful)login).SlotData;
        goalSetting = (Goal)Convert.ToInt32(slotData["goal"]);
        fastKillSetting = (FastKill)Convert.ToInt32(slotData["fast_kill"]);
        apBaseIDOffset = Convert.ToInt64(slotData["base_offset"]);

        if(Convert.ToInt32(slotData["deathlink"]) == 1){
            DeathLink = apSession.CreateDeathLinkService();
        }

        // Read the item count from the data storage
        apSession.DataStorage[Scope.Slot, "item_count"].Initialize(0); // only sets to 0 if this doesnt yet exist
        savedItemCount = apSession.DataStorage[Scope.Slot, "item_count"];
        receivedItemCount = savedItemCount;
    }

    /*
     * Initialize the Randomizer
     */
    public void InitializeRandomizer(GiveItemDelegate GiveItem, KillPlayerDelegate KillPlayer, SetupLocationDelegate SetupLocation){
        this.GiveItem = GiveItem;
        apSession.Items.ItemReceived += NewItemRecieved;
        ResyncArchipeligoItemsOnStartup();

        // Setup Deathlink if enabled
        if (DeathLink != null)
            DeathLink.OnDeathLinkReceived += _ => KillPlayer();

        // Get the locations from the server and setup our replacements
        SendMessage("Initializing Randomizer...");
        SetupLocations(SetupLocation, apSession.Locations.ScoutLocationsAsync(false, apSession.Locations.AllLocations.ToArray()));
    }

    /*
    * Async Method to process the list of item replacements reported by the archipelago server.
    */
    private async void SetupLocations(SetupLocationDelegate SetupLocation, Task<Dictionary<long, ScoutedItemInfo>> scouterInfoTask){
        var infoReport = await scouterInfoTask; // wait for the task to finish

        foreach (var serverlocationID in infoReport.Keys) {
            int gameLocationID = (int)(serverlocationID - apBaseIDOffset);

            if (gameLocationID >= MomodoraRandomizer.RANDOMIZER_SOURCE_AMOUNT)
                continue; // Ignore "Special" archipelago locations

            // Extract the relevant info
            var scoutedItem = infoReport[serverlocationID];
            int gameItemID = (int) (scoutedItem.ItemId - apBaseIDOffset);
            string name = scoutedItem.ItemDisplayName ?? scoutedItem.ItemName;

            // Handle other player's items by making them an Archipelago item with a relevant name
            if (scoutedItem.Player.Slot != apSession.ConnectionInfo.Slot){
                gameItemID = (int) MomodoraRandomizer.Items.ArchipelagoItem;
                name = scoutedItem.Player.Name + "'s " + name;
            }

            // Setup the item replacement in the game's data.
            SetupLocation(gameLocationID, gameItemID, name);
        }

        SendMessage("Randomization Complete!");
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
        apSession.Locations.CompleteLocationChecks(location + apBaseIDOffset);
    }

    /*
    * Issue the player all items which were lost on death.
    */
    public void ResyncArchipeligoItemsOnDeath() {
        int amount =  receivedItemCount - savedItemCount;

        // Re-issue the missing items
        foreach(var item in apSession.Items.AllItemsReceived.Skip(savedItemCount).Take(amount))
            GiveArchipelagoItem(item);
    }

    /*
    * Issue the player all items which they received while the client wasn't running.
    */
    private void ResyncArchipeligoItemsOnStartup() {
        // Re-issue the missing items
        foreach(var item in apSession.Items.AllItemsReceived.Skip(savedItemCount)) {
            receivedItemCount++;
            GiveArchipelagoItem(item);
        }
    }

    /*
     * Update revieved item counts to mark current items as saved in the game itself.
     */
    public void SaveItems() {
        savedItemCount = receivedItemCount;
        apSession.DataStorage[Scope.Slot, "item_count"] = savedItemCount;
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
    * Check if a fast kill should be performed on the player when they lose health
    * levelID - the id of the room the player is currently in
    */
    public bool ShouldFastKill(int levelID) {
        if (fastKillSetting == FastKill.Always)
            return true;
        else if (fastKillSetting == FastKill.BossOnly) {
            return MomodoraRandomizer.bossRoomLevelIDList.Contains(levelID);
        } else
            return false;
    }
}