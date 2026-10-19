# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.2**, publicată la 19 octombrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.2](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.2)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.2](https://github.com/grifnas/OrizontRSS/tree/v1.6.2)
- Commitul tagului v1.6.2: `88bc4305987dbc75f0e398cfbeacd11372ffd5bf`. Ramura `main` conține acest commit și actualizările de documentație ulterioare.
- Installer autonom offline: `OrizontSetup-1.6.2.exe` — 252.152.856 bytes; SHA-256 `b489755451536b60946461369e78dc10ee8fae83ea5ef81267a99311aaedf815`.
- Variantă portabilă: `Orizont-RSS-1.6.2-win-x64.zip` — 90.362.318 bytes; SHA-256 `717e139ecbb7014d94d51efe106a6e335e1fbc8decd642e2f1ce5babf5148708`.
- Arhivă sursă: `Orizont-RSS-1.6.2-source.zip` — 13.745.246 bytes; SHA-256 `b9bf295912ed380005a6387897600df642908d6c70fbea7a7e4e37a304c8121d`.
- Cele trei fișiere `.sha256` sunt atașate alături de pachete. Pagina publică a release-ului afișează cele șase fișiere încărcate și cele două arhive automate ale sursei GitHub.

Pachetele includ .NET și Windows Desktop 8.0.31; nu cer instalarea separată a .NET Desktop Runtime. WebReader folosește Microsoft Edge WebView2 Runtime. Hash-urile locale au fost comparate cu digesturile afișate de GitHub.

## Acceptare și limite cunoscute

A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; nu sunt declarate trecute. Auditul NuGet, rulat la 19 octombrie 2026, nu a raportat pachete vulnerabile. Aceste limite sunt descrise în notele publice [RELEASE-NOTES-1.6.2.md](RELEASE-NOTES-1.6.2.md).

## Pagina de prezentare și documentele

Cele opt pagini `docs/index*.html`, linkurile directe spre installerul și arhiva portabilă versionate, precum și documentele de stare au fost sincronizate cu v1.6.2 după publicarea release-ului și împinse pe `main`. Până la propagarea GitHub Pages, pagina publică poate afișa încă versiunea anterioară. Orice build local ulterior nu este pachetul public al tagului și **nu este pachetul public v1.6.2**.

## WinGet

Manifestele locale curente sunt numai cele din `packaging/winget/Grifnas.OrizontRSS/1.6.2/`; URL-ul installerului și hash-ul arhivei portabile corespund release-ului public. `winget validate` a trecut la pregătirea manifestelor; la reverificarea din 8 octombrie, comanda `winget.exe` nu a putut porni din mediul izolat (`SEC_E_NO_CREDENTIALS`/WindowsApps), deci validarea nu a putut fi repetată în această sesiune. Snapshotul local 1.6.0 a fost eliminat din arborele curent; istoricul său rămâne în Git.

PR-ul existent [microsoft/winget-pkgs#431971](https://github.com/microsoft/winget-pkgs/pull/431971) este deschis și încă vizează 1.6.0; trebuie actualizat la 1.6.2 pe ramura existentă `submission/orizont-rss-1.5.3`, fără PR duplicat. Integrarea GitHub conectată a refuzat scrierea în ramura forkului (HTTP 403); niciun fișier nu a fost modificat acolo. Până la actualizarea și fuziunea PR-ului, disponibilitatea în catalogul WinGet nu este confirmată.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. După publicarea 1.6.1 a fost eliminat și snapshotul local 1.6.0 al manifestelor; release-urile și tagurile istorice nu au fost șterse și rămân disponibile pe GitHub și în istoricul Git.

## Regula pentru documentația de release

Fișierul `docs/RELEASE-STATUS.json` este registrul canonic al ultimei versiuni publicate și al stării sursei `main`. Înaintea următorului release, actualizează registrul, `README.md`, `CHANGELOG.md`, notele, manifestele WinGet și paginile publice; apoi rulează `verify-release-consistency.ps1` și verificările de publicare. Modificările care apar după un tag trebuie delimitate clar de pachetele imutabile ale acelui release.
