# Foaia de parcurs Orizont RSS

## Situația curentă

Versiunea 1.5.3 este o versiune stabilizată. Funcțiile de bază pentru feeduri, articole, organizare, citire accesibilă, AI Gemini, traducere DeepL, voce, alerte sonore, meniu contextual AI, backup și localizare sunt implementate.

Localizarea este disponibilă în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană. Verificarea automată curentă confirmă resurse complete și fără erori pentru toate cele opt limbi.

Distribuția 1.5.3 Windows x64 și arhiva sursă au fost verificate și sunt disponibile.

## Workflow obligatoriu pentru orice versiune nouă

Pentru ca instalatorul și versiunea portabilă să conțină aceleași modificări, se folosește aceeași stare a sursei și aceeași versiune:

1. Modificarea codului și actualizarea documentației, limbilor și `WORKLOG.md`.
2. Build, smoke tests și verificări manuale țintite cu JAWS/NVDA.
3. Creșterea versiunii (de exemplu `1.5.3` → `1.5.4`).
4. Construirea arhivei portabile Windows x64.
5. Construirea instalatorului autonom din aceeași stare a sursei.
6. Actualizarea în instalator a versiunii, adresei arhivei și hash-ului SHA-256.
7. Încărcarea în același release GitHub a arhivei portabile și a `OrizontSetup.exe`.
8. Actualizarea paginii GitHub Pages și verificarea linkurilor publice.

Nu se publică un singur pachet izolat. O nouă distribuție se creează numai la cererea expresă și după verificarea tuturor limbilor.

## Următoarea etapă

1. Publicarea modificărilor SEO pe ramura GitHub Pages și verificarea adreselor publice `sitemap.xml` și `robots.txt` — verificată la 9 septembrie 2026.
2. Verificarea manuală cu JAWS/NVDA a comenzilor pentru exemplele RSS: adăugare locală, verificare online, focalizare pe primul articol și eliminare cu confirmare.
3. Verificarea instalatorului public descărcat direct de pe GitHub, inclusiv limbă, focus, bară de stare, pictogramă desktop, instalare și dezinstalare — disponibil public și verificat ca asset; retestarea manuală JAWS/NVDA rămâne criteriu separat.
4. Retestarea WinGet prin instalatorul public, inclusiv instalare și dezinstalare în regim non-administrator — clientul local este disponibil, manifestul trece validarea, dar pachetul nu este încă în catalog.
5. Manifestul WinGet a fost trimis prin PR-ul oficial `microsoft/winget-pkgs#431971`; în continuare se urmăresc validările Microsoft și este necesară semnarea CLA de către autor.
6. Promovarea paginii publice și colectarea feedbackului inițial.
7. Menținerea proiectului Windows fără schimbarea funcțiilor stabile; orice modificare nouă rămâne supusă regulilor din `AGENTS.md`.

## Cerință de securitate GitHub — acțiune necesară

- Contul GitHub `grifnas` trebuie să activeze autentificarea în doi pași (2FA) până la **21 octombrie 2026, ora 00:00 UTC**.
- Configurarea se face din pagina oficială: <https://github.com/settings/two_factor_authentication/setup/intro>.
- Se recomandă o aplicație de autentificare sau un passkey; codurile de recuperare trebuie salvate într-un loc sigur.
- Cerința protejează accesul la GitHub și nu necesită nicio modificare în aplicația Orizont RSS, în release-uri sau în GitHub Pages.
- Până la confirmarea activării, starea contului va fi verificată periodic și utilizatorului i se va reaminti această acțiune.

## Idei pentru etape ulterioare

- furnizor de traducere alternativ, fără cheie, doar dacă poate fi folosit legal și stabil;
- îmbunătățiri suplimentare pentru partajare și conversațiile AI;
- extinderea testelor automate pentru scenarii cu colecții mari de articole.

## Integrare viitoare cu NewsBlur

NewsBlur este adăugat oficial pe roadmap ca furnizor opțional de sincronizare. API-ul oficial este REST, nu cere o cheie API separată, dar necesită autentificarea și consimțământul utilizatorului. Documentația de referință este disponibilă la <https://www.newsblur.com/api>.

Implementarea va fi etapizată și nu începe automat:

1. definirea unui adaptor separat pentru servicii externe, fără modificarea fluxului RSS local;
2. conectare NewsBlur cu autentificare explicită și posibilitate de deconectare/revocare;
3. import inițial al abonamentelor și folderelor — implementat local la 9 septembrie 2026;
4. sincronizare controlată a feedurilor, folderelor, articolelor și a stărilor citit/necitit, favorit și etichete — etapa de import bidirecțional controlat este implementată local la 9 septembrie 2026;
5. limitarea cererilor, reluare prudentă după erori și mesaje accesibile pentru JAWS/NVDA;
6. păstrarea OPML ca metodă de rezervă, astfel încât aplicația să rămână complet funcțională fără NewsBlur.

Integrarea bidirecțională va fi propusă pentru implementare numai după aprobarea expresă a utilizatorului și după verificarea condițiilor actuale ale serviciului.

### Stadiul primei etape

Fereastra locală de autentificare este implementată și testată automat. Ea folosește endpointurile oficiale `/api/login`, `/api/signup` și `/api/logout`, păstrează numai sesiunea protejată pentru utilizatorul Windows curent și oferă o deconectare explicită. Fluxul OAuth cu Google, Facebook sau alți furnizori rămâne blocat până la primirea acreditărilor OAuth de la NewsBlur; nu se folosesc butoane care ar putea colecta parole sau tokenuri în afara fluxului aprobat.

Aceste idei nu sunt angajamente de implementare și nu modifică funcționalitatea curentă.

### Stadiul importului inițial

Importul controlat al abonamentelor și folderelor NewsBlur este implementat local. Comanda este disponibilă în `Feeduri → Servicii externe → Sincronizează abonamentele NewsBlur`. Aplicația citește exportul OPML oficial, elimină duplicatele după adresă și afișează înainte de confirmare numărul de abonamente noi, feeduri actualizate și foldere noi. Operația nu șterge feeduri locale și nu modifică articolele, favoritele, lista „Mai târziu” sau etichetele.

Sincronizarea controlată a feedurilor, folderelor și articolelor lipsă este implementată local. Următoarele etape rămân sincronizarea automată în fundal, ștergerile sincronizate și rezolvarea avansată a conflictelor; acestea vor necesita verificare manuală și aprobare explicită separată.

## Capturi pentru pagina publică

Galeria de previzualizări este activă în toate cele opt pagini GitHub Pages. Cele patru imagini PNG din `docs/assets/screenshots/` sunt generate offline din componente WPF, în limba engleză ca limbă internațională comună, și folosesc exclusiv date demonstrative; nu conțin fluxuri personale sau chei API. Textul alternativ și descrierile sunt localizate pentru fiecare limbă.
