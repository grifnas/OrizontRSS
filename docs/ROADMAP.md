# Foaia de parcurs Orizont RSS

## Situația curentă

Versiunea 1.5.3 este o versiune stabilizată. Funcțiile de bază pentru feeduri, articole, organizare, citire accesibilă, AI Gemini, traducere DeepL, voce, alerte sonore, meniu contextual AI, backup și localizare sunt implementate.

Localizarea este disponibilă în română, engleză, spaniolă, franceză, germană, portugheză, maghiară și italiană. Verificarea automată curentă confirmă resurse complete și fără erori pentru toate cele opt limbi.

Distribuția 1.5.3 Windows x64 și arhiva sursă au fost verificate și sunt disponibile.

## Workflow obligatoriu pentru orice versiune nouă

Pentru ca instalatorul și versiunea portabilă să conțină aceleași modificări, se folosește aceeași stare a sursei și aceeași versiune:

1. Modificarea codului și actualizarea documentației, limbilor și `WORKLOG.md`.
2. Build, smoke tests și verificări manuale țintite cu JAWS/NVDA.
3. Creșterea versiunii (de exemplu `1.5.3` → `1.5.4`).
4. Construirea arhivei portabile Windows x64.
5. Construirea instalatorului autonom din aceeași stare a sursei.
6. Actualizarea în instalator a versiunii, adresei arhivei și hash-ului SHA-256.
7. Încărcarea în același release GitHub a arhivei portabile și a `OrizontSetup.exe`.
8. Actualizarea paginii GitHub Pages și verificarea linkurilor publice.

Nu se publică un singur pachet izolat. O nouă distribuție se creează numai la cererea expresă și după verificarea tuturor limbilor.

## Următoarea etapă

1. Publicarea modificărilor SEO pe ramura GitHub Pages și verificarea adreselor publice `sitemap.xml` și `robots.txt`.
2. Verificarea manuală cu JAWS/NVDA a comenzilor pentru exemplele RSS: adăugare locală, verificare online, focalizare pe primul articol și eliminare cu confirmare.
3. Verificarea instalatorului public descărcat direct de pe GitHub, inclusiv limbă, focus, bară de stare, pictogramă desktop, instalare și dezinstalare.
4. Retestarea WinGet prin instalatorul public, inclusiv instalare și dezinstalare în regim non-administrator.
5. Trimiterea manifestului WinGet numai după aceste verificări și confirmarea identității publice.
6. Promovarea paginii publice și colectarea feedbackului inițial.
7. Menținerea proiectului Windows fără schimbarea funcțiilor stabile; orice modificare nouă rămâne supusă regulilor din `AGENTS.md`.

## Cerință de securitate GitHub — acțiune necesară

- Contul GitHub `grifnas` trebuie să activeze autentificarea în doi pași (2FA) până la **21 octombrie 2026, ora 00:00 UTC**.
- Configurarea se face din pagina oficială: <https://github.com/settings/two_factor_authentication/setup/intro>.
- Se recomandă o aplicație de autentificare sau un passkey; codurile de recuperare trebuie salvate într-un loc sigur.
- Cerința protejează accesul la GitHub și nu necesită nicio modificare în aplicația Orizont RSS, în release-uri sau în GitHub Pages.
- Până la confirmarea activării, starea contului va fi verificată periodic și utilizatorului i se va reaminti această acțiune.

## Idei pentru etape ulterioare

- furnizor de traducere alternativ, fără cheie, doar dacă poate fi folosit legal și stabil;
- îmbunătățiri suplimentare pentru partajare și conversațiile AI;
- extinderea testelor automate pentru scenarii cu colecții mari de articole.

Aceste idei nu sunt angajamente de implementare și nu modifică funcționalitatea curentă.

## Capturi pentru pagina publică

Galeria de previzualizări este activă în toate cele opt pagini GitHub Pages. Cele patru imagini PNG din `docs/assets/screenshots/` sunt generate offline din componente WPF și folosesc exclusiv date demonstrative; nu conțin fluxuri personale sau chei API. Textul alternativ și descrierile sunt localizate pentru fiecare limbă.
