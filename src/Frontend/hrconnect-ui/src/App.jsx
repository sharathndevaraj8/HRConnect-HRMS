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

function App() {
    const [employees, setEmployees] = useState([]);
    const [formData, setFormData] = useState(emptyForm);
    const [searchText, setSearchText] = useState("");
    const [isEditing, setIsEditing] = useState(false);

    useEffect(() => {
        loadEmployees();
    }, []);

    async function loadEmployees() {
        const data = await getEmployees();
        setEmployees(data);
    }

    function handleChange(e) {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value,
        });
    }

    async function handleSubmit(e) {
        e.preventDefault();

        if (isEditing) {
            await updateEmployee(formData.id, {
                ...formData,
                id: Number(formData.id),
            });
        } else {
            await createEmployee({
                firstName: formData.firstName,
                lastName: formData.lastName,
                email: formData.email,
                designation: formData.designation,
                dateOfJoining: formData.dateOfJoining,
            });
        }

        setFormData(emptyForm);
        setIsEditing(false);
        await loadEmployees();
    }

    function handleEdit(employee) {
        setIsEditing(true);
        setFormData({
            id: employee.id,
            firstName: employee.firstName,
            lastName: employee.lastName,
            email: employee.email,
            designation: employee.designation,
            dateOfJoining: employee.dateOfJoining?.split("T")[0],
        });
    }

    async function handleDelete(id) {
        const confirmDelete = window.confirm("Are you sure you want to delete this employee?");

        if (!confirmDelete) return;

        await deleteEmployee(id);
        await loadEmployees();
    }

    function handleCancel() {
        setFormData(emptyForm);
        setIsEditing(false);
    }

    const filteredEmployees = employees.filter((employee) => {
        const search = searchText.toLowerCase();

        return (
            employee.firstName?.toLowerCase().includes(search) ||
            employee.lastName?.toLowerCase().includes(search) ||
            employee.email?.toLowerCase().includes(search) ||
            employee.designation?.toLowerCase().includes(search)
        );
    });

    return (
        <div className="app">
            <header className="header">
                <div>
                    <h1>HRConnect</h1>
                    <p>Employee Management Dashboard</p>
                </div>
            </header>

            <main className="container">
                <section className="card form-card">
                    <h2>{isEditing ? "Update Employee" : "Add Employee"}</h2>

                    <form onSubmit={handleSubmit} className="form">
                        <input
                            name="firstName"
                            placeholder="First Name"
                            value={formData.firstName}
                            onChange={handleChange}
                            required
                        />

                        <input
                            name="lastName"
                            placeholder="Last Name"
                            value={formData.lastName}
                            onChange={handleChange}
                            required
                        />

                        <input
                            name="email"
                            type="email"
                            placeholder="Email"
                            value={formData.email}
                            onChange={handleChange}
                            required
                        />

                        <input
                            name="designation"
                            placeholder="Designation"
                            value={formData.designation}
                            onChange={handleChange}
                            required
                        />

                        <input
                            name="dateOfJoining"
                            type="date"
                            value={formData.dateOfJoining}
                            onChange={handleChange}
                            required
                        />

                        <div className="button-row">
                            <button type="submit" className="primary-btn">
                                {isEditing ? "Update" : "Add"}
                            </button>

                            {isEditing && (
                                <button type="button" className="secondary-btn" onClick={handleCancel}>
                                    Cancel
                                </button>
                            )}
                        </div>
                    </form>
                </section>

                <section className="card table-card">
                    <div className="table-header">
                        <h2>Employees</h2>

                        <input
                            className="search"
                            placeholder="Search employees..."
                            value={searchText}
                            onChange={(e) => setSearchText(e.target.value)}
                        />
                    </div>

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
                            {filteredEmployees.length === 0 ? (
                                <tr>
                                    <td colSpan="6" className="empty">
                                        No employees found
                                    </td>
                                </tr>
                            ) : (
                                filteredEmployees.map((employee) => (
                                    <tr key={employee.id}>
                                        <td>{employee.id}</td>
                                        <td>
                                            {employee.firstName} {employee.lastName}
                                        </td>
                                        <td>{employee.email}</td>
                                        <td>{employee.designation}</td>
                                        <td>{employee.dateOfJoining?.split("T")[0]}</td>
                                        <td>
                                            <button
                                                className="edit-btn"
                                                onClick={() => handleEdit(employee)}
                                            >
                                                Edit
                                            </button>

                                            <button
                                                className="delete-btn"
                                                onClick={() => handleDelete(employee.id)}
                                            >
                                                Delete
                                            </button>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </section>
            </main>
        </div>
    );
}

export default App;
