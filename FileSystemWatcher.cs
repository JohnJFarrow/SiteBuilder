
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SiteBuilder
{
    partial class Builder
    {
        public void Watch()
        {
            // monitor a directory, if a *.md file is changed or created
            // regenerate the index and the output file

            using var watcher = new FileSystemWatcher(@".\content");
            watcher.Filter = "*.md";

            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = false;

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += NonStaticOnChanged;
            watcher.Error += OnError;


            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        public static string ToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            { 
                sb.Append(b.ToString("x2").ToLower());
            }

            return sb.ToString();
        }

        private bool IsFileLocked(string FileName, out FileStream? fs )
        {
            FileInfo file = new FileInfo( FileName );
            fs = null;
            try
            {
                fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
            }
            return false;
        }

        // hold a hash of the file contents, so when we get multiple events related to the same
        // save we can see if we need to rebuild the corresponding output file

        Dictionary<string, string > LastHashStrings = new Dictionary<string, string>();

        private void NonStaticOnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            var fileInfo = new FileInfo(e.FullPath);
            if (!fileInfo.Exists)
            {
                return;
            }

            // sleep otherwise with rapid saves we calculate a hash which is a mix of partially written file
            System.Threading.Thread.Sleep(500);


            byte[] NewHash = new byte[0];

            FileStream? stream = null;
            while ( IsFileLocked(e.FullPath, out stream ) )
            {
                System.Threading.Thread.Sleep(200);
                Console.WriteLine("sleeping 200");
            }

            if (stream != null)
            {
                using (HashAlgorithm hashAlgorithm = SHA256.Create())
                {
                    NewHash = hashAlgorithm.ComputeHash(stream);
                }
                // don't wait for garbage collection to release the file
                stream.Close();
            }

            string NewHashString = ToHexString(NewHash);

            string? LastHashString;
            if(LastHashStrings.TryGetValue( e.FullPath, out LastHashString) )
            {
                if(LastHashString == NewHashString )
                {
                    // we already processed this 
                    Console.WriteLine("for {0} found matching hash {1}", e.FullPath, NewHashString );
                    return;
                }
                else
                {
                    Console.WriteLine("for {0} found old hash {1} new hash {2}", e.FullPath, LastHashString, NewHashString );
                }
            }
            else
            {
                Console.WriteLine("for {0} found no hash", e.FullPath);
            }

            LastHashStrings[ e.FullPath ] = NewHashString;


            // for a given file track the last write time, to handle the fact that one change can
            // result in multiple calls
            Console.WriteLine("file {0} write time {1}", e.FullPath, fileInfo.LastAccessTimeUtc);

            UpdateOneFile(e.Name);
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}