# Orizont RSS — listă fixă de regresie manuală

Această listă se execută după orice modificare care poate afecta interfața, focalizarea, tastatura, vocea, localizarea, cititorul articolului sau sincronizarea. Testarea automată nu înlocuiește verificarea cu JAWS și NVDA.

## Reguli de execuție

- Se testează separat cu JAWS 2026 și NVDA, folosind aceeași versiune locală a executabilului.
- Se notează pentru fiecare scenariu: rezultat, cititor de ecran, versiune, temă, limbă și eventualul mesaj/raport de eroare.
- Dacă un scenariu eșuează, nu se creează distribuție și nu se continuă cu funcții noi până la izolarea problemei.
- Pentru operații asupra NewsBlur se folosește numai un profil de test sau o sesiune autorizată explicit; nu se modifică date personale în timpul unei verificări obișnuite.

## A. Pornire, focalizare și închidere

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| A1 | Pornire pe profil cu feeduri | Fereastra apare, focusul ajunge pe primul articol din lista agregată și cititorul anunță zona activă. |
| A2 | Pornire pe profil fără feeduri | Se afișează mesajul vizual și se anunță că nu există feeduri. |
| A3 | `F6` între panouri | Focusul ajunge în panoul următor și instrucțiunea de navigare este anunțată. |
| A4 | `F11` maximizare/restaurare | Dimensiunea se schimbă, iar JAWS/NVDA anunță imediat noua stare. |
| A5 | Închidere normală și `Escape` în ferestre secundare | Dialogul corect se închide, focusul revine la controlul anterior, iar aplicația salvează fără eroare. |

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
| C6 | Copiere și distribuire | Copierea titlului, articolului și adresei produce conținutul corect; trimiterea prin email rămâne disponibilă unde este documentată. |

## D. Setări, limbi și contrast

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| D1 | Setări aplicație/voce/AI | Categoriile sunt separate vizual și semantic; fiecare deschide panoul corect. |
| D2 | Schimbare limbă | Etichetele, meniurile, mesajele goale și ferestrele secundare folosesc aceeași limbă. |
| D3 | Cele cinci teme | Textul, fundalul, meniurile, bordurile și focusul rămân lizibile în fiecare temă. |
| D4 | Salvare și repornire | Limba, tema, vocea și opțiunile permise se păstrează după repornire. |
| D5 | Google Translate/Gemini/DeepL | Setările sunt separate, mesajele sunt localizate, iar rezultatul apare într-o fereastră accesibilă. |

## E. Actualizare, alerte și NewsBlur

| ID | Scenariu | Rezultat așteptat |
|---|---|---|
| E1 | Actualizare manuală | Bara de stare comunică începutul, rezultatul și feedurile cu eroare fără blocarea interfeței. |
| E2 | Alerte sonore și cuvinte-cheie | Sunetul este opțional; dialogul rezultat se focalizează și citește doar titlul și cuvântul-cheie. |
| E3 | Sincronizare la pornire | Bifele din setări sunt respectate, iar operațiile nu pornesc dublat sau după închiderea aplicației. |
| E4 | Profil NewsBlur curat | Feedurile și folderele se importă inițial în direcția NewsBlur → Orizont. |
| E5 | Modificare bidirecțională | Adăugarea, mutarea, redenumirea și ștergerea confirmată se propagă conform regulilor aprobate. |
| E6 | Închidere în timpul sincronizării | Utilizatorul este avertizat, sincronizarea este anulată/încheiată controlat, iar datele se salvează. |

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
