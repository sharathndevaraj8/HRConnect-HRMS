import { useEffect, useState } from "react";
import {
    getEmployees,
    createEmployee,
    updateEmployee,
    deleteEmployee,
    loginUser,
    signupUser,
    refreshSession,
    logoutUser,
} from "./services/employeeServices";

const emptyEmployeeForm = {
    id: 0,
    firstName: "",
    lastName: "",
    email: "",
    designation: "",
    dateOfJoining: "",
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

const emailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
const namePattern = "^[A-Za-z][A-Za-z.'-]*(?: [A-Za-z][A-Za-z.'-]*)*$";
const pageSize = 25;
const tokenStorageKey = "hrconnect_token";
const userStorageKey = "hrconnect_user";

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

function HRApp() {
    const [employees, setEmployees] = useState([]);
    const [addFormData, setAddFormData] = useState(emptyEmployeeForm);
    const [updateFormData, setUpdateFormData] = useState(null);
    const [authMode, setAuthMode] = useState("login");
    const [loginFormData, setLoginFormData] = useState(emptyLoginForm);
    const [signupFormData, setSignupFormData] = useState(emptySignupForm);
    const [currentUser, setCurrentUser] = useState(() => getStoredUser());
    const [currentPage, setCurrentPage] = useState("list");
    const [searchText, setSearchText] = useState("");
    const [pageNumber, setPageNumber] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);
    const [successMessage, setSuccessMessage] = useState("");
    const [errorMessage, setErrorMessage] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [isAuthenticating, setIsAuthenticating] = useState(false);
    const [employeeToDelete, setEmployeeToDelete] = useState(null);

    useEffect(() => {
        if (!currentUser) {
            restoreSession();
        }
        // restoreSession intentionally runs only at startup or after a full logout.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (currentUser) {
            loadEmployees();
        }
        // loadEmployees closes over the current pagination/search state by design.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentUser, pageNumber, searchText]);

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

    async function handleAddSubmit(e) {
        e.preventDefault();
        setIsSaving(true);
        setErrorMessage("");

        try {
            await createEmployee({
                firstName: cleanName(addFormData.firstName),
                lastName: cleanName(addFormData.lastName),
                email: addFormData.email.trim().toLowerCase(),
                designation: cleanText(addFormData.designation),
                dateOfJoining: addFormData.dateOfJoining,
            });

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
            await updateEmployee(updateFormData.id, {
                ...updateFormData,
                firstName: cleanName(updateFormData.firstName),
                lastName: cleanName(updateFormData.lastName),
                email: updateFormData.email.trim().toLowerCase(),
                designation: cleanText(updateFormData.designation),
                id: Number(updateFormData.id),
            });

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

    function renderAuth() {
        const isSignup = authMode === "signup";

        return (
            <main className="auth-shell">
                <section className="auth-panel">
                    <div className="auth-copy">
                        <p className="eyebrow">HRConnect</p>
                        <h1>Secure employee operations</h1>
                        <p>Sign in to manage employee records with protected API access.</p>
                    </div>

                    <div className="auth-card">
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

                        {successMessage && <p className="success-message">{successMessage}</p>}
                        {errorMessage && <p className="error-message">{errorMessage}</p>}

                        {!isSignup ? (
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
            <form onSubmit={onSubmit} className="form">
                <input
                    name="firstName"
                    pattern={namePattern}
                    minLength="2"
                    maxLength="50"
                    title="Enter 2-50 letters. Single spaces, periods, apostrophes, and hyphens are allowed."
                    placeholder="First Name"
                    value={formData.firstName}
                    onChange={onChange}
                    required
                />
                <input
                    name="lastName"
                    pattern={namePattern}
                    minLength="2"
                    maxLength="50"
                    title="Enter 2-50 letters. Single spaces, periods, apostrophes, and hyphens are allowed."
                    placeholder="Last Name"
                    value={formData.lastName}
                    onChange={onChange}
                    required
                />
                <input
                    name="email"
                    type="email"
                    pattern={emailPattern}
                    title="Enter an email like someone@something.example"
                    placeholder="Email"
                    value={formData.email}
                    onChange={onChange}
                    required
                />
                <select
                    name="designation"
                    value={formData.designation}
                    onChange={onChange}
                    required
                >
                    <option value="">Select Designation</option>
                    {designations.map((designation) => (
                        <option key={designation} value={designation}>
                            {designation}
                        </option>
                    ))}
                </select>
                <input
                    name="dateOfJoining"
                    type="date"
                    value={formData.dateOfJoining}
                    onChange={onChange}
                    required
                />
                <div className="button-row">
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

    return (
        <div className="app">
            <header className="header">
                <div>
                    <h1>HRConnect</h1>
                    <p>Employee Management Dashboard</p>
                </div>
                <div className="user-menu">
                    <span>{currentUser.fullName}</span>
                    <button type="button" className="secondary-btn" onClick={() => handleLogout()}>
                        Sign out
                    </button>
                </div>
            </header>

            <main className="container">
                {successMessage && <p className="success-message">{successMessage}</p>}
                {errorMessage && <p className="error-message">{errorMessage}</p>}

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

                {currentPage === "list" && (
                    <section className="card table-card">
                        <div className="table-header">
                            <h2>Employees</h2>
                            <div className="table-actions">
                                <button
                                    type="button"
                                    className="primary-btn"
                                    onClick={() => {
                                        setAddFormData(emptyEmployeeForm);
                                        setCurrentPage("add");
                                    }}
                                >
                                    Add Employee
                                </button>
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
                                        <th>Id</th>
                                        <th>Name</th>
                                        <th>Email</th>
                                        <th>Designation</th>
                                        <th>Date of Joining</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {isLoading ? (
                                        <tr>
                                            <td colSpan="6" className="empty">
                                                Loading employees...
                                            </td>
                                        </tr>
                                    ) : employees.length === 0 ? (
                                        <tr>
                                            <td colSpan="6" className="empty">
                                                No employees found
                                            </td>
                                        </tr>
                                    ) : (
                                        employees.map((employee) => (
                                            <tr key={employee.id}>
                                                <td>{employee.id}</td>
                                                <td>
                                                    {cleanName(employee.firstName)}{" "}
                                                    {cleanName(employee.lastName)}
                                                </td>
                                                <td>{employee.email}</td>
                                                <td>{employee.designation}</td>
                                                <td>{employee.dateOfJoining?.split("T")[0]}</td>
                                                <td>
                                                    <div className="action-buttons">
                                                        <button
                                                            className="edit-btn"
                                                            onClick={() => {
                                                                setUpdateFormData({
                                                                    id: employee.id,
                                                                    firstName: cleanName(employee.firstName),
                                                                    lastName: cleanName(employee.lastName),
                                                                    email: employee.email,
                                                                    designation: cleanText(employee.designation),
                                                                    dateOfJoining: employee.dateOfJoining?.split("T")[0],
                                                                });
                                                                setCurrentPage("update");
                                                            }}
                                                        >
                                                            Edit
                                                        </button>
                                                        <button
                                                            className="delete-btn"
                                                            onClick={() => setEmployeeToDelete(employee)}
                                                        >
                                                            Delete
                                                        </button>
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
