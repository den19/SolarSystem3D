using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Canonical navigation order: Sun through Pluto with moons grouped after parents,
/// comets appended after Pluto sorted by semi-major axis.
/// </summary>
public static class BodyNavigationOrder
{
    public enum EntryKind
    {
        Planet,
        Comet
    }

    public struct NavigationEntry
    {
        public EntryKind kind;
        public string objectName;
        public string labelKey;
        public GameObject sceneObject;
    }

    static readonly string[] BaseBodyNames =
    {
        "Sun", "Mercury", "Venus", "Earth", "Moon", "Mars", "Phobos", "Deimos",
        "Jupiter", "Io", "Europa", "Ganymede", "Callisto", "Saturn", "Titan", "Uranus", "Neptune", "Triton", "Pluto"
    };

    const string CometInsertAfter = "Pluto";

    public static List<NavigationEntry> BuildNavigationList()
    {
        var entries = new List<NavigationEntry>();
        var cometBuffer = new List<NavigationEntry>();

        if (CometMovementSettings.UseCometMovement)
        {
            for (int i = 0; i < CometCatalog.Comets.Length; i++)
            {
                CometCatalog.CometDefinition definition = CometCatalog.Comets[i];
                GameObject cometGo = GameObject.Find(definition.objectName);
                if (cometGo == null || !cometGo.activeInHierarchy)
                    continue;

                cometBuffer.Add(new NavigationEntry
                {
                    kind = EntryKind.Comet,
                    objectName = definition.objectName,
                    labelKey = definition.labelKey,
                    sceneObject = cometGo
                });
            }

            cometBuffer.Sort((a, b) =>
            {
                CometCatalog.TryGetByObjectName(a.objectName, out CometCatalog.CometDefinition defA);
                CometCatalog.TryGetByObjectName(b.objectName, out CometCatalog.CometDefinition defB);
                return defA.semiMajorAxisAu.CompareTo(defB.semiMajorAxisAu);
            });
        }

        for (int i = 0; i < BaseBodyNames.Length; i++)
        {
            string bodyName = BaseBodyNames[i];
            GameObject bodyGo = GameObject.Find(bodyName);
            if (bodyGo == null)
                continue;

            entries.Add(new NavigationEntry
            {
                kind = EntryKind.Planet,
                objectName = bodyName,
                labelKey = bodyName + "Header",
                sceneObject = bodyGo
            });

            if (bodyName == CometInsertAfter && cometBuffer.Count > 0)
                entries.AddRange(cometBuffer);
        }

        return entries;
    }

    public static int FindIndexForObject(List<NavigationEntry> entries, GameObject sceneObject)
    {
        if (sceneObject == null || entries == null)
            return -1;

        string objectName = sceneObject.name;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].sceneObject == sceneObject)
                return i;

            if (entries[i].objectName == objectName)
                return i;
        }

        return -1;
    }

    public static int WrapIndex(int index, int count)
    {
        if (count <= 0)
            return -1;

        if (index < 0)
            return count - 1;

        if (index >= count)
            return 0;

        return index;
    }
}
