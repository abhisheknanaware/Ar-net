using UnityEngine;

public class PC : NetworkDevice
{
    public void SendPacket(string destinationIP)
    {
        Debug.Log(deviceName + " is sending packet to " + destinationIP);
    }

    public void ReceivePacket()
    {
        Debug.Log(deviceName + " received a packet.");
    }
}