# 🌍 CO₂Crawler

Et .NET 8-konsolprogram der crawler et website, analyserer hver side med [co2.js](https://www.npmjs.com/package/@tgwf/co2), og genererer en CSV-rapport over CO₂-forbruget pr. side. Rapporten sendes automatisk via e-mail.

---

## 🧩 Funktioner

- Crawler hele domænet (undgår dubletter med / og #)
- Analyserer CO₂-udledning ved hjælp af `@tgwf/co2`
- Checker om domænet er hostet grønt (via The Green Web Foundation)
- Bruger grøn hosting info til korrekt CO₂-beregning
- CSV-rapport gemmes midlertidigt og sendes via e-mail
- Antal samtidige requests kan styres i konfigurationen

---

## 🚀 Installation

### 1. Klon projektet

```bash
git clone https://github.com/din-bruger/co2crawler.git
cd co2crawler
