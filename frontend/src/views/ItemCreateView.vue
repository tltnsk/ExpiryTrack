<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { checkPdf } from '@/utils/file'

// CategoryResponse
interface Category {
    id: number,
    name: string,
    warningPeriodDays: number,
    isActive: boolean
}

interface User {
  id: number
  firstName: string
  lastName: string
}

const router = useRouter()

const categories = ref<Category[]>([])
const name = ref('')
const categoryId = ref<number | ''>('')
const description = ref('')
const provider = ref('')
const referenceNumber = ref('')
const startDate = ref('')
const expirationDate = ref('')
const cost = ref('')
const responsibleUserId = ref<number | ''>('')

const errorMessage = ref('')
const saving = ref(false)

const errors = ref<Record<string,string>>({})

function onInvalid(event: Event) {
  const input = event.target as HTMLInputElement
  event.preventDefault() 
  errors.value[input.name] = input.validity.valueMissing
      ? 'This field is required.'
      : input.validationMessage
}

const auth = useAuthStore()
const employees = ref<User[]>([])

onMounted(async () => {
    const response = await fetch('/api/categories')
    if (response.ok) {
        const all: Category[] = await response.json()
        categories.value = all.filter((c) => c.isActive)
    } else {
        errorMessage.value = 'Could not load the categories.'
    }
    if (auth.user?.role === 'DepartmentManager') {
      const employeesResponse = await fetch('/api/users/employees')
      if (employeesResponse.ok) {
        employees.value = await employeesResponse.json()
      } else {
        errorMessage.value = 'Could not load the employees.'
      }
    }
})

const selectedFile = ref<File | null>(null)

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    selectedFile.value = input.files[0] ?? null
  } else {
    selectedFile.value = null
  }
}

async function createItem() {
  errorMessage.value = ''

  // check the file first, so the item is not created with a file that the server would reject
  if (selectedFile.value) {
    const fileError = await checkPdf(selectedFile.value)
    if (fileError) {
      errorMessage.value = fileError
      return 
    }
  }
  saving.value = true

  const response = await fetch('/api/items', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      name: name.value,
      categoryId: categoryId.value === '' ? null : categoryId.value,
      description: description.value || null,
      provider: provider.value || null,
      referenceNumber: referenceNumber.value || null,
      startDate: startDate.value,
      expirationDate: expirationDate.value,
      cost: cost.value === '' ? null : Number(cost.value),
      responsibleUserId: responsibleUserId.value === '' ? null : responsibleUserId.value
    }),
  })

  if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = await response.json()
      errorMessage.value = Object.values(problem.errors).flat().join(' ')
    } else {
      errorMessage.value = await response.text()
    }
    saving.value = false
    return
  }
  if (!response.ok) {
    errorMessage.value = `Could not create the item (error ${response.status}).`
    saving.value = false
    return
  }

  const created = await response.json()

  if (selectedFile.value) {
    // form data is a built in browser object for sending data the way an HTML form does
    // as a list of name value pairs
    const formData = new FormData()
    formData.append('file', selectedFile.value)

    const documentResponse = await fetch(`/api/items/${created.id}/documents`, {
      method: 'POST',
      body: formData
    })

    if (!documentResponse.ok) {
      alert('Item created, but the document could not be uploaded: ' + await documentResponse.text())
    }
  }

  saving.value = false
  router.push(`/items/${created.id}`)
}
</script>

<template>
    <h2>New item</h2>

    <form @submit.prevent="createItem" @invalid.capture="onInvalid" @change="errors = {}">
      <div>
      <label>
        Name <span class="required">*</span>
        <input v-model="name" name="name" required maxlength="150" />
      </label>
      <p v-if="errors.name" class="field-error">{{ errors.name }}</p>
       </div>
        <div>
            <label>
                Category <span class="required">*</span>
                <select v-model="categoryId" name="categoryId" required>
                    <option value="" disabled>Select a category</option>
                    <option v-for="category in categories" :key="category.id" :value="category.id">
                        {{ category.name }}
                    </option>
                </select>
            </label>
            <p v-if="errors.categoryId" class="field-error">{{ errors.categoryId }}</p>
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
        Start date <span class="required">*</span>
        <input v-model="startDate" name="startDate" type="date" required />
      </label>
      <p v-if="errors.startDate" class="field-error">{{ errors.startDate }}</p>
    </div>
    <div>
      <label>
        Expiration date <span class="required">*</span>
        <input v-model="expirationDate" name="expirationDate" type="date" required />
      </label>
      <p v-if="errors.expirationDate" class="field-error">{{ errors.expirationDate }}</p>
    </div>
    <div>
      <label>
        Cost
        <input v-model="cost" name="cost" type="number" min="0.01" step="0.01" placeholder="Leave empty if free" />
      </label>
      <p v-if="errors.cost" class="field-error">{{ errors.cost }}</p>
    </div>
    <div v-if="auth.user?.role === 'DepartmentManager'">
      <label>
        Responsible Employee <span class="required">*</span>
        <select v-model="responsibleUserId" name="responsibleUserId" required>
          <option value="" disabled>Select a responsible user</option>
          <option v-if="employees.length===0" value="" disabled>No active employees in your department.</option>
          <option v-for="employee in employees" :key="employee.id" :value="employee.id">
            {{ employee.firstName }} {{  employee.lastName }}
          </option>
    
        </select>
      </label>
      <p v-if="errors.responsibleUserId" class="field-error">{{ errors.responsibleUserId }}</p>
    </div>
    <div>
      <label>
        Attach a file
        <input type="file" accept="application/pdf" @change="onFileChange"/>
      </label>
    </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Create item' }}</button>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    </form>
</template>