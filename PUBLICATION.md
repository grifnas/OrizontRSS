# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.1**, publicată la 8 octombrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.1](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.1)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.1](https://github.com/grifnas/OrizontRSS/tree/v1.6.1)
- Commitul tagului și al ramurii `main`: `161eeff6db6d931130bf79ae6ca3f5e09ea7636b`.
- Installer autonom offline: `OrizontSetup-1.6.1.exe` — 254.266.392 bytes; SHA-256 `59303ab255a00aa818790338c29024cd50999904cb29daea485cb8827e81bf4b`.
- Variantă portabilă: `Orizont-RSS-1.6.1-win-x64.zip` — 92.550.743 bytes; SHA-256 `ebe60ba4c8bc1fab4109f957c430ff13783ccb1b09b36ff23697fd325485b34c`.
- Arhivă sursă: `Orizont-RSS-1.6.1-source.zip` — 13.648.907 bytes; SHA-256 `82f80902638bd00f10ae4d51c0dee3b2ab7441fac260f152d097ac6ad8dfb230`.
- Cele trei fișiere `.sha256` sunt atașate alături de pachete. Pagina publică a release-ului afișează cele șase fișiere încărcate și cele două arhive automate ale sursei GitHub.

Pachetele includ .NET și Windows Desktop 8.0.31; nu cer instalarea separată a .NET Desktop Runtime. WebReader folosește Microsoft Edge WebView2 Runtime. Hash-urile locale au fost comparate cu digesturile afișate de GitHub.

## Acceptare și limite cunoscute

A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; nu sunt declarate trecute. Auditul online NuGet nu a putut fi efectuat (`NU1900`). Aceste limite sunt descrise în notele publice [RELEASE-NOTES-1.6.1.md](RELEASE-NOTES-1.6.1.md).

## Pagina de prezentare și documentele

Cele opt pagini `docs/index*.html`, linkurile directe spre installerul și arhiva portabilă versionate, precum și documentele de stare au fost sincronizate local cu v1.6.1. Ele trebuie împinse pe `main` și apoi verificate live; până la propagarea GitHub Pages, pagina publică poate afișa încă versiunea 1.6.0. Orice build local ulterior nu este pachetul public al tagului și **nu este pachetul public v1.6.1**.

## WinGet

Manifestele locale curente sunt numai cele din `packaging/winget/Grifnas.OrizontRSS/1.6.1/`; URL-ul installerului și hash-ul arhivei portabile corespund release-ului public. `winget validate` a trecut la pregătirea manifestelor; la reverificarea din 8 octombrie, comanda `winget.exe` nu a putut porni din mediul izolat (`SEC_E_NO_CREDENTIALS`/WindowsApps), deci validarea nu a putut fi repetată în această sesiune. Snapshotul local 1.6.0 a fost eliminat din arborele curent; istoricul său rămâne în Git.

PR-ul existent [microsoft/winget-pkgs#431971](https://github.com/microsoft/winget-pkgs/pull/431971) este deschis și încă vizează 1.6.0; trebuie actualizat la 1.6.1 pe ramura existentă `submission/orizont-rss-1.5.3`, fără PR duplicat. Integrarea GitHub conectată a refuzat scrierea în ramura forkului (HTTP 403); niciun fișier nu a fost modificat acolo. Până la actualizarea și fuziunea PR-ului, disponibilitatea în catalogul WinGet nu este confirmată.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. După publicarea 1.6.1 a fost eliminat și snapshotul local 1.6.0 al manifestelor; release-urile și tagurile istorice nu au fost șterse și rămân disponibile pe GitHub și în istoricul Git.

## Regula pentru documentația de release

Fișierul `docs/RELEASE-STATUS.json` este registrul canonic al ultimei versiuni publicate și al stării sursei `main`. Înaintea următorului release, actualizează registrul, `README.md`, `CHANGELOG.md`, notele, manifestele WinGet și paginile publice; apoi rulează `verify-release-consistency.ps1` și verificările de publicare. Modificările care apar după un tag trebuie delimitate clar de pachetele imutabile ale acelui release.
