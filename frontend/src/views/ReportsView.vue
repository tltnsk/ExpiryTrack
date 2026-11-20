<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import {formatMoney} from '@/utils/format'

interface CategoryReportRow {
    categoryName: string
    itemCount: number
    totalCost: number | null
}

interface ExpiringReport {
    within30Days: number
    within60Days: number
    within90Days: number
}

interface TopReportRow {
    name: string
    itemCount: number
}

interface ReportResponse {
    expiring: ExpiringReport
    categories: CategoryReportRow[]
    topProviders: TopReportRow[]
    topEmployees: TopReportRow[]
}

const report = ref<ReportResponse | null>(null)
const errorMessage = ref('')
const loading = ref(true)


const showAllProviders = ref(false)
const showAllEmployees = ref(false)

const sortBy = ref<'items' | 'cost'>('items')

onMounted(async() => {
    const response = await fetch('/api/reports')
    if (response.ok) {
        report.value = await response.json()
    } else {
        errorMessage.value = `Could not load the report (error ${response.status}).`
    }
    loading.value = false
})

const totalExpiring = computed(() => {
    if(!report.value) return 0
    const e = report.value.expiring
    return e.within30Days + e.within60Days + e.within90Days
})

const shownProviders = computed(() => {
    const list = report.value?.topProviders ?? []
    return showAllProviders.value ? list : list.slice(0, 3)
})

const shownEmployees = computed(() => {
    const list = report.value?.topEmployees ?? []
    return showAllEmployees.value ? list : list.slice(0, 3)
})

// the categories table sorted by the selected tab
const sortedCategories = computed(() => {
    if (!report.value) return []
    return [...report.value.categories].sort((a,b) =>
        sortBy.value === 'items'
            ? b.itemCount - a.itemCount
            : (b.totalCost ?? 0) - (a.totalCost ?? 0))
})

const totalItems = computed(() =>
    report.value?.categories.reduce((sum, c) => sum + c.itemCount, 0) ?? 0
)

const totalCost = computed(() =>
    report.value?.categories.reduce((sum, c) => sum + (c.totalCost ?? 0), 0) ?? 0
)
</script>
<template>
    <h2>Reports</h2>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>
    <div v-else-if="report">
        <div class="cards">
            <div class="report-card">
                <div class="card-title">
                    <h3>Expiring soon</h3>
                </div>
                <p class="label">Items expiring in the next 90 days</p>
                <p class="value">{{ totalExpiring }}</p>
                <table class="mini-table">
                    <thead>
                        <tr><th>Period</th><th>Items</th></tr>
                    </thead>
                    <tbody>
                        <tr><td>0 - 30 days</td><td>{{ report.expiring.within30Days }}</td></tr>
                        <tr><td>31 - 60 days</td><td>{{ report.expiring.within60Days }}</td></tr>
                        <tr><td>61 - 90 days</td><td>{{ report.expiring.within90Days }}</td></tr>
                    </tbody>
                </table>
            </div>

            <div class="report-card">
                <div class="card-title">
                    <h3>Top providers</h3>
                </div>
                <p class="label">Provider with the most items</p>
                <p class="value">{{ report.topProviders[0]?.name ?? '-' }}</p>
                <p v-if="report.topProviders.length === 0" class="empty">No providers yet.</p>
                <table v-else class="mini-table" :class="{ faded: report.topProviders.length > 3 && !showAllProviders }">
                    <thead>
                        <tr><th>Provider</th><th>Items</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="row in shownProviders" :key="row.name">
                            <td>{{ row.name }}</td>
                            <td>{{ row.itemCount }}</td>
                        </tr>
                    </tbody>
                </table>
                <button v-if="report.topProviders.length > 3" type="button" class="more" :class="{open: showAllProviders}" @click="showAllProviders = !showAllProviders">
                    {{ showAllProviders ? 'Show less' : ` ⤢ View ${report.topProviders.length - 3} more` }}
                </button>
            </div>

            <div class="report-card">
                <div class="card-title">
                    <h3>Top employees</h3>
                </div>
                <p class="label">Employee with the most items</p>
                <p class="value">{{ report.topEmployees[0]?.name ?? '-' }}</p>
                <p v-if="report.topEmployees.length === 0" class="empty">No employees yet.</p>
                <table v-else class="mini-table" :class="{ faded: report.topEmployees.length > 3 && !showAllEmployees }">
                    <thead>
                        <tr><th>Employee</th><th>Items</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="row in shownEmployees" :key="row.name">
                            <td>{{ row.name }}</td>
                            <td>{{ row.itemCount }}</td>
                        </tr>
                    </tbody>
                </table>
                <button v-if="report.topEmployees.length > 3" type="button" class="more" :class="{open: showAllEmployees}" @click="showAllEmployees = !showAllEmployees">
                    {{ showAllEmployees ? 'Show less' : ` ⤢ View ${report.topEmployees.length - 3} more` }}
                </button>
            </div>
        </div>

        <div class="table-heading">
            <h3>Items and costs by category</h3>
        <div class="sort">
            <span class="sort-label">Sort by</span>
            <button type="button" :class="{active: sortBy === 'items'}" @click="sortBy = 'items'">Most items</button>
            <button type="button" :class="{active: sortBy === 'cost'}" @click="sortBy = 'cost'">Highest cost</button>
        </div>
        </div>
        <table class="report-table">
            <thead>
                <tr>
                    <th>Category</th>
                    <th>Items</th>
                    <th>Total cost</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="row in sortedCategories" :key="row.categoryName">
                    <td>{{row.categoryName}}</td>
                    <td>{{row.itemCount}}</td>
                    <td>{{formatMoney(row.totalCost)}}</td>
                </tr>
            </tbody>
            <tfoot>
                <tr>
                    <td>Total</td>
                    <td>{{totalItems}}</td>
                    <td>{{ formatMoney(totalCost) }}</td>
                </tr>
            </tfoot>
        </table>
    </div>
</template>
<style scoped>
.cards {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
    margin-bottom: 40px;
}

.report-card {
    position: relative;
    overflow: hidden;
    padding: 20px 24px;
    border: 1px solid rgb(224, 216, 216);
    box-shadow: 0 5px 5px rgba(0,0,0,0.04);
    background-color: rgba(251, 253, 251, 0.929);
}


.card-title {
    display: flex;
    align-items: center;
    gap: 10px;
}

.mini-table, .report-table {
    border-collapse: separate;
    border-spacing: 0;
    background-color: rgba(251, 253, 251, 0.929);
}

.mini-table {
    margin: 16px 0 0;
}

.mini-table th, .mini-table td {
    padding: 10px 14px;
    border: none;
}

.report-table {
    margin-top: 16px;
}

.report-table th, .report-table td {
    padding: 14px 18px;
    border: none;
}

.mini-table th:first-child,
.report-table th:first-child {border-radius: 10px 0 0 10px;}

.mini-table th:last-child,
.report-table th:last-child {border-radius: 0 10px 10px 0;}

.mini-table th:last-child,
.mini-table td:last-child {
    text-align: right;
}

.mini-table tbody tr:hover {
    background: none;
}

.faded tbody tr:last-child td {
    opacity: 0.35;
}

.more {
    position: relative;
    display: block;
    margin: -34px auto 0;
    padding: 6px 12px;
    background: white;
    color: var(--text);
    border: 1px solid #d1d5db;
    border-radius: 8px;
}

.more.open {
    margin-top: 12px;
}

.more:hover {
    opacity: 1;
    border-color: var(--text);
}

.table-heading {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 16px;
}

.table-heading h3 {
    margin: 0;
}

.sort {
    align-items: center;
    gap: 4px;
    padding: 4px;
}

.sort-label {
    padding: 0 8px;
}

.sort button {
    margin: 0;
    padding: 6px 12px;
    background: #e5e7eb;
    color: var(--text);
    border-radius: 8px;
    font-size: 0.9em;
}

.sort button.active {
    background-color: black;
    color: white;
}

.report-table tfoot td {
    border-top: 1px solid #eceef1;
    font-weight: 600;
}

@media (max-width: 900px) {
    .cards { grid-template-columns: 1fr;}
}
</style>
