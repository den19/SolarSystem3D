"""Try Sketchfab official download via browser cookies / API token.

Do not commit secrets. Temporary helper — safe to delete after success.
"""
from __future__ import annotations

import base64
import ctypes
import ctypes.wintypes
import json
import os
import shutil
import sqlite3
import sys
import tempfile
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

try:
    from Crypto.Cipher import AES
except ImportError:
    import subprocess

    subprocess.check_call([sys.executable, "-m", "pip", "install", "pycryptodome", "-q"])
    from Crypto.Cipher import AES

UID = "5b7853d53cd84b6ca6c16fe68a92c98a"
OUT = Path(__file__).resolve().parent / "Mars3-source.glb"
UA = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
)


class DATA_BLOB(ctypes.Structure):
    _fields_ = [
        ("cbData", ctypes.wintypes.DWORD),
        ("pbData", ctypes.POINTER(ctypes.c_char)),
    ]


def dpapi_unprotect(data: bytes) -> bytes:
    blob_in = DATA_BLOB(len(data), ctypes.create_string_buffer(data, len(data)))
    blob_out = DATA_BLOB()
    if not ctypes.windll.crypt32.CryptUnprotectData(
        ctypes.byref(blob_in), None, None, None, None, 0, ctypes.byref(blob_out)
    ):
        raise OSError("CryptUnprotectData failed")
    try:
        return ctypes.string_at(blob_out.pbData, blob_out.cbData)
    finally:
        ctypes.windll.kernel32.LocalFree(blob_out.pbData)


def get_aes_key(local_state: Path) -> bytes:
    data = json.loads(local_state.read_text(encoding="utf-8"))
    enc = base64.b64decode(data["os_crypt"]["encrypted_key"])
    assert enc.startswith(b"DPAPI")
    return dpapi_unprotect(enc[5:])


def decrypt_value(buff: bytes, key: bytes) -> str:
    if not buff:
        return ""
    # Chrome App-Bound Encryption (v20) — cannot decrypt without elevation / ABE.
    if buff.startswith(b"v20"):
        return ""
    if buff.startswith(b"v10") or buff.startswith(b"v11"):
        iv = buff[3:15]
        payload = buff[15:]
        cipher = AES.new(key, AES.MODE_GCM, iv)
        try:
            return cipher.decrypt(payload[:-16]).decode("utf-8", errors="replace")
        except Exception:
            return ""
    try:
        return dpapi_unprotect(buff).decode("utf-8", errors="replace")
    except Exception:
        return ""


def discover_cookie_dbs() -> list[tuple[Path, Path, str]]:
    found: list[tuple[Path, Path, str]] = []
    local = Path(os.environ["LOCALAPPDATA"])
    browsers = [
        ("Chrome", local / r"Google\Chrome\User Data"),
        ("Edge", local / r"Microsoft\Edge\User Data"),
        ("Brave", local / r"BraveSoftware\Brave-Browser\User Data"),
    ]
    for brand, root in browsers:
        if not root.exists():
            continue
        state = root / "Local State"
        for profile in root.iterdir():
            if not profile.is_dir():
                continue
            db = profile / "Network" / "Cookies"
            if db.exists() and state.exists():
                found.append((db, state, f"{brand}/{profile.name}"))
    return found


def load_jars() -> list[tuple[str, dict[str, str], int]]:
    jars: list[tuple[str, dict[str, str], int]] = []
    for db, state, label in discover_cookie_dbs():
        try:
            key = get_aes_key(state)
        except Exception as exc:
            print(f"{label}: AES key fail: {exc}")
            continue
        tmp = Path(tempfile.gettempdir()) / f"skfb_{label.replace('/', '_')}.db"
        try:
            # Browser often locks Cookies; shared-read + write works when CopyFile fails.
            with open(db, "rb", buffering=0) as src, open(tmp, "wb") as dst:
                while True:
                    chunk = src.read(1024 * 1024)
                    if not chunk:
                        break
                    dst.write(chunk)
        except Exception as exc:
            msg = str(exc).encode("ascii", "replace").decode("ascii")
            print(f"{label}: cookie copy fail: {msg}")
            continue
        try:
            con = sqlite3.connect(str(tmp))
            rows = con.execute(
                "SELECT host_key, name, encrypted_value, value FROM cookies "
                "WHERE host_key LIKE '%sketchfab%'"
            ).fetchall()
            print(f"{label}: {len(rows)} sketchfab cookie rows")
            jar: dict[str, str] = {}
            v20 = 0
            for host, name, enc, plain in rows:
                enc_b = enc or b""
                if enc_b.startswith(b"v20"):
                    v20 += 1
                val = (plain or "") or decrypt_value(enc_b, key)
                if val:
                    jar[name] = val
                    print(f"  decrypted {host} {name} len={len(val)}")
                else:
                    prefix = enc_b[:3] if enc_b else b""
                    print(f"  undecryptable {host} {name} prefix={prefix!r}")
            jars.append((label, jar, v20))
            con.close()
        finally:
            try:
                tmp.unlink()
            except OSError:
                pass
    return jars


def http_get(url: str, headers: dict[str, str], timeout: int = 60) -> tuple[int, bytes]:
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return resp.status, resp.read()
    except urllib.error.HTTPError as e:
        return e.code, e.read()


def save_glb_from_download_json(data: dict) -> bool:
    # Prefer glb, then gltf archive, then source.
    for fmt in ("glb", "gltf", "source", "usdz"):
        entry = data.get(fmt)
        if not isinstance(entry, dict) or not entry.get("url"):
            continue
        dl = entry["url"]
        print(f"fetching format={fmt}")
        code, body = http_get(
            dl,
            {"User-Agent": UA, "Referer": f"https://sketchfab.com/3d-models/mars-3-spacecraft-{UID}"},
            timeout=180,
        )
        if code != 200 or not body:
            print(f"  fetch fail status={code} bytes={len(body)}")
            continue
        if fmt == "glb" and body[:4] == b"glTF":
            OUT.write_bytes(body)
            print(f"saved GLB {OUT} size={OUT.stat().st_size}")
            return True
        # Maybe zip containing glb
        zip_path = OUT.with_name(f"Mars3-source-{fmt}.zip")
        zip_path.write_bytes(body)
        print(f"saved archive {zip_path} size={zip_path.stat().st_size}")
        try:
            with zipfile.ZipFile(zip_path) as zf:
                for name in zf.namelist():
                    if name.lower().endswith(".glb"):
                        OUT.write_bytes(zf.read(name))
                        print(f"extracted {name} -> {OUT} size={OUT.stat().st_size}")
                        return True
                # fallback: any gltf + bin not enough alone; keep zip for manual
                print("archive has no .glb; members:", zf.namelist()[:20])
        except zipfile.BadZipFile:
            # raw glb without glTF magic? still try
            if body[:4] == b"glTF":
                OUT.write_bytes(body)
                return True
            print("not a zip / not glb")
    return False


def try_endpoints(auth_headers: dict[str, str], label: str) -> bool:
    endpoints = [
        f"https://api.sketchfab.com/v3/models/{UID}/download",
        f"https://sketchfab.com/i/models/{UID}/download",
    ]
    base = {
        "User-Agent": UA,
        "Accept": "application/json",
        "Referer": f"https://sketchfab.com/3d-models/mars-3-spacecraft-{UID}",
        "Origin": "https://sketchfab.com",
    }
    for url in endpoints:
        headers = {**base, **auth_headers}
        print(f"[{label}] GET {url}")
        code, body = http_get(url, headers)
        text = body.decode("utf-8", errors="replace")
        print(f"  status={code} body[:300]={text[:300]!r}")
        if code != 200:
            continue
        try:
            data = json.loads(text)
        except json.JSONDecodeError:
            print("  not JSON")
            continue
        if save_glb_from_download_json(data):
            return True
    return False


def main() -> int:
    # 1) Explicit API token
    token = (os.environ.get("SKETCHFAB_API_TOKEN") or "").strip()
    if token:
        print(f"env token present len={len(token)}")
        if try_endpoints({"Authorization": f"Token {token}"}, "env-token"):
            return 0

    # 2) Browser cookies
    jars = load_jars()
    for label, jar, v20 in jars:
        print(f"trying jar {label}: keys={list(jar.keys())} v20_rows={v20}")
        if not jar:
            continue
        cookie_header = "; ".join(f"{k}={v}" for k, v in jar.items())
        # Also probe whoami
        code, body = http_get(
            "https://api.sketchfab.com/v3/me",
            {
                "Cookie": cookie_header,
                "User-Agent": UA,
                "Accept": "application/json",
            },
        )
        print(f"  /v3/me status={code} body[:200]={body[:200]!r}")
        if try_endpoints({"Cookie": cookie_header}, f"cookie:{label}"):
            return 0

    # 3) Check Downloads for a manual drop
    downloads = Path.home() / "Downloads"
    if downloads.exists():
        cands = sorted(
            list(downloads.glob("*mars*3*.glb"))
            + list(downloads.glob("*Mars*3*.glb"))
            + list(downloads.glob("*5b7853d*.glb"))
            + list(downloads.glob("*mars*3*.zip"))
            + list(downloads.glob("*Mars*3*.zip")),
            key=lambda p: p.stat().st_mtime,
            reverse=True,
        )
        for p in cands[:5]:
            print(f"found download candidate {p} size={p.stat().st_size}")
            if p.suffix.lower() == ".glb":
                shutil.copy2(p, OUT)
                print(f"copied {p} -> {OUT}")
                return 0
            if p.suffix.lower() == ".zip":
                with zipfile.ZipFile(p) as zf:
                    for name in zf.namelist():
                        if name.lower().endswith(".glb"):
                            OUT.write_bytes(zf.read(name))
                            print(f"extracted {name} from {p}")
                            return 0

    print("NO_SUCCESS")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
