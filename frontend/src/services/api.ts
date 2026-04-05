import axios from 'axios';
import { authService } from './authService';

const getBaseUrl = () => {
    let savedUrl = localStorage.getItem('netpro_api_url');
    
    // Migration logic: Resolve legacy port mismatch automatically
    if (savedUrl && savedUrl.includes(':5246')) {
        savedUrl = savedUrl.replace(':5246', ':5247');
        localStorage.setItem('netpro_api_url', savedUrl);
        console.info('Migrated API Base URL from legacy port 5246 to 5247.');
    }
    
    return savedUrl || 'http://localhost:5247/api';
};

const api = axios.create({
    baseURL: getBaseUrl(),
});

/**
 * Recursive function to convert object keys to camelCase.
 * Needed because the .NET backend returns PascalCase.
 */
const toCamelCase = (o: any): any => {
    if (o === null || typeof o !== 'object') return o;
    if (Array.isArray(o)) return o.map(toCamelCase);
    
    const n: any = {};
    Object.keys(o).forEach((k) => {
        n[k.charAt(0).toLowerCase() + k.slice(1)] = toCamelCase(o[k]);
    });
    return n;
};

api.interceptors.request.use(async (config) => {
    // Ensure token is updated before each request
    try {
        await authService.updateToken(30);
    } catch (e) {
        console.warn('Token update failed in interceptor', e);
    }
    
    const token = authService.getToken();
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    config.baseURL = getBaseUrl();
    return config;
}, (error) => {
    console.error('API Request Error:', error);
    return Promise.reject(error);
});

api.interceptors.response.use(
    (response) => {
        // Automatically convert PascalCase keys to camelCase for the frontend
        if (response.data) {
            response.data = toCamelCase(response.data);
        }
        return response;
    },
    (error) => {
        if (!error.response) {
            console.error('NETWORK ERROR: Backend might be offline or port 5247 is incorrect.');
        } else {
            const { status } = error.response;
            if (status === 401) console.error('AUTH ERROR: 401 Unauthorized. Keycloak session might be invalid.');
            if (status === 403) console.error('AUTH ERROR: 403 Forbidden. Scope/Audience mismatch.');
            if (status === 404) console.error('ROUTE ERROR: 404 Not Found.', error.config.url);
            if (status >= 500) console.error('SERVER ERROR: 500 Backend Exception.');
        }
        return Promise.reject(error);
    }
);

export default api;

// Feature-specific API calls
export const stocksApi = {
    getAll: (params: any) => api.get('/stock', { params }),
    getById: (id: number) => api.get(`/stock/${id}`),
    getBySymbol: (symbol: string) => api.get(`/stock/${symbol}`),
    create: (data: any) => api.post('/stock', data),
    update: (id: number, data: any) => api.put(`/stock/${id}`, data),
    delete: (id: number) => api.delete(`/stock/${id}`),
    getLivePrice: (symbol: string) => api.get(`/stock/${symbol}/live-price`),
};

export const commentsApi = {
    getAll: () => api.get('/comment'),
    getById: (id: number) => api.get(`/comment/${id}`),
    create: (stockId: number, data: any) => api.post(`/comment/${stockId}`, data),
    update: (id: number, data: any) => api.put(`/comment/${id}`, data),
    delete: (id: number) => api.delete(`/comment/${id}`),
};

export const ratingsApi = {
    getAll: () => api.get('/rating'),
    getById: (id: number) => api.get(`/rating/${id}`),
    create: (stockId: number, data: any) => api.post(`/rating/${stockId}`, data),
    update: (id: number, data: any) => api.put(`/rating/${id}`, data),
    delete: (id: number) => api.delete(`/rating/${id}`),
};

export const watchlistApi = {
    get: () => api.get('/watchlist'),
    add: (stockId: number) => api.post(`/watchlist/${stockId}`),
    remove: (stockId: number) => api.delete(`/watchlist/${stockId}`),
};

export const portfolioApi = {
    get: () => api.get('/portfolio'),
    add: (symbol: string) => api.post(`/portfolio?symbol=${symbol}`),
    remove: (symbol: string) => api.delete(`/portfolio?symbol=${symbol}`),
};

export const transactionsApi = {
    getAll: (params?: any) => api.get('/transaction', { params }),
    getById: (id: number) => api.get(`/transaction/${id}`),
    create: (data: any) => api.post('/transaction', data),
    getSummary: () => api.get('/transaction/summary'),
    getByStockId: (stockId: number) => api.get(`/transaction/stock/${stockId}`),
    getRealizedGains: () => api.get('/transaction/realized-gains'),
    exportCsv: () => api.get('/transaction/export', { responseType: 'blob' }),
};

export const analyticsApi = {
    getPortfolioPerformance: () => api.get('/portfolioanalytics/performance'),
    getPortfolioHistory: (days: number = 30) => api.get(`/portfolioanalytics/history?days=${days}`),
    getDiversification: () => api.get('/portfolioanalytics/diversification'),
    getStockPerformance: (id: number) => api.get(`/stockanalytics/${id}/performance`),
};
