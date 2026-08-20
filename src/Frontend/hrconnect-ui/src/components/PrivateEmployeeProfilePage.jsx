import { useEffect, useState } from "react";
import {
    deleteEmployeeDocument,
    downloadEmployeeDocument,
    getEmployee,
    getEmployeeDocuments,
    getPersonalEmployee,
    updatePersonalEmployee,
    uploadEmployeeDocument,
} from "../services/employeeServices";

const emptyPersonalDetails = {
    personalEmail: "", phoneNumber: "", alternatePhoneNumber: "", dateOfBirth: "",
    gender: "", maritalStatus: "", bloodGroup: "", addressLine1: "", addressLine2: "",
    city: "", state: "", postalCode: "", country: "India", emergencyContactName: "",
    emergencyContactRelationship: "", emergencyContactPhone: "",
};

function personalToForm(details) {
    return { ...emptyPersonalDetails, ...details, dateOfBirth: details.dateOfBirth?.split("T")[0] ?? "" };
}

export default function PrivateEmployeeProfilePage({ employeeId, currentUser, onBack, onEdit, onError, onSuccess }) {
    const [employee, setEmployee] = useState(null);
    const [personal, setPersonal] = useState(emptyPersonalDetails);
    const [documents, setDocuments] = useState([]);
    const [documentType, setDocumentType] = useState("AadhaarCard");
    const [isSavingPersonal, setIsSavingPersonal] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const canManageWork = ["HR", "Admin"].includes(currentUser.role);
    const isSelf = Number(currentUser.employeeId) === Number(employeeId);
    const canManagePrivateData = isSelf || currentUser.role === "Admin";

    async function load() {
        try {
            setEmployee(await getEmployee(employeeId));
            if (canManagePrivateData) {
                const [details, employeeDocuments] = await Promise.all([getPersonalEmployee(employeeId), getEmployeeDocuments(employeeId)]);
                setPersonal(personalToForm(details));
                setDocuments(employeeDocuments);
            }
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to load employee profile."); }
    }

    useEffect(() => { const timer = setTimeout(load, 0); return () => clearTimeout(timer); }, [employeeId, canManagePrivateData]); // eslint-disable-line react-hooks/exhaustive-deps

    async function savePersonalDetails(event) {
        event.preventDefault(); setIsSavingPersonal(true);
        try {
            await updatePersonalEmployee(employeeId, {
                ...personal,
                personalEmail: personal.personalEmail || null,
                alternatePhoneNumber: personal.alternatePhoneNumber || null,
                dateOfBirth: personal.dateOfBirth || null,
            });
            onSuccess("Your personal details were updated."); await load();
        } catch (error) { onError(error?.response?.data?.message ?? "Unable to update personal details."); }
        finally { setIsSavingPersonal(false); }
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

    async function uploadDocument(event) {
        event.preventDefault();
        if (isUploading) return;

        const form = event.currentTarget;
        const file = form.elements.file.files[0];
        if (!file) return;

        setIsUploading(true);
        try {
            await uploadEmployeeDocument(employeeId, documentType, file);
            form.reset();
            onSuccess("Document uploaded securely.");
            await load();
        } catch (error) {
            onError(error?.response?.data?.message ?? error?.response?.data?.detail ?? error?.response?.data?.Message ?? "Unable to upload document.");
        } finally {
            setIsUploading(false);
        }
    }

    if (!employee) return <section className="card"><p>Loading employee profile...</p></section>;
    const workFields = [
        ["Employee code", employee.employeeCode], ["Work email", employee.email],
        ["Department", employee.departmentName], ["Manager", employee.managerName],
        ["Designation", employee.designation], ["Employment type", employee.employmentType],
        ["Status", employee.employmentStatus], ["Work location", employee.workLocation],
        ["Joined", employee.dateOfJoining?.split("T")[0]], ["Leaving date", employee.dateOfLeaving?.split("T")[0]],
    ];
    const updateField = event => setPersonal({ ...personal, [event.target.name]: event.target.value });

    return <div className="module-stack">
        <section className="card profile-hero"><div><p className="eyebrow">Employee profile</p><h2>{employee.firstName} {employee.lastName}</h2><p>{employee.designation} | {employee.departmentName || "No department"}</p></div>
            <div className="button-row"><button className="secondary-btn" onClick={onBack}>Back</button>{canManageWork && <button className="primary-btn" onClick={() => onEdit(employee)}>Edit work details</button>}</div></section>
        <section className="card"><h3>Employment information</h3><div className="detail-grid">{workFields.map(([label, value]) => <div key={label}><span>{label}</span><strong>{value || "-"}</strong></div>)}</div></section>

        {canManagePrivateData ? <section className="card"><div className="page-header"><div><h3>{isSelf ? "Your private personal details" : "Private personal details"}</h3><p>{isSelf ? "Only you can retrieve or update this information." : "Administrators can manage employee personal details."}</p></div></div>
            <form className="form form-grid" onSubmit={savePersonalDetails}>
                <label>Phone number<input name="phoneNumber" type="tel" maxLength="20" value={personal.phoneNumber} onChange={updateField} required /></label>
                <label>Alternate phone<input name="alternatePhoneNumber" type="tel" maxLength="20" value={personal.alternatePhoneNumber} onChange={updateField} /></label>
                <label>Personal email<input name="personalEmail" type="email" maxLength="255" value={personal.personalEmail} onChange={updateField} /></label>
                <label>Date of birth<input name="dateOfBirth" type="date" value={personal.dateOfBirth} onChange={updateField} /></label>
                <label>Gender<select name="gender" value={personal.gender} onChange={updateField}><option value="">Not specified</option><option>Female</option><option>Male</option><option>Non-binary</option><option>Prefer not to say</option></select></label>
                <label>Marital status<select name="maritalStatus" value={personal.maritalStatus} onChange={updateField}><option value="">Not specified</option><option>Single</option><option>Married</option><option>Divorced</option><option>Widowed</option></select></label>
                <label>Blood group<select name="bloodGroup" value={personal.bloodGroup} onChange={updateField}><option value="">Not specified</option>{["A+","A-","B+","B-","AB+","AB-","O+","O-"].map(value => <option key={value}>{value}</option>)}</select></label>
                <label className="span-2">Address line 1<input name="addressLine1" maxLength="255" value={personal.addressLine1} onChange={updateField} /></label>
                <label className="span-2">Address line 2<input name="addressLine2" maxLength="255" value={personal.addressLine2} onChange={updateField} /></label>
                <label>City<input name="city" maxLength="100" value={personal.city} onChange={updateField} /></label>
                <label>State<input name="state" maxLength="100" value={personal.state} onChange={updateField} /></label>
                <label>Postal code<input name="postalCode" maxLength="12" value={personal.postalCode} onChange={updateField} /></label>
                <label>Country<input name="country" maxLength="100" value={personal.country} onChange={updateField} /></label>
                <label>Emergency contact name<input name="emergencyContactName" maxLength="100" value={personal.emergencyContactName} onChange={updateField} /></label>
                <label>Relationship<input name="emergencyContactRelationship" maxLength="50" value={personal.emergencyContactRelationship} onChange={updateField} /></label>
                <label>Emergency phone<input name="emergencyContactPhone" type="tel" maxLength="20" value={personal.emergencyContactPhone} onChange={updateField} /></label>
                <div className="span-2"><button className="primary-btn" disabled={isSavingPersonal}>{isSavingPersonal ? "Saving..." : isSelf ? "Save my details" : "Save personal details"}</button></div>
            </form></section> : <section className="card"><p className="info-message">Personal details are private and visible only to this employee.</p></section>}

        {canManagePrivateData && <section className="card"><div className="page-header"><div><h3>{isSelf ? "Your secure documents" : "Secure documents"}</h3><p>{isSelf ? "PAN, Aadhaar, profile photos, and other files are accessible only to you." : "Administrators can manage employee documents."}</p></div></div>
            <form className="upload-form" onSubmit={uploadDocument}>
                <select value={documentType} onChange={event => setDocumentType(event.target.value)} disabled={isUploading}>{["PanCard","AadhaarCard","ProfilePhoto","Passport","Education","Employment","Other"].map(type => <option key={type}>{type}</option>)}</select>
                <input name="file" type="file" accept=".pdf,.jpg,.jpeg,.png" required disabled={isUploading} /><button className="primary-btn" disabled={isUploading}>{isUploading ? "Uploading..." : "Upload"}</button>
            </form>
            <div className="document-list">{documents.length === 0 ? <p className="empty">No documents uploaded.</p> : documents.map(employeeDocument => <div className="document-item" key={employeeDocument.id}><div><strong>{employeeDocument.documentType}</strong><span>{employeeDocument.originalFileName} | {(employeeDocument.fileSize / 1024).toFixed(1)} KB</span></div><div className="action-buttons"><button type="button" className="secondary-btn" onClick={() => downloadDocument(employeeDocument)}>Download</button><button type="button" className="delete-btn" onClick={async () => { try { await deleteEmployeeDocument(employeeId, employeeDocument.id); onSuccess("Document deleted."); await load(); } catch { onError("Unable to delete document."); } }}>Delete</button></div></div>)}</div>
        </section>}
    </div>;
}
