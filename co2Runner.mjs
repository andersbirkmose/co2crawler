import fetch from 'node-fetch';
import { JSDOM } from 'jsdom';
import fs from 'fs';
import { co2 } from '@tgwf/co2';
import { URL } from 'url';

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
  if (greenCache[hostname] !== undefined) return greenCache[hostname];
  const res = await fetch(`https://api.thegreenwebfoundation.org/greencheck/${hostname}`);
  const json = await res.json();
  const isGreen = json.green || false;
  greenCache[hostname] = isGreen;
  fs.writeFileSync(cacheFile, JSON.stringify(greenCache, null, 2));
  return isGreen;
};

const getResourceBytes = async (url) => {
  try {
    if (!url || url.startsWith('data:') || url.startsWith('blob:')) return 0;
    const res = await fetch(url, { method: 'HEAD' });
    let length = res.headers.get('content-length');
    if (!length || length === '0') {
      const getRes = await fetch(url);
      const buffer = await getRes.arrayBuffer();
      return buffer.byteLength;
    }
    return parseInt(length, 10);
  } catch {
    console.warn(`⚠️ Kunne ikke hente størrelse for: ${url}`);
    return 0;
  }
};

const makeAbsolute = (src) => {
  try {
    return new URL(src, pageUrl).href;
  } catch {
    return null;
  }
};

(async () => {
  try {
    const pageRes = await fetch(pageUrl);
    const html = await pageRes.text();
    const htmlBytes = Buffer.byteLength(html, 'utf8');
    const dom = new JSDOM(html, { url: pageUrl });
    const document = dom.window.document;

    const hostname = new URL(pageUrl).hostname;
    const isGreen = await getGreenStatus(hostname);

    const collect = (selector, attr) =>
      Array.from(document.querySelectorAll(selector))
        .map(el => makeAbsolute(el.getAttribute(attr)))
        .filter(Boolean);

    const imageLinks = collect('img[src]', 'src');
    const jsLinks = collect('script[src]', 'src');
    const cssLinks = collect('link[rel="stylesheet"]', 'href')
      .concat(collect('link[as="style"][href]', 'href'));

    const preloadVideos = collect('link[rel="preload"][as="video"]', 'href');
    const tagVideos = collect('video[src], source[src]', 'src');
    const iframeVideos = collect('iframe[src]', 'src')
      .filter(src =>
        src.includes('youtube') ||
        src.includes('vimeo') ||
        src.includes('dreambroker') ||
        src.includes('videotool') ||
        src.includes('23video') ||
        src.includes('twentythree')
      );

    const directVideoLinks = [...preloadVideos, ...tagVideos];
    let videoBytes = 0;
    for (const src of directVideoLinks) videoBytes += await getResourceBytes(src);

    const videoBytesEstimate = iframeVideos.length * 5 * 1024 * 1024; // estimer 5MB pr iframe
    videoBytes += videoBytesEstimate;

    let imageBytes = 0, jsBytes = 0, cssBytes = 0;
    for (const src of imageLinks) imageBytes += await getResourceBytes(src);
    for (const src of jsLinks) jsBytes += await getResourceBytes(src);
    for (const src of cssLinks) cssBytes += await getResourceBytes(src);

    const totalBytes = htmlBytes + imageBytes + jsBytes + cssBytes + videoBytes;
    const estimator = new co2();

    const result = {
      url: pageUrl,
      green: isGreen,
      bytes: totalBytes,
      htmlBytes,
      imageBytes,
      jsBytes,
      cssBytes,
      videoBytes,
      co2: {
        grid: { grams: estimator.perByte(totalBytes, false) },
        renewable: { grams: estimator.perByte(totalBytes, true) },
        chosen: { grams: estimator.perByte(totalBytes, isGreen) }
      }
    };

    console.log(JSON.stringify(result));
  } catch (err) {
    console.error("❌ Fejl i co2Runner.mjs:\n", err);
    process.exit(1);
  }
})();
