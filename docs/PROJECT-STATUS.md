# Orizont RSS — stare și plan de proiect

Ultima actualizare: 5 septembrie 2026

## Scopul documentului

Acest document păstrează separat ceea ce este deja realizat, ceea ce este în lucru și ceea ce este doar planificat. O idee planificată nu este considerată o cerere de implementare până când utilizatorul nu o confirmă explicit.

## Realizat

- Orizont RSS pentru Windows este la versiunea stabilizată 1.5.3.
- Sunt implementate feedurile RSS, folderele, actualizarea, filtrele, favoritele, lista „Mai târziu”, etichetele și curățarea duplicatelor.
- Sunt implementate cititorul integrat accesibil, citirea vocală, căutarea, copierea, partajarea, backupul și restaurarea.
- Sunt integrate funcțiile Gemini și traducerea DeepL, cu activare explicită de către utilizator.
- Interfața este disponibilă în română, engleză, spaniolă, franceză, germană și portugheză.
- Regula de retenție pentru „Citește acum” este stabilită: articolele obișnuite respectă perioada configurată, iar favoritele și articolele „Mai târziu” rămân până la ștergerea manuală.
- Alertele sonore pentru actualizarea feedurilor sunt disponibile în Setări aplicație, cu opțiuni separate pentru finalizare reușită, articole noi și erori, plus buton de test și limitare a repetării fără articole noi.
- Fereastra conversației AI are un meniu contextual accesibil, grupat pentru citire vocală, copiere, distribuire și continuarea conversației.
- Bara de stare a conversației AI emite acum evenimente live pentru cititoarele de ecran, inclusiv la schimbarea stării citirii și la maximizare/restaurare.
- Distribuția 1.5.3 pentru Windows x64 și arhiva sursă au fost generate și verificate la cererea expresă a utilizatorului.
- Verificările de compilare, localizare, voce și distribuție pentru versiunea stabilizată au fost finalizate anterior.

## În lucru acum

- Pregătirea pentru publicare publică a început: sunt pregătite planul de publicare, pagina GitHub Pages, formularele de feedback și fluxul CI; publicarea efectivă a depozitului așteaptă contul și adresa finală GitHub.
- Verificarea manuală raportată de utilizator pentru bara de stare și facilitățile Orizont este în regulă; etapa de consolidare și distribuția 1.5.3 sunt închise.
- Experimentul Android `OrizontRSSAndroid` este separat de proiectul Windows și este pus pe pauză.
- Feedul Bistrițeanul a fost validat prin XML-ul primit, dar prototipul Android a primit 404. Nu se mai fac încercări speculative până la o decizie nouă și o metodă de diagnostic adecvată.

## Următorii pași propuși

1. Menținerea și verificarea proiectului Windows fără a schimba funcții stabile.
2. Rezolvarea numai a problemelor sau îmbunătățirilor cerute explicit.
3. Pentru orice modificare de accesibilitate, păstrarea navigării native cu tastatura și verificarea țintită cu cititoare de ecran.
4. Actualizarea acestui document și a `WORKLOG.md` după fiecare intervenție efectivă.
5. Crearea unei distribuții noi numai la cererea expresă a utilizatorului.

## Idei pentru mai târziu

- îmbunătățiri ale cititorului și ale navigării accesibile;
- extinderea testelor automate pentru colecții mari de articole;
- eventuală reluare a proiectului Android, numai după stabilirea unei strategii tehnice verificabile;
- funcții noi pentru partajare, traducere și conversații AI, dacă sunt solicitate.

Ideile din această secțiune nu autorizează implementarea și nu schimbă funcționalitatea existentă.

## Regula de interpretare

La reluarea proiectului, se continuă de aici. Nu se reiau experimente abandonate și nu se transformă o idee în implementare fără confirmarea utilizatorului.
