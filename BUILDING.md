# Compilarea Orizont RSS

## Cerințe

- Windows 10 sau Windows 11 pe 64 de biți;
- .NET 8 SDK;
- PowerShell sau un terminal echivalent.

Proiectul folosește WPF din .NET 8, pachetul NuGet Microsoft.Web.WebView2 și, în timpul execuției, poate apela interfața SAPI5 disponibilă în Windows.

## Compilare pentru dezvoltare

Din directorul sursei rulează:

```powershell
dotnet restore CititorRSS.Jaws.csproj
dotnet build CititorRSS.Jaws.csproj -c Release
```

## Publicare Windows x64

### Executabilul local de lucru

Locația permanentă este `bin\Release\net8.0-windows10.0.17763.0\Orizont.exe`. După curățarea din 30 septembrie 2026 conține copia autonomă, cu .NET inclus. Păstrează toate fișierele din acest folder împreună.

Un `dotnet build` sau un test care compilează aplicația ca referință poate înlocui această copie cu un build framework-dependent. Dacă ai compilat chiar în sursa principală, înainte de predarea executabilului refă copia autonomă locală:

```powershell
dotnet publish CititorRSS.Jaws.csproj -c Release -r win-x64 -p:SelfContained=true -p:PublishSelfContained=true -p:AppendRuntimeIdentifierToOutputPath=false -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false --no-restore -o bin\Release\net8.0-windows10.0.17763.0
```

Pentru această copie standard sunt necesare proprietățile explicite de mai sus. Fără `-p:AppendRuntimeIdentifierToOutputPath=false`, buildul obișnuit și publicarea cu RID pot concura asupra aceluiași director, iar un `Orizont.runtimeconfig.json` framework-dependent, mai nou, poate rămâne în rădăcină.

Înainte de a preda executabilul, verifică `Orizont.runtimeconfig.json`: trebuie să conțină `includedFrameworks` cu versiunile runtime incluse, nu numai `frameworks` care cer runtime instalat separat.

Verifică runtimeconfig (`includedFrameworks`), conținutul folderului și pornirea; compilarea singură nu validează pornirea sau JAWS. Nu redirecționa `OutputPath`, `OutDir` sau `--output` ale instalatorului ori ale unei soluții întregi spre folderul aplicației. Instalatorul și proiectele de test folosesc directoarele lor proprii. Fișierele intermediare eliminate la curățare se regenerează la restore/build; prima verificare ulterioară necesită restore, nu `-NoRestore`.

Înainte de publicare este necesară cererea expresă a utilizatorului. Pentru verificarea obișnuită a sursei, fără pachete de distribuție, folosește comenzile din secțiunea următoare.

## Verificare comună a sursei, fără distribuție

**Atenție:** următoarea comandă compilează în checkoutul curent. Dacă acesta conține copia autonomă păstrată a aplicației, folosește mai întâi procedura cu worktree separat de mai jos.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify-source.ps1 -Mode Full
```

Aceasta este și comanda din CI: compilează aplicația și instalatorul, rulează succesiv CoreSmoke, WorkflowSmoke, RulesSmoke, ShortcutsSmoke, AiProviderSmoke (proiectul OpenAiSmoke), LocalizationSmoke, EspeakSmoke și ScaleSmoke, apoi verifică localizările, ghidurile, codarea textelor, paginile locale și comportamentul orchestratorului la eșec. Se oprește la prima eroare. Nu creează o distribuție și nu contactează conturile personale pentru teste.

**Protejarea executabilului autonom local:** comanda compilează în directorul din care este rulată. Dacă în sursa principală există deja copia autonomă din `bin\Release`, rulează Full într-un checkout Git separat, nu peste acea copie. La 1 octombrie 2026, acest flux a trecut integral pornind de la `checkpoint-local-2026-10-01-a12`; executabilul principal și-a păstrat suma SHA-256. Din sursa principală, după verificarea că folderul de test nu există deja:

```powershell
git worktree add --detach '..\Orizont RSS-verification-full' HEAD
Push-Location '..\Orizont RSS-verification-full'
powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify-source.ps1 -Mode Full
Pop-Location
```

Verifică rezultatul, statusul Git al checkoutului de test și suma executabilului principal înainte de curățare. Elimină numai checkoutul de test identificat exact, prin `git worktree remove`, după ce te asiguri că nu conține modificări sau date de păstrat. În terminalul izolat al agentului, conexiunile HTTPS prin Windows Schannel pot eșua cu `SEC_E_NO_CREDENTIALS`, inclusiv către NuGet; în afara acelei izolări, accesul a fost confirmat la 1 octombrie 2026. Pentru o verificare Full care include și auditul, rulează în checkoutul separat, dar într-un mediu de comandă cu acces HTTPS normal și fără `NuGetAudit=false`. Nu dezactiva verificarea certificatelor și nu modifica setările TLS globale pentru această eroare a izolării.

Verificarea punctuală a pachetelor aplicației, fără recompilare, se poate repeta astfel:

```powershell
dotnet restore CititorRSS.Jaws.csproj --force-evaluate -p:NuGetAudit=true
dotnet list CititorRSS.Jaws.csproj package --vulnerable --include-transitive
```

La 1 octombrie 2026, ambele comenzi au reușit în afara izolării și a doua nu a raportat pachete cu vulnerabilități cunoscute în sursele curente. Acesta este un rezultat datat, nu o garanție permanentă; avertismentul `NU1900` dintr-o rulare izolată înseamnă în continuare că acea rulare nu a obținut datele de audit. Vezi [explicația oficială pentru NU1900](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1900) și [documentația auditului NuGet](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages).

`-Mode Quick` omite compilarea instalatorului, benchmarkul ScaleSmoke și verificarea paginilor publice; nu reprezintă validare completă. `-ListOnly` afișează pașii fără execuție. `-NoRestore` se folosește numai când dependențele tuturor proiectelor sunt deja restaurate. NU1900 înseamnă că auditul vulnerabilităților NuGet nu a obținut date, chiar dacă testele trec.

WorkflowSmoke execută comenzile reale din MainWindow cu operațiile externe substituite: nu deschide ferestre, nu citește profilul și nu trimite cereri online. Include schimbarea selecției în timpul operațiilor și matricea bifelor de pornire. Nu înlocuiește verificarea practică JAWS/NVDA sau sincronizarea reală pe două calculatoare.

## Publicare Windows x64 — comandă

Pentru o distribuție autonomă, care include .NET Runtime:

```powershell
dotnet publish CititorRSS.Jaws.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o publish
```

Executabilul rezultat este `publish\Orizont.exe`.

## Date locale

Aplicația nu scrie feedurile și setările în directorul sursei sau al executabilului. Datele utilizatorului sunt păstrate în:

```text
%LOCALAPPDATA%\CititorRSS-JAWS
```

Acest director nu trebuie inclus într-o distribuție sau într-un raport public de problemă. Nu publica niciodată fișierul `settings.json`, deoarece poate conține cheia Gemini protejată pentru contul Windows curent.

## Verificarea unei distribuții

Înainte de publicare verifică:

- compilare cu zero erori și zero avertismente;
- absența fișierelor `*.pdb`, `settings.json`, `feeds.json`, backupurilor și diagnosticelor;
- prezența ghidului HTML, a licenței și a notificărilor pentru componente terțe;
- funcționarea comenzilor `Ctrl+1`, `Ctrl+2`, `Ctrl+3`, `F6`, `F1`, `Delete`, `F9`, `Escape`, `Shift+F9` și `F11`;
- calcularea și publicarea sumei SHA-256 pentru fiecare arhivă.

Validarea completă, inclusiv testele automate pentru logică, 1.200 de articole, localizare, eSpeak NG, ghiduri și distribuție, se rulează astfel:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-all.ps1 -DistributionPath .\bin\Release\final-1.6.1-win-x64
```

Comanda folosește candidatul indicat de `docs\RELEASE-STATUS.json` (în prezent 1.6.1); pentru altă versiune se transmite și `-ExpectedProductVersion`.

Pentru installerul offline local, construiește mai întâi arhiva portabilă și fișierul
`.sha256`, apoi publică proiectul `packaging\installer\OrizontSetup.csproj` cu
proprietățile `OfflinePackageZip` și `OfflinePackageHash` setate la căile acestor
fișiere. Ambele sunt incluse în installer, iar hash-ul este verificat înainte de
extragere. Un installer 1.6.0 fără pachetul inclus se oprește; nu descarcă accidental
o versiune publică anterioară.

## Verificarea localizării

Pentru un raport al resurselor traduse și lipsă:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-localization.ps1
```

Pentru a considera orice traducere lipsă drept eroare:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-localization.ps1 -RequireComplete
```

Testul de fum pentru resurse și denumirile accesibile dinamice se rulează fără a deschide interfața:

```powershell
dotnet run --project tests\LocalizationSmoke\LocalizationSmoke.csproj -c Release
```

Structura ghidurilor HTML, legăturile interne și scurtăturile protejate se verifică prin:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-user-guides.ps1
```

Distribuția trebuie să conțină ghidul român și fișierele `.en.html`, `.es.html`, `.fr.html`, `.de.html`, `.pt.html`, `.hu.html` și `.it.html`. Aplicația deschide ghidul corespunzător limbii interfeței și revine la cel român dacă traducerea lipsește.

În timpul dezvoltării, o traducere lipsă revine la textul românesc de bază; interfața nu afișează etichete goale.
