# Orizont RSS 1.6.2 — Note de versiune

## Noutăți

- **Mod Citire al browserului, în fereastră externă:** la adăugarea sau editarea unui feed, la „La deschiderea articolului”, și în Setări, la „Deschiderea implicită a articolelor”, poți alege ca articolele să se deschidă în modul Citire al Microsoft Edge, într-o fereastră separată. Opțiunea există și în meniul contextual al articolului.
- **Sold DeepSeek și cotă DeepL:** în Setări, lângă cheia API, poți verifica soldul contului DeepSeek și caracterele DeepL folosite și rămase. Verificarea nu trimite niciun articol și nu schimbă setările.
- **Limba țintă pentru DeepL:** în Setări poți alege limba traducerii DeepL. Implicit, traducerea urmează limba interfeței.
- **Instrucțiuni AI în limba interfeței:** instrucțiunea implicită pentru asistentul AI apare în limba aleasă pentru interfață.
- **Articolele se marchează citite** și după deschiderea în modul Citire al browserului.

## Corecturi

- DeepL traducea în engleză atunci când interfața era în maghiară sau italiană; acum folosește maghiara, respectiv italiana.
- Actualizarea prin RSS nu mai șterge articolele care au ieșit din fluxul site-ului. Favoritele, articolele „Mai târziu”, etichetele și notițele AI rămân.
- Sincronizarea NewsBlur nu mai raportează conflict când aceeași schimbare a fost făcută pe ambele părți.
- Curățarea textului articolelor nu mai șterge cuvinte obișnuite din fraze și nu mai taie articolul la expresii din text.
- În limbile străine, poziția articolului, folderul implicit și „Toate folderele” apar tradus, iar datele NewsBlur rămân neschimbate.
- Vocea eSpeak NG funcționează și când aplicația este instalată într-un folder cu diacritice.
- Vocea eSpeak NG are volum mai mare, reglabil din aceeași setare.
- Gemini are limită de răspuns mai mare și mesaje clare pentru răspunsurile tăiate sau blocate.
- Textul integral al articolelor este extras cu SmartReader 0.9.6, iar găsirea feedurilor pe un site este mai fiabilă.
- Actualizarea feedurilor este mai rapidă: descărcare paralelă și feedurile nemodificate nu se mai descarcă.
- Căutarea după subiect prin Feedly funcționează din nou.
- Ghidurile pentru utilizatori în cele opt limbi descriu noile funcții.

## Verificări și limite cunoscute

Verificările automate pentru sursă, fluxuri de lucru, localizare, ghiduri, codare și distribuție rulează înainte de publicare. Ele nu înlocuiesc acceptarea practică cu cititorul de ecran.

Probele practice A02 (NewsBlur pe două calculatoare), A09 (scenariile Google Translate), A07 (revizia lingvistică umană) și A11 (verificarea interfeței cu JAWS 2026) rămân deschise, conform registrului de acceptare. Ele nu sunt declarate trecute.

Auditul online NuGet (`NU1900`) poate rămâne fără date într-un mediu izolat. Pachetele includ .NET și Windows Desktop 8.0.31, iar WebReader folosește Microsoft Edge WebView2 Runtime.

Acest candidat nu este semnat digital. Semnarea instalatorului este amânată, conform foii de parcurs.
