# Istoricul modificărilor

Toate modificările importante ale Orizont RSS sunt documentate în acest fișier.

## 1.5.3 — 2026-09-05

- adăugate alerte sonore configurabile pentru finalizarea actualizării, articole noi și erori de feed, cu buton de test și limitarea repetării fără articole noi;
- meniul contextual al conversației Gemini include citirea vocală, copierea, distribuirea și focalizarea pe întrebarea următoare;
- bara de stare a conversației AI emite evenimente live pentru cititoarele de ecran;
- documentația și verificările pentru toate cele șase limbi au fost actualizate.

## 1.5.2 — 2026-08-27

- restaurată comanda distinctă „Deschide articolul în modul Citire al browserului” în meniurile contextuale ale articolelor și ale conținutului;
- comanda folosește Microsoft Edge Immersive Reader prin prefixul `read:` și revine sigur la browserul implicit dacă Edge nu este instalat;
- păstrate separat cititorul integrat Orizont și pagina originală în browser.

## 1.5.1 — 2026-08-27

- consolidare a căutării locale într-o componentă testabilă, cu suport pentru mai multe cuvinte și diacritice;
- consolidare a regulilor de retenție, cu protejarea explicită a articolelor Favorite și De citit mai târziu;
- backupul păstrează acum și setările ferestrei de citire, fără cheia Gemini;
- test de fum offline pentru 1.200 de articole, retenție, duplicate, backup și căutare;
- verificări de compilare, localizare, ghiduri și distribuție reunite pentru publicarea reproductibilă.

## 1.5.0 — 2026-08-24

- titlul ferestrei principale, istoricul stării și diagnosticele preiau automat versiunea din executabil;
- comenzile `Ctrl+Shift+1` pentru selectorul de foldere și `Ctrl+Shift+2` pentru articolele combinate ale folderului selectat;
- revenirea directă din vederea unui singur feed la articolele combinate ale folderului prin `Ctrl+Shift+2`;
- tratarea accesibilă a situației în care selectorul de foldere este indisponibil din cauza filtrului „Feeduri care necesită atenție”;
- noile comenzi documentate în ajutorul `F1` și în ghidurile celor cinci limbi.

### Funcții incluse în 1.5.0

- vederea unui folder combină într-o singură listă cronologică articolele tuturor feedurilor sale;
- fiecare articol din vederea combinată anunță sursa prin cititorul de ecran și o afișează în detaliile vizuale;
- căutarea în articole include acum și numele sursei;
- bara de stare a folderului anunță numărul feedurilor, numărul articolelor și ordonarea cronologică;
- integrarea Gemini TTS ca al treilea motor vocal, online, pentru articole și conversații Gemini;
- alegerea uneia dintre cele 30 de voci Gemini și folosirea cheii API salvate local;
- avertizare explicită că textul citit este trimis la Google și poate consuma cota sau creditele API;
- mesaje distincte pentru cheie respinsă, cotă, conexiune, timp de așteptare și răspuns audio invalid;
- deschiderea articolelor direct în modul Citire Microsoft Edge, fără reclame, bare laterale și navigare inutilă pe paginile compatibile;
- păstrarea separată a comenzii pentru pagina originală în browserul implicit;
- Orizont păstrează un singur motor vocal activ și eliberează complet SAPI5 când se alege eSpeak, respectiv eSpeak când se alege SAPI5;
- verificare obligatorie a fișierelor WPF și eSpeak înainte ca o distribuție să poată fi considerată completă;
- integrarea locală a eSpeak NG 1.52 ca motor vocal inclus în distribuție, fără instalare separată;
- alegerea accesibilă între SAPI5 și eSpeak NG în Setări, cu liste distincte de voci;
- reglarea vitezei, volumului și, pentru eSpeak, a înălțimii vocii;
- folosirea motorului ales atât pentru articole, cât și pentru conversațiile Gemini;
- aceleași comenzi `Ctrl+Alt+V`, `Ctrl+Alt+P` și `Ctrl+Alt+S` pentru ambele motoare;
- 132 de voci eSpeak de bază detectate în testul automat, inclusiv limba română; vocile MBROLA neincluse sunt filtrate;
- licențele și proveniența eSpeak NG incluse în distribuție;
- interfața și stările eSpeak localizate în română, engleză, spaniolă, franceză și germană.

### Citire vocală și conversații Gemini

- citirea vocală SAPI5 a selecției sau a întregii conversații Gemini cu `Ctrl+Alt+V`;
- citirea conversației Gemini de la poziția cursorului din meniul contextual;
- pauză sau continuare cu `Ctrl+Alt+P` și oprire cu `Ctrl+Alt+S` direct în fereastra răspunsului;
- butoane accesibile pentru citire, pauză și oprire în fereastra Gemini;
- stare vocală live în fereastra conversației și oprirea sigură a citirii la închiderea acesteia;
- reutilizarea motorului SAPI5 și a setărilor de voce configurate pentru articole;
- citirea SAPI5 a răspunsurilor și conversațiilor Gemini, verificată manual cu succes;
- direcție vocală stabilită pentru 1.5 și versiunile următoare: SAPI5, eSpeak NG și vocile Gemini; Piper nu va fi integrat;
- corectarea instrumentului de traducere pentru protejarea separată a termenilor „Orizont” și „orizont”.

## 1.4.0 — 2026-08-17

- fereastra de ajutor F1 este localizată înainte de afișare, evitând anunțarea succesivă a titlului românesc și a celui tradus de către cititorul de ecran;
- infrastructură de localizare bazată pe resurse .NET;
- selector pentru română, engleză, spaniolă, franceză și germană;
- detectarea automată a limbii Windows la instalările noi;
- păstrarea limbii române pentru setările create înainte de 1.4;
- revenire sigură la textul românesc când o traducere lipsește;
- localizarea controalelor, barei de stare, dialogurilor, ajutorului F1, setărilor, citirii SAPI5, conversației Gemini și instrumentelor de gestionare a feedurilor;
- răspunsurile standard Gemini cer acum limba interfeței, nu limba română în mod obligatoriu;
- valori interne stabile pentru filtrele de articole, perioade și foldere, independente de textul tradus afișat;
- formatarea regională a datelor în articole, notițe AI, diagnostice și informațiile despre versiune;
- 676 de resurse complete pentru fiecare limbă suplimentară;
- verificare automată pentru chei lipsă, valori goale, argumente de format, termeni tehnici, extensii de fișiere și marcaje interne;
- test de fum multilingv pentru texte statice, formate dinamice și denumiri accesibile;
- ghiduri HTML în română, engleză, spaniolă, franceză și germană, selectate automat după limba interfeței;
- revenire la ghidul român dacă fișierul tradus nu este disponibil;
- fără modificarea comenzilor de tastatură sau a ordinii de focalizare.

## 1.3.0 — 2026-08-16

Prima versiune stabilă și open-source.

### Funcții principale

- interfață WPF cu trei panouri și navigare completă din tastatură;
- gestionarea feedurilor, folderelor și articolelor;
- import și export OPML;
- descoperirea feedurilor din pagini web și din catalogul local;
- filtrare avansată, favorite, „Mai târziu” și etichete reutilizabile;
- listă pentru feedurile care necesită atenție și operații asupra selecției;
- prevenirea feedurilor duplicate și curățarea duplicatelor existente;
- reguli de păstrare a articolelor și prevenirea reapariției articolelor vechi curățate;
- integrare Gemini pentru rezumare, traducere, întrebări și conversații despre articol;
- copierea, salvarea, exportul și partajarea răspunsurilor AI;
- backup și restaurare;
- ajutor `F1`, ghid HTML accesibil și istoric al stării;
- citire locală SAPI5 cu pornire, pauză, continuare și oprire;
- recuperarea automată a motorului SAPI5 după repaus, suspendare sau schimbarea dispozitivului audio;
- păstrarea selecției și a focalizării la revenirea din conținut în lista de articole;
- închidere sigură în timpul actualizării și salvare serializată a datelor.

### Publicare

- proiect declarat sub `GPL-3.0-or-later`;
- documentație pentru compilare și contribuții;
- atribuirea inițiatorului și recunoașterea colaborării cu OpenAI Codex;
- distribuție Windows x64 și arhivă separată cu sursa corespunzătoare.
