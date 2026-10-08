# Orizont RSS — listă fixă de regresie manuală

Această listă se execută după orice modificare care poate afecta interfața, focalizarea, tastatura, vocea, localizarea, cititorul articolului sau sincronizarea. Testarea automată nu înlocuiește verificarea practică. Utilizatorul folosește în general JAWS 2026 și foarte rar NVDA; pentru confirmările lui, JAWS 2026 este cititorul implicit dacă nu spune altceva.

## Reguli de execuție

- Pentru verificarea practică obișnuită se folosește JAWS 2026 și aceeași versiune locală a executabilului. NVDA se testează separat când este cerută verificarea lui ori apare o problemă specifică acestuia; nu este obligatoriu la fiecare regresie.
- Se notează pentru fiecare scenariu: rezultat, versiunea aplicației, cititorul dacă diferă de JAWS 2026 implicit, temă, limbă și eventualul mesaj/raport de eroare. Nu se cere din nou utilizatorului această precizare în mod obișnuit.
- Dacă un scenariu eșuează, nu se creează distribuție și nu se continuă cu funcții noi până la izolarea problemei.
- Pentru operații asupra NewsBlur se folosește numai un profil de test sau o sesiune autorizată explicit; nu se modifică date personale în timpul unei verificări obișnuite.

## A. Pornire, focalizare și închidere

### Consolidare — A01 și A03 confirmate; A02 în verificare

- Deschide un articol în Cititor Orizont, revino la fereastra principală și selectează alt articol. Reîncărcarea din cititor trebuie să păstreze articolul inițial; meniul contextual, Ctrl+Shift+F8 și marcarea citit trebuie să funcționeze ca înainte.
- Dacă descărcarea/traducerea durează, selectează între timp alt articol. Răspunsul vechi nu trebuie să înlocuiască noul conținut, să deschidă o fereastră pentru articolul greșit sau să mute focusul înapoi. Verifică și o descărcare/traducere obișnuită, fără schimbarea selecției.
- Pentru controlul pornirii, schimbă câte o singură bifă și notează valorile inițiale, cu excepția unui scenariu explicit de oprire completă. La 28 septembrie 2026, utilizatorul a autorizat testarea pe contul său NewsBlur curent și a extins acordul la testele obișnuite ale aplicației, fără reconfirmare la fiecare pas; intervalul periodic normal este de 30 de minute. Utilizatorul a confirmat că toate opțiunile NewsBlur oprite suprimă oglindirea. Cu numai sincronizarea feedurilor/folderelor activă, aplicația a anunțat „56 reușite, 2 cu eroare”; cauza erorilor rămâne de identificat. Rezumatul complet al stărilor este anunțat după fallback-ul RSS și a fost confirmat vocal de utilizator. La 1 octombrie, o eroare de direcție citit/necitit a fost corectată; pe un articol real, sincronizarea manuală local→NewsBlur a trimis citit și apoi necitit, ambele confirmate prin API. Timerul temporar de 15 minute a rulat automat: 2 feeduri trimise/0 erori, 58 verificate, 7 fallback RSS reușite/0 erori, 15 articole importate și 16 noi RSS. O probă live separată a schimbat temporar în NewsBlur un articol necitit în citit; sincronizarea la pornire a persistat local citit și reperele NewsBlur, apoi starea remote și fișierele locale au fost restaurate exact. Nu a fost o probă JAWS. A02 rămâne deschis pentru identificarea celor două erori structurale și testul între două calculatoare.
- Pentru matricea rămasă, cu ambele sincronizări NewsBlur la pornire și actualizarea RSS debifate nu se pornesc acele operații. Cu doar bifa feeduri/foldere activă se sincronizează structura; cu doar articole/stări activă se actualizează articolele fără oglindire structurală. Actualizarea RSS de la pornire este separată când bifa NewsBlur pentru articole este oprită. Verificările live pe contul curent sunt excepția autorizată explicit de utilizator; păstrează temporar starea aleasă, nu șterge remote, notează efectele locale ale regulii de păstrare și restaurează setările/UI-ul schimbate pentru test.
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

Confirmare completată la 1 octombrie 2026 pentru copia locală autonomă: cu JAWS 2026 pornit, a doua lansare a aceleiași căi a afișat dialogul în engleză și butonul OK, anunțate de cititor. După închiderea dialogului a rămas un singur proces Orizont, iar utilizatorul a confirmat că poate naviga normal prin articole în prima fereastră. După închiderea normală a primei instanțe, procesul s-a terminat și aplicația a pornit din nou cu listele încărcate. Nu s-a testat NVDA și nu s-a comparat fiecare fișier de profil.

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
| C9 | Bara de stare a rezultatului traducerii | Deschide rezultatul Google Translate și DeepL: bara vizibilă anunță finalizarea, furnizorul și limba rezultatului; focusul inițial rămâne în textul tradus pentru citire cu săgețile. Cu JAWS 2026, confirmă că mesajul este rostit la deschidere și că bara nu adaugă un opritor inutil în ordinea Tab. Testele automate verifică structura, mesajul localizat și live-region; ele nu înlocuiesc confirmarea vocală practică. |
| C10 | Meniu contextual în textul tradus și curățat | **Confirmat cu JAWS 2026, 6 octombrie:** în Cititor Orizont, modul Text, Application deschide acum meniul; Shift+F10 fusese confirmată funcțională. Mai rămâne verificarea WebReader și, separat, a rezultatelor Google Translate și DeepL: ambele taste, focalizarea meniului, comenzile disponibile și revenirea cu Escape fără pierderea traducerii. Smoke-urile verifică structura/rutarea, nu focusul JAWS. |

**Confirmare practică parțială, 2 octombrie 2026:** utilizatorul a confirmat că JAWS 2026 rostește anunțul așteptat în rezultatul Google Translate. Aceasta nu confirmă separat focusul inițial, ordinea Tab sau comportamentul rezultatului DeepL.

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
| E6 | Închidere în timpul sincronizării | Cu JAWS 2026, pornește o actualizare/sincronizare și apasă Alt+F4. Dialogul trebuie focalizat și să explice efectele; „Rămâi în aplicație și continuă” este opțiunea implicită, iar Escape trebuie să lase aplicația și sincronizarea active. Repetă și alege explicit „Oprește sincronizarea și închide” prin Tab/Enter; aplicația așteaptă salvarea locală, apoi se închide. Pentru NewsBlur, reține că acțiunile deja confirmate pe server nu pot fi retrase și se vor reconcilia la următoarea sincronizare. Utilizatorul a confirmat la 3 octombrie 2026 că scenariul testat funcționează conform așteptărilor. |
| E7 | Verificare actualizări | Cu JAWS/NVDA, folosește Ajutor → Verifică actualizări. Un rezultat HTTP valid fără versiune nouă trebuie anunțat ca „la zi”; eroarea de rețea trebuie anunțată ca eșec, nu „la zi”. Confirmă că focusul rămâne utilizabil. Testele automate simulează assetul, SHA-256, descărcarea incompletă și salvarea eșuată. Nu încerca instalarea reală în acest scenariu; o probă de instalare cere aprobare separată și mediu izolat. |
| E8 | Oprirea la nume/URL incompatibile (A02) | Numai într-un profil/cont de test cu pereche unică local-only/NewsBlur-only: declanșează oglindirea manuală. JAWS trebuie să anunțe dialogul cu numele și ambele adrese; nu trebuie să se schimbe niciun feed sau folder. Nu crea intenționat o pereche greșită în contul personal. Revenirea după aliasul răspuns de server rămâne verificată automat și nu se provoacă prin ștergeri în contul personal. |
| E9 | Partajare și proveniență (A08) | Scenariu de regresie: dacă apar modificări în traducere/partajare, cu JAWS 2026 testează un articol tradus și unul lung din fereastra principală și Cititor Orizont. Verifică titlu, corp, linkul Orizont RSS, sursă, furnizor/limbă și footerul complet după trunchiere; notează focalizarea și mesajul de stare. Nu este necesar să trimiți efectiv mesajul. **Acceptare existentă:** utilizatorul a confirmat la 1 octombrie 2026 că testele de traduceri și texte lungi au fost verificate repetat de-a lungul timpului; nu se cere repetarea pentru închiderea curentă A08. |

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
