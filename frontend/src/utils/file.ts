// the same checks as server, so the item isn't created with a file that will be refused
export async function checkPdf(file: File): Promise<string | null> {
    if (file.size > 10 * 1024 * 1024) return 'The file must be 10 MB or smaller.'
    if (!file.name.toLowerCase().endsWith('.pdf')) return 'Only .pdf files can be uploaded'
    if (await file.slice(0, 5).text() !== '%PDF-') return 'You can only upload PDF files.'
    const content = await file.text()
    if (content.includes('/JavaScript') || content.includes('/JS') || content.includes('/Launch'))
        return 'PDF files with scripts or actions are not allowed.'
    return null
}