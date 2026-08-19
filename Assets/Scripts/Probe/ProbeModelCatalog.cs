using System.Collections.Generic;

namespace SolarSystemApp
{
    /// <summary>
    /// Display order and localization keys for probe model picker.
    /// Custom stays enum value 3 for PlayerPrefs; shown last in UI.
    /// </summary>
    public static class ProbeModelCatalog
    {
        public struct Entry
        {
            public ProbeModelKind Kind;
            public string LocalizationKey;
        }

        static readonly Entry[] DisplayOrder =
        {
            new Entry { Kind = ProbeModelKind.Voyager, LocalizationKey = "ProbeModelVoyager" },
            new Entry { Kind = ProbeModelKind.NewHorizons, LocalizationKey = "ProbeModelNewHorizons" },
            new Entry { Kind = ProbeModelKind.Juno, LocalizationKey = "ProbeModelJuno" },
            new Entry { Kind = ProbeModelKind.Luna1, LocalizationKey = "ProbeModelLuna1" },
            new Entry { Kind = ProbeModelKind.Venera7, LocalizationKey = "ProbeModelVenera7" },
            new Entry { Kind = ProbeModelKind.Luna16, LocalizationKey = "ProbeModelLuna16" },
            new Entry { Kind = ProbeModelKind.Mars3, LocalizationKey = "ProbeModelMars3" },
            new Entry { Kind = ProbeModelKind.Change4, LocalizationKey = "ProbeModelChange4" },
            new Entry { Kind = ProbeModelKind.Tianwen1, LocalizationKey = "ProbeModelTianwen1" },
            new Entry { Kind = ProbeModelKind.Change5, LocalizationKey = "ProbeModelChange5" },
            new Entry { Kind = ProbeModelKind.Hayabusa2, LocalizationKey = "ProbeModelHayabusa2" },
            new Entry { Kind = ProbeModelKind.Akatsuki, LocalizationKey = "ProbeModelAkatsuki" },
            new Entry { Kind = ProbeModelKind.Chandrayaan3, LocalizationKey = "ProbeModelChandrayaan3" },
            new Entry { Kind = ProbeModelKind.Custom, LocalizationKey = "ProbeModelCustom" },
        };

        public static int Count => DisplayOrder.Length;

        public static IReadOnlyList<Entry> Entries => DisplayOrder;

        public static string GetLocalizationKey(ProbeModelKind kind)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].Kind == kind)
                    return DisplayOrder[i].LocalizationKey;
            }

            return "ProbeModelVoyager";
        }

        public static int GetDisplayIndex(ProbeModelKind kind)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].Kind == kind)
                    return i;
            }

            return 0;
        }
    }
}
