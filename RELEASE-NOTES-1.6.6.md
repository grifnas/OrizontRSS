# Orizont RSS 1.6.6 — note de lucru

Versiunea 1.6.6 este lansată pe GitHub, cu installerul autonom, varianta portabilă ZIP, arhiva sursă și fișierele SHA-256 atașate release-ului. Ultima versiune publică anterioară era 1.6.5.

## Videoclipuri YouTube

- Articolele cu adresă de videoclip YouTube, inclusiv cele din canalele la care ești abonat, se deschid într-o fereastră Orizont cu playerul oficial YouTube.
- Comenzile din fereastra videoclipului sunt Alt+Z și Alt+X (înapoi 30 și 5 secunde), Alt+V și Alt+B (înainte 5 și 30 de secunde), Alt+C (redare sau pauză), Alt+S (viteza de redare), Alt+T (poziția, procentul și timpul rămas) și Alt+D (deschide în browser). Escape oprește videoclipul și închide fereastra.
- Tonul nu se poate schimba la videoclipurile YouTube.
- Butonul „Deschide în Orizont” apare la rezultatele video din căutare.
- Transcriptul videoclipului se descarcă automat, se poate copia și se poate salva ca fișier `.txt`.
- Căutarea de videoclipuri afișează mai multe rezultate cu „Mai multe rezultate”.

## Siguranța datelor

- Setările și feedurile se salvează întâi într-un fișier temporar și apoi înlocuiesc fișierul vechi. Ultima stare salvată rămâne în copia `.bak` de lângă fișier.

## Instalator

- Instalatorul refuză folderele de sistem și personale, cum sunt rădăcina unui disc, profilul, Documente, Desktop sau Program Files, precum și un folder nevid care nu este o instalare Orizont.
- Dezinstalarea șterge numai un folder care conține `Orizont.exe`.

## Ce nu este verificat încă

- Canalele și playlisturile YouTube din căutare: butonul, anunțurile, focusul și adăugarea feedului.
- Importul transcriptului din clipboard și deschiderea paginii YouTube din dialogul transcriptului.
- Verificarea instalatorului cu refuzul folderelor nesigure și cu dezinstalarea.
- Acceptările JAWS rămase deschise din registrul de acceptare: A02, A07, A09 și A11.

## Limite cunoscute

- Căutarea YouTube și redarea depind de pagini și de servicii neoficiale, care se pot schimba sau limita.
- Unele videoclipuri au embed-ul dezactivat de autor și se deschid doar în browser.
