using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CatCubeLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var servers = scanActiveProcesses();
            
            Console.WriteLine("[");
            for (int i = 0; i < servers.Count; i++)
            {
                var s = servers[i];
                Console.WriteLine("  {");
                Console.WriteLine("    \"id\": " + (i+1) + ",");
                Console.WriteLine("    \"name\": \"" + s.Name + "\",");
                Console.WriteLine("    \"host\": \"" + s.Host + "\",");
                Console.WriteLine("    \"players\": \"" + s.Players + "\",");
                Console.WriteLine("    \"thumb\": \"" + s.Thumb + "\",");
                Console.WriteLine("    \"status\": \"ONLINE\"");
                Console.WriteLine("  }" + (i < servers.Count - 1 ? "," : ""));
            }
            Console.WriteLine("]");
        }

        static List<ServerInfo> scanActiveProcesses()
        {
            var list = new List<ServerInfo>();
            
            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "ps",
                    Arguments = "-eo pid,args",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo)) {
                    using (var reader = process.StandardOutput) {
                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            // Look for 'CatCube' and '--server' anywhere in the command line
                            if (line.Contains("CatCube") && line.Contains("--server")) {
                                var mapName = "Unknown Map";
                                var port = "53640";
                                
                                var matchMap = Regex.Match(line, "--mapname\\s+([^\\s]+)");
                                if (matchMap.Success) mapName = matchMap.Groups[1].Value.Replace("_", " ");
                                
                                var matchPort = Regex.Match(line, "--port\\s+([0-9]+)");
                                if (matchPort.Success) port = matchPort.Groups[1].Value;

                                list.Add(new ServerInfo {
                                    Name = mapName,
                                    Host = "127.0.0.1:" + port,
                                    Players = "LIVE",
                                    Thumb = getThumb(mapName)
                                });
                            }
                        }
                    }
                }
            } catch { }

            return list;
        }

        static string getThumb(string map) {
            map = map.ToLower();
            if (map.Contains("test")) return "🧪";
            if (map.Contains("obby")) return "🏃";
            if (map.Contains("sword")) return "⚔️";
            if (map.Contains("base")) return "🧱";
            return "🏰";
        }
    }

    class ServerInfo
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string Players { get; set; }
        public string Thumb { get; set; }
    }
}
