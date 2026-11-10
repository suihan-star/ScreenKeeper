"""Package the approved promotion assets; preserve original files and QA evidence."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import unquote, urlsplit
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parent
ARCHIVE = ROOT / "screenkeeper-social-v2.zip"
MANIFEST = ROOT / "manifest.json"
CLIPS = ["01-screen-rest", "02-privacy", "03-health", "04-color"]

def sha256(data):
    return hashlib.sha256(data).hexdigest()

class LocalLinks(HTMLParser):
    def __init__(self):
        super().__init__()
        self.links = []

    def handle_starttag(self, tag, attrs):
        self.links.extend(value for name, value in attrs
                          if name in {"src", "href", "poster"} and value)

files = [p for p in ROOT.iterdir()
         if p.is_file() and p not in {ARCHIVE, MANIFEST}]
for folder in ("assets", "exports", "motion", "posters", "vendor"):
    files.extend(p for p in (ROOT / folder).rglob("*") if p.is_file())
files.extend(ROOT / "qa" / name for name in (
    "verification.json", "export-verification.json", "media-verification.json",
    "01-screen-rest-03.png", "02-privacy-05.png", "03-health-05.png", "04-color-05.png",
))
files = sorted(set(files))
assert all(p.is_file() for p in files)
allowed = set(files) | {ARCHIVE, MANIFEST}
link_count = 0
for page in (p for p in files if p.suffix == ".html"):
    parser = LocalLinks()
    parser.feed(page.read_text(encoding="utf-8"))
    for link in parser.links:
        parts = urlsplit(link)
        if parts.scheme or parts.netloc or not parts.path:
            continue
        target = (page.parent / unquote(parts.path)).resolve()
        assert target in allowed, f"Missing package dependency: {page.name}: {link}"
        link_count += 1

manifest = {
    "product": "屏幕良伴 ScreenKeeper",
    "revision": 2,
    "positioning": "单屏多屏，皆由你心定",
    "note": "四段均为单屏功能示意，非软件实录。素材未发布。",
    "media": {
        "clips": CLIPS, "durationSeconds": 18,
        "mp4": {"width": 900, "height": 1200, "fps": 24, "frames": 432},
        "gif": {"width": 540, "height": 720, "frames": 216, "loop": "infinite"},
        "posters": {"x": [1672, 941], "xiaohongshu": [1086, 1448]},
    },
    "verifiedLocalHtmlReferences": link_count,
    "files": [{"path": p.relative_to(ROOT).as_posix(), "bytes": p.stat().st_size,
               "sha256": sha256(p.read_bytes())} for p in files],
}
MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
files.append(MANIFEST)
with zipfile.ZipFile(ARCHIVE, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for p in files:
        archive.write(p, p.relative_to(ROOT).as_posix())
with zipfile.ZipFile(ARCHIVE) as archive:
    assert archive.testzip() is None
    assert len(archive.infolist()) == len(files)
    for p in files:
        assert sha256(archive.read(p.relative_to(ROOT).as_posix())) == sha256(p.read_bytes())
print(json.dumps({"archive": ARCHIVE.name, "entries": len(files),
                  "bytes": ARCHIVE.stat().st_size, "sha256": sha256(ARCHIVE.read_bytes()),
                  "localHtmlReferences": link_count, "verified": True}, indent=2))
