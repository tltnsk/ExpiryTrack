<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import type { EmployeeDashboard } from '@/utils/dashboard'
import { percent } from '@/utils/dashboard'

const auth = useAuthStore()

const dashboard = ref<EmployeeDashboard | null>(null)
const loading = ref(true)
const errorMessage = ref('')

onMounted( async () => {
    const response = await fetch('/api/dashboard/employee')
    if (response.ok) {
        dashboard.value = await response.json()
    } else {
        errorMessage.value = 'Could not load the dashboard.'
    }
    loading.value = false
})

// one task on the to-do list
interface Todo {
    key: string
    kind: 'answer' | 'complete' | 'renew'
    title: string
    itemName: string
    link: string
    button: string
}

const today = new Date().toISOString().slice(0, 10)

// how many days until a date (negative when it has passed)
function daysLeft(date: string) {
    return Math.round((Date.parse(date) - Date.parse(today)) / 86400000)
}

// to do list for the employee containing renewals that need to be submitted, answers to clarification or completion
const todos = computed<Todo[]>(() => {
    if (!dashboard.value) return []
    const list: Todo[] = []

    // the manager asked a question and the request waits for the answer
    for (const request of dashboard.value.requestsNeedingClarification) {
        list.push({
            key: `answer-${request.id}`,
            kind: 'answer',
            title: "Answer the manager's question",
            itemName: request.itemName,
            link: `/requests/${request.id}`,
            button: 'Answer',
        })
    }

    // approved: the employee renews the item and records it here
    for (const request of dashboard.value.requestsAwaitingCompletion) {
        const tooLate = request.proposedExpirationDate <= today
        list.push({
            key: `complete-${request.id}`,
            kind: 'complete',
            title: tooLate ? 'Renewal can no longer be completed' : 'Complete the renewal',
            itemName: request.itemName,
            link: `/requests/${request.id}`,
            button: tooLate ? 'Open' : 'Complete',
        })
    }

    // expiring or expired, and nobody has asked for a renewal yet
    for (const item of dashboard.value.itemsNeedingRenewal) {
        list.push({
            key: `renew-${item.id}`,
            kind: 'renew',
            title: 'Submit a renewal request',
            itemName: item.name,
            link: `/items/${item.id}/renew`,
            button: 'Submit renewal',
        })
    }
    return list
})

// items that expire in the next 30 days
const expiringIn30Days = computed(() => {
    if (!dashboard.value) return 0
    return dashboard.value.itemsByTimeLeft.slice(0, 2).reduce((sum, group) => sum + group.value, 0)
})

// items without a request: expiring soon ones still have a future date, expired ones a past date
const expiringWithoutRequest = computed(() =>
    dashboard.value ? dashboard.value.itemsNeedingRenewal.filter((item) => item.expirationDate >= today).length : 0)

const expiredWithoutRequest = computed(() =>
    dashboard.value ? dashboard.value.itemsNeedingRenewal.length - expiringWithoutRequest.value : 0)

// share of the items (without cancelled ones) that do not need anything
const coverage = computed(() => {
    if (!dashboard.value) return 0
    const counted = dashboard.value.totalItems - dashboard.value.cancelledItems
    return percent(counted - dashboard.value.itemsNeedingRenewal.length, counted)
})
</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>

    <div v-else-if="dashboard" class="employee-home">
        <header class="greeting">
            <h2>Hello, {{ auth.user?.firstName }}</h2>
            <p class="summary">
                <template v-if="todos.length === 0">Nothing needs you right now</template>
                <template v-else>You have <strong>{{ todos.length }}</strong> {{ todos.length === 1 ? 'thing' : 'things' }} to do.</template>
                {{ expiringIn30Days }} of your items expire in the next 30 days.
            </p>
        </header>

        <div class="home-grid">
            <div class="main-column">
                <section class="box">
                    <h3>To do</h3>
                    <p v-if="todos.length === 0" class="empty">All done. Nothing needs you right now.</p>

                    <div v-for="todo in todos" :key="todo.key" class="todo-row">
                        <div class="todo-text">
                            <strong>{{ todo.title }}: {{ todo.itemName }}</strong>
                        </div>
                        <RouterLink :to="todo.link" class="todo-button">{{ todo.button }}</RouterLink>
                    </div>
                </section>
            </div>

            <aside class="side-column">
            </aside>
        </div>
    </div>
</template>
<style scoped>
.greeting h2 {
    margin-bottom: 4px;
}

.summary {
    margin: 0 0 24px;
}

/* main column on the left, cards on the right */
.home-grid {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 20px;
    align-items: start;
}

.box {
    padding: 16px 20px;
    border: 1px solid var(--border);
}

.box h3 {
    margin: 0 0 12px;
}

.count {
    margin-left: 6px;
    padding: 1px 8px;

}

/* one task */
.todo-row {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 12px 0 12px 10px;
    border-bottom: 1px solid #eceef1;
    border-left: 3px solid transparent;
}

.todo-row:last-child {
    border-bottom: none;
}

.todo-text {
    display: flex;
    flex-direction: column;
    flex: 1;
    min-width: 0;
    padding: 0;
}

.todo-button {
    flex-shrink: 0;
    padding: 6px 12px;
    border-radius: 6px;
    background: var(--primary);
    color: white;
    font-size: 0.85em;
    text-decoration: none;
}

.todo-button:hover {
    opacity: 0.85;
}

.box h3 {
    font-size: 1.5em;
}

@media (max-width: 900px) {
    .home-grid {
        grid-template-columns: 1fr;
    }
}
</style>