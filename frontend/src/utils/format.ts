// convert the raw date from the API into readable date 

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