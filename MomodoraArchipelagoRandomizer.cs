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
using System.Drawing.Drawing2D;
using System.IO;
using Archipelago.MultiClient.Net.Packets;

namespace LiveSplit.UI.Components
{
    public class MomodoraArchipelagoRandomizer : IComponent
    {
        public enum Items : int
        {
            AdornedRing = 1,
            NecklaceOfSacrifice = 2,
            Bellflower = 4,
            AstralCharm = 5,
            EdeaPearl = 6,
            DullPearl = 7,
            RedRing = 8,
            MagnetStone = 9,
            RottenBellflower = 10,
            FaerieTear = 11,
            ImpurityFlask = 13,
            Passiflora = 14,
            CrystalSeed = 15,
            MedalOfEquivalence = 16,
            TaintedMissive = 17,
            BlackSachet = 18,
            RingOfCandor = 21,
            SmallCoin = 22,
            BackmanPatch = 23,
            CatSphere = 24,
            HazelBadge = 25,
            TornBranch = 26,
            MonasteryKey = 27,
            ClarityShard = 31,
            DirtyShroom = 32,
            IvoryBug = 34,
            VioletSprite = 35,
            SoftTissue = 36,
            GardenKey = 37,
            SparseThread = 38,
            BlessingCharm = 39,
            HeavyArrows = 40,
            BloodstainedTissue = 41,
            MapleLeaf = 42,
            FreshSpringLeaf = 43,
            PocketIncensory = 44,
            Birthstone = 45,
            QuickArrows = 46,
            DrillingArrows = 47,
            SealedWind = 48,
            CinderKey = 49,
            FragmentBowPow = 50,
            FragmentBowQuick = 51,
            FragmentDash = 52,
            FragmentWarp = 53,
            VitalityFragment = 54,
            // The following are flages used by the randomizer
            ArchipelagoItem = 254,
            None = 255 // Placeholder item type
        };
        public enum shops : int
        {
            KarstCity = 0,
            ForlornMonsatery = 1,
            SubterraneanGrave = 2,
            WhiteleafMemorialPark = 3,
            CinderChambers_1 = 4,
            CinderChambers_2 = 5,
            RoyalPinacotheca = 6,
        }

        //Mapping of item source to what pointer points to the string that gets displayed when you pick it up.
        //-1 means that it does not have a string attached (shop source)
        private int[] sourceToStringMapping = new int[83] { 1, 2, 1, 6, 10, 18, 8, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 20, 22, 1, 24, 11, 12, 1, 9, 21, 30, 31, 3, 4, 14, 19, 13, 5, 25, 23, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 27, 26, 28, 29, 15, 16, 17 };
        public const int RANDOMIZER_SOURCE_AMOUNT = 83;
        const int GET_ITEM_STRING_AMNT = 32;

        private SimpleLabel RandomizerLabel;
        private Process gameProc = null;
        private double gameVersion;
        private int difficulty;
        private MomodoraArchipelagoRandomizerSettings settingsControl;
        public LiveSplitState state;
        private Momo4Archipelago archipelago;

        #region Randomizer logic fields
        private MemoryWatcherList randoSourceWatchers;
        private readonly object inventoryLock;
        private bool shopping;
        private List<int> deferredItemQueue = new List<int>();
        private string deferredDeathCause = null;

        //                                         CatSphere, Crest, Garden, Cinder, Mona, Haze, Soft, Dirty, Sealed Wind,Bugs
        private bool[,] requirementMatrix = new bool[9,10];

        private Dictionary<int, int> sourceIdMapping = new Dictionary<int, int>
        {
            [0] = (int)Items.Bellflower,
            [1] = (int)Items.AstralCharm,
            [2] = (int)Items.Bellflower,
            [3] = (int)Items.MagnetStone,
            [4] = (int)Items.GardenKey,
            [5] = (int)Items.CinderKey,
            [6] = (int)Items.MonasteryKey,
            [7] = (int)Items.TaintedMissive,
            [8] = (int)Items.CrystalSeed,
            [9] = (int)Items.FaerieTear,
            [10] = (int)Items.RingOfCandor,
            [11] = (int)Items.ClarityShard,
            [12] = (int)Items.NecklaceOfSacrifice,
            [13] = (int)Items.DullPearl,
            [14] = (int)Items.RedRing,
            [15] = (int)Items.DrillingArrows,
            [16] = (int)Items.ImpurityFlask,
            [17] = (int)Items.VioletSprite,
            [18] = (int)Items.QuickArrows,
            [19] = (int)Items.PocketIncensory,
            [20] = (int)Items.BlackSachet,
            [21] = (int)Items.SealedWind,
            [22] = (int)Items.Bellflower,
            [23] = (int)Items.Passiflora,
            [24] = (int)Items.DirtyShroom,
            [25] = (int)Items.CatSphere,
            [26] = (int)Items.Bellflower,
            [27] = (int)Items.SoftTissue,
            [28] = (int)Items.FreshSpringLeaf,
            [29] = (int)Items.BlessingCharm,
            [30] = (int)Items.RottenBellflower,
            [31] = (int)Items.EdeaPearl,
            [32] = (int)Items.BackmanPatch,
            [33] = (int)Items.SparseThread,
            [34] = (int)Items.PocketIncensory,
            [35] = (int)Items.TornBranch,
            [36] = (int)Items.TaintedMissive,
            [37] = (int)Items.BloodstainedTissue,
            [38] = (int)Items.HeavyArrows,
            [39] = (int)Items.VitalityFragment,
            [40] = (int)Items.VitalityFragment,
            [41] = (int)Items.VitalityFragment,
            [42] = (int)Items.VitalityFragment,
            [43] = (int)Items.VitalityFragment,
            [44] = (int)Items.VitalityFragment,
            [45] = (int)Items.VitalityFragment,
            [46] = (int)Items.VitalityFragment,
            [47] = (int)Items.VitalityFragment,
            [48] = (int)Items.VitalityFragment,
            [49] = (int)Items.VitalityFragment,
            [50] = (int)Items.VitalityFragment,
            [51] = (int)Items.VitalityFragment,
            [52] = (int)Items.VitalityFragment,
            [53] = (int)Items.VitalityFragment,
            [54] = (int)Items.VitalityFragment,
            [55] = (int)Items.VitalityFragment,
            [56] = (int)Items.IvoryBug,
            [57] = (int)Items.IvoryBug,
            [58] = (int)Items.IvoryBug,
            [59] = (int)Items.IvoryBug,
            [60] = (int)Items.IvoryBug,
            [61] = (int)Items.IvoryBug,
            [62] = (int)Items.IvoryBug,
            [63] = (int)Items.IvoryBug,
            [64] = (int)Items.IvoryBug,
            [65] = (int)Items.IvoryBug,
            [66] = (int)Items.IvoryBug,
            [67] = (int)Items.IvoryBug,
            [68] = (int)Items.IvoryBug,
            [69] = (int)Items.IvoryBug,
            [70] = (int)Items.IvoryBug,
            [71] = (int)Items.IvoryBug,
            [72] = (int)Items.IvoryBug,
            [73] = (int)Items.IvoryBug,
            [74] = (int)Items.IvoryBug,
            [75] = (int)Items.IvoryBug,
            [76] = (int)Items.FragmentBowQuick,
            [77] = (int)Items.FragmentDash,
            [78] = (int)Items.FragmentBowPow,
            [79] = (int)Items.FragmentWarp,
            [80] = (int)Items.Bellflower,
            [81] = (int)Items.HazelBadge,
            [82] = (int)Items.Passiflora
        };
        private Dictionary<int, int[]> sourceToLevelMapping = new Dictionary<int, int[]>
        {
            [0] = new int[] { 25 },
            [1] = new int[] { 37 },
            [2] = new int[] { 70, 64 },
            [3] = new int[] { 90 },
            [4] = new int[] { 134 },
            [5] = new int[] { 191 },
            [6] = new int[] { 113 },
            [7] = new int[] { 117 },
            [8] = new int[] { 63 },
            [9] = new int[] { 63, 111 },
            [10] = new int[] { 63, 181, 187 },
            [11] = new int[] { 127 },
            [12] = new int[] { 127 },
            [13] = new int[] { 160 },
            [14] = new int[] { 181 },
            [15] = new int[] { 187 },
            [16] = new int[] { 187 },
            [17] = new int[] { 199 },
            [18] = new int[] { 199 },
            [19] = new int[] { 199 },
            [20] = new int[] { 204 },
            [21] = new int[] { 220 },
            [22] = new int[] { 269 },
            [23] = new int[] { 261 },
            [24] = new int[] { 126 },
            [25] = new int[] { 149 },
            [26] = new int[] { 162 },
            [27] = new int[] { 103 },
            [28] = new int[] { 83 },
            [29] = new int[] { 52 },
            [30] = new int[] { 46 },
            [31] = new int[] { 53 },
            [32] = new int[] { 73 },
            [33] = new int[] { 141 },
            [34] = new int[] { 193 },
            [35] = new int[] { 153 },
            [36] = new int[] { 104 },
            [37] = new int[] { 97 },
            [38] = new int[] { 212 },
            [39] = new int[] { 39 },
            [40] = new int[] { 47 },
            [41] = new int[] { 67 },
            [42] = new int[] { 81 },
            [43] = new int[] { 144 },
            [44] = new int[] { 168 },
            [45] = new int[] { 191 },
            [46] = new int[] { 108 },
            [47] = new int[] { 35 },
            [48] = new int[] { 58 },
            [49] = new int[] { 185 },
            [50] = new int[] { 199 },
            [51] = new int[] { 205 },
            [52] = new int[] { 209 },
            [53] = new int[] { 270 },
            [54] = new int[] { 246 },
            [55] = new int[] { 264 },
            [56] = new int[] { 41 },
            [57] = new int[] { 50 },
            [58] = new int[] { 62 },
            [59] = new int[] { 78 },
            [60] = new int[] { 170 },
            [61] = new int[] { 191 },
            [62] = new int[] { 183 },
            [63] = new int[] { 40 },
            [64] = new int[] { 56 },
            [65] = new int[] { 123 },
            [66] = new int[] { 152 },
            [67] = new int[] { 155 },
            [68] = new int[] { 156 },
            [69] = new int[] { 102 },
            [70] = new int[] { 103 },
            [71] = new int[] { 211 },
            [72] = new int[] { 249 },
            [73] = new int[] { 255 },
            [74] = new int[] { 214 },
            [75] = new int[] { 213 },
            [76] = new int[] { 142 },
            [77] = new int[] { 195 },
            [78] = new int[] { 105 },
            [79] = new int[] { 60 },
            [80] = new int[] { 163 },
            [81] = new int[] { 163 },
            [82] = new int[] { 163 },
        };
        const int FINAL_BOSS_LEVEL_ID = 232;
        public static readonly Dictionary<int, string> bossNameDictionary = new Dictionary<int, string>() {
            {53, "Antrhopod Demon Edea"},
            {73, "Antrhopod Demon Edea"},
            {141, "Antrhopod Demon Edea"},
            {193, "Antrhopod Demon Edea"},
            {153, "Antrhopod Demon Edea"},
            {104, "Antrhopod Demon Edea"},
            {97, "Antrhopod Demon Edea"},
            {212, "Antrhopod Demon Edea"},
            {FINAL_BOSS_LEVEL_ID, "Accurst Queen of Karst"},

        };
        private static readonly List<int> shopPlaceholderItems = new List<int>() {
            22, 29, 45
        };
        #endregion

        #region log	
        private List<string> Events;
        #endregion

        #region pointers

        IntPtr[] itemGetStringPtrs;

        IntPtr[] potentialSourcesPointers;

        #region charge item pointers
        IntPtr bellflowerSaveValuePointer;
        IntPtr bellflowerMaxValuePointer;
        IntPtr taintedMissiveSaveValuePointer;
        IntPtr taintedMissiveMaxValuePointer;
        IntPtr passifloraSaveValuePointer;
        IntPtr passifloraMaxValuePointer;
        #endregion

        #region ivory bug pointers
        IntPtr ivoryBugCountPointer;
        #endregion

        #region vitality fragment pointers
        IntPtr vitalityFragmentCountPointer;
        IntPtr maxHealthPointer;
        IntPtr currentHealthPointer;
        #endregion

        #region crest fragment pointers
        IntPtr crestFragmentCountPointer;
        #endregion

        #region inventory pointers
        IntPtr activeItemsPointer;
        IntPtr passiveItemsPointer;
        IntPtr keyItemsPointer;
        IntPtr totalItemsPointer;
        IntPtr inventoryItemsStartPointer;
        IntPtr inventoryItemsChargeStartPointer;
        #endregion

        #region Shop pointers
        IntPtr munnyPointer;
        IntPtr invOpenPointer;
        IntPtr convOpenPointer;
        IntPtr shopPointer;
        IntPtr itemInfoPointer;
        #endregion

        #region misc pointers
        IntPtr difficultyPointer;
        IntPtr levelIDPointer;
        IntPtr inGamePointer;
        IntPtr saveAmountPointer;
        IntPtr deathAmountPointer;
        IntPtr warpStartPointer;
        IntPtr playerFormPointer;
        IntPtr playerXPointer;
        IntPtr playerYPointer;
        private IntPtr lubella2Pointer;
        private IntPtr cutsceneStatePointer;
        private IntPtr spiderMerchantPointer;
        #endregion
        #endregion

        #region Memory Watchers
        private MemoryWatcher<double> taintedMissiveWatcher;
        private MemoryWatcher<double> bellflowerWatcher;
        private MemoryWatcher<double> passifloraWatcher;
        private MemoryWatcher<double> ivoryBugWatcher;
        private MemoryWatcher<double> crestFragmentWatcher;
        private MemoryWatcher<double> vitalityFragmentWatcher;
        private MemoryWatcher<int> levelIDWatcher;
        private MemoryWatcher<double> deathWatcher;
        private MemoryWatcher<double> saveWatcher;
        private MemoryWatcher<double> inGameWatcher;
        private MemoryWatcher<double> convOpenWatcher;
        private MemoryWatcher<double> playerYWatcher;
        private MemoryWatcher<double> currentHealthWatcher;
        #endregion

        private bool randomizerRunning;
        private int itemGiven;
        private double[] healthChange;

        #region item acquired bools
        List<bool> hasChargeItem;
        List<bool> hasSavedChargeItem;
        private bool hasFoundGreenLeaf;
        private bool hasSavedFoundGreenLeaf;
        private bool hasBathedLeaf;
        private bool hasSavedBathedLeaf;
        private bool hasCatSphere;
        private bool hasSavedCatSphere;

        List<List<int>> doorLocations;
        private double unlocked;
        List<int> hasSavedKey;
        List<int> hasKey;
        private bool hasWarp;
        private bool hasSavedWarp;
        #endregion

        #region shop fields
        List<int> shopLocations;
        List<List<int>> originalShopItems;
        List<List<int>> shopItems;
        List<List<int>> shopOffsets;
        List<List<bool>> hasBoughtItem;
        List<List<bool>> hasSavedBoughtItem;
        List<string> shopItemNames;
        List<List<int>> pointerValues;
        List<string> itemNames = new List<string>
        {
            "Small Coin",
            "Karst Crest",
            "Birthstone",
        };
        List<string> itemEffects = new List<string>
        {
            "Key Item.",
            "Key Item. Allows warping when praying.",
            "Key Item.",
        };
        #endregion

        private bool inGame;

        public MomodoraArchipelagoRandomizer(LiveSplitState state)
        {
            this.state = state;
            RandomizerLabel = new SimpleLabel();
            settingsControl = new MomodoraArchipelagoRandomizerSettings();
            InitializeLists(); // Setup door and shop constants
            inventoryLock = new object();

            // Create the archipelago object
            archipelago = new Momo4Archipelago(SendMessage);

            // Register the live split methods.
            state.OnStart += OnStart;
            state.OnReset += OnReset;
        }
        
        private void InitializeLists()
        {
            //GardenKey, MonasteryKey, CinderKey
            doorLocations = new List<List<int>>
            {
                new List<int> { 145, 84},
                new List<int> { 183 },
                new List<int> { 99 }
            };
            //Karst City, Forlorn Monsatery, Subterranean Grave, Whiteleaf Memorial Park, Cinder Chambers 1, Cinder Chambers 2, Royal Pinacotheca
            shopLocations = new List<int>
            {
                63, 111, 127, 160, 181, 187, 205
            };
            //Karst City, Forlorn Monsatery, Subterranean Grave, Whiteleaf Memorial Park, Cinder Chambers 1, Cinder Chambers 2, Royal Pinacotheca
            originalShopItems = new List<List<int>>
            {
                new List<int> { 8, 9, 10 },
                new List<int> { 9 },
                new List<int> { 11, 12 },
                new List<int> { 13 },
                new List<int> { 14, 10 },
                new List<int> { 15, 10, 16 },
                new List<int> { 17, 18, 19 }
            };
            shopItemNames = new List<string>();
            foreach (int x in Enumerable.Range(8,12)){
                shopItemNames.Add(Enum.GetName(typeof(Items), sourceIdMapping[x]));
            }
        }

        private void OnReset(object sender, TimerPhase value)
        {
            randomizerRunning = false;
            LogResults();

            // Reset the shops so as to not affect other runs
            if (VerifyProcessRunning())
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
            if (VerifyProcessRunning())
            {
                SetupVersionDifferences();
                hasSavedChargeItem = new List<bool> { false, false, false };
                hasChargeItem = new List<bool> { false, false, false };
                hasSavedKey = new List<int> { 0, 0, 0 };
                hasKey = new List<int> { 0, 0, 0 };
                hasBathedLeaf = false;
                hasSavedBathedLeaf = false;
                hasFoundGreenLeaf = false;
                hasSavedFoundGreenLeaf = false;
                // Place original numbers for now, they get changed later
                shopItems = new List<List<int>>
                {
                    new List<int> { 15, 11, 21 },
                    new List<int> { 11 },
                    new List<int> { 31, 2 },
                    new List<int> { 7 },
                    new List<int> { 8, 21 },
                    new List<int> { 47, 21, 13 },
                    new List<int> { 35, 46, 44 }
                };
                hasBoughtItem = new List<List<bool>>
                {
                    new List<bool> { false, false, false },
                    new List<bool> { false },
                    new List<bool> { false, false },
                    new List<bool> { false },
                    new List<bool> { false, false },
                    new List<bool> { false, false, false },
                    new List<bool> { false, false, false }
                };
                hasSavedBoughtItem = new List<List<bool>>
                {
                    new List<bool> { false, false, false },
                    new List<bool> { false },
                    new List<bool> { false, false },
                    new List<bool> { false },
                    new List<bool> { false, false },
                    new List<bool> { false, false, false },
                    new List<bool> { false, false, false }
                };
                pointerValues = new List<List<int>>
                {
                    new List<int> { },
                    new List<int> { },
                    new List<int> { }
                };
                hasWarp = false;
                hasSavedWarp = false;
                Events = new List<string> { };

                Array.Clear(requirementMatrix, 0, requirementMatrix.Length);
                randoSourceWatchers = new MemoryWatcherList();

                levelIDWatcher = new MemoryWatcher<int>(levelIDPointer);
                levelIDWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                levelIDWatcher.Enabled = true;
                levelIDWatcher.OnChanged += (old, current) =>
                {
                    CheckRoom(old, current);
                };

                #region Special memory watchers
                taintedMissiveWatcher = new MemoryWatcher<double>(taintedMissiveMaxValuePointer);
                taintedMissiveWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                taintedMissiveWatcher.Enabled = true;

                bellflowerWatcher = new MemoryWatcher<double>(bellflowerMaxValuePointer);
                bellflowerWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                bellflowerWatcher.Enabled = true;

                passifloraWatcher = new MemoryWatcher<double>(passifloraMaxValuePointer);
                passifloraWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                passifloraWatcher.Enabled = true;

                ivoryBugWatcher = new MemoryWatcher<double>(ivoryBugCountPointer);
                ivoryBugWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                ivoryBugWatcher.Enabled = true;

                crestFragmentWatcher = new MemoryWatcher<double>(crestFragmentCountPointer);
                crestFragmentWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                crestFragmentWatcher.Enabled = true;

                vitalityFragmentWatcher = new MemoryWatcher<double>(vitalityFragmentCountPointer);
                vitalityFragmentWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                vitalityFragmentWatcher.Enabled = true;

                inGameWatcher = new MemoryWatcher<double>(inGamePointer);
                inGameWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                inGameWatcher.Enabled = true;
                inGameWatcher.OnChanged += (old, current) =>
                {
                    inGame = Convert.ToBoolean(current);
                    if (current == 0)
                    {
                        LoadVariables();
                    }
                };

                deathWatcher = new MemoryWatcher<double>(deathAmountPointer);
                deathWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                deathWatcher.Enabled = true;
                deathWatcher.OnChanged += (old, current) =>
                {
                    if (current > old)
                    {
                        LoadVariables();
                        archipelago.NotifyPlayerDeath(gameProc.ReadValue<int>(levelIDPointer));
                    }
                };

                saveWatcher = new MemoryWatcher<double>(saveAmountPointer);
                saveWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                saveWatcher.Enabled = true;
                saveWatcher.OnChanged += (old, current) =>
                {
                    if (current > old)
                    {
                        SaveVariables();
                        archipelago.Save();
                    }
                };

                convOpenWatcher = new MemoryWatcher<double>(convOpenPointer);
                convOpenWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                convOpenWatcher.Enabled = true;
                convOpenWatcher.OnChanged += (old, current) =>
                {
                    ConversationCheckIfShop(old, current);
                };
                shopping = false;

                //This needs to be setup whenever entering a new room, currently only used for green leaf stuff and only active in levelID 82. 
                //This is set up in the checkRoom function
                playerYWatcher = new MemoryWatcher<double>(playerYPointer);
                playerYWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                playerYWatcher.Enabled = false;
                playerYWatcher.OnChanged += (old, current) =>
                {
                    if (levelIDWatcher.Current == 82)
                    {
                        double pickedUpLeaf = gameProc.ReadValue<double>(potentialSourcesPointers[28]);
                        double playerXPos = gameProc.ReadValue<double>(playerXPointer);
                        if (playerYWatcher.Current > 176 && playerXPos > 410)
                            //If the state of "has picked green leaf source" is different from "found green leaf" invert
                            if (Convert.ToBoolean(pickedUpLeaf) != hasBathedLeaf) gameProc.WriteValue<double>(potentialSourcesPointers[28], 1 - pickedUpLeaf);
                        else { 
                            gameProc.WriteValue<double>(potentialSourcesPointers[28], pickedUpLeaf);
                        }
  
                    };
                };

                currentHealthWatcher = new MemoryWatcher<double>(currentHealthPointer);
                currentHealthWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                currentHealthWatcher.Enabled = true;
                currentHealthWatcher.OnChanged += (old, current) =>
                {
                    if(old == 0 && current != 0)
                    {
                        Debug.WriteLine("Respawning");
                        itemGiven = 3;
                        archipelago.ResyncArchipeligoItemsOnDeath();
                    }

                    // Check if HP loss should kill the player
                    else if (current != 0 && current < old)
                        if(archipelago.ShouldFastKill(gameProc.ReadValue<int>(levelIDPointer))){
                            gameProc.WriteValue<double>(currentHealthPointer, 0);
                        }
                };

                difficulty = (int) gameProc.ReadValue<double>(difficultyPointer);

                #endregion

                // Archipelago setup contains a lot of server connection and 
                // data retrieval waiting that is better done asynchronously.
                ConfigureArchipelagoAsync();
            }
        }

        private async void ConfigureArchipelagoAsync(){
            // Connect to the archipelago session
            if (archipelago.ConnectArchipelago(
                settingsControl.GetServerAddress(),
                settingsControl.GetServerPort(),
                settingsControl.GetSlot(),
                settingsControl.GetServerPassword()
            )) {
                if (archipelago.HasSaveData)
                    // If save data exists, that means we need to load the shop state.
                    hasBoughtItem = archipelago.GetBoughtItems(originalShopItems);

                else {
                    // This is a legacy from the other randomizer i dont fully understand.
                    //  It seems to be done in order to to initialize the "ivory bug" key item so it can be incremented later.
                    // Regardless, we only ever want to add that item once, at the start of the game.
                    AddItem((int)Items.IvoryBug);
                    itemGiven = 3;
                }

                // Initialize the randomizer
                await archipelago.InitializeRandomizer(
                    ArchipelagoGrantItem,
                    KillPlayer,
                    CreateLocationMemoryWatcherEvent
                );

                // Check if we started the game in a shop room
                if(shopLocations.Contains(gameProc.ReadValue<int>(levelIDPointer)))
                    ModifyShopInventory(gameProc.ReadValue<int>(levelIDPointer));

                // Setup misc source watchers that use archipelago
                randoSourceWatchers.Add(GetFinalBossKillWatcher());
                randoSourceWatchers.Add(GetMeowWatcher());
                //randoSourceWatchers.Add(GetSpiderWatcher()); // Doesnt work, cant find corresponding memory address

                SendMessage("Randomization Complete!");

                inGame = true;
                randomizerRunning = true;
            }
        }

        private void SaveVariables()
        {
            Event("\nSaving variables\n");
            Event("Item Charges & keys:\n");
            for (int i = 0; i < hasChargeItem.Count(); i++)
            {
                Event("SavedChargeItem[" + i+"]: " + hasSavedChargeItem[i] + " = ChargeItem["+i+"]: " + hasChargeItem[i] + "\n");
                Event("SavedKey["+i+"]: " + hasSavedKey[i] + " = Key["+i+"]: " + hasKey[i] + "\n");
                hasSavedChargeItem[i] = hasChargeItem[i];
                hasSavedKey[i] = hasKey[i];
            }
            Event("\nShop items:\n");
            for (int i = 0; i < hasBoughtItem.Count(); i++)
            {
                Event("\n" + Enum.GetName(typeof(shops), i) + " shop:\n");
                for (int j = 0; j < hasBoughtItem[i].Count(); j++)
                {
                    Event("SavedItemBought["+i+"]["+j+"]: " + hasSavedBoughtItem[i][j] +
                               " = ItemBought["+i+"]["+j+"]: " + hasBoughtItem[i][j] + "\n");
                    hasSavedBoughtItem[i][j] = hasBoughtItem[i][j];
                }
            }
            Event("\nSavedLeafBathed: " + hasSavedBathedLeaf + " = LeafBathed: " + hasBathedLeaf + "\n");
            hasSavedBathedLeaf = hasBathedLeaf;
            Event("SavedLeafFound: " + hasSavedFoundGreenLeaf + " = LeafFound: " + hasFoundGreenLeaf + "\n");
            hasSavedFoundGreenLeaf = hasFoundGreenLeaf;
            Event("SavedhasWarp: " + hasSavedWarp + " = hasWarp: " + hasWarp + "\n");
            hasSavedWarp = hasWarp;
            Event("SavedhasCatSphere: " + hasSavedCatSphere + " = hasCatSphere: " + hasCatSphere + "\n");
            hasSavedCatSphere = hasCatSphere;
            Event("\nFinished saving variables\n");
        }

        private void LoadVariables()
        {
            Event("\nLoading saved variables\n");
            Event("Item Charges & keys:\n");
            for (int i = 0; i < hasChargeItem.Count(); i++)
            {
                Event("ChargeItem["+i+"]: " + hasChargeItem[i] + " = SavedChargeItem["+i+"]: " + hasSavedChargeItem[i] + "\n");
                Event("Key["+i+"]: " + hasKey[i] +  " = SavedKey["+i+"]: " + hasSavedKey[i] + "\n");
                hasChargeItem[i] = hasSavedChargeItem[i];
                hasKey[i] = hasSavedKey[i];
            }
            Event("\nShop items:\n");
            for (int i = 0; i < hasBoughtItem.Count(); i++)
            {
                Event("\n" + Enum.GetName(typeof(shops), i) + " shop:\n");
                for (int j = 0; j < hasBoughtItem[i].Count(); j++)
                {
                    Event("hasBoughtItem["+i+"]["+j+"]: " + hasBoughtItem[i][j] + 
                               " = Saved["+i+"]["+j+"]: " + hasSavedBoughtItem[i][j] + "\n");
                    hasBoughtItem[i][j] = hasSavedBoughtItem[i][j];
                }
            }
            Event("LeafBathed: " + hasBathedLeaf + " = SavedLeafBathed: " + hasSavedFoundGreenLeaf + "\n");
            hasBathedLeaf = hasSavedBathedLeaf;
            Event("LeafFound: " + hasFoundGreenLeaf + " = SavedLeafFound: " + hasSavedWarp + "\n");
            hasFoundGreenLeaf = hasSavedFoundGreenLeaf;
            Event("hasWarp: " + hasWarp + " = SavedSavedhasWarp: " + hasSavedBathedLeaf + "\n");
            hasWarp = hasSavedWarp;
            Event("hasCatSphere: " + hasCatSphere + " = SavedhasCatSphere: " + hasSavedCatSphere + "\n");
            hasCatSphere = hasSavedCatSphere;
            Event("\nFinished loading variables\n");
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
                if(sourceIdMapping.ContainsKey(locationID))
                    Debug.WriteLine("Item '" + itemName + "' generated at " + Enum.GetName(typeof(Items), sourceIdMapping[locationID]));
                else
                    Debug.WriteLine("Item '" + itemName + " generated at position " + locationID);
                
                // Setup the memory watcher to check the game's memory every 10 milliseconds 
                MemoryWatcher<double> temp = new MemoryWatcher<double>(potentialSourcesPointers[locationID]) {
                    UpdateInterval = new TimeSpan(0, 0, 0, 0, 10)
                };

                // Add the event to trigger if the item is collected
                temp.OnChanged += (old, current) =>
                {
                    // current is making sure we only do this if the item is added, levelID is for ???
                    int levelID = gameProc.ReadValue<int>(levelIDPointer);
                    if (current == 1 && sourceToLevelMapping[locationID].Contains(levelID))
                    {
                        // track the spring leaf event
                        if (locationID == 28) hasBathedLeaf = true;
                        
                        // Perform the item replacement
                        ReplaceItem(newItemID, potentialSourcesPointers[locationID]);
                        RandomizerLabel.Text = $"Got {itemName}!";

                        // Tell Archipelago that we checked a location
                        archipelago.NotifyLocationChecked(locationID);
                    }
                };

                temp.Enabled = true;
                randoSourceWatchers.Add(temp);

                #region get item text
                int stringIndex = sourceToStringMapping[locationID];
                //Shop items are handled elsewhere
                if(stringIndex != -1)
                {
                    // On screen transitions, check for this location and swap its name in memory
                    levelIDWatcher.OnChanged += (old, current) =>
                    {
                        if (sourceToLevelMapping[locationID].Contains(current))
                        {
                            // Replace the old item name with the new one
                            SetupStringPtrs();
                            byte[] bytes = Encoding.ASCII.GetBytes(itemName);
                            gameProc.WriteBytes(itemGetStringPtrs[stringIndex], bytes);// Set name of placeholder to the one that will get added later
                            gameProc.WriteValue(itemGetStringPtrs[stringIndex] + bytes.Length, 0x0);// Add end of string
                        }
                    };
                }
                #endregion
            }
        }

        #region add/remove items
        /*
         * Adds a new item to the players inventory and removes the last item acquired
         * id = id of item to give out
         * addr = IntPtr to source of item given out
         */
        private bool ReplaceItem(int id, IntPtr addr)
        {
            lock(inventoryLock) {
                SetupItemPtrs();
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
            SetupItemPtrs();
            Debug.WriteLine("Giving item id: " + id);
            
            int allocatedMemory = gameProc.ReadValue<int>(IntPtr.Subtract(inventoryItemsStartPointer, 0x10));
            Debug.WriteLine("Allocated memory: " + allocatedMemory);
            int totalItems = gameProc.ReadValue<int>(totalItemsPointer);
            Debug.WriteLine("Total items: " + totalItems);

            if (id == (int)Items.IvoryBug)
            {
                AddIvoryBug();
            }
            else if (id == (int)Items.VitalityFragment)
            {
                AddVitalityFragment();
            }
            else if (totalItems * 16 != allocatedMemory)
            {
                if ((int)Items.FragmentWarp >= id && id >= (int)Items.FragmentBowPow)
                {
                    AddCrestFragment(id);
                    if (id == 53)
                    {
                        hasWarp = true;
                    }
                }
                else if (id == (int)Items.Bellflower || id == (int)Items.Passiflora || id == (int)Items.TaintedMissive)
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
            itemGiven = 3;
            if(res == true) Event("\nItem " + id + " given, total items: " + totalItems + "\n");
            else Event("\nUnable to give item " + id + ", total items: " + totalItems + "\n");
            return res;
        }
      
        private void AddLeaf()
        {
            gameProc.WriteValue<double>(potentialSourcesPointers[28], 1);
            hasFoundGreenLeaf = true;
            AddItem((int)Items.FreshSpringLeaf);
        }

        /*
         * Remove the most recently obtained item and report which item was removed.
         */
        private Items RemoveItem()
        {
            UpdateItemWatchers();

            if (taintedMissiveWatcher.Changed)
            {
                Event("Removing missive\n");
                RemoveChargeItem((int)Items.TaintedMissive, taintedMissiveWatcher.Current - taintedMissiveWatcher.Old);
                return Items.TaintedMissive;
            }
            else if (bellflowerWatcher.Changed)
            {
                Event("\nRemoving bellflower\n");
                RemoveChargeItem((int)Items.Bellflower, bellflowerWatcher.Current - bellflowerWatcher.Old);
                return Items.Bellflower;
            }
            else if (passifloraWatcher.Changed)
            {
                Event("\nRemoving passiflora\n");
                RemoveChargeItem((int)Items.Passiflora, passifloraWatcher.Current - passifloraWatcher.Old);
                return Items.Passiflora;
            }
            else if (ivoryBugWatcher.Changed)
            {
                Event("\nRemoving IB\n");
                RemoveIvoryBug();
                return Items.IvoryBug;
            }
            else if (crestFragmentWatcher.Changed)
            {
                Event("\nRemoving Crest Frag\n");
                return RemoveCrestFragment();
            }
            else if (vitalityFragmentWatcher.Changed)
            {
                Event("\nRemoving Vit Frag\n");
                RemoveVitalityFragment();
                return Items.VitalityFragment;
            }
            else if (levelIDWatcher.Current == 83)
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
            bool hasItem;
            int j;
            switch (id)
            {
                case (int)Items.Bellflower:
                    j = 0;
                    maxValuePointer = bellflowerMaxValuePointer;
                    saveValuePtr = bellflowerSaveValuePointer;
                    hasItem = hasChargeItem[j];
                    break;
                case (int)Items.Passiflora:
                    j = 1;
                    maxValuePointer = passifloraMaxValuePointer;
                    saveValuePtr = passifloraSaveValuePointer;
                    hasItem = hasChargeItem[j];
                    break;
                case (int)Items.TaintedMissive:
                    j = 2;
                    maxValuePointer = taintedMissiveMaxValuePointer;
                    saveValuePtr = taintedMissiveSaveValuePointer;
                    hasItem = hasChargeItem[j];
                    break;
                default:
                    return;
            }
            double currentMaxCharges = gameProc.ReadValue<double>(maxValuePointer);
            int charges = GetChargeItemIncrease(id, currentMaxCharges);
            gameProc.WriteValue<double>(maxValuePointer, charges + currentMaxCharges);
            gameProc.WriteValue<double>(saveValuePtr, charges + currentMaxCharges);
            if (currentMaxCharges == 0 && !hasItem)
            {
                hasChargeItem[j] = true;
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
            int j = 0;
            switch (id)
            {
                case (int)Items.Bellflower:
                    j = 0;
                    maxValuePointer = bellflowerMaxValuePointer;
                    saveValuePtr = bellflowerSaveValuePointer;
                    break;
                case (int)Items.Passiflora:
                    j = 1;
                    maxValuePointer = passifloraMaxValuePointer;
                    saveValuePtr = passifloraSaveValuePointer;
                    break;
                case (int)Items.TaintedMissive:
                    j = 2;
                    maxValuePointer = taintedMissiveMaxValuePointer;
                    saveValuePtr = taintedMissiveSaveValuePointer;
                    break;
                default:
                    return;
            }
            hasChargeItem[j] = true;
            currentCharges = gameProc.ReadValue<double>(maxValuePointer);
            gameProc.WriteValue<double>(maxValuePointer, currentCharges - charges);
            gameProc.WriteValue<double>(saveValuePtr, currentCharges - charges);
            
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
            SetupItemPtrs();
            var totalItemAmount = gameProc.ReadValue<int>(totalItemsPointer);
            if (id == (int)Items.CatSphere) hasCatSphere = true;
            gameProc.WriteValue<int>(totalItemsPointer, (int)totalItemAmount + 1);
            gameProc.WriteValue<double>(IntPtr.Add(inventoryItemsStartPointer, 0x10 * totalItemAmount), id);
        }

        private Items RemoveLastItem()
        {
            //To remove last item, decrease total item counter by one
            SetupItemPtrs();
            var totalItemAmount = gameProc.ReadValue<int>(totalItemsPointer);

            // Save the removed item's type so we can report it later.
            Items removedItem = (Items) PeekLastItem();

            gameProc.WriteValue<int>(totalItemsPointer, (int)totalItemAmount - 1);
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

        private void RemoveVitalityFragment()
        {
            double[] healthChange = { 0, 2, 1, 1 };
            double fragments = gameProc.ReadValue<double>(vitalityFragmentCountPointer);
            double health = gameProc.ReadValue<double>(maxHealthPointer);
            gameProc.WriteValue<double>(vitalityFragmentCountPointer, fragments - 1);
            gameProc.WriteValue<double>(maxHealthPointer, health - healthChange[(int)difficulty-1]);
        }

        private void AddVitalityFragment()
        {
            double fragments = gameProc.ReadValue<double>(vitalityFragmentCountPointer);
            double health = gameProc.ReadValue<double>(maxHealthPointer);
            gameProc.WriteValue<double>(vitalityFragmentCountPointer, fragments + 1);
            gameProc.WriteValue<double>(maxHealthPointer, health + healthChange[(int)difficulty - 1]);
        }

        private void AddIvoryBug()
        {
            double bugs = gameProc.ReadValue<double>(ivoryBugCountPointer);
            gameProc.WriteValue<double>(ivoryBugCountPointer, bugs + 1);
        }

        private void RemoveIvoryBug()
        {
            double bugs = gameProc.ReadValue<double>(ivoryBugCountPointer);
            gameProc.WriteValue<double>(ivoryBugCountPointer, bugs - 1);
        }

        private void AddKey(int id)
        {
            if (id == (int)Items.GardenKey)
            {
                hasKey[0] = 1;
            }
            else if (id == (int)Items.CinderKey)
            {
                hasKey[1] = 1;
            }
            else
            {
                hasKey[2] = 1;
            }

            AddItem(id);
        }

        private void CountCrestFragments()
        {
            int totalItems = gameProc.ReadValue<int>(totalItemsPointer);
            int fragments = 0;
            for (int i = 0; i < totalItems; i++)
            {
                double itemId = gameProc.ReadValue<double>(IntPtr.Add(inventoryItemsStartPointer, 0x10 * i));
                if ((int)Items.FragmentWarp >= itemId && itemId >= (int)Items.FragmentBowPow)
                {
                    fragments++;
                }
            }
            Debug.WriteLine("Found a total of " + fragments + " crest fragments");
            fragments = Math.Max(Math.Min(fragments, 4), 0);
            Debug.WriteLine("Writing " + fragments + " crest fragments");
            gameProc.WriteValue<double>(crestFragmentCountPointer, fragments);
        }
        #endregion

        private void CheckRoom(int old, int current)
        {
            #region key logic
            int j;

            //Garden = 4,
            //Cinder = 5,
            //Monastery = 6
            for (int i = 0; i < 3; i++)
            {
                j = i + 4;//Id for keys is 4-6

                if (doorLocations[i].Contains(current))//If the player is in a room with doors that pertaint to a key
                {
                    unlocked = gameProc.ReadValue<double>(potentialSourcesPointers[j]);//Read value for key acquired

                    if (unlocked != hasKey[i])//If the state of "key acquired" is different than it should be invert value
                    {
                        Event("\nApplying door logic\n");
                        Event("Door " + ((unlocked == 1) ? "closed" : "opened") + "\n");
                        gameProc.WriteValue<double>(potentialSourcesPointers[j], 1-unlocked);//1-0=1; 1-1=0
                    }
                }
                else if (doorLocations[i].Contains(old) && !doorLocations[i].Contains(current))//If they just left the room revert state of flag
                {
                    gameProc.WriteValue<double>(potentialSourcesPointers[j], unlocked);
                }
            }
            #endregion

            #region LubellaAntiSoftlock
            bool lubellaDefeated = Convert.ToBoolean(gameProc.ReadValue<double>(lubella2Pointer));
            if (current == 149 && lubellaDefeated && !hasCatSphere)
            {
                Event("\nApplying antisoftlock logic\n");
                switch (gameVersion)
                {
                    case 1.05:
                        playerXPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0253597C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x120);
                        playerYPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0253597C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x130);
                        break;
                    case 1.07:
                        playerXPointer = IntPtr.Add((IntPtr)new DeepPointer(0x025A2B3C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x120);
                        playerYPointer = IntPtr.Add((IntPtr)new DeepPointer(0x025A2B3C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x130);
                        break;
                }

                playerYWatcher = new MemoryWatcher<double>(playerYPointer);
                playerYWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                playerYWatcher.Enabled = true;
                playerYWatcher.OnChanged += (oldVal, currentVal) =>
                {
                    double playerXPos = gameProc.ReadValue<double>(playerXPointer);
                    Debug.WriteLine(playerXPos + "   " + currentVal);
                    if (currentVal > 195 && playerXPos > 120)
                    {
                        gameProc.WriteValue<double>(playerFormPointer, 1);
                    }
                    else
                    {
                        gameProc.WriteValue<double>(playerFormPointer, 0);
                    }
                };

            }
            else if (old == 149)
            {
                playerYWatcher.Enabled = false;
                Event("\nEnding antisoftlock logic\n");
            }
            #endregion

            #region Green Leaf logic
            //y/x pointers change each room, need to set it up again when entering the correct room
            if (current == 82)
            {
                switch (gameVersion)
                {
                    case 1.05:
                        playerXPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0253597C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x120);
                        playerYPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0253597C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x130);
                        break;
                    case 1.07:
                        playerXPointer = IntPtr.Add((IntPtr)new DeepPointer(0x025A2B3C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x120);
                        playerYPointer = IntPtr.Add((IntPtr)new DeepPointer(0x025A2B3C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x130);
                        break;
                }

                Event("\nApplying green leaf logic\n");
                playerYWatcher = new MemoryWatcher<double>(playerYPointer);
                playerYWatcher.UpdateInterval = new TimeSpan(0, 0, 0, 0, 10);
                playerYWatcher.Enabled = true;
                playerYWatcher.OnChanged += (oldVal, currentVal) =>
                {
                    double pickedUpLeaf = gameProc.ReadValue<double>(potentialSourcesPointers[28]);
                    double playerXPos = gameProc.ReadValue<double>(playerXPointer);
                    if (playerYWatcher.Current > 176 && playerXPos > 410)
                    {
                        //If the state of "has picked green leaf source" is different from "found green leaf" invert
                        gameProc.WriteValue<double>(potentialSourcesPointers[28], Convert.ToDouble(hasBathedLeaf));

                    }
                    else
                    {
                        gameProc.WriteValue<double>(potentialSourcesPointers[28], Convert.ToDouble(hasFoundGreenLeaf));
                    }
                };

            }
            else if (old == 82)
            {
                playerYWatcher.Enabled = false;
                Event("\nEnding green leaf logic\n");
            }
            #endregion

            // Update/Reset the shop items
            if (shopLocations.Contains(current))
                ModifyShopInventory(current);
            else if (shopLocations.Contains(old))
                ResetShopInventory(old);
        }

        private void ConversationCheckIfShop(double old, double current)
        {
            int room = gameProc.ReadValue<int>(levelIDPointer);// Get current room

            if (shopLocations.Contains(room))// If player is in a shop room
            {
                if (current != 0) { // If player is in a conversation with an npc
                    Event("\nShopping...\n");
                    shopping = true;
                    AddBoughtItemPlaceholders(room);
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
            int currentShopLocation = shopLocations.IndexOf(room);
            List<int> currentShopItems = shopItems[currentShopLocation]; // get shop's randomized inventory

            lock (inventoryLock) {

                // Remove all purchased shop placeholder items from the player's inventory
                List<Items> purchasedItems = new List<Items>();
                while (shopPlaceholderItems.Contains(PeekLastItem())) {
                    purchasedItems.Add(RemoveItem());
                    Event("Last acquired item: " + purchasedItems.Last() + "\n");
                }

                // Check each placeholder to see if it is a newly purchased item
                foreach (var placeholderItem in purchasedItems) {
                    int listPosition = shopPlaceholderItems.IndexOf((int)placeholderItem);

                    // Ensure the item is newly purchased, and not one of the bought item placeholders.
                    if (!hasBoughtItem[currentShopLocation][listPosition])
                    {
                        // For each purchased item, give the player the real item instead
                        int originalItem = originalShopItems[currentShopLocation][listPosition];
                        SendMessage($"Bought {shopItemNames[originalItem - originalShopItems[0][0]]}!");

                        // Update value of hasBoughtItem in all shops that "sell" originalItem
                        for (int i = 0; i < originalShopItems.Count(); i++) 
                            for (int j = 0; j < originalShopItems[i].Count(); j++)
                                if (originalShopItems[i][j] == originalItem)
                                {
                                    Event("Updating shop " + Enum.GetName(typeof(shops), i) + ": position " + j + "\n");
                                    hasBoughtItem[i][j] = true;
                                }

                        // Give the player the item if it's from this game
                        if (currentShopItems[listPosition] != (int)Items.ArchipelagoItem)
                            GrantItem(currentShopItems[listPosition],IntPtr.Zero);

                        // Tell Archipelago
                        archipelago.NotifyLocationChecked(originalItem);
                    }
                }
            }

            // Summarize the purchase
            Event("\nAt shop " + Enum.GetName(typeof(shops), currentShopLocation) + "\n");
            foreach (var item in shopItems[currentShopLocation])
            {
                Event("Items sold: " + Enum.GetName(typeof(Items), item) +
                    (hasBoughtItem[currentShopLocation][shopItems[currentShopLocation].IndexOf(item)] ? " was bought" : " wasn't bought") + "\n");
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
            List<bool> boughtItems = hasBoughtItem[shopLocations.IndexOf(room)];

            for (int i = 0; i < boughtItems.Count(); i++)
                if (boughtItems[i]) // If item was bought add the placeholder
                {
                    AddItem(shopPlaceholderItems[i]);
                    Event($"Adding placholder id {shopPlaceholderItems[i]}\n");
                }
        }

        /* 
         * Get the id of the most recently acquired item
         */
        private int PeekLastItem(){
            int invSize = gameProc.ReadValue<int>(totalItemsPointer);
            return (int)gameProc.ReadValue<double>(IntPtr.Add(inventoryItemsStartPointer, 0x10 * (invSize - 1)));
        }

        /*
         * Save the information about the item to be displayed in the shop.
         */
        private void SaveShopItem(int origin, int swapped, string name)
        {
            int itemPos, listPos;
            foreach (var list in originalShopItems)
            {
                if (list.Contains(origin))
                {
                    listPos = originalShopItems.IndexOf(list);
                    itemPos = list.IndexOf(origin);
                    shopItems[listPos][itemPos] = swapped;
                    shopItemNames[origin - originalShopItems[0][0]] = name;
                }
            }
        }

        private void ModifyShopInventory(int room)
        {
            IntPtr pointer, itemPointer;
            int currentShopLocation = shopLocations.IndexOf(room);
            List<int> list = shopOffsets[currentShopLocation];
            List<int> shopItemsAux = shopItems[currentShopLocation];// Get list of items of the current shop
            int id = 22, pValue;
            byte[] bytes;

            Event("\nSetting items for " + Enum.GetName(typeof(shops), currentShopLocation) + " shop\n");
            for (int i = 0; i < list.Count(); i++)
            {
                id = shopPlaceholderItems[i];

                pointer = IntPtr.Add((IntPtr)new DeepPointer(shopPointer).Deref<Int32>(gameProc), 0x10 * list[i]);// Get pointer to shop item
                gameProc.WriteValue<double>(pointer, id);// Set shop placeholder to id

                // Vitality Fragment & AP Items dont have a name or effect so we have to write to memory
                if (shopItemsAux[i] == (int)Items.VitalityFragment || shopItemsAux[i] == (int)Items.ArchipelagoItem)
                {
                    // Determine the item name and description
                    string name, description;
                    if (shopItemsAux[i] == (int)Items.VitalityFragment) {
                        // (trying to write as few bytes as possible)
                        name = "Vit. Frag.";
                        description = "+" + healthChange[(int)difficulty - 1] + " Max Hp";
                    } else {
                        name = "AP Item"; // Respecting short item name by storing the variable string in the description
                        // Get the replced item's id, then subtract it by id of the first shop item to get the index
                        description = shopItemNames[originalShopItems[currentShopLocation][i] - originalShopItems[0][0]];
                    }

                    bytes = Encoding.ASCII.GetBytes(name);
                    pointer = (IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * id) + 0x10), 0x0 }).Deref<Int32>(gameProc);// Get pointer to item name
                    gameProc.WriteBytes(pointer, bytes);// Set name of placeholder to the one that will get added later
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string

                    bytes = Encoding.ASCII.GetBytes(description);
                    pointer = (IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * id) + 0x20), 0x0 }).Deref<Int32>(gameProc);// Get pointer to item effect
                    gameProc.WriteBytes(pointer, bytes);// Set effect of placeholder to the one that will get added later
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string
                }
                else
                {
                    pointer = IntPtr.Add((IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * id) + 0x10) }).Deref<Int32>(gameProc), 0x0);// Get pointer of placeholder
                    pointerValues[i].Add(gameProc.ReadValue<int>(pointer));// Save value of original pointer
                    itemPointer = IntPtr.Add((IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * shopItemsAux[i]) + 0x10) }).Deref<Int32>(gameProc), 0x0);// Get pointer of real item
                    pValue = gameProc.ReadValue<int>(itemPointer);
                    gameProc.WriteValue(pointer, pValue);// Make placeholder pointer point to the real item's name

                    pointer = IntPtr.Add((IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * id) + 0x20) }).Deref<Int32>(gameProc), 0x0);// Get pointer of placeholder
                    pointerValues[i].Add(gameProc.ReadValue<int>(pointer));// Save value of original pointer
                    itemPointer = IntPtr.Add((IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * shopItemsAux[i]) + 0x20) }).Deref<Int32>(gameProc), 0x0);// Get pointer of real item
                    pValue = gameProc.ReadValue<int>(itemPointer);
                    gameProc.WriteValue(pointer, pValue);// Make placeholder pointer point to the real item's effect
                }
            }
        }

        private void ResetShopInventory(int room)
        {
            IntPtr pointer;
            int currentShopLocation = shopLocations.IndexOf(room);
            List<int> list = shopOffsets[currentShopLocation];
            List<int> shopItemsAux = originalShopItems[currentShopLocation];// Get list of original items for the current shop
            List<int> shopItemsAux2 = shopItems[currentShopLocation];// Get list of items of the current shop
            int id, idAux = 22;
            byte[] bytes;

            Event("\nResetting items for " + Enum.GetName(typeof(shops), currentShopLocation) + " shop\n");
            for (int i = 0; i < list.Count(); i++)
            {
                idAux = shopPlaceholderItems[i];
                id = shopItemsAux[i];

                pointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x10 * list[i]);// Get pointer to shop item
                gameProc.WriteValue<double>(pointer, (double)sourceIdMapping[id]);// Set shop item id to original one

                // check pointerValues[i] so livesplit doesnt freeze on onReset()
                if (shopItemsAux2[i] == 54 && pointerValues[i].Count == 0)
                {
                    bytes = Encoding.ASCII.GetBytes(itemNames[i]);
                    pointer = (IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * idAux) + 0x10), 0x0 }).Deref<Int32>(gameProc);// Get pointer to item name
                    gameProc.WriteBytes(pointer, bytes);// Restore name
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string

                    bytes = Encoding.ASCII.GetBytes(itemEffects[i]);
                    pointer = (IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * idAux) + 0x20), 0x0 }).Deref<Int32>(gameProc);// Get pointer to item effect
                    gameProc.WriteBytes(pointer, bytes);// Restore effect
                    gameProc.WriteValue<int>(pointer + bytes.Length, 0x0);// Add end of string
                }
                else if (pointerValues[i].Count > 0)
                {
                    pointer = IntPtr.Add((IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * idAux) + 0x10) }).Deref<Int32>(gameProc), 0x0);// Get pointer of placeholder
                    gameProc.WriteValue(pointer, pointerValues[i][0]);// Restore placeholder pointer value

                    pointer = IntPtr.Add((IntPtr)new DeepPointer(itemInfoPointer, new int[] { ((0x60 * idAux) + 0x20) }).Deref<Int32>(gameProc), 0x0);// Get pointer of placeholder
                    gameProc.WriteValue(pointer, pointerValues[i][1]);// Restore placeholder pointer value

                    pointerValues[i].Clear();// Remove placeholder pointer values
                }
            }
        }
        
        private void ResetShops() {
            foreach (var room in shopLocations)
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
                    File.AppendAllText(path, "Running on version " + gameVersion + "\n");
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
            if (VerifyProcessRunning() && randomizerRunning)
            {
                inGameWatcher.Update(gameProc);
                if (inGame) {
                    UpdateSpecialWatchers();
                    foreach (var watcher in randoSourceWatchers)
                    {
                        watcher.Update(gameProc);
                    }
                    if (itemGiven > 0)
                    {
                        UpdateItemWatchers();
                        itemGiven--;
                    }
                }

                if (invalidator != null)
                {
                    invalidator.Invalidate(0, 0, width, height);
                }
            }
        }

        private void UpdateSpecialWatchers()
        {
            levelIDWatcher.Update(gameProc);
            saveWatcher.Update(gameProc);
            deathWatcher.Update(gameProc);
            //munnyWatcher.Update(gameProc);
            //invOpenWatcher.Update(gameProc);
            convOpenWatcher.Update(gameProc);
            playerYWatcher.Update(gameProc);
            currentHealthWatcher.Update(gameProc);
        }

        private void UpdateItemWatchers()
        {
            taintedMissiveWatcher.Update(gameProc);
            passifloraWatcher.Update(gameProc);
            bellflowerWatcher.Update(gameProc);
            ivoryBugWatcher.Update(gameProc);
            crestFragmentWatcher.Update(gameProc);
            vitalityFragmentWatcher.Update(gameProc);
        }

        private void SetupVersionDifferences()
        {
            SetupIntPtrs();
            //Karst City, Forlorn Monsatery, Subterranean Grave, Whiteleaf Memorial Park, Cinder Chambers 1, Cinder Chambers 2, Royal Pinacotheca
            switch (gameVersion)
            {
                case 1.05:
                    shopOffsets = new List<List<int>>
                    {
                        new List<int> { 207, 203, 213 },
                        new List<int> { 203 },
                        new List<int> { 221, 199 },
                        new List<int> { 5 },
                        new List<int> { 8, 213 },
                        new List<int> { 235, 213, 205 },
                        new List<int> { 224, 234, 11 }
                    };
                    healthChange = new double[] { 0, 2, 1, 1 };
                    break;
                case 1.07:
                    shopOffsets = new List<List<int>>
                    {
                        new List<int> { 208, 204, 214 },
                        new List<int> { 204 },
                        new List<int> { 222, 200 },
                        new List<int> { 5 },
                        new List<int> { 8, 214 },
                        new List<int> { 236, 214, 206 },
                        new List<int> { 225, 235, 11 }
                    };
                    healthChange = new double[] { 2, 2, 1, 1 };
                    break;
            }
        }
        
        private void SetupIntPtrs()
        {
            switch (gameProc.MainModule.ModuleMemorySize)
            {
                case 39690240:
                    //version 1.05b
                    Debug.WriteLine("Version 1.05b detected");
                    Debug.WriteLine("Setting up pointers");
                    gameVersion = 1.05;
                    #region setting up IntPtrs
                    potentialSourcesPointers = new IntPtr[RANDOMIZER_SOURCE_AMOUNT];
                    potentialSourcesPointers[0] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xF0);
                    potentialSourcesPointers[1] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x100);
                    potentialSourcesPointers[2] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x410);
                    potentialSourcesPointers[3] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x430);
                    potentialSourcesPointers[4] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x700);
                    potentialSourcesPointers[5] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x9B0);
                    potentialSourcesPointers[6] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x260);
                    potentialSourcesPointers[7] = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x420);
                    potentialSourcesPointers[8] = IntPtr.Add((IntPtr)new DeepPointer(0x02304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xcf0);
                    potentialSourcesPointers[9] = IntPtr.Add((IntPtr)new DeepPointer(0x02304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xcb0);
                    potentialSourcesPointers[10] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xd50);
                    potentialSourcesPointers[11] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xdd0);
                    potentialSourcesPointers[12] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc70);
                    potentialSourcesPointers[13] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x50);
                    potentialSourcesPointers[14] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x80);
                    potentialSourcesPointers[15] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xeb0);
                    potentialSourcesPointers[16] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xcd0);
                    potentialSourcesPointers[17] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xe00);
                    potentialSourcesPointers[18] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xea0);
                    potentialSourcesPointers[19] = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xb0);
                    potentialSourcesPointers[20] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xA00);
                    potentialSourcesPointers[21] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xA90);
                    potentialSourcesPointers[22] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x6D0);
                    potentialSourcesPointers[23] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x690);
                    potentialSourcesPointers[24] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x480);
                    potentialSourcesPointers[25] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x720);
                    potentialSourcesPointers[26] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7E0);
                    potentialSourcesPointers[27] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x570);
                    potentialSourcesPointers[28] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x600);
                    potentialSourcesPointers[29] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x840);
                    potentialSourcesPointers[30] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x500);
                    potentialSourcesPointers[31] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x140);
                    potentialSourcesPointers[32] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x450);
                    potentialSourcesPointers[33] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7D0);
                    potentialSourcesPointers[34] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x9F0);
                    potentialSourcesPointers[35] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x580);
                    potentialSourcesPointers[36] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x270);
                    potentialSourcesPointers[37] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x6B0);
                    potentialSourcesPointers[38] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x670);
                    potentialSourcesPointers[39] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x150);
                    potentialSourcesPointers[40] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x190);
                    potentialSourcesPointers[41] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x170);
                    potentialSourcesPointers[42] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x1B0);
                    potentialSourcesPointers[43] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x180);
                    potentialSourcesPointers[44] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8C0);
                    potentialSourcesPointers[45] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8D0);
                    potentialSourcesPointers[46] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x160);
                    potentialSourcesPointers[47] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x940);
                    potentialSourcesPointers[48] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x1C0);
                    potentialSourcesPointers[49] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x1A0);
                    potentialSourcesPointers[50] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8E0);
                    potentialSourcesPointers[51] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8F0);
                    potentialSourcesPointers[52] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x900);
                    potentialSourcesPointers[53] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x930);
                    potentialSourcesPointers[54] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x920);
                    potentialSourcesPointers[55] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x910);
                    potentialSourcesPointers[56] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x280);
                    potentialSourcesPointers[57] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x290);
                    potentialSourcesPointers[58] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3A0);
                    potentialSourcesPointers[59] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2A0);
                    potentialSourcesPointers[60] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x300);
                    potentialSourcesPointers[61] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x310);
                    potentialSourcesPointers[62] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2F0);
                    potentialSourcesPointers[63] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x360);
                    potentialSourcesPointers[64] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x340);
                    potentialSourcesPointers[65] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2E0);
                    potentialSourcesPointers[66] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3B0);
                    potentialSourcesPointers[67] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2C0);
                    potentialSourcesPointers[68] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2D0);
                    potentialSourcesPointers[69] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2B0);
                    potentialSourcesPointers[70] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x390);
                    potentialSourcesPointers[71] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x320);
                    potentialSourcesPointers[72] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x370);
                    potentialSourcesPointers[73] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x380);
                    potentialSourcesPointers[74] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x350);
                    potentialSourcesPointers[75] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x330);
                    potentialSourcesPointers[76] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5B0);
                    potentialSourcesPointers[77] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5D0);
                    potentialSourcesPointers[78] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5A0);
                    potentialSourcesPointers[79] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5C0);
                    potentialSourcesPointers[80] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7A0);
                    potentialSourcesPointers[81] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7B0);
                    potentialSourcesPointers[82] = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7C0);

                    vitalityFragmentCountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xAE0);
                    crestFragmentCountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5f0);
                    ivoryBugCountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3C0);

                    bellflowerMaxValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x0230B134, new int[] { 0x14, 0x0 }).Deref<Int32>(gameProc), 0x1c0);
                    bellflowerSaveValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3f0);
                    taintedMissiveMaxValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x0230B134, new int[] { 0x14, 0x0 }).Deref<Int32>(gameProc), 0x6a0);
                    taintedMissiveSaveValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x400);
                    passifloraMaxValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x0230B134, new int[] { 0x14, 0x0 }).Deref<Int32>(gameProc), 0x580);
                    passifloraSaveValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x9d0);
                    difficultyPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x630);
                    maxHealthPointer = IntPtr.Add((IntPtr)new DeepPointer(0x02304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xa0);
                    currentHealthPointer = (IntPtr)new DeepPointer(0x02304CE8, new int[] { 0x4 }).Deref<Int32>(gameProc);

                    totalItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230b11c, new int[] { 0x1a4 }).Deref<Int32>(gameProc), 0x4);

                    activeItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304ce8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc30);
                    passiveItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304ce8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc40);
                    keyItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304ce8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x1100);
                    inventoryItemsStartPointer = (IntPtr)new DeepPointer(0x230b11c, new int[] { 0x1a4, 0xC }).Deref<int>(gameProc);
                    inventoryItemsChargeStartPointer = (IntPtr)new DeepPointer(0x230b11c, new int[] { 0x1a8, 0xC }).Deref<int>(gameProc);
                    levelIDPointer = IntPtr.Add(gameProc.MainModule.BaseAddress, 0x230F1A0);
                    deathAmountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x02304CE8, new int[] { 0x4 }).Deref<int>(gameProc), 0x540);
                    inGamePointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x780);
                    saveAmountPointer = (IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<int>(gameProc);
                    
                    munnyPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x550);
                    invOpenPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0xAC0);
                    convOpenPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x660);
                    shopPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304CE8).Deref<Int32>(gameProc), 0x4);
                    itemInfoPointer = (IntPtr)new DeepPointer(0x230B134, new int[] { 0x14 }).Deref<Int32>(gameProc);

                    warpStartPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<int>(gameProc), 0xB40);
                    playerFormPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4}).Deref<int>(gameProc), 0x760);
                    lubella2Pointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x560);
                    playerXPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0253597C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x120);
                    playerYPointer = IntPtr.Add((IntPtr)new DeepPointer(0x0253597C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x130);

	                cutsceneStatePointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc),0xAB0);
                    spiderMerchantPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x4, 0x4, 0x60, 0x4, 0x0 }).Deref<int>(gameProc), 0x860);
                    #endregion
                    RandomizerLabel.Text = "1.05b randomizer ready to go!";
                    break;
                case 40222720:
                    //version 1.07
                    Debug.WriteLine("Version 1.07 detected");
                    Debug.WriteLine("Setting up pointers");
                    gameVersion = 1.07;
                    #region setting up IntPtrs
                    potentialSourcesPointers = new IntPtr[RANDOMIZER_SOURCE_AMOUNT];
                    potentialSourcesPointers[0] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xF0);
                    potentialSourcesPointers[1] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x100);
                    potentialSourcesPointers[2] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x410);
                    potentialSourcesPointers[3] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x430);
                    potentialSourcesPointers[4] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x700);
                    potentialSourcesPointers[5] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x9B0);
                    potentialSourcesPointers[6] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x260);
                    potentialSourcesPointers[7] = IntPtr.Add((IntPtr)new DeepPointer(0x02379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x420);
                    potentialSourcesPointers[8] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xd00);
                    potentialSourcesPointers[9] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xcc0);
                    potentialSourcesPointers[10] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xd60);
                    potentialSourcesPointers[11] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xde0);
                    potentialSourcesPointers[12] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc80);
                    potentialSourcesPointers[13] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x60);
                    potentialSourcesPointers[14] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x70);
                    potentialSourcesPointers[15] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xec0);
                    potentialSourcesPointers[16] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xce0);
                    potentialSourcesPointers[17] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xe10);
                    potentialSourcesPointers[18] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xeb0);
                    potentialSourcesPointers[19] = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xb0);
                    potentialSourcesPointers[20] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xA00);
                    potentialSourcesPointers[21] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xA90);
                    potentialSourcesPointers[22] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x6D0);
                    potentialSourcesPointers[23] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x690);
                    potentialSourcesPointers[24] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x480);
                    potentialSourcesPointers[25] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x720);
                    potentialSourcesPointers[26] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7E0);
                    potentialSourcesPointers[27] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x570);
                    potentialSourcesPointers[28] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x600);
                    potentialSourcesPointers[29] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x840);
                    potentialSourcesPointers[30] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x500);
                    potentialSourcesPointers[31] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x140);
                    potentialSourcesPointers[32] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x450);
                    potentialSourcesPointers[33] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7D0);
                    potentialSourcesPointers[34] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x9F0);
                    potentialSourcesPointers[35] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x580);
                    potentialSourcesPointers[36] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x270);
                    potentialSourcesPointers[37] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x6B0);
                    potentialSourcesPointers[38] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x670);
                    potentialSourcesPointers[39] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x150);
                    potentialSourcesPointers[40] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x190);
                    potentialSourcesPointers[41] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x170);
                    potentialSourcesPointers[42] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x1B0);
                    potentialSourcesPointers[43] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x180);
                    potentialSourcesPointers[44] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8C0);
                    potentialSourcesPointers[45] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8D0);
                    potentialSourcesPointers[46] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x160);
                    potentialSourcesPointers[47] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x940);
                    potentialSourcesPointers[48] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x1C0);
                    potentialSourcesPointers[49] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x1A0);
                    potentialSourcesPointers[50] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8E0);
                    potentialSourcesPointers[51] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x8F0);
                    potentialSourcesPointers[52] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x900);
                    potentialSourcesPointers[53] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x930);
                    potentialSourcesPointers[54] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x920);
                    potentialSourcesPointers[55] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x910);
                    potentialSourcesPointers[56] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x280);
                    potentialSourcesPointers[57] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x290);
                    potentialSourcesPointers[58] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3A0);
                    potentialSourcesPointers[59] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2A0);
                    potentialSourcesPointers[60] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x300);
                    potentialSourcesPointers[61] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x310);
                    potentialSourcesPointers[62] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2F0);
                    potentialSourcesPointers[63] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x360);
                    potentialSourcesPointers[64] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x340);
                    potentialSourcesPointers[65] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2E0);
                    potentialSourcesPointers[66] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3B0);
                    potentialSourcesPointers[67] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2C0);
                    potentialSourcesPointers[68] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2D0);
                    potentialSourcesPointers[69] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x2B0);
                    potentialSourcesPointers[70] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x390);
                    potentialSourcesPointers[71] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x320);
                    potentialSourcesPointers[72] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x370);
                    potentialSourcesPointers[73] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x380);
                    potentialSourcesPointers[74] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x350);
                    potentialSourcesPointers[75] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x330);
                    potentialSourcesPointers[76] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5B0);
                    potentialSourcesPointers[77] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5D0);
                    potentialSourcesPointers[78] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5A0);
                    potentialSourcesPointers[79] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5C0);
                    potentialSourcesPointers[80] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7A0);
                    potentialSourcesPointers[81] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7B0);
                    potentialSourcesPointers[82] = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x7C0);

                    vitalityFragmentCountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0xAE0);
                    crestFragmentCountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x5f0);
                    ivoryBugCountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3C0);

                    bellflowerMaxValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x23782F4, new int[] { 0x14, 0x0 }).Deref<Int32>(gameProc), 0x1c0);
                    bellflowerSaveValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x3f0);
                    taintedMissiveMaxValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x23782F4, new int[] { 0x14, 0x0 }).Deref<Int32>(gameProc), 0x6a0);
                    taintedMissiveSaveValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x400);
                    passifloraMaxValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x23782F4, new int[] { 0x14, 0x0 }).Deref<Int32>(gameProc), 0x580);
                    passifloraSaveValuePointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x9d0);
                    difficultyPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x630);
                    maxHealthPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xa0);
                    currentHealthPointer = (IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc);

                    totalItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x023782DC, new int[] { 0x1ac }).Deref<Int32>(gameProc), 0x4);

                    activeItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc40);
                    passiveItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc50);
                    keyItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x1110);
                    inventoryItemsStartPointer = (IntPtr)new DeepPointer(0x23782DC, new int[] { 0x1ac, 0xC }).Deref<int>(gameProc);
                    inventoryItemsChargeStartPointer = (IntPtr)new DeepPointer(0x23782DC, new int[] { 0x1b0, 0xC }).Deref<int>(gameProc);
                    levelIDPointer = IntPtr.Add(gameProc.MainModule.BaseAddress, 0x237C360);
                    deathAmountPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<int>(gameProc), 0x530);
                    inGamePointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x790);
                    saveAmountPointer = (IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<int>(gameProc);
                    
                    munnyPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x540);
                    invOpenPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0xAD0);
                    convOpenPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x670);
                    shopPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8).Deref<Int32>(gameProc), 0x4);
                    itemInfoPointer = (IntPtr)new DeepPointer(0x23782F4, new int[] { 0x14 }).Deref<Int32>(gameProc);

                    warpStartPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<int>(gameProc), 0xB40);
                    playerFormPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<int>(gameProc), 0x760);
                    lubella2Pointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4, 0x60, 0x4, 0x4 }).Deref<Int32>(gameProc), 0x560);
                    playerXPointer = IntPtr.Add((IntPtr)new DeepPointer(0x025A2B3C, new int[] { 0xC, 0xBC, 0x8, 0x4}).Deref<int>(gameProc), 0x120);
                    playerYPointer = IntPtr.Add((IntPtr)new DeepPointer(0x025A2B3C, new int[] { 0xC, 0xBC, 0x8, 0x4 }).Deref<int>(gameProc), 0x130);

	                cutsceneStatePointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x0, 0x4 }).Deref<int>(gameProc),0xAC0);

                    // Based on the attached .CT files, the merchant flags should be one of these, but i cant seem to affect them  or any variant of them in game.
                    spiderMerchantPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x4, 0x4, 0x60, 0x4, 0x0}).Deref<int>(gameProc), 0x860);
                    //spiderMerchantPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x4, 0x4, 0x60, 0x4, 0x0}).Deref<int>(gameProc), 0xAF0);
                    #endregion
                    RandomizerLabel.Text = "1.07 randomizer ready to go!";
                    break;
                default:
                    RandomizerLabel.Text = "Unsupported game version for randomizer";
                    break;
            }
        }

        private void SetupItemPtrs()
        {
            switch (gameVersion)
            {
                case 1.05:
                    totalItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x230b11c, new int[] { 0x1a4 }).Deref<Int32>(gameProc), 0x4);
                    activeItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304ce8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc30);
                    passiveItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304ce8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc40);
                    keyItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2304ce8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x1100);
                    inventoryItemsStartPointer = (IntPtr)new DeepPointer(0x230b11c, new int[] { 0x1a4, 0xC }).Deref<int>(gameProc);
                    inventoryItemsChargeStartPointer = (IntPtr)new DeepPointer(0x230b11c, new int[] { 0x1a8, 0xC }).Deref<int>(gameProc);
                    break;
                case 1.07:
                    totalItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x023782DC, new int[] { 0x1ac }).Deref<Int32>(gameProc), 0x4);
                    activeItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc40);
                    passiveItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0xc50);
                    keyItemsPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2371EA8, new int[] { 0x4 }).Deref<Int32>(gameProc), 0x1110);
                    inventoryItemsStartPointer = (IntPtr)new DeepPointer(0x23782DC, new int[] { 0x1ac, 0xC }).Deref<int>(gameProc);
                    inventoryItemsChargeStartPointer = (IntPtr)new DeepPointer(0x23782DC, new int[] { 0x1b0, 0xC }).Deref<int>(gameProc);
                    break;
            }
        }

        private void SetupStringPtrs()
        {
            IntPtr basePtr;
            switch (gameVersion)
            {
                case 1.05:
                    basePtr = (IntPtr)new DeepPointer(0x230B0F8, new int[] { 0x8, 0x0, 0x0 }).Deref<int>(gameProc);
                    itemGetStringPtrs = new IntPtr[GET_ITEM_STRING_AMNT];
                    //sys_dia03 ivory bug
                    itemGetStringPtrs[0] = (IntPtr)new DeepPointer(basePtr + 0xcc4, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia04 Bellflower
                    itemGetStringPtrs[1] = (IntPtr)new DeepPointer(basePtr + 0x21dc, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia05 Astral Charm
                    itemGetStringPtrs[2] = (IntPtr)new DeepPointer(basePtr + 0x256c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia06 Edea's Pearl
                    itemGetStringPtrs[3] = (IntPtr)new DeepPointer(basePtr + 0x28bc, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia07 Bakman Patch
                    itemGetStringPtrs[4] = (IntPtr)new DeepPointer(basePtr + 0x2c0c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia08 Tainted Missive
                    itemGetStringPtrs[5] = (IntPtr)new DeepPointer(basePtr + 0x4084, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia09 Magnet Stone
                    itemGetStringPtrs[6] = (IntPtr)new DeepPointer(basePtr + 0x4434, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia10 Vitality Fragment
                    itemGetStringPtrs[7] = (IntPtr)new DeepPointer(basePtr + 0xb1c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia12 Monastery Key
                    itemGetStringPtrs[8] = (IntPtr)new DeepPointer(basePtr + 0x27c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia18 Soft Tissue
                    itemGetStringPtrs[9] = (IntPtr)new DeepPointer(basePtr + 0x4a8c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia19 Garden Key
                    itemGetStringPtrs[10] = (IntPtr)new DeepPointer(basePtr + 0x4e3c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia21 Dirty Shroom
                    itemGetStringPtrs[11] = (IntPtr)new DeepPointer(basePtr + 0x11b0, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia24 Cat Sphere
                    itemGetStringPtrs[12] = (IntPtr)new DeepPointer(basePtr + 0x35cc, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia25 Torn Branch
                    itemGetStringPtrs[13] = (IntPtr)new DeepPointer(basePtr + 0x317c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia26 Sparse Thread
                    itemGetStringPtrs[14] = (IntPtr)new DeepPointer(basePtr + 0x3cac, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia28 Bellflower (NPC reward?)
                    itemGetStringPtrs[15] = (IntPtr)new DeepPointer(basePtr + 0x5494, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia29 Hazel Badge
                    itemGetStringPtrs[16] = (IntPtr)new DeepPointer(basePtr + 0x5020, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia30 Passiflora
                    itemGetStringPtrs[17] = (IntPtr)new DeepPointer(basePtr + 0x1f0c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia40 Cinder Key
                    itemGetStringPtrs[18] = (IntPtr)new DeepPointer(basePtr + 0x2934, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia41 Pocket Incensory
                    itemGetStringPtrs[19] = (IntPtr)new DeepPointer(basePtr + 0x2d84, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia44 Black Sachet
                    itemGetStringPtrs[20] = (IntPtr)new DeepPointer(basePtr + 0x9f8, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia47 Fresh Spring Leaf
                    itemGetStringPtrs[21] = (IntPtr)new DeepPointer(basePtr + 0x428, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia48 Sealed Wind
                    itemGetStringPtrs[22] = (IntPtr)new DeepPointer(basePtr + 0x68a0, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia49 Heavy Arrows
                    itemGetStringPtrs[23] = (IntPtr)new DeepPointer(basePtr + 0x6c10, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia50 Passiflora (ground?)
                    itemGetStringPtrs[24] = (IntPtr)new DeepPointer(basePtr + 0x2338, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia51 Bloodstained Tissue
                    itemGetStringPtrs[25] = (IntPtr)new DeepPointer(basePtr + 0x2788, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia52 airdash crest
                    itemGetStringPtrs[26] = (IntPtr)new DeepPointer(basePtr + 0x2a58, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia53 fast charge crest
                    itemGetStringPtrs[27] = (IntPtr)new DeepPointer(basePtr + 0x2eec, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia54 max charge crest
                    itemGetStringPtrs[28] = (IntPtr)new DeepPointer(basePtr + 0x3f4, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia55 warp crest
                    itemGetStringPtrs[29] = (IntPtr)new DeepPointer(basePtr + 0x740, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //eri03_dia07 Blessing Charm!
                    itemGetStringPtrs[30] = (IntPtr)new DeepPointer(basePtr + 0x5a60, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //impnpc_dia08 Rotten Bellflower
                    itemGetStringPtrs[31] = (IntPtr)new DeepPointer(basePtr + 0x160, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    break;
                case 1.07:
                    basePtr = (IntPtr)new DeepPointer(0x023782B8, new int[] { 0x8, 0x0, 0x0 }).Deref<int>(gameProc);
                    itemGetStringPtrs = new IntPtr[GET_ITEM_STRING_AMNT];
                    //sys_dia03 ivory bug
                    itemGetStringPtrs[0] = (IntPtr)new DeepPointer(basePtr + 0xcc4, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia04 Bellflower
                    itemGetStringPtrs[1] = (IntPtr)new DeepPointer(basePtr + 0x21dc, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia05 Astral Charm
                    itemGetStringPtrs[2] = (IntPtr)new DeepPointer(basePtr + 0x256c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia06 Edea's Pearl
                    itemGetStringPtrs[3] = (IntPtr)new DeepPointer(basePtr + 0x28bc, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia07 Bakman Patch
                    itemGetStringPtrs[4] = (IntPtr)new DeepPointer(basePtr + 0x2c0c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia08 Tainted Missive
                    itemGetStringPtrs[5] = (IntPtr)new DeepPointer(basePtr + 0x4084, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia09 Magnet Stone
                    itemGetStringPtrs[6] = (IntPtr)new DeepPointer(basePtr + 0x4434, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia10 Vitality Fragment
                    itemGetStringPtrs[7] = (IntPtr)new DeepPointer(basePtr + 0xb1c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia12 Monastery Key
                    itemGetStringPtrs[8] = (IntPtr)new DeepPointer(basePtr + 0x27c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia18 Soft Tissue
                    itemGetStringPtrs[9] = (IntPtr)new DeepPointer(basePtr + 0x4a8c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia19 Garden Key
                    itemGetStringPtrs[10] = (IntPtr)new DeepPointer(basePtr + 0x4e3c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia21 Dirty Shroom
                    itemGetStringPtrs[11] = (IntPtr)new DeepPointer(basePtr + 0x11b0, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia24 Cat Sphere
                    itemGetStringPtrs[12] = (IntPtr)new DeepPointer(basePtr + 0x35cc, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia25 Torn Branch
                    itemGetStringPtrs[13] = (IntPtr)new DeepPointer(basePtr + 0x317c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia26 Sparse Thread
                    itemGetStringPtrs[14] = (IntPtr)new DeepPointer(basePtr + 0x3cac, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia28 Bellflower (NPC reward?)
                    itemGetStringPtrs[15] = (IntPtr)new DeepPointer(basePtr + 0x5494, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia29 Hazel Badge
                    itemGetStringPtrs[16] = (IntPtr)new DeepPointer(basePtr + 0x5020, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia30 Passiflora
                    itemGetStringPtrs[17] = (IntPtr)new DeepPointer(basePtr + 0x1f0c, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia40 Cinder Key
                    itemGetStringPtrs[18] = (IntPtr)new DeepPointer(basePtr + 0x2934, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia41 Pocket Incensory
                    itemGetStringPtrs[19] = (IntPtr)new DeepPointer(basePtr + 0x2d84, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia44 Black Sachet
                    itemGetStringPtrs[20] = (IntPtr)new DeepPointer(basePtr + 0x9f8, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia47 Fresh Spring Leaf
                    itemGetStringPtrs[21] = (IntPtr)new DeepPointer(basePtr + 0x428, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia48 Sealed Wind
                    itemGetStringPtrs[22] = (IntPtr)new DeepPointer(basePtr + 0x68a0, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia49 Heavy Arrows
                    itemGetStringPtrs[23] = (IntPtr)new DeepPointer(basePtr + 0x6c10, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia50 Passiflora (ground?)
                    itemGetStringPtrs[24] = (IntPtr)new DeepPointer(basePtr + 0x2338, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia51 Bloodstained Tissue
                    itemGetStringPtrs[25] = (IntPtr)new DeepPointer(basePtr + 0x2788, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia52 airdash crest
                    itemGetStringPtrs[26] = (IntPtr)new DeepPointer(basePtr + 0x2a58, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia53 fast charge crest
                    itemGetStringPtrs[27] = (IntPtr)new DeepPointer(basePtr + 0x2eec, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia54 max charge crest
                    itemGetStringPtrs[28] = (IntPtr)new DeepPointer(basePtr + 0x3f4, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //sys_dia55 warp crest
                    itemGetStringPtrs[29] = (IntPtr)new DeepPointer(basePtr + 0x740, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //eri03_dia07 Blessing Charm!
                    itemGetStringPtrs[30] = (IntPtr)new DeepPointer(basePtr + 0x5a60, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    //impnpc_dia08 Rotten Bellflower
                    itemGetStringPtrs[31] = (IntPtr)new DeepPointer(basePtr + 0x160, new int[] { 0xC, 0x10, 0x0 }).Deref<int>(gameProc);
                    break;
            }
        }

        private bool VerifyProcessRunning()
        {
            if (gameProc != null && !gameProc.HasExited)
            {
                return true;
            }
            Process[] game = Process.GetProcessesByName("MomodoraRUtM");
            if (game.Length > 0)
            {
                gameProc = game[0];
                return true;
            }
            return false;
        }

        /*
         * Kill a player if requested (by deathlink or similar)
         * cause = the reason the player was killed
         */
        private void KillPlayer(string cause) {
            if (archipelago.ShouldDeathLink(gameProc.ReadValue<int>(levelIDPointer))) {
                if (!shopping) {
                    SendMessage($"Killed by {cause}.");
                    archipelago.IgnoreNextDeath = true;
                    gameProc.WriteValue<double>(currentHealthPointer, 0);
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
        private MemoryWatcher GetFinalBossKillWatcher() {
            // Setup the memory watcher to check the game's memory every 10 milliseconds 
            MemoryWatcher<double> watcher = new MemoryWatcher<double>(cutsceneStatePointer) {
                UpdateInterval = new TimeSpan(0, 0, 0, 0, 10)
            };

            // Add the event to trigger when the cutscene flag changes
            watcher.OnChanged += (old, current) =>
            {
                // Cutscene State 1000 is triggered on boss death. We check that + room id to ensure the right state.
                int levelID = gameProc.ReadValue<int>(levelIDPointer);
                if (current == 1000 && levelID == FINAL_BOSS_LEVEL_ID)
                    archipelago.CheckVictoryConditionOnBossKill(hasFoundGreenLeaf);
            };

            watcher.Enabled = true;
            return watcher;
        }

        /*
         * Meow :3
         */
        private MemoryWatcher GetMeowWatcher() {
            // Setup the memory watcher to check the game's memory every 10 milliseconds 
            MemoryWatcher<double> watcher = new MemoryWatcher<double>(playerFormPointer) {
                UpdateInterval = new TimeSpan(0, 0, 0, 0, 10)
            };

            // Trigger when the player's form changes
            watcher.OnChanged += (old, current) =>
            {
                archipelago.Say("Meow :3");
                watcher.Enabled = false;
            };

            watcher.Enabled = true;
            return watcher;
        }
 
        /*
         * Setup the memory watcher to kill the player if the spider merchant dies. (anti-softlock)
         */
        private MemoryWatcher GetSpiderWatcher() {
            MemoryWatcher<double> watcher = new MemoryWatcher<double>(spiderMerchantPointer) {
                UpdateInterval = new TimeSpan(0, 0, 0, 0, 10)
            };

            // Kill the player to prevent a potential softlock
            watcher.OnChanged += (old, current) =>
            {
                SendMessage($"Spider: ({old} -> {current})."); // DEBUG
                //SendMessage("Killed by Spider Merchant.");
                //gameProc.WriteValue<double>(currentHealthPointer, 0);
            };

            watcher.Enabled = true;
            return watcher;
        }
    }
}
