# Orizont RSS — stare și plan de proiect

Ultima actualizare: 12 septembrie 2026

## Scopul documentului

Acest document păstrează separat ceea ce este deja realizat, ceea ce este în lucru și ceea ce este doar planificat. O idee planificată nu este considerată o cerere de implementare până când utilizatorul nu o confirmă explicit.

## Realizat

- Sursa, tagul `v1.5.4` și Release-ul public sunt publicate pe GitHub. Sunt atașate cele șase assets: installer, arhivă portabilă, arhivă sursă și fișierele lor SHA-256; hash-urile asseturilor corespund fișierelor pregătite local.
- Pagina GitHub Pages este actualizată și publicată pentru versiunea 1.5.4 în română și în celelalte șapte limbi, inclusiv în metadatele SEO.
- Sunt implementate feedurile RSS, folderele, actualizarea, filtrele, favoritele, lista „Mai târziu”, etichetele și curățarea duplicatelor.
- Sunt implementate cititorul integrat accesibil, citirea vocală, căutarea, copierea, partajarea, backupul și restaurarea.
- Setările eSpeak NG oferă acum toate cele 104 variante incluse (inclusiv Ian, Mike2 și Reed), selectarea separată a limbii și reglarea înălțimii/intonației; alegerea variantei se păstrează și în backup.
- Sunt integrate funcțiile Gemini și traducerea DeepL, cu activare explicită de către utilizator.
- Interfața este disponibilă în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană.
- Panourile fără conținut relevant afișează mesaje distincte pentru lipsa feedurilor, lipsa selecției și filtre fără rezultate; mesajele dispar când există elemente/conținut afișat. Sunt anunțate ca regiuni live și incluse în numele accesibil al listelor și cititorului; șase formulări sunt traduse pentru toate cele opt limbi. Verificarea manuală JAWS/NVDA rămâne necesară.
- Selectarea unui folder dezactivează acum vizualizarea „Citește acum” restaurată anterior, astfel încât lista și numărul de articole să fie limitate la feedurile folderului.
- Regula de retenție pentru „Citește acum” este stabilită: articolele obișnuite respectă perioada configurată, iar favoritele și articolele „Mai târziu” rămân până la ștergerea manuală.
- Alertele sonore pentru actualizarea feedurilor sunt disponibile în Setări aplicație, cu opțiuni separate pentru finalizare reușită, articole noi și erori, plus buton de test și limitare a repetării fără articole noi.
- Fereastra conversației AI are un meniu contextual accesibil, grupat pentru citire vocală, copiere, distribuire și continuarea conversației.
- Bara de stare a conversației AI emite acum evenimente live pentru cititoarele de ecran, inclusiv la schimbarea stării citirii și la maximizare/restaurare.
- Distribuția 1.5.3 pentru Windows x64 și arhiva sursă au fost generate și verificate la cererea expresă a utilizatorului.
- Verificările de compilare, localizare, voce și distribuție pentru versiunea stabilizată au fost finalizate anterior.
- Versiunea 1.5.4 include sincronizarea NewsBlur și îmbunătățiri de accesibilitate; mesajele noi au fost traduse și verificate în toate limbile.
- Installerul local 1.5.4 este offline și include arhiva portabilă verificată; buildul 1.5.4 fără payload încorporat nu va cădea accidental la versiunea publică 1.5.3.

## În lucru acum

- Workflow-ul GitHub Actions compilează separat aplicația și instalatorul; sursele instalatorului nu mai intră accidental în proiectul aplicației principale.
- URL-ul principal GitHub Pages detectează limba preferată a browserului și redirecționează automat către una dintre cele opt pagini; pentru limbile nesuportate folosește engleza, iar paginile accesate explicit rămân stabile.
- Toate paginile publice precizează transparent că Orizont RSS a fost creat de Grigore Frișan în colaborare cu OpenAI Codex și oferă legătură către documentul complet de contribuții.
- Instalatorul autonom `OrizontSetup.exe` pentru Windows x64 include alegerea inițială a limbii (cu limba Windows preselectată), opțiune pentru pictogramă pe desktop și bară de stare live. Release-ul 1.5.4 este public; retestarea acestei versiuni cu JAWS/NVDA rămâne recomandată pentru confirmarea accesibilității installerului actual.
- Verificarea manuală raportată de utilizator pentru bara de stare și facilitățile Orizont este în regulă; testarea manuală a noilor modificări 1.5.4 cu JAWS/NVDA și pe al doilea calculator rămâne de confirmat.
- Experimentul Android `OrizontRSSAndroid` este separat de proiectul Windows și este pus pe pauză.
- Localizarea maghiară (`hu-HU`) și italiană (`it-IT`) este implementată în aplicație, instalator, ghiduri și paginile GitHub Pages; verificarea automată pentru ambele limbi a trecut.
- Exemplele de lucru pentru feeduri sunt implementate: un feed demonstrativ local, disponibil fără internet, și un feed RSS online oficial pentru fiecare dintre cele opt limbi. Feedurile online sunt adăugate numai după verificarea faptului că răspund cu articole; feedul local este exclus din actualizările de rețea.
- Verificarea manuală din 12 septembrie a confirmat anunțarea articolelor din feedul demonstrativ local în JAWS; utilizatorul confirmă și că feedul de test este deja în lista de surse. Testarea fluxului românesc a găsit că vechiul host `rss.hotnews.ro` nu se rezolvă DNS; catalogul local a fost trecut la `https://hotnews.ro/feed`, care răspunde ca RSS. Actualizarea efectivă a feedului, focalizarea pe primul articol și eliminarea cu confirmare rămân de verificat manual.
- Pagina GitHub Pages are acum metadate SEO localizate pentru toate cele opt limbi: descriere, canonical, `hreflang`, Open Graph, Twitter Card și JSON-LD pentru site și aplicație. Au fost adăugate `sitemap.xml` și `robots.txt`, cu URL-uri publice absolute.
- Galeria publică folosește acum patru previzualizări demonstrative în limba engleză, aleasă ca limbă internațională comună; textul alternativ și descrierile rămân localizate pentru fiecare pagină.
- Verificarea publică din 9 septembrie 2026 confirmă că pagina principală și toate cele opt pagini localizate se încarcă, afișează butonul pentru instalator și păstrează navigarea accesibilă; `robots.txt` și `sitemap.xml` sunt prezente și conțin adresele publice corecte.
- Release-ul `v1.5.3` rămâne versiunea istorică anterioară; hash-ul arhivei sale portabile corespunde manifestului WinGet și assetului public de atunci.
- Manifestele WinGet locale trec validarea cu WinGet `1.29.290`. PR-ul oficial `microsoft/winget-pkgs#431971` este deschis cu cele patru fișiere pentru `Grifnas.OrizontRSS` 1.5.3; botul Microsoft solicită semnarea CLA, iar validările tehnice sunt încă în așteptare.
- Feedul Bistrițeanul a fost validat prin XML-ul primit, dar prototipul Android a primit 404. Nu se mai fac încercări speculative până la o decizie nouă și o metodă de diagnostic adecvată.
- Prima etapă NewsBlur este implementată local: fereastră accesibilă pentru autentificare cu utilizator/parolă, creare cont, deconectare și deschiderea site-ului oficial; sesiunea este protejată DPAPI, iar parola nu este salvată.
- OAuth NewsBlur este pregătit doar ca flux explicativ. Google, Facebook și alți furnizori nu sunt activați până la primirea unui client ID și secret aprobat de NewsBlur și confirmarea furnizorilor acceptați.
- Oglindirea structurii NewsBlur este implementată local în `Feeduri → Servicii externe → Sincronizează feedurile și folderele NewsBlur` și rulează automat la pornire când există o sesiune conectată; după activarea sincronizării automate, rulează și la intervalul ales. Feedurile noi, redenumirile și mutările se propagă în ambele sensuri; editarea adresei locale înlocuiește abonamentul remote la următoarea sincronizare. O ștergere locală confirmată se transmite către NewsBlur, iar un feed șters în NewsBlur este eliminat local împreună cu articolele lui, apoi dispare și de pe celelalte dispozitive la următoarea lor sincronizare. Conflictele concurente de nume/folder sunt amânate în fundal și pot fi rezolvate din comanda manuală. Înainte de orice modificare sau ștergere, răspunsurile OPML și feed-index trebuie să fie valide și să descrie aceeași mulțime de adrese; altfel operația se oprește fără schimbări. Ștergerea unui folder local mută feedurile în Neorganizate. Folderele goale nu au echivalent în modelul local.
- Sincronizarea bidirecțională a articolelor și stărilor este implementată local: articolele existente sunt asociate prin `story_hash`, adresă, identificator sau titlu și dată, iar articolele lipsă sunt importate din NewsBlur cu titlu, text disponibil, adresă, dată, starea citit și favorite și etichete. Prima rulare creează baza fără suprascrieri; rulările următoare pot prelua sau trimite citit/necitit, favorite și etichete. Conflictele sunt păstrate neschimbate; „Mai târziu”, notițele AI și ștergerea individuală a articolelor rămân locale.
- Primele două etape ale direcției aprobate sunt implementate local: Saved Stories/stelele NewsBlur pot fi mapate configurabil către Favorite, „Mai târziu” sau ambele, iar etichetele sunt sincronizate împreună cu starea salvată. Alegerea se găsește în Setări aplicație → Sincronizare NewsBlur. Parolele, cheile API, notițele AI și setările vocale rămân locale.
- Oglindirea bidirecțională completă a feedurilor/folderelor este implementată local: prima sincronizare a unui profil nou rămâne import unidirecțional confirmat din NewsBlur; ulterior oglindirea rulează la pornire și, dacă sincronizarea automată este activată, la intervalul configurat. Ștergerile confirmate local se propagă remote fără a cere o a doua confirmare, iar ștergerile remote sunt aplicate local numai după validarea concordanței OPML/feed-index. Conflictele de nume/folder sunt amânate în fundal și rămân rezolvabile manual. Pentru o adresă de feed editată local, abonamentul vechi intră în coada de dezabonare și noua adresă este adăugată. Folderele fără niciun feed nu pot fi oglindite deoarece modelul Orizont le deduce din feeduri. La sincronizarea manuală, dublurile locale sunt prezentate într-un dialog accesibil, cu feedurile și folderele lor; curățarea existentă se deschide numai la alegere explicită și cere confirmare separată. Duplicatele raportate de snapshotul NewsBlur sunt descrise separat, fără ștergere remote. Confirmările și conflictele NewsBlur folosesc acum dialoguri cu focalizare inițială explicită, navigare Tab și opțiune sigură implicită. Verificarea manuală cu JAWS/NVDA și contul NewsBlur rămâne necesară. Mesajele noi folosesc fallback românesc și trebuie traduse înaintea unei distribuții. Ștergerea individuală a articolelor rămâne exclusiv locală, deoarece API-ul NewsBlur nu documentează o operație corespunzătoare.
- Retenția locală este acum aplicată după încercarea de actualizare RSS chiar și când feedurile au erori; articolele obișnuite NewsBlur expirate nu sunt reimportate în Orizont. Articolele marcate salvate se importă conform mapării Favorite/Mai târziu și rămân protejate. Nicio ștergere individuală nu este trimisă către NewsBlur.
- La actualizarea manuală, la actualizarea de pornire și la intervalul automat configurat, Orizont încearcă acum mai întâi să preia articolele din NewsBlur. RSS direct este folosit pentru feedurile absente din NewsBlur sau pentru care cererea de articole eșuează; un răspuns NewsBlur valid fără articole nu declanșează fallback. Fără o sesiune NewsBlur utilizabilă, actualizarea RSS existentă rămâne sursa de rezervă. Feedurile, folderele și dezabonările nu sunt modificate de această rută automată. Verificarea manuală cu JAWS și cu o sesiune NewsBlur reală rămâne necesară.
- Inițializarea NewsBlur este acum unidirecțională pe un profil nou: prima sincronizare importă feedurile și folderele din NewsBlur, păstrează local feedurile neasociate și ignoră feedurile demonstrative; nu trimite feeduri și nu execută dezabonări. Marcajul de inițializare este local pentru profil/cont și nu se transferă prin backup. După importul salvat cu succes, sincronizările următoare sunt bidirecționale. Instalările care au deja o asociere NewsBlur verificată sunt recunoscute și continuă direct bidirecțional; sincronizarea articolelor/stărilor așteaptă inițializarea feedurilor și folderelor.
- Ștergerea unui folder în Orizont mută feedurile la categoria locală „Neorganizate”, mapată la feeduri NewsBlur de nivel superior. Sincronizarea trimite operația de mutare, nu dezabonare și nu folosește endpointul NewsBlur de ștergere a folderului, care dezabonează feedurile din acel folder. Ștergerea explicită a unui feed, inclusiv din „Neorganizate”, rămâne dezabonare separată cu confirmare. Feedurile demonstrative locale și online nu se sincronizează.
- Faza a doua este implementată local: redenumirea și mutarea feedurilor în ambele direcții folosesc o bază locală și păstrează conflictele neschimbate; există vizualizări pentru articole salvate, necitite și toate articolele NewsBlur, comandă pentru marcarea folderului selectat ca citit, iar textul complet poate fi cerut din NewsBlur înaintea fallbackului RSS.
- Sincronizarea automată în fundal se activează din Setări aplicație → Sincronizare NewsBlur și folosește intervalul de 15, 30, 60 sau 180 de minute pentru oglindirea structurii, articolelor și stărilor; oglindirea structurală are loc și la pornire când sesiunea NewsBlur este conectată. Conflictele de nume/folder sunt amânate în fundal. Textele noi trebuie traduse în toate cele opt limbi înaintea unei distribuții; resursele existente au fost păstrate.

### Curățarea duplicatelor și oglindirea NewsBlur — 12 septembrie 2026

- După o curățare reușită, Orizont pornește automat sincronizarea feedurilor și folderelor dacă NewsBlur este conectat. Dacă instrumentul de curățare a fost deschis din dialogul unei sincronizări deja în curs, sincronizarea existentă se reia cu lista curățată, fără o a doua rulare concurentă.
- Feedurile suprapuse eliminate prin alegere explicită intră în coada persistentă de dezabonare NewsBlur, rezolvată după ID sau adresă la sincronizare. Un feed păstrat cu aceeași adresă normalizată nu este dezabonat. Feedurile demonstrative sunt excluse.
- Inițializarea pe un profil nou rămâne unidirecțională: adresele din coada de ștergere sunt omise din importul local, dar nu sunt dezabonate remote în prima etapă; dezabonarea are loc la următoarea sincronizare bidirecțională.
- Verificări: build Release fără avertismente/erori; CoreSmoke 59 verificări; LocalizationSmoke trecut pentru toate cele opt limbi; `git diff --check` trecut cu avertismente informative LF→CRLF. Nu a fost lansată aplicația pe profilul utilizatorului și nu a fost folosită sesiunea NewsBlur reală; verificarea remote și JAWS/NVDA rămân manuale.
- Executabil local de test: `bin/Release/net8.0-windows/Orizont.exe` (necesită .NET 8 Desktop Runtime). Nu s-a creat distribuție și nu s-au schimbat versiunea publică, feedurile utilizatorului sau NewsBlur.

## Următorii pași propuși

1. Menținerea și verificarea proiectului Windows fără a schimba funcții stabile.
2. Rezolvarea numai a problemelor sau îmbunătățirilor cerute explicit.
3. Pentru orice modificare de accesibilitate, păstrarea navigării native cu tastatura și verificarea țintită cu cititoare de ecran.
4. Actualizarea acestui document și a `WORKLOG.md` după fiecare intervenție efectivă.
5. Crearea unei distribuții noi numai la cererea expresă a utilizatorului.

## Idei pentru mai târziu

- îmbunătățiri ale cititorului și ale navigării accesibile;
- extinderea testelor automate pentru colecții mari de articole;
- eventuală reluare a proiectului Android, numai după stabilirea unei strategii tehnice verificabile;
- funcții noi pentru partajare, traducere și conversații AI, dacă sunt solicitate.
- API-ul NewsBlur nu expune ștergerea individuală a articolelor; textele adăugate recent trebuie traduse și verificate în toate limbile înaintea unei distribuții.

Ideile din această secțiune nu autorizează implementarea și nu schimbă funcționalitatea existentă.

## Regula de interpretare

La reluarea proiectului, se continuă de aici. Nu se reiau experimente abandonate și nu se transformă o idee în implementare fără confirmarea utilizatorului.
