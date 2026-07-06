using UnityEngine;

/// <summary>
/// Static catalog of the ten shortest-period periodic comets used in clean view mode.
/// </summary>
public static class CometCatalog
{
    public struct CometDefinition
    {
        public string objectName;
        public string labelKey;
        public string prefabResourcePath;
        public float semiMajorAxis;
        public float semiMajorAxisAu;
        public float eccentricity;
        public float inclinationDeg;
        public float simPeriodSec;
        public float phaseOffsetRad;
    }

    public static bool TryGetByObjectName(string objectName, out CometDefinition definition)
    {
        for (int i = 0; i < Comets.Length; i++)
        {
            if (Comets[i].objectName == objectName)
            {
                definition = Comets[i];
                return true;
            }
        }

        definition = default;
        return false;
    }

    public static readonly CometDefinition[] Comets =
    {
        new CometDefinition
        {
            objectName = "Comet_Encke",
            labelKey = "CometEnckeLabel",
            prefabResourcePath = "Comets/Comet_Encke",
            semiMajorAxis = 115f,
            semiMajorAxisAu = 2.21f,
            eccentricity = 0.85f,
            inclinationDeg = 11.8f,
            simPeriodSec = 18f,
            phaseOffsetRad = 0.2f
        },
        new CometDefinition
        {
            objectName = "Comet_Honda",
            labelKey = "CometHondaMrkosPajdusakovaLabel",
            prefabResourcePath = "Comets/Comet_Honda",
            semiMajorAxis = 125f,
            semiMajorAxisAu = 2.58f,
            eccentricity = 0.83f,
            inclinationDeg = 12.9f,
            simPeriodSec = 24f,
            phaseOffsetRad = 1.4f
        },
        new CometDefinition
        {
            objectName = "Comet_TGK",
            labelKey = "CometTgkLabel",
            prefabResourcePath = "Comets/Comet_TGK",
            semiMajorAxis = 130f,
            semiMajorAxisAu = 2.75f,
            eccentricity = 0.82f,
            inclinationDeg = 9.2f,
            simPeriodSec = 26f,
            phaseOffsetRad = 2.6f
        },
        new CometDefinition
        {
            objectName = "Comet_Wild2",
            labelKey = "CometWild2Label",
            prefabResourcePath = "Comets/Comet_Wild2",
            semiMajorAxis = 145f,
            semiMajorAxisAu = 3.45f,
            eccentricity = 0.54f,
            inclinationDeg = 3.2f,
            simPeriodSec = 32f,
            phaseOffsetRad = 3.8f
        },
        new CometDefinition
        {
            objectName = "Comet_Kopff",
            labelKey = "CometKopffLabel",
            prefabResourcePath = "Comets/Comet_Kopff",
            semiMajorAxis = 150f,
            semiMajorAxisAu = 3.65f,
            eccentricity = 0.59f,
            inclinationDeg = 4.4f,
            simPeriodSec = 34f,
            phaseOffsetRad = 4.9f
        },
        new CometDefinition
        {
            objectName = "Comet_GriggSkjellerup",
            labelKey = "CometGriggSkjellerupLabel",
            prefabResourcePath = "Comets/Comet_GriggSkjellerup",
            semiMajorAxis = 105f,
            semiMajorAxisAu = 2.54f,
            eccentricity = 0.65f,
            inclinationDeg = 22.2f,
            simPeriodSec = 22f,
            phaseOffsetRad = 5.5f
        },
        new CometDefinition
        {
            objectName = "Comet_DArrest",
            labelKey = "CometDArrestLabel",
            prefabResourcePath = "Comets/Comet_DArrest",
            semiMajorAxis = 135f,
            semiMajorAxisAu = 3.49f,
            eccentricity = 0.61f,
            inclinationDeg = 10.5f,
            simPeriodSec = 28f,
            phaseOffsetRad = 0.8f
        },
        new CometDefinition
        {
            objectName = "Comet_Wirtanen",
            labelKey = "CometWirtanenLabel",
            prefabResourcePath = "Comets/Comet_Wirtanen",
            semiMajorAxis = 140f,
            semiMajorAxisAu = 3.09f,
            eccentricity = 0.41f,
            inclinationDeg = 11.4f,
            simPeriodSec = 30f,
            phaseOffsetRad = 2.0f
        },
        new CometDefinition
        {
            objectName = "Comet_Borrelly",
            labelKey = "CometBorrellyLabel",
            prefabResourcePath = "Comets/Comet_Borrelly",
            semiMajorAxis = 155f,
            semiMajorAxisAu = 3.61f,
            eccentricity = 0.62f,
            inclinationDeg = 30.3f,
            simPeriodSec = 33f,
            phaseOffsetRad = 3.2f
        },
        new CometDefinition
        {
            objectName = "Comet_Howell",
            labelKey = "CometHowellLabel",
            prefabResourcePath = "Comets/Comet_Howell",
            semiMajorAxis = 152f,
            semiMajorAxisAu = 3.54f,
            eccentricity = 0.49f,
            inclinationDeg = 4.8f,
            simPeriodSec = 31f,
            phaseOffsetRad = 4.1f
        }
    };
}
