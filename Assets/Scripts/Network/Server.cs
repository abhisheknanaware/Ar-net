using UnityEngine;

public class Server : NetworkDevice
{
    public void ReceiveRequest()
    {
        Debug.Log(deviceName + " received a request.");
    }
}