<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface Item {
    name: string,
    expirationDate: string,
    lifecycleState: string
}

const item = ref<Item | null>(null)
const name = ref('')
const expirationDate = ref('')
const lifecycleState = ref('')

const route = useRoute()
const router = useRouter()
const id = route.params.id

const proposedExpirationDate = ref('')
const proposedCost = ref('')
const justification = ref('')

const errorMessage = ref('')
const saving = ref(false)
const saveError = ref('')
const loading = ref(true)

onMounted(async () => {
    const response = await fetch(`/api/items/${id}`)
    if (response.ok) {
        const data: Item = await response.json()
        item.value = data
    } else if (response.status === 404) {
        errorMessage.value = 'Item not found.'
    } else {
        errorMessage.value = `Could not load item (error ${response.status}).`
    }
    loading.value = false
})

async function submitRequest() {
    saveError.value = ''
    saving.value = true
    
    const response = await fetch(`/api/items/${id}/requests`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            proposedExpirationDate: proposedExpirationDate.value,
            proposedCost: proposedCost.value || null,
            justification: justification.value 
        }),
    })

    saving.value = false 

    if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = await response.json()
      errorMessage.value = Object.values(problem.errors).flat().join(' ')
    } else {
      errorMessage.value = await response.text()
    }
    return
  }
  if (!response.ok) {
    errorMessage.value = `Could not submit the request (error ${response.status}).`
    return
  }

  const data = await response.json()
  router.push(`/requests/${data.id}`)
}

</script>

<template>
  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>

  <div v-else-if="item">
    <h2>Submit Renewal Request</h2>
    <p>Renewal for {{ item.name }}, currently valid until {{ item.expirationDate }}</p>

    <form @submit.prevent="submitRequest">
    <div>
      <label>
          Proposed Expiration Date
          <input v-if="item" v-model="proposedExpirationDate" required type="date" :min="item.expirationDate"/>
      </label>
    </div>
    <div>
      <label>
          Proposed Cost (optional)
          <input v-model="proposedCost" type="number" step="0.01" min="0.01"/>
      </label>
    </div>
    <div>
      <label>
        Justification
          <textarea v-model="justification" required  />
      </label>
    </div>
      <button type="submit" :disabled="saving">{{ saving ? 'Submitting...' : 'Submit' }}</button>
      <p v-if="saveError" class="error">{{ saveError }}</p>
    </form>
  </div>

</template> 