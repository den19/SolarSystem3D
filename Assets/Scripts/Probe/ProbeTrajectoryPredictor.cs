using UnityEngine;

/// <summary>
/// Short-horizon ghost path using frozen attractors.
/// </summary>
public static class ProbeTrajectoryPredictor
{
    public const int PointCount = 72;
    public const float HorizonSeconds = 14f;

    public static int Predict(
        Vector3 startPosition,
        Vector3 startVelocity,
        ProbeGravityIntegrator.Attractor[] attractors,
        int attractorCount,
        Vector3[] buffer)
    {
        if (buffer == null || buffer.Length == 0 || attractorCount <= 0)
            return 0;

        int count = Mathf.Min(PointCount, buffer.Length);
        Vector3 pos = startPosition;
        Vector3 vel = startVelocity;
        float dt = HorizonSeconds / Mathf.Max(1, count - 1);
        buffer[0] = pos;

        for (int i = 1; i < count; i++)
        {
            ProbeGravityIntegrator.IntegrateVerlet(ref pos, ref vel, attractors, attractorCount, dt);
            buffer[i] = pos;
        }

        return count;
    }
}
