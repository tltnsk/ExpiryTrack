<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { formatDate, formatDateTime, formatLabel, formatMoney} from '@/utils/format'

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
    <div class="page-top">
        <h2>Requests</h2>
    </div>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>
    <p v-else-if="requests.length===0">No renewal requests.</p>

    <div v-else class="table-box">
        <table>
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
                    <td> <RouterLink :to="`/requests/${request.id}`" class="item-link">{{ request.itemName }}</RouterLink></td>
                    <td><span class="state" :class="request.status">{{ formatLabel(request.status) }}</span></td>
                    <td>{{formatDate(request.proposedExpirationDate )}}</td>
                    <td>{{ formatMoney(request.proposedCost)  }}</td>
                    <td>{{ formatDateTime(request.submittedAt)}}</td>
                </tr>
            </tbody>
        </table>
    </div>
</template>
<style scoped>
.page-top{
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-top h2 {
  margin: 0;
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

.item-link {
    font-weight: 500;
    color: inherit;
    text-decoration: none;
}

.item-link:hover{
    text-decoration: underline;
}
</style>
