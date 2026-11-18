<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { formatDateTime} from '@/utils/format'
import { useNotificationsStore } from '@/stores/notifications'

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
const search = ref('')
const notificationsStore = useNotificationsStore()

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

// only notifications whose message contains the search text
const filteredNotifications = computed(() => {
    const text = search.value.trim().toLowerCase()
    return notifications.value.filter(n => 
        n.message.toLowerCase().includes(text)
    )
})

function dateAndTime(value: string): string[] {
    return formatDateTime(value).split(', ')
}

async function markAsRead(id: number)  {
    actionError.value = ''
    const response = await fetch(`/api/notifications/${id}/read`, {
        method: 'PUT' })

    if (response.ok) {
        await loadNotifications()
        notificationsStore.loadUnreadCount()
    } else {
        actionError.value = `Could not mark notification as read (error ${response.status}).`
    }
}

</script>
<template>
    <div class="inbox">
        <div class="inbox-header">
            <h2>Notifications</h2>
            <input v-model="search" type="search" class="inbox-search" placeholder="Search notifications" />
        </div>
        <p v-if="actionError" class="error inbox-message">{{ actionError }}</p>
            <p v-if="loading" class="inbox-message">Loading...</p>
            <p v-else-if="errorMessage" class="inbox-message"> {{ errorMessage }}</p>
            <p v-else-if="notifications.length===0" class="inbox-message">No notifications.</p> 
            <p v-else-if="filteredNotifications.length===0" class="inbox-message">No notifications match "{{ search }}".</p> 
            <table v-else>
                <thead>
                    <tr>
                        <th></th>
                        <th>Subject</th>
                        <th>Status</th>
                        <th>Received</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="notification in filteredNotifications" :key="notification.id" :class="{unread: !notification.isRead}">
                        <td class="icon-cell">
                            <span v-if="notification.isRead" class="read-check">✓</span>
                        </td>
                        <td>
                            <p class="subject"> {{ notification.message }}</p>
                        </td>
                        <td>{{ notification.isRead ? 'Read' : 'Unread' }}</td>

                        <td>
                            <div class="date">
                                <span>{{ dateAndTime(notification.createdAt)[0] }}</span>
                                <span class="time">{{ dateAndTime(notification.createdAt)[1] }}</span>
                            </div>
                        </td>

                        <td>
                            <div class="row-actions">
                                <RouterLink v-if="notification.itemId !== null" :to="`/items/${notification.itemId}`" class="outline-button">
                                    Open item
                                </RouterLink>
                                <RouterLink v-else-if="notification.requestId !== null" :to="`/requests/${notification.requestId}`" class="outline-button">
                                    Open request
                                </RouterLink>
                                <button v-if="!notification.isRead" type="button" class="outline-button" @click="markAsRead(notification.id)">
                                    Mark as read
                                </button>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
    </div>
</template>
<style scoped>
.inbox {
    background: white;
    border: 1px solid #e5e7eb;
    border-radius: 12px;
    overflow: hidden;
}

.inbox-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    padding: 20px 24px;
}

.inbox-header h2 {
    margin: 0;
}

.inbox-search {
    width: 320px;
    margin: 0;
}

.inbox-message {
    padding: 24px;
    margin: 0;
    color: #6b7280;
}

table {
    margin: 0;
}

th, td {
    border: none;
    border-bottom: 1px solid #f0f1f3;
    vertical-align: middle;
}

th {
    font-size: 1.2em;
}

tr.unread {
    background: #f4d5d3;
}

.icon-cell {
    width: 24px;
    text-align: center;
}

.subject {
    margin: 0;
}

.date {
    display: flex;
    flex-direction: column;
    white-space: nowrap;
}

.row-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
}

.outline-button {
    margin: 0;
    padding: 6px 12px;
    border: 1px solid #d1d5db;
    border-radius: 4px;
    background: white;
    color: var(--text);
    font-size: 0.90em;
    font-weight: 600;
    text-decoration: none;
    white-space: nowrap;
}

.outline-button:hover {
    opacity: 1;
    border-color: var(--text);
}

@media (max-width: 700px) {
    .inbox-header {flex-direction: column; align-items: stretch;}
    .inbox-search {width: 100%;}
}
</style>