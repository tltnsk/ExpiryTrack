<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { formatLabel } from '@/utils/format'

// UserResposnse 
interface User {
  id: number
  firstName: string
  lastName: string
  email: string
  role: string
  departmentId: number | null
  departmentName: string | null
  isActive: boolean
}

const users = ref<User[]>([])
const loading = ref(true)
const errorMessage = ref('')
const actionError = ref('')
const auth = useAuthStore()

onMounted(async () => {
  const response = await fetch('/api/users')
  if (response.ok) {
    users.value = await response.json()
  } else {
    errorMessage.value = `Could not load users (error ${response.status}).`
  }
  loading.value = false
})

// activate/deactivate user
async function toggleActive(user: User) {
  const newValue = !user.isActive
  const action = newValue ? 'activate' : 'deactivate'

  if (!confirm(`Are you sure you want to ${action} ${user.firstName} ${user.lastName}?`)) {
    return
  }

  actionError.value = ''
  const response = await fetch(`/api/users/${user.id}/active`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isActive: newValue }),
  })

  if (response.ok) {
    user.isActive = newValue
  } else if (response.status === 409) {
    actionError.value = await response.text()
  } else {
    actionError.value = `Could not change the user (error ${response.status}).`
  }
}

const isAdmin = computed(() => auth.user?.role === 'Administrator')
</script> 

<template>
  <div class="page-top">
      <h2>Users</h2>
      <RouterLink v-if="isAdmin" to="/users/new" class="new-button">New user</RouterLink>
  </div>

  <p v-if="actionError" class="error">{{ actionError }}</p>
  <p v-if="loading">Loading...</p>
  <p v-else-if="errorMessage" class="error">{{ errorMessage }}</p>
  <p v-else-if="users.length === 0">No users yet.</p>

  <div v-else class="table-box">
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Department</th>
            <th>Active</th>
            <th v-if="isAdmin"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.id">
            <td class="name">
                {{ user.firstName }} {{ user.lastName }}
            </td>
            <td> {{ user.email}}</td>
            <td> {{ formatLabel(user.role) }}</td>
            <td> {{ user.departmentName  ?? '-'}}</td>
            <td> {{ user.isActive ? "Yes" : "No"}}</td>
            <td v-if="isAdmin" class="actions-cell">
              <RouterLink :to="`/users/${user.id}/edit`" class="edit-link" :title="`Edit ${user.firstName} ${user.lastName}`" :aria-label="`Edit ${user.lastName}`">✎</RouterLink>
              <button :class="{danger: user.isActive}" @click="toggleActive(user)">{{ user.isActive ? 'Deactivate' : 'Activate' }}</button>
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

th {
  background: #f9fafb;
}

.name {
  font-weight: 500;
}

.actions-cell {
  text-align: right;
  white-space: nowrap;
}

.edit-link {
  margin-right: 12px;
  color: #6b7280;
  font-size: 1.23em;
  text-decoration: none;
  vertical-align: middle;
}

.error {
  color: var(--danger);
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

</style>