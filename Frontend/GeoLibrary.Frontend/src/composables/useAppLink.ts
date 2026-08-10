import { useAuthStore } from '@/stores/auth'

/**
 * Le schede di libreria e libro esistono a due indirizzi: pubblico (/libraries/...)
 * e dentro l'area autenticata (/app/libraries/...). Sono gli stessi componenti,
 * cambia solo il layout che li ospita.
 */
export function useAppLink() {
  const auth = useAuthStore()

  const base = () => (auth.isAuthenticated ? '/app' : '')

  function libraryPath(libraryId: string) {
    return `${base()}/libraries/${libraryId}`
  }

  function bookPath(libraryId: string, bookId: string) {
    return `${base()}/libraries/${libraryId}/book/${bookId}`
  }

  return { libraryPath, bookPath }
}
