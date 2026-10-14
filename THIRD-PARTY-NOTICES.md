# Componente și servicii terțe

Orizont RSS este distribuit sub `GPL-3.0-or-later`. Aplicația folosește sau poate interacționa cu următoarele tehnologii externe.

## .NET 8 și Windows Presentation Foundation

Orizont RSS este construit cu .NET 8 și WPF. Distribuția autonomă conține fișiere ale .NET Runtime publicate de Microsoft. Textele de licență și notificările terțe furnizate cu runtime-ul folosit la publicare sunt incluse în directorul `Licenses/dotnet`.

- Proiect: https://github.com/dotnet/runtime
- Licență principală: MIT

## Microsoft Speech API 5

Citirea vocală din Orizont RSS 1.3 folosește interfața SAPI5 disponibilă în Windows și vocile înregistrate de utilizator în sistem. Orizont RSS nu redistribuie o voce SAPI5 și nu revendică drepturi asupra vocilor instalate.

## eSpeak NG 1.52

Orizont RSS include biblioteca `libespeak-ng.dll` și datele vocilor eSpeak NG 1.52, extrase din distribuția oficială Windows x64. eSpeak NG este software liber distribuit în principal sub GNU GPL versiunea 3 sau ulterioară; anumite date au licențe compatibile suplimentare. Textele complete sunt incluse în `Licenses/eSpeakNG`.

- Proiect: https://github.com/espeak-ng/espeak-ng
- Versiune inclusă: 1.52.0
- Licență principală: GPL-3.0-or-later
- Orizont RSS nu include și nu folosește executabilul de linie de comandă eSpeak NG.

## Microsoft Edge WebView2 SDK 1.0.2903.40

Modul de citire WebReader folosește controlul Microsoft Edge WebView2. Orizont RSS include bibliotecile SDK-ului WebView2 (`Microsoft.Web.WebView2.Core.dll`, `Microsoft.Web.WebView2.Wpf.dll`, `Microsoft.Web.WebView2.WinForms.dll`) și `WebView2Loader.dll`. Motorul WebView2 Runtime nu este inclus: aplicația folosește runtime-ul instalat în Windows. Textul licenței și notificările terțe ale pachetului sunt incluse în `Licenses/WebView2`.

- Proiect: https://aka.ms/webview
- Versiune inclusă: 1.0.2903.40
- Licență: licența Microsoft pentru WebView2 SDK (tip BSD), din `Licenses/WebView2/LICENSE.txt`
- Copyright (C) Microsoft Corporation

## SmartReader 0.9.6

Pentru textul integral al articolelor, Orizont RSS folosește biblioteca SmartReader, o adaptare .NET a algoritmului Readability de la Mozilla. Biblioteca alege conținutul articolului dintr-o pagină web, fără meniuri și liste laterale. Textul licenței este inclus în `Licenses/SmartReader`.

- Proiect: https://github.com/strumenta/SmartReader
- Versiune inclusă: 0.9.6
- Licență: Apache-2.0
- Copyright: Strumenta

## AngleSharp 1.1.2

AngleSharp este analizorul HTML folosit de SmartReader. Textul licenței este inclus în `Licenses/AngleSharp`.

- Proiect: https://github.com/AngleSharp/AngleSharp
- Versiune inclusă: 1.1.2
- Licență: MIT
- Copyright (c) 2013 - 2024 AngleSharp

## Google Gemini API

Funcțiile AI sunt opționale și folosesc serviciul Google Gemini prin cheia furnizată de utilizator. Gemini este un serviciu online separat și nu este inclus în codul sau în licența Orizont RSS. Utilizarea sa este supusă condițiilor și limitelor stabilite de Google.

## Căutarea Feedly

Butonul „Caută pe internet” din căutarea după subiect folosește căutarea publică de feeduri a serviciului Feedly. Cuvintele căutate sunt trimise la Feedly numai când utilizatorul apasă butonul, iar feedurile găsite sunt verificate de Orizont RSS înainte de afișare. Feedly este un serviciu online separat, nu este inclus în codul sau în licența Orizont RSS, iar utilizarea sa este supusă condițiilor stabilite de Feedly.

## OpenAI Codex

OpenAI Codex a fost utilizat ca instrument de asistență pentru proiectarea, dezvoltarea, verificarea și documentarea aplicației. Codex nu este inclus ca o componentă de execuție a Orizont RSS.

## Pictograma Orizont RSS

Pictograma Orizont RSS din directorul `Assets` este un element original creat pentru proiect și este distribuită ca parte a Orizont RSS în condițiile licenței proiectului.
