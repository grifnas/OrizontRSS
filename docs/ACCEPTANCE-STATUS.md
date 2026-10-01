# Orizont RSS — situația celor 12 puncte de consolidare

Actualizat la 1 octombrie 2026. Acesta este registrul scurt al stării **locale**, nu o certificare a distribuției publice. Criteriile complete și ordinea lucrărilor sunt în [ROADMAP.md](ROADMAP.md); probele practice sunt în [MANUAL-REGRESSION-CHECKLIST.md](MANUAL-REGRESSION-CHECKLIST.md), iar intervențiile datate sunt în [WORKLOG.md](../WORKLOG.md). Raportul [AUDIT-2026-09-27.md](AUDIT-2026-09-27.md) consemnează constatările inițiale, nu starea reparată.

Buildul de lucru păstrat este `bin/Release/net8.0-windows10.0.17763.0/Orizont.exe`, versiunea fișierului 1.6.0.0. Biblioteca `Orizont.dll` are SHA-256 `A31861C33E0B467E91E0D276E9853F404CEB8C82A2CCFFC3B99913B999C22961`. Sursele au punctul local de revenire `checkpoint-local-2026-09-30`; acesta nu include modificările ulterioare. Nicio distribuție nouă nu a fost creată în consolidarea de mai jos.

## Starea pe puncte

1. **A10 — Plasa comună de teste: închis local.** Runnerul Full a trecut anterior; la 1 octombrie, `WorkflowSmoke` a trecut de trei ori consecutiv cu 148/148 și exit code 0 după corectarea propriului mecanism de așteptare. Full nu a fost rerulat după acea corecție.
2. **A01 — Identitatea articolului: închis local.** Testele simulate și confirmarea utilizatorului cu JAWS din 28 septembrie acoperă schimbarea selecției și reîncărcarea Cititorului Orizont.
3. **A02 — Bifele NewsBlur: deschis, verificare practică parțială.** Sunt confirmate oprirea tuturor bifelor, pornirea oglindirii structurale și anunțul final al stărilor. Lipsesc dovada transferului unei stări și proba intervalului periodic; două feeduri au rămas cu eroare la proba structurală.
4. **A03 — Instanța unică: deschis, verificare practică parțială.** Dialogul celei de-a doua porniri și prima fereastră rămasă vizibilă au fost confirmate. Lipsește confirmarea că prima fereastră poate fi folosită după închiderea dialogului; cititorul folosit la acea probă nu a fost precizat.
5. **A05 — Backup și restaurare: închis local pe baza testelor automate.** Testele acoperă preferințele nesensibile și excluderea cheilor/sesiunii; restaurarea prin dialog într-o instalare reală nu a fost probată.
6. **A04 — Actualizare și oprire: închis local.** Testele simulează integritatea descărcării și ordinea salvare–pornire; utilizatorul a confirmat funcționarea în versiunea curentă. Instalarea reală de probă este o verificare separată, neefectuată.
7. **A06 — Vocea răspunsurilor AI: închis local.** Traseele din panoul principal și Cititor Orizont au test automat și confirmare practică JAWS pentru F9 și Escape; NVDA nu a fost dedus din aceasta.
8. **A08 — Traducere, export și proveniență: deschis, confirmare parțială.** Footerul, revenirea la original, copierea rezultatului Google și salvările TXT/RTF din cititor au confirmări limitate. Lipsesc proba completă Text/WebReader, răspunsul AI, e-mail/WhatsApp și păstrarea footerului în mesaje lungi scurtate.
9. **A09 — Cereri Google Translate simultane: deschis.** Testele simulate au trecut; utilizatorul a amânat proba practică JAWS, care ar folosi serviciul online.
10. **A07 — Localizare dincolo de catalog: deschis.** Structura și șirurile generate au trecut testele automate în opt limbi; lipsesc proba practică JAWS în engleză și revizia umană a naturaleții traducerilor.
11. **A11 — Performanță: deschis.** `ScaleSmoke` a măsurat date sintetice și lista WPF off-screen. Nu există încă probă a latenței ferestrei afișate, focalizării și navigării JAWS cu text lung sau a salvării printr-un profil de test.
12. **A12 — Trasabilitate și acceptare finală: în lucru.** Acest registru separă starea curentă de istoricul proiectului. Ghidurile și probele rămase se revizuiesc fără a echivala compilarea cu acceptarea practică; punctul se închide numai după verificările restante și o rulare completă sigură a suitelor.

## Limite comune ale dovezilor

- Ultima verificare a celorlalte suite este cea din 30 septembrie; repararea mecanismului `WorkflowSmoke` din 1 octombrie a fost verificată separat. Niciun rezultat automat nu certifică anunțurile JAWS sau sincronizarea reală între două calculatoare.
- Runnerul Full implicit începe cu o compilare Release în locația standard. Copia actuală este autonomă și verificată; nu o suprascriem cu un build obișnuit doar pentru a rula suitele. Înaintea următorului Full trebuie stabilit un flux care păstrează executabilul și dependențele lui.
- Indexul online de vulnerabilități NuGet a rămas inaccesibil din mediul de verificare din cauza negocierii TLS. Rezultatele testelor locale nu constituie un audit de vulnerabilități.
- Copiile de siguranță exclud explicit cheile API și sesiunea NewsBlur. Raportul de eroare, în schimb, scrie textul excepției; nu există dovadă că orice detaliu sensibil este eliminat din acesta. Ghidurile avertizează utilizatorul să îl verifice înainte de partajare. Eventuala întărire a filtrării raportului cere o intervenție separată în cod și teste.
- Publicarea pe GitHub, un installer nou și un release nou nu fac parte din această etapă.
