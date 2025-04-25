# CO2Crawler

Et .NET 8-konsolprogram der crawler et website og måler CO₂-forbrug for hver side via `co2.js`. Når analysen er færdig, genereres en CSV-rapport og en opsummering, som sendes via e-mail.

## Funktioner
- Crawler hele domænet asynkront med begrænsning på samtidige forespørgsler (konfigurerbart)
- Beregner CO₂-udledning for hver side med `co2.js`
- Identificerer indholdstyper: HTML, CSS, JS, billeder og video
- Kontrollerer om domænet hostes grønt
- Eksporterer resultater i CSV-format
- Sender en rapport pr. e-mail med både CSV-fil og tekstlig opsummering
- Rapporten kan sendes som **HTML-e-mail med tabeller** eller **ren tekst**, afhængigt af indstilling

## Krav
- .NET 8 SDK
- Node.js (anvendes til at køre `co2.js` via `co2Runner.mjs`)

## Installation og brug
1. Klon projektet
2. Tilpas `appsettings.json` med dine domæne- og e-mailoplysninger
3. Kør projektet:

```bash
dotnet run
```

## appsettings.json eksempel
```json
{
  "CrawlSettings": {
    "Domain": "https://www.birkmose.com",
    "ExcludedPaths": ["/admin", "/api"],
    "MaxConcurrentRequests": 5
  },
  "EmailSettings": {
    "SenderEmail": "rapport@ditdomæne.dk",
    "SenderPassword": "dinkode",
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "Recipient": "modtager@eksempel.dk"
  },
  "Report": {
    "Format": "html"  // eller "text"
  }
}
```

## Output
- `CSV-rapport` genereres og vedhæftes e-mail
- `Email-rapport` inkluderer en sammenfatning:
  - Antal sider
  - CO₂-udledning
  - Top 10 tungeste sider
  - Indholdstypefordeling
  - Sammenligning med best practice
  - Sammenligning med hverdagsaktiviteter (vasketøj / bøffer)

## Licens
MIT License

---

🧪 Klar til test og videreudvikling
