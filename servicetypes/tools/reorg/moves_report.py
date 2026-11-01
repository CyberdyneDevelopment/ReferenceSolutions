#!/usr/bin/env python3
"""Write a complete old-path to new-path map for a reorganised clone.

  moves_report.py <clone-dir> <out-dir>

The map is computed from tools/reorg/moves.json (project directory to project directory) applied to every file
that git has at HEAD, then checked against the working tree. Git's own rename detection is not used for the map,
because it pairs files by content similarity and reports a heavily edited file as a delete plus an add.

Writes <out-dir>/<repo>-files.tsv (one row per file at HEAD that sits under a moved project),
<out-dir>/<repo>-projects.tsv (one row per project) and <out-dir>/<repo>-new-files.tsv (files that did not exist at HEAD).
Exits 1 if a file that should have moved is missing at its new path or still present at the old one.
"""
import hashlib, json, os, subprocess, sys

clone = os.path.realpath(sys.argv[1]); out = sys.argv[2]
repo = os.path.basename(clone)
os.makedirs(out, exist_ok=True)

def git(*a):
    return subprocess.run(["git", "-C", clone, *a], capture_output=True, check=True).stdout

moves = json.load(open(os.path.join(clone, "public", "tools", "reorg", "moves.json")))
prefixes = sorted(moves, key=len, reverse=True)

files = []  # (path, blob sha at HEAD)
for line in git("ls-tree", "-r", "-z", "HEAD").split(b"\0"):
    if not line:
        continue
    meta, path = line.split(b"\t", 1)
    mode, _, sha = meta.split()
    files.append((path.decode("utf-8", "surrogateescape"), sha.decode(), mode == b"120000"))

def link_sha(p):
    data = os.readlink(p).encode("utf-8", "surrogateescape")
    return hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()

# Locate every file first, then hash the regular ones in one `git hash-object --stdin-paths` call, which applies the
# repository's line-ending and attribute filters the way git does when it stages a file. Hashing raw bytes would report
# every CRLF file as edited.
plan, problems = [], []
for path, sha, is_link in files:
    new = None; project = ""
    for p in prefixes:
        if path.startswith(p + "/"):
            new = moves[p] + path[len(p):]; project = os.path.basename(p); break
    target = new or path
    full = os.path.join(clone, target)
    if not os.path.lexists(full):
        problems.append(("missing at new path" if new else "missing", path, target)); continue
    if new and os.path.lexists(os.path.join(clone, path)):
        problems.append(("still present at old path", path, new)); continue
    if "\n" in target:
        problems.append(("path contains a newline", path, target)); continue
    plan.append((path, sha, is_link, new, target, project))

regular = [t for _, _, link, _, t, _ in plan if not link]
hashed = subprocess.run(["git", "-C", clone, "hash-object", "--stdin-paths"], input="\n".join(regular).encode("utf-8", "surrogateescape") + b"\n",
                        capture_output=True, check=True).stdout.decode().split()
if len(hashed) != len(regular):
    sys.exit(f"hash-object returned {len(hashed)} hashes for {len(regular)} files")
now = dict(zip(regular, hashed))

rows = []
counts = {"moved": 0, "moved+edited": 0, "unmoved+edited": 0, "unmoved": 0}
for path, sha, is_link, new, target, project in plan:
    cur = link_sha(os.path.join(clone, target)) if is_link else now[target]
    edited = cur != sha
    kind = ("moved+edited" if edited else "moved") if new else ("unmoved+edited" if edited else "unmoved")
    counts[kind] += 1
    if new or edited:
        rows.append((kind, path, target, project))

with open(os.path.join(out, f"{repo}-files.tsv"), "w", encoding="utf-8", errors="surrogateescape") as f:
    f.write("change\told_path\tnew_path\tproject\n")
    for r in sorted(rows, key=lambda x: (x[2], x[1])):
        f.write("\t".join(r) + "\n")

with open(os.path.join(out, f"{repo}-projects.tsv"), "w", encoding="utf-8") as f:
    f.write("project\tkind\tdomain\tgroup\told_dir\tnew_dir\n")
    for old, new in sorted(moves.items(), key=lambda kv: kv[1]):
        parts = new.split("/")           # public/<src|tests>/[group/]domain/Project
        kind = parts[1]; mid = parts[2:-1]
        domain = mid[-1]; group = mid[0] if len(mid) > 1 else ""
        f.write(f"{parts[-1]}\t{kind}\t{domain}\t{group}\t{old}\t{new}\n")

untracked = [l[3:] for l in git("status", "--short", "-z").decode("utf-8", "surrogateescape").split("\0") if l.startswith("?? ")]
with open(os.path.join(out, f"{repo}-new-files.tsv"), "w", encoding="utf-8", errors="surrogateescape") as f:
    f.write("path\n")
    for u in sorted(untracked):
        f.write(u + "\n")

print(f"{repo}: {len(files)} files at HEAD, {len(moves)} projects moved")
print("  " + ", ".join(f"{k} {v}" for k, v in counts.items()) + f", new untracked entries {len(untracked)}")
for kind, a, b in problems[:10]:
    print(f"  PROBLEM {kind}: {a} -> {b}")
if problems:
    print(f"  {len(problems)} problems"); sys.exit(1)
print("  every file under a moved project is at its new path and gone from the old one")
