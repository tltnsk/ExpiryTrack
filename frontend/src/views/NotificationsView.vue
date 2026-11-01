<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { formatDateTime} from '@/utils/format'

// notification response
interface Notification {
    id: number,
    type: string,
    message: string,
    isRead: boolean,
    createdAt: string,
    itemId: number | null,
    requestId: number | null
}

const notifications = ref<Notification[]>([]);
const errorMessage = ref('')
const actionError = ref('')
const loading = ref(true)

async function loadNotifications() {
    const response = await fetch('/api/notifications');
    if (response.ok) {
        notifications.value = await response.json();
    } else {
        errorMessage.value = `Could not load notifications (error ${response.status}).`
    }
    loading.value = false
}

onMounted(loadNotifications)

async function markAsRead(id: number)  {
    actionError.value = ''
    const response = await fetch(`/api/notifications/${id}/read`, {
        method: 'PUT' })

    if (response.ok) {
        await loadNotifications()
    } else {
        actionError.value = `Could not mark notification as read (error ${response.status}).`
    }
}

</script>
<template>
    <h2>Notifications</h2>
    <p v-if="actionError" class="error">{{ actionError }}</p>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage"> {{ errorMessage }}</p>
    <p v-else-if="notifications.length===0">No notifications.</p>
    
    <div v-else class="notifications-box">
        <div v-for="notification in notifications" :key="notification.id" 
             class="notification" :class="{unread: !notification.isRead}">
            <p>{{ notification.message }}</p>
            <p>{{ notification.isRead  ? "Read" : "Unread"}}</p>
            <p>{{ formatDateTime(notification.createdAt )}}</p>
            <RouterLink v-if="notification.itemId !== null" :to="`/items/${notification.itemId}`">Open Item</RouterLink>
            <RouterLink v-else-if="notification.requestId !== null" :to="`/requests/${notification.requestId}`">Open Request</RouterLink>
            <button v-if="!notification.isRead" class="read-button" title="Mark as read" aria-label="Mark as read" @click="markAsRead(notification.id)">✔️</button>

        </div>
    </div>
</template>
<style scoped>
.notifications-box {
    display: flex;
    flex-direction: column;
    gap: 1em;
}
.notification {
    border: 1px solid black;
    border-radius: 10px;
    padding: 10px;
    position: relative;
    padding-right: 40px;
}

.notification.unread {
    border-color: var(--navy);
}

.read-button {
    position: absolute; top: 8px; right: 8px;
    margin: 0;
    padding: 2px 8px;
    background: none;
    color: var(--navy);
    font-size: 1.2em;
}
</style>