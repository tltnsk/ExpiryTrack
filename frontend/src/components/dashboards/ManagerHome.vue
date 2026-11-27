<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { formatDate, formatDateTime, formatMoney} from '@/utils/format'
import {getTiles, getBarChart, getStateCounts, getMonthChart} from '@/utils/dashboard'
import type {  ManagerDashboard } from '@/utils/dashboard'
import BarChart from '@/components/BarChart.vue'
import StateChart from '@/components/StateChart.vue'

// an employee from the department
interface TeamEmployee {
    id: number
    firstName: string
    lastName: string
    email: string
}


const dashboard = ref<ManagerDashboard | null>(null)
const teamEmployees = ref<TeamEmployee[]>([])
const loading = ref(true)
const errorMessage = ref('')

const tiles = computed(() => getTiles(null, dashboard.value, null, null))
const barChart = computed(() => getBarChart(null, dashboard.value, null, null))
const stateCounts = computed(() => getStateCounts(null, dashboard.value, null, null))
const monthChart = computed(() => getMonthChart(null, dashboard.value, null, null))


onMounted(async () => {
    const response = await fetch('/api/dashboard/manager')
    if (response.ok) {
        dashboard.value = await response.json()
    } else {
        errorMessage.value = 'Could not load the dashboard.'
    }

    // the employees
    const employeeResponse = await fetch('/api/users/employees')
    if (employeeResponse.ok) {
        teamEmployees.value = await employeeResponse.json()
    }

    loading.value = false
})


</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>

    <div v-else-if="dashboard" class="dashboard">
        <div class="main">
            <div class="tiles">
                <RouterLink v-for="tile in tiles" :key="tile.label" :to="tile.link" class="tile" :class="tile.color">
                    <span class="tile-label">{{ tile.label }}</span>
                    <span class="tile-bottom">
                        <span class="tile-value">{{ tile.value }}</span>
                    </span>
                </RouterLink>
            </div>

        <section class="table-section">
            <h3>Requests to review</h3>
            <p v-if="dashboard.requestsToReview.length === 0" class="empty">No requests to review.</p>
            <div v-else class="table-box">
                <table>
                    <thead>
                        <tr><th>Item</th><th>Requested by</th><th>Submitted</th><th>New expiry</th><th>Cost</th><th></th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="request in dashboard.requestsToReview" :key="request.id">
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
    </div>
    <aside class="side">
        <section>
            <h3>Upcoming expirations</h3>
            <p v-if="dashboard.upcomingExpirations.length === 0" class="empty">No items are expiring soon.</p>
            <ul v-else class="timeline">
                <li v-for="item in dashboard.upcomingExpirations" :key="item.id">
                    <span class="timeline-date">{{ formatDate(item.expirationDate) }}</span>
                    <RouterLink :to="`/items/${item.id}`" class="timeline-card">
                        <strong>{{ item.name }}</strong>
                    </RouterLink>
                </li>
            </ul>
            <RouterLink to="/items" class="all-button">View all items</RouterLink>
        </section>

        <section>
            <h3>Awaiting completion</h3>
            <p v-if="dashboard.requestsAwaitingCompletion.length === 0" class="empty">No requests awaiting completion.</p>
            <RouterLink v-for="request in dashboard.requestsAwaitingCompletion" :key="request.id" :to="`/requests/${request.id}`" class="list-row">
                <span class="row-text">
                    <strong>{{ request.itemName }}</strong>
                    <small>{{ request.requestedByUserName }}</small>
                </span>
            </RouterLink>
        </section>
        <section>
            <h3>My employees</h3>
            <p v-if="teamEmployees.length === 0" class="empty">No employees in your department.</p>
            <RouterLink v-for="employee in teamEmployees" :key="employee.id"
                :to="{ path: '/items', query: { responsible: `${employee.firstName} ${employee.lastName}` } }" class="list-row">
                <span class="row-text">
                    <strong>{{ employee.firstName }} {{ employee.lastName }}</strong>
                    <small>{{ employee.email }}</small>
                </span>
            </RouterLink>
            <RouterLink to="/my-employees" class="see-all">See all employees →</RouterLink>
        </section>
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

.wide {
    grid-column: 1 / -1;
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
    color: black;
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

.side section + section {
    margin-top: 32px;
}


.table-box {
  background: white;
  border: 1px solid #eceef1;
  border-radius: 12px;
  overflow: hidden;
}

table {
  margin: 0;
}

th, td {
  padding: 18px 20px;
  border: none;
  border-bottom: 1px solid #eceef1;
}

th {
  background: #f9fafb;
}

th + th {
  border-left: 1px solid #eceef1;
}

tbody tr:last-child td {
  border-bottom: none;
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