# Orizont RSS 1.6.1 — notițe de versiune în pregătire

> **Proiect local de release — nepublicat și nefinalizat.** Versiunea publicată rămâne v1.6.0. Aceste note descriu domeniul candidatului local 1.6.1; nu reprezintă anunțul unei lansări și nu confirmă singure acceptarea funcțiilor.

## Domeniul candidatului 1.6.1

Acest candidat cuprinde schimbările aflate în sursa de lucru după v1.6.0, în următoarele categorii:

- **Furnizori AI:** selectarea și configurarea Gemini, OpenAI, Mistral și DeepSeek, inclusiv furnizorul implicit și setări/chei separate.
- **Tastatură și accesibilitate:** configurarea scurtăturilor de către utilizator și ajustări ale navigării, focalizării, meniurilor contextuale și anunțurilor prin bara de stare.
- **Cititor și traduceri:** completări pentru modurile Text și WebReader, traducerea prin Google Translate și DeepL și comenzile disponibile în rezultatele traduse.
- **Salvare și partajare:** export TXT/RTF și copiere/distribuire cu atribuirea Orizont RSS, sursa articolului și, pentru traduceri, furnizorul și limba.
- **Organizarea articolelor:** reguli de autoetichetare și comenzile aferente articolelor.
- **NewsBlur și închiderea aplicației:** îmbunătățiri ale sincronizării și protecțiilor contra dublurilor, precum și confirmarea opririi operațiilor anulabile la închiderea aplicației.
- **Calitate și documentație:** teste automate, verificări de localizare pentru cele opt limbi și documentație actualizată.

Lista fixează domeniul de lucru pentru candidatul local; nu extinde automat acceptarea manuală. Pentru afirmații mai precise despre un anumit comportament se vor păstra numai formulările susținute de verificări și de registrul de acceptare.

## Nu face parte din candidatul curent

- Alertele de articole după cuvinte-cheie au fost eliminate din sursa de lucru la cererea utilizatorului. Ele există numai în istoricul versiunii publice 1.6.0.
- Nu se anunță încă un installer, o arhivă portabilă sau o arhivă de surse pentru 1.6.1; aceste pachete nu au fost create.

## Verificări transferate la versiunea următoare

Registrul de acceptare păstrează **8 din 12 puncte închise local**. La decizia utilizatorului, următoarele verificări sunt transferate în ciclul versiunii următoare: A02 (acceptarea practică NewsBlur și testul pe două calculatoare), A09 (scenariile practice Google Translate), A07 (verificarea practică și revizia lingvistică) și A11 (interfață afișată, focalizare și navigare cu JAWS 2026). Acestea rămân deschise și neverificate; transferul nu înseamnă că au trecut. Eventualele corecții rezultate din probe vor fi evaluate pentru versiunea următoare.

Pentru publicarea 1.6.1 rămân pașii proprii lansării: verificarea faptului că toate cele opt limbi sunt complete și la zi, validarea sursei și a buildului, pregătirea și verificarea pachetelor, apoi actualizarea notelor, paginii și manifestelor. Verificarea automată a catalogului nu este prezentată drept revizie lingvistică umană.

Notele de față rămân draft până la validarea finală a release-ului. Crearea pachetelor și publicarea cer o solicitare expresă separată.
