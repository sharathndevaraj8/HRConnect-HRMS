import { useEffect, useState } from "react";
import "./App.css";
import {
    getEmployees,
    createEmployee,
    updateEmployee,
    deleteEmployee,
} from "./services/employeeServices";

const emptyForm = {
    id: 0,
    firstName: "",
    lastName: "",
    email: "",
    designation: "",
    dateOfJoining: "",
};

const emailPattern = "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
const namePattern = "^[A-Za-z][A-Za-z.'-]*(?: [A-Za-z][A-Za-z.'-]*)*$";
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

const pageSize = 25;

function cleanName(value) {
    return String(value ?? "").trim().replace(/\s+/g, " ");
}

function cleanText(value) {
    return String(value ?? "").trim();
}

function getErrorMessage(error, fallbackMessage) {
    return error?.response?.data?.message ?? fallbackMessage;
}

function App() {
    const [employees, setEmployees] = useState([]);
    const [addFormData, setAddFormData] = useState(emptyForm);
    const [updateFormData, setUpdateFormData] = useState(null);
    const [currentPage, setCurrentPage] = useState("list");
    const [searchText, setSearchText] = useState("");
    const [pageNumber, setPageNumber] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);
    const [successMessage, setSuccessMessage] = useState("");
    const [errorMessage, setErrorMessage] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [employeeToDelete, setEmployeeToDelete] = useState(null);

    useEffect(() => {
        loadEmployees();
    }, [pageNumber, searchText]);

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
        } catch {
            setErrorMessage("Unable to load employees. Please try again.");
        } finally {
            setIsLoading(false);
        }
    }

    function handleAddChange(e) {
        setSuccessMessage("");
        setErrorMessage("");
        setAddFormData({
            ...addFormData,
            [e.target.name]: e.target.value,
        });
    }

    function handleUpdateChange(e) {
        setSuccessMessage("");
        setErrorMessage("");
        setUpdateFormData({
            ...updateFormData,
            [e.target.name]: e.target.value,
        });
    }

    function handleShowAddPage() {
        setSuccessMessage("");
        setErrorMessage("");
        setAddFormData(emptyForm);
        setUpdateFormData(null);
        setCurrentPage("add");
    }

    function handleBackToList() {
        setCurrentPage("list");
        setUpdateFormData(null);
    }

    function handleSearchChange(e) {
        setSearchText(e.target.value);
        setPageNumber(1);
    }

    async function handleAddSubmit(e) {
        e.preventDefault();
        const firstName = cleanName(addFormData.firstName);
        const lastName = cleanName(addFormData.lastName);

        setIsSaving(true);
        setErrorMessage("");

        try {
            await createEmployee({
                firstName,
                lastName,
                email: addFormData.email.trim().toLowerCase(),
                designation: cleanText(addFormData.designation),
                dateOfJoining: addFormData.dateOfJoining,
            });

            setAddFormData(emptyForm);
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
        const firstName = cleanName(updateFormData.firstName);
        const lastName = cleanName(updateFormData.lastName);

        setIsSaving(true);
        setErrorMessage("");

        try {
            await updateEmployee(updateFormData.id, {
                ...updateFormData,
                firstName,
                lastName,
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

    function handleEdit(employee) {
        setSuccessMessage("");
        setErrorMessage("");
        setUpdateFormData({
            id: employee.id,
            firstName: cleanName(employee.firstName),
            lastName: cleanName(employee.lastName),
            email: employee.email,
            designation: cleanText(employee.designation),
            dateOfJoining: employee.dateOfJoining?.split("T")[0],
        });
        setCurrentPage("update");
    }

    function handleDelete(employee) {
        setSuccessMessage("");
        setErrorMessage("");
        setEmployeeToDelete(employee);
    }

    function handleCloseDeleteModal() {
        setEmployeeToDelete(null);
    }

    async function handleConfirmDelete() {
        setIsSaving(true);
        setErrorMessage("");

        try {
            await deleteEmployee(employeeToDelete.id);
            setSuccessMessage("Employee deleted successfully");
            setUpdateFormData(null);
            setCurrentPage("list");
            setEmployeeToDelete(null);
            await loadEmployees();
        } catch {
            setErrorMessage("Unable to delete employee. Please try again.");
        } finally {
            setIsSaving(false);
        }
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
                        onClick={handleBackToList}
                        disabled={isSaving}
                    >
                        Cancel
                    </button>
                </div>
            </form>
        );
    }

    return (
        <div className="app">
            <header className="header">
                <div>
                    <h1>HRConnect</h1>
                    <p>Employee Management Dashboard</p>
                </div>
            </header>

            <main className="container">
                {successMessage && <p className="success-message">{successMessage}</p>}
                {errorMessage && <p className="error-message">{errorMessage}</p>}

                {currentPage === "add" && (
                    <section className="card page-card">
                        <div className="page-header">
                            <h2>Add Employee</h2>
                            <button type="button" className="secondary-btn" onClick={handleBackToList}>
                                Back to Employees
                            </button>
                        </div>

                        {renderEmployeeForm(addFormData, handleAddChange, handleAddSubmit, "Add")}
                    </section>
                )}

                {currentPage === "update" && updateFormData && (
                    <section className="card page-card update-card">
                        <div className="page-header">
                            <h2>Update Employee</h2>
                            <button type="button" className="secondary-btn" onClick={handleBackToList}>
                                Back to Employees
                            </button>
                        </div>

                        {renderEmployeeForm(
                            updateFormData,
                            handleUpdateChange,
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
                                    onClick={handleShowAddPage}
                                >
                                    Add Employee
                                </button>

                                <p className="employee-count">Employee Count: {totalCount}</p>

                                <input
                                    className="search"
                                    placeholder="Search employees..."
                                    value={searchText}
                                    onChange={handleSearchChange}
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
                                                            onClick={() => handleEdit(employee)}
                                                        >
                                                            Edit
                                                        </button>

                                                        <button
                                                            className="delete-btn"
                                                            onClick={() => handleDelete(employee)}
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
                                onClick={handleCloseDeleteModal}
                                disabled={isSaving}
                            >
                                Cancel
                            </button>

                            <button
                                type="button"
                                className="delete-btn"
                                onClick={handleConfirmDelete}
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

export default App;
