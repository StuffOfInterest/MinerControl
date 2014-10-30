using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MinerControl.Utility.Multicast
{
    public class MulticastDataReceivedEventArgs : EventArgs
    {
        private byte[] _data;
        private IPEndPoint _remote;

        public MulticastDataReceivedEventArgs(IPEndPoint remote, byte[] data)
        {
            _remote = remote;
            _data = data;
        }

        public IPEndPoint RemoteEndPoint { get { return _remote; } }

        public byte[] Data { get { return _data; } }

        public string StringData { get { return Encoding.Unicode.GetString(_data); } }
    }
}
