# Orizont RSS — listă fixă de regresie manuală

Această listă se execută după orice modificare care poate afecta interfața, focalizarea, tastatura, vocea, localizarea, cititorul articolului sau sincronizarea. Testarea automată nu înlocuiește verificarea practică. Utilizatorul folosește în general JAWS 2026 și foarte rar NVDA; pentru confirmările lui, JAWS 2026 este cititorul implicit dacă nu spune altceva.

## Reguli de execuție

- Pentru verificarea practică obișnuită se folosește JAWS 2026 și aceeași versiune locală a executabilului. NVDA se testează separat când este cerută verificarea lui ori apare o problemă specifică acestuia; nu este obligatoriu la fiecare regresie.
- Se notează pentru fiecare scenariu: rezultat, versiunea aplicației, cititorul dacă diferă de JAWS 2026 implicit, temă, limbă și eventualul mesaj/raport de eroare. Nu se cere din nou utilizatorului această precizare în mod obișnuit.
- Dacă un scenariu eșuează, nu se creează distribuție și nu se continuă cu funcții noi până la izolarea problemei.
- Pentru operații asupra NewsBlur se folosește numai un profil de test sau o sesiune autorizată explicit; nu se modifică date personale în timpul unei verificări obișnuite.

## A. Pornire, focalizare și închidere

### Consolidare 28 septembrie 2026 — A01 confirmat; A02–A03 în verificare

- Deschide un articol în Cititor Orizont, revino la fereastra principală și selectează alt articol. Reîncărcarea din cititor trebuie să păstreze articolul inițial; meniul contextual, Ctrl+Shift+F8 și marcarea citit trebuie să funcționeze ca înainte.
- Dacă descărcarea/traducerea durează, selectează între timp alt articol. Răspunsul vechi nu trebuie să înlocuiască noul conținut, să deschidă o fereastră pentru articolul greșit sau să mute focusul înapoi. Verifică și o descărcare/traducere obișnuită, fără schimbarea selecției.
- Pentru controlul pornirii, schimbă câte o singură bifă și notează valorile inițiale, cu excepția unui scenariu explicit de oprire completă. La 28 septembrie 2026, utilizatorul a autorizat testarea pe contul său NewsBlur curent și a extins acordul la testele obișnuite ale aplicației, fără reconfirmare la fiecare pas; intervalul periodic normal este de 30 de minute. Utilizatorul a raportat întâi anunțul „Oglindire NewsBlur x din y”, apoi a debifat toate opțiunile NewsBlur și a confirmat după repornire că feedurile nu se mai oglindesc. Cu numai sincronizarea feedurilor/folderelor activă, aplicația a anunțat „Oglindire NewsBlur, 56 reușite, 2 cu eroare”; bifa structurală pornește oglindirea, iar motivele celor două erori rămân necunoscute doar din acest contor. Cu numai sincronizarea articolelor/stărilor activă, utilizatorul a auzit că se sincronizează articolele, dar nu a primit informații despre stări. S-a constatat că rezumatul NewsBlur era apoi suprascris de mesajul actualizării RSS; acum rezumatul complet, cu stările preluate și trimise, este anunțat după fallback-ul RSS, iar utilizatorul a confirmat că noul anunț funcționează. Transferul efectiv al unei stări rămâne neverificat.
- Pentru matricea rămasă, cu ambele sincronizări NewsBlur la pornire și actualizarea RSS debifate nu se pornesc acele operații. Cu doar bifa feeduri/foldere activă se sincronizează structura; cu doar articole/stări activă se actualizează articolele fără oglindire structurală. Actualizarea RSS rămâne separată când bifa NewsBlur pentru articole este oprită. În mod normal, scenariile care pot modifica contul personal se rulează doar pe profil de test; abaterea din 28 septembrie 2026 este limitată la acordul explicit al utilizatorului pentru această sesiune.
- Notează explicit JAWS/NVDA folosit și scenariile efectiv probate; succesul verificărilor sintetice `WorkflowSmoke` (148/148 la 1 octombrie 2026) nu constituie această confirmare manuală.

Confirmare utilizator, 28 septembrie 2026: după scenariul recomandat cu JAWS, utilizatorul a confirmat că articolul nou nu preia textul/sursa/starea celui anterior și că reîncărcarea în Cititor Orizont rămâne asociată articolului deschis inițial. A01 este confirmat practic; scenariile rămân utile la regresii.

### Ghidul de scurtături (confirmare utilizator la 24 septembrie 2026)

Utilizatorul a confirmat că funcționează după solicitarea testului F1, căutare și navigare. Scenariile de mai jos se păstrează pentru regresii; confirmarea nu certifică individual fiecare scenariu, toate limbile sau NVDA.

- Din lista articolelor, F1 deschide ghidul cu focus pe „Caută scurtături”.
- Caută `voce`, `citire vocala` și `ctrl + shift + f8`; Tab duce la primul rezultat și săgețile citesc comenzile fără repetarea instrucțiunilor generale.
- Ctrl+F revine la căutare și selectează textul. Cu un termen inexistent, se anunță și se afișează lipsa rezultatelor. Ștergerea căutării readuce lista completă.
- Escape închide ghidul la o singură apăsare; verifică revenirea focusului la fereastra inițială. Repetă deschiderea prin meniul Ajutor și în engleză. Verifică și butonul către ghidul HTML.

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| A1 | Pornire pe profil cu feeduri | Fereastra apare, focusul ajunge pe primul articol din lista agregată și cititorul anunță zona activă. |
| A2 | Pornire pe profil fără feeduri | Se afișează mesajul vizual și se anunță că nu există feeduri. |
| A3 | `F6` între panouri | Focusul ajunge în panoul următor și instrucțiunea de navigare este anunțată. |
| A4 | `F11` maximizare/restaurare | Dimensiunea se schimbă, iar JAWS/NVDA anunță imediat noua stare. |
| A5 | Închidere normală și `Escape` în ferestre secundare | Dialogul corect se închide, focusul revine la controlul anterior, iar aplicația salvează fără eroare. |
| A6 | A doua pornire a aplicației | Cu prima instanță deschisă, lansează din nou același executabil. JAWS/NVDA anunță dialogul localizat care spune că aplicația rulează deja și indică `Alt+Tab`; Enter îl închide, prima fereastră rămâne funcțională, iar a doua instanță nu modifică feedurile sau setările. Repetă după închiderea primei instanțe pentru a confirma că aplicația pornește normal. |

Confirmare parțială a utilizatorului, 27 septembrie 2026: mesajul este anunțat în engleză pe Windows în engleză, dialogul are un singur buton `OK` (intenționat), iar după închidere prima fereastră a rămas afișată cu articolele folderului selectat. Mai trebuie confirmată interacțiunea efectivă cu prima fereastră după `OK`; cititorul de ecran folosit nu a fost precizat.

## B. Feeduri și articole

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| B1 | Selectare feed și folder | Bara de stare anunță numărul corect pentru selecția curentă, nu totalul global. |
| B2 | Lista cu peste zece articole | Sunt accesibile toate articolele, inclusiv prin `Sus/Jos`, fără limitare accidentală la primele zece. |
| B3 | Deschiderea cititorului Orizont | Titlul și conținutul lizibil sunt disponibile; săgețile navighează textul ca într-un editor. |
| B4 | Articol fără conținut | Panoul și bara de stare comunică lipsa conținutului; mesajul dispare când apare conținut. |
| B5 | Căutare locală cu unul și mai multe cuvinte | Sunt găsite potrivirile din titlu și text, fără a depinde de limba interfeței. |
| B6 | `Ctrl+F`, `F3` și `Escape` | Căutarea se deschide, `F3` repetă căutarea, iar `Escape` închide dialogul și curăță termenul. |
| B7 | Selectare multiplă și meniu `Shift+F10` | Opțiunile sunt logice pentru zona curentă și acționează asupra tuturor elementelor selectate. |
| B8 | Favorite, Mai târziu, citit/necitit și etichete | Starea este anunțată înaintea titlului și rămâne după închiderea/redeschiderea aplicației. |
| B9 | Ștergere cu `Delete` și confirmare | Sunt șterse numai elementele selectate, cu confirmare accesibilă și revenire corectă a focusului. |

## C. Meniuri contextuale și cititor

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| C1 | Meniu pe listă de articole | Include doar acțiuni relevante pentru articole și starea curentă. |
| C2 | Meniu în Favorite/Mai târziu | Nu oferă din nou „Adaugă în...” pentru colecția deja activă. |
| C3 | Meniu pe feed/folder | Include editare, ștergere, mutare și actualizare, cu confirmări unde este cazul. |
| C4 | Meniu în conținutul articolului | Include copiere, distribuire, traducere, AI și deschiderea în browser/cititor, fără opțiuni de listă irelevante. |
| C5 | Citire vocală SAPI5/eSpeak | Comutarea, pauza și oprirea funcționează; `Escape` oprește citirea fără a pierde focusul. |
| C6 | Copiere și distribuire | E-mailul, WhatsApp, copierea documentului și exportul păstrează atribuirea Orizont RSS și sursa; traducerile indică furnizorul și limba. **Confirmat de utilizator cu JAWS, 27 septembrie 2026:** copierea traducerii din panoul principal include Orizont RSS, furnizorul/limba și sursa; revenirea la original elimină atribuirea traducerii; butonul din fereastra rezultatului Google copiază articolul cu footer. **Confirmare practică, 28 septembrie:** copierea articolului complet din Cititor Orizont include linkul aplicației și sursa. Cititor Orizont oferă acum „Salvează articolul” → TXT/RTF atât în meniul `Shift+F10`, cât și în meniul de sus „Cititor”, în modurile Text și WebReader. Utilizatorul a confirmat că subsolul cu linkuri apare, că ambele salvări TXT și RTF se comportă foarte bine și că bara de stare anunță informațiile necesare despre articol și alte stări relevante. Pentru testele practice ale utilizatorului, cititorul implicit este JAWS 2026; nu s-a indicat NVDA pentru aceste verificări. `WorkflowSmoke` 136/136, `CoreSmoke` 127/127 și `LocalizationSmoke` 1.390/1.265 verifică sintetic rutele, exportul, proveniența și catalogul; build Release fără erori. Rămân de verificat manual cu cititorul de ecran găsirea comenzilor din meniu, răspunsul AI, e-mail/WhatsApp și footerul după scurtarea textelor lungi. |
| C7 | Vocea răspunsului AI — panoul principal și Cititor Orizont | **Confirmat de utilizator cu JAWS, 27 septembrie 2026:** funcționează pe ambele trasee; F9 pornește/pune pe pauză/continuă, Escape oprește. Repetă scenariul la regresii; confirmarea nu se extinde la NVDA. |
| C8 | Cereri Google Translate simultane și anulare | Cu JAWS 2026, testează atât din fereastra principală, cât și din Cititor Orizont: refuză confirmarea și pornește din nou; pentru o cerere acceptată, încearcă o a doua activare cât timp se traduce. Bara de stare trebuie să anunțe starea, iar o singură cerere să producă un singur rezultat. Închide Cititorul sau aplicația în timpul unei traduceri și verifică să nu apară rezultat ori eroare după închidere. Smoke-urile simulează duplicate/refuz/eroare/anulare; această probă JAWS folosește efectiv serviciul online și cere confirmarea normală înainte de trimiterea articolului. |

## D. Setări, limbi și contrast

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| D1 | Setări aplicație/voce/AI | Categoriile sunt separate vizual și semantic; fiecare deschide panoul corect. |
| D2 | Schimbare limbă | Etichetele, meniurile, mesajele goale și ferestrele secundare folosesc aceeași limbă. |
| D3 | Cele cinci teme | Textul, fundalul, meniurile, bordurile și focusul rămân lizibile în fiecare temă. |
| D4 | Salvare și repornire | Limba, tema, vocea și opțiunile permise se păstrează după repornire. |
| D5 | Google Translate/Gemini/OpenAI/Mistral/DeepSeek/DeepL | Setările sunt separate, mesajele sunt localizate, iar rezultatul apare într-o fereastră accesibilă. Testele de conectare ale furnizorilor nu trebuie să trimită articolul. |
| D6 | Furnizor AI implicit | JAWS/NVDA anunță selectorul, bifa, cheia, modelul și feedbackul testului; alegerea implicită se păstrează după repornire. Verifică fiecare furnizor configurat în lista principală și Cititor Orizont, continuarea conversației și schimbarea implicitului. |
| D7 | Backup AI | Cheile API nu apar în copia de siguranță; după restaurare, cheile și alegerea furnizorului de pe calculatorul curent rămân păstrate. |
| D8 | Localizare dinamică A07 | Cu interfața engleză și JAWS 2026, salvează un articol TXT și RTF și verifică data publicării, numele feedului, atribuirea și sursa; verifică feedbackul de salvare și un mesaj de eroare accesibil. Nu trebuie să apară etichete generate în română. Testele automate acoperă toate cele opt limbi; acest control practic nu dovedește calitatea lingvistică pentru celelalte șapte. |

## E. Actualizare, alerte și NewsBlur

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| E1 | Actualizare manuală | Bara de stare comunică începutul, rezultatul și feedurile cu eroare fără blocarea interfeței. |
| E2 | Alerte sonore | Sunetul este opțional; actualizarea comunică succesul, articolele noi și erorile conform bifelor din Setări. |
| E3 | Sincronizare la pornire | Bifele din setări sunt respectate, iar operațiile nu pornesc dublat sau după închiderea aplicației. |
| E4 | Profil NewsBlur curat | Feedurile și folderele se importă inițial în direcția NewsBlur → Orizont. |
| E5 | Modificare bidirecțională | Adăugarea, mutarea, redenumirea și ștergerea confirmată se propagă conform regulilor aprobate. |
| E6 | Închidere în timpul sincronizării | Utilizatorul este avertizat, sincronizarea este anulată/încheiată controlat, iar datele se salvează. |
| E7 | Verificare actualizări | Cu JAWS/NVDA, folosește Ajutor → Verifică actualizări. Un rezultat HTTP valid fără versiune nouă trebuie anunțat ca „la zi”; eroarea de rețea trebuie anunțată ca eșec, nu „la zi”. Confirmă că focusul rămâne utilizabil. Testele automate simulează assetul, SHA-256, descărcarea incompletă și salvarea eșuată. Nu încerca instalarea reală în acest scenariu; o probă de instalare cere aprobare separată și mediu izolat. |

## F. Performanță și texte integrale mari (A11)

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| F1 | Căutare într-un lot de articole cu text integral lung | Cu un profil de test, căutarea rămâne utilizabilă, focusul nu sare, iar lista se actualizează corect. `ScaleSmoke` măsoară datele și layout-ul off-screen al listei WPF, dar nu dovedește latența vizibilă sau anunțurile cu JAWS. |
| F2 | Deschidere și navigare într-un articol lung | Săgețile răspund fără blocări vizibile, iar JAWS 2026 citește conținutul în ordine. Compară cu un articol scurt și notează versiunea/limba. |
| F3 | Salvare/export a textului lung | TXT și RTF păstrează articolul complet, atribuirea și sursa; focusul revine normal după salvare. Smoke-ul măsoară separat serializarea/scrierea sintetică și exporturile, nu performanța discului pe alte calculatoare. |

## Formular scurt de consemnare

```text
Data:
Versiune/build:
Cititor de ecran: JAWS / NVDA
Limbă:
Temă:
Scenarii executate:
Rezultate eșuate:
Mesaje sau pași de reproducere:
Decizie: acceptat / reparare necesară / retestare necesară
```
