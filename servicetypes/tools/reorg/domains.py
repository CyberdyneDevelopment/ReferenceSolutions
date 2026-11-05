"""Which domain a project belongs to, and which parent folder a domain sits in.

The domain rules are the repository's own `domains.json` (the file TechCom reads to know a project's domains, TC-198):
an ordered list of {match, domain}, first match wins, applied to the project name with a trailing `.Tests` removed.
There is no second copy of them here, so the folders the move tool places projects in, the domain guides and
TechCom's consultants cannot disagree. A repository without domains.json, or a project no rule matches, is an error.

A domain is named after the service domain the code already calls itself, or, for the shared foundation, after the
concern it owns. GROUP below is the one thing domains.json does not say: which small, related domains share a parent
folder.
"""
import json
import os
import re

_cache = {}


def rules(repo):
    """The compiled rules of the repository at `repo` (the folder holding domains.json)."""
    repo = os.path.realpath(repo)
    if repo not in _cache:
        path = os.path.join(repo, "domains.json")
        if not os.path.isfile(path):
            raise SystemExit(f"{path} does not exist: the domain rules are the repository's domains.json")
        with open(path, encoding="utf-8-sig") as f:
            _cache[repo] = [(re.compile(r["match"]), r["domain"]) for r in json.load(f)["rules"]]
    return _cache[repo]


def domain_of(repo, name):
    """The domain of project `name` in the repository at `repo`, or None when no rule matches."""
    base = re.sub(r"\.Tests$", "", name)
    return next((d for rx, d in rules(repo) if rx.match(base)), None)


# Related small domains share a folder. Only the folder is shared: every project stays its own project.
# A domain not listed here sits directly under src/ and tests/.
GROUP = {}
for _g, _ds in {
    "foundation":    ["results", "collections", "messagelogging", "analyzers", "registration", "validation", "security", "abstractions", "services"],
    "identity":      ["authentication", "authorization", "multitenancy", "users"],
    "secrets":       ["secretmanagers", "credentials"],
    "observability": ["telemetry", "resiliency", "quality"],
    "ai":            ["agents", "aegis"],
    "tooling":       ["roslyn", "mcp", "devsession", "shells"],
}.items():
    for _d in _ds: GROUP[_d] = _g
