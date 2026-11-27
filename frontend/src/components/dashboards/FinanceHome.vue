<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { formatDate, formatMoney} from '@/utils/format'
import {getTiles, percent, getStateCounts, getMonthChart} from '@/utils/dashboard'
import type { FinanceDashboard } from '@/utils/dashboard'
import BarChart from '@/components/BarChart.vue'
import StateChart from '@/components/StateChart.vue'

const dashboard = ref<FinanceDashboard | null>(null)
const loading = ref(true)
const errorMessage = ref('')

const tiles = computed(() => getTiles(null, null, null, dashboard.value))
const stateCounts = computed(() => getStateCounts(null, null, null, dashboard.value))
const monthChart = computed(() => getMonthChart(null, null, null, dashboard.value))

onMounted(async () => {
        const response = await fetch('/api/dashboard/finance')
        if (response.ok) {
            dashboard.value = await response.json()
        } else {
            errorMessage.value = 'Could not load the dashboard.'
        }
    loading.value = false
})

// how many days the request has been waiting 
function daysWaiting(submittedAt: string) {
    return Math.floor((Date.now() - new Date(submittedAt).getTime()) / 86400000)
}

function waitingText(submittedAt: string) {
    const days = daysWaiting(submittedAt)
    if (days === 0) return 'Today'
    if (days === 1) return '1 day'
    return `${days} days`
}

// cost of all departments together
const totalDepartmentCost = computed(() => {
    if(!dashboard.value) return 0
    let total = 0
    for (const row of dashboard.value.costsByDepartment) {
        total += row.value
    }
    return total
})
</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div v-else-if="dashboard" class="home-grid">
        <div class="main-column">
            <section class="box">
                <h3>Requests to review</h3>
                <p v-if="dashboard.requestsNeedingFinancialReview.length === 0" class="empty">No requests to review.</p>
                <div v-else class="table-box">
                    <table class="requests">
                        <thead>
                            <tr><th>Item</th><th>Requested by</th><th>Waiting</th><th>New expiry</th><th>Cost</th><th></th></tr>
                        </thead>
                        <tbody>
                            <tr v-for="request in dashboard.requestsNeedingFinancialReview" :key="request.id">
                                <td>{{ request.itemName }}</td>
                                <td>{{ request.requestedByUserName }}</td>
                                <td :class="{ late: daysWaiting(request.submittedAt) > 3 }">{{ waitingText(request.submittedAt) }}</td>
                                <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                                <td>{{ formatMoney(request.proposedCost) }}</td>
                                <td><RouterLink :to="`/requests/${request.id}`" class="table-link">Review →</RouterLink></td>
                            </tr>
                        </tbody>
                        <tfoot>
                            <tr class="total-row">
                                <td colspan="4">Total</td>
                                <td>{{ formatMoney(dashboard.totalCostWaitingForReview) }}</td>
                                <td></td>
                            </tr>
                        </tfoot>
                    </table>
                </div>
            </section>

            <div class="charts">
                <section class="box">
                    <h3>Items with a cost</h3>
                    <div class="donut">
                        <StateChart :values="stateCounts" :aspect-ratio="0.8" legend-position="bottom" />
                    </div>
                </section>
                <section class="box">
                    <h3>Expirations in the next 12 months</h3>
                    <BarChart :labels="monthChart.labels" :values="monthChart.values" />
                </section>
            </div>
        </div>

        <aside class="side-column">
            <RouterLink v-for="tile in tiles" :key="tile.label" :to="tile.link" class="stat-card" :class="tile.color">
                <span class="stat-label">{{ tile.label }}</span>
                <strong class="stat-value">{{ tile.value }}</strong>
            </RouterLink>

            <section class="box">
                <h3>Costs by department</h3>
                <p v-if="dashboard.costsByDepartment.length === 0" class="empty">No costs yet.</p>
                <div v-for="row in dashboard.costsByDepartment" :key="row.label" class="department-row">
                    <div class="department-text">
                        <span>{{ row.label }}</span>
                        <strong>{{ formatMoney(row.value) }}</strong>
                    </div>
                    <small>{{ percent(row.value, totalDepartmentCost) }}% of total</small>
                </div>
            </section>
        </aside>
    </div>
</template>
<style scoped>
.home-grid {
    display: grid;
    grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
    gap: 20px;
    align-items: start;
}

.main-column,
.side-column {
    display: flex;
    flex-direction: column;
    gap: 16px;
    min-width: 0;
}

.box {
    padding: 16px 20px;
    border: 1px solid var(--border);
}

.box h3 {
    margin: 0 0 12px;
}

.empty {
    color: #6b6b6b;
}

.requests {
    margin: 0;
    border: none;
}

.requests th,
.requests td {
    padding: 10px 8px;
    border: none;
    border-bottom: 1px solid #eceef1;
}

.late {
    color: var(--danger);
    font-weight: 600;
}

.total-row td {
    border-bottom: none;
    font-weight: 700;
}

.charts {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.donut {
    max-width: 240px;
    margin: 0 auto;
}

.stat-card {
    display: grid;
    grid-template-columns: 1fr auto;
    align-items: center;
    padding: 14px 16px;
    color: #111;
    text-decoration: none;
}

.stat-card:hover {
    opacity: 0.9;
}

.stat-label {
    display: flex;
    font-weight: 600;
}

.stat-value {
    font-size: 1.4em;
}

.shade-5 { background-color: #3e81ec; }
.shade-4 { background-color: #618fd8; }
.shade-3 { background-color: #94bcfa; }
.shade-2 { background-color: #bcd2f6; }

.department-row {
    padding: 8px 0;
}

.department-text {
    display: flex;
    justify-content: space-between;
    font-size: 1.3em;
}

.table-box {
    overflow-x: auto;
}

@media (max-width: 1100px) {
    .home-grid,
    .charts {
        grid-template-columns: 1fr;
    }

    .side-column {
        order: -1;
    }
}
</style>