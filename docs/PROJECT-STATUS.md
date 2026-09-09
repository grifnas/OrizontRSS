# Orizont RSS — stare și plan de proiect

Ultima actualizare: 9 septembrie 2026

## Scopul documentului

Acest document păstrează separat ceea ce este deja realizat, ceea ce este în lucru și ceea ce este doar planificat. O idee planificată nu este considerată o cerere de implementare până când utilizatorul nu o confirmă explicit.

## Realizat

- Orizont RSS pentru Windows este la versiunea stabilizată 1.5.3.
- Sunt implementate feedurile RSS, folderele, actualizarea, filtrele, favoritele, lista „Mai târziu”, etichetele și curățarea duplicatelor.
- Sunt implementate cititorul integrat accesibil, citirea vocală, căutarea, copierea, partajarea, backupul și restaurarea.
- Sunt integrate funcțiile Gemini și traducerea DeepL, cu activare explicită de către utilizator.
- Interfața este disponibilă în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană.
- Regula de retenție pentru „Citește acum” este stabilită: articolele obișnuite respectă perioada configurată, iar favoritele și articolele „Mai târziu” rămân până la ștergerea manuală.
- Alertele sonore pentru actualizarea feedurilor sunt disponibile în Setări aplicație, cu opțiuni separate pentru finalizare reușită, articole noi și erori, plus buton de test și limitare a repetării fără articole noi.
- Fereastra conversației AI are un meniu contextual accesibil, grupat pentru citire vocală, copiere, distribuire și continuarea conversației.
- Bara de stare a conversației AI emite acum evenimente live pentru cititoarele de ecran, inclusiv la schimbarea stării citirii și la maximizare/restaurare.
- Distribuția 1.5.3 pentru Windows x64 și arhiva sursă au fost generate și verificate la cererea expresă a utilizatorului.
- Verificările de compilare, localizare, voce și distribuție pentru versiunea stabilizată au fost finalizate anterior.

## În lucru acum

- Publicarea publică este activă: sursa, tagul `v1.5.3`, Release-ul stabil, arhiva Windows și instalatorul `OrizontSetup.exe` sunt publicate în `grifnas/OrizontRSS`; pagina GitHub Pages multilingvă funcționează la `https://grifnas.github.io/OrizontRSS/` și oferă acum buton direct pentru instalator, cu arhiva portabilă ca alternativă.
- Workflow-ul GitHub Actions compilează separat aplicația și instalatorul; sursele instalatorului nu mai intră accidental în proiectul aplicației principale.
- URL-ul principal GitHub Pages detectează limba preferată a browserului și redirecționează automat către una dintre cele opt pagini; pentru limbile nesuportate folosește engleza, iar paginile accesate explicit rămân stabile.
- Toate paginile publice precizează transparent că Orizont RSS a fost creat de Grigore Frișan în colaborare cu OpenAI Codex și oferă legătură către documentul complet de contribuții.
- Instalatorul autonom `OrizontSetup.exe` pentru Windows x64 include acum alegerea inițială a limbii (cu limba Windows preselectată), opțiune pentru pictogramă pe desktop și bară de stare live; instalarea și dezinstalarea completă au fost testate manual, iar retestarea acestor funcții noi cu JAWS/NVDA rămâne necesară înainte de publicarea lui.
- Verificarea manuală raportată de utilizator pentru bara de stare și facilitățile Orizont este în regulă; etapa de consolidare și distribuția 1.5.3 sunt închise.
- Experimentul Android `OrizontRSSAndroid` este separat de proiectul Windows și este pus pe pauză.
- Localizarea maghiară (`hu-HU`) și italiană (`it-IT`) este implementată în aplicație, instalator, ghiduri și paginile GitHub Pages; verificarea automată pentru ambele limbi a trecut.
- Exemplele de lucru pentru feeduri sunt implementate: un feed demonstrativ local, disponibil fără internet, și un feed RSS online oficial pentru fiecare dintre cele opt limbi. Feedurile online sunt adăugate numai după verificarea faptului că răspund cu articole; feedul local este exclus din actualizările de rețea.
- Pagina GitHub Pages are acum metadate SEO localizate pentru toate cele opt limbi: descriere, canonical, `hreflang`, Open Graph, Twitter Card și JSON-LD pentru site și aplicație. Au fost adăugate `sitemap.xml` și `robots.txt`, cu URL-uri publice absolute.
- Galeria publică folosește acum patru previzualizări demonstrative în limba engleză, aleasă ca limbă internațională comună; textul alternativ și descrierile rămân localizate pentru fiecare pagină.
- Verificarea publică din 9 septembrie 2026 confirmă că pagina principală și toate cele opt pagini localizate se încarcă, afișează butonul pentru instalator și păstrează navigarea accesibilă; `robots.txt` și `sitemap.xml` sunt prezente și conțin adresele publice corecte.
- Release-ul public `v1.5.3` este stabil, cu arhiva portabilă și `OrizontSetup.exe` încărcate; suma SHA-256 a arhivei locale corespunde manifestului WinGet și assetului public.
- Manifestele WinGet locale trec validarea cu WinGet `1.29.290`. PR-ul oficial `microsoft/winget-pkgs#431971` este deschis cu cele patru fișiere pentru `Grifnas.OrizontRSS` 1.5.3; botul Microsoft solicită semnarea CLA, iar validările tehnice sunt încă în așteptare.
- Feedul Bistrițeanul a fost validat prin XML-ul primit, dar prototipul Android a primit 404. Nu se mai fac încercări speculative până la o decizie nouă și o metodă de diagnostic adecvată.
- Prima etapă NewsBlur este implementată local: fereastră accesibilă pentru autentificare cu utilizator/parolă, creare cont, deconectare și deschiderea site-ului oficial; sesiunea este protejată DPAPI, iar parola nu este salvată.
- OAuth NewsBlur este pregătit doar ca flux explicativ. Google, Facebook și alți furnizori nu sunt activați până la primirea unui client ID și secret aprobat de NewsBlur și confirmarea furnizorilor acceptați.
- Sincronizarea inițială NewsBlur este implementată local: abonamentele și folderele sunt preluate prin exportul OPML oficial, apoi utilizatorul primește un rezumat și confirmă adăugarea sau actualizarea. Feedurile locale care lipsesc din NewsBlur sunt păstrate, iar articolele și stările locale nu sunt modificate.
- Prima etapă de sincronizare bidirecțională a stărilor este implementată local: articolele existente sunt asociate prin `story_hash`, adresă, identificator sau titlu și dată; la prima rulare se creează baza locală, iar rulările următoare pot prelua sau trimite citit/necitit, favorite și etichete. Conflictele sunt păstrate neschimbate, iar „Mai târziu” și ștergerile nu sunt sincronizate.

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
- integrare viitoare, opțională, cu NewsBlur pentru stări de articole; importul inițial al abonamentelor și folderelor este deja implementat local, iar sincronizarea stărilor rămâne neimplementată.
- importul articolelor care nu există local, sincronizarea automată în fundal și sincronizarea ștergerilor/feedurilor; acestea rămân neimplementate.

Ideile din această secțiune nu autorizează implementarea și nu schimbă funcționalitatea existentă.

## Regula de interpretare

La reluarea proiectului, se continuă de aici. Nu se reiau experimente abandonate și nu se transformă o idee în implementare fără confirmarea utilizatorului.
