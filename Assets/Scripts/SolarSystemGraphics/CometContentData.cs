using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multilingual encyclopedia text and per-comet visual tuning for comet prefabs.
/// </summary>
public static class CometContentData
{
    static readonly Color GreenComa = new Color(0.55f, 0.92f, 0.78f, 1f);
    static readonly Color BlueGreenComa = new Color(0.45f, 0.88f, 0.85f, 1f);
    static readonly Color DustyComa = new Color(0.85f, 0.88f, 0.82f, 1f);
    static readonly Color PaleGreenComa = new Color(0.58f, 0.88f, 0.72f, 1f);
    static readonly Color BrightIceComa = new Color(0.88f, 0.9f, 0.85f, 1f);
    static readonly Color DustyGreenComa = new Color(0.75f, 0.88f, 0.72f, 1f);

    public struct ContentEntry
    {
        public string id;
        public Vector3 nucleusScale;
        public string nucleusMaterialVariant;
        public float activityStrength;
        public Color comaColor;
        public float comaMaxScale;
        public float dormantThresholdAu;
        public string english;
        public string russian;
        public string chinese;
        public string vietnamese;
        public string uzbek;
    }

    static Dictionary<string, ContentEntry> _byId;

    public static ContentEntry Get(string cometId)
    {
        EnsureLookup();
        if (_byId.TryGetValue(cometId, out ContentEntry entry))
            return entry;
        return default;
    }

    static void EnsureLookup()
    {
        if (_byId != null)
            return;

        var map = new Dictionary<string, ContentEntry>();
        foreach (ContentEntry entry in All)
            map[entry.id] = entry;
        _byId = map;
    }

    public static readonly ContentEntry[] All =
    {
        Entry(
            "Comet_Encke",
            new Vector3(0.35f, 0.25f, 0.55f),
            "dusty",
            activityStrength: 1.5f,
            comaColor: GreenComa,
            comaMaxScale: 1.8f,
            dormantThresholdAu: 4f,
            en: "2P/Encke is a short-period comet with one of the shortest known orbital periods (about 3.3 years). Its nucleus is a dark, elongated body roughly 4.8 km across — darker than charcoal — made of rock, dust, and frozen gases.\n\nNear the Sun, sublimating ices form a pale greenish coma that can grow larger than a planet. Two tails appear: a curved yellow-white dust tail along the orbit, and a straight bluish ion tail pointing away from the Sun.\n\nFar beyond Jupiter it is only a faint smudge with no tail. Approaching perihelion it brightens dramatically.",
            ru: "2P/Энке — короткопериодическая комета с одним из самых коротких известных периодов (около 3,3 года). Её ядро — тёмное вытянутое тело длиной примерно 4,8 км, темнее угля, состоящее из пород, пыли и замороженных газов.\n\nУ Солнца испаряющийся лёд образует бледно-зеленоватую кому, способную превышать размер планеты. Появляются два хвоста: изогнутый жёлто-белый пылевой вдоль орбиты и прямой голубоватый ионный, направленный от Солнца.\n\nЗа орбитой Юпитера комета видна лишь как слабое пятнышко без хвоста. У перигелия яркость резко возрастает.",
            zh: "2P/恩克是一颗短周期彗星，公转周期约3.3年，是周期最短的彗星之一。其核约为4.8千米长的暗色拉长天体，比木炭还暗，由岩石、尘埃和冻结气体组成。\n\n接近太阳时，升华的冰形成淡绿色彗发，可比行星还大。会出现两条彗尾：沿轨道弯曲的黄白色尘埃尾，以及背离太阳的蓝白色离子尾。\n\n在木星轨道之外仅为暗淡无尾的光点；过近日点时显著增亮。",
            vi: "2P/Encke là sao chổi chu kỳ ngắn (khoảng 3,3 năm). Hạt nhân dài khoảng 4,8 km, tối hơn than củi, gồm đá, bụi và khí đông lạnh.\n\nGần Mặt Trời, các khí băng hình thành coma xanh nhạt và hai đuôi: bụi vàng cong theo quỹ đạo, ion xanh thẳng hướng ra xa Mặt Trời.\n\nXa hơn quỹ đạo Sao Mộc chỉ là đốm mờ không đuôi; gần điểm cận nhật sáng mạnh.",
            uz: "2P/Encke — qisqa davrli komet (taxminan 3,3 yil). Yadrosi taxminan 4,8 km uzunlikdagi qorongʻi choʻzilgan jism — koʻmirdek qora, tosh, chang va muzli gazlardan iborat.\n\nQuyosh yaqinida sublimatsiya komasi va ikki dum paydo boʻladi: sariq oq chang dum va toʻgʻri koʻk ion dum.\n\nYupiter ortida faqat xira dogʻ, perigeliyda yorqinlashadi."),
        Entry(
            "Comet_Honda",
            new Vector3(0.38f, 0.28f, 0.52f),
            "ice",
            activityStrength: 1.2f,
            comaColor: BlueGreenComa,
            comaMaxScale: 1.6f,
            dormantThresholdAu: 4f,
            en: "45P/Honda–Mrkos–Pajdušáková is a Jupiter-family comet with a period near 5.3 years. Its nucleus is a small dark icy body, typically a few kilometres across.\n\nWhen solar heat awakens the comet, a diffuse coma glows greenish-blue from fluorescing gases. A dust tail curves behind along the path; an ion tail streams antisunward in pale blue.\n\nDistant from the Sun it appears as a faint stellar point. Activity rises sharply near perihelion.",
            ru: "45P/Хонда—Мркос—Пайдушакова — комета семейства Юпитера с периодом около 5,3 года. Ядро — небольшое тёмное ледяное тело длиной несколько километров.\n\nПодогрев Солнцем вызывает размытую зеленовато-голубую кому. Пылевой хвост изгибается вдоль траектории; ионный — тянется от Солнца голубой струёй.\n\nВдали от Солнца — слабая звёздная точка. У перигелия активность резко растёт.",
            zh: "45P/本田—马尔科斯—帕杜什科娃是木星族彗星，周期约5.3年。核为几千米长的暗色冰体。\n\n受太阳加热形成青绿色彗发；尘埃尾沿轨道弯曲，离子尾呈淡蓝色背离太阳。\n\n远离太阳时仅为微弱光点；过近日点活动剧增。",
            vi: "45P/Honda–Mrkos–Pajdušáková thuộc họ Sao Mộc, chu kỳ ~5,3 năm. Hạt nhân băng tối vài km.\n\nComa xanh lục, đuôi bụi cong và đuôi ion xanh hướng xa Mặt Trời.\n\nXa Mặt Trời chỉ là chấm mờ; gần cận nhật rất sáng.",
            uz: "45P/Honda–Mrkos–Pajdušáková — Yupiter oilasidagi komet, davri ~5,3 yil. Bir necha kilometrli qorongʻi muzli yadro.\n\nYashil-koʻk koma va ikki dum: chang va ion.\n\nUzoqda xira nuqta; perigeliyda faollashadi."),
        Entry(
            "Comet_TGK",
            new Vector3(0.4f, 0.3f, 0.48f),
            "dusty",
            activityStrength: 1.1f,
            comaColor: GreenComa,
            comaMaxScale: 1.5f,
            dormantThresholdAu: 4f,
            en: "41P/Tuttle–Giacobini–Kresák is a compact Jupiter-family comet orbiting about every 5.4 years. Its nucleus is irregular and dark, rich in dust.\n\nNear the Sun the coma expands into a soft glowing halo. Dust forms a broad curved tail; ionized gas produces a narrow blue ray aimed away from the Sun.\n\nBeyond the outer planets only the nucleus is detectable as a dim dot.",
            ru: "41P/TGK (Туттль—Джиакобини—Кресак) — компактная комета семейства Юпитера с периодом около 5,4 года. Ядро неправильной формы, тёмное, богатое пылью.\n\nУ Солнца кома расширяется в мягкое свечение. Пыль образует широкий изогнутый хвост; ионизированный газ — узкий голубой луч от Солнца.\n\nЗа внешними планетами видна лишь тусклая точка ядра.",
            zh: "41P/TGK是紧凑的木星族彗星，周期约5.4年。核不规则、富含尘埃。\n\n近太阳时彗发呈柔和光晕；尘埃尾宽而弯曲，离子气体形成背离太阳的蓝色细束。\n\n在外行星之外仅为暗淡光点。",
            vi: "41P/TGK là sao chổi nhỏ gọn, chu kỳ ~5,4 năm, hạt nhân giàu bụi.\n\nComa phát sáng mềm; đuôi bụi rộng và tia ion xanh.\n\nXa các hành tinh ngoài chỉ là chấm mờ.",
            uz: "41P/TGK — ixcham Yupiter kometasi, ~5,4 yil. Changga boy yadro.\n\nQuyosh yaqinida yumshoq koma va keng chang dum.\n\nTashqi sayyoralar ortida xira nuqta."),
        Entry(
            "Comet_Wild2",
            new Vector3(0.42f, 0.38f, 0.42f),
            "ice",
            activityStrength: 0.65f,
            comaColor: BrightIceComa,
            comaMaxScale: 1.3f,
            dormantThresholdAu: 3.2f,
            en: "81P/Wild 2 is famous for its rugged, nearly spherical nucleus about 4 km wide, visited by NASA's Stardust mission. The surface is dark, cratered, and icy.\n\nSolar heating drives a bright coma and dual tails: yellow dust swept along the orbit and a straight ion tail pointing from the Sun.\n\nFar from the Sun Wild 2 is a faint moving star with no visible coma or tail.",
            ru: "81P/Wild 2 известна неровным почти сферическим ядром около 4 км — к ней летала миссия Stardust. Поверхность тёмная, кратерная, ледяная.\n\nСолнечный нагрев создаёт яркую кому и два хвоста: жёлтый пылевой и прямой ионный от Солнца.\n\nВдали от Солнца — слабая движущаяся звезда без комы и хвоста.",
            zh: "81P/维尔德2号以约4千米、近乎球形的崎岖核闻名，曾由Stardust探测器取样。表面暗、多坑、含冰。\n\n近太阳形成亮彗发与双尾；远太阳仅为无彗发的暗弱移动星点。",
            vi: "81P/Wild 2 có hạt nhân gần cầu ~4 km, từng được Stardust thăm dò.\n\nComa sáng và hai đuôi gần Mặt Trời; xa chỉ là sao mờ di chuyển.",
            uz: "81P/Wild 2 — taxminan 4 km deyarli sferik yadro, Stardust missiyasi tashrifi.\n\nQuyosh yaqinida yorqin koma va ikki dum; uzoqda xira harakatlanuvchi yulduz."),
        Entry(
            "Comet_Kopff",
            new Vector3(0.36f, 0.3f, 0.5f),
            "dusty",
            activityStrength: 0.85f,
            comaColor: DustyGreenComa,
            comaMaxScale: 1.4f,
            dormantThresholdAu: 4f,
            en: "22P/Kopff is a dusty short-period comet with a period near 6.4 years. Its nucleus is elongated and very dark, reflecting little sunlight.\n\nApproaching the Sun, a greenish coma swells and a dust tail arcs behind. An ion tail extends antisunward in pale violet-blue.\n\nIn the cold outer solar system Kopff remains a barely visible speck.",
            ru: "22P/Копфф — пылевая короткопериодическая комета с периодом около 6,4 года. Ядро вытянутое, очень тёмное, почти не отражает свет.\n\nПри сближении с Солнцем зеленоватая кома разрастается, пылевой хвост дугой тянется сзади, ионный — голубовато-фиолетовый — от Солнца.\n\nВ холодной внешней Солнечной системе — едва заметное пятнышко.",
            zh: "22P/科普夫是周期约6.4年的尘埃彗星，核细长极暗。\n\n近太阳时绿色彗发膨胀，尘埃尾成弧，离子尾呈淡紫蓝色背离太阳。\n\n在外太阳系仅为勉强可见的暗点。",
            vi: "22P/Kopff là sao chổi bụi, chu kỳ ~6,4 năm, hạt nhân tối và dài.\n\nComa xanh và hai đuôi gần Mặt Trời; ngoài hệ nội mờ nhạt.",
            uz: "22P/Kopff — changli komet, ~6,4 yil, choʻzilgan qorongʻi yadro.\n\nYashil koma va ikki dum yaqinida; tashqarida deyarli koʻrinmas."),
        Entry(
            "Comet_GriggSkjellerup",
            new Vector3(0.34f, 0.26f, 0.58f),
            "dusty",
            activityStrength: 0.55f,
            comaColor: DustyComa,
            comaMaxScale: 1.2f,
            dormantThresholdAu: 4f,
            en: "26P/Grigg–Skjellerup is a faint periodic comet with a period near 5.1 years and a steeply inclined orbit. Its small nucleus is dark and elongated.\n\nEven at moderate solar distances a modest coma and short dust tail may appear. The ion tail is thin and blue, always oriented away from the Sun.\n\nFar from the Sun it is one of the faintest periodic comets in the catalog.",
            ru: "26P/Григг—Скьеллеруп — слабая периодическая комета с периодом около 5,1 года и сильно наклонённой орбитой. Маленькое ядро тёмное и вытянутое.\n\nУмеренная солнечная дистанция даёт скромную кому и короткий пылевой хвост. Ионный хвост тонкий, голубой, направлен от Солнца.\n\nВдали от Солнца — одна из самых тусклых комет каталога.",
            zh: "26P/格里格—斯基勒鲁普周期约5.1年，轨道倾角大，核小而暗长。\n\n中等日距可出现弱彗发与短尘尾；离子尾细而蓝。\n\n远太阳时极为暗淡。",
            vi: "26P/Grigg–Skjellerup chu kỳ ~5,1 năm, quỹ đạo nghiêng, hạt nhân nhỏ tối.\n\nComa và đuôi ngắn vừa phải; đuôi ion mỏng xanh.\n\nRất mờ khi xa Mặt Trời.",
            uz: "26P/Grigg–Skjellerup — ~5,1 yil, qiya orbita, kichik qorongʻi yadro.\n\nOʻrtacha masofada zaif koma; uzoqda juda xira."),
        Entry(
            "Comet_DArrest",
            new Vector3(0.37f, 0.29f, 0.51f),
            "ice",
            activityStrength: 0.9f,
            comaColor: GreenComa,
            comaMaxScale: 1.5f,
            dormantThresholdAu: 4f,
            en: "6P/d'Arrest is a classic short-period comet (about 6.5 years) named after its discoverer. The nucleus is a dark icy mass a few kilometres long.\n\nSolar ultraviolet drives gas outflows that glow in the coma. Dust trails curve along the orbit; ionized carbon monoxide forms a straight bluish tail.\n\nBeyond Jupiter d'Arrest is a faint stellar point without tails.",
            ru: "6P/d'Arrest — классическая короткопериодическая комета (около 6,5 лет). Ядро — тёмная ледяная глыба длиной несколько километров.\n\nУльтрафиолет Солнца разгоняет газы в светящейся коме. Пыль изгибается вдоль орбиты; ионизированный CO образует прямой голубой хвост.\n\nЗа Юпитером — слабая звёздная точка без хвостов.",
            zh: "6P/d'Arrest是经典短周期彗星，周期约6.5年。核为几千米长的暗冰体。\n\n紫外辐射驱动彗发；尘埃沿轨道弯曲，电离气体形成蓝色直尾。\n\n木星外为无尾暗弱星点。",
            vi: "6P/d'Arrest chu kỳ ~6,5 năm, hạt nhân băng tối vài km.\n\nTia UV tạo coma; đuôi bụi cong, ion thẳng màu xanh.\n\nNgoài Sao Mộc không có đuôi.",
            uz: "6P/d'Arrest — klassik qisqa davrli komet, ~6,5 yil. Bir necha km muzli yadro.\n\nUV komani kuchaytiradi; chang dum egri, ion dum toʻgʻri.\n\nYupiter ortida dumsiz xira nuqta."),
        Entry(
            "Comet_Wirtanen",
            new Vector3(0.4f, 0.36f, 0.44f),
            "ice",
            activityStrength: 1.4f,
            comaColor: GreenComa,
            comaMaxScale: 2.2f,
            dormantThresholdAu: 4f,
            en: "46P/Wirtanen is a small hyperactive comet that passes unusually close to Earth at times. Its nucleus is about 1.2 km across — bright for its size.\n\nNear perihelion the coma can grow very large relative to the nucleus. Dust and ion tails become clearly visible even in modest telescopes.\n\nFar from the Sun Wirtanen is a compact faint smudge.",
            ru: "46P/Виртанен — маленькая «гиперактивная» комета, иногда проходящая необычно близко к Земле. Ядро около 1,2 км — яркое для своего размера.\n\nУ перигелия кома может быть огромной относительно ядра. Пылевой и ионный хвосты видны даже в скромные телескопы.\n\nВдали от Солнца — компактное тусклое пятно.",
            zh: "46P/维尔特宁是小型高活跃彗星，有时非常接近地球。核约1.2千米，相对明亮。\n\n过近日点彗发可远大于核；双尾在中小望远镜中可见。\n\n远太阳为紧凑暗斑。",
            vi: "46P/Wirtanen là sao chổi nhỏ rất hoạt động, đôi khi rất gần Trái Đất. Hạt nhân ~1,2 km.\n\nComa lớn gần cận nhật; hai đuôi dễ thấy.\n\nXa Mặt Trời là vệt mờ nhỏ.",
            uz: "46P/Wirtanen — kichik giperfaol komet, baʼzan Yerga juda yaqin. Yadro ~1,2 km.\n\nPerigeliyda katta koma va aniq ikki dum.\n\nUzoqda ixcham xira dogʻ."),
        Entry(
            "Comet_Borrelly",
            new Vector3(0.33f, 0.24f, 0.6f),
            "dusty",
            activityStrength: 1f,
            comaColor: DustyGreenComa,
            comaMaxScale: 1.6f,
            dormantThresholdAu: 4f,
            en: "19P/Borrelly was imaged closely by Deep Space 1. Its nucleus is highly elongated (about 8×4×4 km) and extremely dark.\n\nNear the Sun jets of dust and gas build a bright coma and dual tails. The dust tail curves; the ion tail points rigidly antisunward.\n\nIn the outer solar system Borrelly shows only a faint nucleus with no tail.",
            ru: "19P/Боррелли снималась с близкого расстояния аппаратом Deep Space 1. Ядро сильно вытянуто (около 8×4×4 км) и крайне тёмно.\n\nУ Солнца струи пыли и газа создают яркую кому и два хвоста. Пылевой изогнут; ионный строго от Солнца.\n\nВо внешней системе — лишь тусклое ядро без хвоста.",
            zh: "19P/博雷利由Deep Space 1近距离拍摄。核极度拉长（约8×4×4千米）且极暗。\n\n近太阳喷流形成亮彗发与双尾；远外太阳系仅见暗淡核。",
            vi: "19P/Borrelly được Deep Space 1 chụp gần. Hạt nhân dài ~8×4×4 km, rất tối.\n\nGần Mặt Trời có coma sáng và hai đuôi; xa chỉ còn hạt nhân mờ.",
            uz: "19P/Borrelly — Deep Space 1 yaqin surati. 8×4×4 km choʻzilgan qorongʻi yadro.\n\nQuyosh yaqinida yorqin koma; uzoqda dumsiz xira yadro."),
        Entry(
            "Comet_Howell",
            new Vector3(0.39f, 0.32f, 0.47f),
            "ice",
            activityStrength: 0.6f,
            comaColor: PaleGreenComa,
            comaMaxScale: 1.3f,
            dormantThresholdAu: 3.2f,
            en: "88P/Howell is a medium-period comet orbiting every 5.5 years. Its nucleus is a dark icy body a few kilometres across with moderate activity.\n\nThe coma glows pale green near the Sun; dust forms a gentle curved tail while ions stream away in a blue lance.\n\nBeyond Jupiter Howell is visible only as a faint moving point.",
            ru: "88P/Howell — комета со средним периодом около 5,5 лет. Ядро — тёмное ледяное тело несколько километров с умеренной активностью.\n\nКома бледно-зелёная у Солнца; пыль образует плавный изогнутый хвост, ионы — голубое копьё от Солнца.\n\nЗа Юпитером видна лишь как слабая движущаяся точка.",
            zh: "88P/豪威尔周期约5.5年，核为几千米暗冰体，活动中等。\n\n近太阳淡绿色彗发；尘埃尾弯曲，离子尾呈蓝色长束。\n\n木星外仅为微弱移动点。",
            vi: "88P/Howell chu kỳ ~5,5 năm, hạt nhân băng vài km.\n\nComa xanh nhạt; đuôi bụi cong, ion xanh.\n\nNgoài Sao Mộc chỉ là điểm mờ.",
            uz: "88P/Howell — ~5,5 yil, bir necha km muzli yadro.\n\nOch yashil koma; egri chang va koʻk ion dum.\n\nYupiter ortida xira harakatlanuvchi nuqta.")
    };

    static ContentEntry Entry(string id, Vector3 scale, string matVariant,
        float activityStrength, Color comaColor, float comaMaxScale, float dormantThresholdAu,
        string en, string ru, string zh, string vi, string uz)
    {
        return new ContentEntry
        {
            id = id,
            nucleusScale = scale,
            nucleusMaterialVariant = matVariant,
            activityStrength = activityStrength,
            comaColor = comaColor,
            comaMaxScale = comaMaxScale,
            dormantThresholdAu = dormantThresholdAu,
            english = en,
            russian = ru,
            chinese = zh,
            vietnamese = vi,
            uzbek = uz
        };
    }
}
