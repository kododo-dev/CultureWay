import * as Flags from 'country-flag-icons/react/3x2';

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

const FlagsByRegion = Flags as unknown as Record<string, React.ComponentType<React.SVGProps<SVGSVGElement>>>;

interface CultureFlagProps {
  code: string;
  size?: number;
}

const CultureFlag = ({ code, size = 13 }: CultureFlagProps) => {
  const region = regionCodeFor(code);
  const FlagComponent = region ? FlagsByRegion[region] : undefined;

  if (!FlagComponent) return null;

  return (
    <FlagComponent
      style={{ width: size * 1.5, height: size, borderRadius: 2, flexShrink: 0, display: 'block' }}
    />
  );
};

export default CultureFlag;
