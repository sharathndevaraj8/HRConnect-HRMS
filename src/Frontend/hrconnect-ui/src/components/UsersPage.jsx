import { useEffect, useState } from "react";
import { changeUserRole, getUsers, linkUserEmployee } from "../services/employeeServices";

export default function UsersPage({ employees, isAdmin, onError, onSuccess }) {
    const [users, setUsers] = useState([]);
    async function load() { try { setUsers(await getUsers()); } catch (error) { onError(error?.response?.data?.message ?? "Unable to load users."); } }
    useEffect(() => { const timer = setTimeout(load, 0); return () => clearTimeout(timer); }, []); // eslint-disable-line react-hooks/exhaustive-deps
    return <section className="card module-card">
        <div className="page-header"><div><p className="eyebrow">Access control</p><h2>Users and roles</h2><p>Link login accounts to employee profiles for self-service leave and documents.</p></div></div>
        <div className="table-scroll"><table><thead><tr><th>User</th><th>Role</th><th>Employee profile</th><th>Status</th></tr></thead><tbody>
            {users.map(user => <tr key={user.id}><td><strong>{user.fullName}</strong><br /><small>{user.email}</small></td>
                <td><select value={user.role} disabled={!isAdmin} onChange={async e => { try { await changeUserRole(user.id, e.target.value); onSuccess("Role updated. The user must sign in again for it to take effect."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to update role."); } }}>
                    {['Employee','Manager','HR','Admin'].map(role => <option key={role}>{role}</option>)}</select></td>
                <td><select value={user.employeeId ?? ""} onChange={async e => { try { await linkUserEmployee(user.id, e.target.value ? Number(e.target.value) : null); onSuccess("Employee profile link updated."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to link employee."); } }}>
                    <option value="">Not linked</option>{employees.map(employee => <option key={employee.id} value={employee.id}>{employee.employeeCode} · {employee.firstName} {employee.lastName}</option>)}</select></td>
                <td><span className={`status-pill ${user.isActive ? "approved" : "cancelled"}`}>{user.isActive ? "Active" : "Inactive"}</span></td></tr>)}
        </tbody></table></div>
    </section>;
}
