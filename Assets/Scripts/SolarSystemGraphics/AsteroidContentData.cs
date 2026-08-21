using UnityEngine;

/// <summary>
/// Multilingual encyclopedia text for named asteroids.
/// </summary>
public static class AsteroidContentData
{
    public struct ContentEntry
    {
        public string id;
        public string english;
        public string russian;
        public string chinese;
        public string vietnamese;
        public string uzbek;
        public string tatar;
        public string belarusian;
    }

    static readonly ContentEntry[] Entries =
    {
        Entry(
            "Asteroid_Ceres",
            "Ceres is the largest object in the asteroid belt and the only dwarf planet in the inner Solar System. Discovered in 1801, it has a rocky–icy composition and bright salt deposits mapped by NASA's Dawn mission.",
            "Церера — крупнейший объект пояса астероидов и единственная карликовая планета во внутренней Солнечной системе. Открыта в 1801 году; миссия Dawn показала соляные отложения на поверхности.",
            "谷神星是小行星带中最大的天体，也是内太阳系唯一的矮行星。1801年发现；黎明号探测到地表盐沉积。",
            "Ceres là thiên thể lớn nhất vành đai tiểu hành tinh và hành tinh lùn duy nhất ở hệ Mặt Trời trong. Phát hiện năm 1801; Dawn đã lập bản đồ muối trên bề mặt.",
            "Serera — asteroid kamaragini eng yirik jismi va ichki Quyosh sistemasidagi yagona mitti sayyora. 1801-yilda ochilgan; Dawn missiyasi tuz konlarini ko‘rsatdi.",
            "Церера — астероидлар билбавындагы иң зур объект һәм эчке Кояш системасындагы бердәнбер кәрлә планета. 1801 елда ачылган.",
            "Цэрэра — найбуйнейшы аб'ект пояса астэроідаў і адзіная карлікавая планета ўнутранай Сонечнай сістэмы. Адкрыта ў 1801 годзе."),
        Entry(
            "Asteroid_Vesta",
            "Vesta is the second-most massive body in the asteroid belt. Dawn revealed a differentiated interior, a giant southern impact basin (Rheasilvia), and basaltic crust — a protoplanet that almost became a planet.",
            "Веста — второе по массе тело пояса астероидов. Dawn показал дифференцированное строение, гигантский бассейн Rheasilvia и базальтовую кору — протопланету, почти ставшую планетой.",
            "灶神星是小行星带中质量第二大的天体。黎明号显示其已分化，南部有巨大撞击盆地，玄武岩壳——几乎成为行星的原行星。",
            "Vesta là thiên thể khối lượng lớn thứ hai vành đai. Dawn cho thấy nội thất phân dị, lưu vực Rheasilvia và vỏ bazan — một tiền hành tinh.",
            "Vesta — asteroid kamaragida massasi bo‘yicha ikkinchi jism. Dawn differensiyalangan tuzilma va Rheasilvia havzasini ko‘rsatdi.",
            "Веста — астероидлар билбавында масса буенча икенче җисем. Dawn дифференциацияләнгән төзелешне күрсәтте.",
            "Веста — другое па масе цела пояса астэроідаў. Dawn паказаў дыферэнцыяванае ўнутранае і басейн Rheasilvia."),
        Entry(
            "Asteroid_Pallas",
            "Pallas is one of the largest asteroids and has an unusually high orbital inclination (~35°). Its orbit and spectrum make it a distinctive member of the main belt.",
            "Паллада — один из крупнейших астероидов с необычно большим наклонением орбиты (~35°). По орбите и спектру это заметный объект главного пояса.",
            "智神星是最大的小行星之一，轨道倾角异常高（约35°），是主带中独特的成员。",
            "Pallas là một trong những tiểu hành tinh lớn nhất, độ nghiêng quỹ đạo cao (~35°) — thành viên đặc biệt của vành đai chính.",
            "Pallas — eng yirik asteroidlardan biri, orbital egilishi juda katta (~35°).",
            "Паллада — иң зур астероидлардан берсе, орбита авышлыгы гадәттән тыш зур (~35°).",
            "Палада — адзін з найбуйнейшых астэроідаў з вялікім нахілам арбіты (~35°)."),
        Entry(
            "Asteroid_Icarus",
            "1566 Icarus is a near-Earth Apollo asteroid whose perihelion dips inside Mercury's orbit. Close approaches to the Sun heat its surface — used here to show the hot asteroid-ball material variant.",
            "1566 Икар — околоземный астероид группы Аполлона; перигелий заходит внутрь орбиты Меркурия. Сближения с Солнцем сильно нагревают поверхность — здесь это показывает «горячий» вариант модели.",
            "1566伊卡洛斯是阿波罗型近地小行星，近日点进入水星轨道内侧；近日加热对应本应用的“热”材质变体。",
            "1566 Icarus là tiểu hành tinh Apollo cận Trái Đất; điểm cận nhật nằm trong quỹ đạo Sao Thủy — dùng để hiện biến thể vật liệu nóng.",
            "1566 Icarus — Yer yaqinidagi Apollon tipidagi asteroid; periheliy Merkuriy orbitasidan ichkariga kiradi.",
            "1566 Икар — Җир янындагы Аполлон төрендәге астероид; перигелий Меркурий орбитасыннан эчкә керә.",
            "1566 Ікар — калязямны астэроід групы Апалона; перыгелій заходзіць унутр арбіты Меркурыя.")
    };

    static ContentEntry Entry(
        string id,
        string english,
        string russian,
        string chinese,
        string vietnamese,
        string uzbek,
        string tatar,
        string belarusian)
    {
        return new ContentEntry
        {
            id = id,
            english = english,
            russian = russian,
            chinese = chinese,
            vietnamese = vietnamese,
            uzbek = uzbek,
            tatar = tatar,
            belarusian = belarusian
        };
    }

    public static ContentEntry Get(string objectName)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].id == objectName)
                return Entries[i];
        }

        return default;
    }
}
