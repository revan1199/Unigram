// <auto-generated/>
using System;

namespace Telegram.Api.TL
{
	public partial class TLDCOption : TLObject 
	{
		[Flags]
		public enum Flag : Int32
		{
			Ipv6 = (1 << 0),
			MediaOnly = (1 << 1),
			TcpoOnly = (1 << 2),
			Cdn = (1 << 3),
		}

		public bool IsIpv6 { get { return Flags.HasFlag(Flag.Ipv6); } set { Flags = value ? (Flags | Flag.Ipv6) : (Flags & ~Flag.Ipv6); } }
		public bool IsMediaOnly { get { return Flags.HasFlag(Flag.MediaOnly); } set { Flags = value ? (Flags | Flag.MediaOnly) : (Flags & ~Flag.MediaOnly); } }
		public bool IsTcpoOnly { get { return Flags.HasFlag(Flag.TcpoOnly); } set { Flags = value ? (Flags | Flag.TcpoOnly) : (Flags & ~Flag.TcpoOnly); } }
		public bool IsCdn { get { return Flags.HasFlag(Flag.Cdn); } set { Flags = value ? (Flags | Flag.Cdn) : (Flags & ~Flag.Cdn); } }

		public Flag Flags { get; set; }
		public Int32 Id { get; set; }
		public String IpAddress { get; set; }
		public Int32 Port { get; set; }

		public TLDCOption() { }
		public TLDCOption(TLBinaryReader from)
		{
			Read(from);
		}

		public override TLType TypeId { get { return TLType.DCOption; } }

		public override void Read(TLBinaryReader from)
		{
			Flags = (Flag)from.ReadInt32();
			Id = from.ReadInt32();
			IpAddress = from.ReadString();
			Port = from.ReadInt32();
		}

		public override void Write(TLBinaryWriter to)
		{
			to.Write(0x5D8C6CC);
			to.Write((Int32)Flags);
			to.Write(Id);
			to.Write(IpAddress);
			to.Write(Port);
		}
	}
}