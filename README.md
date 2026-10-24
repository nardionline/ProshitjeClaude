# Market Prenga (WPF)

Zëvendëson programin Access. Databaza mbetet Access (.mdb).

## Faqet
- **Pasqyra**: shitjet e ditës/muajit, borxhet, stoku i ulët, grafik 7 ditësh, më të shiturat.
- **Shitja** (POS me prekje): barkod, kategori, pllaka artikujsh, +/− sasia, kusuri, pagesa e shpejtë,
  shporta në pritje, printim AED.
- **Faturat**: lista sipas datës, anulim (kthen stokun dhe borxhin).
- **Artikuj, Klientë, Furnitorë**: shtim, ndryshim, fshirje. Te Klientët: pagesë borxhi.
- **Raporte**: xhiro ditore, artikujt më të shitur, sipas përdoruesit, eksport CSV.

## Shortcut-e te Shitja
Enter = shto barkodin · F5 = ruaj + AED · F6 = ruaj pa printuar · F7 = në pritje · F8 = rikthe · Esc = pastro

## Ndërtimi
Push në `main` nis GitHub Actions (**Build exe**). Shkarko artifact-in `MarketPrenga-win-x86`.
Lokalisht:

    dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish

## Konfigurimi (`appsettings.json`)
DatabasePath (.mdb me të dhënat), AedOutputFile, DecimalSeparator, LowStockThreshold.
Programi është 32-bit (x86) sepse Jet 4.0 për .mdb punon vetëm në 32-bit. Bëj kopje të .mdb para provës.
