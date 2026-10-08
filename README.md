# Orizont RSS

Orizont RSS este un cititor RSS accesibil pentru Windows, proiectat pentru utilizare completă din tastatură și compatibil cu cititoarele de ecran uzuale.

Aplicația a fost inițiată și este coordonată de **Grigore Frișan**. Dezvoltarea a fost realizată în colaborare cu **OpenAI Codex**, care a oferit asistență pentru proiectarea aplicației, programare, depanare, testare, documentare și pregătirea versiunilor de distribuție.

## Versiunea publicată

Cea mai recentă versiune publicată și verificată în acest depozit este **v1.6.0**, lansată la 19 septembrie 2026.

- [Pagina de prezentare și descărcare](https://grifnas.github.io/OrizontRSS/)
- [Release-ul public v1.6.0 și toate fișierele sale](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.0)
- [Instalatorul autonom v1.6.0](https://github.com/grifnas/OrizontRSS/releases/download/v1.6.0/OrizontSetup-1.6.0.exe)
- [Varianta portabilă v1.6.0](https://github.com/grifnas/OrizontRSS/releases/download/v1.6.0/Orizont-RSS-1.6.0-win-x64.zip)
- [Codul exact al release-ului v1.6.0](https://github.com/grifnas/OrizontRSS/tree/v1.6.0)

Arhiva sursă și sumele SHA-256 sunt atașate release-ului de mai sus. Linkurile sunt fixate la v1.6.0 pentru ca această pagină să nu sugereze accidental că modificările locale ulterioare au fost publicate.

## Sursa în lucru — modificări încă nepublicate

Copia locală de lucru a ramurii main conține și modificări realizate după release-ul v1.6.0. Acestea sunt în lucru și **nu fac parte din pachetele publice v1.6.0**; prezența lor locală nu înseamnă că au fost trimise în ramura publică GitHub. Nu a fost creată o nouă distribuție pentru ele.

Executabilul local folosit pentru verificări este candidatul **1.6.1** (versiune de fișier 1.6.1.0), compilat din sursa de lucru. Nu este binarul public v1.6.0 și nu trebuie distribuit ca atare. Candidatul nu este publicat și nu există încă o distribuție 1.6.1.

Printre schimbările ulterioare se numără alegerea între furnizori AI (Gemini, OpenAI, Mistral și DeepSeek), configurarea scurtăturilor, reguli de autoetichetare, îmbunătățiri NewsBlur și modul WebReader. Modul WebReader din ramura de lucru necesită Microsoft Edge WebView2 Runtime; acest lucru nu schimbă cerințele pachetului public v1.6.0. Alertele după cuvinte-cheie au existat în v1.6.0 public, dar au fost eliminate ulterior din ramura de lucru la cererea utilizatorului.

## Funcții incluse în versiunea publică v1.6.0

- organizarea feedurilor în foldere și import/export OPML;
- descoperirea feedurilor și actualizarea lor simultană, cu posibilitate de anulare;
- filtrarea articolelor după stare, perioadă, folder și etichetă;
- favorite, lista „Mai târziu”, etichete și reguli de păstrare;
- căutare locală, selecție multiplă și operații asupra articolelor;
- sincronizare NewsBlur pentru feeduri, foldere, articole și stări;
- discuții Gemini despre articol și traducere online prin Google Translate sau DeepL;
- salvarea, copierea, exportul și partajarea articolelor și conversațiilor;
- citire prin vocile locale SAPI5 și eSpeak NG, precum și Gemini TTS;
- ajutor accesibil, ghiduri HTML și interfață în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană;
- navigare cu tastatura, meniuri contextuale și focalizare concepute pentru cititoare de ecran.

Lista completă a noutăților release-ului este în [RELEASE-NOTES-1.6.0.md](RELEASE-NOTES-1.6.0.md).

## Instalare

Poți instala aplicația cu instalatorul autonom sau o poți rula din arhiva portabilă:

1. Descarcă fișierul dorit din release-ul v1.6.0.
2. Pentru varianta portabilă, dezarhivează conținutul într-un folder și pornește Orizont.exe.
3. Deschide Setări pentru limbă, voce, feeduri și configurarea opțională a cheilor API.

Pachetele v1.6.0 includ runtime-ul .NET necesar și nu cer instalarea separată a .NET Desktop Runtime. Pentru siguranță, descarcă aplicația numai din release-ul oficial de mai sus.

## Accesibilitate și raportarea problemelor

Aplicația este construită pentru tastatură și testare cu JAWS 2026 și NVDA. Automatizarea nu înlocuiește verificarea practică cu cititorul de ecran. Pentru a raporta o problemă, menționează versiunea Windows, versiunea Orizont RSS, cititorul de ecran și pașii exacți de reproducere. Folosește [Issues pe GitHub](https://github.com/grifnas/OrizontRSS/issues).

## Cerințe

- Windows 10 sau Windows 11 pe 64 de biți;
- pentru compilarea sursei: .NET 8 SDK;
- conexiune la internet pentru RSS, sincronizare și traduceri;
- chei API proprii pentru funcțiile AI care le solicită; acestea sunt opționale;
- pentru SAPI5, cel puțin o voce compatibilă instalată în Windows. eSpeak NG este inclus în pachetul v1.6.0.

## Documentație și contribuții

- [Instrucțiuni de compilare](BUILDING.md)
- [Starea publicării și a ramurii de lucru](PUBLICATION.md)
- [Foaia de parcurs](docs/ROADMAP.md)
- [Contribuții](CONTRIBUTING.md)
- [Licența GPL-3.0-or-later](LICENSE)
- [Notificări privind componentele terțe](THIRD-PARTY-NOTICES.md)

Resursele și ghidurile de interfață sunt verificate automat pentru toate cele opt limbi înaintea unei distribuții.
