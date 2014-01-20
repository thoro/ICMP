namespace Traceroute
{
    using ICMP;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using Thoro.Console;

    class Program
    {
        /*
        Usage: tracert [-d] [-h maximum_hops] [-j host-list] [-w timeout]
               [-R] [-S srcaddr] [-4] [-6] target_name

Options:
    -d                 Do not resolve addresses to hostnames.
    -h maximum_hops    Maximum number of hops to search for target.
    -j host-list       Loose source route along host-list (IPv4-only).
    -w timeout         Wait timeout milliseconds for each reply.
    -R                 Trace round-trip path (IPv6-only).
    -S srcaddr         Source address to use (IPv6-only).
    -4                 Force using IPv4.
    -6                 Force using IPv6.*/

        static void Main(string[] args)
        {
            ArgumentParser arg = new ArgumentParser("tracert");

            var maximumHops = new Argument<int>("maximumhops", 30);
            var timeout = new Argument<int>("timeout", 2000);
            
            var dontresolve = arg.AddOption("d", help: "Do not resolve addresses to hostnames.");
            var targetname = new Argument<string>("targetname");
            var help = arg.AddOption("h", help: "Show this help");

            help.IsUsed += (se, e) =>
            {
                Console.Write(arg.Help());
                Environment.Exit(0);
            };

            arg.AddOption("h", arguments: new Argument[] { maximumHops }, help: "Maximum number of hops to search for target.");
            arg.AddOption("w", arguments: new Argument[] { timeout }, help: "Wait timeout milliseconds for each reply.");
            arg.AddArgument(targetname);

            if (!arg.Parse(args))
            {
                Console.WriteLine(arg.Help());
                return;
            }

            string name = targetname.Value;
            IPAddress ip = null;

            try
            {
                if (IPAddress.TryParse(name, out ip))
                {
                    name = Dns.GetHostEntry(ip).HostName;   
                }
                else
                {
                    ip = Dns.GetHostEntry(name).AddressList.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                ip = null;
            }

            if (ip == null)
            {
                Console.WriteLine("IP or Host is invalid!");
                return;
            }

            int dataLen = 0;
            int waitTime = timeout.Value;

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            s.ReceiveTimeout = timeout.Value;

            Console.WriteLine("Tracing route to {0} [{1}]", name, ip);
            Console.WriteLine("over a maximum of {0} hops:", maximumHops.Value);
            Console.WriteLine();

            bool targetReached = false;
            short currentTtl = 1;

            while (!targetReached || currentTtl == maximumHops.Value)
            {
                ICMPMessage m = new ICMPMessage(ICMPType.Echo, ip);
                m.Data = new byte[dataLen];

                // Generate Random Data as Payload
                Random rand = new Random();
                for (int i = 0; i < dataLen; i++)
                {
                    m.Data[i] = (byte)rand.Next(255);
                }

                Stopwatch timing = new Stopwatch();

                s.Ttl = currentTtl;

                timing.Start();
                s.SendTo(m, new IPEndPoint(m.Destination, 0));

                byte[] data = new byte[2000];

                try
                {
                    EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    int recv = s.ReceiveFrom(data, ref ep);
                    timing.Stop();
                    DateTime receiveTime = DateTime.Now;
                    m = ICMPMessage.Parse(data.Take(recv).ToArray());

                    if (m.Type == ICMPType.EchoReply)
                    {
                        targetReached = true;
                        PrintResult(!dontresolve.Used, currentTtl, m, timing);
                    }
                    else if (m.Type == ICMPType.TimeToLiveExpired)
                    {
                        PrintResult(!dontresolve.Used, currentTtl, m, timing);
                    }

                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine("{0,-3} {1,10}    Request timed out", currentTtl, "*");
                    }
                    else
                    {
                        Console.WriteLine("Error: " + ex.SocketErrorCode);
                    }
                }

                currentTtl++;
            }

            Console.WriteLine();
            Console.WriteLine("Trace complete.");
        }

        private static void PrintResult(bool resolve, int currentTtl, ICMPMessage m, Stopwatch timing)
        {
            string hostname = "";
            if (resolve)
            {
                try
                {
                    hostname = Dns.GetHostEntry(m.Source).HostName;
                }
                catch
                {

                }
            }

            if (string.IsNullOrWhiteSpace(hostname))
            {
                Console.WriteLine("{0,-3} {1,10:0.0000} ms {2}", currentTtl, timing.Elapsed.TotalMilliseconds, m.Source);
            }
            else
            {
                Console.WriteLine("{0,-3} {1,10:0.0000} ms {2} [{3}]", currentTtl, timing.Elapsed.TotalMilliseconds, hostname, m.Source);
            }
        }
    }
}
