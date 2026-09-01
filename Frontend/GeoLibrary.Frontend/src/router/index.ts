import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // Area pubblica: consultabile senza registrarsi. Le schede di libreria e libro
    // sono gli stessi componenti dell'area autenticata, solo con un altro layout.
    {
      path: '/',
      component: () => import('@/layouts/public.vue'),
      children: [
        {
          path: '',
          component: () => import('@/pages/Home.vue'),
        },
        {
          // Informativa privacy: pubblica e raggiungibile anche dal banner di consenso.
          path: 'privacy',
          meta: { title: 'Privacy' },
          component: () => import('@/pages/privacy.vue'),
        },
        {
          path: 'libraries/:id',
          children: [
            {
              path: '',
              component: () => import('@/pages/app/libraries/detail.vue'),
            },
            {
              path: 'book/:bookId',
              component: () => import('@/pages/app/libraries/book/detail.vue'),
            },
          ],
        },
      ],
    },
    {
      path: '/app/',
      component: () => import('@/layouts/admin.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          component: () => import('@/pages/app/Dashboard.vue'),
        },
        {
          // Riservata al ruolo di realm "admin": vedi il controllo in beforeEach.
          path: 'admin',
          meta: { title: 'Amministrazione', requiresRole: 'admin' },
          component: () => import('@/pages/app/admin/dashboard.vue'),
        },
        {
          path: 'profile',
          meta: { title: 'Profilo' },
          component: () => import('@/pages/app/profile.vue'),
        },
        {
          path: 'loans',
          meta: { title: 'Prestiti' },
          component: () => import('@/pages/app/loans.vue'),
        },
        {
          path: 'libraries',
          name: 'libraries',
          children: [
            {
              path: '',
              component: () => import('@/pages/app/libraries/list.vue'),
            },
            {
              path: ':id',
              name: 'library-detail',
              children: [
                {
                  path: '',
                  component: () => import('@/pages/app/libraries/detail.vue'),
                },
                {
                  path: 'edit',
                  // meta.title è l'ultima briciola delle pagine di azione (vedi breadcrumbs.vue)
                  meta: { title: 'Modifica' },
                  component: () => import('@/pages/app/libraries/edit.vue'),
                },
                {
                  path: 'book',
                  children: [
                    {
                      path: 'new',
                      meta: { title: 'Nuovo libro' },
                      component: () => import('@/pages/app/libraries/book/new.vue'),
                    },
                    {
                      path: 'import',
                      meta: { title: 'Importa libri' },
                      component: () => import('@/pages/app/libraries/book/import.vue'),
                    },
                    {
                      path: ':bookId',
                      component: () => import('@/pages/app/libraries/book/detail.vue'),
                    },
                    {
                      path: ':bookId/edit',
                      meta: { title: 'Modifica' },
                      component: () => import('@/pages/app/libraries/book/edit.vue'),
                    },
                  ],
                },
              ],
            },
            {
              path: 'new',
              meta: { title: 'Nuova libreria' },
              component: () => import('@/pages/app/libraries/new.vue'),
            },
          ],
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  // requiresAuth su un record padre protegge tutta la sua sottostruttura:
  // matched contiene l'intera catena (padre + figli) della route di destinazione.
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  if (requiresAuth && !auth.isAuthenticated) {
    auth.login(to.fullPath)
    return false
  }

  // Rotte riservate a un ruolo: qui si nasconde solo l'interfaccia, l'autorizzazione
  // vera resta sulle policy degli endpoint. Chi è autenticato ma non ha il ruolo
  // viene riportato alla home dell'area riservata, non al login.
  const requiredRole = to.matched.find((record) => record.meta.requiresRole)?.meta.requiresRole as
    | string
    | undefined
  if (requiredRole && !auth.hasRole(requiredRole)) {
    return '/app'
  }
})

export default router
