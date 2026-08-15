import axios from "axios";

export const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7030/api";

const api = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true,
});

api.interceptors.request.use((config) => {
    const token = localStorage.getItem("hrconnect_token");

    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;
        const isAuthEndpoint = originalRequest?.url?.startsWith("/auth/");

        if (error.response?.status !== 401 || originalRequest?._retry || isAuthEndpoint) {
            return Promise.reject(error);
        }

        originalRequest._retry = true;

        try {
            const refreshResponse = await api.post("/auth/refresh");
            localStorage.setItem("hrconnect_token", refreshResponse.data.accessToken);
            localStorage.setItem("hrconnect_user", JSON.stringify(refreshResponse.data.user));
            originalRequest.headers.Authorization = `Bearer ${refreshResponse.data.accessToken}`;
            return api(originalRequest);
        } catch (refreshError) {
            localStorage.removeItem("hrconnect_token");
            localStorage.removeItem("hrconnect_user");
            return Promise.reject(refreshError);
        }
    }
);

export const getEmployees = async ({ search = "", pageNumber = 1, pageSize = 25 } = {}) => {
    const response = await api.get("/employees", {
        params: {
            search,
            pageNumber,
            pageSize,
        },
    });
    return response.data;
};

export const getEmployeeOptions = async () => {
    const response = await api.get("/employees/options");
    return response.data;
};

export const getEmployee = async (id) => {
    const response = await api.get(`/employees/${id}`);
    return response.data;
};

export const getPersonalEmployee = async (id) => {
    const response = await api.get(`/employees/${id}/personal`);
    return response.data;
};

export const updatePersonalEmployee = async (id, details) => {
    await api.put(`/employees/${id}/personal`, details);
};

export const createEmployee = async (employee) => {
    const response = await api.post("/employees", employee);
    return response.data;
};

export const updateEmployee = async (id, employee) => {
    await api.put(`/employees/${id}`, employee);
};

export const deleteEmployee = async (id) => {
    await api.delete(`/employees/${id}`);
};

export const loginUser = async (credentials) => {
    const response = await api.post("/auth/login", credentials);
    return response.data;
};

export const signupUser = async (account) => {
    const response = await api.post("/auth/signup", account);
    return response.data;
};

export const requestPasswordReset = async (email) => {
    const response = await api.post("/auth/forgot-password", { email });
    return response.data;
};

export const resetPassword = async (token, newPassword) => {
    const response = await api.post("/auth/reset-password", { token, newPassword });
    return response.data;
};

export const getGoogleAuthConfig = async () => {
    const response = await api.get("/auth/google/config");
    return response.data;
};

export const loginWithGoogle = async (credential) => {
    const response = await api.post("/auth/google", { credential });
    return response.data;
};

export const refreshSession = async () => {
    const response = await api.post("/auth/refresh");
    return response.data;
};

export const logoutUser = async () => {
    await api.post("/auth/logout");
};

export const getDepartments = async (includeInactive = false) =>
    (await api.get("/departments", { params: { includeInactive } })).data;
export const createDepartment = async (department) => (await api.post("/departments", department)).data;
export const updateDepartment = async (id, department) => api.put(`/departments/${id}`, department);
export const deleteDepartment = async (id) => api.delete(`/departments/${id}`);

export const getLeaveTypes = async (includeInactive = false) =>
    (await api.get("/leave/types", { params: { includeInactive } })).data;
export const createLeaveType = async (leaveType) => (await api.post("/leave/types", leaveType)).data;
export const updateLeaveType = async (id, leaveType) => api.put(`/leave/types/${id}`, leaveType);
export const getLeaveBalances = async (employeeId) =>
    (await api.get("/leave/balances", { params: employeeId ? { employeeId } : {} })).data;
export const adjustLeaveBalance = async (adjustment) => api.post("/leave/balances/adjust", adjustment);
export const getLeaveRequests = async (status) =>
    (await api.get("/leave/requests", { params: status ? { status } : {} })).data;
export const createLeaveRequest = async (request) => (await api.post("/leave/requests", request)).data;
export const reviewLeaveRequest = async (id, status, comment = "") =>
    api.put(`/leave/requests/${id}/review`, { status, comment });
export const cancelLeaveRequest = async (id) => api.put(`/leave/requests/${id}/cancel`);

export const getUsers = async () => (await api.get("/users")).data;
export const changeUserRole = async (id, role) => api.put(`/users/${id}/role`, { role });
export const linkUserEmployee = async (id, employeeId) => api.put(`/users/${id}/employee`, { employeeId });

export const getEmployeeDocuments = async (employeeId) =>
    (await api.get(`/employees/${employeeId}/documents`)).data;
export const uploadEmployeeDocument = async (employeeId, documentType, file, notes = "") => {
    const formData = new FormData();
    formData.append("documentType", documentType);
    formData.append("file", file);
    formData.append("notes", notes);
    return (await api.post(`/employees/${employeeId}/documents`, formData)).data;
};
export const downloadEmployeeDocument = async (employeeId, employeeDocument) => {
    const response = await api.get(`/employees/${employeeId}/documents/${employeeDocument.id}/download`, { responseType: "blob" });
    const url = URL.createObjectURL(response.data);
    const link = document.createElement("a");
    link.href = url;
    link.download = employeeDocument.originalFileName;
    link.click();
    URL.revokeObjectURL(url);
};
export const deleteEmployeeDocument = async (employeeId, documentId) =>
    api.delete(`/employees/${employeeId}/documents/${documentId}`);
