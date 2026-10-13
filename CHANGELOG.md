# Istoricul modificărilor curente

Acest fișier descrie numai ultima versiune publicată și lucrările ulterioare încă nepublicate. Istoricul mai vechi poate fi recuperat din istoricul Git și din release-urile publice anterioare; nu este copiat în această pagină activă.

## Versiunea publicată v1.6.1 — 8 octombrie 2026

Release-ul public v1.6.1 este disponibil pe [GitHub Releases](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.1). Notele complete, inclusiv limitele verificărilor, sunt în [RELEASE-NOTES-1.6.1.md](RELEASE-NOTES-1.6.1.md).

Versiunea aduce furnizori AI configurabili (Gemini, OpenAI, Mistral și DeepSeek), scurtături configurabile, îmbunătățiri ale Cititorului Orizont și meniurilor contextuale, traducere și partajare cu proveniență, export TXT/RTF, autoetichetare și îmbunătățiri NewsBlur. Alertele după cuvinte-cheie nu sunt incluse în 1.6.1.

Probele practice A02, A09, A07 și A11 rămân deschise și transferate ciclului următor; auditul online NuGet (`NU1900`) nu a putut fi efectuat. Aceste limite sunt prezentate în notele release-ului și nu sunt declarate trecute.

## Modificări nepublicate după tagul v1.6.1

După crearea tagului și a pachetelor release-ului, ramura `main` primește actualizări de documentație și de publicare, inclusiv pagina GitHub Pages și urmărirea manifestelor WinGet. Aceste schimbări de întreținere nu modifică pachetele atașate release-ului și nu constituie o nouă versiune a aplicației.

### Pregătire pentru v1.6.2 (nepublicat)

- Actualizarea prin RSS nu mai șterge articolele care au ieșit din fluxul site-ului. Favoritele, articolele „Mai târziu”, etichetele și notițele AI rămân; ștergerea după vechime se face numai prin regula de păstrare, care protejează în continuare favoritele și „Mai târziu”.
- Sincronizarea NewsBlur nu mai raportează conflict când aceeași schimbare (citit/necitit, favorit sau etichete) a fost făcută pe ambele părți. Bazele de comparație se actualizează, așa că o schimbare ulterioară de pe alt dispozitiv, de exemplu marcarea ca necitit pe telefon, este preluată, nu anulată.
- Curățarea textului articolelor (Cititor Orizont și sursa pentru traducere) nu mai șterge cuvinte obișnuite precum „publicidad”, „commento” sau „Kommentar” din fraze și nu mai taie articolul la expresii ca „Articole înrudite” sau „o campanie editorială” aflate în text. Etichetele de reclamă și comentarii se elimină când stau singure pe linie sau, pentru marcajele de reclamă, când sunt scrise cu majuscule. Secțiunile de final (articole înrudite, comentariile cititorilor) se taie de la titlul lor.
- În limbile străine ale interfeței, poziția articolului se citește tradus (de exemplu „3 of 50”), iar folderul implicit apare tradus („Unorganized”, „No organizado” etc.); intern rămâne „Neorganizate”, deci datele și sincronizarea NewsBlur nu se schimbă. „Toate folderele” nu mai apare trunchiat („A” sau „T”) în engleză, spaniolă, franceză, germană și portugheză.
- Vocea eSpeak NG funcționează și când aplicația este instalată într-un folder cu diacritice, de exemplu sub un utilizator Windows „Frișan” sau „Ionuț”. Înainte, eSpeak nu pornea dintr-un astfel de folder, iar cu „ș”/„ț” putea închide brusc aplicația.
- Vocea eSpeak NG este mai puternică: setarea „Volum” acoperă acum întreaga gamă a motorului, iar la 100 vocea are o sonoritate medie de circa 1,9 ori mai mare decât înainte. Volumul se poate reduce din aceeași setare. SAPI5 și Gemini nu sunt afectate.
- Gemini: limită de răspuns mai mare (8.192 în loc de 2.048), ca modelele care „gândesc” să nu mai întoarcă răspunsuri goale la articole lungi. Mesaje clare după motivul opririi: răspuns tăiat la limită (textul rămâne, cu o notă), blocare de siguranță cu motivul, text protejat. Lista modelelor se cere o singură dată pe sesiune, iar cheia API este ascunsă din detaliile erorilor.
- În fereastra principală, o cerere AI în curs se poate opri cu Escape; mesajul de așteptare anunță această posibilitate.
- Textul integral al articolelor (comanda de aducere a textului complet, reîncărcarea din Cititor Orizont și textul pentru traducere) este extras cu SmartReader 0.9.6, adaptarea .NET a algoritmului Readability. Rezultatul conține articolul fără meniuri, liste „cele mai citite” sau butoane de distribuire. Paginile care nu sunt recunoscute ca articol, de exemplu cele cu știri live, folosesc extractorul anterior. Sunt incluse licențele SmartReader (Apache-2.0) și AngleSharp (MIT).
- `THIRD-PARTY-NOTICES.md` menționează acum și Microsoft Edge WebView2 SDK, folosit de WebReader; licența și notificările pachetului sunt incluse în `Licenses/WebView2`.
- Exportul OPML pune feedurile neorganizate la rădăcina fișierului, ca în celelalte cititoare RSS, în loc de un folder „Neorganizate”.
