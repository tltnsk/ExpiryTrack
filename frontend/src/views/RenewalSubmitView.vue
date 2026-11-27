<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { formatDate } from '@/utils/format'
import { checkPdf } from '@/utils/file'

interface Item {
    name: string,
    expirationDate: string,
    lifecycleState: string
}

const item = ref<Item | null>(null)
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

const errors = ref<Record<string, string>>({})

function onInvalid(event: Event) {
    const input = event.target as HTMLInputElement
    event.preventDefault()
    errors.value[input.name] = input.validity.valueMissing
        ? 'This field is required.'
        : input.validationMessage 
}

const selectedFile = ref<File | null>(null)

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    selectedFile.value = input.files[0] ?? null
  } else {
    selectedFile.value = null
  }
}

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
    if (selectedFile.value) {
      const fileError = await checkPdf(selectedFile.value)
      if (fileError) {
        saveError.value = fileError
        return 
      }
    }
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

    if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = await response.json()
      saveError.value = Object.values(problem.errors).flat().join(' ')
    } else {
      saveError.value = await response.text()
    }
    saving.value = false
    return
  }
  if (!response.ok) {
    saveError.value = `Could not submit the request (error ${response.status}).`
    saving.value = false
    return
  }

    const created = await response.json()

  if (selectedFile.value) {
    // form data is a built in browser object for sending data the way an HTML form does
    // as a list of name value pairs
    const formData = new FormData()
    formData.append('file', selectedFile.value)

    const documentResponse = await fetch(`/api/requests/${created.id}/documents`, {
      method: 'POST',
      body: formData
    })

    if (!documentResponse.ok) {
      alert('Request created, but the document could not be uploaded: ' + await documentResponse.text())
    }
  }

  saving.value = false
  router.push(`/requests/${created.id}`)
}

</script>

<template>
  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>

  <div v-else-if="item">
    <h2>Submit Renewal Request</h2>
    <p>Renewal for {{ item.name }}, currently valid until {{ formatDate(item.expirationDate) }}</p>

    <form @submit.prevent="submitRequest" @invalid.capture="onInvalid" @change="errors = {}">
    <div>
      <label>
          Proposed Expiration Date <span class="required">*</span>
          <input v-model="proposedExpirationDate" name="proposedExpirationDate" required type="date" :min="item.expirationDate"/>
      </label>
      <p v-if="errors.proposedExpirationDate" class="field-error">{{ errors.proposedExpirationDate }}</p>
    </div>
    <div>
      <label>
          Proposed Cost (required if the category needs financial review)
          <input v-model="proposedCost" name="proposedCost" type="number" step="0.01" min="0.01"/>
      </label>
      <p v-if="errors.proposedCost" class="field-error">{{ errors.proposedCost }}</p>
    </div>
    <div>
      <label>
        Justification <span class="required">*</span>
          <textarea v-model="justification" name="justification" required  />
      </label>
      <p v-if="errors.justification" class="field-error">{{ errors.justification }}</p>
    </div>
    <div>
      <label>
        Attach a file
        <input type="file" accept="application/pdf" @change="onFileChange"/>
      </label>
    </div>
      <button type="submit" :disabled="saving">{{ saving ? 'Submitting...' : 'Submit' }}</button>
      <p v-if="saveError" class="error">{{ saveError }}</p>
    </form>
  </div>

</template> 