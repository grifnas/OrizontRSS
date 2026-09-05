# Compilarea Orizont RSS

## Cerințe

- Windows 10 sau Windows 11 pe 64 de biți;
- .NET 8 SDK;
- PowerShell sau un terminal echivalent.

Proiectul nu declară pachete NuGet externe. Folosește WPF din .NET 8 și, în timpul execuției, poate apela interfața SAPI5 disponibilă în Windows.

## Compilare pentru dezvoltare

Din directorul sursei rulează:

```powershell
dotnet restore CititorRSS.Jaws.csproj
dotnet build CititorRSS.Jaws.csproj -c Release
```

## Publicare Windows x64

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
- funcționarea comenzilor `Ctrl+1`, `Ctrl+2`, `Ctrl+3`, `F6`, `F1`, `Delete`, `F9`, `Escape`, `Shift+F9`, `F11` și a alternativelor `Ctrl+Alt+V`, `Ctrl+Alt+P`, `Ctrl+Alt+S`;
- calcularea și publicarea sumei SHA-256 pentru fiecare arhivă.

Validarea completă, inclusiv testele automate pentru logică, 1.200 de articole, localizare, eSpeak NG, ghiduri și distribuție, se rulează astfel:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-all.ps1 -DistributionPath .\bin\Release\final-win-x64
```

Comanda verifică distribuția pentru versiunea 1.5.3.

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

Distribuția trebuie să conțină ghidul român și fișierele `.en.html`, `.es.html`, `.fr.html`, `.de.html` și `.pt.html`. Aplicația deschide ghidul corespunzător limbii interfeței și revine la cel român dacă traducerea lipsește.

În timpul dezvoltării, o traducere lipsă revine la textul românesc de bază; interfața nu afișează etichete goale.
