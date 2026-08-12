import { Navigate, Route, Routes } from 'react-router-dom'

import { AppShell } from '@/components/layout/AppShell'
import { AuthProvider } from '@/lib/auth/AuthProvider'
import { ChangePasswordPage } from '@/routes/auth/ChangePasswordPage'
import { ForgotPasswordPage } from '@/routes/auth/ForgotPasswordPage'
import { LoginPage } from '@/routes/auth/LoginPage'
import { ResetPasswordPage } from '@/routes/auth/ResetPasswordPage'
import { DashboardPage } from '@/routes/dashboard/DashboardPage'
import { DepartmentDetailPage } from '@/routes/departments/DepartmentDetailPage'
import { DepartmentsPage } from '@/routes/departments/DepartmentsPage'
import { RequireAnonymous, RequireAuth, RequirePermission } from '@/routes/guards'
import { MemberDetailPage } from '@/routes/members/MemberDetailPage'
import { MemberFormPage } from '@/routes/members/MemberFormPage'
import { MembersPage } from '@/routes/members/MembersPage'
import { MyProfilePage } from '@/routes/members/MyProfilePage'

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

            <Route path="/departments" element={<DepartmentsPage />} />
            <Route path="/departments/:id" element={<DepartmentDetailPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}
