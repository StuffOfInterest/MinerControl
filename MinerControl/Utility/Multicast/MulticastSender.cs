using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MinerControl.Utility.Multicast
{
    public class MulticastSender : IDisposable
    {
        private IPEndPoint _endPoint;
        private int _timeToLive;
        private UdpClient _udpClient;

        public MulticastSender(IPEndPoint endPoint, int timeToLive)
        {
            _endPoint = endPoint;
            _timeToLive = timeToLive;
        }

        public void Start()
        {
            _udpClient = new UdpClient();
            _udpClient.JoinMulticastGroup(_endPoint.Address);
        }

        public void Stop()
        {
            _udpClient.DropMulticastGroup(_endPoint.Address);
            _udpClient.Close();
            _udpClient = null;
        }

        public void Send(byte[] data)
        {
            _udpClient.Send(data, data.Length, _endPoint);
        }

        public void Send(string data)
        {
            Send(Encoding.Unicode.GetBytes(data));
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            if (_udpClient != null)
            {
                Stop();
            }
            _disposed = true;
        }
    }
}
