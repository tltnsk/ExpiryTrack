<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

interface RequestAction{
    type: string
    fromStatus: string | null
    toStatus: string
    recommendation: string | null
    comment: string | null
    createdAt: string
    actorUserId: number | null
    actorUserName: string
}

interface RenewalRequest {
    id: number
    itemId: number
    itemName: string
    status: string
    needsFinancialReview: boolean
    proposedExpirationDate: string
    proposedCost: number | null
    justification: string | null
    submittedAt: string 
    decidedAt: string | null
    completedAt: string | null
    periodId: number
    requestedByUserId: number
    actions: RequestAction[]
}

const route = useRoute()
const auth = useAuthStore()
const id = route.params.id

const request = ref<RenewalRequest | null>(null)

const errorMessage = ref('')
const loading = ref(true)

onMounted(async() => {
    const response = await fetch(`/api/requests/${id}`)
    if (response.ok) {
        const data: RenewalRequest = await response.json()
        request.value = data
    } else if (response.status === 404 || response.status === 400) {
        errorMessage.value = 'Item not found.'
    } else {
        errorMessage.value = `Could not load item (error ${response.status}).`
    }
    loading.value = false
})
</script>

<template>
    <RouterLink to="/requests">Back to Renewal Requests</RouterLink>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage"> {{  errorMessage }}</p>

    <div v-else-if="request">
        <h3>Details</h3>
        <dl>
            <dt>Status</dt>
            <dd>{{ request.status}}</dd>
            <dt>Proposed Expiration Date</dt>
            <dd>{{ request.proposedExpirationDate }}</dd>
            <dt>Proposed Cost</dt>
            <dd>{{ request.proposedCost ?? '-' }}</dd>
            <dt>Justification</dt>
            <dd>{{ request.justification ?? '-' }}</dd>
            <dt>Submitted At</dt>
            <dd>{{ request.submittedAt }}</dd>
            <dt>Decided At</dt>
            <dd>{{ request.decidedAt ?? '-' }}</dd>
            <dt>Completed At</dt>
            <dd>{{ request.completedAt ?? '-' }}</dd>
        </dl>

    <table>
        <thead>
            <tr>
              <th>When</th>
              <th>Who</th>
              <th>Step</th>
              <th>Status</th>
              <th>Recommendation</th>
              <th>Comment</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(action, index) in request.actions" :key="index">
                <td>{{action.createdAt }}</td>
                <td>{{ action.actorUserName}}</td>
                <td>{{ action.type }}</td>
                <td>{{ action.fromStatus ?? '-' }} -> {{ action.toStatus }}</td>
                <td>{{ action.recommendation ?? '-' }}</td>
                <td>{{ action.comment  ?? '-'}}</td>
            </tr>
        </tbody>
    </table>
    </div>
</template>