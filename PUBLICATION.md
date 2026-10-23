# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.4**, publicată la 23 octombrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.4](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.4)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.4](https://github.com/grifnas/OrizontRSS/tree/v1.6.4)
- Commitul tagului v1.6.4: `c1ca351` (hash complet în `git rev-parse v1.6.4`). Ramura `main` conține acest commit și actualizările de documentație ulterioare.
- Installer autonom offline: `OrizontSetup-1.6.4.exe` — 252.541.976 bytes; SHA-256 `2a9efba4f865f7ff71b87445f9c827c0f7004d52c990ab0b67c56071556e2aac`.
- Variantă portabilă: `Orizont-RSS-1.6.4-win-x64.zip` — 90.748.275 bytes; SHA-256 `626dbcc188e5bf1a92de55f0e6858e4b7d2fe9fe2df6c6186423fc7e900e8727`.
- Arhivă sursă: `Orizont-RSS-1.6.4-source.zip` — 14.591.374 bytes; SHA-256 `0113317ea7a4eec8f66bf62f62f20204b58480f50a95d6d5e535b3ea7b7e45bb`.
- Cele trei fișiere `.sha256` sunt atașate alături de pachete. Pagina publică a release-ului afișează cele șase fișiere încărcate și cele două arhive automate ale sursei GitHub.

Pachetele includ .NET și Windows Desktop 8.0.31; nu cer instalarea separată a .NET Desktop Runtime. WebReader folosește Microsoft Edge WebView2 Runtime. Hash-urile locale au fost comparate cu digesturile afișate de GitHub.

## Acceptare și limite cunoscute

A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; nu sunt declarate trecute. Auditul NuGet, rulat la 19 octombrie 2026, nu a raportat pachete vulnerabile. Aceste limite sunt descrise în notele publice [RELEASE-NOTES-1.6.3.md](RELEASE-NOTES-1.6.3.md).

## Pagina de prezentare și documentele

Cele opt pagini `docs/index*.html`, linkurile directe spre installerul și arhiva portabilă versionate, precum și documentele de stare au fost sincronizate cu v1.6.3 după publicarea release-ului și împinse pe `main`. Până la propagarea GitHub Pages, pagina publică poate afișa încă versiunea anterioară. Orice build local ulterior nu este pachetul public al tagului și **nu este pachetul public v1.6.3**.

## WinGet

Manifestele locale curente sunt numai cele din `packaging/winget/Grifnas.OrizontRSS/1.6.3/`; URL-ul installerului și hash-ul arhivei portabile corespund release-ului public. `winget validate` a trecut la pregătirea manifestelor; la reverificarea din 8 octombrie, comanda `winget.exe` nu a putut porni din mediul izolat (`SEC_E_NO_CREDENTIALS`/WindowsApps), deci validarea nu a putut fi repetată în această sesiune. Snapshotul local 1.6.0 a fost eliminat din arborele curent; istoricul său rămâne în Git.

PR-ul existent [microsoft/winget-pkgs#431971](https://github.com/microsoft/winget-pkgs/pull/431971) este deschis și a fost actualizat la 1.6.3 pe ramura existentă `submission/orizont-rss-1.5.3`, fără PR duplicat. Până la actualizarea și fuziunea PR-ului, disponibilitatea în catalogul WinGet nu este confirmată.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. După publicarea 1.6.1 a fost eliminat și snapshotul local 1.6.0 al manifestelor; release-urile și tagurile istorice nu au fost șterse și rămân disponibile pe GitHub și în istoricul Git.

## Regula pentru documentația de release

Fișierul `docs/RELEASE-STATUS.json` este registrul canonic al ultimei versiuni publicate și al stării sursei `main`. Înaintea următorului release, actualizează registrul, `README.md`, `CHANGELOG.md`, notele, manifestele WinGet și paginile publice; apoi rulează `verify-release-consistency.ps1` și verificările de publicare. Modificările care apar după un tag trebuie delimitate clar de pachetele imutabile ale acelui release.
