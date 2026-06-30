import axios from "axios";

const API_BASE_URL =
    import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7030/api/employees";

export const getEmployees = async ({ search = "", pageNumber = 1, pageSize = 25 } = {}) => {
    const response = await axios.get(API_BASE_URL, {
        params: {
            search,
            pageNumber,
            pageSize,
        },
    });
    return response.data;
};

export const createEmployee = async (employee) => {
    const response = await axios.post(API_BASE_URL, employee);
    return response.data;
};

export const updateEmployee = async (id, employee) => {
    await axios.put(`${API_BASE_URL}/${id}`, employee);
};

export const deleteEmployee = async (id) => {
    await axios.delete(`${API_BASE_URL}/${id}`);
};
