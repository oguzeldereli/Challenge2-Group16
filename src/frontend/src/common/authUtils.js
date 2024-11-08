export function generateCodeVerifier(length = 128) {
    const array = new Uint8Array(length);
    window.crypto.getRandomValues(array);

    const string = String.fromCharCode(...array);
    const base64String = btoa(string)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    
    return base64String;
}

export function generateState(length = 32) {
    const array = new Uint8Array(length);
    window.crypto.getRandomValues(array);

    const string = String.fromCharCode(...array);
    const base64String = btoa(string)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    
    return base64String;
}

export async function generateCodeChallenge(codeVerifier) {
    const encoder = new TextEncoder();
    const data = encoder.encode(codeVerifier);
    const digest = await window.crypto.subtle.digest('SHA-256', data);
    
    // Convert digest (ArrayBuffer) directly to Base64
    let base64String = btoa(String.fromCharCode(...new Uint8Array(digest)));
    
    // Make the string URL-safe by replacing certain characters
    base64String = base64String
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');

    return base64String;
}