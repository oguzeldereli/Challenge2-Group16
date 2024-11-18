import jwtDecode from 'jwt-decode';

export function isTokenExpired(token) 
{
    const { exp } = jwtDecode(token);
    return Date.now() >= exp * 1000;
}