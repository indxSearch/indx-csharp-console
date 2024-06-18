using IndxSearchLib;
using IndxSearchLib.API_Files;
namespace IndxConsoleApp
{
    internal class Program
    {
        private static void Main()
        {

            // INDX CONSOLE APP FOR C# LIB V3.3
            // Docs: https://docs.indx.co/csharp/

            //
            // CREATE INSTANCE
            //
            var SearchEngine = new IndxSearchEngine();


            //
            // READ DATA FROM FILE
            //

            string fileName = "tmdb_top10k_movies.txt";
            string fileUrl = "data/" + fileName;
            //handle visual studio and vscode relative paths
            if (!File.Exists("data/" + fileName)) fileUrl = "../../../data/" + fileName;


            Console.Write($"\rProcessing {fileName}");
            string path = Path.Combine(Environment.CurrentDirectory, fileUrl);
            var lines = File.ReadAllLines(path);


            //
            // CREATE DOCUMENTS
            //

            int key = 0; // foreign key. This can also be an ID or a key of your choice.
            /* 
            Several documents can also be the same key, and with removeDuplicates set to true 
            only the one with highest score will be returned
            */
            var documents = new List<Document>();
            foreach (var item in lines)
            {
                int segment = 0; // use this if you want to search for multiple segments of the same key
                string clientText = ""; // use this to add non-indexed information you can retrieve later
                var doc = new Document(key, segment, item, clientText);
                documents.Add(doc);
                key++;
            }

            SearchEngine.Insert(documents.ToArray());
            Console.WriteLine($"\rProcessing {fileName} done\n");


            //
            // RUN INDEXING
            //

            SearchEngine.IndexAsync();

            // Check status if ready to search
            var status = SearchEngine.Status;
            DateTime startTime = DateTime.Now;
            double timeSpent = 0;

            while (status.SystemState != SystemState.Ready)
            {
                timeSpent = (DateTime.Now - startTime).TotalMilliseconds;
                
                // Draw progress bar
                int progressPercent = status.IndexProgressPercent;
                string progressBar = DrawProgressBar(progressPercent, 30);

                // Update the console with progress bar and progress percentage
                Console.Write($"\rIndexing {lines.Length} records [{progressBar}] {progressPercent}% in {(int)timeSpent} ms");

                Thread.Sleep(10); // log every 10ms
            }

            static string DrawProgressBar(int progress, int totalWidth)
            {
                int filledWidth = (int)(progress / 100.0 * totalWidth);
                string filledPart = new string('=', filledWidth);
                string unfilledPart = new string('-', totalWidth - filledWidth);
                return $"{filledPart}{unfilledPart}";
            }


            // Clear screen when indexed
            Console.Clear();


            // Print when index is done
            int indexTime = (int)timeSpent;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🟢 Indexed '{fileName}' ({status.DocumentCount} documents) and ready to search in {indexTime/1000} seconds ({indexTime} ms) \n");
            Console.ResetColor();


            //
            // SEARCH
            //

            bool continueSearch = true;
            while (continueSearch)
            {

                //
                // Set up search query
                //

                Console.Write("🔍 Search: ");
                var text = Console.ReadLine() ?? ""; // pattern to be searched for

                var applyCoverage = true; // Disable this to run pure pattern recognition search
                var numRecords = 30; // Records to be returned
                var timeOutLimit = 1000; // Timeout if cpu overload in milliseconds
                var rmDuplicates = false; // remove duplicates with same key
                var logPrefix = ""; // logger prefix per search

                // Set up coverage (this only activates with applyCoverage true)
                var coverageSetup = new CoverageSetup();
                coverageSetup.MinWordSize = 2;

                var query = new SearchQuery(text, applyCoverage, numRecords, timeOutLimit, null, null, null, null, rmDuplicates, logPrefix, null, coverageSetup);

                //
                // Search and list results
                //

                var result = SearchEngine.Search(query);
                var truncateByCoverage = true; // Needs Algorithm.Coverage to work
                int minimumScore = 40; // 0-255
                int index = 0;
                Console.WriteLine(""); // space
                foreach (var record in result.SearchRecords)
                {
                    if (record.MetricScore < minimumScore) break;
                    if (truncateByCoverage && result.CoverageBottomIndex != -1 && index > result.CoverageBottomIndex)
                    {
                        break;
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"{index}\t");

                    Console.ResetColor();
                    Console.Write(record.DocumentTextToBeIndexed);

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($" ({record.MetricScore})");

                    Console.ResetColor();
                    
                    index++;
                }

                //
                // MEASURE PERFORMANCE
                //

                Console.ForegroundColor = ConsoleColor.Gray;
                // run multiple times to benchmark response time properly
                var numReps = 100;
                startTime = DateTime.Now;
                Parallel.For(1, numReps, i => {
                    SearchEngine.Search(query);
                });
                var timeMs = (DateTime.Now - startTime).TotalMilliseconds / numReps;
                Console.WriteLine("\nResponse time " + timeMs.ToString("F3") + $" ms ({numReps} reps)");

                //collect and console write amount of memory used
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.WriteLine("Memory used: " + GC.GetTotalMemory(false) / 1024 / 1024 + " MB");
                Console.WriteLine("Version: " + SearchEngine.Status.Version);

                // Indx License information
                Console.WriteLine("Valid License: " + SearchEngine.Status.ValidLicense + " / Exp " + SearchEngine.Status.LicenseExpirationDate);

                // Continue prompt
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("\nPress ESC to quit or any other key to continue.");
                Console.ResetColor();
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Escape) continueSearch = false;
                else Console.Clear();
            } // end while loop

        } // end main

    } // end class

} // end namespace