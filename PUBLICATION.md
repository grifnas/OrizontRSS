# Publicare și stare de lucru — Orizont RSS

## Ultimul release public

- Versiune: **v1.6.9**, publicată la 20 noiembrie 2026.
- Release oficial: [github.com/grifnas/OrizontRSS/releases/tag/v1.6.9](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.9)
- Pagina de prezentare: [grifnas.github.io/OrizontRSS](https://grifnas.github.io/OrizontRSS/)
- Codul exact al versiunii: [tagul v1.6.9](https://github.com/grifnas/OrizontRSS/tree/v1.6.9)
- Commitul tagului v1.6.9: `8b6392e`, din care s-au construit pachetele. Ramura `main` conține acest commit și actualizările ulterioare, de documentație și de manifeste.
- Installer autonom offline: `OrizontSetup-1.6.9.exe` — 252.992.997 bytes; SHA-256 `DE82C2DF427A3840D4892D002E03457ECBFEFDC7370CE33B8FE39C24986309CB`.
- Variantă portabilă: `Orizont-RSS-1.6.9-win-x64.zip` — 91.196.354 bytes; SHA-256 `75EB3DAFD772E481528D821F532819F1D3044BA7C70C0B94330399DBABD2ABEB`.
- Arhivă sursă: `Orizont-RSS-1.6.9-source.zip` — 14.734.927 bytes; SHA-256 `E25509A96327BAB07B89BE11C804F875673C11ED73A919D27B75150182B5CEB8`.
- Cele trei fișiere `.sha256` sunt atașate alături de pachete.

Pachetele includ .NET și Windows Desktop 8.0.31; nu cer instalarea separată a .NET Desktop Runtime. WebReader folosește Microsoft Edge WebView2 Runtime. Hash-urile locale au fost comparate cu digesturile afișate de GitHub.

## Acceptare și limite cunoscute

A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; nu sunt declarate trecute. Verificarea cu JAWS a instalatorului 1.6.8 (instalare, dezinstalare, refuzul folderelor nesigure) rămâne de făcut. Auditul NuGet din 15 noiembrie 2026 nu a raportat pachete vulnerabile. Aceste limite sunt descrise în notele publice ale release-ului v1.6.8 și în [RELEASE-NOTES-1.6.8.md](RELEASE-NOTES-1.6.8.md).

## Pagina de prezentare și documentele

Cele opt pagini `docs/index*.html`, ghidurile și linkurile directe spre installerul și arhiva portabilă versionate au fost aduse la v1.6.8 după publicarea release-ului. Până la propagarea GitHub Pages, pagina publică poate afișa încă versiunea anterioară. Orice build local ulterior nu este pachetul public al tagului și **nu este pachetul public v1.6.8**.

## WinGet

Manifestele WinGet din repository sunt cele din `packaging/winget/Grifnas.OrizontRSS/1.6.8/`; URL-ul installerului și hash-ul arhivei portabile corespund release-ului public v1.6.8. Ele nu sunt trimise încă la microsoft/winget-pkgs; pull request-ul de mai jos este pentru 1.6.7. `winget validate` a trecut la pregătirea manifestelor; la reverificarea din 8 octombrie, comanda `winget.exe` nu a putut porni din mediul izolat (`SEC_E_NO_CREDENTIALS`/WindowsApps), deci validarea nu a putut fi repetată în această sesiune. Snapshotul local 1.6.0 a fost eliminat din arborele curent; istoricul său rămâne în Git.

Pull request-ul [microsoft/winget-pkgs#462294](https://github.com/microsoft/winget-pkgs/pull/462294) a fost actualizat la 1.6.7 pe ramura `grifnas:submission/orizont-rss-1.6.5`; titlul este „New version: Grifnas.OrizontRSS version 1.6.7”. CLA-ul este semnat (`license/cla` trecut).

Starea la 10 noiembrie 2026: pașii 01–07, inclusiv scanarea installerelor, au trecut; pasul 08, testul de instalare, a eșuat la 8 noiembrie, iar pasul 09 a fost sărit. PR-ul poartă etichetele `Validation-Defender-Error` și `Needs-Author-Feedback` și așteaptă răspunsul autorului; moderatorii nu au început revizuirea. Jurnalul intern al testului nu este accesibil; cauza probabilă, neconfirmată, este blocarea executabilului nesemnat de Microsoft Defender. La 10 noiembrie 2026, 14:54 UTC, autorul a răspuns în PR: arhiva publicată are hash-ul din manifest, scanarea locală cu Microsoft Defender (definiții 1.463.226.0) nu găsește nicio amenințare, iar comentariul cere reluarea validării. Rezultatul reluării nu este cunoscut. Pas rămas, numai dacă validarea eșuează din nou: trimiterea arhivei portabile la Microsoft pentru analiză ca fals pozitiv.

## Curățarea copiilor istorice

Au fost eliminate din arborele curent notele de versiune 1.5.2, 1.5.3 și 1.5.4 și copiile locale ale manifestelor WinGet 1.5.3/1.5.4. După publicarea 1.6.1 a fost eliminat și snapshotul local 1.6.0 al manifestelor; release-urile și tagurile istorice nu au fost șterse și rămân disponibile pe GitHub și în istoricul Git.

## Regula pentru documentația de release

Fișierul `docs/RELEASE-STATUS.json` este registrul canonic al ultimei versiuni publicate și al stării sursei `main`. Înaintea următorului release, actualizează registrul, `README.md`, `CHANGELOG.md`, notele, manifestele WinGet și paginile publice; apoi rulează `verify-release-consistency.ps1` și verificările de publicare. Modificările care apar după un tag trebuie delimitate clar de pachetele imutabile ale acelui release.
