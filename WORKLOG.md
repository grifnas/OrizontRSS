# Jurnalul intervențiilor Orizont RSS

Acest jurnal păstrează trasabilitatea modificărilor efective din proiect. Se notează acțiunile asupra fișierelor, nu raționamentul intern al agentului.

## 2026-09-12 — Actualizarea paginilor publice la versiunea 1.5.4

- Motiv: după publicarea release-ului `v1.5.4`, utilizatorul a observat că pagina de prezentare anunța încă versiunea 1.5.3.
- Modificări: toate cele opt pagini localizate din `docs/index*.html` afișează acum 1.5.4 în titlul secțiunii de descărcare, subsol și metadatele JSON-LD `softwareVersion`. Linkurile de instalare și descărcare rămân direcționate către release-ul latest. `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md` reflectă release-ul public și assets verificate.
- Comportamente păstrate: nu s-au modificat codul aplicației, linkurile, versiunile installerului/portabilului, feedurile, setările, datele NewsBlur sau arhivele de distribuție.
- Verificări: toate cele opt pagini trec verificarea statică pentru absența lui 1.5.3, `softwareVersion` 1.5.4 și păstrarea linkurilor; `git diff --check` a trecut. Avertismentele LF→CRLF sunt informative.
- Publicarea a fost inițial blocată de indisponibilitatea acreditărilor în sesiunea Codex și de permisiunile read-only ale conectorului GitHub. Utilizatorul a confirmat apoi că Git Credential Manager era deja conectat; accesul la ramura `main` a fost verificat din nou prin Git.
- Rezultat: schimbările au fost comise și împinse pe `main`; pagina live a fost verificată pentru versiunea 1.5.4. `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md` au fost actualizate pentru a consemna publicarea.
- Nu s-a creat un executabil și nu s-a creat o distribuție nouă; schimbarea privește numai paginile și documentația publică.

## 2026-09-12 — Variante vocale eSpeak NG și setări extinse

- Scop: adaptarea în Orizont RSS a variantelor și controalelor eSpeak NG din Orizont Interpret, fără preluarea rutării audio specifice acelei aplicații.
- Cod și setări: selectorul separat al variantei eSpeak NG, reglajul intonației și fallback-ul anunțat către vocea de bază; preferințele noi se salvează în configurație și în backup fără a schimba SAPI5 sau Gemini.
- Date/licență: adăugate variantele oficiale `ian`, `mike2` și `Reed` din eSpeak NG 1.52.0; proveniența și GPL-3.0-or-later sunt notate în `ThirdParty/eSpeakNG/UPSTREAM.md`.
- Localizare: etichetele, mesajul de fallback și starea listării variantelor sunt traduse în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană.
- Verificări: build Release fără erori sau avertismente; CoreSmoke trecut (56 verificări); LocalizationSmoke trecut pentru en-US, es-ES, fr-FR, de-DE, pt-BR, hu-HU și it-IT; 948/948 chei verificate pentru fiecare limbă secundară; eSpeakSmoke trecut cu 132 voci, 104 variante, vocea română și sinteza Ian/Mike2/Reed cu intonație la 22050 Hz. Backup-ul păstrează limba, varianta și intonația.
- Verificare manuală: JAWS/NVDA trebuie să confirme focalizarea și anunțurile din Setări voce și să asculte vocile/variantele efective; nu a fost folosit profilul utilizatorului și nu s-au atins setări active.
- Distribuție: nu s-a creat și nu s-a publicat o distribuție nouă.

## 2026-09-12 — Confirmări manuale eSpeak și feedul de test

- Confirmare utilizator: setările vocale funcționează, iar modificările rămân după închiderea aplicației.
- Confirmare utilizator: feedul de test există deja în lista de surse; nu s-a adăugat încă o copie.
- Foaia de parcurs a fost actualizată: pentru exemplele RSS rămân de confirmat actualizarea efectivă, focalizarea pe primul articol și ștergerea cu confirmare. Nu s-a creat o distribuție și nu s-a reconstruit executabilul, deoarece această intervenție actualizează doar documentația.

## 2026-09-09 — Verificare elemente publice și WinGet

- Scop: verificarea paginii GitHub Pages, a Release-ului 1.5.3 și a pregătirii manifestului WinGet, fără creare de distribuție nouă.
- Fișiere actualizate: `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Pagina publică: pagina principală și toate cele opt pagini localizate s-au încărcat; fiecare afișează versiunea 1.5.3, linkul recomandat către `OrizontSetup.exe`, alternativa portabilă și structura accesibilă cu titluri, regiuni și texte alternative. `robots.txt` și `sitemap.xml` sunt prezente și coerente.
- Release: API-ul GitHub confirmă Release stabil `v1.5.3`, cu arhiva Windows și `OrizontSetup.exe` încărcate. Hash-ul local al arhivei (`7536070499680C2C2859C69C181BB8B6689EFA510E51C8E61E01DBD4EB48F6EB`) corespunde manifestului și assetului public.
- WinGet: clientul local este `1.29.290`; `winget validate --manifest packaging\\winget\\Grifnas.OrizontRSS\\1.5.3` a reușit. Căutarea exactă și căutarea după nume nu găsesc încă pachetul, deci manifestul nu este publicat în catalog și nu s-a creat un PR automat.
- Comportament protejat: aplicația, instalatorul existent, release-ul 1.5.3 și datele utilizatorului nu au fost modificate.
- Verificare manuală: instalarea/dezinstalarea au fost deja testate de utilizator; retestarea țintită JAWS/NVDA pentru instalator și verificarea instalării WinGet non-administrator rămân pași manuali.
- Distribuție: nu s-a creat și nu s-a publicat o versiune nouă.
- Executabil de test: nu s-a reconstruit; versiunea locală existentă rămâne `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-09 — NewsBlur adăugat pe roadmap

- Scop: consemnarea unei integrări viitoare cu NewsBlur, fără schimbarea aplicației funcționale.
- Fișiere actualizate: `docs/ROADMAP.md`, `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Plan consemnat: adaptor separat, autentificare și consimțământ explicit, import abonamente/foldere, sincronizare controlată a stărilor și păstrarea OPML ca rezervă.
- Sursă tehnică: documentația API oficială NewsBlur — <https://www.newsblur.com/api>.
- Verificări: `git diff --check` și inspecția documentelor; nu s-a modificat codul și nu s-a creat distribuție nouă.
- Executabil de test: nu s-a reconstruit; pentru verificarea versiunii locale existente se poate folosi `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-07 — Exemple RSS pentru testare în fiecare limbă

- Scop: oferirea unui exemplu de lucru imediat pentru fiecare limbă a interfeței, la cererea utilizatorului.
- Fișiere adăugate: `DemoFeedCatalog.cs`.
- Fișiere actualizate: `Models.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, toate resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`.
- Comportament: meniul Feeduri are comenzile „Adaugă feed demonstrativ local”, „Adaugă exemple RSS pentru limba interfeței” și „Elimină feedurile demonstrative”. Feedul local conține trei articole fictive, inclusiv marcaje citit/favorit/Mai târziu, și nu este trimis la actualizare pe internet. Feedul online al limbii curente este verificat înainte de salvare; duplicatele sunt refuzate.
- Corecție de focalizare: după adăugarea unui exemplu, filtrul de folder este mutat automat pe folderul demonstrativ, iar feedul și primul articol rămân vizibile și selectabile.
- Catalog online: câte o adresă RSS oficială pentru română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană. Catalogul este separat de feedurile personale și marchează vizibil exemplele în lista accesibilă.
- Verificări: build Release reușit fără erori sau avertismente; CoreSmoke trecut cu 26 verificări; localizare completă 829/829 pentru toate cele opt limbi; ghidurile verificate pentru en, es, fr, de, pt, hu și it; `git diff --check` fără erori.
- Verificare manuală: trebuie efectuată de utilizator cu JAWS/NVDA pentru focusul după adăugare, citirea celor trei articole și confirmarea eliminării; serviciul Computer Use nu este disponibil în această sesiune.
- Distribuție: nu s-a creat și nu s-a publicat o versiune nouă.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-08 — Metadate SEO pentru pagina publică

- Scop: îmbunătățirea indexării și a previzualizărilor la distribuirea paginii GitHub Pages.
- Fișiere actualizate: toate cele opt `docs/index*.html`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`.
- Fișiere adăugate: `docs/sitemap.xml`, `docs/robots.txt`.
- Comportament: fiecare pagină are descriere localizată, URL canonical propriu, nouă legături `hreflang` inclusiv `x-default`, Open Graph, Twitter Card și JSON-LD pentru `WebSite` și `SoftwareApplication`. Sitemap-ul enumeră toate paginile cu URL-uri absolute și este declarat în robots.txt.
- Protecții: nu s-au adăugat meta-keywords și nu s-a modificat aplicația Windows, instalatorul sau datele utilizatorului.
- Verificări: toate cele opt pagini au câte un description, canonical, nouă hreflang, JSON-LD și opt metadate Open Graph; JSON-LD se parsează fără erori; sitemap-ul este XML valid; `git diff --check` fără erori.
- Publicare: modificările sunt pregătite local; nu s-a făcut commit/push și nu s-a creat distribuție nouă.

## 2026-09-09 — Capturi demonstrative în limba engleză

- Scop: înlocuirea textului românesc din galeria publică cu o limbă internațională comună, conform aprobării utilizatorului.
- Fișiere actualizate: `docs/assets/screenshots/main-window.png`, `reader.png`, `context-menu.png`, `settings.png`, `docs/assets/screenshots/README.md`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`.
- Comportament vizual: cele patru previzualizări păstrează dimensiunea 1440×882 și structura WPF demonstrativă, dar afișează interfața, meniul contextual, cititorul și setările în engleză. Datele sunt fictive și nu includ profilul personal.
- Verificări: imaginile au fost regenerate offline și inspectate vizual; textul românesc și caracterele afișate greșit au fost eliminate; utilitarul temporar de generare a fost șters după folosire.
- Distribuție: nu s-a creat o distribuție nouă.

## 2026-09-08 — Commit și push GitHub pentru modificările publice

- Scop: publicarea automată a modificărilor aprobate de utilizator.
- Acțiune: commitul `5a491b6` („Add multilingual RSS demos and SEO metadata”) a fost împins cu succes în `origin/main` pentru repository-ul `grifnas/OrizontRSS`.
- Verificare: `ls-remote` confirmă hash-ul remote `5a491b65d4d8c93f53a039b13da5a1d1db8c7a4d`; arborele local este curat.
- Notă: nu s-a creat release, instalator nou sau distribuție nouă; GitHub Pages poate avea nevoie de câteva minute pentru redeploy.

## 2026-09-07 — Localizare maghiară și italiană

- Scop: adăugarea limbilor `hu-HU` și `it-IT`, solicitată ca necesitate stringentă.
- Fișiere adăugate: `Resources/UiStrings.hu-HU.resx`, `Resources/UiStrings.it-IT.resx`, `Ghid-utilizator-Orizont-RSS.hu.html`, `Ghid-utilizator-Orizont-RSS.it.html`, `docs/index.hu.html`, `docs/index.it.html`.
- Fișiere actualizate: `Localization/UiCulture.cs`, `Localization/UserGuideLocator.cs`, `packaging/installer/InstallerLanguage.cs`, `tests/LocalizationSmoke/Program.cs`, `tools/verify-localization.ps1`, `tools/verify-user-guides.ps1`, `tools/verify-distribution.ps1`, `tools/translate-localization.ps1`, `tools/translate-user-guide.ps1`, `docs/index*.html`, `README.md`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`, `WORKLOG.md`.
- Comportament: detectarea automată după limba Windows recunoaște maghiara și italiana; ambele apar în Setări și în alegerea inițială a instalatorului; ghidurile și paginile publice au pagini dedicate. Traducerile sunt incluse static și nu adaugă dependențe la rularea aplicației.
- Verificări: build aplicație Release fără erori sau avertismente; build instalator fără erori; CoreSmoke trecut (16 verificări, 1.200 articole); LocalizationSmoke trecut pentru toate cele șapte culturi testate, inclusiv `hu-HU` și `it-IT`; eSpeakSmoke trecut (132 voci); verificarea localizării 814/814 pentru fiecare cultură; verificarea ghidurilor trecută pentru en, es, fr, de, pt, hu și it.
- Verificare manuală: executabilul aplicației a pornit și a rămas activ în smoke testul local; testarea cu JAWS/NVDA a listelor de limbă și a anunțurilor rămâne necesară înaintea unei distribuții publice.
- Distribuție: nu s-a creat și nu s-a publicat o versiune nouă.
- Executabil de test: `bin/Release/net8.0-windows/Orizont.exe`.

## 2026-09-07 — Pregătirea capturilor pentru pagina publică

- Scop: pregătirea galeriei de capturi de ecran recomandate pentru pagina GitHub Pages.
- Fișier adăugat: `docs/assets/screenshots/README.md`, cu cele patru capturi necesare și regulile de confidențialitate.
- Documentație actualizată: `docs/ROADMAP.md`, cu etapa și blocajul curent.
- Verificare: în proiect nu existau capturi ale interfeței, ci doar pictogramele aplicației. S-a încercat capturarea ferestrei reale prin serviciul Windows Computer Use, dar acesta a răspuns `Trusted RPC service is not configured: sky`. Nu s-au generat imagini artificiale și nu s-au folosit datele personale din profilul local.
- Distribuție: nu s-a creat și nu s-a publicat o versiune nouă.

## 2026-09-07 — Galerie vizuală demonstrativă pe GitHub Pages

- Scop: adăugarea unor imagini pentru promovarea interfeței, fără capturarea profilului personal.
- Fișiere adăugate: `docs/assets/screenshots/main-window.png`, `reader.png`, `context-menu.png`, `settings.png`.
- Fișiere actualizate: toate cele opt `docs/index*.html`, cu galerie responsive, text alternativ și descrieri localizate; `docs/ROADMAP.md`.
- Metodă: imaginile au fost generate offline cu un utilitar temporar WPF, folosind componente vizuale și date demonstrative. Utilitarul temporar a fost eliminat după generare.
- Verificări: cele opt pagini conțin fiecare cele patru imagini și câte patru texte alternative; imaginile au fost inspectate vizual și decupate la 1440×882 pentru eliminarea zonei negre de randare; nu sunt incluse date personale sau chei API.
- Distribuție: nu s-a creat și nu s-a publicat o versiune nouă.

## 2026-09-07 — Cerință GitHub pentru autentificare în doi pași

- Scop: consemnarea emailului GitHub care solicită activarea 2FA pentru contul `grifnas`.
- Fișiere modificate: `docs/ROADMAP.md`, `WORKLOG.md`.
- Termen: **21 octombrie 2026, ora 00:00 UTC**; după termen, accesul la GitHub va fi limitat până la activarea 2FA.
- Acțiune cerută utilizatorului: configurarea 2FA la <https://github.com/settings/two_factor_authentication/setup/intro> și păstrarea codurilor de recuperare într-un loc sigur.
- Impact asupra proiectului: niciunul asupra codului, feedurilor, articolelor, release-urilor sau GitHub Pages.
- Verificări: documentația a fost verificată textual; nu s-a modificat codul și nu s-a creat distribuție.
- Executabil de test: nu este necesar pentru această actualizare exclusiv documentară.

## 2026-09-06 — Workflow de sincronizare a pachetelor

- Scop: consemnarea modului obligatoriu prin care modificările ajung atât în instalator, cât și în versiunea portabilă.
- Fișiere modificate: `docs/ROADMAP.md`, `WORKLOG.md`.
- Documentație: foaia de parcurs precizează folosirea aceleiași stări a sursei, creșterea versiunii, buildurile și testele, actualizarea hash-ului și încărcarea ambelor pachete în același release GitHub; pagina publică trebuie verificată după publicare.
- Verificări: documentația a fost verificată textual; nu s-a modificat codul și nu s-a creat distribuție.
- Executabil de test: nu este necesar pentru o modificare exclusivă de documentație.

## 2026-09-06 — Menționarea contribuției OpenAI Codex pe pagina publică

- Scop: prezentarea transparentă a colaborării la fel ca în secțiunea „Despre” a aplicației.
- Fișiere modificate: toate cele șase `docs/index*.html`, `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Comportament: fiecare limbă afișează contribuția lui Grigore Frișan și OpenAI Codex și leagă documentul complet `ACKNOWLEDGEMENTS.md`.
- Verificări: toate cele șase pagini conțin mențiunea și legătura; nu s-a modificat codul aplicației și nu s-a creat distribuție.
- Executabil de test: nu este necesar pentru o modificare exclusivă GitHub Pages.

## 2026-09-06 — Selectarea automată a limbii pe pagina publică

- Scop: deschiderea paginii GitHub Pages în limba preferată a browserului utilizatorului.
- Fișiere modificate: `docs/index.html`, `docs/PROJECT-STATUS.md`, `WORKLOG.md`.
- Comportament: numai URL-ul rădăcină detectează `navigator.languages`/`navigator.language`; limbile ro, en, es, fr, de și pt trimit la pagina corespunzătoare, iar orice altă limbă trimite la engleză. Pagini precum `index.en.html` nu sunt redirecționate, astfel încât alegerea manuală rămâne funcțională.
- Verificări: hartă statică pentru toate cele șase pagini și fallback engleză; nu s-a modificat codul aplicației și nu s-a creat distribuție.
- Executabil de test: nu este necesar pentru o modificare exclusivă GitHub Pages.

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
## 2026-09-09 — Pregătirea trimiterii manifestului WinGet

- Scop: trimiterea setului multi-fișier `Grifnas.OrizontRSS` 1.5.3 către depozitul comunitar oficial `microsoft/winget-pkgs`.
- Verificări: nu există un PR deschis pentru identificatorul și versiunea pachetului; validarea locală WinGet a trecut; URL-ul și hash-ul arhivei publice corespund.
- Rezultat intermediar: WinGet `1.29.290` nu include comanda `submit`; deși fork-ul `grifnas/winget-pkgs` există și are drepturi de administrator pentru utilizator, integrarea GitHub a refuzat operațiile de scriere cu HTTP 403. Browserul de lucru cere autentificare manuală înainte de fork și încărcarea celor patru fișiere YAML.
- Următorul pas: după autentificarea manuală în GitHub, creare fork, încărcare exclusivă în `manifests/g/Grifnas/OrizontRSS/1.5.3/` și deschidere PR către `master`.
- Actualizare: fork-ul `grifnas/winget-pkgs` a fost creat de utilizator și este vizibil prin API; integrarea rămâne read-only și nu poate încărca fișierele chiar și în fork. Autentificarea într-un browser extern nu este partajată cu integrarea Codex.
- Actualizare: prin credential helper-ul Git local au fost create și împinse cele patru fișiere în ramura `submission/orizont-rss-1.5.3`; validarea copiei din ramură a trecut. Deschiderea PR-ului prin API rămâne blocată cu HTTP 403, astfel încât PR-ul trebuie creat din interfața GitHub.
- Rezultat final al etapei: PR-ul oficial `microsoft/winget-pkgs#431971` a fost creat și verificat prin API, cu titlul `Add Grifnas Orizont RSS 1.5.3`, 1 commit și 4 fișiere modificate. Botul Microsoft a aplicat etichetele `New-Package` și `Needs-CLA`; validările sunt încă în așteptare.
- Actualizare: autorul a răspuns în PR cu `@microsoft-github-policy-service agree`; eticheta `Needs-CLA` a fost eliminată. PR-ul rămâne deschis, cu `New-Package`, iar statusul tehnic este încă `pending`.

## 2026-09-09 — Checkpoint local înainte de NewsBlur

- Scop: păstrarea unei copii locale de revenire înaintea implementării integrării NewsBlur, cu risc minim pentru publicarea existentă.
- Fișiere și artefacte locale: `bin/Release/checkpoint-pre-NewsBlur-1.5.4/` conține portabilul reconstruit din sursa curentă, arhiva sursă Git și `OrizontSetup-1.5.3-rollback.exe`.
- Decizie de siguranță: nu s-au modificat metadatele versiunii, Release-ul GitHub, manifestele WinGet sau URL-ul instalatorului. Instalatorul rămâne 1.5.3 deoarece bootstrapperul descarcă intenționat Release-ul public 1.5.3.
- Verificări: publicarea autonomă win-x64 a reușit; distribuția portabilă locală trece `verify-distribution.ps1`; limbile en-US, es-ES, fr-FR, de-DE, pt-BR, hu-HU și it-IT sunt prezente; testele CoreSmoke, LocalizationSmoke și eSpeakSmoke au trecut în verificarea automată.
- Notă de verificare: vechiul folder `final-1.5.3-win-x64` a fost respins de `verify-all.ps1` deoarece îi lipsesc hu-HU și it-IT; checkpoint-ul reconstruit este cel verificat și nu folosește acel folder vechi.
- Rezultat: checkpoint local nepublic, pregătit pentru revenire; versiunea reală 1.5.4 se va construi numai după NewsBlur și actualizarea controlată a tuturor metadatelor.
- Executabil de test/rollback: `bin/Release/checkpoint-pre-NewsBlur-1.5.4/OrizontSetup-1.5.3-rollback.exe`.

## 2026-09-09 — Prima etapă de autentificare NewsBlur

- Scop: adăugarea unei ferestre accesibile pentru autentificarea și crearea contului NewsBlur, fără a afecta fluxul RSS local.
- Fișiere adăugate: `NewsBlurConnection.cs`, `NewsBlurAuthWindow.xaml`, `NewsBlurAuthWindow.xaml.cs`; fișiere actualizate: `AppSettings.cs`, `BackupPolicy.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md`.
- Funcții: autentificare cu utilizator și parolă, creare cont cu e-mail, deconectare, deschiderea site-ului NewsBlur în browser; parola nu este salvată, iar sesiunea este protejată cu DPAPI pentru contul Windows curent.
- OAuth: panou explicativ și legătură către documentația oficială; Google, Facebook și alți furnizori nu sunt activați până la primirea unui client ID și secret aprobat de NewsBlur.
- Verificări: build Release reușit; CoreSmoke, LocalizationSmoke, eSpeakSmoke și `verify-localization.ps1` trecute; publicare autonomă locală și `verify-distribution.ps1` trecute. Avertismentul NU1900 provine de la indisponibilitatea temporară a indexului NuGet și nu a blocat buildul.
- Comportament protejat: fluxul RSS local, cheile AI, feedurile, articolele și backupurile existente rămân neschimbate; nu s-a creat o distribuție publică și nu s-a modificat WinGet.
- Executabil de test: `bin/Release/test-newsblur-auth-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Verificarea sesiunii NewsBlur și numărul de feeduri

- Scop: eliminarea ambiguității după autentificare, deoarece sincronizarea abonamentelor nu este încă implementată.
- Fișiere modificate: `NewsBlurConnection.cs`, `NewsBlurAuthWindow.xaml`, `NewsBlurAuthWindow.xaml.cs`, resursele `Resources/UiStrings*.resx`.
- Funcții: după autentificare se verifică endpointul `/reader/feeds`; fereastra raportează dacă sesiunea este validă și câte feeduri a returnat NewsBlur. Pentru sesiunile existente există butonul „Verifică sesiunea NewsBlur”.
- Verificări: build Release, CoreSmoke, LocalizationSmoke, publicare autonomă și `verify-distribution.ps1` trecute; avertismentul NU1900 nu a blocat buildul.
- Comportament protejat: nu se descarcă încă feedurile în lista locală și nu se modifică articolele până la etapa separată de sincronizare.
- Executabil de test: `bin/Release/test-newsblur-auth-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Mesaje detaliate la autentificarea NewsBlur

- Scop: corectarea feedbackului când NewsBlur returnează erori de autentificare.
- Fișier modificat: `NewsBlurConnection.cs`.
- Corecție: răspunsurile API cu `errors` ca obiect JSON sunt acum interpretate și afișate cu numele câmpului și mesajul serverului, nu doar ca eroare generică.
- Verificări: build Release, CoreSmoke, LocalizationSmoke, publicare autonomă și `verify-distribution.ps1` trecute; avertismentul NU1900 este numai de la indexul NuGet indisponibil.
- Executabil de test: `bin/Release/test-newsblur-auth-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Buton accesibil pentru verificarea sesiunii NewsBlur

- Scop: butonul de verificare nu mai trebuie ascuns din navigarea JAWS atunci când nu există încă o sesiune.
- Fișier modificat: `NewsBlurAuthWindow.xaml.cs`.
- Comportament: butonul „Verifică sesiunea NewsBlur” este activ permanent și anunță explicit lipsa sesiunii; după autentificare, sesiunea este salvată imediat, astfel încât să nu fie pierdută dacă verificarea secundară a numărului de feeduri nu răspunde.
- Verificări: build Release, CoreSmoke, LocalizationSmoke, publicare autonomă și `verify-distribution.ps1` trecute; avertismentul NU1900 nu a blocat buildul.
- Executabil de test: `bin/Release/test-newsblur-auth-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Feedback vocal și clarificarea datelor NewsBlur

- Scop: investigarea raportului că autentificarea și verificarea sesiunii nu oferă niciun feedback perceptibil.
- Fișiere modificate: `NewsBlurAuthWindow.xaml`, `NewsBlurAuthWindow.xaml.cs`.
- Corecții: mesajele de așteptare, succes, eroare și verificare sunt anunțate prin `StatusAnnouncer` și bare de stare live; butonul rămâne navigabil; câmpul de autentificare precizează că API-ul cere numele de utilizator, nu adresa de e-mail.
- Verificări: build Release, CoreSmoke, LocalizationSmoke, publicare autonomă și `verify-distribution.ps1` trecute; avertismentul NU1900 nu a blocat buildul.
- Executabil de test: `bin/Release/test-newsblur-auth-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Antet HTTP explicit pentru NewsBlur

- Scop: reducerea riscului ca serverul NewsBlur să trateze cererile aplicației ca trafic neidentificat.
- Fișier modificat: `NewsBlurConnection.cs`.
- Corecție: clientul HTTP trimite acum un User-Agent Orizont RSS și solicită răspuns JSON; nu se schimbă parolele, cookie-urile sau datele locale.
- Verificări: build Release, CoreSmoke, LocalizationSmoke, publicare autonomă și `verify-distribution.ps1` trecute; o rulare paralelă inițială a fost repetată separat după o coliziune temporară de fișiere de resurse.
- Executabil de test: `bin/Release/test-newsblur-auth-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Gruparea meniului principal Feeduri

- Scop: reducerea aglomerării meniului `Feeduri` fără schimbarea comenzilor sau a scurtăturilor.
- Fișiere modificate: `MainWindow.xaml`, resursele `Resources/UiStrings*.resx`.
- Organizare: `Adaugă și descoperă`, `Feeduri demonstrative`, `Actualizare`, `Organizare`, `Import, export și copii de siguranță` și `Servicii externe`.
- Verificări: `verify-localization.ps1 -RequireComplete`, build Release, CoreSmoke, publicare autonomă și `verify-distribution.ps1` trecute; avertismentul NU1900 nu a blocat buildul.
- Comportament protejat: comenzile existente, scurtăturile, meniurile contextuale și fluxul local NewsBlur nu au fost schimbate.
- Executabil de test: `bin/Release/test-newsblur-menu-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Comenzi de actualizare permanent navigabile

- Scop: comenzile „Oprește actualizarea în curs” și „Reîncearcă feedurile cu eroare” erau dezactivate în starea obișnuită și puteau fi omise de JAWS.
- Fișiere modificate: `MainWindow.xaml`, `MainWindow.xaml.cs`.
- Corecție: cele două comenzi rămân permanent disponibile în submeniul `Actualizare`; când nu există o acțiune aplicabilă, handlerul anunță situația fără să modifice datele.
- Verificări: build Release, CoreSmoke, `verify-localization.ps1 -RequireComplete`, publicare autonomă și `verify-distribution.ps1` trecute; avertismentul NU1900 nu a blocat buildul.
- Executabil de test: `bin/Release/test-newsblur-menu-v2-win-x64/Orizont.exe`.

## 2026-09-09 — Sincronizare sigură a abonamentelor NewsBlur

- Scop: preluarea abonamentelor și folderelor NewsBlur în regim controlat, fără ștergeri locale și fără import de articole sau stări.
- Fișiere modificate: `NewsBlurConnection.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md`.
- Funcții: export OPML oficial NewsBlur, parser recursiv pentru foldere, eliminare duplicate după adresă, rezumat accesibil și confirmare înainte de modificare; feedurile locale absente din NewsBlur sunt păstrate, iar feedurile existente își pot actualiza numele și folderul.
- Localizare: toate cele șapte fișiere de cultură au acum 911 chei, fără chei lipsă, valori goale, erori de format sau termeni protejați încălcați.
- Verificări: build Release reușit; CoreSmoke trecut cu 26 verificări; verificarea localizării cu `-RequireComplete` trece pentru en-US, es-ES, fr-FR, de-DE, pt-BR, hu-HU și it-IT. Avertismentul NU1900 provine de la indisponibilitatea indexului NuGet și nu blochează buildul.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages.
- Executabil de test: `bin/Release/test-newsblur-sync-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Prima etapă de sincronizare bidirecțională NewsBlur

- Scop: sincronizarea controlată a stărilor articolelor existente între Orizont RSS și NewsBlur.
- Fișiere modificate: `Models.cs`, `FeedStore.cs`, `NewsBlurConnection.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md`.
- Funcții: se rețin identificatorul feedului NewsBlur, `story_hash` și ultima stare confirmată; articolele sunt asociate prin hash, adresă, identificator sau titlu și dată. Rularea inițială creează baza fără suprascrieri, iar rulările următoare pot prelua sau trimite citit/necitit, favorite și etichete.
- Protecții: conflictele nu sunt suprascrise automat; `Mai târziu`, ștergerile, importul de articole noi și sincronizarea automată în fundal rămân în afara etapei. Cererile sunt grupate și distanțate pentru a respecta recomandările API NewsBlur.
- Localizare: toate cele șapte fișiere de cultură au acum 923 chei, fără chei lipsă sau în plus.
- Verificări: build Release reușit; CoreSmoke trecut cu 26 verificări; publicare autonomă win-x64 și `verify-distribution.ps1` trecute. Avertismentul NU1900 provine de la indisponibilitatea indexului NuGet și nu blochează buildul.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages.
- Executabil de test: `bin/Release/test-newsblur-bidirectional-v1-win-x64/Orizont.exe`.

## 2026-09-09 — Corecție pentru baza locală a sincronizării NewsBlur

- Scop: împiedicarea interpretării unei diferențe existente la prima asociere drept modificare locală nouă la rularea următoare.
- Corecție: fiecare articol păstrează separat ultima stare locală și ultima stare remote confirmată; prima rulare nu suprascrie și nu trimite diferențele existente.
- Verificări: build Release, CoreSmoke, publicare autonomă win-x64, `verify-distribution.ps1` și `git diff --check` trecute. Avertismentul NU1900 provine de la indisponibilitatea indexului NuGet.
- Executabil de test: `bin/Release/test-newsblur-bidirectional-v2-win-x64/Orizont.exe`.

## 2026-09-09 — Sincronizare controlată a feedurilor, folderelor și articolelor lipsă NewsBlur

- Scop: apropierea sincronizării NewsBlur de obiectivul utilizatorului de a accesa aceeași colecție de pe mai multe calculatoare.
- Fișiere modificate: `NewsBlurConnection.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md`.
- Funcții: comandă separată pentru feeduri și foldere; feedurile locale care nu există remote sunt trimise după confirmare, feedurile remote noi sunt importate local, iar numele/folderul remote au prioritate la diferențe. Nu se fac ștergeri automate.
- Articole: endpointul NewsBlur este solicitat cu textul disponibil, iar sincronizarea stărilor importă articolele lipsă cu titlu, conținut, adresă, dată, citit/necitit, favorite și etichete. Articolele importate sunt păstrate la actualizările RSS ulterioare.
- Limitări documentate: „Mai târziu”, notițele AI, folderele goale, ștergerile sincronizate, sincronizarea automată în fundal și rezolvarea avansată a conflictelor rămân locale sau neimplementate.
- Sursă tehnică: documentația oficială NewsBlur — <https://www.newsblur.com/api>.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages; distribuția publică nu este creată.

## 2026-09-09 — Direcție aprobată pentru extinderea sincronizării NewsBlur

- Decizie: următoarele funcții se implementează etapizat, nu implicit toate odată: Saved Stories/stele către Favorite sau „Mai târziu”, etichete bidirecționale, redenumire și mutare feeduri/foldere, vizualizări speciale, text complet la cerere, stări agregate și sincronizare automată.
- Reguli: se păstrează confirmarea înainte de modificări remote, ștergerea rămâne dezactivată implicit, conflictele trebuie documentate și rezolvate explicit, iar fiecare intervenție se verifică și se notează în `WORKLOG.md`.
- Date excluse: parolele, cheile API, notițele AI și setările vocale rămân locale.
- Documentație actualizată: `docs/ROADMAP.md` și `docs/PROJECT-STATUS.md`.
- Nu s-a modificat codul și nu s-a creat o distribuție nouă.

## 2026-09-09 — Sesiunea A: Saved Stories și etichete NewsBlur

- Scop: sincronizarea configurabilă a articolelor salvate și a etichetelor între NewsBlur și Orizont RSS.
- Fișiere modificate: `AppSettings.cs`, `BackupPolicy.cs`, `Models.cs`, `SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `MainWindow.xaml.cs`, resursele `Resources/UiStrings*.resx`, `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md`.
- Funcții: în Setări aplicație → Sincronizare NewsBlur se poate alege maparea stelelor NewsBlur către `Favorite`, `De citit mai târziu` sau ambele. Baza locală reține starea mapată, astfel încât modificările ulterioare și etichetele să poată fi trimise sau preluate fără a interpreta greșit prima rulare.
- Protecții: alegerea se aplică numai la sincronizarea explicită, nu șterge articole și nu modifică parole, chei API, notițe AI sau setări vocale. Backupul păstrează preferința fără date de autentificare.
- Verificări: build Release, CoreSmoke (26 verificări), `verify-distribution.ps1` și `git diff --check` trecute; avertismentul NU1900 provine de la indexul NuGet indisponibil și nu blochează buildul.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages; nu s-a creat o distribuție publică.
- Executabil local de test: `bin/Release/test-newsblur-saved-tags-v1-files-win-x64/Orizont.exe`.

## 2026-09-09 — Sesiunea B: feeduri/foldere, vizualizări și stări agregate NewsBlur

- Scop: continuarea fluxului aprobat cu metadate de feed, vizualizări virtuale, text complet și operații agregate.
- Fișiere modificate: `Models.cs`, `FeedStore.cs`, `NewsBlurConnection.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md`.
- Funcții: redenumirea și mutarea feedurilor pot fi trimise către NewsBlur când numai copia locală s-a schimbat; modificările remote sunt preluate local; modificările concurente sunt păstrate neschimbate. Au fost adăugate vizualizările Saved, Unread și All NewsBlur, comanda de marcare ca citit a folderului selectat și preluarea textului complet NewsBlur la cerere, cu fallback RSS.
- Protecții: nu se șterge nimic; comenzile remote cer confirmare; articolele importate și stările lor sunt păstrate; sincronizarea automată în fundal nu a fost activată.
- Localizare: fișierele resursă verificate anterior au fost restaurate după o generare automată care producea duplicate; mesajele noi folosesc temporar fallbackul sursă și vor fi traduse controlat înaintea unei distribuții.
- Verificări: build Release, CoreSmoke (26 verificări), `verify-distribution.ps1` și `git diff --check` trecute; singurul avertisment este NU1900 pentru indexul NuGet indisponibil.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages; nu s-a creat distribuție publică.
- Executabil local de test: `bin/Release/test-newsblur-phase2-v1-files-win-x64/Orizont.exe`.

## 2026-09-09 — Sesiunea C: sincronizare automată prudentă NewsBlur

- Scop: sincronizarea automată în fundal a articolelor și stărilor, cu control explicit al utilizatorului.
- Fișiere modificate: `AppSettings.cs`, `BackupPolicy.cs`, `SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `MainWindow.xaml.cs`, `docs/PROJECT-STATUS.md` și `docs/ROADMAP.md`.
- Funcții: în Setări aplicație → Sincronizare NewsBlur se poate activa sincronizarea automată și alege intervalul de 15, 30, 60 sau 180 de minute. Timerul pornește numai după încărcarea aplicației și existența unei sesiuni NewsBlur protejate, se oprește la închiderea ferestrei și nu permite rulări simultane.
- Protecții: sincronizarea automată apelează numai sincronizarea bidirecțională a articolelor și stărilor; feedurile și folderele nu sunt modificate automat, nu se fac ștergeri și setarea rămâne dezactivată implicit. Erorile sunt anunțate în bara de stare fără a închide aplicația.
- Localizare: mesajele noi folosesc cheia de traducere cu fallback sursă; completarea controlată a resurselor pentru toate limbile rămâne criteriu înaintea unei distribuții.
- Verificări: build Release reușit; CoreSmoke trecut cu 26 verificări; avertismentul NU1900 provine de la indisponibilitatea indexului NuGet și nu blochează buildul. Verificarea manuală cu JAWS/NVDA a timerului rămâne necesară deoarece sincronizarea în fundal depinde de sesiunea NewsBlur a utilizatorului.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages; nu s-a creat distribuție publică.
- Executabil local de test: `bin/Release/test-newsblur-autosync-v1-files-win-x64/Orizont.exe`.

## 2026-09-12 — Sincronizarea controlată a dezabonărilor NewsBlur

- Scop: continuarea sincronizării între calculatoare fără a transforma ștergerile locale în modificări remote neașteptate.
- Fișiere modificate: `Models.cs`, `AppSettings.cs`, `BackupPolicy.cs`, `NewsBlurConnection.cs`, `MainWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`.
- Funcții: ștergerea locală a unui feed asociat NewsBlur salvează o cerere de dezabonare persistentă; comanda explicită de sincronizare afișează numărul cererilor și cere confirmare înainte de apelarea endpointului NewsBlur `/reader/delete_feed`. Dacă un feed asociat lipsește din NewsBlur, utilizatorul poate alege între ștergerea copiei locale și articolelor ei, reabonare sau anulare. Coada este păstrată în backup fără sesiunea de autentificare.
- Protecții: sincronizarea automată a articolelor/stărilor nu execută dezabonări; anularea nu aplică schimbări; eroarea remote păstrează cererea pentru reîncercare; comenzile locale de ștergere sunt blocate cât timp sincronizarea feedurilor rulează. Ștergerea articolelor individuale rămâne locală, deoarece API-ul NewsBlur documentează dezabonarea feedurilor, dar nu un endpoint de ștergere a unui articol. Ștergerea unui folder local doar mută feedurile în „Neorganizate”, deci nu declanșează dezabonarea.
- Sursă tehnică: documentația oficială NewsBlur — <https://www.newsblur.com/api>.
- Verificări: build Release reușit; CoreSmoke trecut cu 27 verificări; LocalizationSmoke trecut pentru cele șapte limbi secundare; verificarea resurselor și `git diff --check` trecute. Publicarea autonomă pentru Windows x64 a eșuat la restaurarea pachetelor, deoarece conexiunea TLS către `api.nuget.org` nu a putut autentifica accesul (NU1301); executabilul de build framework-dependent este disponibil. Modificarea nu a trimis nicio cerere de ștergere către contul NewsBlur; verificarea manuală cu JAWS/NVDA a noilor dialoguri rămâne necesară.
- Localizare: textele noi folosesc fallbackul românesc; traducerea și verificarea lor în toate cele opt limbi sunt necesare înaintea unei distribuții.
- Decizie de siguranță: nu s-au modificat versiunea publică, Release-ul GitHub, manifestele WinGet sau GitHub Pages; nu s-a creat distribuție publică.
- Executabil local de test (framework-dependent, rezultat din build): `bin/Release/net8.0-windows/Orizont.exe`. Pentru rulare este necesar .NET 8 Desktop Runtime; publicarea autonomă pentru test poate fi refăcută după restabilirea accesului NuGet. Nu s-a creat o distribuție.

## 2026-09-12 — Rezolvarea conflictelor de metadate NewsBlur

- Scop: închiderea următorului punct din roadmap fără suprascriere automată a numelui sau folderului când valoarea s-a schimbat diferit în Orizont RSS și NewsBlur.
- Fișiere modificate în această etapă: `Models.cs`, `MainWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Modificările locale din etapa anterioară au fost păstrate.
- Funcții: numele și folderul sunt comparate independent cu baza ultimei sincronizări. Modificările fără suprapunere se combină. Pentru fiecare câmp schimbat diferit pe ambele părți, dialogul oferă Da (păstrează local și trimite în NewsBlur), Nu (preia valoarea NewsBlur local) sau Anulează (amână doar acel câmp; celelalte operații continuă). Alegerea locală actualizează baza numai după reușita API; erorile păstrează conflictul pentru următoarea încercare. Feedurile fără ID NewsBlur nu sunt marcate ca sincronizate cu succes.
- Protecții: alegerea implicită a dialogului este Anulează; închiderea dialogului este tratată tot ca amânare, nu ca acceptare locală. Nicio valoare concurentă nu este înlocuită fără alegerea utilizatorului. Sincronizarea automată a articolelor și stărilor nu este schimbată.
- Verificări: build Release reușit fără avertismente; CoreSmoke trecut cu 31 de verificări, inclusiv clasificarea conflictelor diferite, convergente, insensibile la majuscule pentru foldere și a editărilor într-o singură parte. LocalizationSmoke a trecut pentru cele șapte limbi secundare; scriptul a confirmat 942 de chei existente per limbă fără lipsuri sau erori; `git diff --check` a trecut. Dialogul nou nu a fost verificat manual cu JAWS/NVDA.
- Localizare: textele noi ale dialogului folosesc încă fallbackul românesc; trebuie traduse și verificate în cele opt limbi înainte de distribuție.
- Decizie de siguranță: nu s-a folosit sesiunea reală NewsBlur, nu s-au modificat abonamente remote, versiunea publică, Release-ul GitHub, WinGet sau GitHub Pages; nu s-a creat distribuție.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime. Publicarea autonomă de test rămâne afectată de eroarea de autentificare TLS NU1301 către NuGet consemnată la etapa anterioară.

## 2026-09-12 — Inițializare NewsBlur pe dispozitive noi și mutarea feedurilor în Neorganizate

- Scop: ca NewsBlur să fie puntea comună între calculatoare: un profil Orizont curat importă inițial din NewsBlur, apoi sincronizează bidirecțional; ștergerea folderului nu trebuie să dezaboneze feedurile.
- Fișiere modificate în această etapă: `AppSettings.cs`, `BackupPolicy.cs`, `NewsBlurConnection.cs`, `MainWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Modificările preexistente din arborele de lucru au fost păstrate.
- Funcții: marcajul inițializării este păstrat local per cont și exclus din backup. Prima sincronizare confirmată importă abonamentele/folderele și metadatele NewsBlur, păstrează feedurile reale locale care nu există remote și nu scrie în NewsBlur; feedurile demonstrative nu sunt sincronizate. După salvarea cu succes, sincronizarea feedurilor/folderelor devine bidirecțională. Instalările deja legate la NewsBlur sunt recunoscute și nu sunt retrogradate la import inițial. Sincronizarea articolelor/stărilor este blocată până la inițializarea abonamentelor.
- Organizare și ștergere: folderul local „Neorganizate” corespunde feedurilor NewsBlur de nivel superior. Ștergerea folderului mută feedurile prin `move_feed_to_folder` cu valorile goale cerute pentru destinația/sursa de nivel superior; nu se apelează `delete_folder`. Ștergerea feedului din „Neorganizate” rămâne dezabonare explicită din coada existentă, cu confirmare separată. Categoria „Neorganizate” nu poate fi ștearsă ca folder obișnuit.
- Sursă tehnică: documentația oficială NewsBlur — <https://www.newsblur.com/api>; `move_feed_to_folder` acceptă nume de folder goale pentru nivelul superior, iar `delete_folder` dezabonează feedurile din folder.
- Verificări: build Release reușit cu 0 avertismente și 0 erori; CoreSmoke trecut cu 44 verificări, inclusiv markerul per cont, migrarea instalărilor deja sincronizate, excluderea exemplelor și parametrii de mutare la nivel superior; LocalizationSmoke a trecut pentru cele șapte limbi secundare; `git diff --check` a trecut. Căutarea codului nu a găsit apeluri către `/reader/delete_folder`.
- Limitări/verificare manuală: nu s-a folosit sesiunea reală NewsBlur și nu s-au modificat abonamente remote. Dialogurile și fluxul cu JAWS/NVDA nu au fost testate manual. Noile mesaje folosesc momentan fallbackul românesc și trebuie traduse/verificate în toate cele opt limbi înaintea unei distribuții.
- Decizie de siguranță: versiunea, Release-ul GitHub, WinGet și GitHub Pages nu au fost modificate; nu s-a creat distribuție.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.

## 2026-09-12 — Pregătirea publicării GitHub 1.5.4

- Cerere expresă: publicarea Orizont RSS 1.5.4 pe GitHub, inclusiv sursa, pagina publică și Release-ul cu portabilul și installerul.
- Verificarea ramurilor: `local/1.5.4-checkpoint` este cu 17 commituri înaintea lui `main`; `main` local corespunde lui `origin/main` (`7d25724`). Tagul `v1.5.4` nu există încă. Publicarea va fi fast-forward, fără force push.
- Fișiere publice de versiune actualizate: `README.md`, `PUBLICATION.md`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și toate cele opt pagini `docs/index*.html`; paginile afișează acum 1.5.4, iar README-ul leagă direct instalatorul.
- Validarea completă rerulată pe `bin/Release/final-1.5.4-win-x64`: FULL VALIDATION PASSED; build Release 0 avertismente/erori; CoreSmoke 59 verificări; LocalizationSmoke pentru toate cele opt limbi; eSpeak NG 132 voci și 104 variante; verificare strictă 954/954 texte per limbă cu zero erori; ghidurile toate prezente și structura validă; distribuția are versiunea 1.5.4.0/1.5.4 și 444 fișiere eSpeak.
- Hash-uri locale confirmate: portabil `Orizont-RSS-1.5.4-win-x64.zip` SHA-256 `1B32F73D6426B39F3B81BCE6F0DEAEA6B4218543EBAC3CFB456DCD479FDEDF24`; installer `OrizontSetup-1.5.4.exe` SHA-256 `18AA82319575CC14E3082E3E8ECA97054C26EA0AB42E0AF381A057B941BC808B`. Arhiva sursă publicată va include paginile 1.5.4 actualizate; hash-ul va fi furnizat prin fișierul sidecar `.sha256`.
- Verificare manuală: JAWS/NVDA pe interfața 1.5.4 și testul NewsBlur pe al doilea calculator nu au fost efectuate de această sesiune; nu sunt declarate drept verificate.
- Stare externă la consemnare: publicarea GitHub nu a fost încă finalizată; pagina Releases solicită autentificarea proprietarului pentru atașarea fișierelor. Nicio dată locală sau remote NewsBlur nu a fost accesată ori modificată.

## 2026-09-12 — Publicarea sursei și a tagului 1.5.4

- Commitul `54d941498785bdcd3521e344b2d6c968bbfd7244` a fost împins fast-forward pe `main`; tagul `v1.5.4` pointează la același commit. Nu s-a folosit force push.
- Verificare publică: refs-urile GitHub confirmă `main` și `v1.5.4`; endpointul Release-by-tag răspunde 404, deci există sursa/tagul și arhivele GitHub generate automat, dar nu există încă Release cu pachetele binare.
- Pachetele atașabile sunt pregătite în `../release-assets-1.5.4/`: installer `OrizontSetup.exe`, portabilul, arhiva sursă curentă, notele de lansare și fișierele SHA-256. Arhiva sursă are 605 intrări și nicio intrare interzisă.
- Browserul GitHub a redirecționat către autentificarea Google. Autentificarea nu a fost automatizată; sunt necesare conectarea manuală și continuarea publicării Release-ului.
- Până când asseturile Release sunt publicate, GitHub Pages și eticheta de versiune descărcabilă rămân la 1.5.3, pentru ca linkul installerului să nu indice greșit versiunea.
- Testarea manuală JAWS/NVDA și confirmarea pe al doilea calculator rămân neefectuate; niciuna nu este declarată ca trecută.

## 2026-09-12 — Distribuția locală Orizont RSS 1.5.4

- Cerere aprobată: creare locală pentru Windows x64 cu arhivă portabilă, installer și sursă; nu publicare GitHub. Release-ul public 1.5.3, Pages și manifestele WinGet au rămas neschimbate.
- Comportamente protejate: inițializarea NewsBlur pe profil nou rămâne import unidirecțional; sincronizările ulterioare sunt bidirecționale; feedurile demonstrative sunt excluse; feedurile și articolele locale neasociate se păstrează; nu s-a pornit aplicația pe profilul utilizatorului și nu s-a folosit sesiunea NewsBlur reală.
- Localizare: mesajele NewsBlur și celelalte resurse noi au fost completate în engleză, spaniolă, franceză, germană, portugheză braziliană, maghiară și italiană. Verificarea strictă a raportat 954/954 chei în fiecare limbă, fără lipsuri, chei suplimentare, texte goale, erori de substituenți/termeni/structură sau marcatori interni. Etichetele simbolice și `OAuth` au rămas intenționat comune.
- Fișiere de versiune/documentație actualizate: `CititorRSS.Jaws.csproj`, `packaging/installer/OrizontSetup.csproj`, `packaging/installer/MainWindow.xaml.cs`, `packaging/installer/InstallerLanguage.cs`, `tests/LocalizationSmoke/Program.cs`, `tools/verify-all.ps1`, `tools/verify-distribution.ps1`, `BUILDING.md`, `CHANGELOG.md`, `RELEASE-NOTES-1.5.4.md`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`; resursele celor șapte limbi au fost actualizate. Schimbările preexistente în cod și documente au fost păstrate.
- Installer: pachetul offline 1.5.4 încorporează arhiva portabilă și hash-ul SHA-256; hash-ul resursei extrase din assembly corespunde arhivei. Dacă lipsește payload-ul offline, installerul 1.5.4 nu descarcă accidental versiunea publică 1.5.3. Mărime installer: 247.188.504 bytes.
- Verificări: build aplicație și installer fără avertismente/erori; CoreSmoke 59; LocalizationSmoke în ro-RO plus șapte limbi; EspeakSmoke 132 voci/104 variante; verificarea strictă a localizării și a ghidurilor; `verify-all.ps1` a raportat `FULL VALIDATION PASSED`; distribuția portabilă are versiunea fișierului 1.5.4.0, produs 1.5.4 și 444 fișiere eSpeak; `git diff --check` trecut. Restaurarea runtime-ului oficial .NET 8.0.31 a reușit după un eșec TLS inițial; auditul NuGet nu a fost dezactivat permanent.
- Arhivă portabilă locală: `../Orizont-RSS-1.5.4-win-x64.zip`, 85.477.525 bytes, SHA-256 `1B32F73D6426B39F3B81BCE6F0DEAEA6B4218543EBAC3CFB456DCD479FDEDF24`.
- Installer offline local: `../OrizontSetup-1.5.4.exe`, 247.188.504 bytes, SHA-256 `18AA82319575CC14E3082E3E8ECA97054C26EA0AB42E0AF381A057B941BC808B`.
- Arhivă sursă locală: `../Orizont-RSS-1.5.4-source.zip`, cu sursa și documentația actuală, fără `.git`, directoare `bin/obj`, setări sau date personale; verificarea arhivei a confirmat fișierele-cheie și absența fișierelor de build/date locale. Hash-ul este în fișierul `.sha256` alăturat.
- Verificare manuală: instalarea/dezinstalarea și traseele JAWS/NVDA nu au fost simulate automat, ca să nu schimbe profilul Windows sau să pretindă acceptare de către cititorul de ecran. Rămân testabile de utilizator.
- Executabil autonom de test: `bin/Release/final-1.5.4-win-x64/Orizont.exe`.

## 2026-09-12 — Corectarea adresei exemplului RSS HotNews

- Scop: continuarea verificării manuale a exemplelor RSS din foaia de parcurs după ce utilizatorul a primit în JAWS mesajul că aplicația nu se poate conecta la serverul HotNews.
- Fișiere modificate: `DemoFeedCatalog.cs`, `tests/CoreSmoke/Program.cs`, `docs/ROADMAP.md`, `docs/PROJECT-STATUS.md` și `WORKLOG.md`.
- Diagnostic: catalogul folosea `https://rss.hotnews.ro`; rezolvarea DNS a eșuat cu „No such host is known”. Pagina HotNews care menționează această adresă este din 2008. Adresa `https://hotnews.ro/feed` de pe domeniul oficial a răspuns în verificarea web cu tipul `application/rss+xml`; descărcarea directă din mediul local nu a putut fi confirmată din cauza erorilor locale DNS/TLS.
- Modificare: exemplul românesc indică acum `https://hotnews.ro/feed`. Nu s-au schimbat alte feeduri, feeduri personale, setări sau date NewsBlur.
- Verificări: build Release reușit cu 0 avertismente și 0 erori; CoreSmoke trecut cu 45 de verificări; `git diff --check` trecut, cu avertismente informative LF→CRLF. Confirmarea că aplicația adaugă articolele de la noua adresă și verificarea focalizării/ștergerii rămân manuale cu JAWS.
- Decizie de siguranță: nu s-a creat distribuție și nu s-a modificat versiunea publică.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.

## 2026-09-12 — Aplicarea retenției locale în fluxurile RSS și NewsBlur

- Scop: respectarea perioadei de păstrare configurate și împiedicarea reapariției în Orizont a articolelor NewsBlur expirate, fără ștergeri remote.
- Fișiere modificate pentru această intervenție: `ArticleRetention.cs`, `MainWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Modificările preexistente din arborele de lucru au fost păstrate.
- Funcționare: după o rundă de actualizare RSS, Orizont curăță articolele obișnuite expirate chiar dacă unele feeduri au eșuat sau actualizarea a fost oprită; favoritele și „Mai târziu” se păstrează. La sincronizarea NewsBlur, curățarea locală se aplică înaintea cererilor, iar articolele remote expirate sunt ignorate la import. Stelele NewsBlur păstrează articolul potrivit mapării Favorite/Mai târziu configurate.
- Protecții: nu se apelează API pentru ștergerea articolelor și nu se modifică sesiunea, abonamentele ori istoricul din NewsBlur. Feedurile, setările și datele locale existente nu au fost deschise sau modificate de un test live.
- Verificări: `dotnet build CititorRSS.Jaws.csproj -c Release --no-restore` reușit cu 0 avertismente și 0 erori; CoreSmoke a trecut cu 47 verificări; `git diff --check` a trecut (doar avertismentele Git informative LF→CRLF pentru fișiere existente).
- Verificare manuală: nu s-a folosit sesiunea NewsBlur reală și nu s-a simulat o indisponibilitate reală de feed; executabilul local este oferit utilizatorului pentru verificarea comportamentului cu datele sale. Schimbarea nu modifică interfața sau comenzile de tastatură.
- Decizie de siguranță: nu s-a creat distribuție și nu s-au modificat versiunea publică, Release-ul GitHub, WinGet sau GitHub Pages. Mesajele noi folosesc fallbackul românesc și trebuie traduse/verificate în cele opt limbi înaintea unei distribuții.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.

## 2026-09-12 — NewsBlur ca sursă principală pentru articole

- Scop aprobat: Orizont RSS să folosească NewsBlur ca punte de articole între calculatoare, păstrând administrarea abonamentelor și a folderelor în Orizont și fără schimbări structurale automate.
- Fișiere modificate în această etapă: `MainWindow.xaml.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Modificările locale preexistente din proiect au fost păstrate.
- Funcționare: actualizarea manuală a unui feed sau a tuturor, actualizarea la pornire și actualizarea automată configurată încearcă NewsBlur mai întâi. Articolele și stările continuă să folosească mecanismul bidirecțional existent. Se folosește RSS direct numai dacă feedul nu există în contul NewsBlur, cererea generală nu poate fi făcută sau cererea pentru acel feed eșuează; răspunsul valid fără articole nu este tratat drept eroare. Fără sesiune NewsBlur utilizabilă, aplicația păstrează actualizarea RSS existentă.
- Protecții: un feed reușit prin NewsBlur își curăță contorul și mesajul de erori RSS anterioare, fără a șterge statutul de trei luni fără articole noi. Oprirea anulează cererea curentă, păstrează articolele deja primite și nu pornește fallback după anulare. Eșecul trimiterii unor stări nu aruncă articolele deja preluate. Alertele sonore existente sunt păstrate. Feedurile/folderele și dezabonările nu se sincronizează automat; acestea rămân pe comenzile explicite existente.
- Verificări: `dotnet build CititorRSS.Jaws.csproj -c Release --no-restore` a reușit cu 0 avertismente și 0 erori; CoreSmoke a trecut cu 47 de verificări; LocalizationSmoke a trecut pentru `en-US`, `es-ES`, `fr-FR`, `de-DE`, `pt-BR`, `hu-HU` și `it-IT`. `git diff --check` a trecut; Git a emis doar avertismente informative că fișierele cu LF vor fi normalizate la CRLF.
- Limitări: nu s-a deschis aplicația cu profilul utilizatorului, nu s-a apelat NewsBlur și nu s-au schimbat datele remote; astfel, JAWS 2026 și sesiunea NewsBlur reală trebuie verificate manual. Textele noi sunt în română până la traducerea controlată pentru celelalte limbi.
- Decizie: nu s-a creat distribuție și nu s-au modificat versiunea publică, Release-ul GitHub, WinGet sau GitHub Pages.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.

## 2026-09-12 — Oglindirea bidirecțională automată a structurii NewsBlur

- Scop confirmat de utilizator: Orizont RSS și NewsBlur să oglindească abonamentele și organizarea, astfel încât modificările de pe un calculator să ajungă pe celelalte prin contul NewsBlur.
- Fișiere modificate în această etapă: `Models.cs`, `NewsBlurConnection.cs`, `MainWindow.xaml.cs`, `SettingsWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Modificările deja prezente în arborele de lucru au fost păstrate.
- Funcționare: după importul inițial confirmat pe un profil nou, oglindirea structurală rulează la pornirea aplicației când există sesiune NewsBlur și la intervalul setat dacă sincronizarea automată este activată. Adăugările, redenumirile și mutările de feeduri/foldere se propagă; schimbarea locală a URL-ului devine dezabonarea vechiului feed plus adăugarea celui nou. Ștergerea locală deja confirmată se transmite la sincronizare fără o a doua confirmare. Ștergerea remote elimină feedul și articolele locale asociate, iar celelalte calculatoare o vor prelua la următoarea sincronizare.
- Conflicte: editările independente se combină; conflictele simultane de nume/folder se amână în fundal și pot fi decise la o sincronizare manuală. După abonarea unui feed local nou, numele și folderul local sunt trimise la prima observare a feedului de către indexul NewsBlur, pentru a nu pierde eticheta locală.
- Referință API: documentația oficială NewsBlur — <https://www.newsblur.com/api> (abonare, redenumire, mutare și dezabonare feed; `delete_folder` nu este folosit).
- Protecții: înaintea oricărei mutații, exportul OPML și indexul `/reader/feeds?flat=true` trebuie să fie valide și să conțină aceleași adrese normalizate; la neconcordanță sincronizarea nu scrie și nu șterge nimic. Identitățile feedurilor asociate sunt verificate și duplicatele locale opresc oglindirea. Un folder local șters mută feedurile la nivelul superior, nu apelează endpointul NewsBlur `delete_folder`. Folderele complet goale nu pot fi oglindite, deoarece modelul local le deduce din feeduri.
- Verificări: build Release trecut cu 0 avertismente și 0 erori; CoreSmoke trecut cu 54 verificări, inclusiv snapshoturi complete/incomplete, normalizarea adreselor și clasificarea ștergerilor remote; LocalizationSmoke trecut pentru cele șapte limbi secundare; `git diff --check` trecut (numai avertismentele Git informative LF→CRLF).
- Verificare manuală rămasă: nu s-a folosit sesiunea reală NewsBlur și nu s-au modificat date remote. Noul flux trebuie testat de utilizator cu contul său și pe al doilea calculator; anunțurile/focalizarea necesită verificare țintită cu JAWS/NVDA. Mesajele noi folosesc fallbackul românesc și trebuie traduse în toate cele opt limbi înaintea unei distribuții.
- Decizie de siguranță: nu s-a creat distribuție și nu s-au schimbat versiunea publică, Release-ul GitHub, WinGet sau GitHub Pages.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.

## 2026-09-12 — Dialoguri accesibile pentru duplicate și decizii NewsBlur

- Scop aprobat: la sincronizarea feedurilor/folderelor NewsBlur, utilizatorul să poată înțelege care adrese locale sunt duplicate și dialogurile de decizie să primească focalizare previzibilă.
- Comportamente protejate: sincronizarea continuă să se oprească înaintea oricărei mutații când există duplicate locale sau snapshot NewsBlur nevalid; nu se comasează și nu se șterge nimic fără confirmare separată; sincronizarea nu este relansată automat după curățare.
- Fișiere modificate pentru această intervenție: `MainWindow.xaml.cs`, `NewsBlurConnection.cs`, `NewsBlurDecisionWindow.xaml`, `NewsBlurDecisionWindow.xaml.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Schimbările preexistente și modificările din turele anterioare au fost păstrate.
- Funcționare: oprirea pentru duplicate locale deschide acum o fereastră cu grupurile, numele, folderele și adresele normalizate. Utilizatorul poate anula sau poate deschide curățarea existentă, care cere confirmarea proprie; apoi sincronizarea trebuie pornită din nou. Duplicatele detectate în răspunsul NewsBlur sunt prezentate separat, fără a pretinde că pot fi șterse local de pe server. Confirmările sincronizării inițiale/manuale și rezolvările de conflict, precum și confirmările relevante din curățarea duplicatelor, folosesc dialoguri WPF cu focalizare explicită pe textul explicativ, Tab către opțiuni, Escape și opțiune sigură implicită.
- Verificări automate: build Release reușit cu 0 avertismente și 0 erori; CoreSmoke trecut cu 55 de verificări, inclusiv gruparea duplicatelor normalizate; LocalizationSmoke trecut pentru cele șapte limbi secundare; `git diff --check` trecut. Avertismentele Git privind normalizarea LF→CRLF sunt informative.
- Verificare manuală: nu a fost lansată aplicația pe profilul utilizatorului și nu s-a folosit o sesiune reală NewsBlur, pentru a evita pornirea sincronizării automate sau modificarea datelor remote. Confirmarea JAWS/NVDA pentru deschiderea dialogului, citirea textului și traseul Tab/Enter/Escape rămâne necesară.
- Localizare: textele noi folosesc fallbackul românesc și trebuie traduse/verificate în toate cele opt limbi înaintea unei distribuții.
- Decizie: nu s-a creat distribuție, nu s-au schimbat feeduri, setări, istoric sau date NewsBlur și nu s-a modificat release-ul public.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.
## 2026-09-12 — Mesaje accesibile pentru panourile fără selecție sau conținut

- Scop aprobat: panourile goale să explice clar când lipsește selecția unui feed/articol sau când lista nu are rezultate, fără să afișeze mesaj peste conținut real.
- Comportamente protejate: nu s-au schimbat selecția implicită, navigarea cu tastele, filtrele, feedurile, articolele, setările sau sincronizarea NewsBlur; mesajele sunt suprapuse fără a primi focus și se ascund când panoul are elemente sau text.
- Funcționare: lista Feeduri distinge lipsa tuturor feedurilor de o vizualizare fără feeduri; lista Articole distinge lipsa feedului selectat de filtre fără rezultate; cititorul anunță când nu este selectat un articol sau când conținutul selectat nu este afișat. Textele sunt regiuni live UI Automation, anunțate la schimbare când panoul respectiv are focus; mesajul rămâne în numele accesibil al controlului pentru navigarea ulterioară.
- Fișiere modificate: `MainWindow.xaml`, `MainWindow.xaml.cs`, cele opt resurse `Resources/UiStrings*.resx`, `tests/LocalizationSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`.
- Verificări: build Release trecut cu 0 avertismente și 0 erori; CoreSmoke trecut cu 56 de verificări; LocalizationSmoke trecut pentru `en-US`, `es-ES`, `fr-FR`, `de-DE`, `pt-BR`, `hu-HU` și `it-IT`, inclusiv cele șase texte noi; `git diff --check` trecut. Avertismentele Git LF→CRLF sunt informative.
- Verificare manuală: nu s-a lansat aplicația pe profilul utilizatorului, deoarece pornirea poate declanșa sincronizarea NewsBlur; JAWS/NVDA trebuie folosite pentru confirmarea anunțului live și a numelor accesibile, fără a schimba navigarea.
- Decizie: nu s-a creat distribuție și nu s-au modificat versiunea publică, GitHub, NewsBlur sau datele locale.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.
## 2026-09-12 — Numărul de articole la selectarea unui folder

- Simptom raportat: după selectarea unui folder, numărul de feeduri era corect, dar numărul de articole reflecta vizualizarea globală.
- Cauză: la restaurarea sesiunii putea rămâne activ modul „Citește acum”; schimbarea folderului actualiza lista de feeduri, dar nu dezactiva acest mod, iar lista de articole continua să agregheze toate feedurile.
- Corecție: schimbarea folderului dezactivează acum „Citește acum” înainte de reîncărcarea listei; articolele și numărul anunțat se calculează pentru folderul ales, cu filtrele existente păstrate.
- Comportamente protejate: selectarea și numărul feedurilor, filtrele active, navigarea cu tastatura, funcționarea explicită a butonului „Citește acum” și starea feedurilor/articolelor locale nu sunt modificate.
- Fișiere modificate: `MainWindow.xaml.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`.
- Verificări: build Release reușit cu 0 avertismente și 0 erori; CoreSmoke trecut cu 56 de verificări; LocalizationSmoke trecut pentru toate cele opt limbi; `git diff --check` trecut, cu avertismente informative de normalizare LF→CRLF. Verificarea manuală cu JAWS/NVDA rămâne necesară pentru anunțul numărului în folder.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.
- Decizie: nu s-a creat distribuție și nu s-au schimbat feeduri, setări, versiunea publică sau date NewsBlur.

## 2026-09-12 — Sincronizare NewsBlur după curățarea duplicatelor

- Cerință confirmată: după fiecare curățare reușită, sincronizarea feedurilor și folderelor să ruleze automat pentru ca NewsBlur să nu reimporte feedurile suprapuse eliminate.
- Comportamente păstrate: curățarea cere în continuare confirmarea existentă; o copie locală cu aceeași adresă normalizată ca feedul păstrat nu produce dezabonare; feedurile demonstrative sunt excluse; snapshoturile NewsBlur incomplete sau duplicate opresc mutațiile; inițializarea unui profil nou rămâne import unidirecțional.
- Modificări: feedurile suprapuse eliminate sunt adăugate în coada persistentă de dezabonare, cu identificator NewsBlur dacă este cunoscut sau adresă pentru rezolvare la sincronizare. Sincronizarea post-curățare pornește automat când contul este conectat. Dacă instrumentul este deschis dintr-o sincronizare deja în curs, sincronizarea curentă se reia după curățare, fără concurență. În prima inițializare, adresele marcate pentru ștergere nu sunt reimportate local, dar NewsBlur nu este modificat până la o sincronizare bidirecțională ulterioară.
- Fișiere modificate pentru această intervenție: `MainWindow.xaml.cs`, `NewsBlurConnection.cs`, `tests/CoreSmoke/Program.cs`, `docs/PROJECT-STATUS.md`, `docs/ROADMAP.md` și `WORKLOG.md`. Schimbările preexistente din arborele de lucru au fost păstrate.
- Verificări: build Release reușit fără avertismente și erori; CoreSmoke trecut cu 59 de verificări; LocalizationSmoke trecut pentru toate cele opt limbi; `git diff --check` trecut (doar avertismente Git informative despre normalizarea LF→CRLF).
- Verificare manuală: nu a fost lansată aplicația pe profilul utilizatorului și nu a fost folosită sesiunea NewsBlur reală, pentru a nu modifica abonamente remote. JAWS/NVDA și confirmarea pe contul real rămân de verificat de utilizator.
- Decizie: nu s-a creat distribuție și nu s-au schimbat feeduri locale, NewsBlur, versiunea publică, GitHub, WinGet sau GitHub Pages.
- Executabil local de test (framework-dependent): `bin/Release/net8.0-windows/Orizont.exe`; necesită .NET 8 Desktop Runtime.
