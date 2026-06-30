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

export const refreshSession = async () => {
    const response = await api.post("/auth/refresh");
    return response.data;
};

export const logoutUser = async () => {
    await api.post("/auth/logout");
};
