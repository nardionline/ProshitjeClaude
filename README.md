# Market Prenga (WPF) — Faza 1

Zëvendëson ekranin e shitjes, login-in, Artikujt dhe Klientët. Databaza mbetet Access (.mdb).

## Ndërtimi (Windows, me .NET 8 SDK)
    dotnet publish -c Release -r win-x86 --self-contained false -o publish

Për një .exe që nuk kërkon .NET të instaluar në kompjuterin tjetër:

    dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish

Pastaj ndrysho `publish\appsettings.json` (shtegu i .mdb dhe i skedarit AED).

## Rëndësishme
- Programi është 32-bit (x86) sepse Jet 4.0 për .mdb punon vetëm në 32-bit.
- DatabasePath duhet të jetë .mdb me TË DHËNAT (backend), jo front-end-i me linke.
- Bëj kopje të .mdb para provës së parë.

## Shortcut-e te Shitja
Enter = shto barkodin, F5 = ruaj + printo AED, F6 = ruaj pa printuar, Del = fshi rresht, Esc = pastro.

## GitHub
Push në `main` nis automatikisht workflow-n **Build exe** (Actions). Kur mbaron, shkarko artifact-in
`MarketPrenga-win-x86` — brenda është `MarketPrenga.exe` dhe `appsettings.json`.
