    // auth.js or similar file
export async function isAuthenticated() {
    try {
        const response = await fetch("/api/auth/validate", {
            credentials: "include" // Ensures cookies are sent with the request
        });
        if (response.ok) {
            const result = await response.json();
            return result.isAuthenticated;
        }
        return false;
    } catch (error) {
        console.error("Failed to check auth status", error);
        return false;
    }
}
