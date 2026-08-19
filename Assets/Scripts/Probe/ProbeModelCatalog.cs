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
            public string DescriptionKey;
            public string PortraitResourcePath;
        }

        static readonly Entry[] DisplayOrder =
        {
            new Entry { Kind = ProbeModelKind.Voyager, LocalizationKey = "ProbeModelVoyager", DescriptionKey = "ProbeDescVoyager", PortraitResourcePath = "ProbePortraits/Voyager" },
            new Entry { Kind = ProbeModelKind.NewHorizons, LocalizationKey = "ProbeModelNewHorizons", DescriptionKey = "ProbeDescNewHorizons", PortraitResourcePath = "ProbePortraits/NewHorizons" },
            new Entry { Kind = ProbeModelKind.Juno, LocalizationKey = "ProbeModelJuno", DescriptionKey = "ProbeDescJuno", PortraitResourcePath = "ProbePortraits/Juno" },
            new Entry { Kind = ProbeModelKind.Luna1, LocalizationKey = "ProbeModelLuna1", DescriptionKey = "ProbeDescLuna1", PortraitResourcePath = "ProbePortraits/Luna1" },
            new Entry { Kind = ProbeModelKind.Venera7, LocalizationKey = "ProbeModelVenera7", DescriptionKey = "ProbeDescVenera7", PortraitResourcePath = "ProbePortraits/Venera7" },
            new Entry { Kind = ProbeModelKind.Luna16, LocalizationKey = "ProbeModelLuna16", DescriptionKey = "ProbeDescLuna16", PortraitResourcePath = "ProbePortraits/Luna16" },
            new Entry { Kind = ProbeModelKind.Mars3, LocalizationKey = "ProbeModelMars3", DescriptionKey = "ProbeDescMars3", PortraitResourcePath = "ProbePortraits/Mars3" },
            new Entry { Kind = ProbeModelKind.Change4, LocalizationKey = "ProbeModelChange4", DescriptionKey = "ProbeDescChange4", PortraitResourcePath = "ProbePortraits/Change4" },
            new Entry { Kind = ProbeModelKind.Tianwen1, LocalizationKey = "ProbeModelTianwen1", DescriptionKey = "ProbeDescTianwen1", PortraitResourcePath = "ProbePortraits/Tianwen1" },
            new Entry { Kind = ProbeModelKind.Change5, LocalizationKey = "ProbeModelChange5", DescriptionKey = "ProbeDescChange5", PortraitResourcePath = "ProbePortraits/Change5" },
            new Entry { Kind = ProbeModelKind.Hayabusa2, LocalizationKey = "ProbeModelHayabusa2", DescriptionKey = "ProbeDescHayabusa2", PortraitResourcePath = "ProbePortraits/Hayabusa2" },
            new Entry { Kind = ProbeModelKind.Akatsuki, LocalizationKey = "ProbeModelAkatsuki", DescriptionKey = "ProbeDescAkatsuki", PortraitResourcePath = "ProbePortraits/Akatsuki" },
            new Entry { Kind = ProbeModelKind.Chandrayaan3, LocalizationKey = "ProbeModelChandrayaan3", DescriptionKey = "ProbeDescChandrayaan3", PortraitResourcePath = "ProbePortraits/Chandrayaan3" },
            new Entry { Kind = ProbeModelKind.Custom, LocalizationKey = "ProbeModelCustom", DescriptionKey = "ProbeDescCustom", PortraitResourcePath = "ProbePortraits/Custom" },
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

        public static string GetDescriptionKey(ProbeModelKind kind)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].Kind == kind)
                    return DisplayOrder[i].DescriptionKey;
            }

            return "ProbeDescVoyager";
        }

        public static string GetPortraitResourcePath(ProbeModelKind kind)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].Kind == kind)
                    return DisplayOrder[i].PortraitResourcePath;
            }

            return "ProbePortraits/Voyager";
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
