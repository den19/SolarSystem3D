using UnityEngine;

/// <summary>
/// Shared comet VFX materials loaded from Resources/CometGraphics.
/// </summary>
public static class CometGraphicsLibrary
{
    const string Root = "CometGraphics/";

    static Material _coma;
    static Material _dustTrail;
    static Material _ion;

    public static Material LoadComaMaterial()
    {
        if (_coma == null)
        {
            _coma = Resources.Load<Material>(Root + "CometComa");
            if (_coma == null)
                _coma = CreateRuntimeComaMaterial();
        }

        return _coma;
    }

    public static Material LoadDustTrailMaterial()
    {
        if (_dustTrail == null)
        {
            _dustTrail = Resources.Load<Material>(Root + "CometDustTrail");
            if (_dustTrail == null)
                _dustTrail = CreateRuntimeParticleMaterial(new Color(1f, 0.95f, 0.7f, 0.9f));
        }

        return _dustTrail;
    }

    public static Material LoadIonMaterial()
    {
        if (_ion == null)
        {
            _ion = Resources.Load<Material>(Root + "CometIonTailParticle");
            if (_ion == null)
                _ion = CreateRuntimeIonMaterial();
        }

        return _ion;
    }

    public static Material LoadNucleusMaterial(string variant)
    {
        string path = variant switch
        {
            "ice" => Root + "CometNucleus_Ice",
            "dusty" => Root + "CometNucleus_Dusty",
            _ => Root + "CometNucleus_Dark"
        };

        var mat = Resources.Load<Material>(path);
        if (mat != null)
            return mat;

        return Resources.Load<Material>(Root + "CometNucleus_Dark");
    }

    static Material CreateRuntimeComaMaterial()
    {
        var shader = Shader.Find("Custom/AtmosphereRim") ?? Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        mat.SetColor("_AtmosphereColor", new Color(0.55f, 0.92f, 0.78f, 0.28f));
        mat.SetFloat("_RimPower", 3.2f);
        mat.SetFloat("_SunInfluence", 0.75f);
        return mat;
    }

    static Material CreateRuntimeParticleMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        mat.renderQueue = 3000;
        return mat;
    }

    static Material CreateRuntimeIonMaterial()
    {
        var mat = CreateRuntimeParticleMaterial(new Color(0.4f, 0.78f, 1f, 1f));
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 1f);
        return mat;
    }
}
