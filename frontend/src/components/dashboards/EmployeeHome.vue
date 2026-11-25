<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import type { EmployeeDashboard } from '@/utils/dashboard'
import StateChart from '../StateChart.vue'
import { formatLabel } from '@/utils/format'
import BarChart from '../BarChart.vue'

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

// open requests where someone else has to act    
const waitingForOthers = computed(() => dashboard.value ? dashboard.value.myOpenRequests.filter((r) => r.status === 'PendingFinancialReview' || r.status === 'PendingManagerApproval') : [])
</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>

    <div v-else-if="dashboard" class="employee-home">
        <header class="greeting">
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

                <section class="box waiting">
                    <h3>Waiting for others</h3>
                    <p v-if="waitingForOthers.length === 0" class="empty">None of your requests are waiting.</p>

                    <RouterLink v-for="request in waitingForOthers" :key="request.id" :to="`/requests/${request.id}`" class="waiting-row">
                        <strong>{{ request.itemName }}</strong>
                        <span class="state" :class="request.status">{{ formatLabel(request.status) }}</span>
                    </RouterLink>
                </section>
                
                <section class="box">
                    <h3>When do my items expire</h3>
                    <BarChart 
                        :labels="dashboard.itemsByTimeLeft.map((group) => group.label)"
                        :values="dashboard.itemsByTimeLeft.map((group) => group.value)"
                        :aspect-ratio="3"/>
                </section>
            </div>

            <aside class="side-column">
                <RouterLink to="/items?state=ExpiringSoon" class="stat-card" id="expiring-soon">
                    <span class="stat-label" >Expiring soon</span>
                    <strong class="stat-value">{{ dashboard.expiringSoonItems }}</strong>
                </RouterLink>

                <RouterLink to="/items?state=Expired" class="stat-card" id="expired">
                    <span class="stat-label">Expired</span>
                    <strong class="stat-value">{{ dashboard.expiredItems }}</strong>
                </RouterLink>

                <RouterLink to="/items?state=Active" class="stat-card" id="active">
                    <span class="stat-label" >Active</span>
                    <strong class="stat-value">{{ dashboard.activeItems }}</strong>
                </RouterLink>

                <div class="stat-card coverage">
                    <span class="stat-label">Your items</span>
                    <div class="donut">
                        <StateChart :values="[dashboard.activeItems, dashboard.expiringSoonItems, dashboard.expiredItems, dashboard.cancelledItems]" 
                                :aspect-ratio="1" legend-position="bottom"/>
                    </div>
                </div>
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
    grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
    gap: 20px;
    align-items: start;
}

.box {
    padding: 16px 20px;
    border: 1px solid var(--border);
    background-color: #f7f7f7;
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

.side-column {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.stat-card {
    display: grid;
    grid-template-columns: 1fr auto;
    align-items: center;
    padding: 14px 16px;
    border: 1px solid var(--border);
    color: inherit;
    text-decoration: none;
    background-color: rgb(233, 233, 245);
}

.stat-card:hover {
    opacity: 0.9;
}

.stat-label {
    font-weight: 600;
}

.stat-value {
    font-size: 1.6em;
    text-align: right;
}

.main-column {
    display: flex;
    flex-direction: column;
    gap: 20px;
}

.waiting-row {
    display: grid;
    grid-template-columns: 1fr auto;
    align-items: center;
    gap: 12px;
    padding: 10px 0;
    text-decoration: none;
}

.waiting-row:last-child {
    border-bottom: none;
}

.coverage {
    grid-template-columns: 1fr;
    gap: 10px;
}

#expiring-soon {
    background-color: #3e81ec;
}

#active {
    background-color: #618fd8; 
}

#expired {
    background-color: #94bcfa;
}

.donut {
    width: 100%;
    max-width: 260px;
    margin: 0 auto;
}

@media (max-width: 1100px) {
    .home-grid {
        grid-template-columns: 1fr;
    }

    .side-column {
        order: -1;
        display: grid;
        grid-template-columns: repeat(3, 1fr);
    }

    .coverage {
        grid-column: 1 / -1;
    }
}


@media (max-width: 600px) {
    .stat-grid {
        grid-template-columns: 1fr;
        padding: 10px 12px;
    }

    .stat-label {
        font-size: 0.85em;
    }

    .stat-value {
        font-size: 1.3em;
        text-align: left;
    }

    .todo-row {
        flex-direction: column;
        align-items: flex-start;
        gap: 8px;
        padding-left: 0;
    }
}
</style>