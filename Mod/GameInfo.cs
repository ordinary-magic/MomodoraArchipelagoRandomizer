using System.Collections.Generic;

namespace MomodoraArchipelago
{
    // All of the items in the game, using their internal numbering
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
        ShopPlaceholder1 = 19,
        ShopPlaceholder2 = 20,
        RingOfCandor = 21,
        SmallCoin = 22,
        BackmanPatch = 23,
        CatSphere = 24,
        HazelBadge = 25,
        TornBranch = 26,
        MonasteryKey = 27,
        ShopPlaceholder3 = 29,
        ShopPlaceholder4 = 30,
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
        
        // The following are "items" which do not exist in the base game
        Munny1 = 56,
        Munny10 = 57,
        Munny50 = 58,
        Munny100 = 59,
        Munny250 = 60,
        Munny500 = 61,
        Munny999 = 62,
        IvorySwarm = 63,
        VitalityChunk = 64,

        // Randomizer flags
        ArchipelagoItem = 254,
        None = 255 // Placeholder item type
    };
    
    // All of the shops in the game, using internal numbering
    public enum Shops : int
    {
        KarstCity = 0,
        ForlornMonsatery = 1,
        SubterraneanGrave = 2,
        WhiteleafMemorialPark = 3,
        CinderChambers_1 = 4,
        CinderChambers_2 = 5,
        RoyalPinacotheca = 6,
    }

    public static class GameInfo
    {
        //Mapping of item source to what pointer points to the string that gets displayed when you pick it up.
        //-1 means that it does not have a string attached (shop source)
        public static readonly int[] sourceToStringMapping = {1, 2, 1, 6, 10, 18, 8, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 20, 22, 1, 24, 11, 12, 1, 9, 21, 30, 31, 3, 4, 14, 19, 13, 5, 25, 23, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 27, 26, 28, 29, 15, 16, 17};
        public static readonly int RANDOMIZER_SOURCE_AMOUNT = sourceToStringMapping.Length;
        public const int GET_ITEM_STRING_AMNT = 32;

        public const int BOSS_LOCATION_START = 31;

        // Mapping from game id of a "Location" to the item that resides there.
        public static readonly Dictionary<int, int> sourceIdMapping = new Dictionary<int, int>()
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

        // Mapping from an item's source location id to what level (screen) id its found on.
        public static readonly Dictionary<int, int[]> sourceToLevelMapping = new Dictionary<int, int[]>()
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

        // List of items which have charges
        public readonly static List<int> chargeItems = new List<int>() {
            (int)Items.Bellflower, (int)Items.Passiflora, (int)Items.TaintedMissive, (int)Items.IvoryBug
        };

        // The level ID of the final boss
        public const int FINAL_BOSS_LEVEL_ID = 232;

        // A Mapping from level id to the nam eof the boss which is found there (TODO)
        public static readonly Dictionary<int, string> bossNameDictionary = new  Dictionary<int, string>() {
            {53, "Antrhopod Demon Edea"},
            {73, "Lubella, the Witch of Decay"},
            {141, "Derelict Frida"},
            {193, "The Heretical Arsonist"},
            {153, "Lubella, the Witch of Decay"},
            {104, "Pardoner Fennel"},
            {97, "Archpriestess Choir"},
            {212, "Duchess Lupiar & Royal Huntress Magnolia"},
            {FINAL_BOSS_LEVEL_ID, "The Accurst Queen of Karst"},
        };

        // List of unused items to give as shop placeholder items 
        public static readonly List<int> shopPlaceholderItems = new List<int>() {
            (int)Items.ShopPlaceholder1, (int)Items.ShopPlaceholder2, (int)Items.ShopPlaceholder3, (int)Items.ShopPlaceholder4
        };

        // How Much the health amount should change per the game's difficulty
        public static double GetHealthChangePerDifficulty(double version, int difficulty)
        {
            if (version == 1.05)
            {
                return new double[] {0, 2, 1, 1}[difficulty - 1];
            }
            else
            {
                return new double[] {2, 2, 1, 1}[difficulty - 1];
            }
        }

        // List of items which are keys that open doors
        public static readonly List<int> doorKeys = new List<int> {
            (int) Items.GardenKey, (int)Items.CinderKey, (int)Items.MonasteryKey
        };
        
        // List of rooms which with locked doors that correspond to the keys
        public static readonly List<List<int>> lockedDoorLocations = new List<List<int>> () {
            new List<int> {145, 84},
            new List<int> {183},
            new List<int> {99}
        };

        // Level ID's which contain shops(Same ordering used everywhere)
        // Karst City, Forlorn Monsatery, Subterranean Grave, Whiteleaf Memorial Park, Cinder Chambers 1, Cinder Chambers 2, Royal Pinacotheca
        public static readonly List<int> shopLocations = new List<int> {
            63, 111, 127, 160, 181, 187, 205
        };

        // List of the items originally in each of the shops 
        public static readonly List<List<int>> originalShopItems = new List<List<int>> {
            new List<int> {8, 9, 10},
            new List<int> {9},
            new List<int> {11, 12},
            new List<int> {13},
            new List<int> {14, 10},
            new List<int> {15, 10, 16},
            new List<int> {17, 18, 19}
        };

        // Get the shop offsets
        public static List<List<int>> GetShopOffsets(double version)
        {
            if (version == 1.05)
                return new List<List<int>>
                {
                    new List<int> { 207, 203, 213 },
                    new List<int> { 203 },
                    new List<int> { 221, 199 },
                    new List<int> { 5 },
                    new List<int> { 8, 213 },
                    new List<int> { 235, 213, 205 },
                    new List<int> { 224, 234, 11 }
                };

            else 
                return new List<List<int>>
                {
                    new List<int> { 208, 204, 214 },
                    new List<int> { 204 },
                    new List<int> { 222, 200 },
                    new List<int> { 5 },
                    new List<int> { 8, 214 },
                    new List<int> { 236, 214, 206 },
                    new List<int> { 225, 235, 11 }
                };
        }

        public const int SHOP_SIZE = 12;

        // Names and Descriptions of the placeholder shop items
        public static readonly List<string> placeholderShopNames = new List<string> {
            "Placeholder", "Placeholder", "Placeholder", "Sold Out",
        };

        public static readonly List<string> placeholderShopDescription = new List<string> {
            "Placeholder #1", "Placeholder #2", "Placeholder #3", "Sorry!",
        };

        // Mapping of custom item ids to their shop descriptions
        public static readonly Dictionary<int, string> customItemShopDescriptions = new Dictionary<int, string> {
            { (int) Items.IvorySwarm, "5 Ivory Bugs" },
            { (int) Items.VitalityFragment, "+ Max HP" }, // Does not have a description in the game, so we need to make one.
            { (int) Items.VitalityChunk, "++ Max HP" },
        };

        // Ordered amount of munny to drop for each munny item.
        public static readonly List<int> MunnyAmounts = 
            new List<int> { 1, 10, 50, 100, 250, 500, 999 };
    }
}