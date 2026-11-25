<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { formatDate, formatDateTime, formatLabel, formatMoney} from '@/utils/format'
import { useSort } from '@/utils/sort'

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

const search = ref('')
const tab = ref<'all' | 'open' | 'finished'>('all')

// requests that are not finished yet
const openStatuses = ['PendingFinancialReview', 'PendingManagerApproval', 'ClarificationRequired', 'Approved']

function isOpen(request: RenewalRequest) {
  return openStatuses.includes(request.status)
}

const openCount = computed(() => requests.value.filter(isOpen).length)
const finishedCount = computed(() => requests.value.length - openCount.value)


// the chosen tab and the search text
const filteredRequests = computed(() => {
  const text = search.value.trim().toLowerCase()
  return requests.value.filter((request) => {
    if (tab.value === 'open' && !isOpen(request)) return false
    if (tab.value === 'finished' && isOpen(request)) return false
    return request.itemName.toLowerCase().includes(text)
  })
})
onMounted(async() => {
    const response = await fetch('/api/requests')
    if(response.ok) {
        requests.value = await response.json()
    } else {
        errorMessage.value = `Could not load requests (error ${response.status}).`
    }
    loading.value = false
})

const { sorted, sortBy, arrow } = useSort(filteredRequests, 'submittedAt', false)
</script>

<template>
    <div class="page-top">
        <h2>Requests</h2>
    </div>
    <div class="toolbar">
        <div class="tabs">
            <button type="button" :class="{active: tab === 'all'}" @click="tab = 'all'">All ({{ requests.length }})</button>
            <button type="button" :class="{active: tab === 'open'}" @click="tab = 'open'">Open ({{ openCount }})</button>
            <button type="button" :class="{active: tab === 'finished'}" @click="tab = 'finished'">Finished ({{ finishedCount }})</button>
        </div> 
        <input v-model="search" type="search" class="search" placeholder="Search by item" />
    </div>

    <p v-if="loading">Loading...</p>
    <p v-else-if="errorMessage" class="error"> {{ errorMessage }}</p>
    <p v-else-if="requests.length===0">No renewal requests.</p>
    <p v-else-if="filteredRequests.length===0">No requests match.</p>
    
    <div v-else class="table-box">
        <table>
            <thead>
                <tr>
                  <th class="sortable" @click="sortBy('itemName')">Name {{ arrow('itemName') }}</th>
                  <th class="sortable" @click="sortBy('status')">Status {{ arrow('status') }}</th>
                  <th class="sortable" @click="sortBy('proposedExpirationDate')">Proposed Expiration Date {{ arrow('proposedExpirationDate') }}</th>
                  <th class="sortable" @click="sortBy('proposedCost')">Proposed Cost {{ arrow('proposedCost') }}</th>
                  <th class="sortable" @click="sortBy('submittedAt')">Submitted At {{ arrow('submittedAt') }}</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="request in sorted" :key="request.id">
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

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
    margin-bottom: 16px;
}

.tabs {
    display: flex;
    gap: 4px;
}

.tabs button {
    margin: 0;
    padding: 5px 15px;
    background: transparent;
    color: var(--text);
}

.tabs button.active {
    background: var(--primary);
    color: white;
}

.search {
    width: 280px;
    margin: 0;
}
</style>
