# Istoricul modificărilor curente

Acest fișier descrie numai ultima versiune publicată și lucrările ulterioare încă nepublicate. Istoricul mai vechi poate fi recuperat din istoricul Git și din release-urile publice anterioare; nu este copiat în această pagină activă.

## Versiunea publicată v1.6.5 — 26 octombrie 2026

Release-ul public v1.6.5 este disponibil pe [GitHub Releases](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.5). Notele complete sunt în [RELEASE-NOTES-1.6.5.md](RELEASE-NOTES-1.6.5.md).

- Podcasturi în Cititor: bara de comenzi (-30 s, -5 s, redare, +5 s, +30 s, viteză, ton, descărcare) și volumul podcastului cu Alt+săgeată sus/jos, în pași de 5%, între 0% și 300%.
- Podcasturi din afișarea standard: „Redă sau pune pe pauză podcastul” în meniul Articol.
- Un singur player pentru fereastra principală și Cititor; tastele podcastului se tratează o singură dată.
- Căutarea podcasturilor prin Apple Podcasts din descoperirea subiectelor.
- YouTube: descrierea din feed și notă, fără pagina de consimțământ; „Deschide pagina originală în browser” în meniul Cititorului.
- Tasta Application în modul text deschide meniul Cititorului.
- Tema sonoră implicită este Orizont.

## Lucrări nepublicate după v1.6.5 (versiunea 1.6.6, în pregătire locală)

Notele de lucru sunt în [RELEASE-NOTES-1.6.6.md](RELEASE-NOTES-1.6.6.md). Nu există pachete, tag sau release pentru 1.6.6.

- Transcript YouTube: preluarea automată funcționează din nou (Innertube, cu rezervă pagina videoclipului și cookie de consimțământ); sunt citite ambele formate de subtitrare.
- Transcript YouTube: lista limbilor afișează și anunță numai numele limbii, fără coduri sau adrese.
- Transcript YouTube: descărcare, copiere și salvare ca `.txt`, confirmate cu JAWS 2026.
- Căutare YouTube, videoclipuri: butonul „Mai multe rezultate” încarcă paginile următoare; limita de 20 de rezultate a fost ridicată.
- Videoclipuri YouTube într-o fereastră Orizont, cu playerul oficial și comenzile Alt+Z, Alt+X, Alt+V, Alt+B, Alt+C, Alt+S, Alt+T și Alt+D. Escape oprește videoclipul și închide fereastra.
- Articolele cu adresă de videoclip YouTube, inclusiv din canalele la care ești abonat, se deschid în fereastra de videoclip.
- Salvarea setărilor și a feedurilor se face întâi într-un fișier temporar, cu copie `.bak` a stării anterioare.
- Instalatorul refuză folderele de sistem și personale și șterge la dezinstalare numai un folder care conține Orizont.exe.
- Confirmat manual cu JAWS 2026 la 2 noiembrie 2026 pentru transcript și căutarea de videoclipuri. Restul funcțiilor noi sunt listate ca neverificate în RELEASE-NOTES-1.6.6.md.
