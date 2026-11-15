# Orizont RSS 1.6.8 — note de versiune (candidat, nepublicat)

Versiunea 1.6.8 este în pregătire locală și nu este publicată. Ultima versiune publicată rămâne 1.6.7. Pachetele, tag-ul și release-ul pentru 1.6.8 se construiesc doar la cerere expresă.

## Fereastra „Actualizare disponibilă”

- Notele versiunii se citesc rând cu rând cu săgețile. În 1.6.7 se citea doar un rând: caseta notelor nu avea atributul `AcceptsReturn`, ca celelalte casete numai citire. Confirmat cu JAWS în versiunea de test.

## Date locale

- Dacă `feeds.json` este trunchiat sau gol la pornire, aplicația folosește copia `feeds.json.bak` și păstrează fișierul corupt lângă ea, ca `feeds.json.corrupt-<data>`. Până acum pornirea se oprea.

## Diagnostic

- Jurnalul tehnic `jurnal.txt` din folderul de diagnostic notează pornirea, oprirea, rezultatul actualizărilor și feedurile cu eroare. Nu conține adrese de feeduri, texte de articole sau chei.
- Raportul de eroare ascunde adresele cu chei, tokenii Bearer și antetele Cookie. Filtrul este euristic.
- Erorile neașteptate din sarcinile de fundal lasă un raport tehnic, ca erorile din interfață.

## Cititorul și securitate

- Cititorul Orizont deschide din articol numai linkuri web (HTTP, HTTPS) și de e-mail.
- Fișierele temporare de salvare mai vechi de o zi, rămase după o oprire bruscă, sunt șterse la pornire.
- Microsoft Edge WebView2 SDK este actualizat la 1.0.4314.35. WebReader a fost confirmat cu JAWS după actualizare.

## Lucrări interne

- `MainWindow` este împărțit în fișiere `partial`, fără schimbare de comportament.
- Integrarea continuă verifică pachetele NuGet cu vulnerabilități cunoscute; la verificarea din 15 noiembrie 2026 nu au fost găsite.
- Compilarea folosește .NET SDK 8 fixat prin `global.json`.

## Ce nu este verificat

- Instalatorul 1.6.8 nu există încă; instalarea și dezinstalarea rămân de verificat cu JAWS.
- Acceptările A02, A07, A09 și A11 rămân deschise.
- Ghidurile și paginile de prezentare afișează încă 1.6.7 și se actualizează la publicare.
