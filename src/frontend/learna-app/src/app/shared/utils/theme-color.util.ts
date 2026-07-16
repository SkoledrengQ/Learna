/**
 * Small self-contained color-math helpers for runtime brand theming. No dependency on a
 * full M3 color library — just enough HSL tinting/shading and WCAG contrast math to derive
 * a coherent set of "on-color" / container shades from a single admin-picked hex color.
 */

interface Rgb { r: number; g: number; b: number; }
interface Hsl { h: number; s: number; l: number; }

const DARK_ON_COLOR = '#1a1a1a';
const LIGHT_ON_COLOR = '#ffffff';

function hexToRgb(hex: string): Rgb {
  const normalized = hex.replace('#', '');
  const r = parseInt(normalized.substring(0, 2), 16);
  const g = parseInt(normalized.substring(2, 4), 16);
  const b = parseInt(normalized.substring(4, 6), 16);
  return { r, g, b };
}

function rgbToHex({ r, g, b }: Rgb): string {
  const toHex = (c: number) => Math.max(0, Math.min(255, Math.round(c))).toString(16).padStart(2, '0');
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

function rgbToHsl({ r, g, b }: Rgb): Hsl {
  const rn = r / 255, gn = g / 255, bn = b / 255;
  const max = Math.max(rn, gn, bn), min = Math.min(rn, gn, bn);
  let h = 0;
  const l = (max + min) / 2;
  const d = max - min;
  const s = d === 0 ? 0 : d / (1 - Math.abs(2 * l - 1));

  if (d !== 0) {
    switch (max) {
      case rn: h = ((gn - bn) / d) % 6; break;
      case gn: h = (bn - rn) / d + 2; break;
      default: h = (rn - gn) / d + 4; break;
    }
    h *= 60;
    if (h < 0) h += 360;
  }

  return { h, s, l };
}

function hslToRgb({ h, s, l }: Hsl): Rgb {
  const c = (1 - Math.abs(2 * l - 1)) * s;
  const x = c * (1 - Math.abs((h / 60) % 2 - 1));
  const m = l - c / 2;
  let [r, g, b] = [0, 0, 0];

  if (h < 60) [r, g, b] = [c, x, 0];
  else if (h < 120) [r, g, b] = [x, c, 0];
  else if (h < 180) [r, g, b] = [0, c, x];
  else if (h < 240) [r, g, b] = [0, x, c];
  else if (h < 300) [r, g, b] = [x, 0, c];
  else [r, g, b] = [c, 0, x];

  return { r: (r + m) * 255, g: (g + m) * 255, b: (b + m) * 255 };
}

/** Relative luminance per WCAG 2.x. */
function relativeLuminance({ r, g, b }: Rgb): number {
  const linearize = (c: number) => {
    const cs = c / 255;
    return cs <= 0.03928 ? cs / 12.92 : Math.pow((cs + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * linearize(r) + 0.7152 * linearize(g) + 0.0722 * linearize(b);
}

/** WCAG contrast ratio between two hex colors, in [1, 21]. */
export function contrastRatio(hexA: string, hexB: string): number {
  const lA = relativeLuminance(hexToRgb(hexA));
  const lB = relativeLuminance(hexToRgb(hexB));
  const lighter = Math.max(lA, lB);
  const darker = Math.min(lA, lB);
  return (lighter + 0.05) / (darker + 0.05);
}

/** Returns whichever of near-black/white gives the higher contrast against `hex` (AA-safe text-on-color). */
export function pickOnColor(hex: string): string {
  const darkRatio = contrastRatio(hex, DARK_ON_COLOR);
  const lightRatio = contrastRatio(hex, LIGHT_ON_COLOR);
  return lightRatio >= darkRatio ? LIGHT_ON_COLOR : DARK_ON_COLOR;
}

/** Returns `hex` with its HSL lightness replaced (0-1), hue/saturation preserved. */
export function withLightness(hex: string, lightness: number): string {
  const hsl = rgbToHsl(hexToRgb(hex));
  return rgbToHex(hslToRgb({ ...hsl, l: lightness }));
}

export interface DerivedPrimaryPalette {
  primary: string;
  onPrimary: string;
  primaryContainer: string;
  onPrimaryContainer: string;
  primaryFixed: string;
  primaryFixedDim: string;
  onPrimaryFixed: string;
  onPrimaryFixedVariant: string;
  inversePrimary: string;
  surfaceTint: string;
}

/** Derives a full M3 "primary" role family from a single brand color, for a light-only theme. */
export function derivePrimaryPalette(baseHex: string): DerivedPrimaryPalette {
  const primaryContainer = withLightness(baseHex, 0.9);
  const primaryFixedDim = withLightness(baseHex, 0.8);

  return {
    primary: baseHex,
    onPrimary: pickOnColor(baseHex),
    primaryContainer,
    onPrimaryContainer: pickOnColor(primaryContainer),
    primaryFixed: primaryContainer,
    primaryFixedDim,
    onPrimaryFixed: pickOnColor(primaryContainer),
    onPrimaryFixedVariant: pickOnColor(primaryFixedDim),
    inversePrimary: primaryFixedDim,
    surfaceTint: baseHex
  };
}
