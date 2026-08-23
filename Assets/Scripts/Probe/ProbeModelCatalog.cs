using System.Collections.Generic;

namespace SolarSystemApp
{
    /// <summary>
    /// Display order and localization keys for probe model picker.
    /// Custom stays enum value 3 for PlayerPrefs; shown last in UI when visible.
    /// </summary>
    public static class ProbeModelCatalog
    {
        public struct Entry
        {
            public ProbeModelKind Kind;
            public string LocalizationKey;
            public string DescriptionKey;
            public string PortraitResourcePath;
            public string OriginKey;
            /// <summary>When false, model stays in enum/assets but is hidden from the picker until re-enabled.</summary>
            public bool ShowInPicker;
        }

        static readonly Entry[] DisplayOrder =
        {
            new Entry { Kind = ProbeModelKind.Luna1, LocalizationKey = "ProbeModelLuna1", DescriptionKey = "ProbeDescLuna1", PortraitResourcePath = "ProbePortraits/Luna1", OriginKey = "ProbeOriginRussia", ShowInPicker = true },
            new Entry { Kind = ProbeModelKind.Mars3, LocalizationKey = "ProbeModelMars3", DescriptionKey = "ProbeDescMars3", PortraitResourcePath = "ProbePortraits/Mars3", OriginKey = "ProbeOriginRussia", ShowInPicker = true },
            new Entry { Kind = ProbeModelKind.Voyager, LocalizationKey = "ProbeModelVoyager", DescriptionKey = "ProbeDescVoyager", PortraitResourcePath = "ProbePortraits/Voyager", OriginKey = "ProbeOriginUSA", ShowInPicker = true },
            new Entry { Kind = ProbeModelKind.NewHorizons, LocalizationKey = "ProbeModelNewHorizons", DescriptionKey = "ProbeDescNewHorizons", PortraitResourcePath = "ProbePortraits/NewHorizons", OriginKey = "ProbeOriginUSA", ShowInPicker = true },
            new Entry { Kind = ProbeModelKind.Juno, LocalizationKey = "ProbeModelJuno", DescriptionKey = "ProbeDescJuno", PortraitResourcePath = "ProbePortraits/Juno", OriginKey = "ProbeOriginUSA", ShowInPicker = true },
            new Entry { Kind = ProbeModelKind.Venera7, LocalizationKey = "ProbeModelVenera7", DescriptionKey = "ProbeDescVenera7", PortraitResourcePath = "ProbePortraits/Venera7", OriginKey = "ProbeOriginRussia", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Luna16, LocalizationKey = "ProbeModelLuna16", DescriptionKey = "ProbeDescLuna16", PortraitResourcePath = "ProbePortraits/Luna16", OriginKey = "ProbeOriginRussia", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Change4, LocalizationKey = "ProbeModelChange4", DescriptionKey = "ProbeDescChange4", PortraitResourcePath = "ProbePortraits/Change4", OriginKey = "ProbeOriginChina", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Tianwen1, LocalizationKey = "ProbeModelTianwen1", DescriptionKey = "ProbeDescTianwen1", PortraitResourcePath = "ProbePortraits/Tianwen1", OriginKey = "ProbeOriginChina", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Change5, LocalizationKey = "ProbeModelChange5", DescriptionKey = "ProbeDescChange5", PortraitResourcePath = "ProbePortraits/Change5", OriginKey = "ProbeOriginChina", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Hayabusa2, LocalizationKey = "ProbeModelHayabusa2", DescriptionKey = "ProbeDescHayabusa2", PortraitResourcePath = "ProbePortraits/Hayabusa2", OriginKey = "ProbeOriginJapan", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Akatsuki, LocalizationKey = "ProbeModelAkatsuki", DescriptionKey = "ProbeDescAkatsuki", PortraitResourcePath = "ProbePortraits/Akatsuki", OriginKey = "ProbeOriginJapan", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Chandrayaan3, LocalizationKey = "ProbeModelChandrayaan3", DescriptionKey = "ProbeDescChandrayaan3", PortraitResourcePath = "ProbePortraits/Chandrayaan3", OriginKey = "ProbeOriginIndia", ShowInPicker = false },
            new Entry { Kind = ProbeModelKind.Custom, LocalizationKey = "ProbeModelCustom", DescriptionKey = "ProbeDescCustom", PortraitResourcePath = "ProbePortraits/Custom", OriginKey = null, ShowInPicker = false },
        };

        static readonly List<Entry> PickerList = BuildPickerList();

        static List<Entry> BuildPickerList()
        {
            var list = new List<Entry>(DisplayOrder.Length);
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].ShowInPicker)
                    list.Add(DisplayOrder[i]);
            }

            return list;
        }

        public static int Count => DisplayOrder.Length;

        public static IReadOnlyList<Entry> Entries => DisplayOrder;

        public static IReadOnlyList<Entry> PickerEntries => PickerList;

        public static ProbeModelKind GetDefaultPickerModel() => ProbeModelKind.Luna1;

        public static bool IsVisibleInPicker(ProbeModelKind kind)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].Kind == kind)
                    return DisplayOrder[i].ShowInPicker;
            }

            return false;
        }

        public static ProbeModelKind ClampToPicker(ProbeModelKind kind)
        {
            if (IsVisibleInPicker(kind))
                return kind;
            return GetDefaultPickerModel();
        }

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

        /// <summary>
        /// Localization keys for description pages: base, base+"2", base+"3".
        /// Empty/missing translations are filtered by the caller.
        /// </summary>
        public static string[] GetDescriptionPageKeys(ProbeModelKind kind)
        {
            string baseKey = GetDescriptionKey(kind);
            return new[] { baseKey, baseKey + "2", baseKey + "3" };
        }

        public static string GetOriginKey(ProbeModelKind kind)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                if (DisplayOrder[i].Kind == kind)
                    return DisplayOrder[i].OriginKey;
            }

            return "ProbeOriginUSA";
        }

        public static bool TryGetKindFromLocalizationKey(string key, out ProbeModelKind kind)
        {
            if (!string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < DisplayOrder.Length; i++)
                {
                    if (DisplayOrder[i].LocalizationKey == key)
                    {
                        kind = DisplayOrder[i].Kind;
                        return true;
                    }
                }
            }

            kind = default;
            return false;
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
            for (int i = 0; i < PickerList.Count; i++)
            {
                if (PickerList[i].Kind == kind)
                    return i;
            }

            return 0;
        }
    }
}
