<script setup lang="ts">
import { ref, onMounted } from 'vue'

interface RenewalRequest {
    id: number
    itemName: string
    status: string
    proposedExpirationDate: string,
    proposedCost: number | null,
    submittedAt: string 
}

const requests = ref<RenewalRequest[]>([])
const loading = ref(true)
const errorMessage = ref('')

onMounted(async() => {
    const response = await fetch('/api/requests')
    if(response.ok) {
        requests.value = await response.json()
    } else {
        errorMessage.value = `Could not load requests (error ${response.status}).`
    }
    loading.value = false
})
</script>

<template>
    <h2>Requests</h2>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage"> {{ errorMessage }}</p>
    <p v-else-if="requests.length===0">No renewal requests.</p>

    <table v-else>
        <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              <th>Proposed Expiration Date</th>
              <th>Proposed Cost</th>
              <th>Submitted At</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="request in requests" :key="request.id">
                <td> <RouterLink :to="`/requests/${request.id}`">{{ request.itemName }}</RouterLink></td>
                <td>{{ request.status }}</td>
                <td>{{request.proposedExpirationDate }}</td>
                <td>{{ request.proposedCost ?? '-' }}</td>
                <td>{{ request.submittedAt}}</td>
            </tr>
        </tbody>
    </table>
</template>