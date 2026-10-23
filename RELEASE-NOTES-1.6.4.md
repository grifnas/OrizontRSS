# Orizont RSS 1.6.4 — Note de versiune

## Noutăți

- **Teme sonore:** în Setări, la „Temă sonoră”, alegi între Sunetele Windows (implicit), Interfață, Clopoței și Orizont. Temele au sunete pentru articole noi, actualizare încheiată, erori, schimbarea folderului, capătul listei, favorite, ștergere, răspunsuri AI și traduceri, precum și sincronizarea NewsBlur. „Testează sunetul” redă tema aleasă. „Activează alertele sonore” oprește toate sunetele aplicației.
- **Bare de stare în toate ferestrele:** cele 17 ferestre și dialoguri care nu aveau bară de stare au acum una jos. La deschidere, cititorul de ecran anunță titlul ferestrei și cum se parcurg comenzile.

## Corecturi

- **Cititorul Orizont, modul text:** tastele Application și Shift+F10 deschid meniul contextual de fiecare dată, fără să se închidă imediat.
- **Cititorul Orizont, WebReader:** Application și Shift+F10 deschid meniul nativ, cu comenzile Cititorului, și funcționează săgețile, Enter și submeniurile.
- **Traducere DeepL în Cititor:** citirea vocală în curs se oprește înainte de înlocuirea textului cu traducerea, iar F9 citește textul tradus.
- **Bara de stare din Cititor:** mesajul curent este și numele accesibil al barei, iar rezultatul traducerii DeepL se repetă după ce focusul ajunge în text.

## Limite cunoscute

- În WebReader, mesajele de stare nu sunt accesibile cititorului de ecran, deoarece focusul rămâne în motorul web. În modul text, bara de stare funcționează. Ghidul utilizatorului precizează asta.
- Pentru corecturile de la meniul din WebReader și de la bara de stare, testul practic cu JAWS 2026 este făcut de autor doar pentru meniu.

## Verificări și limite de acceptare

Verificările automate pentru sursă, fluxuri de lucru, localizare, ghiduri, codare și distribuție rulează înainte de publicare. Ele nu înlocuiesc acceptarea practică cu cititorul de ecran.

Probele practice A02 (NewsBlur pe două calculatoare), A09 (scenariile Google Translate), A07 (revizia lingvistică umană) și A11 (verificarea completă a interfeței cu JAWS 2026) rămân deschise, conform registrului de acceptare.

Pachetele includ .NET și Windows Desktop 8.0.31; WebReader folosește Microsoft Edge WebView2 Runtime. Instalatorul nu este semnat digital; semnarea este amânată, conform foii de parcurs.
