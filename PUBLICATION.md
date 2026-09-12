# Publicarea Orizont RSS — versiunea 1.5.4

Acest document consemnează pachetele și verificările release-ului. Nu conține chei API,
date ale utilizatorilor sau feeduri personale.

## Starea versiunii

- versiune stabilă descărcabilă: `1.5.3`;
- versiune-sursă pregătită: `1.5.4` (tag publicat; Release-ul cu binarele este în curs);
- platformă: Windows x64;
- pachet binar: `Orizont-RSS-1.5.4-win-x64.zip`;
- installer offline: `OrizontSetup.exe`;
- arhivă sursă: `Orizont-RSS-1.5.4-source.zip`;
- licență: GPL-3.0-or-later;
- limbi: română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană.

## Depozit GitHub public

Depozitul public este `grifnas/OrizontRSS`; sursa, documentația, licența și notificările
terțe sunt publicate acolo. Pagina de prezentare este
<https://grifnas.github.io/OrizontRSS/>.

## Pachetul pregătit pentru Release 1.5.4

După autentificarea proprietarului, Release-ul `v1.5.4` va conține:

- `Orizont-RSS-1.5.4-win-x64.zip` și fișierul său `.sha256`;
- `OrizontSetup.exe` și fișierul său `.sha256`;
- `Orizont-RSS-1.5.4-source.zip` și fișierul său `.sha256`;
- `RELEASE-NOTES-1.5.4.md`.

Pachetul portabil și installerul sunt autonome pentru Windows x64. Cheile API sunt
opționale și furnizate de utilizator; datele aplicației rămân în profilul local Windows.

## GitHub Pages

Pagina din `docs/index.html` este punctul de pornire pentru prezentare și descărcare;
publicarea este configurată din ramura principală și directorul `/docs`. Sunt disponibile
opt limbi și linkul stabil către installerul `OrizontSetup.exe`.

## WinGet

Manifestul pentru `Grifnas.OrizontRSS` a fost trimis prin PR-ul
`microsoft/winget-pkgs#431971`; integrarea în catalog și verificările Microsoft sunt
urmărite separat de acest Release.

## Etapa 5 — Microsoft Store

Microsoft Store rămâne o etapă ulterioară. Pentru WPF este preferat un pachet MSIX,
semnat și verificat, deoarece permite instalare/dezinstalare curată și actualizări
gestionate de Store.

## Checklist înainte de publicare

- [ ] nu există `settings.json`, `feeds.json`, backupuri, jurnale sau chei API în sursă;
- [x] toate cele opt limbi sunt complete și verificate automat;
- [ ] ghidurile HTML și notele de lansare sunt actualizate;
- [ ] testele automate sunt trecute;
- [ ] verificarea manuală cu JAWS/NVDA și testul pe un al doilea calculator rămân de confirmat separat;
- [ ] hash-urile SHA-256 corespund arhivelor publicate;
- [ ] licența GPL și notificările terțe sunt incluse;
- [ ] pagina publică explică cerințele, confidențialitatea și modul de raportare a problemelor.

Verificarea manuală JAWS/NVDA pentru interacțiunile NewsBlur din 1.5.4 și confirmarea
sincronizării pe al doilea calculator rămân de efectuat; nu sunt declarate drept trecute.
Înaintea finalizării Release-ului, API-ul GitHub a confirmat că tagul există, dar
Release-ul și asseturile binare încă nu sunt publicate.
