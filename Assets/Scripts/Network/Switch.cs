using UnityEngine;

public class Switch : NetworkDevice
{
    public void ForwardPacket(string destinationMAC)
    {
        Debug.Log(deviceName + " is forwarding packet to MAC: " + destinationMAC);
    }
}