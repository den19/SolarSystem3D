using UnityEngine;

/// <summary>
/// Restricted N-body acceleration in current Unity world units.
/// </summary>
public static class ProbeGravityIntegrator
{
    public const int DefaultSubsteps = 6;
    public const float MaxPhysicsDelta = 0.2f;

    public struct Attractor
    {
        public Vector3 position;
        public Vector3 velocity;
        public float gm;
        public float radius;
        public string name;
        public Transform transform;
    }

    public static float GetSimulationDeltaTime()
    {
        if (SimulationTimeController.IsPaused)
            return 0f;

        if (SolarSystemApp.SimulationClock.DrivesMotion)
            return Time.unscaledDeltaTime * SimulationTimeController.SpeedMultiplier;

        return Time.deltaTime;
    }

    public static Vector3 Acceleration(Vector3 position, Attractor[] attractors, int count)
    {
        Vector3 acc = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Vector3 delta = attractors[i].position - position;
            float distSq = delta.sqrMagnitude;
            float soften = attractors[i].radius * 0.35f;
            distSq += soften * soften;
            if (distSq < 1e-8f)
                continue;

            float invDist = 1f / Mathf.Sqrt(distSq);
            acc += attractors[i].gm * invDist * invDist * invDist * delta;
        }

        return acc;
    }

    public static void IntegrateVerlet(
        ref Vector3 position,
        ref Vector3 velocity,
        Attractor[] attractors,
        int count,
        float dt)
    {
        if (dt <= 0f || count <= 0)
            return;

        int steps = DefaultSubsteps;
        float step = dt / steps;
        for (int s = 0; s < steps; s++)
        {
            Vector3 a0 = Acceleration(position, attractors, count);
            position += velocity * step + 0.5f * a0 * step * step;
            Vector3 a1 = Acceleration(position, attractors, count);
            velocity += 0.5f * (a0 + a1) * step;
        }

        if (float.IsNaN(position.x) || float.IsNaN(velocity.x))
        {
            position = Vector3.zero;
            velocity = Vector3.zero;
        }
    }

    public static float CalibrateGmSun(Vector3 sunPosition, Vector3 earthPosition, Vector3 earthVelocity)
    {
        Vector3 r = earthPosition - sunPosition;
        r.y = 0f;
        float radius = r.magnitude;
        if (radius < 0.01f)
            return 1f;

        Vector3 tangential = earthVelocity;
        tangential.y = 0f;
        Vector3 radial = r.normalized;
        tangential -= Vector3.Dot(tangential, radial) * radial;
        float speed = tangential.magnitude;
        if (speed < 1e-5f)
            return radius * radius;

        return speed * speed * radius;
    }

    public static float HeliocentricEnergy(Vector3 position, Vector3 velocity, Vector3 sunPosition, float gmSun)
    {
        float r = Vector3.Distance(position, sunPosition);
        if (r < 1e-5f)
            return float.PositiveInfinity;

        return 0.5f * velocity.sqrMagnitude - gmSun / r;
    }
}
