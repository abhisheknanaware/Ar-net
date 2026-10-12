using UnityEngine;

public class Router : NetworkDevice
{
    public void RoutePacket(string destinationIP)
    {
        Debug.Log(deviceName + " is routing packet to: " + destinationIP);
    }
}