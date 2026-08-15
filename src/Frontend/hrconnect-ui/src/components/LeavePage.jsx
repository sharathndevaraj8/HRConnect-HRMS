import { useEffect, useState } from "react";
import { cancelLeaveRequest, createLeaveRequest, createLeaveType, getLeaveBalances, getLeaveRequests, getLeaveTypes, reviewLeaveRequest, updateLeaveType } from "../services/employeeServices";

const emptyRequest = { employeeId: "", leaveTypeId: "", startDate: "", endDate: "", isHalfDay: false, reason: "", contactDuringLeave: "" };
const emptyPolicy = { code: "", name: "", description: "", annualEntitlement: 0, carryForwardLimit: 0, maxConsecutiveDays: "", documentRequiredAfterDays: "", isPaid: true, allowsHalfDay: true, isActive: true, applicableGender: "", sortOrder: 10 };

export default function LeavePage({ currentUser, employees, canManagePolicy, canReview, onError, onSuccess }) {
    const [types, setTypes] = useState([]);
    const [balances, setBalances] = useState([]);
    const [requests, setRequests] = useState([]);
    const [requestForm, setRequestForm] = useState({ ...emptyRequest, employeeId: currentUser.employeeId ?? "" });
    const [policyForm, setPolicyForm] = useState(emptyPolicy);
    const [editingPolicyId, setEditingPolicyId] = useState(null);
    const [linkWarning, setLinkWarning] = useState("");

    async function load() {
        try {
            const [leaveTypes, leaveRequests] = await Promise.all([getLeaveTypes(canManagePolicy), getLeaveRequests()]);
            setTypes(leaveTypes); setRequests(leaveRequests);
            if (currentUser.employeeId) { setBalances(await getLeaveBalances()); setLinkWarning(""); }
            else setLinkWarning("Link your login under Users & roles to enable personal leave balances and requests.");
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to load leave information."); }
    }
    useEffect(() => { const timer = setTimeout(load, 0); return () => clearTimeout(timer); }, []); // eslint-disable-line react-hooks/exhaustive-deps

    async function submitRequest(event) {
        event.preventDefault();
        try {
            await createLeaveRequest({ ...requestForm, employeeId: requestForm.employeeId ? Number(requestForm.employeeId) : null, leaveTypeId: Number(requestForm.leaveTypeId) });
            setRequestForm({ ...emptyRequest, employeeId: currentUser.employeeId ?? "" }); onSuccess("Leave request submitted."); await load();
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to submit leave request."); }
    }

    async function submitPolicy(event) {
        event.preventDefault();
        const payload = { ...policyForm, annualEntitlement: Number(policyForm.annualEntitlement), carryForwardLimit: Number(policyForm.carryForwardLimit), maxConsecutiveDays: policyForm.maxConsecutiveDays === "" ? null : Number(policyForm.maxConsecutiveDays), documentRequiredAfterDays: policyForm.documentRequiredAfterDays === "" ? null : Number(policyForm.documentRequiredAfterDays), applicableGender: policyForm.applicableGender || null, sortOrder: Number(policyForm.sortOrder) };
        try {
            if (editingPolicyId) await updateLeaveType(editingPolicyId, payload); else await createLeaveType(payload);
            setPolicyForm(emptyPolicy); setEditingPolicyId(null); onSuccess("Leave policy saved."); await load();
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to save leave policy."); }
    }

    return <div className="module-stack">
        <section className="card"><div className="page-header"><div><p className="eyebrow">Time off</p><h2>Leave management</h2><p>Policy categories follow MRI India&apos;s publicly listed benefits and remain configurable by HR.</p></div></div>
            {linkWarning && <p className="info-message">{linkWarning}</p>}
            {balances.length > 0 && <div className="balance-grid">{balances.map(balance => <div className="balance-card" key={balance.id}><span>{balance.name}</span><strong>{balance.available}</strong><small>{balance.used} used · {balance.accrued + balance.openingBalance + balance.adjustment} credited</small></div>)}</div>}
        </section>

        {(currentUser.employeeId || canManagePolicy) && <section className="card"><h3>Request leave</h3><form className="form form-grid" onSubmit={submitRequest}>
            {canManagePolicy && <label>Employee<select value={requestForm.employeeId} onChange={e => setRequestForm({ ...requestForm, employeeId: e.target.value })} required><option value="">Select employee</option>{employees.map(x => <option key={x.id} value={x.id}>{x.employeeCode} · {x.firstName} {x.lastName}</option>)}</select></label>}
            <label>Leave type<select value={requestForm.leaveTypeId} onChange={e => setRequestForm({ ...requestForm, leaveTypeId: e.target.value })} required><option value="">Select leave type</option>{types.filter(x => x.isActive).map(type => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label>
            <label>Start date<input type="date" value={requestForm.startDate} onChange={e => setRequestForm({ ...requestForm, startDate: e.target.value })} required /></label>
            <label>End date<input type="date" value={requestForm.endDate} onChange={e => setRequestForm({ ...requestForm, endDate: e.target.value })} required /></label>
            <label className="check-field"><input type="checkbox" checked={requestForm.isHalfDay} onChange={e => setRequestForm({ ...requestForm, isHalfDay: e.target.checked })} /> Half day</label>
            <label className="span-2">Reason<textarea maxLength="1000" value={requestForm.reason} onChange={e => setRequestForm({ ...requestForm, reason: e.target.value })} required /></label>
            <label>Contact during leave<input value={requestForm.contactDuringLeave} onChange={e => setRequestForm({ ...requestForm, contactDuringLeave: e.target.value })} /></label>
            <div><button className="primary-btn">Submit request</button></div>
        </form></section>}

        <section className="card"><div className="page-header"><div><h3>Leave requests</h3><p>{canReview ? "Review your own and authorized team requests." : "Track your submitted requests."}</p></div></div>
            <div className="table-scroll"><table><thead><tr><th>Employee</th><th>Type</th><th>Dates</th><th>Days</th><th>Reason</th><th>Status</th><th>Actions</th></tr></thead><tbody>
                {requests.length === 0 ? <tr><td colSpan="7" className="empty">No leave requests.</td></tr> : requests.map(request => <tr key={request.id}><td>{request.employeeName}<br /><small>{request.employeeCode}</small></td><td>{request.leaveTypeName}</td><td>{request.startDate?.split("T")[0]} → {request.endDate?.split("T")[0]}</td><td>{request.numberOfDays}</td><td>{request.reason}</td><td><span className={`status-pill ${request.status.toLowerCase()}`}>{request.status}</span></td><td><div className="action-buttons">
                    {canReview && request.status === "Pending" && request.employeeId !== currentUser.employeeId && <><button className="approve-btn" onClick={async () => { try { await reviewLeaveRequest(request.id, 2); onSuccess("Leave approved."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to approve request."); } }}>Approve</button><button className="delete-btn" onClick={async () => { try { await reviewLeaveRequest(request.id, 3); onSuccess("Leave rejected."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to reject request."); } }}>Reject</button></>}
                    {request.status === "Pending" && (request.employeeId === currentUser.employeeId || canManagePolicy) && <button className="secondary-btn" onClick={async () => { try { await cancelLeaveRequest(request.id); onSuccess("Leave request cancelled."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to cancel request."); } }}>Cancel</button>}
                </div></td></tr>)}</tbody></table></div>
        </section>

        {canManagePolicy && <section className="card"><div className="page-header"><div><h3>Leave policy configuration</h3><p>Entitlements are defaults and should be aligned with the internal policy handbook.</p></div></div>
            <form className="form form-grid policy-form" onSubmit={submitPolicy}>
                <label>Code<input value={policyForm.code} onChange={e => setPolicyForm({ ...policyForm, code: e.target.value })} required /></label><label>Name<input value={policyForm.name} onChange={e => setPolicyForm({ ...policyForm, name: e.target.value })} required /></label>
                <label>Annual entitlement<input type="number" min="0" step="0.5" value={policyForm.annualEntitlement} onChange={e => setPolicyForm({ ...policyForm, annualEntitlement: e.target.value })} /></label><label>Carry-forward limit<input type="number" min="0" step="0.5" value={policyForm.carryForwardLimit} onChange={e => setPolicyForm({ ...policyForm, carryForwardLimit: e.target.value })} /></label>
                <label>Max consecutive days<input type="number" min="0.5" step="0.5" value={policyForm.maxConsecutiveDays} onChange={e => setPolicyForm({ ...policyForm, maxConsecutiveDays: e.target.value })} /></label><label>Document required after<input type="number" min="0.5" step="0.5" value={policyForm.documentRequiredAfterDays} onChange={e => setPolicyForm({ ...policyForm, documentRequiredAfterDays: e.target.value })} /></label>
                <label className="span-2">Description<textarea value={policyForm.description} onChange={e => setPolicyForm({ ...policyForm, description: e.target.value })} /></label>
                <label className="check-field"><input type="checkbox" checked={policyForm.isPaid} onChange={e => setPolicyForm({ ...policyForm, isPaid: e.target.checked })} /> Paid</label><label className="check-field"><input type="checkbox" checked={policyForm.allowsHalfDay} onChange={e => setPolicyForm({ ...policyForm, allowsHalfDay: e.target.checked })} /> Half-day allowed</label>
                <div className="button-row span-2"><button className="primary-btn">{editingPolicyId ? "Update policy" : "Add policy"}</button>{editingPolicyId && <button type="button" className="secondary-btn" onClick={() => { setEditingPolicyId(null); setPolicyForm(emptyPolicy); }}>Cancel</button>}</div>
            </form>
            <div className="policy-list">{types.map(type => <button key={type.id} className="policy-row" onClick={() => { setEditingPolicyId(type.id); setPolicyForm({ code: type.code, name: type.name, description: type.description ?? "", annualEntitlement: type.annualEntitlement, carryForwardLimit: type.carryForwardLimit, maxConsecutiveDays: type.maxConsecutiveDays ?? "", documentRequiredAfterDays: type.documentRequiredAfterDays ?? "", isPaid: type.isPaid, allowsHalfDay: type.allowsHalfDay, isActive: type.isActive, applicableGender: type.applicableGender ?? "", sortOrder: type.sortOrder }); }}><strong>{type.name}</strong><span>{type.annualEntitlement} days · {type.isPaid ? "Paid" : "Unpaid"}</span></button>)}</div>
        </section>}
    </div>;
}
