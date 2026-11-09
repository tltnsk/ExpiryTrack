<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth';
import { useNotificationsStore } from '@/stores/notifications';
import { computed, watch } from 'vue'
import { formatLabel } from '@/utils/format'
import logo from '@/assets/expiry-track-logo.png'

const auth = useAuthStore()
const notificationsStore = useNotificationsStore()
const router = useRouter()
const route = useRoute()

// initials shown in circle
const initials = computed(() => {
  if (!auth.user) return ''
  return (auth.user.firstName.charAt(0) + auth.user.lastName.charAt(0)).toUpperCase() 
})

async function logout() {
  await auth.logout()
  router.push('/login')
}

// reload the unread notifications count every time the user opens another page
// and once when the application starts
watch (() => route.path, () => {
  if (auth.user) notificationsStore.loadUnreadCount()
}, { immediate: true })
</script>

<template>
  <div class="layout">
    <aside v-if="auth.user" class="sidebar">
      <div class="logo">
        <span class="logo-icon"><img :src="logo" alt=""/></span>
        <span class="logo-text">ExpiryTrack</span>
      </div>
      <nav>
        <RouterLink to="/dashboard">Home</RouterLink>
        <RouterLink to="/items">Items</RouterLink>
        <RouterLink to="/categories">Categories</RouterLink>
        <RouterLink v-if="auth.user?.role === 'Administrator'" to="/departments">Departments</RouterLink>
        <RouterLink v-if="auth.user.role === 'Administrator'" to="/users">Users</RouterLink>
        <RouterLink to="/requests">Requests</RouterLink>
        <RouterLink to="/notifications">
          Notifications 
          <span v-if="notificationsStore.unreadCount > 0" class="badge">{{ notificationsStore.unreadCount }}</span>
        </RouterLink> 
        <RouterLink v-if="auth.user?.role !== 'Employee'" to="/reports">
          Reports
        </RouterLink>
      </nav>

      <div class="account">
        <div class="account-info">
          <span class="avatar">{{ initials }}</span>
          <div>
            <div class="account-name">{{ auth.user.firstName }} {{ auth.user?.lastName }}</div>
            <div class="account-role"> {{ formatLabel(auth.user.role) }}</div>
          </div>
        </div>
        <RouterLink to="/change-password">Change Password</RouterLink>
        <button @click="logout" id="logout-btn">Log out</button>
      </div>
    </aside>

    <main>
      <div class="content">
        <RouterView />
      </div>
    </main>
  </div>
</template>
