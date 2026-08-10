/**
 * Le etichette degli stati di prestito servono sia nella scheda del libro sia
 * nella pagina dei prestiti: stanno qui per non riscriverle in due posti.
 * Gli stati arrivano dal backend come stringhe (Pending, Approved, Rejected, Returned).
 */
export function useLoanStatus() {
  function statusLabel(status: string) {
    return {
      Pending: 'In attesa',
      Approved: 'Attivo',
      Rejected: 'Rifiutato',
      Returned: 'Restituito',
    }[status]
  }

  function statusColor(status: string) {
    return { Pending: 'warning', Approved: 'success', Rejected: 'error', Returned: '' }[status]
  }

  function statusIcon(status: string) {
    return {
      Pending: 'mdi-clock-outline',
      Approved: 'mdi-check',
      Rejected: 'mdi-close',
      Returned: 'mdi-keyboard-return',
    }[status]
  }

  /** Un prestito ancora attivo oltre la data di rientro è in ritardo. */
  function isOverdue(loan: { status: string; returnDate: string }) {
    return loan.status === 'Approved' && new Date(loan.returnDate) < new Date()
  }

  function formatDate(value: string) {
    return new Date(value).toLocaleDateString('it-IT')
  }

  return { statusLabel, statusColor, statusIcon, isOverdue, formatDate }
}
