# Orizont RSS 1.6.0 — Productivitate, actualizare automată și accesibilitate

## Noutăți

- Sistem complet de verificare și actualizare automată de pe GitHub Releases: notificare accesibilă pentru JAWS/NVDA, descărcare în fundal cu bară de progres și lansarea automată a noului instalator.
- Descoperire de feeduri după subiecte: catalog local structurat pe categorii combinat cu căutare pe internet.
- Reorganizarea meniului Căutare în trei submeniuri logice: Căutare și filtrare articole, Descoperire feeduri noi și Vizualizări rapide.
- Optimizarea secvenței de citire a articolelor pentru cititoarele de ecran: status (citit/necitit, favorit, mai târziu), titlu, poziție (x din y) și abia apoi detaliile generale.
- River of News: articolele necitite sunt agregate și ordonate cronologic.
- Semnale audio (earcons) accesibile la schimbarea folderului și la capetele listelor Feeduri și Articole.
- Alerte după cuvinte-cheie într-o fereastră accesibilă, cu titlu și cuvântul potrivit, fără detalii tehnice inutile.
- Navigare alfabetică prin apăsarea primei litere în listele Feeduri și Articole.
- Sincronizare NewsBlur bidirecțională robustă la abonare, actualizare și dezabonare feeduri.
- Ghidurile utilizatorului organizate modular în directorul `docs/user-guides/` în toate cele opt limbi.
- Păstrarea integrală a traducerilor, a citirii vocale eSpeak NG și a comenzilor de tastatură.

## Verificări efectuate

- Compilare Release autonomă win-x64 și installer offline fără erori.
- Teste de logică CoreSmoke și sinteză vocală EspeakSmoke trecute cu succes.
- Verificarea ghidurilor utilizatorului în toate cele 8 limbi (100% valide).
- Verificarea integrității fișierelor și absența oricăror erori de codare (0 mojibake).
- Validarea distribuției prin `verify-distribution.ps1` cu 444 fișiere de date eSpeak NG și zero fișiere de depanare sau date locale.

