#!/usr/bin/env python3
"""Проверяет rules.json по corpus.json: документ и пункт найдены, цитата есть в тексте пункта.

Использование: python tools/verify_rules.py <папка с rules.json и corpus.json>
Статусы совпадают с плагином: актуально / проверить / устарело.
"""
import json
import re
import sys
from pathlib import Path


def key(text):
    return "".join(ch.lower() for ch in (text or "") if ch.isalnum())


def normalize(text):
    return " ".join((text or "").lower().replace(" ", " ").split())


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        return 2

    folder = Path(sys.argv[1])
    rules = json.loads((folder / "rules.json").read_text(encoding="utf-8")).get("rules", [])
    corpus = json.loads((folder / "corpus.json").read_text(encoding="utf-8"))
    documents = corpus.get("documents", [])

    bad = 0
    for rule in rules:
        document_key = key(rule.get("document"))
        docs = [d for d in documents if document_key and document_key in key(d.get("name"))]
        number = re.search(r"\d+(?:\.\d+)+", rule.get("clause") or "")
        clause = None
        if docs and number:
            clause = next((c for d in docs for c in d["clauses"] if c["number"] == number.group(0)), None)

        quote = normalize((rule.get("quote") or "").strip(" .…"))
        if clause is None:
            status = "устарело (нет документа или пункта)"
        elif quote and quote in normalize(clause["text"]):
            status = "актуально"
        else:
            status = "проверить (цитаты нет в пункте)"

        if not status.startswith("актуально"):
            bad += 1
        print(f"{rule.get('id')}: {status}")

    print(f"Правил: {len(rules)}, требуют внимания: {bad}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
