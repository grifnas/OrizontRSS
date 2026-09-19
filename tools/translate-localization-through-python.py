"""Translate missing or untranslated UI resource values via the existing public Google endpoint.

This is a development fallback for environments where PowerShell's TLS stack cannot
establish a connection. It sends only static UI strings, never feeds or article data.
"""

from __future__ import annotations

import argparse
import collections
import html
import json
import pathlib
import re
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET


ROOT = pathlib.Path(__file__).resolve().parent.parent
RESOURCES = ROOT / "Resources"
CULTURES = {
    "en-US": "en",
    "es-ES": "es",
    "fr-FR": "fr",
    "de-DE": "de",
    "pt-BR": "pt",
    "hu-HU": "hu",
    "it-IT": "it",
}
PROTECTED_TERMS = (
    "Orizont",
    "Gemini",
    "SAPI5",
    "OPML",
    "RSS",
    "Ctrl",
    "Shift",
    "Alt",
)
PLACEHOLDER = re.compile(r"\{[^{}]+\}|\\n")

# Google occasionally rewrites numbered placeholders despite marker protection.
# Keep this reviewed wording as a safe fallback for the long Hungarian duplicate prompt.
MANUAL_OVERRIDES = {
    "hu-HU": {
        "Aceste feeduri au adrese diferite, dar": (
            "Ezeknek a hírforrásoknak eltérő a címe, de a kisebb feed {1} cikke közül "
            "{0} közös, azaz {2}%.\\n\\nELSŐ: {3}\\nMappa: {4}\\nCím: {5}"
            "\\n\\nMÁSODIK: {6}\\nMappa: {7}\\nCím: {8}\\n\\n"
            "Ha nem szeretnéd összevonni a feedeket, válaszd mindkettő megtartását."
        ),
        "Oglindire NewsBlur încheiată. Feeduri trimise:": (
            "A NewsBlur-tükrözés befejeződött. Elküldött feedek: {0}, sikertelen: {1}, "
            "létrehozott mappák: {2}, helyben importált feedek: {3}, elküldött metaadatok: {4}, "
            "átvett elemek: {5}, az Orizont RSS-ben feloldott ütközések: {6}, NewsBlurral feloldott "
            "ütközések: {7}, elhalasztottak: {8}, ütközési hibák: {9}, sikeres "
            "NewsBlur-leiratkozások: {10}, sikertelenek: {11}, helyben törölt feedek és cikkek "
            "a NewsBlurban történt törlés után: {12} és {13}."
        ),
    }
}


def translate_batch(texts: list[str], language: str) -> list[str]:
    chunks: list[str] = []
    restore_maps: list[tuple[list[tuple[str, str]], list[tuple[str, str]], str]] = []
    for index, original in enumerate(texts):
        terms: list[tuple[str, str]] = []
        value = original
        for term in PROTECTED_TERMS:
            if term in value:
                marker = f"ZXQTERM{len(terms):02d}QXZ"
                value = value.replace(term, marker)
                terms.append((marker, term))
        placeholders: list[tuple[str, str]] = []

        def protect(match: re.Match[str]) -> str:
            marker = f"ZXQPH{len(placeholders):03d}QXZ"
            placeholders.append((marker, match.group(0)))
            return marker

        value = PLACEHOLDER.sub(protect, value)
        restore_maps.append((terms, placeholders, original))
        chunks.append(f"[[[ORZ_{index:04d}]]]\n{value}\n")
    chunks.append("[[[ORZ_END]]]")

    query = "\n".join(chunks)
    parameters = urllib.parse.urlencode(
        {"client": "gtx", "sl": "ro", "tl": language, "dt": "t", "q": query}
    )
    request = urllib.request.Request(
        "https://translate.googleapis.com/translate_a/single?" + parameters,
        headers={"User-Agent": "Mozilla/5.0"},
    )
    with urllib.request.urlopen(request, timeout=45) as response:
        payload = json.loads(response.read().decode("utf-8"))
    translated_block = "".join(
        segment[0] for segment in payload[0] if segment and segment[0]
    )

    results: list[str] = []
    for index, (terms, placeholders, original) in enumerate(restore_maps):
        start_marker = f"[[[ORZ_{index:04d}]]]"
        end_marker = (
            f"[[[ORZ_{index + 1:04d}]]]"
            if index + 1 < len(restore_maps)
            else "[[[ORZ_END]]]"
        )
        start = translated_block.find(start_marker)
        end = translated_block.find(end_marker, start + len(start_marker))
        if start < 0 or end <= start:
            raise RuntimeError(f"Translation markers were lost at item {index}.")
        value = translated_block[start + len(start_marker) : end].strip()
        for marker, replacement in reversed(placeholders):
            if marker not in value:
                raise RuntimeError(f"Placeholder marker {marker} was lost in: {original}")
            value = value.replace(marker, replacement)
        for marker, replacement in reversed(terms):
            if marker not in value:
                raise RuntimeError(f"Protected term marker {marker} was lost in: {original}")
            value = value.replace(marker, replacement)
        if collections.Counter(PLACEHOLDER.findall(original)) != collections.Counter(
            PLACEHOLDER.findall(value)
        ):
            raise RuntimeError(f"Placeholder mismatch for: {original}")
        results.append(value)
    return results


def update_resx(
    path: pathlib.Path,
    changes: dict[str, str],
    key_renames: dict[str, str],
) -> tuple[int, int]:
    source = path.read_bytes()
    has_bom = source.startswith(b"\xef\xbb\xbf")
    text = source.decode("utf-8-sig")
    newline = "\r\n" if "\r\n" in text else "\n"
    existing: set[str] = set()
    replacements: list[tuple[int, int, str]] = []
    data_pattern = re.compile(r"<data\b([^>]*)>(.*?)</data>", re.DOTALL)
    value_pattern = re.compile(r"(<value\b[^>]*>).*?(</value>)", re.DOTALL)

    for match in data_pattern.finditer(text):
        name = re.search(r'\bname="([^"]*)"', match.group(1))
        if not name:
            continue
        raw_key = html.unescape(name.group(1))
        key = key_renames.get(raw_key, raw_key)
        existing.add(key)
        if raw_key != key or any(character in raw_key for character in "\r\n\t"):
            replacements.append(
                (
                    match.start(1) + name.start(1),
                    match.start(1) + name.end(1),
                    encode_attribute(key),
                )
            )
        if key not in changes:
            continue
        body = match.group(2)
        if not value_pattern.search(body):
            raise RuntimeError(f"Missing value element for {key} in {path.name}.")
        encoded = html.escape(changes[key], quote=False)
        body = value_pattern.sub(lambda m: m.group(1) + encoded + m.group(2), body, count=1)
        replacements.append((match.start(2), match.end(2), body))

    for start, end, replacement in sorted(replacements, reverse=True):
        text = text[:start] + replacement + text[end:]

    missing = sorted((key for key in changes if key not in existing), key=str.casefold)
    if missing:
        closing = text.rfind("</root>")
        if closing < 0:
            raise RuntimeError(f"Missing root element in {path.name}.")
        nodes = [
            f'  <data name="{encode_attribute(key)}" xml:space="preserve">'
            f"<value>{html.escape(changes[key], quote=False)}</value></data>"
            for key in missing
        ]
        text = text[:closing].rstrip() + newline + newline.join(nodes) + newline + text[closing:]

    path.write_bytes((b"\xef\xbb\xbf" if has_bom else b"") + text.encode("utf-8"))
    return len(missing), len(replacements)


def encode_attribute(value: str) -> str:
    return (
        html.escape(value, quote=True)
        .replace("\r", "&#13;")
        .replace("\n", "&#10;")
        .replace("\t", "&#9;")
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--culture", choices=tuple(CULTURES), action="append")
    arguments = parser.parse_args()
    selected = arguments.culture or list(CULTURES)
    base_root = ET.parse(RESOURCES / "UiStrings.resx").getroot()
    base = {
        node.attrib["name"]: node.findtext("value", default="")
        for node in base_root.findall("data")
    }

    for culture in selected:
        path = RESOURCES / f"UiStrings.{culture}.resx"
        target_root = ET.parse(path).getroot()
        target = {
            node.attrib["name"]: node.findtext("value", default="")
            for node in target_root.findall("data")
        }
        normalized_base: dict[str, list[str]] = {}
        for key in base:
            normalized_base.setdefault(" ".join(key.split()), []).append(key)
        key_renames: dict[str, str] = {}
        for key, value in list(target.items()):
            if key in base:
                continue
            matches = normalized_base.get(" ".join(key.split()), [])
            if len(matches) == 1:
                key_renames[key] = matches[0]
                target[matches[0]] = value
        candidates = [
            key
            for key in sorted(base, key=str.casefold)
            if key not in target
            or target[key] == base[key]
            or any(
                key.startswith(prefix) and target[key] != value
                for prefix, value in MANUAL_OVERRIDES.get(culture, {}).items()
            )
        ]
        translated: dict[str, str] = {}
        batch: list[str] = []
        batch_length = 0

        def flush(keys: list[str]) -> None:
            if keys:
                translated.update(zip(keys, translate_batch([base[key] for key in keys], CULTURES[culture])))
                print(f"{culture}: {len(translated)}/{len(candidates)} translated", flush=True)

        for key in candidates:
            if key.startswith(tuple(MANUAL_OVERRIDES.get(culture, {}))):
                translated[key] = next(
                    value
                    for prefix, value in MANUAL_OVERRIDES[culture].items()
                    if key.startswith(prefix)
                )
                continue
            size = len(base[key]) + 35
            if batch and batch_length + size > 2400:
                flush(batch)
                batch, batch_length = [], 0
            batch.append(key)
            batch_length += size
        flush(batch)
        added, refreshed = update_resx(path, translated, key_renames)
        renamed = len(key_renames)
        unchanged = sum(translated[key] == base[key] for key in translated)
        print(
            f"{culture}: added {added}; refreshed {refreshed}; repaired keys {renamed}; "
            f"unchanged by translator {unchanged}",
            flush=True,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
