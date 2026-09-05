# Contribuții la Orizont RSS

Îți mulțumim pentru interesul acordat proiectului Orizont RSS.

## Principiul proiectului

Accesibilitatea este o cerință de bază, nu o funcție opțională. O modificare nu trebuie să reducă utilizarea cu cititoare de ecran, controlul din tastatură, claritatea mesajelor de stare sau restaurarea focalizării.

## Înainte de o modificare

- descrie problema și rezultatul așteptat;
- păstrează compatibilitatea cu Windows x64 și .NET 8;
- nu introduce dependențe externe fără o justificare clară și o verificare a licenței;
- nu include date reale ale utilizatorilor, chei API, feeduri private sau diagnostice cu informații personale;
- păstrează comenzile de tastatură existente, exceptând situațiile în care schimbarea a fost discutată și documentată.

## Verificări de accesibilitate

Pentru modificările de interfață verifică, după caz:

- ordinea logică la `Tab` și `Shift+Tab`;
- accesul la panouri prin `Ctrl+1`, `Ctrl+2`, `Ctrl+3`, `F6` și `Shift+F6`;
- numele și descrierile Automation pentru controale;
- folosirea săgeților în liste și în textele doar în citire;
- anunțurile din bara de stare;
- revenirea la articolul corect după închiderea conținutului;
- absența zonelor goale inutile în navigarea din tastatură;
- lizibilitatea vizuală, contrastul și adaptarea la texte mai lungi.

Testarea cu orice cititor de ecran este deosebit de valoroasă. Sunt binevenite rezultate obținute cu cititoare diferite și cu Naratorul Windows.

## Traduceri

Traducerile trebuie să păstreze sensul funcțional și indicațiile exacte ale comenzilor. Comenzile de tastatură nu se traduc și nu se schimbă între limbi. O traducere nouă trebuie să includă și etichetele accesibile, mesajele de stare, erorile și ajutorul relevant.

## Licența contribuțiilor

Prin trimiterea intenționată a unei contribuții pentru includere în Orizont RSS, confirmi că ai dreptul să o oferi și că aceasta poate fi distribuită în condițiile `GPL-3.0-or-later`, aceeași licență ca proiectul.

Contributorii își păstrează drepturile de autor asupra contribuțiilor lor. Numele lor poate fi adăugat în [CONTRIBUTORS.md](CONTRIBUTORS.md).
