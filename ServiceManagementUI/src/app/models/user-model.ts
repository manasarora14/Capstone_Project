export interface User {
    email: string;
    name?: string;
    displayName?: string;
    role: string;
    token: string;
}

export interface AuthResponse {
    token: string;
    email: string;
    role: string;
}

