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
        <RouterLink to="/dashboard"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-house preview-icon"><path d="M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8"/><path d="M3 10a2 2 0 0 1 .709-1.528l7-6a2 2 0 0 1 2.582 0l7 6A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>Home</RouterLink>
        <RouterLink to="/items"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-layout-list preview-icon"><rect width="7" height="7" x="3" y="3" rx="1"/><rect width="7" height="7" x="3" y="14" rx="1"/><path d="M14 4h7"/><path d="M14 9h7"/><path d="M14 15h7"/><path d="M14 20h7"/></svg>Items</RouterLink>
        <RouterLink to="/categories"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-list-sort-descending preview-icon"><path d="M15 12H3"/><path d="M3 5h18"/><path d="M9 19H3"/></svg>Categories</RouterLink>
        <RouterLink v-if="auth.user?.role === 'Administrator'" to="/departments"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-graph-hub preview-icon"><circle cx="12" cy="7" r="4"/><circle cx="4" cy="19" r="2"/><circle cx="12" cy="19" r="2"/><circle cx="20" cy="19" r="2"/><path d="M8 7a4 4 0 0 0-4 4v6"/><path d="M12 11v6"/><path d="M16 7a4 4 0 0 1 4 4v6"/></svg>Departments</RouterLink>
        <RouterLink v-if="auth.user.role === 'Administrator'" to="/users"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-users preview-icon"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><path d="M16 3.128a4 4 0 0 1 0 7.744"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><circle cx="9" cy="7" r="4"/></svg>Team</RouterLink>
        <RouterLink to="/requests"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-file-text preview-icon"><path d="M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z"/><path d="M14 2v5a1 1 0 0 0 1 1h5"/><path d="M10 9H8"/><path d="M16 13H8"/><path d="M16 17H8"/></svg>Requests</RouterLink>
        <RouterLink v-if="auth.user.role !== 'Administrator'" to="/notifications"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-bell preview-icon"><path d="M10.268 21a2 2 0 0 0 3.464 0"/><path d="M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326"/></svg>
          Notifications 
          <span v-if="notificationsStore.unreadCount > 0" class="badge">{{ notificationsStore.unreadCount }}</span>
        </RouterLink> 
        <RouterLink v-if="auth.user?.role !== 'Employee'" to="/reports"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-chart-column-increasing preview-icon"><path d="M13 17V9"/><path d="M18 17V5"/><path d="M3 3v16a2 2 0 0 0 2 2h16"/><path d="M8 17v-3"/></svg>
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
