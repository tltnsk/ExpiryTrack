<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

interface Item {
    id: number
    name: string
    description: string | null
    provider: string | null
    referenceNumber: string | null
    lifecycleState: string
    categoryName: string
    departmentName: string
    responsibleUserName: string
    periodNumber: string
    startDate: string 
    expirationDate: string
    cost: number
}


const router = useRouter()
const route = useRoute()
const id = route.params.id

const item = ref<Item |null>(null)
const loading = ref(true)
const errorMessage = ref('')
const saveError = ref('')
const saving = ref(false)

// only these descriptive fields can be edited
const name = ref('')
const description = ref('')
const provider = ref('')
const referenceNumber = ref('')

onMounted(async () => {
    const response = await fetch(`/api/items/${id}`) 
    if (response.ok) {
        const data: Item = await response.json()
        item.value = data
        name.value = data.name
        description.value = data.description ?? ''
        provider.value = data.provider ?? ''
        referenceNumber.value = data.referenceNumber ?? ''
    } else if (response.status === 404 || response.status === 400) {
        errorMessage.value = 'Item not found.'
    } else {
        errorMessage.value = `Could not load item (error ${response.status}).`
    }

    loading.value = false
})

async function editItem() {
  errorMessage.value = ''
  saving.value = true

  const response = await fetch(`/api/items/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      name: name.value,
      description: description.value || null,
      provider: provider.value || null,
      referenceNumber: referenceNumber.value || null,
    }),
  })

  saving.value = false

  if (response.ok) {
    router.push(`/items/${id}`)
  } else if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      // [Required], [MaxLength] from the DTO: the messages are inside "errors"
      const problem = await response.json()
      saveError.value = Object.values(problem.errors).flat().join(' ')
    } else {
      // BadRequest("...") from the controller: the message is plain text
      saveError.value = await response.text()
    }
  } else {
    saveError.value = `Could not edit the item (error ${response.status}).`
  }
}
</script>

<template>
    <RouterLink to="/items">Back to items</RouterLink>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage">{{ errorMessage }}</p>

    <div v-else-if="item">
    <h2>Edit item</h2>

    <form @submit.prevent="editItem">
      <div>
      <label>
        Name
        <input v-model="name" required maxlength="150" />
      </label>
       </div>
        <div>
      <label>
        Description
        <textarea v-model="description" rows="3"></textarea>
      </label>
    </div>
    <div>
      <label>
        Provider
        <input v-model="provider" maxlength="150" />
      </label>
    </div>
    <div>
      <label>
        Reference number
        <input v-model="referenceNumber" maxlength="100" />
      </label>
    </div>
    <div>
      <label>
        Start date
        <input :value="item.startDate" type="date" readonly />
      </label>
    </div>
    <div>
      <label>
        Expiration date
        <input :value="item.expirationDate" type="date" readonly />
      </label>
    </div>
    <div>
      <label>
        Cost
        <input :value="item.cost" readonly />
      </label>
    </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Save' }}</button>
    <p v-if="saveError" class="error">{{ saveError }}</p>
    </form>
    </div>
</template>