using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiveSplit.ComponentUtil;

namespace MomodoraArchipelago
{
    /**
     * Class to track the game's proccess and memory state
     */
    public class GameMonitor
    {
        public readonly Process gameProc;
        public double Version;
        
        // Pointers //

        public IntPtr[] itemGetStringPtrs;

        public IntPtr[] potentialSourcesPointers;

        // charge item pointers //
        public IntPtr bellflowerSaveValuePointer;
        public IntPtr bellflowerMaxValuePointer;
        public IntPtr taintedMissiveSaveValuePointer;
        public IntPtr taintedMissiveMaxValuePointer;
        public IntPtr passifloraSaveValuePointer;
        public IntPtr passifloraMaxValuePointer;

        // ivory bug pointers //
        public IntPtr ivoryBugCountPointer;

        // vitality fragment pointers //
        public IntPtr vitalityFragmentCountPointer;
        public IntPtr maxHealthPointer;
        public IntPtr currentHealthPointer;

        // crest fragment pointers //
        public IntPtr crestFragmentCountPointer;

        // inventory pointers //
        public IntPtr activeItemsPointer;
        public IntPtr passiveItemsPointer;
        public IntPtr keyItemsPointer;
        public IntPtr totalItemsPointer;
        public IntPtr inventoryItemsStartPointer;
        public IntPtr inventoryItemsChargeStartPointer;

        // Shop Pointers //
        public IntPtr munnyPointer;
        public IntPtr invOpenPointer;
        public IntPtr convOpenPointer;
        public IntPtr shopPointer;
        public IntPtr itemInfoPointer;

        // Misc Pointers //
        public IntPtr difficultyPointer;
        public IntPtr levelIDPointer;
        public IntPtr inGamePointer;
        public IntPtr saveAmountPointer;
        public IntPtr deathAmountPointer;
        public IntPtr warpStartPointer;
        public IntPtr playerFormPointer;
        public IntPtr playerXPointer;
        public IntPtr playerYPointer;
        public IntPtr lubella2Pointer;
        public IntPtr cutsceneStatePointer;
        public IntPtr spiderMerchantPointer;

        // Charge Item Memory Watchers //
        public MemoryWatcher<double> taintedMissiveWatcher;
        public MemoryWatcher<double> bellflowerWatcher;
        public MemoryWatcher<double> passifloraWatcher;
        public MemoryWatcher<double> ivoryBugWatcher;
        public MemoryWatcher<double> crestFragmentWatcher;
        public MemoryWatcher<double> vitalityFragmentWatcher;

        // Special Memory Watchers //
        public MemoryWatcher<int> levelIDWatcher;
        public MemoryWatcher<double> deathWatcher;
        public MemoryWatcher<double> saveWatcher;
        public MemoryWatcher<double> inGameWatcher;
        public MemoryWatcher<double> convOpenWatcher;
        public MemoryWatcher<double> playerYWatcher;
        public MemoryWatcher<double> currentHealthWatcher;
        
        private MemoryWatcherList randoSourceWatchers = new MemoryWatcherList();
        private MemoryWatcherList specialWatchers = new MemoryWatcherList();
        private MemoryWatcherList chargeItems = new MemoryWatcherList();

        public GameMonitor(Process gameProc)
        {
            this.gameProc = gameProc;

            SetupIntPtrs(); // game version is set here, so must go first
            SetupItemPtrs();
            SetupStringPtrs();

            CreateDefaultSpecialWatchers();
            CreateChargeItemWatchers();
        }

        private void SetupIntPtrs()
        {
            switch (gameProc.MainModule.ModuleMemorySize)
            {
                case 39690240:
                    //version 1.05b
                    Debug.WriteLine("Version 1.05b detected");
                    Debug.WriteLine("Setting up pointers");
                    Version = 1.05;
                    #region setting up IntPtrs
                    potentialSourcesPointers = new IntPtr[GameInfo.RANDOMIZER_SOURCE_AMOUNT];
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
                    spiderMerchantPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x4, 0x4, 0x60, 0x4, 0x4}).Deref<int>(gameProc), 0xD0);
                    
                    #endregion
                    break;
                case 40222720:
                    //version 1.07
                    Debug.WriteLine("Version 1.07 detected");
                    Debug.WriteLine("Setting up pointers");
                    Version = 1.07;
                    #region setting up IntPtrs
                    potentialSourcesPointers = new IntPtr[GameInfo.RANDOMIZER_SOURCE_AMOUNT];
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
                    spiderMerchantPointer = IntPtr.Add((IntPtr)new DeepPointer(0x2379600, new int[] { 0x4, 0x4, 0x60, 0x4, 0x4}).Deref<int>(gameProc), 0xD0);
                    
                    #endregion
                    break;

                default:
                    throw new InvalidOperationException("Unsupported game version for randomizer");
            }
        }

        private void SetupStringPtrs()
        {
            IntPtr basePtr;
            switch (Version)
            {
                case 1.05:
                    basePtr = (IntPtr)new DeepPointer(0x230B0F8, new int[] { 0x8, 0x0, 0x0 }).Deref<int>(gameProc);
                    itemGetStringPtrs = new IntPtr[GameInfo.GET_ITEM_STRING_AMNT];
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
                    itemGetStringPtrs = new IntPtr[GameInfo.GET_ITEM_STRING_AMNT];
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

        private void SetupItemPtrs()
        {
            switch (Version)
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

        public bool VerifyProcessRunning()
        {
            return gameProc != null && !gameProc.HasExited;
        }

        public static GameMonitor TryConnectToGame()
        {
            Process[] game = Process.GetProcessesByName("MomodoraRUtM");
            if (game.Length > 0)
            {
                return new GameMonitor(game[0]);
            }
            return null;
        }

        public int Difficulty => (int) gameProc.ReadValue<double>(difficultyPointer);

        /**
         * Create a memory watcher using the provided settings
         */
        public MemoryWatcher<T> InitializeMemoryWatcher<T>(IntPtr address, bool enabled=true, TimeSpan? interval=null, Action<T, T> onChanged=null) where T: struct
        {
            var watcher = new MemoryWatcher<T>(address)
            {
                Enabled = enabled,
                UpdateInterval = interval,
            };

            if (onChanged != null)
                watcher.OnChanged += (old, current) => onChanged.Invoke(old, current);

            return watcher;
        }

        public void CreateChargeItemWatchers()
        {
            taintedMissiveWatcher = InitializeMemoryWatcher<double>(taintedMissiveMaxValuePointer);
            bellflowerWatcher = InitializeMemoryWatcher<double>(bellflowerMaxValuePointer);
            passifloraWatcher = InitializeMemoryWatcher<double>(passifloraMaxValuePointer);
            ivoryBugWatcher = InitializeMemoryWatcher<double>(ivoryBugCountPointer);
            crestFragmentWatcher =InitializeMemoryWatcher<double>(crestFragmentCountPointer);
            vitalityFragmentWatcher = InitializeMemoryWatcher<double>(vitalityFragmentCountPointer);

            // Add them all to the list
            chargeItems.AddRange(new List<MemoryWatcher<double>> {
                taintedMissiveWatcher, bellflowerWatcher, passifloraWatcher, ivoryBugWatcher, crestFragmentWatcher, vitalityFragmentWatcher
            });

            UpdateChargeItemWatchers(); // Set the initial values the list
        }
        
        /**
         * Update all the charge item memory watchers to see if any of the values changed since the last check
         */
        public void UpdateChargeItemWatchers() => chargeItems.UpdateAll(gameProc);

        /**
         * Create and register a watcher for a randomizer source location
         */
        public MemoryWatcher<double> CreateRandomizerSourceWatcher(IntPtr addr, Action<double, double> onChanged = null)
        {
            var watcher = InitializeMemoryWatcher(addr, true, new TimeSpan(0, 0, 0, 0, 10), onChanged);
            randoSourceWatchers.Add(watcher);
            return watcher;
        }

        /**
         * Update all the randomizer source memory watchers to see if any of the values changed since the last check
         */
        public void UpdateRandomizerSourceWatchers() => randoSourceWatchers.UpdateAll(gameProc);

        /**
         * Create a special memory watcher and register it with the monitor.
         */
        public MemoryWatcher<T> CreateSpecialMemoryWatcher<T>(IntPtr addr, Action<T, T> onChanged = null) where T: struct
        {
            var watcher = InitializeMemoryWatcher(addr, true, new TimeSpan(0, 0, 0, 0, 10), onChanged);
            specialWatchers.Add(watcher);
            return watcher;
        }

        private void CreateDefaultSpecialWatchers()
        {
            levelIDWatcher = CreateSpecialMemoryWatcher<int>(levelIDPointer);
            inGameWatcher = CreateSpecialMemoryWatcher<double>(inGamePointer);
            deathWatcher = CreateSpecialMemoryWatcher<double>(deathAmountPointer);
            saveWatcher = CreateSpecialMemoryWatcher<double>(saveAmountPointer);
            convOpenWatcher = CreateSpecialMemoryWatcher<double>(convOpenPointer);
            playerYWatcher = CreateSpecialMemoryWatcher<double>(playerYPointer);
            currentHealthWatcher = CreateSpecialMemoryWatcher<double>(currentHealthPointer);
            UpdateSpecialWatchers();
        }

        /**
         * Update all the misc special memory watchers to see if any of the values changed since the last check
         */
        public void UpdateSpecialWatchers() => specialWatchers.UpdateAll(gameProc);

        public T ReadValue<T>(IntPtr addr) where T: struct => gameProc.ReadValue<T>(addr);

        public void WriteValue<T>(IntPtr addr, T value) where T: struct => gameProc.WriteValue(addr, value);

        public void WriteBytes(IntPtr addr, byte[] bytes) => gameProc.WriteBytes(addr, bytes);

        public bool InGame => Convert.ToBoolean(gameProc.ReadValue<double>(inGamePointer));
    }
}