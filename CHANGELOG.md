# Istoricul modificărilor curente

Acest fișier descrie numai ultima versiune publicată și lucrările ulterioare încă nepublicate. Istoricul mai vechi poate fi recuperat din istoricul Git și din release-urile publice anterioare; nu este copiat în această pagină activă.

## Versiunea publicată v1.6.8 — 15 noiembrie 2026

Release-ul public v1.6.8 este disponibil pe [GitHub Releases](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.8). Notele complete sunt în release-ul de pe GitHub.

- Caseta notelor din fereastra „Actualizare disponibilă” are `AcceptsReturn`, ca JAWS să citească notele rând cu rând cu săgețile. În 1.6.7 se citea doar un rând. Confirmat cu JAWS în versiunea de test.
- Dacă `feeds.json` este trunchiat sau gol la pornire, aplicația folosește copia `feeds.json.bak` și păstrează fișierul corupt lângă ea, ca `feeds.json.corrupt-<data>`. Până acum pornirea se oprea.
- Cititorul Orizont deschide din articol numai linkuri web (HTTP, HTTPS) și de e-mail; fișierele locale, căile de rețea și protocoalele de aplicații sunt refuzate.
- La pornire sunt șterse fișierele temporare de salvare mai vechi de o zi, rămase după o oprire bruscă.
- Jurnal tehnic de funcționare în folderul de diagnostic (`jurnal.txt`): pornire, oprire, rezultatul actualizărilor și feedurile cu eroare, fără adrese, conținut de articole sau chei.
- Rapoartele de eroare ascund adresele cu chei, tokenii Bearer și antetele Cookie înainte de a fi scrise; filtrul este euristic.
- Erorile neașteptate din sarcinile de fundal lasă un raport tehnic în folderul de diagnostic, ca erorile din interfață.
- Microsoft Edge WebView2 SDK actualizat la 1.0.4314.35.
- Integrarea continuă verifică pachetele NuGet cu vulnerabilități cunoscute, înainte de verificarea completă.
- `global.json` fixează compilarea pe .NET SDK 8.0, la fel local și pe GitHub Actions.
- `MainWindow` este împărțit în fișiere `partial`, fără schimbare de comportament.

## Versiunea publicată v1.6.7 — 8 noiembrie 2026

Release-ul public v1.6.7 a fost disponibil pe [GitHub Releases](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.7). Notele lui nu mai sunt în arbore.

- Rezultatele căutării YouTube se deschid implicit în fereastra Orizont; focusul revine pe videoclipul deschis după Escape, în rezultate, în lista canalului și în lista articolelor.
- Meniul Feeduri → Organizare: „Vezi toate videoclipurile canalului YouTube”, cu încărcarea paginilor următoare.
- Alt+K din player deschide transcriptul videoclipului.
- Transcriptele se salvează în `Documente\Orizont RSS\Transcripts`; folderul implicit pentru articole este `Articles`.
- Notele versiunii din fereastra de actualizare își păstrează liniile. Afirmația că se citesc rând cu rând cu săgețile nu era adevărată pentru această versiune; remedierea a intrat în 1.6.8.

## Lucrări nepublicate după v1.6.8

Nicio lucrare nepublicată deocamdată.
