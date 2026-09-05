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

## Reguli noi

Utilizatorul poate adăuga reguli noi în orice moment. Regulile noi se introduc aici și devin active după ce sunt consemnate.
