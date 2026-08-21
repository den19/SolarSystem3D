"""Download Mars 3 via Chrome CDP using in-page fetch (full browser session).

Chrome must be running with:
  --remote-debugging-port=9222 --remote-allow-origins=*
Do not commit secrets.
"""
from __future__ import annotations

import json
import sys
import time
import urllib.request
import zipfile
from pathlib import Path

try:
    import websocket  # type: ignore
except ImportError:
    import subprocess

    subprocess.check_call([sys.executable, "-m", "pip", "install", "websocket-client", "-q"])
    import websocket  # type: ignore

UID = "5b7853d53cd84b6ca6c16fe68a92c98a"
OUT = Path(__file__).resolve().parent / "Mars3-source.glb"
MODEL_URL = f"https://sketchfab.com/3d-models/mars-3-spacecraft-{UID}"
LOGIN_URL = "https://sketchfab.com/login"


def http_json(url: str):
    with urllib.request.urlopen(url, timeout=10) as r:
        return json.loads(r.read().decode("utf-8"))


class Cdp:
    def __init__(self, url: str):
        self.ws = websocket.create_connection(url, timeout=15)
        self._id = 0

    def call(self, method: str, params: dict | None = None, timeout: float = 60) -> dict:
        self._id += 1
        msg_id = self._id
        payload = {"id": msg_id, "method": method}
        if params:
            payload["params"] = params
        self.ws.send(json.dumps(payload))
        deadline = time.time() + timeout
        while time.time() < deadline:
            data = json.loads(self.ws.recv())
            if data.get("id") == msg_id:
                if "error" in data:
                    raise RuntimeError(f"{method}: {data['error']}")
                return data.get("result") or {}
        raise TimeoutError(method)

    def close(self) -> None:
        try:
            self.ws.close()
        except Exception:
            pass


def connect_page() -> Cdp:
    tabs = http_json("http://127.0.0.1:9222/json")
    page = None
    for t in tabs:
        if t.get("type") == "page" and t.get("webSocketDebuggerUrl"):
            if "sketchfab" in (t.get("url") or "").lower():
                page = t
                break
    if page is None:
        for t in tabs:
            if t.get("type") == "page" and t.get("webSocketDebuggerUrl"):
                page = t
                break
    if page is None:
        raise RuntimeError("No CDP page target")
    print("Using tab", page.get("url"))
    return Cdp(page["webSocketDebuggerUrl"])


def eval_json(cdp: Cdp, expression: str, timeout: float = 120) -> object:
    # Runtime.evaluate with awaitPromise for async fetch
    res = cdp.call(
        "Runtime.evaluate",
        {
            "expression": expression,
            "awaitPromise": True,
            "returnByValue": True,
            "userGesture": True,
        },
        timeout=timeout,
    )
    if res.get("exceptionDetails"):
        raise RuntimeError(res["exceptionDetails"])
    return (res.get("result") or {}).get("value")


ME_JS = """
(async () => {
  const r = await fetch('https://api.sketchfab.com/v3/me', {
    credentials: 'include',
    headers: { 'Accept': 'application/json' }
  });
  const text = await r.text();
  return { status: r.status, text: text.slice(0, 500) };
})()
"""

DOWNLOAD_JS = f"""
(async () => {{
  const urls = [
    'https://api.sketchfab.com/v3/models/{UID}/download',
    'https://sketchfab.com/i/models/{UID}/download'
  ];
  const out = [];
  for (const url of urls) {{
    try {{
      const r = await fetch(url, {{
        credentials: 'include',
        headers: {{
          'Accept': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        }}
      }});
      const text = await r.text();
      out.push({{ url, status: r.status, text: text.slice(0, 2000) }});
    }} catch (e) {{
      out.push({{ url, status: -1, text: String(e) }});
    }}
  }}
  return out;
}})()
"""

LOCAL_TOKEN_JS = """
(() => {
  const keys = Object.keys(localStorage);
  const interesting = {};
  for (const k of keys) {
    const v = localStorage.getItem(k) || '';
    if (/token|auth|session|user|api/i.test(k) || /token|Bearer/i.test(v)) {
      interesting[k] = v.slice(0, 200);
    }
  }
  return interesting;
})()
"""


def save_from_download_payload(text: str) -> bool:
    data = json.loads(text)
    for fmt in ("glb", "gltf", "source", "usdz"):
        entry = data.get(fmt)
        if not isinstance(entry, dict) or not entry.get("url"):
            continue
        dl = entry["url"]
        print("fetching", fmt, dl[:120])
        with urllib.request.urlopen(dl, timeout=300) as r:
            blob = r.read()
        if blob[:4] == b"glTF":
            OUT.write_bytes(blob)
            print("saved GLB", OUT, OUT.stat().st_size)
            return True
        zpath = OUT.with_name(f"Mars3-source-{fmt}.zip")
        zpath.write_bytes(blob)
        try:
            with zipfile.ZipFile(zpath) as zf:
                for name in zf.namelist():
                    if name.lower().endswith(".glb"):
                        OUT.write_bytes(zf.read(name))
                        print("extracted", name, OUT.stat().st_size)
                        return True
                print("members", zf.namelist()[:25])
        except zipfile.BadZipFile:
            print("not zip, magic", blob[:8])
    return False


def ensure_debug_chrome() -> None:
    try:
        http_json("http://127.0.0.1:9222/json/version")
        return
    except Exception:
        pass
    import os
    import subprocess

    profile = Path(os.environ["TEMP"]) / "skfb_chrome_profile"
    profile.mkdir(parents=True, exist_ok=True)
    chrome = Path(r"C:\Program Files\Google\Chrome\Application\chrome.exe")
    subprocess.Popen(
        [
            str(chrome),
            "--remote-debugging-port=9222",
            "--remote-allow-origins=*",
            f"--user-data-dir={profile}",
            "--no-first-run",
            "--no-default-browser-check",
            LOGIN_URL,
        ]
    )
    for _ in range(20):
        time.sleep(0.5)
        try:
            http_json("http://127.0.0.1:9222/json/version")
            return
        except Exception:
            pass
    raise RuntimeError("Failed to start debug Chrome")


def main() -> int:
    ensure_debug_chrome()
    cdp = connect_page()
    try:
        cdp.call("Page.enable")
        cdp.call("Network.enable")
        cdp.call("Runtime.enable")
        cdp.call("Page.navigate", {"url": LOGIN_URL})
        time.sleep(2)
        print("Log in to Sketchfab in the debug Chrome window (up to 5 min)…")
        deadline = time.time() + 300
        logged_in = False
        while time.time() < deadline:
            try:
                me = eval_json(cdp, ME_JS, timeout=30)
            except Exception as exc:
                print("me eval error", exc)
                # reconnect
                try:
                    cdp.close()
                except Exception:
                    pass
                time.sleep(2)
                cdp = connect_page()
                cdp.call("Page.enable")
                cdp.call("Network.enable")
                cdp.call("Runtime.enable")
                continue
            print("me:", me)
            if isinstance(me, dict) and me.get("status") == 200:
                logged_in = True
                break
            # still show localStorage hints
            try:
                toks = eval_json(cdp, LOCAL_TOKEN_JS, timeout=10)
                if toks:
                    print("localStorage keys:", list(toks.keys()) if isinstance(toks, dict) else toks)
            except Exception:
                pass
            time.sleep(5)

        if not logged_in:
            print("TIMEOUT: not logged in")
            return 1

        cdp.call("Page.navigate", {"url": MODEL_URL})
        time.sleep(3)
        results = eval_json(cdp, DOWNLOAD_JS, timeout=120)
        print("download results:", json.dumps(results, indent=2)[:2000])
        if isinstance(results, list):
            for item in results:
                if item.get("status") == 200 and item.get("text"):
                    if save_from_download_payload(item["text"]):
                        return 0
        print("DOWNLOAD_FAILED")
        return 2
    finally:
        cdp.close()


if __name__ == "__main__":
    raise SystemExit(main())
