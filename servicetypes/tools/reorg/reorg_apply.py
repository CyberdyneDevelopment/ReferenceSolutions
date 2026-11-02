#!/usr/bin/env python3
"""Regroup public/src and public/tests by domain and rewrite every relative path that crossed a move.

Usage: reorg_apply.py <repo-key fdw|rst> <repo-dir> [--apply]
Without --apply it only reports. Writes nothing outside the repo it is pointed at.
"""
import os, re, sys, json, subprocess, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import domains

repo_key, repo_dir = sys.argv[1], sys.argv[2]
APPLY = "--apply" in sys.argv
REPO = os.path.realpath(repo_dir)
PUB = os.path.join(REPO, "public")

# ---------- plan the moves (works from the flat layout or any already-grouped one) ----------
SKIPDIR = {"bin", "obj", "node_modules", ".git"}
current = {}
for kind in ("src", "tests"):
    root = os.path.join(PUB, kind)
    for dp, dns, fns in os.walk(root):
        dns[:] = [x for x in dns if x not in SKIPDIR]
        if os.path.basename(dp) + ".csproj" in fns:
            dns[:] = []
            current[dp] = (kind, os.path.basename(dp))
moves = {}
for p, (kind, n) in current.items():
    dom = domains.domain_of(repo_key, n)
    if dom is None and kind == "tests": dom = "_cross"
    if dom is None: raise SystemExit(f"no domain for non-test project {n}")
    grp = domains.GROUP.get(dom)
    new = os.path.join(PUB, kind, *([grp] if grp else []), dom, n)
    if new != p: moves[p] = new
order = sorted(moves, key=len, reverse=True)

def mapped(path):
    for old in order:
        if path == old or path.startswith(old + os.sep):
            return moves[old] + path[len(old):]
    return path

# ---------- rewrite relative paths in build files ----------
SKIP = {".git", "bin", "obj", "node_modules"}
files = []
for root, ds, fs in os.walk(REPO):
    ds[:] = [x for x in ds if x not in SKIP]
    for f in fs:
        if f.endswith((".csproj", ".props", ".targets", ".slnx", ".proj")):
            files.append(os.path.join(root, f))

REL = re.compile(r'(?P<pre>\$\(MSBuildThisFileDirectory\)|\$\(MSBuildProjectDirectory\)[\\/])?(?P<rel>(?:\.\.[\\/])+[^"\'<>;|\r\n$*]*)')
REL2 = re.compile(r'(?P<pre>\$\(MSBuildThisFileDirectory\)|\$\(MSBuildProjectDirectory\)[\\/])(?P<rel>(?!\.\.)[^"\'<>;|\r\n$*]+)')
SLNX = re.compile(r'(?P<k>Path=")(?P<v>[^"]+)(")')

def rel_to(newdir, target, style, trailing):
    r = os.path.relpath(target, newdir)
    r = r.replace("/", style)
    return r + (style if trailing and not r.endswith(style) else "")

stats = collections.Counter(); changes = {}
for f in files:
    old_dir = os.path.dirname(f)
    new_dir = os.path.dirname(mapped(f))
    text = open(f, encoding="utf-8", newline="").read()
    out = text
    if f.endswith(".slnx"):
        def sub_slnx(m):
            v = m.group("v")
            if "$(" in v or v.startswith(("/", "http")): return m.group(0)
            style = "\\" if "\\" in v else "/"
            absold = os.path.normpath(os.path.join(old_dir, v.replace("\\", "/")))
            if not os.path.exists(absold): return m.group(0)
            newabs = mapped(absold)
            if newabs == absold and new_dir == old_dir: return m.group(0)
            stats["slnx"] += 1
            return m.group("k") + rel_to(new_dir, newabs, style, v.endswith(("/", "\\"))) + '"'
        out = SLNX.sub(sub_slnx, text)
    else:
        def sub(m):
            pre, rel = m.group("pre") or "", m.group("rel")
            style = "\\" if "\\" in rel else "/"
            trailing = rel.endswith(("/", "\\"))
            absold = os.path.normpath(os.path.join(old_dir, rel.replace("\\", "/")))
            newabs = mapped(absold)
            if newabs == absold and new_dir == old_dir: return m.group(0)
            new_rel = rel_to(new_dir, newabs, style, trailing)
            if new_rel.replace("\\", "/") == rel.replace("\\", "/"): return m.group(0)
            stats["paths"] += 1
            return pre + new_rel
        out = REL.sub(sub, text)

        # A path under a directory macro with no leading '..' (for example $(MSBuildThisFileDirectory)src\Fdw.X\Fdw.X.csproj)
        # is just as relative; the first pass cannot see it, and a missed one is a project reference that is silently skipped.
        def sub2(m):
            pre, rel = m.group("pre"), m.group("rel")
            style = "\\" if "\\" in rel else "/"
            trailing = rel.endswith(("/", "\\"))
            absold = os.path.normpath(os.path.join(old_dir, rel.replace("\\", "/")))
            newabs = mapped(absold)
            if newabs == absold and new_dir == old_dir: return m.group(0)
            new_rel = rel_to(new_dir, newabs, style, trailing)
            if new_rel.replace("\\", "/") == rel.replace("\\", "/"): return m.group(0)
            stats["paths"] += 1
            return pre + new_rel
        out = REL2.sub(sub2, out)
    if out != text:
        changes[f] = out
        stats["files"] += 1

print(f"[{repo_key}] projects to move: {len(moves)}")
byd = collections.Counter(os.path.basename(os.path.dirname(v)) for v in moves.values())
print(" ", dict(sorted(byd.items())))
print(f"  build files to rewrite: {stats['files']} ({stats['paths']} path values, {stats['slnx']} slnx entries)")

# literal src/<Project> mentions in scripts
SCRIPT_GLOBS = [os.path.join(PUB, "scripts")]
names = {os.path.basename(o): o for o in moves}
lit = re.compile(r"(?P<pre>(?:public/)?)(?P<k>src|tests)/(?P<n>[A-Za-z0-9_.]+)(?=[/\s\"'`)]|$)")
script_changes = {}
for sd in SCRIPT_GLOBS:
    if not os.path.isdir(sd): continue
    for fn in os.listdir(sd):
        fp = os.path.join(sd, fn)
        if not fn.endswith((".sh", ".ps1", ".py")) or not os.path.isfile(fp): continue
        t = open(fp, encoding="utf-8", newline="").read()
        def s2(m):
            old = os.path.join(PUB, m.group("k"), m.group("n"))
            if old in moves:
                nd = os.path.relpath(moves[old], os.path.join(PUB, m.group("k")))
                return f'{m.group("pre")}{m.group("k")}/{nd}'
            return m.group(0)
        t2 = lit.sub(s2, t)
        if t2 != t: script_changes[fp] = t2
print(f"  scripts with literal project paths: {[os.path.relpath(p, REPO) for p in script_changes]}")

if not APPLY:
    sys.exit(0)

# ---------- apply: write rewritten text at OLD location, then git mv ----------
for f, out in changes.items():
    open(f, "w", encoding="utf-8", newline="").write(out)
for fp, t in script_changes.items():
    open(fp, "w", encoding="utf-8", newline="").write(t)
MARKERS = (".expert.json", "CLAUDE.md")
def folders_without_project(root):
    for dp, dns, fns in os.walk(root, topdown=False):
        if dp != root and os.path.basename(dp) + ".csproj" not in fns and not any(f.endswith(".csproj") for f in fns):
            yield dp
for dp in list(folders_without_project(os.path.join(PUB, "src"))):
    for m in MARKERS:
        f = os.path.join(dp, m)
        if os.path.isfile(f): os.remove(f)
doms = {os.path.dirname(v) for v in moves.values()}
for dd in doms: os.makedirs(dd, exist_ok=True)
for old in sorted(moves):
    subprocess.run(["git", "-C", REPO, "mv", os.path.relpath(old, REPO), os.path.relpath(moves[old], REPO)], check=True)
for kind in ("src", "tests"):
    for dp in list(folders_without_project(os.path.join(PUB, kind))):
        if not os.listdir(dp): os.rmdir(dp)
mj = os.path.join(PUB, "tools", "reorg", "moves.json")
prev = json.load(open(mj)) if os.path.exists(mj) else {}
rel = lambda x: os.path.relpath(x, REPO)
if prev:
    rec = {flat: rel(moves.get(os.path.join(REPO, cur), os.path.join(REPO, cur))) for flat, cur in prev.items()}
else:
    rec = {rel(o): rel(n) for o, n in moves.items()}
os.makedirs(os.path.dirname(mj), exist_ok=True)
json.dump(rec, open(mj, "w"), indent=1, sort_keys=True)
print(f"  applied: {len(moves)} directories moved, moves.json now maps {len(rec)} original paths")
here = os.path.dirname(os.path.abspath(__file__))
for slnx in sorted(f for f in os.listdir(PUB) if f.endswith(".slnx")):
    subprocess.run([sys.executable, os.path.join(here, "regenerate_slnx.py"), os.path.join(PUB, slnx)], check=True)
