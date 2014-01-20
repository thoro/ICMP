

namespace Ping
{
    using ICMP;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Thoro.Console;

    class Program
    {
        static void Main(string[] args)
        {
            ArgumentParser arg = new ArgumentParser("ping");

            var echoCount = new Argument<int>("count", 4);
            var bufferSize = new Argument<int>("size", 32);
            var ttl = new Argument<short>("TTL", 64);
            var timeout = new Argument<int>("timeout", 2000);
            var targetname = new Argument<string>("targetname");
            var endless = arg.AddOption("t", help: "Ping the specified host until stopped.");
            var rarp = arg.AddOption("a", help: "Resolve addresses to hostnames");
            var help = arg.AddOption("h", help: "Show this help");

            help.IsUsed += (se, e) =>
            {
                Console.Write(arg.Help());
                Environment.Exit(0);
            };

            arg.AddOption("n", arguments: new Argument[] { echoCount }, help: "Number of echo requests to send.");
            arg.AddOption("l", arguments: new Argument[] { bufferSize }, help: "Send buffer size.");
            arg.AddOption("i", arguments: new Argument[] { ttl }, help: "Time To Live.");
            arg.AddOption("w", arguments: new Argument[] { timeout }, help: "Timeout in milliseconds to wait for each reply.");
            arg.AddArgument(targetname);

            if (!arg.Parse(args))
            {
                Console.WriteLine(arg.Help());
                return;
            }

            IPAddress ip = null;

            try
            {
                ip = Dns.GetHostEntry(targetname.Value).AddressList.FirstOrDefault();
            }
            catch(Exception)
            {
                ip = null;
            }

            if (ip == null)
            {
                Console.WriteLine("IP or Host is invalid!");
                return;
            }

            int dataLen = bufferSize.Value;
            int waitTime = timeout.Value;
            int count = echoCount.Value;

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            s.ReceiveTimeout = timeout.Value;

            Console.WriteLine("Pinging " + ip + " with " + dataLen + " bytes of data:");

            while (endless.Used || count > 0)
            {
                if (!endless.Used)
                {
                    count--;
                }

                ICMPMessage m = new ICMPMessage(ICMPType.Echo, ip);
                m.Data = new byte[dataLen];

                // Generate Random Data as Payload
                Random rand = new Random();
                for (int i = 0; i < dataLen; i++)
                {
                    m.Data[i] = (byte)rand.Next(255);
                }

                Stopwatch timing = new Stopwatch();
                
                s.Ttl = ttl.Value;

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
                        Console.WriteLine("Reply from {0}: bytes={1} time={2}ms TTL={3}", m.Source, m.Data.Length, timing.Elapsed.TotalMilliseconds, m.TTL);
                    }
                    else if (m.Type == ICMPType.TimeToLiveExpired)
                    {
                        Console.WriteLine("Reply from {0}: TTL expired in transit.", m.Source);
                    }

                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine("Request timed out.");
                    }
                    else
                    {
                        Console.WriteLine("Error: " + ex.SocketErrorCode);
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }
}