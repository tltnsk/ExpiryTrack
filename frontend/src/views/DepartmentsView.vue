<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

// DepartmentResponse in C#
interface Department {
  id: number
  name: string
  isActive: boolean
  managerId: number | null
  managerName: string | null
}

const departments = ref<Department[]>([])
const auth = useAuthStore()

const loading = ref(true)
const errorMessage = ref('')

const isAdmin = computed(() => auth.user?.role === 'Administrator')

// fetch departments from the backend
onMounted(async () => {
  const response = await fetch('/api/departments')
  if (response.ok) {
    departments.value = await response.json()
  } else {
    errorMessage.value = `Could not load departments (error ${response.status}).`
  }
  loading.value = false
})
</script>

<template>
  <div class="page-top">
      <h2>Departments</h2>
      <RouterLink v-if="isAdmin" to="/departments/new" class="new-button">New department</RouterLink>
  </div>

  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <p v-else-if="departments.length === 0">No departments yet.</p>

  <div v-else class="table-box">
    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>Manager</th>
          <th>Active</th>
          <th v-if="isAdmin"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="department in departments" :key="department.id">
          <td class="name">{{ department.name }}</td>
          <td>{{ department.managerName ?? 'No manager' }}</td>
          <td>
            <span v-if="department.isActive" class="active-cat">Active</span>
            <span v-else class="inactive-cat">Inactive</span>
          </td>
          <td v-if="isAdmin" class="edit-cell">
              <RouterLink :to="`/departments/${department.id}/edit`" class="edit-link" :title="`Edit ${department.name}`" :aria-label="`Edit ${department.name}`">✎</RouterLink>
          </td>
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

.new-button {
  padding: 8px 14px;
  border-radius: 8px;
  background: var(--primary);
  color: white;
  text-decoration: none;
  font-weight: 500;
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

.name {
  font-weight: 500;
}

.description {
  max-width: 280px;
}

.active-cat {
  background-color: rgb(132, 219, 132);
  padding: 5px;
}

.inactive-cat {
  background-color: rgb(221, 113, 113);
  padding: 5px;
}

.edit-cell {
  text-align: right;
}

.edit-link {
  color: #6b7280;
  font-size: 1.23em;
  text-decoration: none;
}
</style>