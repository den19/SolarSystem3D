using UnityEngine;

/// <summary>
/// Static catalog of the five shortest-period periodic comets used in clean view mode.
/// </summary>
public static class CometCatalog
{
    public struct CometDefinition
    {
        public string objectName;
        public string labelKey;
        public float semiMajorAxis;
        public float eccentricity;
        public float inclinationDeg;
        public float simPeriodSec;
        public float phaseOffsetRad;
    }

    public static readonly CometDefinition[] Comets =
    {
        new CometDefinition
        {
            objectName = "Comet_Encke",
            labelKey = "CometEnckeLabel",
            semiMajorAxis = 115f,
            eccentricity = 0.85f,
            inclinationDeg = 11.8f,
            simPeriodSec = 18f,
            phaseOffsetRad = 0.2f
        },
        new CometDefinition
        {
            objectName = "Comet_Honda",
            labelKey = "CometHondaMrkosPajdusakovaLabel",
            semiMajorAxis = 125f,
            eccentricity = 0.83f,
            inclinationDeg = 12.9f,
            simPeriodSec = 24f,
            phaseOffsetRad = 1.4f
        },
        new CometDefinition
        {
            objectName = "Comet_TGK",
            labelKey = "CometTgkLabel",
            semiMajorAxis = 130f,
            eccentricity = 0.82f,
            inclinationDeg = 9.2f,
            simPeriodSec = 26f,
            phaseOffsetRad = 2.6f
        },
        new CometDefinition
        {
            objectName = "Comet_Wild2",
            labelKey = "CometWild2Label",
            semiMajorAxis = 145f,
            eccentricity = 0.54f,
            inclinationDeg = 3.2f,
            simPeriodSec = 32f,
            phaseOffsetRad = 3.8f
        },
        new CometDefinition
        {
            objectName = "Comet_Kopff",
            labelKey = "CometKopffLabel",
            semiMajorAxis = 150f,
            eccentricity = 0.59f,
            inclinationDeg = 4.4f,
            simPeriodSec = 34f,
            phaseOffsetRad = 4.9f
        }
    };
}
