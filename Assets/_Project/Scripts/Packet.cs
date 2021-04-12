
using System.Net;

public struct Packet
{
    public readonly IPEndPoint endPoint;
    public readonly byte[] message;
    public uint bytesRead;

    public Packet(IPEndPoint endPoint, byte[] message)
    {
        this.endPoint = endPoint;
        this.message = message;
        bytesRead = 0;
    }

    public Packet(IPEndPoint endPoint, byte[] message, uint bytesRead)
    {
        this.endPoint = endPoint;
        this.message = message;
        this.bytesRead = bytesRead;
    }
}
