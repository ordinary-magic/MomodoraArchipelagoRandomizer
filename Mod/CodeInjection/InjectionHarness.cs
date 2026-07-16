using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using EasyHook;
using Momo4Injection;

namespace MomodoraArchipelago
{
    /*
     * Class which will communicate with the injection
     */
    public class InjectionHarness
    {
        public static IpcServerChannel ipcServer;

        public static void CreateInjection(Process gameProc)
        {
            try
            {
                // Server does not have a reference to the dll, so we need to give it one
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    if (args.Name.Contains("Momo4Injection"))
                        return typeof(Momo4Ipc).Assembly;
                    return null;
                };

                // Create the IPC server, and register this class as the singleton which manages it.
                string channelName = null;
                ipcServer = RemoteHooking.IpcCreateServer<Momo4Ipc>(ref channelName, WellKnownObjectMode.Singleton);

                // Setup the dependency paths
                Config.DependencyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Config.HelperLibraryLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // Get the full path to the assembly we want to inject into the target process
                string injection = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Momo4Injection.dll");

                // Inject it
                RemoteHooking.Inject(gameProc.Id, injection, injection, channelName);
            }            
            catch (Exception e)
            {
                // Debug logging
                var output = new StreamWriter("Momo4IPCFailure.log", true);
                output.WriteLine(e.Message);
                output.WriteLine(e.StackTrace);
                output.Close();
            }
        }
    }
}