<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime, formatMoney} from '@/utils/format'
import {getTiles, getBarChart, getStateCounts, getMonthChart, percent, daysLeftText} from '@/utils/dashboard'
import type { AdminDashboard, ManagerDashboard, EmployeeDashboard, FinanceDashboard } from '@/utils/dashboard'
import BarChart from '@/components/BarChart.vue'
import StateChart from '@/components/StateChart.vue'

const auth = useAuthStore()

const adminDashboard = ref<AdminDashboard | null>(null)
const managerDashboard = ref<ManagerDashboard | null>(null)
const employeeDashboard = ref<EmployeeDashboard | null>(null)
const financeDashboard = ref<FinanceDashboard | null>(null)
const loading = ref(true)
const errorMessage = ref('')

const tiles = computed(() => getTiles(adminDashboard.value, managerDashboard.value, employeeDashboard.value, financeDashboard.value))
const barChart = computed(() => getBarChart(adminDashboard.value, managerDashboard.value, employeeDashboard.value, financeDashboard.value))
const stateCounts = computed(() => getStateCounts(adminDashboard.value, managerDashboard.value, employeeDashboard.value, financeDashboard.value))
const monthChart = computed(() => getMonthChart(adminDashboard.value, managerDashboard.value, employeeDashboard.value, financeDashboard.value))

const upcoming = computed(() => 
(adminDashboard.value ?? managerDashboard.value ?? employeeDashboard.value)?.upcomingExpirations ?? [])

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
    if (auth.user?.role === 'Employee') {
        const response = await fetch('/api/dashboard/employee')
        if (response.ok) {
            employeeDashboard.value = await response.json()
        } else {
            errorMessage.value = 'Could not load the dashboard.'
        }
    }
    if (auth.user?.role === 'FinanceOfficer') {
        const response = await fetch('/api/dashboard/finance')
        if (response.ok) {
            financeDashboard.value = await response.json()
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

    <div v-else class="dashboard" :class="{ 'no-side': financeDashboard}">
        <div class="main">
            <div class="tiles">
                <RouterLink v-for="tile in tiles" :key="tile.label" :to="tile.link" class="tile" :class="tile.color">
                    <span class="tile-label">{{ tile.label }}</span>
                    <span class="tile-bottom">
                        <span class="tile-value">{{ tile.value }}</span>
                    </span>
                </RouterLink>
            </div>

            <div class="charts">
                <div class="chart-card">
                    <h3>Items by state</h3>
                    <StateChart :values="stateCounts" />
                </div>
                <div class="chart-card">
                    <h3>{{barChart.title }}</h3>
                    <BarChart :labels="barChart.labels" :values="barChart.values" />
                 </div>
                <div class="chart-card wide">
                    <h3>Expirations in the next 12 months</h3>
                    <BarChart :labels="monthChart.labels" :values="monthChart.values" :aspect-ratio="4"/>
                 </div>
            </div>

        <section v-if="managerDashboard" class="table-section">
            <h3>Requests to review</h3>
            <p v-if="managerDashboard.requestsToReview.length === 0" class="empty">No requests to review.</p>
            <div v-else class="table-box">
                <table>
                    <thead>
                        <tr><th>Item</th><th>Requested by</th><th>Submitted</th><th>New expiry</th><th>Cost</th><th></th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="request in managerDashboard.requestsToReview" :key="request.id">
                            <td>{{ request.itemName }}</td>
                            <td>{{ request.requestedByUserName }}</td>
                            <td>{{ formatDateTime(request.submittedAt) }}</td>
                            <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                            <td>{{ formatMoney(request.proposedCost) }}</td>
                            <td><RouterLink :to="`/requests/${request.id}`" class="table-link">Review →</RouterLink></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>



        <!--Employee-->
        <section v-if="employeeDashboard" class="table-section">
                <h3>Items needing renewal</h3>
                <p v-if="employeeDashboard.itemsNeedingRenewal.length === 0" class="empty">No items needing renewal.</p>
                <div v-else class="table-box">
                    <table>
                        <thead>
                            <tr><th>Item</th><th>Department</th><th>Expires</th><th>Time left</th><th></th></tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in employeeDashboard.itemsNeedingRenewal" :key="item.id">
                                <td><RouterLink :to="`/items/${item.id}`">{{ item.name }}</RouterLink></td>
                                <td>{{ item.departmentName }}</td>
                                <td>{{ formatDate(item.expirationDate) }}</td>
                                <td>{{ daysLeftText(item.expirationDate) }}</td>
                                <td><RouterLink :to="`/items/${item.id}/renew`" class="table-link">Renew →</RouterLink></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
        </section>
        
        <section v-if="financeDashboard" class="table-section">
            <h3>Requests to review</h3>
            <p v-if="financeDashboard.requestsNeedingFinancialReview.length === 0" class="empty">No requests to review.</p>
            <div v-else class="table-box">
                <table>
                    <thead>
                        <tr><th>Item</th><th>Requested by</th><th>Submitted</th><th>New expiry</th><th>Cost</th><th></th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="request in financeDashboard.requestsNeedingFinancialReview" :key="request.id">
                            <td>{{ request.itemName }}</td>
                            <td>{{ request.requestedByUserName }}</td>
                            <td>{{ formatDateTime(request.submittedAt) }}</td>
                            <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                            <td>{{ formatMoney(request.proposedCost) }}</td>
                            <td><RouterLink :to="`/requests/${request.id}`" class="table-link">Review →</RouterLink></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>
        </div>

    <aside v-if="!financeDashboard" class="side">
        <section>
            <h3>Upcoming expirations</h3>
            <p v-if="upcoming.length === 0" class="empty">No items are expiring soon.</p>
            <ul v-else class="timeline">
                <li v-for="item in upcoming" :key="item.id">
                    <span class="timeline-date">{{ formatDate(item.expirationDate) }}</span>
                    <RouterLink :to="`/items/${item.id}`" class="timeline-card">
                        <strong>{{ item.name }}</strong>
                    </RouterLink>
                </li>
            </ul>
            <RouterLink to="/items" class="all-button">View all items</RouterLink>
        </section>

        <section v-if="adminDashboard" class="table-section">
            <h3>Department totals</h3>
            <div v-for="row in adminDashboard.itemsByDepartment" :key="row.departmentName" class="list-row">
                <span class="row-text">
                    <strong>{{ row.departmentName }}</strong>
                    <span class="share">
                        <span class="share-bar" :style="{ width: percent(row.itemCount, adminDashboard.totalItems) + '%' }"></span>
                    </span>
                </span>
                <span class="row-count">{{ row.itemCount }}</span>
            </div>
        </section>
        <section v-if="managerDashboard">
            <h3>Awaiting completion</h3>
            <p v-if="managerDashboard.requestsAwaitingCompletion.length === 0" class="empty">No requests awaiting completion.</p>
            <RouterLink v-for="request in managerDashboard.requestsAwaitingCompletion" :key="request.id" :to="`/requests/${request.id}`" class="list-row">
                <span class="row-text">
                    <strong>{{ request.itemName }}</strong>
                    <small>{{ request.requestedByUserName }}</small>
                </span>
            </RouterLink>
        </section>
        <template v-if="employeeDashboard">
            <section>
                <h3>Needs clarification</h3>
                <p v-if="employeeDashboard.requestsNeedingClarification.length === 0" class="empty">No requests needing clarification.</p>
                <RouterLink v-for="request in employeeDashboard.requestsNeedingClarification" :key="request.id" :to="`/requests/${request.id}`" class="list-row">
                    <span class="row-text">
                        <strong>{{ request.itemName }}</strong>
                        <small>Submitted {{ formatDateTime(request.submittedAt) }}</small>
                    </span>
                </RouterLink>
            </section>
            <section>
                <h3>Awaiting completion</h3>
                <p v-if="employeeDashboard.requestsAwaitingCompletion.length === 0" class="empty">No requests awaiting completion.</p>
                <RouterLink v-for="request in employeeDashboard.requestsAwaitingCompletion" :key="request.id" :to="`/requests/${request.id}`" class="list-row">
                    <span class="row-text">
                        <strong>{{ request.itemName }}</strong>
                        <small>New expiry date</small>
                    </span>
                </RouterLink>
            </section>
            <section>
                <h3>My open requests</h3>
                <p v-if="employeeDashboard.myOpenRequests.length === 0" class="empty">No open requests.</p>
                <RouterLink v-for="request in employeeDashboard.myOpenRequests" :key="request.id" :to="`/requests/${request.id}`" class="list-row">
                    <span class="row-text">
                        <strong>{{ request.itemName }}</strong>
                        <small>Submitted {{ formatDateTime(request.submittedAt) }}</small>
                    </span>
                </RouterLink>
            </section>
        </template>
    </aside>
    </div>
</template>
<style scoped>
.dashboard {
    display: grid;
    grid-template-columns: 1fr 300px;
    gap: 32px;
    align-items: start;
}

.dashboard.no-side {
    grid-template-columns: 1fr;
}

.main {
    min-width: 0;
}

h3 {
    margin: 0 0 12px;
}

.tiles {
    display: flex;
    flex-direction: column;
    gap: 16px;
    margin-bottom: 24px;
}

.tile {
    flex: 1;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 8px;
    padding: 10px 14px;
    border-radius: 10px;
    color: #111;
    text-decoration: none;
}

.tile:hover {
    opacity: 0.85;
}

.tile-label {
    font-size: 14px;
    font-weight: 600;
}

.tile-value {
    font-size: 32px;
}

.shade-5 { background-color: #3e81ec; }
.shade-4 { background-color: #618fd8; }
.shade-3 { background-color: #94bcfa; }
.shade-2 { background-color: #96b8ef; }
.shade-1 { background-color: #bcd2f6; }
.charts {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-bottom: 32px;
}

.chart-card {
    min-width: 0;
    padding: 20px;
    background-color: #f1f5fe;
}

.list-row {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 8px;
    padding: 10px 14px;
    background-color: #f5f2f2;
    color: #111;
    text-decoration: none;
}
.list-row:hover {
    background-color: #ebe7e0;
}

.row-text {
    display: flex;
    flex-direction: column;
    flex: 1;
    min-width: 0;
}

.row-text strong {
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.row-text small,
.timeline-card small,
.empty {
    color: white;
}

/* timeline */
.timeline {
    position: relative;
    margin: 0;
    padding: 0;
    list-style: none;
}

/* the vertical line */
.timeline::before {
    content: '';
    position: absolute;
    top: 0;
    bottom: 0;
    left: 72px;
    width: 1px;
    background-color: #d3cabb;
}

.timeline li {
    display: grid;
    grid-template-columns: 56px 1fr;
    gap: 32px;
    margin-bottom: 12px;
}

.timeline-date {
    padding-top: 10px;
    font-size: 12px;
}

.timeline-card {
    position: relative;
    display: flex;
    flex-direction: column;
    padding: 10px 14px;
    color: #111;
    text-decoration: none;
}

/* the dot on the line */
.timeline-card::before {
    content: '';
    position: absolute;
    top: 16px;
    left: -20px;
    width: 9px;
    height: 9px;
    border-radius: 50%;
    background-color: #000000;
}

.all-button {
    display: block;
    margin-top: 16px;
    padding: 12px;
    background-color: #000000;
    color: white;
    border-radius: 10px;
    text-align: center;
    text-decoration: none;
}

.tile-bottom {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 6px;
}


.tile-bottom small {
    font-size: 12px;
    opacity: 0.75;
}

.wide {
    grid-column: 1 / -1;
}

.table-section {
    margin-bottom: 32px;
}

.table-box {
    border: 1px solid rgb(247, 247, 247);
    border-radius: 12px;
    overflow-x: auto;
}

.table-box table {
    margin: 0;
}

.table-box th {
    background-color: #fcfbfb;
}

.table-link {
    font-weight: 600;
    white-space: nowrap;
}

.share {
    display: inline-block;
    width: 80px;
    height: 6px;
    margin-right: 8px;
    border-radius: 999px;
    background-color: white;
    vertical-align: middle;
}

.share-bar {
    display: block;
    height: 100%;
    border-radius: 999px;
    background-color: #2f61e2;
}

.side section + section {
    margin-top: 32px;
}

.row-text .share {
    width: 100%;
    margin: 6px 0 0;
}

.row-count {
    font-weight: 700;
}
/* keep this last so it wins */
@media (max-width: 1000px) {
    .dashboard,
    .charts
    .lists {
        grid-template-columns: 1fr;
    }

    .tiles {
        flex-wrap: wrap;
    }

    .tile {
        flex: 1 1 40%;
    }
}

</style>