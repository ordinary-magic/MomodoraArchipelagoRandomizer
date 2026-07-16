using System;
using System.IO;

namespace Momo4Injection
{
    /*
     * Class which will definine the communication protocol between server and client
     */
    public class Momo4Ipc: MarshalByRefObject
    {
        // Log a message
        public void LogMessage(string message) => LogToDebugFile(message);

        public static void LogToDebugFile(string message)
        {
            // File will be in either the livesplit or game folder, depending on who makes it
            // Most of the time itll be in livesplit, unless the client explicitly calls logtodebugfile
            var output = new StreamWriter("Momo4IPCTestFile.log", true);
            output.WriteLine(message);
            output.Close();
        }

        // A Ping between the two 
        public void Ping() {}
    }
}