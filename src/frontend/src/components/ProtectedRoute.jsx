// ProtectedRoute.js
import { useState, useEffect } from "react";
import { Route, Navigate } from "react-router-dom";
import { isAuthenticated } from "../common/IsAuthenticated"; // import the function above

const ProtectedRoute = () => {
    const [isAuth, setIsAuth] = useState(null);

    useEffect(() => {
        const checkAuth = async () => {
            const authStatus = await isAuthenticated();
            setIsAuth(authStatus);
        };
        checkAuth();
    }, []);

    if (isAuth === null) return <div>Loading...</div>; // Show loading state

    return isAuth ? <Outlet /> : <Navigate to="https://www.google.com/auth/authorize" />;
};

export default ProtectedRoute;