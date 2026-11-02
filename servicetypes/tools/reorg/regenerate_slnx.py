#!/usr/bin/env python3
"""Rebuild a .slnx's /src and /tests solution folders from each project's path on disk.

  regenerate_slnx.py <file.slnx> [--check]

A project at src/<group>/<domain>/<Project>/<Project>.csproj goes in solution folder /src/<group>/<domain>/;
one at src/<domain>/<Project>/... goes in /src/<domain>/. Tests are handled the same way.
Everything else in the file (configurations, the Solution Items folder, comments) is left as it is, and each
<Project> element is moved whole, so its per-configuration <BuildType> mappings are kept.
With --check nothing is written; the exit status is 1 if the file would change.
"""
import re, sys, collections

path = sys.argv[1]
check = "--check" in sys.argv
raw = open(path, "rb").read()
bom = raw.startswith(b"\xef\xbb\xbf")
text = raw.decode("utf-8-sig")
nl = "\r\n" if "\r\n" in text else "\n"
text = text.replace("\r\n", "\n")

FOLDER = re.compile(r'[ \t]*<Folder Name="(/(?:src|tests)(?:/[^"]*)?/)">\n(.*?)\n[ \t]*</Folder>[ \t]*\n', re.S)
PROJECT = re.compile(r'([ \t]*)<Project Path="([^"]+)"[ \t]*(?:/>|>.*?</Project>)[ \t]*\n', re.S)

projects = []  # (path, raw element text)
def take(m):
    for pm in PROJECT.finditer(m.group(0)):
        projects.append((pm.group(2), pm.group(0)))
    return ""
stripped = FOLDER.sub(take, text)
if "<Project Path" in PROJECT.sub("", stripped) and False:
    pass
if not projects:
    sys.exit(f"{path}: no /src or /tests folders with projects found; refusing to write")

groups = collections.defaultdict(list)
for p, elem in projects:
    parts = p.replace("\\", "/").split("/")
    if parts[0] not in ("src", "tests") or len(parts) < 4:
        sys.exit(f"{path}: project {p} is not src|tests/<domain>/<Project>/<Project>.csproj")
    groups["/" + "/".join(parts[:-2]) + "/"].append((p, elem))

def order(name):
    return (0 if name.startswith("/src/") else 1, name)

blocks = []
for name in sorted(groups, key=order):
    body = "".join(e if e.endswith("\n") else e + "\n" for _, e in sorted(groups[name]))
    blocks.append(f'  <Folder Name="{name}">\n{body}  </Folder>\n')

end = stripped.rindex("</Solution>")
out = stripped[:end] + "".join(blocks) + stripped[end:]

before = {p for p, _ in projects}
after = set(re.findall(r'<Project Path="([^"]+)"', out))
if before != after or len(projects) != len(re.findall(r"<Project Path", out)):
    sys.exit(f"{path}: project set changed ({len(before)} before, {len(after)} after); refusing to write")
import xml.etree.ElementTree as ET
ET.fromstring(out.encode("utf-8"))

result = out.replace("\n", nl)
changed = result != text.replace("\n", nl)
print(f"{path}: {len(projects)} projects in {len(groups)} solution folders" + ("" if changed else " (already current)"))
if check:
    sys.exit(1 if changed else 0)
if changed:
    open(path, "wb").write((b"\xef\xbb\xbf" if bom else b"") + result.encode("utf-8"))
