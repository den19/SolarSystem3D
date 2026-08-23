import json
import re
import urllib.request

uid = "1b7feef6a0614837b21790e219e68ab2"
url = f"https://sketchfab.com/models/{uid}/embed"
req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
html = urllib.request.urlopen(req, timeout=30).read().decode("utf-8", "replace")
patterns = [
    r"https://[^\"']+\.glb[^\"']*",
    r"https://media\.sketchfab\.com[^\"']+",
    r"\"model\"\s*:\s*\{[^\}]+\}",
]
for pat in patterns:
    ms = re.findall(pat, html)
    print(pat, "count", len(ms))
    for m in ms[:8]:
        print(" ", m[:200])
