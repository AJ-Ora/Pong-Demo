
using System.Collections.Generic;
using UnityEngine;

public class PacketBuffer : MonoBehaviour
{
    private Queue<Packet> queue = new Queue<Packet>();
    private readonly object lockObject = new object();

    public void Store(in Packet packet)
    {
        lock (lockObject)
        {
            queue.Enqueue(packet);
        }
    }

    public void Store(in Packet[] packets)
    {
        lock (lockObject)
        {
            foreach (Packet packet in packets)
            {
                queue.Enqueue(packet);
            }
        }
    }

    public Packet Retrieve()
    {
        lock (lockObject)
        {
            return queue.Dequeue();
        }
    }

    public Packet[] RetrieveAll()
    {
        lock (lockObject)
        {
            Packet[] returnList = queue.ToArray();
            queue.Clear();
            return returnList;
        }
    }
}
