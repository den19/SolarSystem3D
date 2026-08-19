using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Runtime probe instance: velocity, loadout, and visual hooks.
/// </summary>
public class ProbeCraft : MonoBehaviour
{
    public ProbeModelKind Model { get; private set; }
    public Vector3 Velocity;
    public bool HasAntenna;
    public bool HasEngine;
    public bool HasShield;
    public int BurnsRemaining;
    public Transform Antenna;
    public string OriginName;
    public bool Overheated;
    public bool Impacted;

    public void Configure(ProbeModelKind kind)
    {
        Model = kind;
        HasAntenna = ProbeSettings.ResolveHasAntenna();
        HasEngine = ProbeSettings.ResolveHasEngine();
        HasShield = ProbeSettings.ResolveHasShield();
        BurnsRemaining = HasEngine ? 3 : 0;
    }

    public void PointAntennaAt(Vector3 worldPoint)
    {
        if (Antenna == null || !HasAntenna)
            return;

        Vector3 to = worldPoint - Antenna.position;
        if (to.sqrMagnitude < 1e-6f)
            return;

        Antenna.rotation = Quaternion.LookRotation(to.normalized, transform.up) * Quaternion.Euler(90f, 0f, 0f);
    }

    public void AlignToVelocity()
    {
        if (Velocity.sqrMagnitude < 1e-6f)
            return;

        transform.rotation = Quaternion.LookRotation(Velocity.normalized, Vector3.up);
    }
}
