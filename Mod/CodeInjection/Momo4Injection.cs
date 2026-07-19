using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyHook;
using static EasyHook.RemoteHooking;

namespace Momo4Injection
{

    /*
     * Class which will be injected into game code
     */
    public class Momo4Injection : IEntryPoint
    {
        
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        private readonly Momo4Ipc server; // Interface to the server running in the harness process
        private EventLoopDelegate eventLoop; // Reference to the event loop method

        public Momo4Injection(IContext context, string channelName)
        {
            try
            {
                server = IpcConnectClient<Momo4Ipc>(channelName);
                server.Ping();
            }
            catch (Exception e)
            {
                Momo4Ipc.LogToDebugFile(e.Message);
                Momo4Ipc.LogToDebugFile(e.StackTrace);
            }
        }
        
        public void Run(IContext context, string channelName)
        {
            try
            {
                // Set the player's munny
                //SetMunny(1337); // Will crash the hook as currently written

                // Setup the event loop hook
                HookEventLoop();
            }
            catch (Exception e)
            {
                Momo4Ipc.LogToDebugFile(e.Message);
                Momo4Ipc.LogToDebugFile(e.StackTrace);
                server.LogMessage(e.Message);
            }
            server.LogMessage("Injection Complete!");
        }

        public void SetMunny(double amount)
        {
            // Recreate the following pointer:
            // IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x550);
            
            // DOES NOT WORK, WILL SEGFAULT AND SILENTLY CRASH THE INJECTION THREAD

            /*
            unsafe
            {
                // Convert base pointer to a byte address
                byte* deepPointer = (byte*) Process.GetCurrentProcess().MainModule.BaseAddress.ToPointer();
                
                // Derefence the pointer for each of the offsets
                deepPointer = *(byte**)(deepPointer + 0x0);
                deepPointer = *(byte**)(deepPointer + 0x4);

                // Dereference the int value
                int deref = *(int*)deepPointer;

                // Now use IntPtr.Add, as in the original
                var ptr = IntPtr.Add((IntPtr)deref, 0x550);

                // Finally, convert it to a double and write to it
                *(double*)ptr.ToPointer() = amount;
            }
            */

            // Use IntPtrs and offests to reconstruct the original pointer chain. 
            var basePtr = Process.GetCurrentProcess().MainModule.BaseAddress;
            var ptr = IntPtr.Add(basePtr, 0x230C440);
            ptr = Marshal.ReadIntPtr(IntPtr.Add(ptr, 0x0));
            ptr = Marshal.ReadIntPtr(IntPtr.Add(ptr, 0x04));
            var munnyPtr = new IntPtr(Marshal.ReadInt32(ptr) + 0x550);

            uint oldProtect;
            const uint PAGE_READWRITE = 0x04;

            // Write the double to memory (ensure memory access)
            byte[] bytes = BitConverter.GetBytes(amount);
            VirtualProtect(munnyPtr, 8, PAGE_READWRITE, out oldProtect); // idk im tryin anything
            Marshal.Copy(bytes, 0, munnyPtr, bytes.Length);
            VirtualProtect(munnyPtr, 8, oldProtect, out _);
        }

        // Search for the address of a specific string of bytes in the game's memory
        public IntPtr FindAddressOfBytes(string bytes, string mask)
        {
            // Mask must be at least the size of the bytestring (ideally the same, but extra is just ignored)
            if (mask.Length < bytes.Length)
            {
                server.LogMessage($"Error: search target and mask not the same length: {bytes.Length} < {mask.Length}");
                return IntPtr.Zero;
            }

            var mainModule = Process.GetCurrentProcess().MainModule;
            int searchSize = mainModule.ModuleMemorySize - bytes.Length;
            unsafe
            {
                byte* gameMemory = (byte*) mainModule.BaseAddress.ToPointer();
                for (int i = 0; i < searchSize; ++i)
                    if (CompareWithMask(gameMemory+i, bytes, mask))
                        return new IntPtr(gameMemory+i);
            }
            return IntPtr.Zero;
        }

        // Compare a region of memory to the value of a target string, for values within the mask
        private unsafe bool CompareWithMask(byte* start, string target, string mask)
        {
            for (int i = 0; i < target.Length; ++i)
                if (mask[i] == 'x' && target[i] != *start)
                    return false;
            return true;
        } 

        // Create our event loop hook
        public void HookEventLoop()
        {
            // Borrow the event loop lookup from a different game and hope it works lol
            // It does not work lol
            string target = "\x83\xEC\x0C\x00\x00\x00\x00\x00\x00\x00\x53\x57\x0F\x84\x00\x00\x00\x00\x56\x8B\x74\x24\x24\x00\x00\x00\x00\x00\x00\x00\x8B\x46\x08\x85\xC0";
            string mask = "xxx???????xxxx????xxxxx???????xxxxx";

            // Find the original method
            var addr = FindAddressOfBytes(target, mask);
            if (addr == IntPtr.Zero)
            {
                server.LogMessage("Could not find event loop's pattern in the game memory");
                return;
            }
            eventLoop = Marshal.GetDelegateForFunctionPointer<EventLoopDelegate>(addr);

            unsafe
            {
                var hook = LocalHook.Create(addr, new EventLoopDelegate(EventLoopHook), null);
                hook.ThreadACL.SetInclusiveACL(new int[] { 0 });
            }
        }
    
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate bool EventLoopDelegate(void* pvArg1, void* pvArg2, void* pvArg3, void* pvArg4, void* pvArg5);

        // Hook to overwrite the original event loop
        private int timesLogged = 0;
        private unsafe bool EventLoopHook(void* pvArg1, void* pvArg2, void* pvArg3, void* pvArg4, void* pvArg5)
        {
            // Extract the script name from the event object
            // 0x005C - 0x005F
	        byte* m_pszScriptName = (byte*)pvArg3 + 0x5C;
            string scriptName = Marshal.PtrToStringAnsi(new IntPtr(m_pszScriptName));

            // Do stuff we care about here
            if (timesLogged++ < 100)
            {
                server.LogMessage($"Called Event Loop: {scriptName}");   
            }

            // Then call the original
            return eventLoop(pvArg1, pvArg2, pvArg3, pvArg4, pvArg5);
        }
    }
}