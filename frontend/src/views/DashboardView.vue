<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime} from '@/utils/format'

interface DepartmentItemCount {
    departmentName: string
    itemCount: number
}

interface DashboardItem {
    id: number
    name: string
    departmentName: string
    expirationDate: string
}

interface AdminDashboard {
    totalItems: number
    activeItems: number
    expiringSoonItems: number
    expiredItems: number
    cancelledItems: number
    itemsByDepartment: DepartmentItemCount[]
    upcomingExpirations: DashboardItem[]
}

interface DashboardRequest {
    id: number
    itemName: string
    status: string
    needsFinancialReview: boolean
    proposedExpirationDate: string
    submittedAt: string
    proposedCost: number | null
    requestedByUserName: string
}

interface ManagerDashboard {
    totalItems: number
    activeItems: number
    expiringSoonItems: number
    expiredItems: number
    cancelledItems: number
    requestsToReview: DashboardRequest[]
    requestsAwaitingCompletion: DashboardRequest[]
    upcomingExpirations: DashboardItem[]
}

const auth = useAuthStore()

const adminDashboard = ref<AdminDashboard | null>(null)
const managerDashboard = ref<ManagerDashboard | null>(null)
const loading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
    if (auth.user?.role === 'Administrator') {
        const response = await fetch('/api/dashboard/admin')
        if (response.ok) {
            adminDashboard.value = await response.json()
        } else {
            errorMessage.value = 'Could not load the dashboard.'
        }
    }
    if (auth.user?.role === 'DepartmentManager') {
        const response = await fetch('/api/dashboard/manager')
        if (response.ok) {
            managerDashboard.value = await response.json()
        } else {
            errorMessage.value = 'Could not load the dashboard.'
        }
    }
    loading.value = false
})
</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>

    <!-- ADMIN -->
    <div v-else-if="adminDashboard">
        <div class="cards">
            <div class="card">
                <span class="number">{{ adminDashboard.totalItems }}</span>
                <span>Total Items</span>
            </div>
            <div class="card">
                <span class="number">{{ adminDashboard.activeItems }}</span>
                <span>Active Items</span>
            </div>
            <div class="card">
                <span class="number">{{ adminDashboard.expiringSoonItems }}</span>
                <span>Expiring Soon Items</span>
            </div>
            <div class="card">
                <span class="number">{{ adminDashboard.expiredItems }}</span>
                <span>Expired Items</span>
            </div>
            <div class="card">
                <span class="number">{{ adminDashboard.cancelledItems }}</span>
                <span>Cancelled Items</span>
            </div>
        </div>

        <h3>Items per department</h3>
        <table>
            <thead>
                <tr>
                    <th>Department</th>
                    <th>Items</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="row in adminDashboard.itemsByDepartment" :key="row.departmentName">
                    <td>{{ row.departmentName }}</td>
                    <td>{{ row.itemCount }}</td>
                </tr>
            </tbody>
        </table>

        <h3>Upcoming expirations</h3>
        <p v-if="adminDashboard.upcomingExpirations.length === 0">No items are expiring soon.</p>
        <table v-else>
            <thead>
                <tr>
                    <th>Item</th>
                    <th>Department</th>
                    <th>Expires</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in adminDashboard.upcomingExpirations" :key="item.id">
                    <td><RouterLink :to="`/items/${item.id}`">{{ item.name }}</RouterLink></td>
                    <td>{{ item.departmentName }}</td>
                    <td>{{ formatDate(item.expirationDate) }}</td>
                </tr>
            </tbody>
        </table>
    </div>

    <!-- MANAGER -->
    <div v-else-if="managerDashboard">
        <div class="cards">
            <div class="card">
                <span class="number">{{ managerDashboard.totalItems }}</span>
                <span>Total Items</span>
            </div>
            <div class="card">
                <span class="number">{{ managerDashboard.activeItems }}</span>
                <span>Active Items</span>
            </div>
            <div class="card">
                <span class="number">{{ managerDashboard.expiringSoonItems }}</span>
                <span>Expiring Soon Items</span>
            </div>
            <div class="card">
                <span class="number">{{ managerDashboard.expiredItems }}</span>
                <span>Expired Items</span>
            </div>
            <div class="card">
                <span class="number">{{ managerDashboard.cancelledItems }}</span>
                <span>Cancelled Items</span>
            </div>
        </div>

        <h3>Requests to Review</h3>
        <p v-if="managerDashboard.requestsToReview.length === 0">No requests to review.</p>
        <table v-else>
            <thead>
                <tr>
                    <th>Item</th>
                    <th>Proposed Expiration Date</th>
                    <th>Submitted At</th>
                    <th>Proposed Cost</th>
                    <th>Requested By</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="request in managerDashboard.requestsToReview" :key="request.id">
                    <td><RouterLink :to="`/requests/${request.id}`">{{ request.itemName }}</RouterLink></td>
                    <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                    <td>{{ formatDateTime(request.submittedAt) }}</td>
                    <td>{{ request.proposedCost ?? '-'}}</td>
                    <td>{{ request.requestedByUserName }}</td>
                </tr>
            </tbody>
        </table>

        <h3>Requests Awaiting Completion</h3>
        <p v-if="managerDashboard.requestsAwaitingCompletion.length === 0">No requests awaiting completion.</p>
        <table v-else>
            <thead>
                <tr>
                    <th>Item</th>
                    <th>Proposed Expiration Date</th>
                    <th>Submitted At</th>
                    <th>Proposed Cost</th>
                    <th>Requested By</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="request in managerDashboard.requestsAwaitingCompletion" :key="request.id">
                    <td><RouterLink :to="`/requests/${request.id}`">{{ request.itemName }}</RouterLink></td>
                    <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                    <td>{{ formatDateTime(request.submittedAt) }}</td>
                    <td>{{ request.proposedCost ?? '-'}}</td>
                    <td>{{ request.requestedByUserName }}</td>
                </tr>
            </tbody>
        </table>

        <h3>Upcoming expirations</h3>
        <p v-if="managerDashboard.upcomingExpirations.length === 0">No items are expiring soon.</p>
        <table v-else>
            <thead>
                <tr>
                    <th>Item</th>
                    <th>Expires</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in managerDashboard.upcomingExpirations" :key="item.id">
                    <td><RouterLink :to="`/items/${item.id}`">{{ item.name }}</RouterLink></td>
                    <td>{{ formatDate(item.expirationDate) }}</td>
                </tr>
            </tbody>
        </table>
    </div>
</template>
<style scoped>
.cards{
    display: flex;
    gap: 16px;
    flex-wrap: wrap;
    margin-bottom: 24px;
}

.card{
    display: flex;
    flex-direction: column;
    padding: 16px;
    min-width: 140px;
}

.number {
    font-size: 28px;
    font-weight: bold;
}
</style>