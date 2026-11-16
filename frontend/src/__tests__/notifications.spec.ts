import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useNotificationsStore } from '@/stores/notifications'

describe('notifications store', () => {
    beforeEach(() => {
        // each test gets a new Pinia store
        setActivePinia(createPinia())
    })
    afterEach(() => {
        // after each test, put the real fetch back
        vi.unstubAllGlobals()
    })

    // when the server answers ok unreadCount becomes the number from the response
    it('saves the unread count when the request succeeds', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, 
            json: async () => 3
        }))

        const store = useNotificationsStore()

        await store.loadUnreadCount()

        expect(store.unreadCount).toBe(3)
    })

    // when the request fails, the store keeps the count it already has
    it('keeps 0 when request fails', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false}))
        const store = useNotificationsStore()
        store.unreadCount = 5

        await store.loadUnreadCount()

        expect(store.unreadCount).toBe(5)
    })
})