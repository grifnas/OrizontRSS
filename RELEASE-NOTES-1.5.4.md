# Orizont RSS 1.5.4 — sincronizare NewsBlur și accesibilitate

Această distribuție locală Windows x64 reunește modificările realizate după versiunea publică 1.5.3. Release-ul public GitHub rămâne 1.5.3; această pregătire nu publică și nu înlocuiește nimic online.

## Noutăți

- Sincronizare NewsBlur pentru feeduri, foldere, articole, citit/necitit, favorite și etichete. La prima conectare pe un profil local curat se importă din NewsBlur; după inițializare, sincronizarea este bidirecțională.
- La pornire și la actualizare, NewsBlur poate furniza articolele. Actualizarea RSS locală rămâne o alternativă pentru feedurile care nu sunt disponibile prin NewsBlur sau când solicitarea de articole eșuează.
- După curățarea duplicatelor locale, structura feedurilor și folderelor se sincronizează din nou cu NewsBlur. Feedul păstrat cu aceeași adresă nu este dezabonat.
- Dialogurile de decizie privind sincronizarea și duplicatele au focalizare explicită și trasee de tastatură mai previzibile.
- Panourile fără conținut anunță distinct lipsa feedurilor, lipsa articolului selectat și lipsa rezultatelor; mesajele nu se suprapun peste conținut real.
- Selectarea unui folder revine la articolele acelui folder, nu la vederea agregată „Citește acum”.
- Mesajele noi sunt localizate în toate cele șapte limbi non-române ale aplicației.

## Pachete

- Portabil: arhivă autonomă Windows x64.
- Instalare: installer Windows x64 offline, cu arhiva aplicației inclusă și verificată prin SHA-256.
- Cod sursă: arhivă separată, fără date, setări sau istoricul personal al utilizatorului.

Instalarea și sincronizarea reală cu NewsBlur nu au fost rulate automat pe profilul utilizatorului. Recomand testarea manuală a executabilului și a installerului cu JAWS/NVDA înainte de publicarea acestei versiuni.
