import { describe, it, expect } from 'vitest'
import { formatLabel, formatMoney, formatDate } from '@/utils/format'

describe('formatLabel', () => {
    it('turns a status into readable text', () => {
        expect(formatLabel('PendingManagerApproval')).toBe('Waiting for manager')
    })
    it('returns a dash for null', () => {
        expect(formatLabel(null)).toBe('-')
    })
    it('returns an unknown value unchanged', () => {
        expect(formatLabel('Something')).toBe('Something')
    })
})

describe('formatMoney', () => {
    it('adds thousands separator with 2 decimals', () => {
        expect(formatMoney(12400.5)).toBe('12,400.50')
    })
    it('shows 0 as a number', () => {
        expect(formatMoney(0)).toBe('0.00')
    })
})

describe('formatDate', () => {
    it('shows the date in a readable form', () => {
        expect(formatDate('2027-10-31')).toBe('31 Oct 2027')
    })
})