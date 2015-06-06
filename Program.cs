using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace GTAHash
{
    class Program
    {
        static ConsoleColor goodColor = ConsoleColor.DarkGreen;
        static ConsoleColor badColor = ConsoleColor.DarkRed;
        static ConsoleColor unknownColor = ConsoleColor.DarkCyan;
        static int goodCount = 0, badCount = 0, unknownCount = 0;
        static List<string> fileList = new List<string>(), fileHashList = new List<string>();
        static List<string> hashQueue = new List<string>(), reportQueue = new List<string>(), writeQueue = new List<string>();
        static Stopwatch stopwatch = new Stopwatch();
        static WebClient client = new WebClient();
        static bool reporting = false, writing = false;
        static string hashFile = "hashes";
        static string action = "verify";

        static void Main(string[] args)
        {
            if (args.Length > 0)
                if (args[0] == "write")
                {
                    action = "write";
                    if (File.Exists(hashFile))
                        File.Delete(hashFile);
                }

            // Show header
            showHeader();

            // Create threads
            Thread reporterThread = new Thread(new ThreadStart(reporter));
            Thread writerThread = new Thread(new ThreadStart(writer));
            Thread hashWorker1Thread = new Thread(new ThreadStart(hashWorker1));
            Thread hashWorker2Thread = new Thread(new ThreadStart(hashWorker2));
            Thread hashWorker3Thread = new Thread(new ThreadStart(hashWorker3));
            Thread hashWorker4Thread = new Thread(new ThreadStart(hashWorker4));

            // Get file lists
            fileList = Directory.EnumerateFiles(".", "*", SearchOption.AllDirectories).ToList();
            fileHashList = client.DownloadString("https://github.com/Scarsz/GTAHash/raw/master/hashes").Split(new string[] { "\n", " " }, StringSplitOptions.None).ToList();

            // Clean file list
            // There has to be .ToList() on foreach loops where the list that's being looped is being modified inside of the foreach
            // .ToList() creates an instance of the list for foreach to loop on, unaffected by the modifications it's doing to the fileList list
            foreach (string file in fileList.ToList())
            {
                if (file == ".\\" + AppDomain.CurrentDomain.FriendlyName)
                    fileList.Remove(fileList[fileList.IndexOf(file)]);
                if (file.Split('\\')[1] == "_CommonRedist")
                    fileList.Remove(fileList[fileList.IndexOf(file)]);
                if (file.Split('\\')[1] == "Installers")
                    fileList.Remove(fileList[fileList.IndexOf(file)]);
                if (file.Split('\\')[1] == "hashes")
                    fileList.Remove(fileList[fileList.IndexOf(file)]);
            }

            // Loop through each file to add it to the queue
            ConsoleColor oldColor = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            foreach (string file in fileList)
            {
                Console.WriteLine("Hash queue: Added file \"" + file + "\"");
                hashQueue.Add(file);
            }
            Console.BackgroundColor = oldColor;

            // Add another line after the assigning messages to look nicer
            Console.WriteLine();

            // Start stopwatch to time how long it takes to calculate all of the hashes
            stopwatch.Start();

            // Start threads
            reporterThread.Start();
            writerThread.Start();
            hashWorker1Thread.Start();
            hashWorker2Thread.Start();
            hashWorker3Thread.Start();
            hashWorker4Thread.Start();

            // Wait until all threads have did their work before continuing
            while (hashWorker1Thread.IsAlive || hashWorker2Thread.IsAlive || hashWorker3Thread.IsAlive || hashWorker4Thread.IsAlive || reporting || writing) { Thread.Sleep(100); }

            // Abort the reporter thread because it won't receive any more reports
            reporterThread.Abort();
            // Abort the writer thread because it won't receive any more text
            writerThread.Abort();

            // Stop the stopwatch
            stopwatch.Stop();

            // Display information
            Console.WriteLine("\nCompleted in " + stopwatch.ElapsedMilliseconds / 1000 + " seconds");
            if (action == "verify")
                Console.WriteLine("Good files: {0}\nBad files: {1}\nUnknown files: {2}", goodCount, badCount, unknownCount);
            Console.ReadKey();
        }

        static bool processWork(int threadId)
        {
            if (hashQueue.Count == 0)
                return false;

            while (hashQueue.Count > 0)
            {
                // Get the next available file to hash
                string file = hashQueue[0];

                // Remove file from list so that another thread doesn't snag it
                hashQueue.RemoveAt(0);

                // Calculate hash of file
                string fileHash = getHash(file);

                // Do stuff with the file hash
                if (action == "verify")
                {
                    // Get index of file & hash in downloaded hash list
                    int fileIndex = Array.IndexOf(fileHashList.ToArray(), file);
                    int hashIndex = fileIndex + 1;

                    // If fileIndex == -1 the file is either custom or the downloaded file list is outdated
                    if (fileIndex == -1)
                        reportQueue.Add(file + " [Thread " + threadId + "]" + "|" + "unknown");
                    else
                        if (fileHash == fileHashList[hashIndex])
                            reportQueue.Add(file + " [Thread " + threadId + "]" + "|" + "good");
                        else
                            reportQueue.Add(file + " [Thread " + threadId + "]" + "|" + "bad");
                }
                else if (action == "write")
                {
                    // If writing hashes send the hash to the file
                    writeQueue.Add(file + "|" + fileHash + "|" + threadId);
                }
            }

            return true;
        }

        static void reporter()
        {
            while (true)
            {
                if (reportQueue.Count != 0)
                {
                    reporting = true;
                    string info = reportQueue[0].Split('|')[0];
                    string result = reportQueue[0].Split('|')[1];
                    reportResult(info, result);
                    reportQueue.RemoveAt(0);
                }
                else { reporting = false; Thread.Sleep(100); }
            }
        }
        static void writer()
        {
            while (true)
            {
                if (writeQueue.Count != 0)
                {
                    writing = true;
                    string file = writeQueue[0].Split('|')[0];
                    string fileHash = writeQueue[0].Split('|')[1];
                    string thread = writeQueue[0].Split('|')[2];
                    writeHash(file, fileHash, thread);
                    writeQueue.RemoveAt(0);
                }
                else { writing = false; Thread.Sleep(100); }
            }
        }
        static void hashWorker1()
        {
            //Boolean to know if thread exits normally
            bool hasExitedAfterWorkDone = processWork(1);
        }
        static void hashWorker2()
        {
            //Boolean to know if thread exits normally
            bool hasExitedAfterWorkDone = processWork(2);
        }
        static void hashWorker3()
        {
            //Boolean to know if thread exits normally
            bool hasExitedAfterWorkDone = processWork(3);
        }
        static void hashWorker4()
        {
            //Boolean to know if thread exits normally
            bool hasExitedAfterWorkDone = processWork(4);
        }

        static void showHeader()
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("GTAHash");
            Console.ForegroundColor = oldColor;
            Console.WriteLine("@ScarszRawr / github.com/Scarsz/GTAHash\n");
        }
        static string getHash(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
        static void reportResult(string info, string result)
        {
            Console.Title = "GTAHash | Hashed files: " + hashQueue.Count + "/" + fileList.Count;
            ConsoleColor oldColor = Console.BackgroundColor;
            switch (result)
            {
                case "good":
                    Console.BackgroundColor = goodColor;
                    Console.WriteLine("    " + info);
                    goodCount++;
                    break;
                case "bad":
                    Console.BackgroundColor = badColor;
                    Console.WriteLine("    " + info);
                    badCount++;
                    break;
                case "unknown":
                    Console.BackgroundColor = unknownColor;
                    Console.WriteLine("    " + info);
                    unknownCount++;
                    break;
            }
            Console.BackgroundColor = oldColor;
        }
        static void writeHash(string file, string fileHash, string thread)
        {
            Console.Title = "GTAHash | Hashed files: " + writeQueue.Count + "/" + fileList.Count;
            Console.WriteLine("Thread " + thread + ": Writing hash \"" + fileHash + "\" of \"" + file + "\"");
            //if (!File.Exists(hashFile))
            //    File.Create(hashFile);
            using (StreamWriter sw = File.AppendText(hashFile))
                sw.WriteLine(file + " " + fileHash);
        }
    }
}
