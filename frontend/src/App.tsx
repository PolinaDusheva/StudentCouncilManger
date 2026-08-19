import { Navigate, Route, Routes } from 'react-router-dom'

import { AppShell } from '@/components/layout/AppShell'
import { AuthProvider } from '@/lib/auth/AuthProvider'
import { ChangePasswordPage } from '@/routes/auth/ChangePasswordPage'
import { ForgotPasswordPage } from '@/routes/auth/ForgotPasswordPage'
import { LoginPage } from '@/routes/auth/LoginPage'
import { ResetPasswordPage } from '@/routes/auth/ResetPasswordPage'
import { DashboardPage } from '@/routes/dashboard/DashboardPage'
import { BudgetPage } from '@/routes/budget/BudgetPage'
import { DepartmentDetailPage } from '@/routes/departments/DepartmentDetailPage'
import { CalendarPage } from '@/routes/events/CalendarPage'
import { EventDetailPage } from '@/routes/events/EventDetailPage'
import { EventFormPage } from '@/routes/events/EventFormPage'
import { DepartmentsPage } from '@/routes/departments/DepartmentsPage'
import { RequireAnonymous, RequireAuth, RequirePermission } from '@/routes/guards'
import { MemberDetailPage } from '@/routes/members/MemberDetailPage'
import { MemberFormPage } from '@/routes/members/MemberFormPage'
import { MembersPage } from '@/routes/members/MembersPage'
import { MyProfilePage } from '@/routes/members/MyProfilePage'
import { TaskBoardPage } from '@/routes/tasks/TaskBoardPage'
import { TaskDetailPage } from '@/routes/tasks/TaskDetailPage'
import { TaskFormPage } from '@/routes/tasks/TaskFormPage'
import { TasksPage } from '@/routes/tasks/TasksPage'

export function App() {
  return (
    <AuthProvider>
      <Routes>
        {/* Signed-out screens; a signed-in member is bounced back to the app. */}
        <Route element={<RequireAnonymous />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Route>

        <Route element={<RequireAuth />}>
          {/* Rendered outside the shell: until the temporary password is replaced the API
              rejects everything else, so there is no navigation to offer. */}
          <Route path="/change-password" element={<ChangePasswordPage />} />

          <Route element={<AppShell />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/profile" element={<MyProfilePage />} />

            <Route path="/members" element={<MembersPage />} />
            {/* Declared before `/members/:id` so "new" is not read as an id. */}
            <Route element={<RequirePermission permission="canManageMembers" />}>
              <Route path="/members/new" element={<MemberFormPage />} />
              <Route path="/members/:id/edit" element={<MemberFormPage />} />
            </Route>
            <Route path="/members/:id" element={<MemberDetailPage />} />

            <Route path="/tasks" element={<TasksPage />} />
            <Route path="/tasks/board" element={<TaskBoardPage />} />
            {/* Declared before `/tasks/:id` so "new" is not read as an id. The server has the
                final say on who may create or edit — this only keeps the form out of sight. */}
            <Route path="/tasks/new" element={<TaskFormPage />} />
            <Route path="/tasks/:id/edit" element={<TaskFormPage />} />
            <Route path="/tasks/:id" element={<TaskDetailPage />} />

            <Route path="/events" element={<CalendarPage />} />
            {/* Declared before `/events/:id` so "new" is not read as an id. */}
            <Route path="/events/new" element={<EventFormPage />} />
            <Route path="/events/:id/edit" element={<EventFormPage />} />
            <Route path="/events/:id" element={<EventDetailPage />} />

            <Route path="/departments" element={<DepartmentsPage />} />
            <Route path="/departments/:id" element={<DepartmentDetailPage />} />

            <Route path="/budget" element={<BudgetPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}
