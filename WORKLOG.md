# Jurnalul intervențiilor Orizont RSS

Acest jurnal păstrează trasabilitatea modificărilor efective din proiect. Se notează acțiunile asupra fișierelor, nu raționamentul intern al agentului.

## 2026-09-05 — Restaurarea dependențelor în CI

- Scop: corectarea celui de-al doilea eșec al workflow-ului GitHub după separarea surselor instalatorului.
- Fișiere modificate: `.github/workflows/ci.yml`, `WORKLOG.md`.
- Cauză: `LocalizationSmoke` și `eSpeakSmoke` erau rulate cu `--no-restore` pe runner curat, fără `project.assets.json`.
- Remediere: cele două comenzi permit restaurarea normală a dependențelor.
- Verificări: ambele smoke testuri trecute local cu restaurare normală; workflow-ul GitHub va rula din nou după commit.
- Notă: avertizarea GitHub despre Node.js 20 rămâne informativă și nu blochează execuția.

## 2026-09-05 — Repararea workflow-ului CI

- Scop: eliminarea eșecurilor repetate GitHub Actions raportate prin email.
- Fișiere modificate: `CititorRSS.Jaws.csproj`, `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Cauză: globurile implicite SDK includeau sursele proiectului separat `packaging/installer` în compilarea aplicației principale, iar referința Windows Forms exista doar în proiectul instalatorului.
- Remediere: sursele C# și XAML ale instalatorului sunt excluse explicit din proiectul aplicației; instalatorul rămâne compilat prin `packaging/installer/OrizontSetup.csproj`.
- Verificări: build Release fără erori; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke trecut pentru en-US, es-ES, fr-FR, de-DE și pt-BR; eSpeakSmoke trecut (132 voci); verificarea localizării și ghidurilor trecută.
- Notificările de eroare au fost mutate în Coș înainte de remediere; următorul workflow GitHub va confirma remedierea pe server.

## 2026-09-05 — Localizarea dialogului de limbă pentru cititoarele de ecran

- Scop: eliminarea anunțului românesc „Limba instalatorului” atunci când interfața instalatorului este în engleză.
- Fișiere modificate: `packaging/installer/LanguageWindow.xaml.cs`, `WORKLOG.md`.
- Remediere: numele și textul de ajutor pentru lista de limbi și instrucțiunile dialogului sunt actualizate din textele limbii selectate.
- Verificări: publicare win-x64 fără erori.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Buton public pentru instalator

- Scop: facilitarea descărcării pentru utilizatorii finali printr-un buton direct către instalator.
- Fișiere modificate: `docs/index.html`, `docs/index.en.html`, `docs/index.es.html`, `docs/index.fr.html`, `docs/index.de.html`, `docs/index.pt.html`, `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Comportament: toate cele șase pagini GitHub Pages au buton principal „instalează” către `releases/latest/download/OrizontSetup.exe`; arhiva portabilă rămâne link alternativ către ultimul release. Butonul are stil vizibil și indicator de focus accesibil.
- Verificări: fiecare pagină conține ambele linkuri; URL-ul public al instalatorului răspunde HTTP 200 și livrează 161.696.832 bytes.
- Executabil de test: nu s-a modificat codul aplicației; instalatorul public verificat este `https://github.com/grifnas/OrizontRSS/releases/latest/download/OrizontSetup.exe`.

## 2026-09-05 — Localizarea etichetei folderului de instalare

- Scop: eliminarea anunțului mixt română-engleză pentru câmpul folderului de instalare.
- Fișiere modificate: `packaging/installer/MainWindow.xaml.cs`, `WORKLOG.md`.
- Remediere: numele de automatizare al grupului și textul de ajutor al câmpului sunt setate din limba selectată, nu rămân valorile românești din XAML.
- Verificări: publicare win-x64 fără erori.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Localizarea anunțului pentru pictograma desktop

- Scop: corectarea anunțului JAWS/NVDA care rămânea în română după alegerea limbii engleze.
- Fișiere modificate: `packaging/installer/MainWindow.xaml.cs`, `WORKLOG.md`.
- Remediere: `AutomationProperties.Name` și `AutomationProperties.HelpText` pentru opțiunea pictogramei sunt actualizate din textele limbii selectate, la fel ca textul vizibil.
- Verificări: publicare win-x64 fără erori; verificare statică a setării proprietăților localizate.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Corectarea tranziției dialogului de limbă

- Scop: remedierea închiderii neașteptate a instalatorului după apăsarea butonului `Continue`.
- Fișiere modificate: `packaging/installer/App.xaml.cs`, `WORKLOG.md`.
- Cauză identificată: modul implicit WPF `OnLastWindowClose` închidea aplicația între dialogul de limbă și fereastra principală.
- Remediere: `ShutdownMode.OnExplicitShutdown` pe durata dialogului, apoi revenire la `OnMainWindowClose` după afișarea instalatorului; excepțiile de pornire sunt jurnalizate în `%TEMP%\\OrizontSetup-startup.log`.
- Verificări: publicare win-x64 fără erori; test automat UI Automation — după `Continue`, procesul rămâne activ și titlul devine `Install Orizont RSS`.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Pregătirea publicării publice

- Scop: inițierea demersurilor pentru publicarea publică a Orizont RSS 1.5.3, fără publicare externă neautorizată.
- Fișiere adăugate: `PUBLICATION.md`, `docs/index.html`, `.github/workflows/ci.yml`, `.github/ISSUE_TEMPLATE/bug_report.yml`, `.github/ISSUE_TEMPLATE/feature_request.yml`.
- Fișiere actualizate: `README.md`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Conținut: plan etapizat GitHub Releases, GitHub Pages, WinGet și Microsoft Store; pagină publică accesibilă cu linkuri demonstrative; CI Windows pentru build, smoke tests, localizare și ghiduri; formulare de feedback fără date sensibile.
- Verificări: fișierele publice sunt prezente; funcționalitatea aplicației nu a fost modificată. Executabilul stabil pentru testare rămâne [`bin/Release/final-1.5.3-win-x64/Orizont.exe`](bin/Release/final-1.5.3-win-x64/Orizont.exe).

## 2026-09-05 — Publicarea sursei în GitHub

- Scop: încărcarea sursei Orizont RSS în depozitul public furnizat de utilizator.
- Depozit: `https://github.com/grifnas/OrizontRSS.git`, ramura `main`.
- Commit inițial publicat: `64bc049` (`Prepare Orizont RSS 1.5.3 for public release`).
- Actualizare: linkurile din `PUBLICATION.md` și `docs/index.html` au fost fixate pe depozitul real `grifnas/OrizontRSS`.
- Stare: sursa este publicată în `main`, iar tagul `v1.5.3` este publicat; Release-ul cu arhivele binare și activarea Pages rămân de finalizat în interfața GitHub.

## 2026-09-05 — Asset Windows publicat în Release

- Scop: verificarea publicării arhivei Windows pentru utilizatorii finali.
- Release: `https://github.com/grifnas/OrizontRSS/releases/tag/v1.5.3`.
- Rezultat: `Orizont-RSS-1.5.3-win-x64.zip` apare ca asset oficial, cu 85.194.997 bytes; API-ul GitHub confirmă `draft=false`, `prerelease=false`, `asset_count=1`.
- Verificare download: URL-ul public al asset-ului a răspuns `HTTP 200` și a transferat 85.194.997 bytes.

## 2026-09-05 — Activarea GitHub Pages

- Scop: publicarea unei pagini accesibile de prezentare și descărcare pentru Orizont RSS.
- Configurație: ramura `main`, directorul `/docs`.
- URL public: `https://grifnas.github.io/OrizontRSS/`.
- Verificare: `HTTP 200`; titlu `Orizont RSS — cititor RSS accesibil`; limba HTML `ro`; linkul către `releases/latest` prezent.

## 2026-09-05 — Pagini GitHub Pages multilingve

- Scop: extinderea paginii publice pentru toate limbile interfeței.
- Fișiere adăugate: `docs/index.en.html`, `docs/index.es.html`, `docs/index.fr.html`, `docs/index.de.html`, `docs/index.pt.html`.
- Fișier actualizat: `docs/index.html`, cu selector accesibil de limbă și indicarea paginii curente.
- Conținut: aceleași informații despre versiune, funcții, accesibilitate, confidențialitate, contribuții și descărcare, traduse pentru engleză, spaniolă, franceză, germană și portugheză.
- Verificare locală: toate cele șase pagini conțin `lang`, titlu, navigare de limbă și linkul către Release-ul stabil; codul aplicației nu a fost modificat.

## 2026-09-05 — Distribuția Orizont RSS 1.5.3

- Scop: generarea distribuției 1.5.3 la cererea expresă a utilizatorului.
- Fișiere modificate: `CititorRSS.Jaws.csproj`, `CHANGELOG.md`, `RELEASE-NOTES-1.5.3.md`, `BUILDING.md`, toate ghidurile HTML, `tests/LocalizationSmoke/Program.cs`, `tools/verify-all.ps1`, `tools/verify-distribution.ps1`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Verificări înainte de publicare: localizare 814/814 pentru en-US, es-ES, fr-FR, de-DE și pt-BR; ghiduri complete; CoreSmoke, LocalizationSmoke și eSpeakSmoke trecute; publish Release self-contained reușit.
- Verificarea distribuției: versiune fișier 1.5.3.0, versiune produs 1.5.3, 441 fișiere eSpeak și toate fișierele obligatorii prezente; fără PDB, date locale sau alte fișiere interzise.
- Arhive: `Orizont-RSS-1.5.3-win-x64.zip` — SHA-256 `7536070499680C2C2859C69C181BB8B6689EFA510E51C8E61E01DBD4EB48F6EB`; `Orizont-RSS-1.5.3-source.zip` — SHA-256 `F20BB0A12A38162847A6E0D0A13502EDBDC03EF3FD7EE25257A3A2375037FBA4`.

## 2026-09-05 — Confirmarea manuală a facilităților Orizont

- Scop: închiderea etapei de verificare după remedierea barei de stare a conversației AI.
- Fișiere modificate: `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Rezultat: utilizatorul a verificat bara de stare și celelalte facilități Orizont și a confirmat că funcționează corect.
- Executabil de test folosit: `bin/Release/test-ai-status-v1-win-x64/Orizont.exe`.

## 2026-09-04 — Anunțarea barei de stare în conversația AI

- Scop: remedierea situației în care mesajele din bara de stare a răspunsului Gemini erau vizibile, dar nu erau anunțate de cititoarele de ecran.
- Fișiere modificate: `AiResponseWindow.xaml`, `AiResponseWindow.xaml.cs`, `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Comportament: `SpeechStatus` este găzduit într-un StatusBar dedicat și actualizat prin `StatusAnnouncer`, inclusiv pentru stările vocale, erorile motorului vocal și maximizare/restaurare.
- Verificări: publish Release self-contained reușit; CoreSmoke, LocalizationSmoke și eSpeakSmoke trecute; localizare completă 814/814; ghidurile utilizatorului verificate.
- Executabil de test: `bin/Release/test-ai-status-v1-win-x64/Orizont.exe`.

## 2026-09-03 — Meniu contextual pentru conversația AI

- Scop: îmbunătățirea accesibilă a conversațiilor AI și a partajării, conform foii de parcurs.
- Fișiere modificate: `AiResponseWindow.xaml`, `AiResponseWindow.xaml.cs`, toate resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Comportament: Shift+F10 în răspunsul Gemini oferă grupuri pentru citire vocală, copiere și distribuire, plus comanda de focalizare pe întrebarea următoare; comenzile reutilizează acțiunile verificate ale ferestrei.
- Verificări: publish Release self-contained reușit; localizare completă 814/814 pentru en-US, es-ES, fr-FR, de-DE și pt-BR; CoreSmoke, LocalizationSmoke și eSpeakSmoke trecute.
- Executabil de test: `bin/Release/test-ai-context-v1-win-x64/Orizont.exe`.

## 2026-09-03 — Verificare de consolidare conform foii de parcurs

- Scop: verificarea automată a stării curente după etapa alertelor sonore, fără modificarea funcțiilor stabile.
- Fișiere modificate: `WORKLOG.md`.
- Verificări: publish Release self-contained reușit; ghidurile utilizatorului trecute pentru en, es, fr, de și pt; localizare 813/813 fără erori pentru toate cele cinci limbi suplimentare; CoreSmoke, LocalizationSmoke și eSpeakSmoke trecute; distribuția existentă 1.5.2 verificată cu 441 fișiere eSpeak și versiunile 1.5.2.0/1.5.2.
- Observație: verificarea manuală JAWS/NVDA rămâne necesară pentru confirmarea anunțurilor și alertelor sonore; nu s-a creat o distribuție nouă.
- Executabil de test: `bin/Release/test-roadmap-v1-win-x64/Orizont.exe`.

## 2026-09-03 — Control separat și limitare pentru alertele sonore

- Scop: consolidarea alertelor sonore aprobate, cu testare directă și fără repetare deranjantă la actualizări fără articole noi.
- Fișiere modificate: `AppSettings.cs`, `SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `SoundAlertService.cs`, `MainWindow.xaml.cs`, toate resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Comportament: controale separate pentru finalizare reușită, articole noi și erori; buton „Testează sunetul”; erorile au prioritate, iar succesul fără articole noi este limitat la o alertă la cinci minute. Setările sunt păstrate și la curățarea manuală.
- Verificări: publish Release self-contained reușit; localizare completă 813/813 pentru en-US, es-ES, fr-FR, de-DE și pt-BR; CoreSmoke, LocalizationSmoke și eSpeakSmoke trecute.
- Executabil de test: `bin/Release/test-alerts-v2-win-x64/Orizont.exe`.

## 2026-09-03 — Alerte sonore pentru actualizarea feedurilor

- Scop: feedback audio discret la finalizarea actualizării feedurilor, fără a întrerupe citirea vocală.
- Fișiere modificate: `AppSettings.cs`, `SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `SoundAlertService.cs`, `MainWindow.xaml.cs`, toate resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Comportament: sunetul Asterisk la actualizare reușită și Exclamation când există erori de feed; funcția poate fi dezactivată din Setări aplicație și este activată implicit pentru utilizatorii existenți.
- Verificări: publish Release self-contained reușit; localizare completă 808/808 pentru en-US, es-ES, fr-FR, de-DE și pt-BR; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke și eSpeakSmoke trecute.
- Executabil de test: `bin/Release/test-alerts-win-x64/Orizont.exe`.

## 2026-08-28 — Reguli de prevenire a regresiilor și citire vocală în Cititor Orizont

- Scop: restabilirea citirii vocale în fereastra separată „Cititor Orizont” și instituirea unui proces de lucru verificabil.
- Fișiere modificate: `ArticleReaderWindow.xaml`, `ArticleReaderWindow.xaml.cs`, `MainWindow.xaml.cs`, `README.md`, `docs/ROADMAP.md`, `AGENTS.md`.
- Schimbări funcționale: conectarea ferestrei Cititor Orizont la `SpeechService`; comenzi F9, Ctrl+Alt+V/P/S și Escape; opțiuni de citire în meniul contextual.
- Documentație: adăugarea foii de parcurs, a scurtăturilor și a regulilor obligatorii de prevenire a regresiilor.
- Verificări: compilare Release fără erori sau avertismente; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke trecut pentru en-US, es-ES, fr-FR, de-DE și pt-BR.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## Regula jurnalului

Orice intervenție viitoare asupra proiectului trebuie adăugată aici după aplicarea modificării și verificarea rezultatului. Intrarea trebuie să includă data, scopul, fișierele atinse, efectul, testele și calea executabilului de test, dacă a fost construit.

## 2026-09-05 — Prezentare publică și instrucțiuni de pornire

- Scop: clarificarea paginii publice a proiectului pentru utilizatori noi, fără modificarea funcțiilor stabile ale aplicației.
- Fișiere modificate: `README.md`, `WORKLOG.md`.
- Schimbări: linkuri directe către pagina GitHub Pages multilingvă, Release-ul 1.5.3, codul-sursă și Issues; pași de instalare rapidă; secțiune dedicată accesibilității JAWS/NVDA; instrucțiuni mai clare pentru raportarea problemelor și trimitere către documentația de publicare.
- Verificări: verificare statică a linkurilor și a secțiunilor Markdown; nu s-a modificat codul aplicației și nu s-a creat o distribuție nouă.
- Executabil de test: `bin/Release/final-1.5.3-win-x64/Orizont.exe` (neschimbat; disponibil pentru retestare manuală).

## 2026-09-05 — Manifest WinGet pregătit pentru revizie

- Scop: pregătirea instalării prin Windows Package Manager folosind Release-ul stabil existent, fără trimitere încă în depozitul Microsoft.
- Fișiere adăugate: `packaging/winget/Grifnas.OrizontRSS/1.5.3/Grifnas.OrizontRSS.yaml`, `Grifnas.OrizontRSS.locale.ro-RO.yaml`, `Grifnas.OrizontRSS.locale.en-US.yaml`, `Grifnas.OrizontRSS.installer.yaml`; `docs/ROADMAP.md` actualizat.
- Comportament protejat: arhiva și executabilul publicate nu au fost modificate; nu s-a creat o distribuție nouă.
- Metadate: identificator propus `Grifnas.OrizontRSS`, instalare portabilă x64 din arhiva oficială Release, alias `orizont-rss`, hash SHA-256 verificat local.
- Verificări: structură multi-fișier și câmpuri YAML verificate static; URL-ul, versiunea și hash-ul corespund Release-ului 1.5.3; nu s-a executat instalarea WinGet pe un Windows curat.
- Executabil de test: `bin/Release/final-1.5.3-win-x64/Orizont.exe` (neschimbat; disponibil pentru retestare manuală).

## 2026-09-05 — Verificare automată completă după pregătirea publicării

- Scop: verificarea automată a stării actuale înainte de următoarea etapă publică.
- Fișiere modificate: doar `WORKLOG.md`.
- Verificări: build Release fără erori sau avertismente; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke trecut pentru en-US, es-ES, fr-FR, de-DE și pt-BR; eSpeakSmoke trecut (132 voci); localizare 814/814 fără lipsuri, intrări extra sau erori; ghiduri HTML trecute; distribuția dezarhivată verificată cu versiunea fișierului `1.5.3.0`, versiunea produsului `1.5.3` și 441 fișiere eSpeak NG.
- Observație: `verify-all.ps1` așteaptă directorul distribuției, nu arhiva ZIP; verificarea finală a fost rerulată cu `bin/Release/final-1.5.3-win-x64` și a trecut.
- Rezultat: nu au fost identificate regresii automate; nu s-a creat o distribuție nouă.
- Executabil de test: `bin/Release/final-1.5.3-win-x64/Orizont.exe`.

## 2026-09-05 — Test WinGet local

- Scop: verificarea instalării reale din manifestul local WinGet, pas cu pas, cu utilizatorul.
- Rezultat: validarea manifestului a trecut; instalarea a raportat succes; `winget list` afișează `Orizont RSS 1.5.3`; executabilul instalat a fost găsit în `%LOCALAPPDATA%\Microsoft\WinGet\Packages\Grifnas.OrizontRSS__DefaultSource\Orizont.exe` și a pornit normal.
- Observație: aliasul `orizont-rss.exe` a fost creat în `%LOCALAPPDATA%\Microsoft\WinGet\Links`, dar nu a fost recunoscut în sesiunea PowerShell curentă deoarece acel folder nu era disponibil în `PATH`. Acest lucru nu a împiedicat pornirea aplicației.
- Comportament protejat: nu au fost atinse feedurile, articolele sau setările existente; nu s-a creat o distribuție nouă și nu s-a trimis manifestul în depozitul Microsoft.
- Executabil de test: copia instalată prin WinGet; executabilul local de referință rămâne `bin/Release/final-1.5.3-win-x64/Orizont.exe`.

## 2026-09-05 — Dezinstalare WinGet verificată

- Scop: confirmarea curățării instalării locale WinGet după testul de pornire.
- Rezultat: `winget uninstall --id Grifnas.OrizontRSS -e` a dezinstalat cu succes copia instalată prin manifest.
- Comportament protejat: testul nu a modificat feedurile, articolele sau setările aplicației și nu a afectat Release-ul public.
- Concluzie: ciclul local WinGet validare → instalare → pornire → dezinstalare este închis cu succes.
- Executabil de test: nu mai există copia WinGet instalată; executabilul local de referință rămâne `bin/Release/final-1.5.3-win-x64/Orizont.exe`.

## 2026-09-05 — Decizie: amânarea trimiterii WinGet

- Decizie: trimiterea manifestului în `microsoft/winget-pkgs` se amână până la pregătirea unui instalator Windows accesibil.
- Motiv: testul WinGet portabil a confirmat instalarea și pornirea, dar nu a creat o intrare evidentă în meniul Start, iar verificarea în regim non-administrator nu este încă efectuată.
- Ordine stabilită: instalator accesibil cu scurtătură și navigare din tastatură → test administrator/non-administrator → Pull Request WinGet.
- Comportament protejat: manifestul existent, Release-ul 1.5.3 și funcțiile aplicației rămân neschimbate; nu s-a creat o distribuție nouă.

## 2026-09-05 — Primul instalator Windows accesibil

- Scop: crearea unui instalator autonom, per utilizator, care descarcă Release-ul oficial, verifică hash-ul și creează scurtături în meniul Start.
- Fișiere adăugate: `packaging/installer/OrizontSetup.csproj`, `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`; `docs/PROJECT-STATUS.md` actualizat.
- Comportament: interfață WPF cu controale etichetate pentru cititoare de ecran; instalare în `%LOCALAPPDATA%\Programs\Orizont RSS`; păstrarea datelor utilizatorului; scurtături pentru pornire și dezinstalare; verificare SHA-256 a arhivei înainte de extragere.
- Verificări: compilare fără erori sau avertismente; publicare autonomă win-x64 reușită; executabilul `OrizontSetup.exe` are versiunea de fișier `1.5.3.0`, versiunea produsului `1.5.3`, 68,28 MB și pornește fără închidere imediată. Publicarea a emis avertismente de conectare la NuGet, fără a bloca rezultatul; testarea manuală cu JAWS/NVDA rămâne necesară.
- Comportament protejat: nu s-a modificat codul aplicației; nu s-au atins feeduri, articole sau setări; distribuția publică 1.5.3 existentă nu a fost înlocuită.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Închiderea instalatorului după pornirea aplicației

- Scop: corectarea comportamentului observat la testul manual: aplicația pornea, dar fereastra instalatorului rămânea deschisă.
- Fișier modificat: `packaging/installer/MainWindow.xaml.cs`.
- Schimbare: butonul „Pornește Orizont RSS” lansează aplicația instalată și închide imediat fereastra instalatorului.
- Verificări: recompilare și publicare autonomă win-x64 reușite; aplicația principală nu a fost modificată.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Curățare completă la dezinstalarea instalatorului

- Scop: eliminarea folderului gol rămas după dezinstalarea observată în testul manual.
- Fișier modificat: `packaging/installer/MainWindow.xaml.cs`.
- Schimbare: procesul de ștergere amânată rulează din folderul temporar, nu din folderul pe care îl elimină, pentru a permite ștergerea completă a directorului instalării.
- Verificări: publicare autonomă win-x64 reușită; problema a fost izolată ca limitare de director de lucru, fără atingerea datelor utilizatorului.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Retestare instalare și dezinstalare completă

- Scop: verificarea manuală a instalatorului corectat după observația privind folderul gol rămas.
- Rezultat: instalarea și pornirea aplicației au reușit; scurtătura din meniul Start a pornit aplicația; dezinstalarea confirmată a eliminat complet folderul `%LOCALAPPDATA%\Programs\Orizont RSS`, executabilul, copia instalatorului și ambele scurtături.
- Comportament protejat: nu au fost modificate feedurile, articolele sau setările utilizatorului; nu rulează procese Orizont după dezinstalare.
- Concluzie: ciclul instalator accesibil → instalare → pornire → scurtătură Start → dezinstalare completă este trecut manual.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-05 — Limbă, pictogramă desktop și bară de stare în instalator

- Scop: îmbunătățirea experienței de instalare pentru utilizatori nevăzători.
- Fișiere adăugate/modificate: `packaging/installer/LanguageWindow.xaml`, `LanguageWindow.xaml.cs`, `InstallerLanguage.cs`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`; `docs/PROJECT-STATUS.md` actualizat.
- Schimbări: dialog inițial de limbă cu preselectarea limbii Windows și șase opțiuni; interfața principală și mesajele de progres localizate în română, engleză, spaniolă, franceză, germană și portugheză; opțiune implicit activată pentru pictogramă pe desktop; bara de stare este un `StatusBar` cu regiune live `Assertive`, nume de automatizare actualizat și procente de descărcare anunțate etapizat.
- Verificări: build fără erori; publicare autonomă win-x64 reușită; executabilul are versiunea `1.5.3.0`/`1.5.3`; verificare statică pentru dialog, cele șase limbi, pictogramă și regiune live; avertismentele de restaurare sunt numai de conectare la NuGet și nu blochează publicarea. Testarea manuală JAWS/NVDA a acestor funcții noi rămâne necesară.
- Comportament protejat: instalarea, pornirea, scurtăturile existente și dezinstalarea completă rămân disponibile; nu sunt atinse feedurile, articolele sau setările.
- Executabil de test: `bin/Release/installer-1.5.3-win-x64/OrizontSetup.exe`.

## 2026-09-01 — Perioade scurte de păstrare

- Scop: adăugarea perioadelor de 1, 3, 7 și 14 zile pentru curățarea automată a articolelor obișnuite.
- Fișiere modificate: `SettingsWindow.xaml`, `MainWindow.xaml.cs`, `WORKLOG.md`.
- Comportament protejat: valoarea implicită rămâne 90 de zile; Favoritele și articolele „Mai târziu” rămân protejate; regula separată de 90 de zile pentru feedurile fără articole nu se schimbă.
- Verificări: compilare Release fără erori sau avertismente; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke trecut pentru en-US, es-ES, fr-FR, de-DE și pt-BR.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-01 — Marcarea tuturor articolelor afișate ca citite

- Scop: buton accesibil în lista de articole și comandă contextuală pentru marcarea tuturor articolelor vizibile ca citite.
- Fișiere modificate: `MainWindow.xaml`, `MainWindow.xaml.cs`, `Resources/UiStrings.resx`, `Resources/UiStrings.en-US.resx`, `Resources/UiStrings.es-ES.resx`, `Resources/UiStrings.fr-FR.resx`, `Resources/UiStrings.de-DE.resx`, `Resources/UiStrings.pt-BR.resx`, `WORKLOG.md`.
- Comportament: se aplică numai articolelor afișate după filtrele curente, cere confirmare, salvează imediat și anunță numărul modificat; articolele ascunse nu sunt afectate.
- Verificări: compilare Release fără erori sau avertismente; localizare 800/800 pentru toate cele cinci limbi suplimentare; CoreSmoke și LocalizationSmoke trecute.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-01 — Eliminarea redundanței din meniul contextual

- Scop: păstrarea unei singure comenzi pentru marcarea tuturor articolelor afișate.
- Fișiere modificate: `MainWindow.xaml`, `WORKLOG.md`.
- Comportament: butonul rămâne pentru toate articolele afișate; meniul contextual păstrează doar operațiile asupra articolelor selectate, inclusiv marcarea ca citite și ștergerea cu `Delete`.
- Verificări: compilare Release fără erori sau avertismente; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke trecut pentru en-US, es-ES, fr-FR, de-DE și pt-BR.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-01 — Partajare îmbunătățită pentru conversațiile AI

- Scop: extinderea partajării răspunsurilor Gemini fără Telegram.
- Fișiere modificate: `AiResponseWindow.xaml`, `AiResponseWindow.xaml.cs`, toate resursele `Resources/UiStrings*.resx`, `WORKLOG.md`.
- Funcții: buton pentru copierea conversației cu articolul și sursa; buton pentru distribuirea conversației prin WhatsApp; fallback prin clipboard când conversația depășește limita URL.
- Verificări: compilare Release fără erori sau avertismente; localizare 805/805 pentru toate cele cinci limbi suplimentare; CoreSmoke și LocalizationSmoke trecute.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-01 — Terminologie uniformă pentru partajare

- Scop: eliminarea neuniformității dintre „Trimite conversația” și „Distribuie prin WhatsApp”.
- Fișiere modificate: `AiResponseWindow.xaml`, `AiResponseWindow.xaml.cs`, toate resursele `Resources/UiStrings*.resx`, `WORKLOG.md`.
- Comportament: toate acțiunile de partajare a conversației folosesc „Distribuie”; „Trimite” rămâne pentru întrebările trimise către Gemini.
- Verificări: compilare Release fără erori sau avertismente; localizare 806/806 pentru toate cele cinci limbi suplimentare; CoreSmoke și LocalizationSmoke trecute.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-08-28 — Verificare finală și distribuție 1.5.2

- Verificare completă: build, CoreSmoke, LocalizationSmoke, eSpeakSmoke, localizare completă, ghiduri și verificarea distribuției au trecut.
- Localizare: 797 resurse traduse pentru fiecare dintre cele cinci limbi suplimentare; nicio cheie lipsă sau eroare.
- Distribuții generate la cererea expresă: `Orizont-RSS-1.5.2-Windows-x64.zip` și `Orizont-RSS-1.5.2-Source.zip`.
- SHA-256 Windows x64: `AC7B61D3555D0F3520D2A76F2C5F65C5F31E3162F39F8F13EA4C1B83B54E2FF9`.
- SHA-256 sursă: `A38E4BE48291DAE4E24CC9C75D5693D04A5CE86F60C95F955DC6B44D70B596BB`.

## 2026-08-28 — Documentarea stării proiectului și a regulilor de lucru

- Scop: consemnarea stării actuale, a etapelor închise și a planului de continuare după experimentul Android.
- Fișiere modificate: `docs/PROJECT-STATUS.md`, `AGENTS.md`, `WORKLOG.md`.
- Documentație: `PROJECT-STATUS.md` separă realizatul, lucrul activ, pașii propuși și ideile neautorizate; `AGENTS.md` precizează separarea proiectului Windows de prototipuri și interzice ajustările speculative fără test reproductibil.
- Verificări: citirea documentației existente și verificarea conținutului documentelor noi; nu s-a modificat codul și nu s-a creat distribuție.
- Executabil de test: nu s-a construit în această intervenție.
