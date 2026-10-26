# Orizont RSS 1.6.5 — Note de versiune (candidat local, nepublicat)

Acest document descrie versiunea 1.6.5 aflată în pregătire. Nu există încă pachete, tag sau release public pentru 1.6.5. Ultima versiune publicată rămâne **v1.6.4**, cu notele din [RELEASE-NOTES-1.6.4.md](RELEASE-NOTES-1.6.4.md).

## Noutăți

- **Podcasturi în Cititorul Orizont:** deasupra textului apare o bară cu butoanele -30 s, -5 s, Redă podcastul, +5 s, +30 s, Viteza de redare, Tonul redării și Descarcă podcastul. Apare numai la articolele cu podcast și face același lucru ca tastele Z, X, C, V, B, S, P, D.
- **Volumul podcastului:** Alt+săgeată sus și Alt+săgeată jos schimbă volumul cu 5%, între 0% și 300%. Volumul este aplicat în aplicație și nu modifică volumul Windows. Implicit este 100%.
- **Podcasturi din afișarea standard:** în meniul Articol, din lista de articole sau din textul articolului, „Redă sau pune pe pauză podcastul” pornește sau pune pe pauză redarea fără să deschizi cititorul.
- **Căutarea podcasturilor** prin catalogul Apple Podcasts, din fereastra de descoperire a subiectelor. Funcția a fost adăugată după v1.6.4.
- **Tema sonoră implicită** este Orizont. Utilizatorii care au deja o temă salvată în Setări o păstrează.
- **YouTube:** linkurile YouTube nu mai afișează ecranul de consimțământ în Cititor. Apare descrierea videoclipului din feed și o notă, iar în meniul cititorului există „Deschide pagina originală în browser”.

## Corecturi

- **Pauza podcastului:** fereastra principală și Cititorul folosesc același player. Dacă porneai podcastul din lista de articole și apăsai C în Cititor, pornea o a doua redare, iar pauza oprea doar una dintre ele.
- **Tastele podcastului** se tratează o singură dată. Înainte, C putea porni și opri redarea în aceeași apăsare, iar Alt+săgeată schimba volumul cu 10%.
- **Tasta Application în modul text** deschide meniul Cititorului, ca Shift+F10.
- **Meniul Cititorului** conține „Deschide pagina originală în browser”, atât în modul text, cât și în WebReader.

## Limite cunoscute

- Redarea podcasturilor, volumul și pauza nu au fost verificate pe ecran cu JAWS 2026 și nici cu sunet real. Verificarea practică este încă necesară înainte de publicare.
- Volumul peste 100% poate distorsiona sunetul.
- Podcasturile de pe YouTube nu se redau în aplicație. Videoclipul se deschide în browser.
- În WebReader, mesajele de stare nu sunt accesibile cititorului de ecran, cum era și în versiunea 1.6.4.

## Verificări și limite de acceptare

Verificările automate pentru sursă, fluxuri de lucru, localizare, ghiduri, codare și consistența documentației se rulează înainte de publicare. Ele nu înlocuiesc acceptarea practică cu cititorul de ecran.

Probele practice A02 (NewsBlur pe două calculatoare), A09 (scenariile Google Translate), A07 (revizia lingvistică umană) și A11 (verificarea completă a interfeței cu JAWS 2026) rămân deschise și nu sunt declarate trecute.
