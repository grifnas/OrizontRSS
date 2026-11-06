# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.6**, publicată la 6 noiembrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.6](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.6)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.6](https://github.com/grifnas/OrizontRSS/tree/v1.6.6)
- Commitul tagului v1.6.6: `736940b`. Ramura `main` conține acest commit și actualizările de documentație ulterioare.
- Installer autonom offline: `OrizontSetup-1.6.6.exe` — 252.914.752 bytes; SHA-256 `D7AC63B5A6B161F9BD20E8A41C53C1143FB00DD39A33C75926B49E3C5A7FEE9D`.
- Variantă portabilă: `Orizont-RSS-1.6.6-win-x64.zip` — 91.118.089 bytes; SHA-256 `93DFB877D6EF6A66A83FD8C2C11F3EB46D3D027CFD98F9C9E5F20B43023D82E1`.
- Arhivă sursă: `Orizont-RSS-1.6.6-source.zip` — 14.731.151 bytes; SHA-256 `1B403C251C77FFF725BB22F51290242652F02CAF4F36477B93AC1A369AEF1BD6`.
- Cele trei fișiere `.sha256` sunt atașate alături de pachete.

Pachetele includ .NET și Windows Desktop 8.0.31; nu cer instalarea separată a .NET Desktop Runtime. WebReader folosește Microsoft Edge WebView2 Runtime. Hash-urile locale au fost comparate cu digesturile afișate de GitHub.

## Acceptare și limite cunoscute

A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; nu sunt declarate trecute. Auditul NuGet, rulat la 19 octombrie 2026, nu a raportat pachete vulnerabile. Aceste limite sunt descrise în notele publice [RELEASE-NOTES-1.6.6.md](RELEASE-NOTES-1.6.6.md).

## Pagina de prezentare și documentele

Cele opt pagini `docs/index*.html`, linkurile directe spre installerul și arhiva portabilă versionate, precum și documentele de stare au fost sincronizate cu v1.6.6 după publicarea release-ului și împinse pe `main`. Până la propagarea GitHub Pages, pagina publică poate afișa încă versiunea anterioară. Orice build local ulterior nu este pachetul public al tagului și **nu este pachetul public v1.6.6**.

## WinGet

Manifestele locale curente sunt numai cele din `packaging/winget/Grifnas.OrizontRSS/1.6.6/`; URL-ul installerului și hash-ul arhivei portabile corespund release-ului public. `winget validate` a trecut la pregătirea manifestelor; la reverificarea din 8 octombrie, comanda `winget.exe` nu a putut porni din mediul izolat (`SEC_E_NO_CREDENTIALS`/WindowsApps), deci validarea nu a putut fi repetată în această sesiune. Snapshotul local 1.6.0 a fost eliminat din arborele curent; istoricul său rămâne în Git.

PR-ul pentru 1.6.6 este [microsoft/winget-pkgs#462294](https://github.com/microsoft/winget-pkgs/pull/462294). La 28 octombrie 2026 aștepta aprobarea unui moderator voluntar, iar semnarea CLA pe contul `grifnas` nu era confirmată. PR-ul [#431971](https://github.com/microsoft/winget-pkgs/pull/431971) (1.6.4) a fost închis ca înlocuit. Până la fuziunea PR-ului, disponibilitatea în catalogul WinGet nu este confirmată.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. După publicarea 1.6.1 a fost eliminat și snapshotul local 1.6.0 al manifestelor; release-urile și tagurile istorice nu au fost șterse și rămân disponibile pe GitHub și în istoricul Git.

## Regula pentru documentația de release

Fișierul `docs/RELEASE-STATUS.json` este registrul canonic al ultimei versiuni publicate și al stării sursei `main`. Înaintea următorului release, actualizează registrul, `README.md`, `CHANGELOG.md`, notele, manifestele WinGet și paginile publice; apoi rulează `verify-release-consistency.ps1` și verificările de publicare. Modificările care apar după un tag trebuie delimitate clar de pachetele imutabile ale acelui release.
