# Plan de publicare — Orizont RSS

Acest document descrie pregătirea pentru publicarea proiectului. Nu conține chei API,
date ale utilizatorilor sau feeduri personale.

## Starea versiunii

- versiune stabilă: `1.5.3`;
- platformă: Windows x64;
- pachet binar: `Orizont-RSS-1.5.3-win-x64.zip`;
- arhivă sursă: `Orizont-RSS-1.5.3-source.zip`;
- licență: GPL-3.0-or-later;
- limbi: română, engleză, spaniolă, franceză, germană și portugheză.

## Etapa 1 — depozit GitHub public

1. Creează sau alege contul GitHub care va fi autorul public al proiectului.
2. Creează un depozit public, de exemplu `orizont-rss`.
3. Linkurile publice folosesc depozitul real `grifnas/OrizontRSS`.
4. Publică sursa, documentația, licența și notificările terțe.
5. Activează Issues și Discussions numai dacă dorești să primești feedback direct în GitHub.
6. Adaugă topicurile: `rss`, `accessibility`, `screen-reader`, `jaws`, `nvda`, `windows`, `wpf`, `romanian`.

## Etapa 2 — primul Release

Creează tagul `v1.5.3` și atașează la Release următoarele fișiere:

- `Orizont-RSS-1.5.3-win-x64.zip`;
- `Orizont-RSS-1.5.3-win-x64.zip.sha256`;
- `Orizont-RSS-1.5.3-source.zip`;
- `Orizont-RSS-1.5.3-source.zip.sha256`;
- `RELEASE-NOTES-1.5.3.md`.

Descrierea Release-ului trebuie să precizeze că versiunea este autonomă pentru Windows
x64, că utilizatorul își furnizează propriile chei API și că datele aplicației rămân în
profilul local Windows.

## Etapa 3 — GitHub Pages

Pagina din `docs/index.html` este un punct de pornire accesibil pentru prezentare și
descărcare. În setările depozitului se selectează publicarea din ramura principală,
directorul `/docs`.

Înainte de activare trebuie completate:

- numele contului și al depozitului în linkurile de descărcare;
- adresa de contact pentru probleme;
- eventualele capturi de ecran și o demonstrație audio, dacă sunt disponibile.

## Etapa 4 — WinGet

După ce Release-ul are URL stabil și instalarea a fost verificată pe un Windows curat,
se pregătește manifestul pentru depozitul `microsoft/winget-pkgs`. Identificatorul
publisherului și al pachetului se aleg numai după stabilirea contului GitHub și a
identității publice a aplicației.

## Etapa 5 — Microsoft Store

Microsoft Store rămâne o etapă ulterioară. Pentru WPF este preferat un pachet MSIX,
semnat și verificat, deoarece permite instalare/dezinstalare curată și actualizări
gestionate de Store.

## Checklist înainte de publicare

- [ ] nu există `settings.json`, `feeds.json`, backupuri, jurnale sau chei API în sursă;
- [ ] toate cele șase limbi sunt complete și verificate;
- [ ] ghidurile HTML și notele de lansare sunt actualizate;
- [ ] testele automate sunt trecute;
- [ ] executabilul a fost testat manual cu JAWS și NVDA;
- [ ] hash-urile SHA-256 corespund arhivelor publicate;
- [ ] licența GPL și notificările terțe sunt incluse;
- [ ] pagina publică explică cerințele, confidențialitatea și modul de raportare a problemelor.

Publicarea externă necesită autentificarea proprietarului contului GitHub și confirmarea
adresei finale a depozitului.
