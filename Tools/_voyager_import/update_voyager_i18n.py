import json
from pathlib import Path

updates = {
    "english.json": {
        "ProbeModelVoyager": "Voyager 1",
        "ProbeDescVoyager": (
            "Launched in 1977; flew by Jupiter and Saturn, then entered interstellar space. "
            "Carries the Golden Record with greetings from Earth."
        ),
    },
    "russian.json": {
        "ProbeModelVoyager": "Вояджер 1",
        "ProbeDescVoyager": (
            "Запущен в 1977 г.; пролетел мимо Юпитера и Сатурна и вышел в межзвёздное пространство. "
            "На борту — «Золотая пластина» с приветствиями с Земли."
        ),
    },
    "chinese.json": {
        "ProbeModelVoyager": "旅行者1号",
        "ProbeDescVoyager": "1977年发射；飞掠木星与土星后进入星际空间。携带地球问候的“金唱片”。",
    },
    "vietnamese.json": {
        "ProbeModelVoyager": "Voyager 1",
        "ProbeDescVoyager": (
            "Phóng năm 1977; bay qua Sao Mộc và Sao Thổ rồi vào không gian liên sao. "
            "Mang theo Đĩa Vàng chào từ Trái Đất."
        ),
    },
    "uzbek.json": {
        "ProbeModelVoyager": "Voyager 1",
        "ProbeDescVoyager": (
            "1977-yilda uchirilgan; Yupiter va Saturn yonidan o'tib, yulduzlararo fazoga chiqdi. "
            "Yer salomlari bilan «Oltin plastinka» bor."
        ),
    },
    "tatar.json": {
        "ProbeModelVoyager": "Voyager 1",
        "ProbeDescVoyager": (
            "1977 elında cibärelde; Yupiter häm Saturn yanınnan uçıp, yoldızara fazağa çıqtı. "
            "Älfäten salamlarlı «Altın Plastinka» bar."
        ),
    },
    "belarusian.json": {
        "ProbeModelVoyager": "Voyager 1",
        "ProbeDescVoyager": (
            "Запушчаны ў 1977 г.; праляцеў міма Юпітэра і Сатурна і выйшаў у міжзоркавую прастору. "
            "На борту — «Залатая пласціна» з прывітаннямі з Зямлі."
        ),
    },
}

base = Path(__file__).resolve().parents[2] / "Assets" / "Resources" / "Languages"
for name, patch in updates.items():
    path = base / name
    data = json.loads(path.read_text(encoding="utf-8"))
    data.update(patch)
    path.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print("updated", name, "->", data["ProbeModelVoyager"])
