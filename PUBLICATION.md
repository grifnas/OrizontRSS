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

Ramura main conține schimbările nepublicate de după v1.6.0. Pregătirea candidatului aplicației 1.6.1 și curățarea snapshoturilor 1.5.x sunt în commitul local `3a176e9`; utilitarul de monitorizare a descărcărilor este separat în `40243b6`. Commiturile rămân locale, fără push.

Candidatul **1.6.1** are acum pachete pregătite local: `bin/Release/Orizont-RSS-1.6.1-win-x64.zip`, `bin/Release/Orizont-RSS-1.6.1-source.zip` și `bin/Release/OrizontSetup-1.6.1.exe`, fiecare cu fișier `.sha256` alăturat. Acestea nu sunt încă publicate; candidatul local nu este pachetul public până la atașarea fișierelor la un release GitHub. Release-ul oficial și linkurile publice rămân v1.6.0. Nu au fost create tag sau release GitHub și nu s-a făcut push.

- Portable: 92.550.743 bytes; SHA-256: `ebe60ba4c8bc1fab4109f957c430ff13783ccb1b09b36ff23697fd325485b34c`.
- Installer offline: 254.266.392 bytes; SHA-256: `59303ab255a00aa818790338c29024cd50999904cb29daea485cb8827e81bf4b`.
- Arhiva sursă a pachetului pregătit anterior avea 750 de intrări; după adăugarea manifestelor 1.6.1, numărul final se recalculează din tagul local înainte de predare. Suma și dimensiunea rămân în sidecar-ul `bin/Release/Orizont-RSS-1.6.1-source.zip.sha256`.
- Buildul aplicației include Microsoft .NET 8.0.31 și Windows Desktop 8.0.31; nu cere instalarea separată a .NET Runtime. WebReader folosește WebView2 disponibil în Windows/Edge.

Probele A02, A09, A07 și A11 sunt transferate explicit în ciclul următor; dacă 1.6.1 se publică înaintea lor, notele păstrează această limită și nu le declară trecute. Auditul online NuGet nu a putut fi efectuat (`NU1900`), deci nu este prezentat ca verificat. Pentru publicarea efectivă mai trebuie actualizate paginile publice și manifestele WinGet cu URL-urile/hash-urile release-ului, apoi create tagul și release-ul și împinse commiturile. Paginile și manifestele rămân momentan la v1.6.0; nu s-a făcut nicio operație publică.

Manifestele locale includ în acest moment versiunea publică 1.6.0 și candidatul 1.6.1. Cele patru fișiere 1.6.1 sunt pregătite în `packaging/winget/Grifnas.OrizontRSS/1.6.1/`, cu URL-ul viitorului pachet și hash-ul portabil verificat; `winget validate` a reușit. La publicarea v1.6.1, elimină snapshotul local 1.6.0, conform validatorului de consistență.

La verificarea din 8 octombrie 2026, PR-ul existent [microsoft/winget-pkgs#431971](https://github.com/microsoft/winget-pkgs/pull/431971) era deschis și încă viza versiunea 1.6.0 pe ramura `submission/orizont-rss-1.5.3`. Actualizează acel PR după apariția release-ului 1.6.1; nu crea o dublură. Starea se poate schimba după verificarea datată.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. Au rămas notele și manifestele 1.6.0, precum și tot codul și documentația de după release. Tagurile Git și release-urile GitHub anterioare nu au fost șterse; ele păstrează sursele și pachetele istorice.

## WinGet

Manifestele locale păstrate sunt numai cele din packaging/winget/Grifnas.OrizontRSS/1.6.0/. PR-ul asociat este [microsoft/winget-pkgs#431971](https://github.com/microsoft/winget-pkgs/pull/431971). Starea din acest fișier este un reper documentar, nu o verificare live a PR-ului.

## Regula pentru documentația de release

Fișierul docs/RELEASE-STATUS.json este registrul canonic al ultimei versiuni publicate și al stării sursei main. Înaintea publicării viitoare, actualizează registrul, README.md, CHANGELOG.md, notele de versiune, manifeste WinGet și paginile publice; apoi rulează verify-release-consistency.ps1 și verificările de publicare. Modificările nepublicate trebuie etichetate clar ca atare până la apariția unui release real.
