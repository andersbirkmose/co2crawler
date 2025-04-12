import { co2 } from '@tgwf/co2';
import fetch from 'node-fetch';
import { URL } from 'url';
import fs from 'fs';

const pageUrl = process.argv[2];
if (!pageUrl || !pageUrl.startsWith('http')) {
  console.error("❌ Ugyldig URL:", pageUrl);
  process.exit(1);
}

const cacheFile = './green-hosting-cache.json';
let greenCache = {};

if (fs.existsSync(cacheFile)) {
  const raw = fs.readFileSync(cacheFile);
  greenCache = JSON.parse(raw);
}

const getGreenStatus = async (hostname) => {
  if (greenCache[hostname] !== undefined) {
    return greenCache[hostname];
  }

  const check = await fetch(`https://api.thegreenwebfoundation.org/greencheck/${hostname}`);
  const result = await check.json();
  const isGreen = result.green || false;

  greenCache[hostname] = isGreen;
  fs.writeFileSync(cacheFile, JSON.stringify(greenCache, null, 2));

  return isGreen;
};

(async () => {
  try {
    const response = await fetch(pageUrl);
    const html = await response.text();
    const bytes = Buffer.byteLength(html, 'utf8');

    const hostname = new URL(pageUrl).hostname;
    const isGreen = await getGreenStatus(hostname);

    const estimator = new co2();
    const grid = estimator.perByte(bytes, false);
    const renewable = estimator.perByte(bytes, true);
    const chosen = estimator.perByte(bytes, isGreen);

    const result = {
      url: pageUrl,
      green: isGreen,
      bytes,
      co2: {
        grid: { grams: grid },
        renewable: { grams: renewable },
        chosen: { grams: chosen }
      }
    };

    console.log(JSON.stringify(result));
  } catch (err) {
    console.error("Fejl i co2Runner.mjs:", err);
    process.exit(1);
  }
})();
