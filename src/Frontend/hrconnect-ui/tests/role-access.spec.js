import { test, expect } from "@playwright/test";
const admin = { id: 1, fullName: "Admin User", email: "admin@example.com", role: "Admin", employeeId: 1 };
const employee = { id: 2, fullName: "Employee User", email: "employee@example.com", role: "Employee", employeeId: 2 };
function stubApi(page, user) { return page.route("**/api/**", async route => { const path = new URL(route.request().url()).pathname;
  if (path === "/api/auth/google/config") return route.fulfill({ json: { enabled: false, clientId: "" } });
  if (path === "/api/auth/login") return route.fulfill({ json: { accessToken: "test", expiresAtUtc: "2030-01-01T00:00:00Z", user } });
  if (path === "/api/employees") return route.fulfill({ json: { items: [{ id: 1, employeeCode: "EMP001", firstName: "Admin", lastName: "User", email: "admin@example.com", designation: "Admin", employmentStatus: "Active" }], totalCount: 1, totalPages: 1 } });
  if (path.includes("/documents")) return route.fulfill({ json: [] });
  if (path.includes("/personal")) return route.fulfill({ json: { phoneNumber: "1" } });
  if (path.startsWith("/api/employees/2")) return route.fulfill({ json: { id: 2, employeeCode: "EMP002", firstName: "Employee", lastName: "User", email: "employee@example.com", designation: "Engineer", employmentStatus: "Active" } });
  return route.fulfill({ json: [] }); }); }
async function login(page, user) { await stubApi(page, user); await page.goto("/"); await page.locator('input[name="email"]').fill(user.email); await page.locator('input[name="password"]').fill("password"); await page.locator("form").getByRole("button", { name: "Sign in" }).click(); }
test("admin login opens employee list", async ({ page }) => { await login(page, admin); await expect(page.getByRole("heading", { name: "Employees" })).toBeVisible(); });
test("employee login opens own profile", async ({ page }) => { await login(page, employee); await expect(page.getByRole("heading", { name: "Employee User" })).toBeVisible(); });
test("employee cannot see employee directory", async ({ page }) => { await login(page, employee); await expect(page.getByRole("button", { name: "Employees" })).toHaveCount(0); });
test("employee can open leave", async ({ page }) => { await login(page, employee); await page.getByRole("button", { name: "Leave" }).click(); await expect(page.getByRole("heading", { name: "Leave management" })).toBeVisible(); });
