using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MinerControl.Utility.Multicast
{
    public delegate void MulticastDataReceivedEventHandler(object sender, MulticastDataReceivedEventArgs e);

    public class MulticastReceiver : IDisposable
    {
        private Thread _listener;
        private IPEndPoint _endPoint;

        public event MulticastDataReceivedEventHandler DataReceived;

        public MulticastReceiver(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public void Start()
        {
            _listener = new Thread(new ThreadStart(BackgroundListener))
            {
                IsBackground = true
            };
            _listener.Start();
        }

        public void Stop()
        {
            lock (this)
            {
                _listener.Abort();
                _listener.Join();
            }
        }

        private void BackgroundListener()
        {
            var bindingEndpoint = new IPEndPoint(IPAddress.Any, _endPoint.Port);
            using (var client = new UdpClient())
            {
                client.ExclusiveAddressUse = false;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(bindingEndpoint);
                client.JoinMulticastGroup(_endPoint.Address);

                var keepRunning = true;
                while (keepRunning)
                {
                    try
                    {
                        IPEndPoint remote = new IPEndPoint(IPAddress.Any, _endPoint.Port);
                        var buffer = client.Receive(ref remote);
                        lock (this)
                        {
                            DataReceived(this, new MulticastDataReceivedEventArgs(remote, buffer));
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        keepRunning = false;
                        Thread.ResetAbort();
                    }
                }

                client.DropMulticastGroup(_endPoint.Address);
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            if (_listener.IsAlive)
            {
                Stop();
            }
            _disposed = true;
        }
    }
}
