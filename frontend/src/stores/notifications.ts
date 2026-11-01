import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useNotificationsStore = defineStore ('notifications', () => {
    const unreadCount = ref(0)
    async function loadUnreadCount() {
        const response = await fetch("/api/notifications/unread-count")
        if (response.ok) {
            unreadCount.value = await response.json()
        }
    }

    return { unreadCount, loadUnreadCount }
})