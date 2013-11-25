namespace ICMP
{
    using System;
    using System.Linq;
    using System.Net;

    public class ICMPMessage
    {
        public ICMPType Type { get; set; }

        public byte Code { get; set; }

        public ushort Checksum { get; set; }

        public ushort Identifier { get; set; }

        public ushort SequenceNumber { get; set; }

        public byte[] Data { get; set; }

        public byte TTL { get; set; }

        public IPAddress Source { get; set; }

        public IPAddress Destination { get; set; }

        public ICMPMessage()
        {
            Data = new byte[0];
        }

        public ICMPMessage(ICMPType type, IPAddress destination)
        {
            this.Type = type;
            this.Destination = destination;
        }

        public static ICMPMessage Parse(byte[] data)
        {
            if (data.Length < 20)
            {
                // damn! kein ip header?!
                throw new Exception("No IP Header received!");
            }

            ICMPMessage m = new ICMPMessage();
            m.TTL = data[8];

            m.Source = new IPAddress((uint)data[12] | (uint)data[13] << 8 | (uint)data[14] << 16 | (uint)data[15] << 24);
            m.Destination = new IPAddress((uint)data[16] | (uint)data[17] << 8 | (uint)data[18] << 16 | (uint)data[19] << 24);

            // 20 bytes == IP Header
            byte[] icmpMessage = data.Skip(20).ToArray();

            if (icmpMessage.Length < 8)
            {
                // damn! kein icmp header?!
                throw new Exception("No ICMP Header received!");
            }

            m.Type = (ICMPType)icmpMessage[0];
            m.Code = icmpMessage[1];
            m.Checksum = (ushort)(icmpMessage[2] << 8 | icmpMessage[3]);
            m.Identifier = (ushort)(icmpMessage[4] << 8 | icmpMessage[5]);
            m.SequenceNumber = (ushort)(icmpMessage[6] << 8 | icmpMessage[7]);
            m.Data = icmpMessage.Skip(8).ToArray();

            return m;
        }

        public byte[] ToArray()
        {
            byte[] icmp = new byte[8 + this.Data.Length];

            icmp[0] = (byte)this.Type;
            icmp[1] = this.Code;
            icmp[4] = (byte)(this.Identifier & 0xff);
            icmp[5] = (byte)((this.Identifier & 0xff00) >> 8);
            icmp[6] = (byte)(this.SequenceNumber & 0xff);
            icmp[7] = (byte)((this.SequenceNumber & 0xff00) >> 8);

            ushort co = CalculateICMPChecksum(icmp);

            icmp[2] = (byte)((co & 0xFF00) >> 8);
            icmp[3] = (byte)(co & 0xFF);

            return icmp;
        }

        public static ushort CalculateICMPChecksum(byte[] data)
        {
            uint chcksm = 0;

            for (int i = 0; i < data.Length; i += 2)
            {
                byte h = (byte)(data.Length <= i + 1 ? 0 : data[i + 1]);
                chcksm += (uint)(data[i] << 8 | h);
            }

            return (ushort)(~((chcksm >> 16) + (chcksm & 0xffff) + (chcksm >> 16)));
        }

        public static implicit operator byte[](ICMPMessage m)
        {
            return m.ToArray();
        }
    }
}
