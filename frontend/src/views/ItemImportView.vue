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

const dragging = ref(false)
function onDrop(event: DragEvent) {
  dragging.value = false
  const file = event.dataTransfer?.files[0]
  if (file) {
    selectedFile.value = file
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
    selectedFile.value = null
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
    <RouterLink to="/items" class="back-link">Back to items</RouterLink>

    <div class="import-card">
      <div class="import-header">
        <div>
            <h2>Import items</h2>
            <p>Fill in the template and upload it. If any row has an error, nothing is imported.</p>
        </div>
      </div>

      <div class="dropzone"
          :class="{dragging}"
          @dragover.prevent="dragging = true"
          @dragleave="dragging = false"
          @drop.prevent="onDrop"
      >
      <div class="upload-icon">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"
             stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M12 13v8" />
          <path d="M4 14.9A7 7 0 1 1 15.7 8h1.8a4.5 4.5 0 0 1 2.5 8.2" />
          <path d="m8 17 4-4 4 4" />
        </svg>
      </div>
      <p class="drop-title">Choose a file or drag and drop it here.</p>
      <p>{{ selectedFile ? selectedFile.name : 'Excel files (.xlsx) only' }}</p>
      <label class="browse">
        Browse file
        <input type="file" accept=".xlsx" hidden @change="onFileChange"/>
      </label>
    </div>
    <button class="import-button" :disabled="importing || !selectedFile" @click="importFile">
      {{ importing ? 'Importing...' : 'Import' }}
    </button>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
      <ul v-if="errors.length > 0">
        <li v-for="error in errors" :key="error" class="error">
            {{error}}
        </li>
      </ul>
    <p v-if="createdCount !== null">{{ createdCount }} items imported </p>

    <div class="template-row">
      <div class="file-info">
        <p class="file-name">import-template.xlsx</p>
        <p>Excel file with all the columns you need to fill in.</p>
      </div>
      <a href="/import-template.xlsx" download class="download-link">Download</a>
    </div>
    </div>

   
</template>
<style scoped>
.import-card {
  max-width: 560px;
  margin: 24px auto;
  padding: 28px;
  background: white;
  border: 1px solid #eceef1;
  border-radius: 24px;
}

.import-header {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 24px;
}

.dropzone {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 36px 20px;
  text-align: center;
  background: #f9fafb;
  border: 2px dashed #d1d5db;
  border-radius: 16px;
}

.dropzone.dragging {
  border-color: var(--primary);
  background: #eff6ff;
}

.upload-icon {
  width: 70px;
  height: 70px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #374151;
}

.drop-title {
  margin: 16px 0 4px;
  font-size: 1.2em;
}

.browse {
  padding: 10px 22px;
  border-radius: 10px;
  cursor: pointer;
}

.import-button {
  width: 100%;
  margin: 20px 0 0 ;
  padding: 10px;
}

.error-list {
  margin: 16px 0 0;
  padding: 12px 12px 12px 32px;
  border-radius: 10px;
  color: var(--danger);
}

.success {
  color: #16a34a;
}

.template-row {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 16px;
  border: 1px solid #eceef1;
  border-radius: 14px;
  margin-top: 10px;
}

.file-info {
  flex: 1
}

.file-info p {
  margin: 0;
}

.file-name {
  font-weight: 500;
}

.download-link {
  padding: 8px 14px;
  border: 1px solid #d1d5db;
  border-radius: 10px;
  color: #111827;
  text-decoration: none;
  font-weight: 500;
}
</style>
