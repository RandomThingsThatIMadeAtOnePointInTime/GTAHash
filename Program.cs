using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace GTA5_MD5_Checker
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            ConsoleColor oldBackColor = Console.BackgroundColor;
            WebClient client = new WebClient();
            int goodCount = 0;
            int idkCount = 0;
            int badCount = 0;
            showHeader();
            showDirectory();
            bool deleteFiles = areWeDeleting();
            string[] fileHashList = client.DownloadString("https://github.com/Scarsz/GTA5-MD5-Checker/raw/master/hashes").Split(new string[] { "\n", " " }, StringSplitOptions.None);
            string[] files = Directory.GetFiles(".\\");
            // Change this for a different text color of the scanning process
            Console.ForegroundColor = ConsoleColor.White;
            foreach (string file in Directory.EnumerateFiles(".\\", "*.*", SearchOption.AllDirectories))
            {
                // Crude way to exclude this exe from the search as well as _CommonRedist
                if (file != ".\\" + System.AppDomain.CurrentDomain.FriendlyName & file.Split('\\')[1] != "_CommonRedist")
                {
                    int index = Array.IndexOf(fileHashList, file);
                    int hashIndex = index + 1;
                    if (index == -1)
                    {
                        // File list is either outdated or custom files were added
                        Console.BackgroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("    " + file);
                        idkCount++;
                    }
                    else
                    {
                        // File was found in array
                        if (getHash(file) == fileHashList[hashIndex])
                        {
                            // Match
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("    " + file);
                            goodCount++;
                        }
                        else
                        {
                            // Not so match
                            Console.BackgroundColor = ConsoleColor.Red;
                            if (deleteFiles)
                            {
                                Console.WriteLine("    " + file + " [DELETED]");
                                // We should be deleting the files so they can be downloaded later, let's do that
                                File.Delete(file);
                            }
                            else
                            {
                                Console.WriteLine("    " + file);
                            }
                            badCount++;
                        }
                    }
                }
            }
            Console.BackgroundColor = oldBackColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nScan completed with " + goodCount + " good files, " + idkCount + " unknown files, and " + badCount + " bad files.");
            Console.ReadKey();
            Console.ForegroundColor = oldColor;
        }

        static string getHash(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    string hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    return hash;
                }
            }
        }
        static void showHeader()
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("GTA5 PC MD5 Checker");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("v1 @ScarszRawr");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("████ File hash and expected hash matches");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("████ Expected file hash unknown (custom file or out of date hash list)");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("████ File hash and expected hash do not match");
            Console.WriteLine();

            Console.ForegroundColor = oldColor;
        }
        static void showDirectory()
        {
            writeInfo("Scanning directory: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Directory.GetCurrentDirectory() + "\n");
        }
        static void writeInfo(string message, bool newline = true)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            if (newline)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }
            Console.ForegroundColor = oldColor;
        }
        static string getResponse(string message)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            writeInfo(message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Response [\"y\"=yes \"n\"=no]: ");
            string response = Console.ReadKey().Key.ToString().ToLower();
            if (response == "y")
            {
                Console.WriteLine("\n");
                return response;
            }
            else if (response == "n")
            {
                Console.WriteLine("\n");
                return response;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid response \"" + response + "\", this was a very simple question.");
                Console.ForegroundColor = oldColor;
                Environment.Exit(1);
            }
            Console.WriteLine();
            Console.ForegroundColor = oldColor;
            return response;
        }
        static bool areWeDeleting()
        {
            string deleteFiles = getResponse("Do you want to automatically delete files that do not match?");

            if (deleteFiles == "y")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
