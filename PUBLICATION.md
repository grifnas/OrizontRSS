# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.7**, publicată la 8 noiembrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.7](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.7)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.7](https://github.com/grifnas/OrizontRSS/tree/v1.6.7)
- Commitul tagului v1.6.7: `be49fba`. Ramura `main` conține acest commit și actualizările de documentație ulterioare.
- Installer autonom offline: `OrizontSetup-1.6.7.exe` — 252.922.944 bytes; SHA-256 `F8F1D3F3C8D98961FB10D0C5AE9F0A91F23DDAB85B6D07A24E7F93E3D208028C`.
- Variantă portabilă: `Orizont-RSS-1.6.7-win-x64.zip` — 91.125.522 bytes; SHA-256 `0AD4F53C7EBCDA134668EBE1C34B5C02032571F4B6AB6EED932863B2C5195554`.
- Arhivă sursă: `Orizont-RSS-1.6.7-source.zip` — 14.744.007 bytes; SHA-256 `C04497A2D8EEA099A71B931E3630DE40C739CFCC22218556D307C173E2BC8BBC`.
- Cele trei fișiere `.sha256` sunt atașate alături de pachete.

Pachetele includ .NET și Windows Desktop 8.0.31; nu cer instalarea separată a .NET Desktop Runtime. WebReader folosește Microsoft Edge WebView2 Runtime. Hash-urile locale au fost comparate cu digesturile afișate de GitHub.

## Acceptare și limite cunoscute

A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; nu sunt declarate trecute. Auditul NuGet, rulat la 8 noiembrie 2026 și repetat la 10 noiembrie, nu a raportat pachete vulnerabile. Aceste limite sunt descrise în notele publice [RELEASE-NOTES-1.6.7.md](RELEASE-NOTES-1.6.7.md).

## Pagina de prezentare și documentele

Cele opt pagini `docs/index*.html`, linkurile directe spre installerul și arhiva portabilă versionate, precum și documentele de stare au fost sincronizate cu v1.6.7 după publicarea release-ului și împinse pe `main`. Până la propagarea GitHub Pages, pagina publică poate afișa încă versiunea anterioară. Orice build local ulterior nu este pachetul public al tagului și **nu este pachetul public v1.6.7**.

## WinGet

Manifestele WinGet din repository sunt cele din `packaging/winget/Grifnas.OrizontRSS/1.6.7/`; URL-ul installerului și hash-ul arhivei portabile corespund release-ului public. `winget validate` a trecut la pregătirea manifestelor; la reverificarea din 8 octombrie, comanda `winget.exe` nu a putut porni din mediul izolat (`SEC_E_NO_CREDENTIALS`/WindowsApps), deci validarea nu a putut fi repetată în această sesiune. Snapshotul local 1.6.0 a fost eliminat din arborele curent; istoricul său rămâne în Git.

Pull request-ul [microsoft/winget-pkgs#462294](https://github.com/microsoft/winget-pkgs/pull/462294) a fost actualizat la 1.6.7 pe ramura `grifnas:submission/orizont-rss-1.6.5`; titlul este „New version: Grifnas.OrizontRSS version 1.6.7”. CLA-ul este semnat (`license/cla` trecut).

Starea la 10 noiembrie 2026: pașii 01–07, inclusiv scanarea installerelor, au trecut; pasul 08, testul de instalare, a eșuat la 8 noiembrie, iar pasul 09 a fost sărit. PR-ul poartă etichetele `Validation-Defender-Error` și `Needs-Author-Feedback` și așteaptă răspunsul autorului; moderatorii nu au început revizuirea. Jurnalul intern al testului nu este accesibil; cauza probabilă, neconfirmată, este blocarea executabilului nesemnat de Microsoft Defender. Pașii rămași autorului: trimiterea arhivei portabile la Microsoft pentru analiză ca fals pozitiv și un comentariu în PR pentru reluarea validării.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. După publicarea 1.6.1 a fost eliminat și snapshotul local 1.6.0 al manifestelor; release-urile și tagurile istorice nu au fost șterse și rămân disponibile pe GitHub și în istoricul Git.

## Regula pentru documentația de release

Fișierul `docs/RELEASE-STATUS.json` este registrul canonic al ultimei versiuni publicate și al stării sursei `main`. Înaintea următorului release, actualizează registrul, `README.md`, `CHANGELOG.md`, notele, manifestele WinGet și paginile publice; apoi rulează `verify-release-consistency.ps1` și verificările de publicare. Modificările care apar după un tag trebuie delimitate clar de pachetele imutabile ale acelui release.
