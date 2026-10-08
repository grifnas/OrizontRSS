# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.0**, publicată la 19 septembrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.0](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.0)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.0](https://github.com/grifnas/OrizontRSS/tree/v1.6.0)
- Instalator autonom: OrizontSetup-1.6.0.exe
- Variantă portabilă: Orizont-RSS-1.6.0-win-x64.zip
- Arhivă sursă: Orizont-RSS-1.6.0-source.zip
- Fișierele SHA-256 sunt atașate release-ului împreună cu pachetele.

Acestea sunt fișierele publicate. Nu șterge și nu înlocui release-ul sau tagul pentru a curăța copiile locale.

## Sursa și executabilul local

Ramura main conține schimbările nepublicate de după v1.6.0. Pregătirea locală este consemnată în commitul `3a176e9` (candidatul aplicației 1.6.1 și curățarea snapshoturilor 1.5.x); utilitarul de monitorizare a descărcărilor este separat în `40243b6`. Commiturile sunt numai locale, fără push. Nu s-au creat distribuție, installer, arhivă sau release; versiunea publică rămâne v1.6.0.

Este în pregătire candidatul local **1.6.1**. Executabilul său de test va afișa versiunea de fișier 1.6.1.0, dar nu va fi un release și nu va fi publicat ori distribuit; nu este pachetul public v1.6.0. Versiunea publică rămâne v1.6.0. Înaintea oricărei distribuții viitoare sunt necesare acceptările rămase, verificarea tuturor limbilor, actualizarea notelor/manifeste/paginilor și o cerere expresă separată pentru distribuție.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. Au rămas notele și manifestele 1.6.0, precum și tot codul și documentația de după release. Tagurile Git și release-urile GitHub anterioare nu au fost șterse; ele păstrează sursele și pachetele istorice.

## WinGet

Manifestele locale păstrate sunt numai cele din packaging/winget/Grifnas.OrizontRSS/1.6.0/. PR-ul asociat este [microsoft/winget-pkgs#431971](https://github.com/microsoft/winget-pkgs/pull/431971). Starea din acest fișier este un reper documentar, nu o verificare live a PR-ului.

## Regula pentru documentația de release

Fișierul docs/RELEASE-STATUS.json este registrul canonic al ultimei versiuni publicate și al stării sursei main. Înaintea publicării viitoare, actualizează registrul, README.md, CHANGELOG.md, notele de versiune, manifeste WinGet și paginile publice; apoi rulează verify-release-consistency.ps1 și verificările de publicare. Modificările nepublicate trebuie etichetate clar ca atare până la apariția unui release real.
