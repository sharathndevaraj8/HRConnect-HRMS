import { useEffect, useState } from "react";
import { createDepartment, deleteDepartment, getDepartments, updateDepartment } from "../services/employeeServices";

const emptyDepartment = { code: "", name: "", description: "", isActive: true };

export default function DepartmentsPage({ canManage, onError, onSuccess }) {
    const [departments, setDepartments] = useState([]);
    const [form, setForm] = useState(emptyDepartment);
    const [editingId, setEditingId] = useState(null);
    async function load() { try { setDepartments(await getDepartments(canManage)); } catch (error) { onError(error?.response?.data?.message ?? "Unable to load departments."); } }
    useEffect(() => { const timer = setTimeout(load, 0); return () => clearTimeout(timer); }, []); // eslint-disable-line react-hooks/exhaustive-deps
    async function submit(event) {
        event.preventDefault();
        try {
            if (editingId) await updateDepartment(editingId, form); else await createDepartment(form);
            onSuccess(editingId ? "Department updated." : "Department created.");
            setForm(emptyDepartment); setEditingId(null); await load();
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to save department."); }
    }
    return <section className="card module-card">
        <div className="page-header"><div><p className="eyebrow">Organization</p><h2>Departments</h2></div></div>
        {canManage && <form className="inline-form" onSubmit={submit}>
            <input placeholder="Code" maxLength="20" value={form.code} onChange={e => setForm({ ...form, code: e.target.value })} required />
            <input placeholder="Department name" maxLength="100" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required />
            <input placeholder="Description" maxLength="500" value={form.description ?? ""} onChange={e => setForm({ ...form, description: e.target.value })} />
            <label className="check-field"><input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} /> Active</label>
            <button className="primary-btn">{editingId ? "Update" : "Add department"}</button>
            {editingId && <button type="button" className="secondary-btn" onClick={() => { setEditingId(null); setForm(emptyDepartment); }}>Cancel</button>}
        </form>}
        <div className="table-scroll"><table><thead><tr><th>Code</th><th>Name</th><th>Description</th><th>Employees</th><th>Status</th>{canManage && <th>Actions</th>}</tr></thead>
            <tbody>{departments.map(department => <tr key={department.id}>
                <td><span className="code-pill">{department.code}</span></td><td>{department.name}</td><td>{department.description || "—"}</td>
                <td>{department.employeeCount}</td><td><span className={`status-pill ${department.isActive ? "approved" : "cancelled"}`}>{department.isActive ? "Active" : "Inactive"}</span></td>
                {canManage && <td><div className="action-buttons"><button className="edit-btn" onClick={() => { setEditingId(department.id); setForm({ code: department.code, name: department.name, description: department.description ?? "", isActive: department.isActive }); }}>Edit</button>
                    <button className="delete-btn" onClick={async () => { try { await deleteDepartment(department.id); onSuccess("Department deleted."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to delete department."); } }}>Delete</button></div></td>}
            </tr>)}</tbody></table></div>
    </section>;
}
