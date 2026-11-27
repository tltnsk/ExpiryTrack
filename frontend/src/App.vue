<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth';
import { useNotificationsStore } from '@/stores/notifications';
import { computed, watch, ref } from 'vue'
import { formatLabel } from '@/utils/format'
import logo from '@/assets/expiry-track-logo.png'

const auth = useAuthStore()
const notificationsStore = useNotificationsStore()
const router = useRouter()
const route = useRoute()

// on smaller screens the menu is hidden until the user opens it
const menuOpen = ref(false)

// initials shown in circle
const initials = computed(() => {
  if (!auth.user) return ''
  return (auth.user.firstName.charAt(0) + auth.user.lastName.charAt(0)).toUpperCase() 
})

async function logout() {
  await auth.logout()
  router.push('/login')
}

// names for the first part of the URL
const sections: Record<string, string> = {
  dashboard: 'Home', items: 'Items', categories: 'Categories', departments: 'Departments',
  users: 'Users', requests: 'Requests', notifications: 'Notifications', reports: 'Reports',
  'change-password': 'Change password', 'my-employees': 'My employees'
}

// names for the last part 
const pages: Record<string, string> = { new: 'New', import: 'Import', edit: 'Edit', renew: 'Renewal request' }

// create breadcrumb
const breadcrumb = computed(() => {
  const parts = route.path.split('/').filter((part) => part !== '')
  const section = parts[0] ?? ''
  const crumbs = [{ label: sections[section] ?? '', to: '/' + section }]

  if (section === 'items' && parts.length === 3) {
    crumbs.push({ label: 'Details', to: `/items/${parts[1]}` })
  }
  if (parts.length > 1) {
    const last = parts[parts.length - 1] ?? ''
    crumbs.push({ label: pages[last] ?? 'Details', to: route.path })
  }
  return crumbs
})

// shortcuts in the top bar
const quickActions = computed(() => {
  const role = auth.user?.role
  if (role === 'Administrator') return [{ label: '+ New user', to: '/users/new' }, { label: '+ New category', to: '/categories/new' }]
  if (role === 'DepartmentManager') return [{ label: '+ New item', to: '/items/new' }, { label: 'Import', to: '/items/import' }]
  if (role === 'Employee') return [{ label: '+ New item', to: '/items/new' }]
  if (role === 'FinanceOfficer') return [{ label: 'Review requests', to: '/requests' }]
  return []
})

// reload the unread notifications count every time the user opens another page
// and once when the application starts
watch (() => route.path, () => {
  if (auth.user) notificationsStore.loadUnreadCount()
  menuOpen.value = false
}, { immediate: true })
</script>

<template>
  <div class="layout">
    <aside v-if="auth.user" class="sidebar" :class="{ open: menuOpen}">
      <div class="sidebar-top">
        <div class="logo">
          <span class="logo-icon"><img :src="logo" alt=""/></span>
          <span class="logo-text">ExpiryTrack</span>
        </div>
        <button class="menu-button" @click="menuOpen = !menuOpen" aria-label="Menu">☰</button>
      </div>
      <nav>
        <p class="nav-heading">Work</p>
        <RouterLink to="/dashboard"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8"/><path d="M3 10a2 2 0 0 1 .709-1.528l7-6a2 2 0 0 1 2.582 0l7 6A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>Home</RouterLink>
        <RouterLink to="/items"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><rect width="7" height="7" x="3" y="3" rx="1"/><rect width="7" height="7" x="3" y="14" rx="1"/><path d="M14 4h7"/><path d="M14 9h7"/><path d="M14 15h7"/><path d="M14 20h7"/></svg>Items</RouterLink>
        <RouterLink to="/requests"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z"/><path d="M14 2v5a1 1 0 0 0 1 1h5"/><path d="M10 9H8"/><path d="M16 13H8"/><path d="M16 17H8"/></svg>Requests</RouterLink>
        <RouterLink v-if="auth.user.role !== 'Administrator'" to="/notifications"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M10.268 21a2 2 0 0 0 3.464 0"/><path d="M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326"/></svg>
          Notifications 
          <span v-if="notificationsStore.unreadCount > 0" class="badge">{{ notificationsStore.unreadCount }}</span>
        </RouterLink> 

        <!--employes have no reports-->
        <template v-if="auth.user.role !== 'Employee'">
          <p class="nav-heading">Insights</p>
          <RouterLink v-if="auth.user?.role !== 'Employee'" to="/reports"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M13 17V9"/><path d="M18 17V5"/><path d="M3 3v16a2 2 0 0 0 2 2h16"/><path d="M8 17v-3"/></svg>
            Reports
          </RouterLink>
        </template>

        <p class="nav-heading">Organization</p>
        <RouterLink to="/categories"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M15 12H3"/><path d="M3 5h18"/><path d="M9 19H3"/></svg>Categories</RouterLink>
        <template v-if="auth.user.role === 'Administrator'">
            <RouterLink to="/departments"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><circle cx="12" cy="7" r="4"/><circle cx="4" cy="19" r="2"/><circle cx="12" cy="19" r="2"/><circle cx="20" cy="19" r="2"/><path d="M8 7a4 4 0 0 0-4 4v6"/><path d="M12 11v6"/><path d="M16 7a4 4 0 0 1 4 4v6"/></svg>Departments</RouterLink>
            <RouterLink to="/users"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><path d="M16 3.128a4 4 0 0 1 0 7.744"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><circle cx="9" cy="7" r="4"/></svg>Team</RouterLink>
        </template>
        <RouterLink v-if="auth.user.role === 'DepartmentManager'" to="/my-employees"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><path d="M16 3.128a4 4 0 0 1 0 7.744"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><circle cx="9" cy="7" r="4"/></svg>My Employees</RouterLink>
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
        <header v-if="auth.user" class="topbar">
          <nav class="breadcrumb">
            <template v-for="(crumb, index) in breadcrumb" :key="index">
              <span v-if="index > 0" class="separator">/</span>
              <RouterLink v-if="index < breadcrumb.length - 1" :to="crumb.to">{{ crumb.label }}</RouterLink>
              <span v-else class="current">{{ crumb.label }}</span>
            </template>
          </nav>

          <div class="topbar-right">
            <span class="role-badge">{{ formatLabel(auth.user.role) }}</span>
            <RouterLink v-for="action in quickActions" :key="action.to" :to="action.to" class="quick-action">
              {{ action.label }}
            </RouterLink>
          </div>
        </header>

        <RouterView v-slot="{ Component }">
          <Transition :name="route.path === '/login' ? '' : 'fade'" mode="out-in">
            <div :key="route.path">
              <component :is="Component" />
            </div>
          </Transition>
        </RouterView>
      </div>
    </main>     
  </div>
</template>
