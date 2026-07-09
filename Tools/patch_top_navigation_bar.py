"""Patch Level1.unity: unify top navigation bar layout."""

from pathlib import Path

SCENE = Path(r"c:\git\SolarSystemV7_01082025_07_00\Assets\_Scenes\Level1.unity")

LAYOUT_ELEMENT_GUID = "306cc8c2b49d7114eaa3623786fc2126"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise SystemExit(f"Missing block: {label}")
    return text.replace(old, new, 1)


def main() -> None:
    text = SCENE.read_text(encoding="utf-8")

    # BodyNavigationBar children + stretch top layout
    text = replace_once(
        text,
        """  m_Children:
  - {fileID: 960001111}
  - {fileID: 960001131}
  - {fileID: 960001151}
  m_Father: {fileID: 992762334}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 1}
  m_AnchorMax: {x: 0.5, y: 1}
  m_AnchoredPosition: {x: -25.950022, y: -61.839294}
  m_SizeDelta: {x: 218.1, y: 48}
  m_Pivot: {x: 0.5, y: 1}""",
        """  m_Children:
  - {fileID: 960001111}
  - {fileID: 960001131}
  - {fileID: 960001151}
  - {fileID: 950001101}
  - {fileID: 1363285165}
  m_Father: {fileID: 992762334}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 48}
  m_Pivot: {x: 0.5, y: 1}""",
        "BodyNavigationBar rect",
    )

    # Remove buttons from canvas direct children list
    text = replace_once(
        text,
        """  - {fileID: 2215868324214501490}
  - {fileID: 1363285165}
  - {fileID: 700001101}""",
        """  - {fileID: 2215868324214501490}
  - {fileID: 700001101}""",
        "canvas children remove SimulationControlButton",
    )
    text = replace_once(
        text,
        """  - {fileID: 4167420902131862358}
  - {fileID: 950001101}
  - {fileID: 950001201}""",
        """  - {fileID: 4167420902131862358}
  - {fileID: 950001201}""",
        "canvas children remove SidePanelMenuButton",
    )

    # SidePanelMenuButton reparent + layout child rect + LayoutElement component list
    text = replace_once(
        text,
        """  m_Component:
  - component: {fileID: 950001101}
  - component: {fileID: 950001102}
  - component: {fileID: 950001103}
  - component: {fileID: 950001104}
  m_Layer: 5
  m_Name: SidePanelMenuButton""",
        """  m_Component:
  - component: {fileID: 950001101}
  - component: {fileID: 950001102}
  - component: {fileID: 950001103}
  - component: {fileID: 950001104}
  - component: {fileID: 950001105}
  m_Layer: 5
  m_Name: SidePanelMenuButton""",
        "SidePanelMenuButton components",
    )
    text = replace_once(
        text,
        """  m_Father: {fileID: 992762334}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -7.9000244, y: -62.900024}
  m_SizeDelta: {x: 44, y: 44}
  m_Pivot: {x: 1, y: 1}
--- !u!222 &950001102""",
        """  m_Father: {fileID: 960001101}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &950001102""",
        "SidePanelMenuButton rect",
    )

  # SimulationControlButton reparent + LayoutElement
    text = replace_once(
        text,
        """  m_Component:
  - component: {fileID: 1363285165}
  - component: {fileID: 1363285169}
  - component: {fileID: 1363285168}
  - component: {fileID: 1363285167}
  - component: {fileID: 1363285166}
  m_Layer: 5
  m_Name: SimulationControlButton""",
        """  m_Component:
  - component: {fileID: 1363285165}
  - component: {fileID: 1363285169}
  - component: {fileID: 1363285168}
  - component: {fileID: 1363285167}
  - component: {fileID: 1363285166}
  - component: {fileID: 1363285170}
  m_Layer: 5
  m_Name: SimulationControlButton""",
        "SimulationControlButton components",
    )
    text = replace_once(
        text,
        """  m_Father: {fileID: 992762334}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -7.9000244, y: -6.9000244}
  m_SizeDelta: {x: 98.8132, y: 44}
  m_Pivot: {x: 1, y: 1}
--- !u!114 &1363285166""",
        """  m_Father: {fileID: 960001101}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &1363285166""",
        "SimulationControlButton rect",
    )

    # BodyNameButton label auto-size
    text = replace_once(
        text,
        "  m_fontSize: 17\n  m_fontSizeBase: 17\n  m_fontWeight: 700\n  m_enableAutoSizing: 0\n  m_fontSizeMin: 12\n  m_fontSizeMax: 24",
        "  m_fontSize: 17\n  m_fontSizeBase: 17\n  m_fontWeight: 700\n  m_enableAutoSizing: 1\n  m_fontSizeMin: 12\n  m_fontSizeMax: 17",
        "BodyName auto size",
    )

    if "&950001105" not in text:
        menu_layout = f"""--- !u!114 &950001105
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 950001100}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {LAYOUT_ELEMENT_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreLayout: 0
  m_MinWidth: 44
  m_MinHeight: 44
  m_PreferredWidth: 44
  m_PreferredHeight: 44
  m_FlexibleWidth: 0
  m_FlexibleHeight: 0
  m_LayoutPriority: 1
"""
        anchor = "--- !u!1 &950001200"
        text = replace_once(text, anchor, menu_layout + anchor, "insert menu LayoutElement")

    if "&1363285170" not in text:
        sim_layout = f"""--- !u!114 &1363285170
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1363285164}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {LAYOUT_ELEMENT_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreLayout: 0
  m_MinWidth: 72
  m_MinHeight: 44
  m_PreferredWidth: 88
  m_PreferredHeight: 44
  m_FlexibleWidth: 0
  m_FlexibleHeight: 0
  m_LayoutPriority: 1
"""
        anchor = "--- !u!222 &1363285169"
        text = replace_once(text, anchor, sim_layout + anchor, "insert sim LayoutElement")

    SCENE.write_text(text, encoding="utf-8")
    print("Patched Level1.unity navigation bar layout")


if __name__ == "__main__":
    main()
