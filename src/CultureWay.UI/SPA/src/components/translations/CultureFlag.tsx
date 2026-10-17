import { useEffect, useState } from 'react';

const LANGUAGE_DEFAULT_REGION: Record<string, string> = {
  en: 'US', pl: 'PL', de: 'DE', fr: 'FR', es: 'ES', it: 'IT', pt: 'PT', nl: 'NL',
  ru: 'RU', uk: 'UA', cs: 'CZ', sk: 'SK', hu: 'HU', ro: 'RO', bg: 'BG', el: 'GR',
  tr: 'TR', sv: 'SE', da: 'DK', no: 'NO', nb: 'NO', nn: 'NO', fi: 'FI', is: 'IS',
  lt: 'LT', lv: 'LV', et: 'EE', sl: 'SI', hr: 'HR', sr: 'RS', bs: 'BA', mk: 'MK',
  sq: 'AL', he: 'IL', ar: 'SA', hi: 'IN', bn: 'BD', ur: 'PK', fa: 'IR', th: 'TH',
  vi: 'VN', id: 'ID', ms: 'MY', zh: 'CN', ja: 'JP', ko: 'KR', tl: 'PH', sw: 'TZ',
  am: 'ET', az: 'AZ', ka: 'GE', hy: 'AM', kk: 'KZ', uz: 'UZ', mn: 'MN', ga: 'IE',
  cy: 'GB', mt: 'MT', lb: 'LU',
};

function regionCodeFor(culture: string): string | null {
  if (!culture) return null;

  const parts = culture.split(/[-_]/);
  const lang = parts[0]?.toLowerCase() ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1] : undefined;
  const region = last && last.length === 2 ? last.toUpperCase() : LANGUAGE_DEFAULT_REGION[lang];

  return region && /^[A-Z]{2}$/.test(region) ? region : null;
}

// One small file per flag, loaded when a culture shows it: all flags together would be most of the module.
const FLAG_FILES = import.meta.glob<string>('/node_modules/country-flag-icons/string/3x2/[A-Z][A-Z].js', { import: 'default' });
const flagUrls = new Map<string, Promise<string | null>>();

function flagUrl(region: string): Promise<string | null> {
  let url = flagUrls.get(region);
  if (!url) {
    const load = FLAG_FILES[`/node_modules/country-flag-icons/string/3x2/${region}.js`];
    url = load
      ? load().then((svg) => `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`, () => null)
      : Promise.resolve(null);
    flagUrls.set(region, url);
  }
  return url;
}

interface CultureFlagProps {
  code: string;
  size?: number;
}

const CultureFlag = ({ code, size = 13 }: CultureFlagProps) => {
  const region = regionCodeFor(code);
  const [url, setUrl] = useState<{ region: string; url: string | null }>();

  useEffect(() => {
    if (!region) return;
    let current = true;
    flagUrl(region).then((loaded) => current && setUrl({ region, url: loaded }));
    return () => {
      current = false;
    };
  }, [region]);

  const style = { width: size * 1.5, height: size, borderRadius: 2, flexShrink: 0, display: 'block' } as const;
  if (!region) return null;
  // The same box while the flag loads, so the row does not shift.
  if (url?.region !== region) return <span style={style} aria-hidden />;
  return url.url ? <img src={url.url} alt="" style={style} /> : null;
};

export default CultureFlag;
