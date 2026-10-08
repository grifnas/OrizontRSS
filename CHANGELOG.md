# Istoricul modificărilor curente

Acest fișier descrie numai ultima versiune publicată și lucrările ulterioare încă nepublicate. Istoricul mai vechi poate fi recuperat din istoricul Git și din release-urile publice anterioare; nu este copiat în această pagină activă.

## Versiunea publicată v1.6.0 — 19 septembrie 2026

Release-ul public v1.6.0 este disponibil pe [GitHub Releases](https://github.com/grifnas/OrizontRSS/releases/tag/v1.6.0). Noutățile și verificările consemnate la publicare sunt în [notele v1.6.0](RELEASE-NOTES-1.6.0.md).

Funcțiile livrate includ actualizarea automată, descoperirea feedurilor după subiect, reorganizarea meniurilor de căutare, navigarea River of News și alfabetică, semnale sonore pentru liste, alerte după cuvinte-cheie, sincronizare NewsBlur și ghiduri în opt limbi.

## Candidat local v1.6.1 — domeniu definit, acceptare și publicare în așteptare

Domeniul de lucru al candidatului local este descris în [notițele v1.6.1](RELEASE-NOTES-1.6.1.md): furnizori AI suplimentari, scurtături configurabile, accesibilitate și Cititor Orizont, traducere/export/partajare cu proveniență, reguli de autoetichetare și îmbunătățiri NewsBlur. Acesta este un rezumat al schimbărilor de după v1.6.0, nu o confirmare că toate verificările sunt închise. Registrul de acceptare arată încă patru puncte deschise. Versiunea publică rămâne v1.6.0; notele 1.6.1 sunt de lucru și pachete pentru acest candidat nu au fost create.

## Lucrări ulterioare pe ramura main — nepublicate

Următoarele modificări sunt în sursa de lucru de după v1.6.0. Nu sunt incluse în pachetul public v1.6.0 și nu constituie o distribuție nouă:

- selecția furnizorului AI: Gemini, OpenAI, Mistral și DeepSeek;
- configurarea scurtăturilor de către utilizator și reguli de autoetichetare;
- îmbunătățiri de accesibilitate, partajare cu proveniență și sincronizare NewsBlur;
- îmbunătățiri ale modurilor Cititor Orizont, inclusiv WebReader;
- verificări automate și documentare suplimentare.

La cererea utilizatorului, alertele după cuvinte-cheie au fost eliminate din sursa de lucru după publicarea v1.6.0. Ele rămân doar în release-ul public istoric și în notele acelui release; nu sunt funcție a ramurii main curente.

Versiunea fișierului executabil local de test poate rămâne 1.6.0.0, deoarece nu s-a creat un release nou. Acest număr de metadate nu identifică binarul public: executabilul de test este compilat din sursa main ulterioară și nu trebuie prezentat drept pachetul v1.6.0 publicat.
