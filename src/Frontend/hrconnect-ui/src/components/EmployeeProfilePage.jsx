import { useEffect, useState } from "react";
import { deleteEmployeeDocument, downloadEmployeeDocument, getEmployee, getEmployeeDocuments, uploadEmployeeDocument } from "../services/employeeServices";

export default function EmployeeProfilePage({ employeeId, currentUser, onBack, onEdit, onError, onSuccess }) {
    const [employee, setEmployee] = useState(null);
    const [documents, setDocuments] = useState([]);
    const [documentType, setDocumentType] = useState("AadhaarCard");
    const canManage = ["HR", "Admin"].includes(currentUser.role);
    const canAccessDocuments = canManage || currentUser.employeeId === employeeId;
    async function load() {
        try {
            setEmployee(await getEmployee(employeeId));
            if (canAccessDocuments) setDocuments(await getEmployeeDocuments(employeeId));
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to load employee profile."); }
    }
    async function downloadDocument(employeeDocument) {
        try {
            await downloadEmployeeDocument(employeeId, employeeDocument);
        } catch (error) {
            onError(error?.response?.status === 404
                ? "This document is listed, but its stored file is missing. Upload it again."
                : "Unable to download document. Please try again.");
        }
    }
    useEffect(() => { const timer = setTimeout(load, 0); return () => clearTimeout(timer); }, [employeeId]); // eslint-disable-line react-hooks/exhaustive-deps
    if (!employee) return <section className="card"><p>Loading employee profile…</p></section>;
    const fields = [
        ["Employee code", employee.employeeCode], ["Work email", employee.email], ["Personal email", employee.personalEmail],
        ["Phone", employee.phoneNumber], ["Alternate phone", employee.alternatePhoneNumber], ["Date of birth", employee.dateOfBirth?.split("T")[0]],
        ["Gender", employee.gender], ["Marital status", employee.maritalStatus], ["Blood group", employee.bloodGroup],
        ["Department", employee.departmentName], ["Manager", employee.managerName], ["Designation", employee.designation],
        ["Employment type", employee.employmentType], ["Status", employee.employmentStatus], ["Work location", employee.workLocation],
        ["Joined", employee.dateOfJoining?.split("T")[0]], ["Address", [employee.addressLine1, employee.addressLine2, employee.city, employee.state, employee.postalCode, employee.country].filter(Boolean).join(", ")],
        ["Emergency contact", [employee.emergencyContactName, employee.emergencyContactRelationship, employee.emergencyContactPhone].filter(Boolean).join(" · ")]
    ];
    return <div className="module-stack"><section className="card profile-hero"><div><p className="eyebrow">Employee profile</p><h2>{employee.firstName} {employee.lastName}</h2><p>{employee.designation} · {employee.departmentName || "No department"}</p></div>
        <div className="button-row"><button className="secondary-btn" onClick={onBack}>Back</button>{canManage && <button className="primary-btn" onClick={() => onEdit(employee)}>Edit profile</button>}</div></section>
        <section className="card"><h3>Personal and employment information</h3><div className="detail-grid">{fields.map(([label, value]) => <div key={label}><span>{label}</span><strong>{value || "—"}</strong></div>)}</div></section>
        {canAccessDocuments && <section className="card"><div className="page-header"><div><h3>Secure documents</h3><p>PAN, Aadhaar and other identity files are visible only to the employee and HR/Admin.</p></div></div>
            <form className="upload-form" onSubmit={async event => { event.preventDefault(); const file = event.currentTarget.elements.file.files[0]; if (!file) return; try { await uploadEmployeeDocument(employeeId, documentType, file); event.currentTarget.reset(); onSuccess("Document uploaded securely."); await load(); } catch (error) { onError(error?.response?.data?.message ?? "Unable to upload document."); } }}>
                <select value={documentType} onChange={e => setDocumentType(e.target.value)}>{['PanCard','AadhaarCard','ProfilePhoto','Passport','Education','Employment','Other'].map(type => <option key={type}>{type}</option>)}</select>
                <input name="file" type="file" accept=".pdf,.jpg,.jpeg,.png" required /><button className="primary-btn">Upload</button>
            </form>
            <div className="document-list">{documents.length === 0 ? <p className="empty">No documents uploaded.</p> : documents.map(doc => <div className="document-item" key={doc.id}><div><strong>{doc.documentType}</strong><span>{doc.originalFileName} · {(doc.fileSize / 1024).toFixed(1)} KB</span></div><div className="action-buttons"><button type="button" className="secondary-btn" onClick={() => downloadDocument(doc)}>Download</button><button type="button" className="delete-btn" onClick={async () => { try { await deleteEmployeeDocument(employeeId, doc.id); onSuccess("Document deleted."); await load(); } catch { onError("Unable to delete document."); } }}>Delete</button></div></div>)}</div>
        </section>}
    </div>;
}
