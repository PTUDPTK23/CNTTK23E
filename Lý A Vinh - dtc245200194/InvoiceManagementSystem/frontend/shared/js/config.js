// Keep the API hostname aligned with the page hostname so CORS and auth cookies match.
const localApiHost = ["localhost", "127.0.0.1"].includes(window.location.hostname)
  ? window.location.hostname
  : "localhost";
window.APP_CONFIG = Object.freeze({ apiBaseUrl: `http://${localApiHost}:5000` });
