
using static LogUtility;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class AccessPoint : MonoBehaviour
{
    private struct UdpState
    {
        public UdpClient client;
        public IPEndPoint endPoint;

        public UdpState(UdpClient client, IPEndPoint endPoint)
        {
            this.client = client;
            this.endPoint = endPoint;
        }
    }

    #region Variables

    [SerializeField] private PacketBuffer bufferObject = null;

    [SerializeField] [ReadOnly] private bool _isActive = false;
    public bool IsActive
    {
        get => _isActive;
        private set => _isActive = value;
    }

    private UdpClient client;
    private UdpState state;
    private byte[] header;

    #endregion

    private void Awake() => header = Encoding.ASCII.GetBytes("PONG");

    private void OnDestroy() => Close();

    public bool Listen(ushort port)
    {
        if (IsActive)
        {
            LogWarning("Access point already active!");
            return false;
        }

        if (bufferObject == null)
        {
            LogError("Buffer is not assigned!");
            return false;
        }

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

        while (client == null)
        {
            try
            {
                client = new UdpClient(endPoint);
            }
            catch (SocketException)
            {
                if (endPoint.Port >= 65535)
                {
                    LogError("Highest possible port number is already taken!");
                    Close();
                    return false;
                }

                LogWarning("Port number " + endPoint.Port + " is already taken, trying to get one higher...");
                endPoint = new IPEndPoint(IPAddress.Any, endPoint.Port + 1);
            }
            catch (Exception e)
            {
                LogError(e.ToString());
                Close();
                return false;
            }
        }

        state = new UdpState(client, endPoint);

        client.BeginReceive(new AsyncCallback(OnMessageReceived), state);
        IsActive = true;
        Log("Access point listening for new messages at " + endPoint.ToString() + ".");
        return true;
    }

    public void Close()
    {
        if (!IsActive) return;

        client.Close();
        client = null;
        IsActive = false;
        Log("Access point disabled.");
    }

    #region Message Functions

    private void OnMessageReceived(IAsyncResult result)
    {
        UdpClient udp = ((UdpState)(result.AsyncState)).client;
        IPEndPoint ep = ((UdpState)(result.AsyncState)).endPoint;

        byte[] message;

        try
        {
            message = udp.EndReceive(result, ref ep);
            udp.BeginReceive(new AsyncCallback(OnMessageReceived), state);
        }
        catch (ObjectDisposedException)
        {
            // Underlying socket has been closed / disposed.
            // Unfortunately, it's not possible to end BeginReceive
            // early without causing an exception.
            return;
        }
        catch (SocketException)
        {
            // "Connection reset by peer" expected.
            udp.BeginReceive(new AsyncCallback(OnMessageReceived), state);
            return;
        }
        catch (Exception e)
        {
            // An unexpected error happened.
            LogError(e.ToString());
            Close();
            return;
        }

        if (!ByteTools.IsIdentical(message, 0, header, 0, (uint)header.Length))
        {
            LogWarning("Header mismatch detected!");
            return;
        }

        if (bufferObject == null)
        {
            LogError("Received a message, but buffer is not assigned!");
            Close();
            return;
        }

        byte[] messageWithoutHeader = new byte[message.Length - header.Length];
        Buffer.BlockCopy(message, header.Length, messageWithoutHeader, 0, messageWithoutHeader.Length);

        Packet packet = new Packet(ep, messageWithoutHeader);

        bufferObject.Store(packet);
    }

    public void SendMessage(in byte[] message, IPEndPoint endPoint)
    {
        if (!IsActive)
        {
            LogError("Tried to send a message, but access point isn't active!");
            return;
        }

        byte[] messageWithHeader = new byte[header.Length + message.Length];
        Buffer.BlockCopy(header, 0, messageWithHeader, 0, header.Length);
        Buffer.BlockCopy(message, 0, messageWithHeader, header.Length, message.Length);

        client.Send(messageWithHeader, messageWithHeader.Length, endPoint);
    }

    public void SendMessage(in byte[] message, List<IPEndPoint> endPoints)
    {
        foreach (IPEndPoint ep in endPoints)
        {
            SendMessage(message, ep);
        }
    }

    #endregion
}
