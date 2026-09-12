# Foaia de parcurs Orizont RSS

## Situația curentă

Ultima versiune descărcabilă publică este 1.5.3. Sursa și tagul `v1.5.4` au fost publicate; Release-ul cu installerul și arhiva portabilă este în curs de atașare. Până atunci, GitHub Pages indică versiunea 1.5.3.

Localizarea este disponibilă în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană. Mesajele adăugate în etapa NewsBlur au fost traduse, iar verificarea completă a tuturor resurselor a trecut pentru cele opt limbi.

La 12 septembrie 2026 au fost adăugate stări goale accesibile pentru listele Feeduri/Articole și zona Conținut articol. Textele diferențiază lipsa feedurilor, lipsa selecției și rezultatele absente după filtre; dispar când există conținut în panoul respectiv și au resurse pentru toate cele opt limbi. Anunțarea vizuală și UI Automation este implementată; validarea manuală cu JAWS/NVDA rămâne necesară.

La 12 septembrie 2026 s-a corectat și schimbarea din „Citește acum” către un folder: selectarea folderului dezactivează vizualizarea agregată, astfel încât articolele și numărul vocal să provină doar din feedurile folderului.

Distribuția 1.5.4 Windows x64, installerul offline și arhiva sursă au fost generate și verificate din aceeași stare a sursei. Asseturile sunt pregătite local pentru Release.

La 12 septembrie 2026, după curățarea reușită a duplicatelor, sincronizarea feedurilor/folderelor NewsBlur pornește automat dacă există o sesiune conectată. Feedurile suprapuse eliminate prin alegere explicită sunt puse în coada de dezabonare, iar adresa păstrată nu este atinsă; o curățare lansată dintr-o sincronizare o face pe aceasta să reia operația cu lista actualizată. Prima inițializare a unui profil nou rămâne unidirecțională și nu dezabonează remote.

## Workflow obligatoriu pentru orice versiune nouă

Pentru ca instalatorul și versiunea portabilă să conțină aceleași modificări, se folosește aceeași stare a sursei și aceeași versiune:

1. Modificarea codului și actualizarea documentației, limbilor și `WORKLOG.md`.
2. Build, smoke tests și verificări manuale țintite cu JAWS/NVDA.
3. Creșterea versiunii (de exemplu `1.5.3` → `1.5.4`).
4. Construirea arhivei portabile Windows x64.
5. Construirea instalatorului autonom din aceeași stare a sursei, cu arhiva și hash-ul SHA-256 încorporate.
6. Verificarea versiunii installerului și a hash-ului înaintea extragerii; varianta 1.5.4 fără payload nu descarcă versiunea publică 1.5.3.
7. Încărcarea arhivei portabile și a `OrizontSetup.exe` în GitHub numai după o cerere expresă separată de publicare.
8. Actualizarea paginii GitHub Pages numai în cadrul aceleiași publicări aprobate.

Nu se publică un singur pachet izolat. O nouă distribuție se creează numai la cererea expresă și după verificarea tuturor limbilor.

## Următoarea etapă

1. Publicarea modificărilor SEO pe ramura GitHub Pages și verificarea adreselor publice `sitemap.xml` și `robots.txt` — verificată la 9 septembrie 2026.
2. Verificarea manuală cu JAWS/NVDA a comenzilor pentru exemplele RSS: adăugarea locală și navigarea au fost confirmate de utilizator la 12 septembrie 2026, iar utilizatorul confirmă că feedul de test este deja în lista de surse. Testul online a identificat hostul HotNews vechi, care nu se rezolvă DNS; catalogul local folosește acum `https://hotnews.ro/feed`, adresă care răspunde cu tip de conținut RSS. Rămân de confirmat în aplicație actualizarea efectivă a feedului, focalizarea pe primul articol și eliminarea cu confirmare; nu este necesară adăugarea din nou a feedului de test.
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

Integrarea bidirecțională a fost aprobată de utilizator. Modificările rămân etapizate, documentate și fără distribuție implicită.

### Stadiul primei etape

Fereastra locală de autentificare este implementată și testată automat. Ea folosește endpointurile oficiale `/api/login`, `/api/signup` și `/api/logout`, păstrează numai sesiunea protejată pentru utilizatorul Windows curent și oferă o deconectare explicită. Fluxul OAuth cu Google, Facebook sau alți furnizori rămâne blocat până la primirea acreditărilor OAuth de la NewsBlur; nu se folosesc butoane care ar putea colecta parole sau tokenuri în afara fluxului aprobat.

Aceste idei nu sunt angajamente de implementare și nu modifică funcționalitatea curentă.

### Stadiul importului inițial

Importul controlat al abonamentelor și folderelor NewsBlur este implementat local. Comanda este disponibilă în `Feeduri → Servicii externe → Sincronizează abonamentele NewsBlur`. Aplicația citește exportul OPML oficial, elimină duplicatele după adresă și afișează înainte de confirmare numărul de abonamente noi, feeduri actualizate și foldere noi. Operația nu șterge feeduri locale și nu modifică articolele, favoritele, lista „Mai târziu” sau etichetele.

Oglindirea bidirecțională a feedurilor și folderelor este implementată local. Prima conectare a unui profil nou rămâne un import confirmat, unidirecțional, din NewsBlur. După inițializare, structura se oglindește automat la pornire și la intervalul configurat dacă utilizatorul a activat sincronizarea automată; comanda manuală pornește aceeași operație imediat. Feedurile noi, redenumirile, mutările și editarea URL-ului (dezabonare veche + abonare nouă) se propagă în ambele direcții. O ștergere locală deja confirmată se trimite fără o a doua confirmare; dispariția remote este aplicată local și se va propaga celorlalte dispozitive la sincronizarea lor. Înainte de orice mutație, OPML și feed-index trebuie să fie valide și să conțină aceleași adrese normalizate; în lipsa concordanței nu se fac schimbări. Conflictele concurente de metadate nu deschid dialoguri în fundal și rămân amânate pentru rezolvare manuală. Folderele goale nu pot fi reprezentate local; ștergerea unui folder local mută feedurile în Neorganizate și nu apelează endpointul NewsBlur `delete_folder`.

Retenția configurată rămâne o regulă locală Orizont: articolele obișnuite expirate sunt curățate după o încercare de actualizare chiar dacă aceasta a eșuat, iar importul NewsBlur ignoră articolele expirate pentru a nu le readuce în listele locale. Favoritele și articolele „Mai târziu”, inclusiv elementele salvate NewsBlur potrivit mapării alese, rămân protejate. NewsBlur nu primește cereri de ștergere individuală.

Inițializarea pe un profil nou este unidirecțională din NewsBlur spre Orizont. Importul cere confirmare, păstrează feedurile locale care nu există remote și nu trimite date către NewsBlur; feedurile demonstrative sunt excluse. Marcajul reușitei se salvează local pentru contul curent și nu intră în backup, astfel încât un calculator nou să pornească prin import. După finalizare, sincronizările feedurilor/folderelor sunt bidirecționale; o instalare deja asociată este recunoscută pentru a evita reinițializarea după actualizarea aplicației. Sincronizarea stărilor nu pornește înaintea acestui pas.

Ștergerea locală a unui folder mută feedurile în „Neorganizate”; la sincronizare, NewsBlur primește o mutare la nivel superior, nu dezabonare. Ștergerea unui feed din „Neorganizate” folosește în continuare coada explicit confirmată de dezabonare. Nu se apelează endpointul NewsBlur `delete_folder`, care dezabonează feedurile aflate în folder.

### Direcția aprobată pentru extinderea sincronizării NewsBlur

Etapele se abordează pe rând; dacă două etape sunt suficient de independente, pot fi implementate în aceeași sesiune, cu verificări separate:

1. **Colecțiile de articole salvate** — mapare configurabilă între `Saved Stories`/stelele NewsBlur și `Favorite`, `Mai târziu` sau ambele în Orizont RSS — implementat local la 9 septembrie 2026.
2. **Etichete bidirecționale** — sincronizarea etichetelor articolelor și o regulă explicită pentru conflicte — implementat împreună cu etapa 1 la 9 septembrie 2026.
3. **Feeduri și foldere complete** — redenumire și mutare în ambele direcții, cu bază locală și protecție la modificări concurente; implementat local la 9 septembrie 2026, cu ștergerea dezactivată implicit.
4. **Vizualizări speciale** — liste virtuale pentru necitite, Saved Stories și toate articolele NewsBlur; implementat local la 9 septembrie 2026. River of News rămâne o extindere ulterioară.
5. **Text complet la cerere** — preluarea textului extins NewsBlur numai pentru articolul cerut, cu fallback RSS și feedback accesibil; implementat local la 9 septembrie 2026.
6. **Stări agregate** — marcarea ca citite a folderului selectat sau a tuturor feedurilor NewsBlur; implementat local la 9 septembrie 2026.
7. **Sincronizare automată** — implementată pentru structura feedurilor/folderelor și pentru articole/stări: oglindirea structurii la pornire, apoi la intervalul configurabil (15/30/60/180 de minute) când opțiunea este activată. Sunt protejate rulările concurente, conflictele de metadate se amână în fundal, iar rezultatul se anunță prin bara de stare. Prima inițializare pe un profil nou rămâne confirmată.
8. **Dezabonări sincronizate** — implementate bidirecțional: ștergerea locală confirmată pune feedul într-o coadă persistentă și se transmite la următoarea sincronizare; ștergerea făcută în NewsBlur elimină feedul și articolele sale locale. Oglindirea se execută și în fundal după inițializare. Ștergerea individuală a articolelor nu este documentată de API și rămâne locală.
9. **Conflicte avansate** — implementate local la 12 septembrie 2026: editările independente de nume și folder se combină; când același câmp a fost schimbat diferit pe ambele părți, dialogul oferă păstrarea valorii locale, preluarea valorii NewsBlur sau amânarea acelui câmp. Amânarea nu schimbă valorile sau baza de sincronizare. Confirmările și conflictele folosesc o fereastră cu focalizare inițială pe textul explicativ, navigare la opțiuni prin Tab și alegere sigură implicită. Lista locală de feeduri duplicate este descrisă într-un dialog separat; curățarea nu începe fără alegere și confirmare distincte. Verificarea manuală JAWS/NVDA rămâne necesară.
10. **Localizare înainte de distribuție** — traducerea și verificarea tuturor mesajelor noi în cele opt limbi; distribuția nu este autorizată prin această foaie de parcurs.
11. **Inițializare și ștergere sigură pe mai multe calculatoare** — implementate la 12 septembrie 2026: profilul nou importă întâi din NewsBlur, apoi trece la bidirecțional; ștergerea unui folder mută feedurile la nivelul superior, iar ștergerea unui feed deja confirmată local se propagă remote.
12. **NewsBlur ca sursă principală pentru articole** — implementat local la 12 septembrie 2026: actualizarea manuală, cea de la pornire și cea automată încearcă mai întâi NewsBlur; RSS direct este fallback doar pentru feedurile absente sau pentru care cererea de articole eșuează. Oglindirea abonamentelor/folderelor are acum verificări complete de snapshot înainte de mutații; noile fluxuri de ștergere și sincronizare automată structurală trebuie testate manual pe două dispozitive. Textele noi trebuie traduse în cele opt limbi înainte de distribuție.

Nu se sincronizează parole, chei API, notițe AI sau setări vocale. Acestea rămân locale. Niciuna dintre etapele de mai sus nu autorizează singură o distribuție publică; fiecare modificare necesită verificare și consemnare în `WORKLOG.md`.

## Capturi pentru pagina publică

Galeria de previzualizări este activă în toate cele opt pagini GitHub Pages. Cele patru imagini PNG din `docs/assets/screenshots/` sunt generate offline din componente WPF, în limba engleză ca limbă internațională comună, și folosesc exclusiv date demonstrative; nu conțin fluxuri personale sau chei API. Textul alternativ și descrierile sunt localizate pentru fiecare limbă.
