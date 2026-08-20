import { useEffect, useRef, useState } from "react";
import {
    getEmployees,
    createEmployee,
    updateEmployee,
    deleteEmployee,
    loginUser,
    signupUser,
    refreshSession,
    logoutUser,
    getGoogleAuthConfig,
    loginWithGoogle,
    getDepartments,
    getEmployee,
    getEmployeeOptions,
    requestPasswordReset,
    resetPassword,
} from "./services/employeeServices";
import DepartmentsPage from "./components/DepartmentsPage";
import LeavePage from "./components/LeavePage";
import UsersPage from "./components/UsersPage";
import EmployeeProfilePage from "./components/PrivateEmployeeProfilePage";

const emptyEmployeeForm = {
    id: 0,
    employeeCode: "",
    firstName: "",
    lastName: "",
    email: "",
    personalEmail: "",
    phoneNumber: "",
    alternatePhoneNumber: "",
    dateOfBirth: "",
    gender: "",
    maritalStatus: "",
    bloodGroup: "",
    designation: "",
    departmentId: "",
    managerId: "",
    employmentType: "Permanent",
    employmentStatus: "Active",
    workLocation: "",
    dateOfJoining: "",
    dateOfLeaving: "",
    addressLine1: "",
    addressLine2: "",
    city: "",
    state: "",
    postalCode: "",
    country: "India",
    emergencyContactName: "",
    emergencyContactRelationship: "",
    emergencyContactPhone: "",
};

const emptyLoginForm = {
    email: "",
    password: "",
};

const emptySignupForm = {
    fullName: "",
    email: "",
    password: "",
    confirmPassword: "",
};

const emptyForgotPasswordForm = { email: "" };
const initialResetToken = new URLSearchParams(window.location.search).get("resetToken") ?? "";
const emptyResetPasswordForm = { token: initialResetToken, password: "", confirmPassword: "" };

const emailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
const namePattern = "^[A-Za-z][A-Za-z.'-]*(?: [A-Za-z][A-Za-z.'-]*)*$";
const pageSize = 25;
const tokenStorageKey = "hrconnect_token";
const userStorageKey = "hrconnect_user";

function loadGoogleIdentityServices() {
    if (window.google?.accounts?.id) return Promise.resolve();

    return new Promise((resolve, reject) => {
        const existing = document.querySelector('script[src="https://accounts.google.com/gsi/client"]');
        if (existing) {
            existing.addEventListener("load", resolve, { once: true });
            existing.addEventListener("error", reject, { once: true });
            return;
        }

        const script = document.createElement("script");
        script.src = "https://accounts.google.com/gsi/client";
        script.async = true;
        script.defer = true;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

const designations = [
    "Software Engineer",
    "Senior Software Engineer",
    "Frontend Developer",
    "Backend Developer",
    "Full Stack Developer",
    "QA Engineer",
    "DevOps Engineer",
    "Cloud Engineer",
    "Data Engineer",
    "UI/UX Designer",
    "Business Analyst",
    "Project Manager",
    "Scrum Master",
    "System Administrator",
    "Technical Lead",
];

function cleanName(value) {
    return String(value ?? "").trim().replace(/\s+/g, " ");
}

function cleanText(value) {
    return String(value ?? "").trim();
}

function getErrorMessage(error, fallbackMessage) {
    return error?.response?.data?.message ?? fallbackMessage;
}

function getStoredUser() {
    try {
        return JSON.parse(localStorage.getItem(userStorageKey));
    } catch {
        return null;
    }
}

function persistSession(authResponse) {
    localStorage.setItem(tokenStorageKey, authResponse.accessToken);
    localStorage.setItem(userStorageKey, JSON.stringify(authResponse.user));
}

function employeePayload(form) {
    return {
        id: Number(form.id || 0),
        employeeCode: cleanText(form.employeeCode).toUpperCase(),
        firstName: cleanName(form.firstName),
        lastName: cleanName(form.lastName),
        email: cleanText(form.email).toLowerCase(),
        designation: cleanText(form.designation),
        departmentId: form.departmentId ? Number(form.departmentId) : null,
        managerId: form.managerId ? Number(form.managerId) : null,
        employmentType: form.employmentType,
        employmentStatus: form.employmentStatus,
        workLocation: cleanText(form.workLocation) || null,
        dateOfJoining: form.dateOfJoining,
        dateOfLeaving: form.dateOfLeaving || null,
    };
}

function employeeToForm(employee) {
    const form = { ...emptyEmployeeForm, ...employee };
    for (const field of ["dateOfBirth", "dateOfJoining", "dateOfLeaving"]) {
        form[field] = employee[field]?.split("T")[0] ?? "";
    }
    form.departmentId = employee.departmentId ?? "";
    form.managerId = employee.managerId ?? "";
    return form;
}

function HRApp() {
    const [employees, setEmployees] = useState([]);
    const [employeeOptions, setEmployeeOptions] = useState([]);
    const [departments, setDepartments] = useState([]);
    const [addFormData, setAddFormData] = useState(emptyEmployeeForm);
    const [updateFormData, setUpdateFormData] = useState(null);
    const [authMode, setAuthMode] = useState(initialResetToken ? "reset" : "login");
    const [loginFormData, setLoginFormData] = useState(emptyLoginForm);
    const [signupFormData, setSignupFormData] = useState(emptySignupForm);
    const [forgotPasswordFormData, setForgotPasswordFormData] = useState(emptyForgotPasswordForm);
    const [resetPasswordFormData, setResetPasswordFormData] = useState(emptyResetPasswordForm);
    const [currentUser, setCurrentUser] = useState(() => initialResetToken ? null : getStoredUser());
    const [currentPage, setCurrentPage] = useState("list");
    const [searchText, setSearchText] = useState("");
    const [pageNumber, setPageNumber] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);
    const [successMessage, setSuccessMessage] = useState("");
    const [errorMessage, setErrorMessage] = useState("");
    const [notificationVersion, setNotificationVersion] = useState(0);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [isAuthenticating, setIsAuthenticating] = useState(false);
    const [isApiAvailable, setIsApiAvailable] = useState(true);
    const [employeeToDelete, setEmployeeToDelete] = useState(null);
    const [profileEmployeeId, setProfileEmployeeId] = useState(null);
    const googleButtonRef = useRef(null);
    const notificationRef = useRef(null);
    const [googleLoginStatus, setGoogleLoginStatus] = useState("loading");

    useEffect(() => {
        if (!successMessage && !errorMessage) return;

        const frame = window.requestAnimationFrame(() => {
            notificationRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
            notificationRef.current?.focus({ preventScroll: true });
        });

        return () => window.cancelAnimationFrame(frame);
    }, [successMessage, errorMessage, notificationVersion]);

    useEffect(() => {
        if (!currentUser && !initialResetToken) {
            restoreSession();
        }
        // restoreSession intentionally runs only at startup or after a full logout.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        const updateApiStatus = event => setIsApiAvailable(event.detail.available);
        window.addEventListener("hrconnect-api-status", updateApiStatus);
        return () => window.removeEventListener("hrconnect-api-status", updateApiStatus);
    }, []);

    useEffect(() => {
        if (currentUser) {
            if (["HR", "Admin"].includes(currentUser.role)) {
                loadEmployees();
                loadDepartments();
            }
            if (["HR", "Admin"].includes(currentUser.role)) loadEmployeeOptions();
        }
        // loadEmployees closes over the current pagination/search state by design.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentUser, pageNumber, searchText]);

    useEffect(() => {
        if (currentUser?.role === "Employee" && currentPage === "list") {
            setCurrentPage(currentUser.employeeId ? "profile" : "leave");
            setProfileEmployeeId(currentUser.employeeId ?? null);
        }
    }, [currentUser, currentPage]);

    async function loadDepartments() {
        try { setDepartments(await getDepartments(false)); }
        catch { setDepartments([]); }
    }

    async function loadEmployeeOptions() {
        try { setEmployeeOptions(await getEmployeeOptions()); }
        catch { setEmployeeOptions([]); }
    }

    useEffect(() => {
        if (currentUser) return;
        let cancelled = false;

        async function initializeGoogleLogin() {
            try {
                const config = await getGoogleAuthConfig();
                if (cancelled) return;
                if (!config.enabled) {
                    setGoogleLoginStatus("unavailable");
                    return;
                }

                await loadGoogleIdentityServices();
                if (cancelled || !googleButtonRef.current) return;

                window.google.accounts.id.initialize({
                    client_id: config.clientId,
                    callback: handleGoogleCredential,
                    ux_mode: "popup",
                });
                googleButtonRef.current.replaceChildren();
                window.google.accounts.id.renderButton(googleButtonRef.current, {
                    theme: "outline",
                    size: "large",
                    width: googleButtonRef.current.clientWidth,
                    text: "continue_with",
                });
                setGoogleLoginStatus("ready");
            } catch {
                // Password authentication remains available if Google is unavailable.
                if (!cancelled) setGoogleLoginStatus("unavailable");
            }
        }

        initializeGoogleLogin();
        return () => { cancelled = true; };
    }, [currentUser, authMode]);

    async function loadEmployees() {
        setIsLoading(true);
        setErrorMessage("");

        try {
            const data = await getEmployees({
                search: searchText,
                pageNumber,
                pageSize,
            });

            setEmployees(data.items ?? []);
            setTotalCount(data.totalCount ?? 0);
            setTotalPages(data.totalPages ?? 1);
        } catch (error) {
            if (error?.response?.status === 401) {
                handleLogout("Your session expired. Please sign in again.");
                return;
            }

            setErrorMessage("Unable to load employees. Please try again.");
        } finally {
            setIsLoading(false);
        }
    }

    async function restoreSession() {
        try {
            const authResponse = await refreshSession();
            persistSession(authResponse);
            setCurrentUser(authResponse.user);
        } catch {
            localStorage.removeItem(tokenStorageKey);
            localStorage.removeItem(userStorageKey);
        }
    }

    async function handleLogout(message = "") {
        try {
            await logoutUser();
        } catch {
            // Local logout should still succeed if the server session is already gone.
        }

        localStorage.removeItem(tokenStorageKey);
        localStorage.removeItem(userStorageKey);
        setCurrentUser(null);
        setEmployees([]);
        setDepartments([]);
        setProfileEmployeeId(null);
        setCurrentPage("list");
        setSearchText("");
        setPageNumber(1);
        setSuccessMessage("");
        setErrorMessage(message);
    }

    async function handleLoginSubmit(e) {
        e.preventDefault();
        setIsAuthenticating(true);
        setErrorMessage("");

        try {
            const authResponse = await loginUser({
                email: loginFormData.email.trim().toLowerCase(),
                password: loginFormData.password,
            });

            persistSession(authResponse);
            setCurrentUser(authResponse.user);
            setLoginFormData(emptyLoginForm);
            setSuccessMessage(`Welcome back, ${authResponse.user.fullName}`);
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to sign in. Please check your credentials."));
        } finally {
            setIsAuthenticating(false);
        }
    }

    async function handleSignupSubmit(e) {
        e.preventDefault();

        if (signupFormData.password !== signupFormData.confirmPassword) {
            setErrorMessage("Passwords do not match.");
            return;
        }

        setIsAuthenticating(true);
        setErrorMessage("");

        try {
            const authResponse = await signupUser({
                fullName: cleanName(signupFormData.fullName),
                email: signupFormData.email.trim().toLowerCase(),
                password: signupFormData.password,
            });

            persistSession(authResponse);
            setCurrentUser(authResponse.user);
            setSignupFormData(emptySignupForm);
            setSuccessMessage(`Welcome to HRConnect, ${authResponse.user.fullName}`);
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to create your account. Please try again."));
        } finally {
            setIsAuthenticating(false);
        }
    }

    async function handleForgotPasswordSubmit(e) {
        e.preventDefault();
        setIsAuthenticating(true);
        setErrorMessage("");
        setSuccessMessage("");
        try {
            const response = await requestPasswordReset(forgotPasswordFormData.email.trim().toLowerCase());
            setSuccessMessage(response.message);
            if (response.developmentResetUrl) {
                const token = new URL(response.developmentResetUrl).searchParams.get("resetToken") ?? "";
                setResetPasswordFormData({ ...emptyResetPasswordForm, token });
                setAuthMode("reset");
                setSuccessMessage("Development mode: reset link created. Choose a new password below.");
            }
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to request a password reset. Please try again."));
        } finally {
            setIsAuthenticating(false);
        }
    }

    async function handleResetPasswordSubmit(e) {
        e.preventDefault();
        if (resetPasswordFormData.password !== resetPasswordFormData.confirmPassword) {
            setErrorMessage("Passwords do not match.");
            return;
        }
        setIsAuthenticating(true);
        setErrorMessage("");
        try {
            const response = await resetPassword(resetPasswordFormData.token, resetPasswordFormData.password);
            window.history.replaceState({}, document.title, window.location.pathname);
            setResetPasswordFormData({ token: "", password: "", confirmPassword: "" });
            setLoginFormData({ ...emptyLoginForm, email: forgotPasswordFormData.email });
            setAuthMode("login");
            setSuccessMessage(response.message);
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to reset your password. The link may have expired."));
        } finally {
            setIsAuthenticating(false);
        }
    }

    async function handleGoogleCredential(response) {
        if (isAuthenticating) return;

        if (!response?.credential) {
            setErrorMessage("Google sign-in did not return a credential.");
            return;
        }

        setIsAuthenticating(true);
        setErrorMessage("");
        try {
            const authResponse = await loginWithGoogle(response.credential);
            persistSession(authResponse);
            setCurrentUser(authResponse.user);
            setSuccessMessage(`Welcome, ${authResponse.user.fullName}`);
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to sign in with Google. Please try again."));
        } finally {
            setIsAuthenticating(false);
        }
    }

    async function handleAddSubmit(e) {
        e.preventDefault();
        setIsSaving(true);
        setErrorMessage("");

        try {
            await createEmployee(employeePayload(addFormData));

            setAddFormData(emptyEmployeeForm);
            setCurrentPage("list");
            setPageNumber(1);
            setSuccessMessage("Employee added successfully");
            await loadEmployees();
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to add employee. Please try again."));
        } finally {
            setIsSaving(false);
        }
    }

    async function handleUpdateSubmit(e) {
        e.preventDefault();
        setIsSaving(true);
        setErrorMessage("");

        try {
            await updateEmployee(updateFormData.id, employeePayload(updateFormData));

            setUpdateFormData(null);
            setCurrentPage("list");
            setSuccessMessage("Employee updated successfully");
            await loadEmployees();
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to update employee. Please try again."));
        } finally {
            setIsSaving(false);
        }
    }

    async function openEmployeeForEdit(employeeId) {
        setErrorMessage("");
        try {
            const employee = await getEmployee(employeeId);
            setUpdateFormData(employeeToForm(employee));
            setCurrentPage("update");
        } catch (error) {
            setErrorMessage(getErrorMessage(error, "Unable to load employee details."));
        }
    }

    function showSuccess(message) {
        setErrorMessage("");
        setSuccessMessage(message);
        setNotificationVersion(version => version + 1);
    }

    function showError(message) {
        setSuccessMessage("");
        setErrorMessage(message);
        setNotificationVersion(version => version + 1);
    }

    function renderAuth() {
        const isSignup = authMode === "signup";
        const isForgotPassword = authMode === "forgot";
        const isResetPassword = authMode === "reset";

        return (
            <main className="auth-shell">
                <section className="auth-panel">
                    <div className="auth-copy">
                        <p className="eyebrow">HRConnect</p>
                        <h1>Secure employee operations</h1>
                        <p>Sign in to manage employee records with protected API access.</p>
                    </div>

                    <div className="auth-card" aria-busy={isAuthenticating}>
                        {isAuthenticating && <div className="auth-progress-overlay" role="status" aria-live="polite">Signing in securely…</div>}
                        <div className="auth-tabs" role="tablist" aria-label="Authentication">
                            <button
                                type="button"
                            className={!isSignup ? "active" : ""}
                                onClick={() => {
                                    setAuthMode("login");
                                    setErrorMessage("");
                                }}
                            >
                                Sign in
                            </button>
                            <button
                                type="button"
                                className={isSignup ? "active" : ""}
                                onClick={() => {
                                    setAuthMode("signup");
                                    setErrorMessage("");
                                }}
                            >
                                Sign up
                            </button>
                        </div>

                        {successMessage && <p ref={notificationRef} className="success-message" role="status" tabIndex={-1}>{successMessage}</p>}
                        {errorMessage && <p ref={notificationRef} className="error-message" role="alert" tabIndex={-1}>{errorMessage}</p>}

                        {!isForgotPassword && !isResetPassword && <>
                            <div
                                ref={googleButtonRef}
                                className={`google-signin ${googleLoginStatus === "ready" ? "" : "google-signin-hidden"} ${isAuthenticating ? "google-signin-disabled" : ""}`}
                                aria-label="Continue with Google"
                            />
                            {googleLoginStatus !== "ready" && (
                                <button type="button" className="google-fallback" disabled>
                                    <span className="google-mark" aria-hidden="true">G</span>
                                    {googleLoginStatus === "loading" ? "Loading Google sign-in..." : "Continue with Google"}
                                </button>
                            )}
                            {googleLoginStatus === "unavailable" && (
                                <p className="google-status">Google login needs a client ID and a running API.</p>
                            )}
                            <p className="google-access-notice">
                                Google sign-in is available only to registered HRConnect employees and approved users. Contact your administrator if you need access.
                            </p>
                            <div className="auth-divider"><span>or use email</span></div>
                        </>}

                        {isForgotPassword ? (
                            <form onSubmit={handleForgotPasswordSubmit} className="auth-form">
                                <div className="auth-form-heading"><h2>Forgot password?</h2><p>Enter your account email and we will prepare a secure reset link.</p></div>
                                <label>Email<input name="email" type="email" pattern={emailPattern} value={forgotPasswordFormData.email} onChange={e => setForgotPasswordFormData({ email: e.target.value })} required /></label>
                                <button type="submit" className="primary-btn" disabled={isAuthenticating}>{isAuthenticating ? "Preparing link..." : "Reset password"}</button>
                                <button type="button" className="auth-link" onClick={() => { setAuthMode("login"); setErrorMessage(""); setSuccessMessage(""); }}>Back to sign in</button>
                            </form>
                        ) : isResetPassword ? (
                            <form onSubmit={handleResetPasswordSubmit} className="auth-form">
                                <div className="auth-form-heading"><h2>Choose a new password</h2><p>Use at least 12 characters. This link works once.</p></div>
                                <label>New password<input name="password" type="password" minLength="12" maxLength="128" value={resetPasswordFormData.password} onChange={e => setResetPasswordFormData({ ...resetPasswordFormData, password: e.target.value })} required /></label>
                                <label>Confirm new password<input name="confirmPassword" type="password" minLength="12" maxLength="128" value={resetPasswordFormData.confirmPassword} onChange={e => setResetPasswordFormData({ ...resetPasswordFormData, confirmPassword: e.target.value })} required /></label>
                                <button type="submit" className="primary-btn" disabled={isAuthenticating}>{isAuthenticating ? "Resetting password..." : "Set new password"}</button>
                                <button type="button" className="auth-link" onClick={() => { setAuthMode("login"); setErrorMessage(""); setSuccessMessage(""); }}>Back to sign in</button>
                            </form>
                        ) : !isSignup ? (
                            <form onSubmit={handleLoginSubmit} className="auth-form">
                                <label>
                                    Email
                                    <input
                                        name="email"
                                        type="email"
                                        pattern={emailPattern}
                                        value={loginFormData.email}
                                        onChange={(e) =>
                                            setLoginFormData({
                                                ...loginFormData,
                                                [e.target.name]: e.target.value,
                                            })
                                        }
                                        required
                                    />
                                </label>

                                <label>
                                    Password
                                    <input
                                        name="password"
                                        type="password"
                                        value={loginFormData.password}
                                        onChange={(e) =>
                                            setLoginFormData({
                                                ...loginFormData,
                                                [e.target.name]: e.target.value,
                                            })
                                        }
                                        required
                                    />
                                </label>

                                <button type="button" className="auth-link auth-link-right" onClick={() => { setAuthMode("forgot"); setErrorMessage(""); setSuccessMessage(""); setForgotPasswordFormData({ email: loginFormData.email }); }}>Forgot password?</button>

                                <button type="submit" className="primary-btn" disabled={isAuthenticating}>
                                    {isAuthenticating ? "Signing in..." : "Sign in"}
                                </button>
                            </form>
                        ) : (
                            <form onSubmit={handleSignupSubmit} className="auth-form">
                                <label>
                                    Full name
                                    <input
                                        name="fullName"
                                        minLength="2"
                                        maxLength="100"
                                        value={signupFormData.fullName}
                                        onChange={(e) =>
                                            setSignupFormData({
                                                ...signupFormData,
                                                [e.target.name]: e.target.value,
                                            })
                                        }
                                        required
                                    />
                                </label>

                                <label>
                                    Email
                                    <input
                                        name="email"
                                        type="email"
                                        pattern={emailPattern}
                                        value={signupFormData.email}
                                        onChange={(e) =>
                                            setSignupFormData({
                                                ...signupFormData,
                                                [e.target.name]: e.target.value,
                                            })
                                        }
                                        required
                                    />
                                </label>

                                <label>
                                    Password
                                    <input
                                        name="password"
                                        type="password"
                                        minLength="12"
                                        maxLength="128"
                                        value={signupFormData.password}
                                        onChange={(e) =>
                                            setSignupFormData({
                                                ...signupFormData,
                                                [e.target.name]: e.target.value,
                                            })
                                        }
                                        required
                                    />
                                </label>

                                <label>
                                    Confirm password
                                    <input
                                        name="confirmPassword"
                                        type="password"
                                        minLength="12"
                                        maxLength="128"
                                        value={signupFormData.confirmPassword}
                                        onChange={(e) =>
                                            setSignupFormData({
                                                ...signupFormData,
                                                [e.target.name]: e.target.value,
                                            })
                                        }
                                        required
                                    />
                                </label>

                                <button type="submit" className="primary-btn" disabled={isAuthenticating}>
                                    {isAuthenticating ? "Creating account..." : "Create account"}
                                </button>
                            </form>
                        )}
                    </div>
                </section>
            </main>
        );
    }

    function renderEmployeeForm(formData, onChange, onSubmit, submitLabel) {
        return (
            <form onSubmit={onSubmit} className="form form-grid employee-form">
                <h3 className="span-2">Employment information</h3>
                <label>Employee code<input name="employeeCode" maxLength="30" value={formData.employeeCode} onChange={onChange} required /></label>
                <label>Work email<input name="email" type="email" pattern={emailPattern} value={formData.email} onChange={onChange} required /></label>
                <label>First name<input name="firstName" pattern={namePattern} minLength="2" maxLength="50" value={formData.firstName} onChange={onChange} required /></label>
                <label>Last name<input name="lastName" pattern={namePattern} minLength="2" maxLength="50" value={formData.lastName} onChange={onChange} required /></label>
                <label>Designation<input name="designation" list="designation-options" maxLength="100" value={formData.designation} onChange={onChange} required /><datalist id="designation-options">{designations.map(x => <option key={x} value={x} />)}</datalist></label>
                <label>Department<select name="departmentId" value={formData.departmentId} onChange={onChange}><option value="">No department</option>{departments.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
                <label>Manager<select name="managerId" value={formData.managerId} onChange={onChange}><option value="">No manager</option>{employeeOptions.filter(x => x.id !== formData.id).map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName}</option>)}</select></label>
                <label>Employment type<select name="employmentType" value={formData.employmentType} onChange={onChange}><option>Permanent</option><option>Contract</option><option>Intern</option><option>Consultant</option></select></label>
                <label>Status<select name="employmentStatus" value={formData.employmentStatus} onChange={onChange}><option>Active</option><option>On Leave</option><option>Notice Period</option><option>Inactive</option></select></label>
                <label>Work location<input name="workLocation" maxLength="100" value={formData.workLocation ?? ""} onChange={onChange} /></label>
                <label>Date of joining<input name="dateOfJoining" type="date" value={formData.dateOfJoining} onChange={onChange} required /></label>
                <label>Date of leaving<input name="dateOfLeaving" type="date" value={formData.dateOfLeaving ?? ""} onChange={onChange} /></label>

                <div className="button-row span-2">
                    <button type="submit" className="primary-btn" disabled={isSaving}>
                        {isSaving ? "Saving..." : submitLabel}
                    </button>
                    <button
                        type="button"
                        className="secondary-btn"
                        onClick={() => {
                            setCurrentPage("list");
                            setUpdateFormData(null);
                        }}
                        disabled={isSaving}
                    >
                        Cancel
                    </button>
                </div>
            </form>
        );
    }

    if (!currentUser) {
        return renderAuth();
    }

    const canManagePeople = ["HR", "Admin"].includes(currentUser.role);
    const canReviewLeave = ["Manager", "HR", "Admin"].includes(currentUser.role);
    const isAdmin = currentUser.role === "Admin";

    return (
        <div className="app">
            <header className="header">
                <div>
                    <h1>HRConnect</h1>
                    <p>People operations workspace</p>
                </div>
                <nav className="main-nav" aria-label="HRConnect modules">
                    {canManagePeople && <button className={currentPage === "list" ? "active" : ""} onClick={() => setCurrentPage("list")}>Employees</button>}
                    {canManagePeople && <button className={currentPage === "departments" ? "active" : ""} onClick={() => setCurrentPage("departments")}>Departments</button>}
                    <button className={currentPage === "leave" ? "active" : ""} onClick={() => setCurrentPage("leave")}>Leave</button>
                    {canManagePeople && <button className={currentPage === "users" ? "active" : ""} onClick={() => setCurrentPage("users")}>Users & roles</button>}
                    {currentUser.employeeId && <button onClick={() => { setProfileEmployeeId(currentUser.employeeId); setCurrentPage("profile"); }}>My profile</button>}
                </nav>
                <div className="user-menu">
                    <span>{currentUser.fullName}<small>{currentUser.role}</small></span>
                    <button type="button" className="secondary-btn" onClick={() => handleLogout()}>
                        Sign out
                    </button>
                </div>
            </header>

            <main className="container">
                {!isApiAvailable && <p className="api-status-banner" role="alert">HRConnect services are currently unavailable. Your changes have not been saved; please try again shortly.</p>}
                {successMessage && <p ref={notificationRef} className="success-message" role="status" tabIndex={-1}>{successMessage}</p>}
                {errorMessage && <p ref={notificationRef} className="error-message" role="alert" tabIndex={-1}>{errorMessage}</p>}

                {currentPage === "departments" && <DepartmentsPage canManage={canManagePeople} onError={showError} onSuccess={showSuccess} />}
                {currentPage === "leave" && <LeavePage currentUser={currentUser} employees={employeeOptions} canManagePolicy={canManagePeople} canReview={canReviewLeave} onError={showError} onSuccess={showSuccess} />}
                {currentPage === "users" && canManagePeople && <UsersPage employees={employeeOptions} isAdmin={isAdmin} onError={showError} onSuccess={showSuccess} />}
                {currentPage === "profile" && profileEmployeeId && <EmployeeProfilePage employeeId={profileEmployeeId} currentUser={currentUser} onBack={() => setCurrentPage(canManagePeople ? "list" : "leave")} onEdit={(employee) => { setUpdateFormData(employeeToForm(employee)); setCurrentPage("update"); }} onError={showError} onSuccess={showSuccess} />}

                {currentPage === "add" && (
                    <section className="card page-card">
                        <div className="page-header">
                            <h2>Add Employee</h2>
                            <button
                                type="button"
                                className="secondary-btn"
                                onClick={() => setCurrentPage("list")}
                            >
                                Back to Employees
                            </button>
                        </div>
                        {renderEmployeeForm(
                            addFormData,
                            (e) =>
                                setAddFormData({
                                    ...addFormData,
                                    [e.target.name]: e.target.value,
                                }),
                            handleAddSubmit,
                            "Add"
                        )}
                    </section>
                )}

                {currentPage === "update" && updateFormData && (
                    <section className="card page-card update-card">
                        <div className="page-header">
                            <h2>Update Employee</h2>
                            <button
                                type="button"
                                className="secondary-btn"
                                onClick={() => setCurrentPage("list")}
                            >
                                Back to Employees
                            </button>
                        </div>
                        {renderEmployeeForm(
                            updateFormData,
                            (e) =>
                                setUpdateFormData({
                                    ...updateFormData,
                                    [e.target.name]: e.target.value,
                                }),
                            handleUpdateSubmit,
                            "Update"
                        )}
                    </section>
                )}

                {currentPage === "list" && canManagePeople && (
                    <section className="card table-card">
                        <div className="table-header">
                            <h2>Employees</h2>
                            <div className="table-actions">
                                {canManagePeople && <button
                                    type="button"
                                    className="primary-btn"
                                    onClick={() => {
                                        setAddFormData(emptyEmployeeForm);
                                        setCurrentPage("add");
                                    }}
                                >
                                    Add Employee
                                </button>}
                                <p className="employee-count">Employee Count: {totalCount}</p>
                                <input
                                    className="search"
                                    placeholder="Search employees..."
                                    value={searchText}
                                    onChange={(e) => {
                                        setSearchText(e.target.value);
                                        setPageNumber(1);
                                    }}
                                />
                            </div>
                        </div>

                        <div className="table-scroll">
                            <table>
                                <thead>
                                    <tr>
                                        <th>Code</th>
                                        <th>Name</th>
                                        <th>Email</th>
                                        <th>Department</th>
                                        <th>Designation</th>
                                        <th>Status</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {isLoading ? (
                                        <tr>
                                            <td colSpan="7" className="empty">
                                                Loading employees...
                                            </td>
                                        </tr>
                                    ) : employees.length === 0 ? (
                                        <tr>
                                            <td colSpan="7" className="empty">
                                                No employees found
                                            </td>
                                        </tr>
                                    ) : (
                                        employees.map((employee) => (
                                            <tr key={employee.id}>
                                                <td><span className="code-pill">{employee.employeeCode}</span></td>
                                                <td>
                                                    {cleanName(employee.firstName)}{" "}
                                                    {cleanName(employee.lastName)}
                                                </td>
                                                <td>{employee.email}</td>
                                                <td>{employee.departmentName || "-"}</td>
                                                <td>{employee.designation}</td>
                                                <td><span className={`status-pill ${employee.employmentStatus === "Active" ? "approved" : "pending"}`}>{employee.employmentStatus}</span></td>
                                                <td>
                                                    <div className="action-buttons">
                                                        <button className="secondary-btn" onClick={() => { setProfileEmployeeId(employee.id); setCurrentPage("profile"); }}>View</button>
                                                        {canManagePeople && <>
                                                        <button
                                                            className="edit-btn"
                                                            onClick={() => openEmployeeForEdit(employee.id)}
                                                        >
                                                            Edit
                                                        </button>
                                                        {isAdmin &&
                                                        <button
                                                            className="delete-btn"
                                                            onClick={() => setEmployeeToDelete(employee)}
                                                        >
                                                            Delete
                                                        </button>}
                                                        </>}
                                                    </div>
                                                </td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>

                        <div className="pagination">
                            <button
                                type="button"
                                className="secondary-btn"
                                disabled={pageNumber <= 1 || isLoading}
                                onClick={() => setPageNumber((current) => Math.max(current - 1, 1))}
                            >
                                Previous
                            </button>
                            <span>
                                Page {pageNumber} of {Math.max(totalPages, 1)}
                            </span>
                            <button
                                type="button"
                                className="secondary-btn"
                                disabled={pageNumber >= totalPages || isLoading}
                                onClick={() => setPageNumber((current) => current + 1)}
                            >
                                Next
                            </button>
                        </div>
                    </section>
                )}
            </main>

            {employeeToDelete && (
                <div className="modal-backdrop" role="presentation">
                    <div className="modal" role="dialog" aria-modal="true" aria-labelledby="delete-title">
                        <h2 id="delete-title">Delete Employee</h2>
                        <p>
                            Are you sure you want to delete {cleanName(employeeToDelete.firstName)}{" "}
                            {cleanName(employeeToDelete.lastName)}?
                        </p>
                        <div className="modal-actions">
                            <button
                                type="button"
                                className="secondary-btn"
                                onClick={() => setEmployeeToDelete(null)}
                                disabled={isSaving}
                            >
                                Cancel
                            </button>
                            <button
                                type="button"
                                className="delete-btn"
                                onClick={async () => {
                                    setIsSaving(true);
                                    setErrorMessage("");

                                    try {
                                        await deleteEmployee(employeeToDelete.id);
                                        setSuccessMessage("Employee deleted successfully");
                                        setEmployeeToDelete(null);
                                        await loadEmployees();
                                    } catch {
                                        setErrorMessage("Unable to delete employee. Please try again.");
                                    } finally {
                                        setIsSaving(false);
                                    }
                                }}
                                disabled={isSaving}
                            >
                                {isSaving ? "Deleting..." : "Delete"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

export default HRApp;
