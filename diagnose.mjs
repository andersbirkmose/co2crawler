import fetch from 'node-fetch';
import { JSDOM } from 'jsdom';

const html = await fetch("https://www.birkmose.com").then(r => r.text());
const dom = new JSDOM(html);
const document = dom.window.document;

console.log("JS:", Array.from(document.querySelectorAll('script[src]')).map(e => e.getAttribute('src')));
console.log("CSS:", Array.from(document.querySelectorAll('link[rel="stylesheet"]')).map(e => e.getAttribute('href')));
