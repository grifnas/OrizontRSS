# Orizont RSS

Orizont RSS este un cititor RSS accesibil pentru Windows, proiectat pentru utilizare completă din tastatură și compatibil cu cititoarele de ecran uzuale.

Aplicația a fost inițiată și este coordonată de **Grigore Frișan**. Dezvoltarea a fost realizată în colaborare cu **OpenAI Codex**, care a oferit asistență pentru proiectarea aplicației, programare, depanare, testare, documentare și pregătirea versiunilor de distribuție.

## Versiunea publicată

Cea mai recentă versiune publicată este **v1.6.1**, lansată la 8 octombrie 2026.

- [Pagina de prezentare și descărcare](https://grifnas.github.io/OrizontRSS/)
- [Release-ul v1.6.1 și toate fișierele sale](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.1)
- [Instalatorul autonom v1.6.1](https://github.com/grifnas/OrizontRSS/releases/download/v1.6.1/OrizontSetup-1.6.1.exe)
- [Varianta portabilă v1.6.1](https://github.com/grifnas/OrizontRSS/releases/download/v1.6.1/Orizont-RSS-1.6.1-win-x64.zip)
- [Codul exact al release-ului v1.6.1](https://github.com/grifnas/OrizontRSS/tree/v1.6.1)

Arhiva sursă și fișierele cu sumele SHA-256 sunt atașate release-ului. Pentru instalare și descărcare folosește numai fișierele din release-ul oficial.

## Limite cunoscute și acceptare

Notele release-ului precizează deschis că probele practice A02 (NewsBlur pe două calculatoare), A09 (scenarii Google Translate), A07 (revizie lingvistică umană) și A11 (verificarea interfeței cu JAWS 2026) rămân pentru ciclul următor și nu sunt declarate trecute. Auditul online NuGet nu a putut fi încheiat (`NU1900`).

## Sursa în lucru — după release-ul v1.6.1

Tagul `v1.6.1` și pachetele release-ului sunt reperul exact al versiunii publice. Ramura `main` poate primi actualizări de documentație, pagini publice și manifeste WinGet după tag; acestea **nu fac parte din pachetele publice v1.6.1**. Un executabil recompilat local nu este binarul public v1.6.1. Pentru versiunea publică descarcă fișierele atașate release-ului.

Modul WebReader folosește Microsoft Edge WebView2 Runtime, disponibil în Windows/Edge. Alertele după cuvinte-cheie nu fac parte din versiunea 1.6.1.

## Funcții incluse în versiunea publică v1.6.1

- organizarea feedurilor în foldere și import/export OPML;
- descoperirea feedurilor și actualizarea lor simultană, cu posibilitate de anulare;
- filtrarea articolelor după stare, perioadă, folder și etichetă;
- favorite, lista „Mai târziu”, etichete, autoetichetare și reguli de păstrare;
- căutare locală, selecție multiplă și operații asupra articolelor;
- sincronizare NewsBlur pentru feeduri, foldere, articole și stări;
- furnizori AI configurabili: Gemini, OpenAI, Mistral și DeepSeek;
- traducere online prin Google Translate sau DeepL și discuții cu AI despre articol;
- Cititor Orizont în modurile Text și WebReader, cu meniuri contextuale;
- scurtături configurabile, export TXT/RTF și partajare cu indicarea sursei și traducătorului;
- salvarea, copierea și partajarea articolelor și conversațiilor;
- citire prin vocile locale SAPI5 și eSpeak NG, precum și Gemini TTS;
- ajutor accesibil, ghiduri HTML și interfață în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană;
- navigare cu tastatura, meniuri contextuale și focalizare concepute pentru cititoare de ecran.

Lista completă a noutăților release-ului este în [RELEASE-NOTES-1.6.1.md](RELEASE-NOTES-1.6.1.md).

## Instalare

Poți instala aplicația cu instalatorul autonom sau o poți rula din arhiva portabilă:

1. Descarcă fișierul dorit din release-ul v1.6.1.
2. Pentru varianta portabilă, dezarhivează conținutul într-un folder și pornește Orizont.exe.
3. Deschide Setări pentru limbă, voce, feeduri și configurarea opțională a cheilor API.

Pachetele v1.6.1 includ runtime-ul .NET necesar și nu cer instalarea separată a .NET Desktop Runtime. Pentru siguranță, descarcă aplicația numai din release-ul oficial de mai sus.

## Accesibilitate și raportarea problemelor

Aplicația este construită pentru tastatură și testare cu JAWS 2026 și NVDA. Automatizarea nu înlocuiește verificarea practică cu cititorul de ecran. Pentru a raporta o problemă, menționează versiunea Windows, versiunea Orizont RSS, cititorul de ecran și pașii exacți de reproducere. Folosește [Issues pe GitHub](https://github.com/grifnas/OrizontRSS/issues).

## Cerințe

- Windows 10 sau Windows 11 pe 64 de biți;
- pentru compilarea sursei: .NET 8 SDK;
- conexiune la internet pentru RSS, sincronizare și traduceri;
- chei API proprii pentru funcțiile AI care le solicită; acestea sunt opționale;
- pentru SAPI5, cel puțin o voce compatibilă instalată în Windows. eSpeak NG este inclus în pachetul v1.6.1.

## Documentație și contribuții

- [Instrucțiuni de compilare](BUILDING.md)
- [Starea publicării și a ramurii de lucru](PUBLICATION.md)
- [Foaia de parcurs](docs/ROADMAP.md)
- [Contribuții](CONTRIBUTING.md)
- [Licența GPL-3.0-or-later](LICENSE)
- [Notificări privind componentele terțe](THIRD-PARTY-NOTICES.md)

Resursele și ghidurile de interfață sunt verificate automat pentru toate cele opt limbi înaintea unei distribuții.
