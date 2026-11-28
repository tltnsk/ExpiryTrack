import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'
import ItemsView from '@/views/ItemsView.vue'
import ItemDetailView from '@/views/ItemDetailView.vue'
import ItemCreateView from '@/views/ItemCreateView.vue'
import ItemEditView from '@/views/ItemEditView.vue'
import CategoriesView from '@/views/CategoriesView.vue'
import CategoriesCreateView from '@/views/CategoriesCreateView.vue'
import CategoriesEditView from '@/views/CategoriesEditView.vue'
import DepartmentsViews from '@/views/DepartmentsView.vue'
import DepartmentsCreateView from '@/views/DepartmentsCreateView.vue'
import DepartmentsEditView from '@/views/DepartmentsEditView.vue'
import UsersView from '@/views/UsersView.vue'
import UsersCreateView from '@/views/UsersCreateView.vue'
import UsersEditView from '@/views/UsersEditView.vue'
import RequestsView from '@/views/RequestsView.vue'
import RequestDetailView from '@/views/RequestDetailView.vue'
import RenewalSubmitView from '@/views/RenewalSubmitView.vue'
import ChangePasswordView from '@/views/ChangePasswordView.vue'
import NotificationsView from '@/views/NotificationsView.vue'
import DashboardView from '@/views/DashboardView.vue'
import ItemImportView from '@/views/ItemImportView.vue'
import ReportsView from '@/views/ReportsView.vue'
import MyEmployeesView from '@/views/MyEmployeesView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/dashboard' },
    { path: '/login', component: LoginView },
    { path: '/items', component: ItemsView },
    { path: '/items/:id', component: ItemDetailView },
    { path: '/items/new', component: ItemCreateView },
    { path: '/items/:id/edit', component: ItemEditView },
    { path: '/categories', component: CategoriesView },
    { path: '/categories/new', component: CategoriesCreateView },
    { path: '/categories/:id/edit', component: CategoriesEditView },
    { path: '/departments', component: DepartmentsViews },
    { path: '/departments/new', component: DepartmentsCreateView },
    { path: '/departments/:id/edit', component: DepartmentsEditView },
    { path: '/users', component: UsersView },
    { path: '/users/new', component: UsersCreateView },
    { path: '/users/:id/edit', component: UsersEditView },
    { path: '/requests', component: RequestsView },
    { path: '/requests/:id', component: RequestDetailView },
    { path: '/items/:id/renew', component: RenewalSubmitView },
    { path: '/change-password', component: ChangePasswordView },
    { path: '/notifications', component: NotificationsView },
    { path: '/dashboard', component: DashboardView },
    { path: '/items/import', component: ItemImportView }, 
    { path: '/reports', component: ReportsView },
    { path: '/my-employees', component: MyEmployeesView },
  ],
})

// runs before each navigation
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // ask the user who is logged in only once on the first page load
  // after logout we already know that noone is logged in
  if (auth.user === null && !auth.checked) {
    await auth.loadMe()
  }

  if (auth.user === null && to.path !== '/login') {
    return '/login'
  }

  // if authenticated and try to access login page, redirect them to items
  if (auth.user !== null && to.path === '/login') {
    return '/dashboard'
  }

  // if any other user besides admin tries to access departments
  if (auth.user?.role !== "Administrator" && to.path.startsWith('/departments')) {
    return '/dashboard'
  }

  // if any other user tries to edit or create categories
  if (auth.user?.role !== "Administrator" && to.path.startsWith('/categories/')) {
    return '/categories'
  }

  // only admins can access users
  if (auth.user?.role !== "Administrator" && to.path.startsWith('/users')) {
    return '/dashboard'
  }

  if (auth.user?.role !== "DepartmentManager" && to.path.startsWith('/items/import')) {
    return '/items'
  }

  // only employees and department managers can create and edit items
  const isEmployeeOrManager = auth.user?.role === "Employee" || auth.user?.role === "DepartmentManager"
  const isItemForm = to.path === '/items/new' || (to.path.startsWith('/items/') && to.path.endsWith('/edit'))
  if (!isEmployeeOrManager && isItemForm) {
    return '/items'
  }

  // only employees can submit renewal requests
  if (auth.user?.role !== "Employee" && to.path.startsWith('/items/') && to.path.endsWith('/renew')) {
    return '/items'
  }

  if (auth.user?.role === "Employee" && to.path.startsWith('/reports')) {
    return '/dashboard'
  }

  if (auth.user?.role !== "DepartmentManager" && to.path.startsWith('/my-employees')) {
    return '/dashboard'
  }
})

export default router
