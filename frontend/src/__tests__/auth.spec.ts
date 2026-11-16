import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore, type User } from '@/stores/auth'

describe('auth store', () => {
    const user = {
        id: 1,
        firstName: 'Ana',
        lastName: 'Test',
        email: 'ana@test.com',
        role: 'Employee',
        departmentName: 'It'
    }

    beforeEach(() => {
        // each test gets a new Pinia store
        setActivePinia(createPinia())
    })
    afterEach(() => {
        // after each test, put the real fetch back
        vi.unstubAllGlobals()
    })

    // when loadme succeeds, the user is set
    it('saves the user when the request succeeds', async () => {
        vi.stubGlobal(
            'fetch',
            vi.fn().mockResolvedValue({
                ok: true,
                json: async () => user
            })
        )
        const store = useAuthStore()

        await store.loadMe()

        expect(store.user).toEqual(user)
    })

    // if loadme fails, the user becomes null
    it('user is null if load fails', async () => {
        vi.stubGlobal(
            'fetch',
            vi.fn().mockResolvedValue({
                ok: false
            })
        )
        const store = useAuthStore()
        store.user = user

        await store.loadMe()

        expect(store.user).toEqual(null)
    })

    // on logout, user becomes null
    it('user is null on logout', async () => {
        const fetchMock = vi.fn().mockResolvedValue({ok: true})
        vi.stubGlobal('fetch', fetchMock)

        const store = useAuthStore()
        store.user = user

        await store.logout()

        expect(store.user).toEqual(null)
        expect(fetchMock).toHaveBeenCalledWith('/api/auth/logout', { method: 'POST' })
    })
})