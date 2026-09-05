# Orizont RSS

Orizont RSS este un cititor RSS accesibil pentru Windows, proiectat pentru utilizare completă din tastatură și compatibil cu cititoarele de ecran uzuale.

Aplicația a fost inițiată și este coordonată de **Grigore Frișan**. Dezvoltarea a fost realizată în colaborare cu **OpenAI Codex**, care a oferit asistență pentru proiectarea aplicației, programare, depanare, testare, documentare și pregătirea versiunilor de distribuție.

## Funcții principale

- organizarea feedurilor în foldere;
- import și export OPML;
- descoperirea feedurilor dintr-o pagină web sau din catalogul local;
- actualizarea simultană și anulabilă a feedurilor;
- filtrarea articolelor după stare, perioadă, folder și etichetă;
- favorite, lista „Mai târziu” și etichete;
- identificarea feedurilor care necesită atenție;
- prevenirea și curățarea feedurilor și articolelor duplicate;
- reguli de păstrare și curățare a articolelor vechi;
- conversații Gemini despre conținutul integral al articolului;
- traducere online opțională prin DeepL, din meniul contextual al articolului;
- salvarea, copierea, exportul și partajarea conversațiilor AI;
- backup și restaurare;
- citire prin vocile locale SAPI5 și eSpeak NG sau prin cele 30 de voci online Gemini TTS;
- ajutor accesibil cu `F1` și ghiduri HTML cu titluri în română, engleză, spaniolă, franceză, germană și portugheză;
- control complet din tastatură și restaurarea focalizării după citirea unui articol;
- scurtături documentate pentru panouri (`F6`), căutare (`Ctrl+F` și `F3`), citire vocală (`F9` / `Ctrl+Alt+V`), control vocal (`Ctrl+Alt+P`, `Ctrl+Alt+S`) și maximizare/restaurare (`F11`);
- interfață în română, engleză, spaniolă, franceză, germană și portugheză, cu alegere automată după limba Windows sau selecție explicită în Setări.

Resursele interfeței și ghidurile HTML sunt verificate automat pentru toate cele șase limbi înaintea unei distribuții.

## Cerințe

- Windows 10 sau Windows 11 pe 64 de biți;
- pentru compilare: .NET 8 SDK;
- pentru funcțiile Gemini: conexiune la internet și o cheie API configurată de utilizator;
- pentru traducerea DeepL: conexiune la internet și o cheie API DeepL configurată de utilizator;
- pentru citirea SAPI5: cel puțin o voce SAPI5 instalată în Windows; eSpeak NG nu necesită instalare separată; Gemini TTS necesită internet și o cheie API Gemini.

Distribuția Windows x64 este autonomă și nu necesită instalarea separată a .NET Runtime.

## Configurare DeepL

În aplicație, deschide `Setări` → `Inteligență artificială`. Apasă `Obține cheie API DeepL`; pagina oficială DeepL pentru dezvoltatori conține opțiunea de creare a unui cont API Free și fila `API Keys & Limits`. Creează cheia, copiaz-o în câmpul `Cheie API DeepL`, apasă `Testează conexiunea DeepL`, apoi `Salvează`. Testul verifică utilizarea contului fără să trimită un articol. Traducerea pornește ulterior din meniul contextual al articolului și folosește limba interfeței ca limbă țintă.

## Confidențialitate

Feedurile, articolele, setările, notițele și cheia Gemini sunt păstrate în profilul local al utilizatorului și nu fac parte din distribuție sau din codul-sursă. Cheia Gemini este protejată prin mecanismul Windows DPAPI pentru contul curent.

Textul este trimis către Google numai atunci când utilizatorul pornește explicit o funcție Gemini, inclusiv citirea Gemini TTS. Citirea SAPI5 și eSpeak NG este locală. Gemini TTS poate consuma cota sau creditele API ale utilizatorului.

DeepL rulează online atunci când utilizatorul pornește explicit traducerea. Textul articolului este trimis către DeepL și poate consuma limita contului API. Cheia este protejată local prin mecanismul Windows DPAPI.

## Compilare

Instrucțiunile complete sunt în [BUILDING.md](BUILDING.md).

## Contribuții

Rapoartele de probleme, corecțiile, traducerile și îmbunătățirile de accesibilitate sunt binevenite. Consultă [CONTRIBUTING.md](CONTRIBUTING.md) înainte de a trimite o contribuție.

## Publicare

Planul pentru publicarea depozitului, a versiunilor Windows, a paginii GitHub Pages și a pachetului WinGet este în [PUBLICATION.md](PUBLICATION.md).

## Licență

Copyright © 2026 Grigore Frișan.

Orizont RSS este software liber, distribuit în condițiile GNU General Public License, versiunea 3 sau, la alegerea utilizatorului, orice versiune ulterioară (`GPL-3.0-or-later`). Textul integral se află în [LICENSE](LICENSE).

Componentele și serviciile terțe sunt descrise în [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
