# Foaia de parcurs Orizont RSS

La 17 septembrie 2026 a fost creată local compilarea 1.6.0, în două forme: arhivă portabilă autonomă Windows x64 (`bin/Release/Orizont-RSS-1.6.0-win-x64.zip`, SHA-256 `bc4e0313350b25f26d7acc301721da81d10c845040a399a8f91ab8aea61c3b08`) și installer offline cu pachetul inclus (`bin/Release/OrizontSetup-1.6.0.exe`, SHA-256 `ff761660fe06d4910cb7c81c03d1d30a038aeaa41456f6e3ce229f7248d605a8`). Aceasta este o versiune de test locală; publicarea GitHub, pagina GitHub Pages și WinGet rămân la 1.5.4 până la o aprobare separată.

## Situația curentă

La 16 septembrie 2026, au fost aduse îmbunătățiri majore pe canalul de accesibilitate și parsare: bara de stare emite notificări UIA native (`RaiseNotificationEvent`), navigarea pe liste (`Ctrl+1`, `Ctrl+2`, `F6`) poziționează focusul nativ direct pe `ListBoxItem`-ul selectat, iar motorul RSS/Atom acceptă fusuri orare textuale și XML cu caractere speciale.

La 15 septembrie 2026, documentația a fost aliniată la starea reală a proiectului, iar lista fixă de regresie manuală pentru JAWS/NVDA este disponibilă în `docs/MANUAL-REGRESSION-CHECKLIST.md`. Codul aplicației nu a fost modificat în această etapă; nu s-a creat distribuție și nu s-au schimbat date locale sau NewsBlur.

În aceeași rundă, utilizatorul a confirmat cu JAWS 2026 că funcțiile din checklist, cu excepția verificării vizuale a temelor de contrast, funcționează conform așteptărilor. Rămân deschise doar confirmarea vizuală a temelor cu ajutorul unei persoane văzătoare și, dacă va fi necesar, o retestare NVDA.

La 15 septembrie 2026 a fost implementat footerul de atribuire pentru partajarea prin e-mail și WhatsApp. Acesta este localizat, include pagina de prezentare și sursa originală și menționează DeepL sau Google Translate numai când textul distribuit este tradus. Rezultatul Google Translate poate fi partajat direct din fereastra traducerii.

La 13 septembrie 2026, linkul versiunii portabile a fost corectat în toate cele opt limbi pentru descărcarea directă a arhivei ZIP 1.5.4. Commitul `7606912` a fost publicat pe `main`, iar pagina GitHub Pages live a fost verificată; linkul instalatorului a rămas neschimbat.

La 14 septembrie 2026, opțiunea Google Translate a fost adăugată și în meniurile contextuale ale listei de articole și conținutului din fereastra principală; cititorul Orizont avea deja opțiunea. Utilizatorul a confirmat că traducerea din meniul contextual al zonei de conținut funcționează bine. Verificarea din lista de articole și confirmarea specifică JAWS/NVDA a focalizării/anunțurilor rămân deschise.

La 16 septembrie 2026, coada de dezabonare NewsBlur a fost întărită pentru adrese canonice: aliasurile `www` și o potrivire unică a numelui leagă acum feedul local de abonamentul remote echivalent. Căile diferite nu sunt unite automat fără o potrivire sigură, iar feedurile confirmate ca șterse nu mai sunt reimportate.

Ultima versiune descărcabilă publică este 1.5.4. Release-ul `v1.5.4` este public și include cele șase assets pregătite manual — installerul, arhiva portabilă, arhiva sursă și cele trei fișiere SHA-256 — plus cele două arhive automate ale sursei generate de GitHub. Hash-urile afișate pentru assetsurile pregătite corespund fișierelor locale. Toate cele opt pagini GitHub Pages au fost actualizate la 1.5.4 și publicate pe `main`.

Localizarea este disponibilă în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană. Verificarea completă a tuturor resurselor a trecut pentru cele opt limbi; inventarul curent este de 1.181 de chei și 1.100 de referințe UI.

Traducerea Google Translate fără cheie API este implementată local ca opțiune online de compromis: utilizatorul o alege explicit, confirmă trimiterea textului articolului, iar traducerea apare într-o fereastră separată fără să schimbe originalul. În Setări Inteligență artificială se aleg limba-sursă (autodetectare implicită) și limba-țintă (limba interfeței implicită sau o alegere explicită din 38 de limbi uzuale); preferințele se păstrează și în backup. Lista de limbi este selectată, nu exhaustivă. Endpointul web este neoficial și se poate schimba sau limita. Verificarea manuală JAWS/NVDA rămâne necesară; nu s-a creat o distribuție.

DeepL și Google Translate folosesc acum aceeași rezolvare a sursei: text lizibil extras din pagina articolului când nu există deja conținut complet curățat sau când textul cunoscut conține indicii clare de chrome. Extractorul elimină, în toate cele opt limbi, etichete de navigare/subsol, comentarii, oferte afiliate și blocuri editoriale inline întâlnite la pagini precum GSMArena, iar aceeași curățare se aplică textului local afișat în cititorul Orizont, cu fallback controlat la textul RSS normalizat. Astfel, traducerea și citirea originalului nu ar trebui să includă reclamele și meniurile paginii; dacă pagina nu poate fi reîncărcată, aplicația nu pierde conținutul disponibil local.

La 13 septembrie 2026, auditul exhaustiv a corectat șirurile deteriorate din resursele germane, spaniole, franceze și portugheze, ghidurile localizate și istoricul vechi din `WORKLOG.md`; a eliminat 11 chei vechi/orfane din toate cele opt fișiere de resurse. Inventarul la audit era de 1.149 de chei în fiecare limbă și 1.070 de referințe UI detectate în C#/XAML; după ultimele completări, inventarul curent verificat este de 1.181 de chei și 1.100 de referințe UI. Toate culturile trec verificarea cheilor, a valorilor goale, a formatelor, termenilor protejați, structurii, traducerilor românești rămase și a fragmentelor de codare deteriorată. Smoke testul verifică încărcarea runtime, iar CI compilează și instalatorul. Rezultatele sunt locale; calitatea editorială și validarea efectivă a anunțurilor cu JAWS/NVDA rămân verificări umane.

La 14 septembrie 2026, o revizie a meniurilor și mesajelor Setări a uniformizat denumirea categoriilor aplicație/voce/AI și textele de ghidare Gemini/DeepL pentru limbile în care numele căii nu corespundea meniului. Au fost adăugate aserțiuni automate pentru etichetele de setări în toate cele șapte limbi secundare. Perechea textuală exactă „Configuración/Configuration” nu apare în resursele spaniole/XAML; utilizatorul a confirmat ulterior că forma engleză nu mai apare. Testele și buildul trec. Nu s-a creat distribuție.

La 15 septembrie 2026, mesajul pentru panoul gol „Conținut articol” a primit un nivel vizual explicit peste TextBox-ul read-only, pentru a fi afișat sigur când nu există articol sau text. Comportamentul de actualizare și numele accesibil rămân neschimbate; verificarea manuală cu JAWS/NVDA este necesară.

Confirmarea manuală ulterioară arată că mesajul este anunțat în bara de stare, dar nu este vizibil în fereastra propriu-zisă. Pentru moment, bara de stare este considerată comportamentul accesibil funcțional; afișarea vizuală directă rămâne o îmbunătățire de layout neurgentă.

Evaluarea utilizatorului: bara de stare este bună, însă fereastra propriu-zisă poate fi făcută mai clară într-o etapă ulterioară.

La 12 septembrie 2026 au fost adăugate stări goale accesibile pentru listele Feeduri/Articole și zona Conținut articol. Textele diferențiază lipsa feedurilor, lipsa selecției și rezultatele absente după filtre; dispar când există conținut în panoul respectiv și au resurse pentru toate cele opt limbi. Anunțarea vizuală și UI Automation este implementată; validarea manuală cu JAWS/NVDA rămâne necesară.

La 12 septembrie 2026 s-a corectat și schimbarea din „Citește acum” către un folder: selectarea folderului dezactivează vizualizarea agregată, astfel încât articolele și numărul vocal să provină doar din feedurile folderului.

La 13 septembrie 2026, rapoartele locale de eroare au identificat o excepție la schimbarea vederii „River of News”: filtrul era căutat după eticheta românească afișată, ceea ce eșua când textul UI nu corespundea exact. Selectarea folosește acum cheile stabile `Tag` prin helperul existent; verificarea manuală cu JAWS/NVDA rămâne necesară.

La 13 septembrie 2026, revizuirea fluxului de închidere a identificat că o sincronizare manuală a articolelor/stărilor NewsBlur nu era urmărită înainte de salvarea datelor locale. Închiderea îi cere acum anularea și așteaptă finalizarea înainte de salvare; operațiile automate noi nu pornesc după începerea închiderii. Utilizatorul confirmă că închiderea obișnuită funcționează; închiderea în timpul sincronizării și anunțul JAWS rămân de verificat.

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
3. Verificarea instalatorului public descărcat direct de pe GitHub — utilizatorul confirmă că l-a instalat și verificat pe acest PC și pe celălalt; punctul este închis.
4. Retestarea WinGet prin instalarea și dezinstalarea pachetului în regim non-administrator — se poate face după ce PR-ul oficial este aprobat/combinat și pachetul apare în catalog. Validarea locală a manifestului nu înlocuiește testul instalării din catalog.
5. Actualizarea WinGet pentru 1.5.4 este în PR-ul oficial `microsoft/winget-pkgs#431971`; commitul remote `524e3e5` conține setul 1.5.4 și a trecut `winget validate` local. Titlul și descrierea PR-ului au fost actualizate și verificate prin API. Emailul primit confirmă că PR-ul așteaptă aprobarea unui moderator Microsoft; nu este necesară o acțiune din partea utilizatorului acum. CLA este semnat și recunoscut.
6. Promovarea paginii publice și colectarea feedbackului inițial.
7. Menținerea proiectului Windows fără schimbarea funcțiilor stabile; orice modificare nouă rămâne supusă regulilor din `AGENTS.md`.

## Cerință de securitate GitHub — acțiune necesară

- Contul GitHub `grifnas` trebuie să activeze autentificarea în doi pași (2FA) până la **21 octombrie 2026, ora 00:00 UTC**.
- Configurarea se face din pagina oficială: <https://github.com/settings/two_factor_authentication/setup/intro>.
- Se recomandă o aplicație de autentificare sau un passkey; codurile de recuperare trebuie salvate într-un loc sigur.
- Cerința protejează accesul la GitHub și nu necesită nicio modificare în aplicația Orizont RSS, în release-uri sau în GitHub Pages.
- Până la confirmarea activării, starea contului va fi verificată periodic și utilizatorului i se va reaminti această acțiune.

## Idei pentru etape ulterioare

- îmbunătățiri suplimentare pentru partajare și conversațiile AI;
- navigare alfabetică și ordine stabilă pentru listele de feeduri — implementată local la 16 septembrie 2026;
- extinderea testelor automate pentru scenarii cu colecții mari de articole;
- autoetichetarea articolelor pe baza unor reguli definite de utilizator pentru etichetele existente: cuvinte-cheie sau expresii căutate în titlu, descriere și conținut, cu normalizarea literelor/diacriticelor, activare opțională și previzualizare înainte de aplicare;
- preluarea automată a textului complet per feed (Auto Full-Text): opțiune configurabilă per feed pentru descărcarea și curățarea automată a textului integral la preluarea articolelor RSS scurte;
- sumar zilnic (Daily Digest): modul interactiv și lectură continuă audio (TTS) pentru parcurgerea rapidă a celor mai importante articole necitite din ultimele 24 de ore (sau interval personalizat), cu grupare pe categorii/foldere, prioritizare după cuvinte-cheie și acțiuni rapide (Enter/Space);
- ghid rapid accesibil de comenzi de tastatură: dialog dedicat accesibil (de ex. prin `F2` sau `Ctrl+?`) pentru consultarea și căutarea rapidă a tuturor scurtăturilor din aplicație;
- mecanism de arhivare locală comprimată: arhivare opțională a articolelor vechi pentru păstrarea performanței instantanee a arhivei active la peste 50.000 de articole.

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
7. **Sincronizare automată** — implementată pentru structura feedurilor/folderelor și pentru articole/stări. În Setări există acum bife separate pentru fiecare sincronizare la pornire; pe un profil nou, articolele/stările așteaptă inițializarea feedurilor/folderelor. Sincronizarea periodică completă are propriul comutator și interval configurabil (15/30/60/180 de minute). Comenzile manuale rămân disponibile indiferent de bifele de pornire. Sunt protejate rulările concurente, conflictele de metadate se amână în fundal, iar rezultatul se anunță prin bara de stare. Prima inițializare pe un profil nou rămâne confirmată.
8. **Dezabonări sincronizate** — implementate bidirecțional: ștergerea locală confirmată pune feedul într-o coadă persistentă și se transmite la următoarea sincronizare; ștergerea făcută în NewsBlur elimină feedul și articolele sale locale. Oglindirea se execută și în fundal după inițializare. Ștergerea individuală a articolelor nu este documentată de API și rămâne locală.
9. **Conflicte avansate** — implementate local la 12 septembrie 2026: editările independente de nume și folder se combină; când același câmp a fost schimbat diferit pe ambele părți, dialogul oferă păstrarea valorii locale, preluarea valorii NewsBlur sau amânarea acelui câmp. Amânarea nu schimbă valorile sau baza de sincronizare. Confirmările și conflictele folosesc o fereastră cu focalizare inițială pe textul explicativ, navigare la opțiuni prin Tab și alegere sigură implicită. Lista locală de feeduri duplicate este descrisă într-un dialog separat; curățarea nu începe fără alegere și confirmare distincte. Verificarea manuală JAWS/NVDA rămâne necesară.
10. **Localizare înainte de distribuție** — traducerea și verificarea tuturor mesajelor noi în cele opt limbi; distribuția nu este autorizată prin această foaie de parcurs.
11. **Inițializare și ștergere sigură pe mai multe calculatoare** — implementate la 12 septembrie 2026: profilul nou importă întâi din NewsBlur, apoi trece la bidirecțional; ștergerea unui folder mută feedurile la nivelul superior, iar ștergerea unui feed deja confirmată local se propagă remote.
12. **NewsBlur ca sursă principală pentru articole** — implementat local la 12 septembrie 2026: actualizarea manuală, cea de la pornire și cea automată încearcă mai întâi NewsBlur; RSS direct este fallback doar pentru feedurile absente sau pentru care cererea de articole eșuează. Oglindirea abonamentelor/folderelor are acum verificări complete de snapshot înainte de mutații; noile fluxuri de ștergere și sincronizare automată structurală trebuie testate manual pe două dispozitive. Textele noi trebuie traduse în cele opt limbi înainte de distribuție.

Nu se sincronizează parole, chei API, notițe AI sau setări vocale. Acestea rămân locale. Niciuna dintre etapele de mai sus nu autorizează singură o distribuție publică; fiecare modificare necesită verificare și consemnare în `WORKLOG.md`.

## Capturi pentru pagina publică

Galeria de previzualizări este activă în toate cele opt pagini GitHub Pages. Cele patru imagini PNG din `docs/assets/screenshots/` sunt generate offline din componente WPF, în limba engleză ca limbă internațională comună, și folosesc exclusiv date demonstrative; nu conțin fluxuri personale sau chei API. Textul alternativ și descrierile sunt localizate pentru fiecare limbă.

### Direcția aprobată pentru versiunea 1.6: Productivitate și Feedback Audio

Funcțiile aprobate pentru versiunea 1.6 sunt acum integrate în sursa locală, cu scopul de a crește viteza de navigare și claritatea auditivă fără a adăuga verbiozitate:

1. **River of News (Cronologia Completă)** — implementată local: articolele necitite din feeduri sunt agregate, deduplicate conform setării existente și ordonate cronologic; comanda mută focusul în lista de articole.
2. **Earcons (Navigare Audio Spațială)** — implementate local: semnale sonore la schimbarea folderului și la atingerea capetelor listelor Feeduri și Articole cu săgețile Sus/Jos.
3. **Rezultate accesibile pentru cuvinte-cheie** — implementate local: alertele automate caută cuvintele separate prin virgulă în titlul și conținutul lizibil al articolelor noi; textul din scripturi, stiluri și atribute HTML nu este folosit, iar diacriticele românești sunt normalizate. Comanda „Caută în articolele existente după cuvinte-cheie” scanează arhiva locală, ordonează cele mai recente potriviri și afișează maximum trei în același dialog accesibil. Fiecare rând anunță titlul și cuvântul-cheie potrivit; numele feedului și celelalte date tehnice sunt omise. Enter/„Deschide articolul” deschide cititorul Orizont, „Adaugă la Mai târziu” salvează local, iar Escape închide fereastra și revine la focusul anterior. Dacă arhiva nu conține potriviri, rezultatul este anunțat în bara de stare. Setările explică diferența dintre alertele pentru articole noi și căutarea arhivei. Textele sunt localizate în toate cele opt limbi.

Verificare automată la 13 septembrie 2026: Release build fără avertismente/erori, CoreSmoke 75 verificări, LocalizationSmoke 1.154 de resurse/limbă și validator fără erori în toate cele șapte culturi secundare. Confirmarea focalizării, anunțului listei și traseului săgeți/Enter/Tab/Escape cu JAWS/NVDA rămâne de făcut manual de utilizator. Modificările sunt locale; nu s-a creat și nu s-a publicat o distribuție.

### Aspect și contrast — implementat local la 15 septembrie 2026

Setări aplicație începe acum cu selectorul de temă. Sunt disponibile `Windows automat` (tema implicită, sincronizată cu Contrast themes Windows), negru pe alb, alb pe negru, negru pe galben și galben pe albastru închis. Alegerea este persistentă, inclusă în backup și se aplică imediat după Salvare. Resursele de interfață sunt semantice și dinamice, pentru ca textul, controalele, meniurile, bordurile și focusul să rămână lizibile. Următorul pas este confirmarea manuală cu JAWS/NVDA; nu se creează distribuție fără cerere expresă.
