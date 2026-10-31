// convert the raw date from the API into readable date 


// readable names for states, stutuses and history steps
const labels: Record<string, string> = {
  // item states
  Active: 'Active',
  ExpiringSoon: 'Expiring soon',
  Expired: 'Expired',
  Cancelled: 'Cancelled',
  // request statuses
  PendingFinancialReview: 'Waiting for finance',
  PendingManagerApproval: 'Waiting for manager',
  ClarificationRequired: 'Clarification needed',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Completed: 'Completed',
  // history steps
  Submitted: 'Submitted',
  FinanceReviewed: 'Finance reviewed',
  ClarificationRequested: 'Clarification requested',
  ClarificationProvided: 'Clarification provided',
  ClosedByExpiry: 'Closed by expiry',
}

export function formatLabel(value: string | null): string {
    if (!value) return ' '
    return labels[value] ?? value
}

export function formatDateTime(value: string | null): string {
    if (!value) return '-'

    const utc = value.endsWith('Z') ? value : value + 'Z'
    return new Date(utc).toLocaleString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    })
}

export function formatDate(value: string | null): string {
    if (!value) return '-'
    return new Date(value).toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        timeZone: 'UTC',
    })
}