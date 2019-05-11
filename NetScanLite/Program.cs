using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetScanLite
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                string LocalIP = "1.1.1.1";
                string Subnet = "1.1.1.1";
                using (System.Net.Sockets.Socket Socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    Socket.BeginConnect("8.8.8.8", 65530, null, null).AsyncWaitHandle.WaitOne(500, true);
                    LocalIP = (Socket.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString();
                    foreach (var Interface in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        var UniAdd = Interface.GetIPProperties().UnicastAddresses;
                        foreach (var Add in UniAdd)
                            if (Add.Address.ToString() == LocalIP)
                                Subnet = Add.IPv4Mask.ToString();
                    }
                }

                byte[] mask = IPAddress.Parse(Subnet).GetAddressBytes();
                byte[] iprev = IPAddress.Parse(LocalIP).GetAddressBytes();
                byte[] netid = BitConverter.GetBytes(BitConverter.ToUInt32(iprev, 0) & BitConverter.ToUInt32(mask, 0));
                var inv_mask = mask.Select(r => (byte)~r).ToArray();
                uint start = BitConverter.ToUInt32(BitConverter.GetBytes(BitConverter.ToUInt32(netid, 0) ^ BitConverter.ToUInt32(new byte[4], 0)).Reverse().ToArray(), 0);
                uint end = BitConverter.ToUInt32(BitConverter.GetBytes(BitConverter.ToUInt32(netid, 0) ^ BitConverter.ToUInt32(inv_mask, 0)).Reverse().ToArray(), 0);
                uint current = start;
                
                while (current <= end)
                {
                    string Address = String.Join(".", BitConverter.GetBytes(current + 1).Reverse());
                    Ping p = new Ping();
                    p.PingCompleted += p_pingCompleted;
                    p.SendAsync(Address, 200, Address);
                    current++;
                }
                Console.ReadKey();
            }
        }

        public string GetNextIpAddress(byte[] addressBytes)
        {
            try
            {
                uint ipAsUint = BitConverter.ToUInt32(addressBytes, 0);
                var nextAddress = BitConverter.GetBytes(ipAsUint + 1);
                return String.Join(".", nextAddress.Reverse());
            }
            catch { }
            return null;
        }

        private static void p_pingCompleted(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e)
        {
            if (e.Reply.Status == IPStatus.Success)
                try
                {
                    Console.WriteLine($"[{(e.Reply.RoundtripTime == 0 ? "LOCAL" : e.Reply.RoundtripTime.ToString()).PadRight(5, '·')}] {e.UserState.ToString().PadRight(15)}{Dns.GetHostEntry(e.UserState.ToString()).HostName}");
                }
                catch
                {
                    Console.WriteLine($"[{(e.Reply.RoundtripTime == 0 ? "LOCAL" : e.Reply.RoundtripTime.ToString()).PadRight(5, '·')}] {e.UserState.ToString().PadRight(15)}");
                }
        }


    }
}
