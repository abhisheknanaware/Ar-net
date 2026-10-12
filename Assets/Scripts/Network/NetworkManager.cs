using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public List<NetworkDevice> devices = new List<NetworkDevice>();
    public List<NetworkConnection> connections = new List<NetworkConnection>();

    public GameObject packetPrefab;

    private NetworkLogUI networkLog;

    void Start()
    {
        FindDevices();
        FindConnections();

        networkLog = FindFirstObjectByType<NetworkLogUI>();

        Debug.Log("Devices found: " + devices.Count);
        Debug.Log("Connections found: " + connections.Count);

        NetworkDevice pc1 = GameObject.Find("PC1")?.GetComponent<NetworkDevice>();
        NetworkDevice switch1 = GameObject.Find("Switch1")?.GetComponent<NetworkDevice>();
        NetworkDevice router1 = GameObject.Find("Router1")?.GetComponent<NetworkDevice>();
        NetworkDevice server1 = GameObject.Find("Server1")?.GetComponent<NetworkDevice>();

        if (pc1 != null && switch1 != null)
        {
            Debug.Log("PC1 → Switch1: " + AreConnected(pc1, switch1));
        }

        if (switch1 != null && router1 != null)
        {
            Debug.Log("Switch1 → Router1: " + AreConnected(switch1, router1));
        }

        if (router1 != null && server1 != null)
        {
            Debug.Log("Router1 → Server1: " + AreConnected(router1, server1));
        }
    }

    void FindDevices()
    {
        devices.Clear();

        NetworkDevice[] foundDevices = FindObjectsByType<NetworkDevice>(
            FindObjectsSortMode.None
        );

        devices.AddRange(foundDevices);
    }

    void FindConnections()
    {
        connections.Clear();

        NetworkConnection[] foundConnections = FindObjectsByType<NetworkConnection>(
            FindObjectsSortMode.None
        );

        connections.AddRange(foundConnections);
    }

    // Instantiates a cable and connects two devices at runtime
    public NetworkConnection ConnectDevices(NetworkDevice a, NetworkDevice b, GameObject cablePrefab)
    {
        if (a == null || b == null || a == b)
        {
            Debug.LogWarning("ConnectDevices: Invalid device selection.");
            return null;
        }

        if (AreConnected(a, b))
        {
            Debug.LogWarning($"{a.deviceName} and {b.deviceName} are already connected!");
            return null;
        }

        if (cablePrefab == null)
        {
            Debug.LogError("ConnectDevices: Cable prefab is not assigned.");
            return null;
        }

        GameObject cableObject = Instantiate(cablePrefab, Vector3.zero, Quaternion.identity);
        NetworkConnection connection = cableObject.GetComponent<NetworkConnection>();

        if (connection == null)
        {
            Debug.LogError("ConnectDevices: Cable prefab is missing the NetworkConnection script!");
            Destroy(cableObject);
            return null;
        }

        // Initialize your existing NetworkConnection fields
        connection.deviceA = a;
        connection.deviceB = b;
        connection.isConnected = true;
        connection.ResetVisual();

        if (!connections.Contains(connection))
        {
            connections.Add(connection);
        }

        Debug.Log($"Connected: {a.deviceName} ↔ {b.deviceName}");
        return connection;
    }

    public bool AreConnected(NetworkDevice deviceA, NetworkDevice deviceB)
    {
        foreach (NetworkConnection connection in connections)
        {
            if (connection == null)
                continue;

            if (!connection.isConnected)
                continue;

            if (connection.Connects(deviceA, deviceB))
            {
                return true;
            }
        }

        return false;
    }

    public List<NetworkDevice> FindPath(NetworkDevice start, NetworkDevice target)
    {
        List<NetworkDevice> emptyPath = new List<NetworkDevice>();

        if (start == null || target == null || !start.isOnline || !target.isOnline)
        {
            Debug.LogWarning("FindPath: Start or target is NULL or offline.");
            return emptyPath;
        }

        Queue<NetworkDevice> queue = new Queue<NetworkDevice>();
        Dictionary<NetworkDevice, NetworkDevice> previous = new Dictionary<NetworkDevice, NetworkDevice>();
        HashSet<NetworkDevice> visited = new HashSet<NetworkDevice>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            NetworkDevice current = queue.Dequeue();

            if (current == target)
                break;

            foreach (NetworkConnection connection in connections)
            {
                if (connection == null || !connection.isConnected)
                    continue;

                NetworkDevice neighbor = null;

                if (connection.deviceA == current)
                {
                    neighbor = connection.deviceB;
                }
                else if (connection.deviceB == current)
                {
                    neighbor = connection.deviceA;
                }

                if (neighbor == null || !neighbor.isOnline || visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                previous[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        if (!visited.Contains(target))
        {
            Debug.Log($"NO ROUTE FOUND: {start.deviceName} → {target.deviceName}");

            if (networkLog == null)
            {
                networkLog = FindFirstObjectByType<NetworkLogUI>();
            }

            if (networkLog != null)
            {
                networkLog.ShowFailedPacket(start, target, "NO ROUTE AVAILABLE");
            }
            else
            {
                Debug.LogError("NetworkLogUI could not be found.");
            }

            return emptyPath;
        }

        List<NetworkDevice> path = new List<NetworkDevice>();
        NetworkDevice currentDevice = target;

        while (currentDevice != start)
        {
            path.Add(currentDevice);

            if (!previous.ContainsKey(currentDevice))
            {
                Debug.LogError("Path reconstruction failed.");
                return emptyPath;
            }

            currentDevice = previous[currentDevice];
        }

        path.Add(start);
        path.Reverse();

        return path;
    }

    public GameObject CreatePacket(NetworkDevice source, NetworkDevice destination, List<NetworkDevice> path)
    {
        if (source == null || destination == null)
        {
            Debug.LogError("CreatePacket: Source or destination is NULL.");
            return null;
        }

        if (path == null || path.Count < 2)
        {
            HandleFailedPacket(source, destination, "NO ROUTE AVAILABLE");
            return null;
        }

        if (packetPrefab == null)
        {
            Debug.LogError("Packet prefab is not assigned.");
            HandleFailedPacket(source, destination, "PACKET PREFAB NOT ASSIGNED");
            return null;
        }

        GameObject packetObject = Instantiate(
            packetPrefab,
            source.transform.position + Vector3.up * 0.3f,
            Quaternion.identity
        );

        Packet packet = packetObject.GetComponent<Packet>();

        if (packet == null)
        {
            Debug.LogError("Packet script missing from prefab.");
            Destroy(packetObject);
            HandleFailedPacket(source, destination, "PACKET SCRIPT MISSING");
            return null;
        }

        if (NetworkStatistics.Instance != null)
        {
            NetworkStatistics.Instance.PacketSent();
        }

        packet.Initialize(source, destination, path);

        Debug.Log($"Packet created: {source.deviceName} → {destination.deviceName}");
        return packetObject;
    }

    void HandleFailedPacket(NetworkDevice source, NetworkDevice destination, string reason)
    {
        if (NetworkStatistics.Instance != null)
        {
            NetworkStatistics.Instance.PacketFailed();
        }

        Debug.Log($"PACKET FAILED: {source.deviceName} → {destination.deviceName} | {reason}");

        if (networkLog == null)
        {
            networkLog = FindFirstObjectByType<NetworkLogUI>();
        }

        if (networkLog != null)
        {
            networkLog.ShowFailedPacket(source, destination, reason);
        }
        else
        {
            Debug.LogError("NetworkLogUI could not be found while reporting failure.");
        }
    }
}