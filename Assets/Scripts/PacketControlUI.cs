using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PacketControlUI : MonoBehaviour
{
    public TMP_Dropdown sourceDropdown;
    public TMP_Dropdown destinationDropdown;

    private NetworkManager networkManager;
    private List<NetworkDevice> devices =
        new List<NetworkDevice>();

    void Start()
    {
        networkManager =
            FindFirstObjectByType<NetworkManager>();

        LoadDevices();
    }

    void LoadDevices()
    {
        devices.Clear();

        devices.AddRange(
            FindObjectsByType<NetworkDevice>(
                FindObjectsSortMode.None
            )
        );

        devices.Sort(
            (a, b) =>
                a.deviceName.CompareTo(b.deviceName)
        );

        sourceDropdown.ClearOptions();
        destinationDropdown.ClearOptions();

        List<string> deviceNames =
            new List<string>();

        foreach (NetworkDevice device in devices)
        {
            deviceNames.Add(device.deviceName);
        }

        sourceDropdown.AddOptions(deviceNames);
        destinationDropdown.AddOptions(deviceNames);

        if (devices.Count > 1)
        {
            destinationDropdown.value = 1;
        }

        sourceDropdown.RefreshShownValue();
        destinationDropdown.RefreshShownValue();
    }

    public void SendPacket()
    {
        if (networkManager == null)
            return;

        if (devices.Count < 2)
            return;

        NetworkDevice source =
            devices[sourceDropdown.value];

        NetworkDevice destination =
            devices[destinationDropdown.value];

        if (source == destination)
        {
            Debug.LogWarning(
                "Source and destination cannot be the same."
            );

            return;
        }

        List<NetworkDevice> path =
            networkManager.FindPath(
                source,
                destination
            );

        if (path.Count == 0)
        {
            Debug.LogWarning(
                "No path found between " +
                source.deviceName +
                " and " +
                destination.deviceName
            );

            return;
        }

        Debug.Log(
            "Sending packet: " +
            source.deviceName +
            " → " +
            destination.deviceName
        );

        Debug.Log("Calculated path:");

        foreach (NetworkDevice device in path)
        {
            Debug.Log(
                "→ " +
                device.deviceName
            );
        }

        networkManager.CreatePacket(
            source,
            destination,
            path
        );
    }
}