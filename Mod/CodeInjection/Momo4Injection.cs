using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyHook;
using LiveSplit.ComponentUtil;
using static EasyHook.RemoteHooking;

namespace Momo4Injection
{
    /*
     * Class which will be injected into game code
     */
    public class Momo4Injection : IEntryPoint
    {
        // Interface to the server running in the livesplit process
        private readonly Momo4Ipc server;
        private readonly Process gameProc;
        private readonly IntPtr baseAddress;

        public Momo4Injection(IContext context, string channelName)
        {
            try
            {
                server = IpcConnectClient<Momo4Ipc>(channelName);
                gameProc = Process.GetCurrentProcess();
                baseAddress = gameProc.MainModule.BaseAddress;
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
                SetMunny(1337);

                // Call the test function
                //CallTestFunction();
            }
            catch (Exception e)
            {
                Momo4Ipc.LogToDebugFile(e.Message);
                server.LogMessage(e.Message);
            }
            Momo4Ipc.LogToDebugFile("Complete!");
            server.LogMessage("Injection Complete!");
        }

        public void SetMunny(int amount)
        {
            // Todo: rewrite this witout livesplit, (if we keep doing it), since its just dereferencing a pointer
            //  Note: currently, to make this resolve, we have to copy Livesplit.Core into the livesplit components, which is fucked up
            //  Further, it doesnt even work.
            var munnyAddr = IntPtr.Add((IntPtr)new DeepPointer(0x230C440, new int[] { 0x0, 0x4 }).Deref<int>(gameProc), 0x550);
            gameProc.WriteValue<double>(munnyAddr, amount);
        }

        public void CallTestFunction()
        {
            IntPtr funcAddress = IntPtr.Add(baseAddress, 0xABCDEF);

            // Create a callable delegate from the address.
            var gameFunc = Marshal.GetDelegateForFunctionPointer<SomeGameFunction>(funcAddress);

            // Call the function
            int result = gameFunc(42, 3.14f);

            // Report the result?
            server.LogMessage($"Game function returned: {result}");
        }

        // Specify the signature of the function we want to call
        // Todo: figure out calling convention (typically Cdecl or StdCall?).
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SomeGameFunction(int param1, float param2);
    }
}