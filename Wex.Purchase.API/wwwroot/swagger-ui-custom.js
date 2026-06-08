// Custom JavaScript for Bearer Token Authorization in Swagger UI
console.log('Bearer Token Auth Script Loaded');

// Add Bearer Auth UI to Swagger
function initBearerAuth() {
    console.log('Initializing Bearer Auth UI');

    const topbar = document.querySelector('.topbar');
    if (!topbar) {
        console.warn('Topbar not found, retrying...');
        setTimeout(initBearerAuth, 500);
        return;
    }

    // Check if already initialized
    if (document.getElementById('bearerAuthContainer')) {
        console.log('Bearer Auth UI already exists');
        return;
    }

    // Create the container
    const container = document.createElement('div');
    container.id = 'bearerAuthContainer';

    container.innerHTML = `
        <div style="background: #fff5f5; border-left: 4px solid #dc3545; padding: 16px; margin: 0; border-radius: 0; font-family: Arial, sans-serif;">
            <div style="max-width: 1200px; margin: 0 auto;">
                <h3 style="margin: 0 0 12px 0; color: #dc3545; font-size: 16px; font-weight: bold;">🔐 Bearer Token Authorization</h3>
                <div style="display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
                    <label style="font-weight: 500; color: #333; margin: 0;">Token:</label>
                    <input type="text" id="bearerTokenInput" placeholder="Enter token (e.g., secret)" 
                           style="padding: 8px 12px; border: 1px solid #ddd; border-radius: 4px; font-family: monospace; width: 250px;" />
                    <button onclick="setBearer()" style="background: #dc3545; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-weight: 500;">Set Token</button>
                    <button onclick="clearBearer()" style="background: #6c757d; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-weight: 500;">Clear</button>
                    <small style="color: #666; margin-left: 10px;">Default: <code style="background: #f0f0f0; padding: 2px 6px; border-radius: 3px; font-family: monospace;">secret</code></small>
                </div>
            </div>
        </div>
    `;

    // Insert before topbar
    topbar.parentElement.insertBefore(container, topbar);

    console.log('Bearer Auth UI added');
    loadSavedToken();
    setupRequestInterceptor();
}

// Intercept all fetch requests to add Bearer token
function setupRequestInterceptor() {
    const originalFetch = window.fetch;
    window.fetch = function (...args) {
        const token = localStorage.getItem('bearerToken');
        if (token) {
            if (!args[1]) {
                args[1] = {};
            }
            if (!args[1].headers) {
                args[1].headers = {};
            }
            args[1].headers['Authorization'] = 'Bearer ' + token;
            console.log('Added Bearer token to request');
        }
        return originalFetch.apply(window, args);
    };
    console.log('Request interceptor setup');
}

function setBearer() {
    const input = document.getElementById('bearerTokenInput');
    const token = input.value.trim();

    if (!token) {
        alert('Please enter a bearer token');
        return;
    }

    localStorage.setItem('bearerToken', token);
    input.style.borderColor = '#28a745';
    alert('✓ Bearer token set! Authorization: Bearer ' + token);

    setTimeout(() => {
        input.style.borderColor = '';
    }, 2000);
}

function clearBearer() {
    const input = document.getElementById('bearerTokenInput');
    input.value = '';
    localStorage.removeItem('bearerToken');
    alert('✓ Bearer token cleared');
}

function loadSavedToken() {
    const saved = localStorage.getItem('bearerToken');
    if (saved) {
        const input = document.getElementById('bearerTokenInput');
        input.value = saved;
        input.style.borderColor = '#28a745';
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initBearerAuth);
} else {
    initBearerAuth();
}

// Also try after a delay as backup
setTimeout(initBearerAuth, 1000);


