# Orizont RSS 1.6.3 — Note de versiune

## Corecturi importante

- **SmartReader funcționează din nou.** În versiunea 1.6.2, biblioteca SmartReader nu pornea din cauza unei incompatibilități cu analizorul HTML AngleSharp, actualizat pentru un avertisment de securitate. Textul integral era extras atunci doar cu extractorul vechi. SmartReader a fost actualizat la 0.11.1, compatibil cu AngleSharp 1.7.1.
- **Meniul contextual din Cititorul Orizont:** tasta Application și Shift+F10 deschid meniul cu comenzile Cititorului de fiecare dată, în modul text și în WebReader. În WebReader, meniul se parcurge cu săgețile, iar submeniurile și comenzile funcționează.
- **Citirea vocală după traducere:** după traducerea DeepL în Cititor, F9 pornește citirea textului tradus; citirea în curs a textului original se oprește la traducere.

## Noutăți

- **WebReader structurat:** WebReader afișează articolul ales de SmartReader cu subtitluri, liste, citate, tabele și linkuri, care pot fi parcurse cu tastele de navigare ale cititorului de ecran (H, L, Tab). Scripturile, imaginile și formularele paginii sunt eliminate. Textul tradus și paginile care nu sunt recunoscute ca articol rămân afișate în paragrafe.

## Verificări și limite cunoscute

Verificările automate pentru sursă, fluxuri de lucru, localizare, ghiduri, codare și distribuție au fost rulate pentru 1.6.3. Meniul contextual și citirea vocală au fost confirmate practic cu JAWS 2026 de autor.

Probele practice A02 (NewsBlur pe două calculatoare), A09 (scenariile Google Translate), A07 (revizia lingvistică umană) și A11 (verificarea completă a interfeței cu JAWS 2026) rămân deschise, conform registrului de acceptare.

Pachetele includ .NET și Windows Desktop 8.0.31; WebReader folosește Microsoft Edge WebView2 Runtime. Instalatorul nu este semnat digital; semnarea este amânată, conform foii de parcurs.
