<script setup lang="ts">
import { ref } from 'vue'

const errorMessage = ref('')
const selectedFile = ref<File | null>(null)
const errors = ref<string[]>([])
const createdCount = ref<number | null>(null)
const importing = ref(false)

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    selectedFile.value = input.files[0] ?? null
  } else {
    selectedFile.value = null
  }
}


async function importFile() {
  errors.value = []
  createdCount.value = null
  errorMessage.value = ''

  if (!selectedFile.value) {
    errorMessage.value = 'No file chosen.';
    return;
  }
  
  const formData = new FormData()
  formData.append('file', selectedFile.value)

  importing.value = true
  const response = await fetch(`/api/items/import`, {
    method: 'POST',
    body: formData
  })

  importing.value = false 
  if (response.ok) {
    const data = await response.json();
    createdCount.value = data.createdCount;
  }
  else if (response.status === 400) {
    const data = await response.json();
    errors.value = data.errors;
  } else {
    errorMessage.value = `Something went wrong (error ${response.status})`;
  }
}
</script>

<template>
    <h2>Import items</h2>
    <RouterLink to="/items">Back to items</RouterLink>
    <p>Fill in the template and upload it. If any row has an error, nothing is imported.</p>
    <p>Download the <a href="/import-template.xlsx" download>Template</a></p>
    <div class="attachment">
      <label>
        Attach a file
        <input type="file" accept=".xlsx" @change="onFileChange"/>
      </label>
      <button :disabled="importing" @click="importFile">{{ importing ? 'Importing...' : 'Import' }}</button>
      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
      <ul v-if="errors.length > 0">
        <li v-for="error in errors" :key="error" class="error">
            {{error}}
        </li>
      </ul>
      <p v-if="createdCount !== null">{{ createdCount }} items imported </p>
    </div>
</template>
<style scoped>
.attachment {
    display: flex;
    flex-direction: column;
    gap: 20px;
}

button {
    max-width: 120px;
    align-self: center;
    padding: 8px;
}
</style>
