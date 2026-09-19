# Reguli de lucru pentru Orizont RSS

Acest fișier conține regulile cerute de utilizator. Trebuie consultat înaintea fiecărei intervenții asupra proiectului.

## Reguli active

1. Nu crea distribuții noi, arhive de distribuție sau pachete de lansare decât la cererea expresă a utilizatorului.
2. După fiecare schimbare sau grup de schimbări, construiește, când este necesar, executabilul local de test și oferă utilizatorului un link direct către acesta.
3. Linkul pentru executabilul de test trebuie să indice calea absolută a fișierului `.exe` rezultat, nu o distribuție implicită și nu o arhivă.
4. Nu considera o distribuție autorizată doar pentru că ai construit un executabil de test. Distribuția necesită o cerere expresă separată.
5. Respectă preferințele existente ale proiectului privind accesibilitatea, navigarea cu tastatura, compatibilitatea cu cititoarele de ecran și verificarea executabilului rezultat.
6. Înaintea fiecărei distribuții noi, verifică faptul că toate limbile interfeței sunt complete și la zi.
7. Înaintea oricărei modificări, stabilește și păstrează explicit comportamentele existente care trebuie protejate.
8. Nu modifica funcții fără legătură cu cererea curentă și nu face refactorizări necerute în cod deja funcțional.
9. După fiecare modificare, rulează testele automate relevante și verifică rezultatul compilării înainte de a continua.
10. Pentru modificările care afectează interfața, focalizarea, comenzile de tastatură, vocea sau cititoarele de ecran, include și o verificare manuală țintită cu JAWS/NVDA.
11. Notează în `WORKLOG.md` fiecare intervenție efectivă: scopul, fișierele atinse, testele rulate, rezultatul și executabilul de test rezultat.
12. Nu șterge sau suprascrie datele utilizatorului, setările, feedurile ori istoricul fără cerere expresă și confirmare clară.
13. Dacă o schimbare riscă să afecteze o funcție existentă, oprește implementarea și explică riscul înainte de a continua.
14. Ține separat proiectul Windows de experimentele Android sau de alte prototipuri. Un experiment pus pe pauză nu se reia și nu se extinde fără confirmarea utilizatorului.
15. Pentru probleme de rețea sau compatibilitate, nu face serii de ajustări speculative. Izolează problema printr-un test reproductibil și raportează ce informație lipsește înainte de a continua.
16. După revenirea la un proiect, citește `docs\PROJECT-STATUS.md` și continuă de la starea consemnată acolo; nu inventa o etapă nouă și nu repeta etape închise.
17. Când utilizatorul cere o acțiune, rezolvă punctual acțiunea respectivă fără a scana tot proiectul. Scanarea totală se face numai la cererea expresă a utilizatorului sau cu avertisment prealabil, dacă se impune.

## Flux verificat — publicarea paginii GitHub Pages

Pentru actualizări ale paginii statice, fără modificări în aplicație:

1. Actualizează toate cele opt fișiere `docs/index*.html`: versiunea din titlul descărcării, subsol și metadatele JSON-LD `softwareVersion`. Păstrează linkurile către asseturile `releases/latest`.
2. Validează toate cele opt limbi, absența versiunii vechi, păstrarea linkului către installer și `git diff --check`; consemnează schimbarea în `WORKLOG.md` și starea în `docs/PROJECT-STATUS.md`/`docs/ROADMAP.md`.
3. Dacă integrarea GitHub poate doar citi sau shell-ul Codex nu poate folosi sesiunea GCM Windows a utilizatorului, nu cere și nu copia tokenul. Pregătește commitul local și cere utilizatorului să ruleze push-ul din PowerShell-ul obișnuit, unde GCM este conectat.
4. Dacă acel shell raportează `detected dubious ownership`, folosește pentru o singură comandă calea exactă indicată de Git, fără excepție globală largă: `git -c "safe.directory=<calea exactă>" -C "<calea exactă>" push origin HEAD:main`.
5. Confirmă publicarea comparând SHA-ul local cu `refs/heads/main` și deschizând pagina GitHub Pages live pentru a verifica versiunea anunțată. Mesajul „100% / done” din JAWS este un indiciu util, dar verificarea remote și a paginii rămâne necesară.
6. Pentru această lucrare exclusiv statică nu se construiește aplicația și nu se creează distribuție nouă.

## Reguli noi

Utilizatorul poate adăuga reguli noi în orice moment. Regulile noi se introduc aici și devin active după ce sunt consemnate.
