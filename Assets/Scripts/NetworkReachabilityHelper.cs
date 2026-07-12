using UnityEngine;

/// <summary>
/// Reports whether the device currently has internet connectivity.
/// </summary>
public static class NetworkReachabilityHelper
{
    public static bool HasInternet =>
        Application.internetReachability != NetworkReachability.NotReachable;
}
