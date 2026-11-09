<script setup lang="ts">
import { ref, onMounted } from 'vue'
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

const reportResponse = ref<ReportResponse | null>(null)
const errorMessage = ref('')
const loading = ref(true)

onMounted(async() => {
    const response = await fetch('/api/reports')
    if (response.ok) {
        reportResponse.value = await response.json()
    } else {
        errorMessage.value = 'Could not load the report.'
    }
    loading.value = false
})
</script>
<template>
    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>
    <div v-else-if="reportResponse">

    <h3>Number of items expiring in </h3>
    <table>
        <thead>
            <tr>
                <th>30 Days</th>
                <th>60 Days</th>
                <th>90 Days</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>{{ reportResponse.expiring.within30Days }} items</td>
                <td>{{ reportResponse.expiring.within60Days }} items</td>
                <td>{{ reportResponse.expiring.within90Days }} items</td>
            </tr>
        </tbody>
    </table>

    <table>
        <thead>
            <th>Category</th>
            <th>Number of Items</th>
            <th>Cost per category</th>
        </thead>
        <tbody>
            <tr v-for="row in reportResponse.categories" :key="row.categoryName">
                <td>{{ row.categoryName }}</td>
                <td>{{ row.itemCount }}</td>
                <td>{{ formatMoney(row.totalCost) }}</td>
            </tr>
        </tbody>
    </table>

    <table>
        <thead>
            <th>Providers</th>
            <th>Number of items</th>
        </thead>
        <tbody>
            <tr v-for="row in reportResponse.topProviders" :key="row.name">
                <td>{{ row.name }}</td>
                <td>{{ row.itemCount }}</td>
            </tr>
        </tbody>
    </table>

    <table>
        <thead>
            <th>Employee</th>
            <th>Number of items</th>
        </thead>
        <tbody>
            <tr v-for="row in reportResponse.topEmployees" :key="row.name">
                <td>{{ row.name }}</td>
                <td>{{ row.itemCount }}</td>
            </tr>
        </tbody>
    </table>
    </div>
</template>