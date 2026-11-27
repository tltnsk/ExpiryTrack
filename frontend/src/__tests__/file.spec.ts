import { describe, it, expect } from 'vitest'
import { checkPdf } from '@/utils/file'

// a small file that starts like a real PDF
const pdfText = '%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF'

describe('checkPdf', () => {
    it('accepts a normal PDF', async () => {
        expect(await checkPdf(new File([pdfText], 'offer.pdf'))).toBeNull()
    })

    it('refuses a file that is not named .pdf', async () => {
        expect(await checkPdf(new File([pdfText], 'offer.txt'))).toBe('Only .pdf files can be uploaded')
    })

    it('refuses a text file that is only named .pdf', async () => {
        expect(await checkPdf(new File(['just some text'], 'fake.pdf'))).toBe('You can only upload PDF files.')
    })

    it('refuses a PDF with a script', async () => {
        const withScript = '%PDF-1.4\n<< /OpenAction << /S /JavaScript /JS (void 0;) >> >>'
        expect(await checkPdf(new File([withScript], 'script.pdf'))).toBe('PDF files with scripts or actions are not allowed.')
    })

    it('refuses a file bigger than 10 MB', async () => {
        const big = new File(['%PDF-', 'x'.repeat(10 * 1024 * 1024)], 'big.pdf')
        expect(await checkPdf(big)).toBe('The file must be 10 MB or smaller.')
    })
})
