// <auto-generated/>
using System;

namespace Telegram.Api.TL
{
	public partial class TLWebPageNotModified : TLWebPageBase 
	{
		public TLWebPageNotModified() { }
		public TLWebPageNotModified(TLBinaryReader from)
		{
			Read(from);
		}

		public override TLType TypeId { get { return TLType.WebPageNotModified; } }

		public override void Read(TLBinaryReader from)
		{
		}

		public override void Write(TLBinaryWriter to)
		{
			to.Write(0x85849473);
		}
	}
}