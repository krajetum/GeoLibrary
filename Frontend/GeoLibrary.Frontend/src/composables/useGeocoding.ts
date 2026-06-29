const NOMINATIM = 'https://nominatim.openstreetmap.org'
const CONTACT_EMAIL = import.meta.env.VITE_NOMINATIM_EMAIL ?? ''

export interface GeoResult {
  label: string // indirizzo formattato (display_name)
  lat: number
  lon: number
  city?: string
  postalCode?: string
  country?: string
  countryCode?: string // ISO 3166-1 alpha-2, es. "IT"
}

function mapResult(r: any): GeoResult {
  const a = r.address ?? {}
  return {
    label: r.display_name,
    lat: Number(r.lat),
    lon: Number(r.lon),
    city: a.city ?? a.town ?? a.village ?? a.municipality,
    postalCode: a.postcode,
    country: a.country,
    countryCode: a.country_code?.toUpperCase(),
  }
}

export function useGeocoding() {
  async function search(query: string, signal?: AbortSignal): Promise<GeoResult[]> {
    if (query.trim().length < 3) return []
    const params = new URLSearchParams({
      q: query,
      format: 'jsonv2',
      addressdetails: '1',
      limit: '5',
    })
    if (CONTACT_EMAIL) params.set('email', CONTACT_EMAIL)

    const res = await fetch(`${NOMINATIM}/search?${params}`, {
      headers: { 'Accept-Language': 'it' },
      signal,
    })
    if (!res.ok) throw new Error(`Nominatim ${res.status}`)
    return (await res.json()).map(mapResult)
  }

  // Reverse geocoding: da coordinate a indirizzo (usato quando si trascina il marker)
  async function reverse(lat: number, lon: number): Promise<GeoResult | null> {
    const params = new URLSearchParams({
      lat: String(lat),
      lon: String(lon),
      format: 'jsonv2',
      addressdetails: '1',
    })
    if (CONTACT_EMAIL) params.set('email', CONTACT_EMAIL)

    const res = await fetch(`${NOMINATIM}/reverse?${params}`, {
      headers: { 'Accept-Language': 'it' },
    })
    if (!res.ok) return null
    return mapResult(await res.json())
  }

  return { search, reverse }
}
