const BASE_URL = "https://bubble-epidermis-landslide.ngrok-free.dev";

async function request(path, options = {}) {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "ngrok-skip-browser-warning": "true",
      ...(options.headers || {}),
    },
  });

  const text = await res.text();
  const data = text ? JSON.parse(text) : null;

  if (!res.ok) {
    throw new Error(typeof data === "string" ? data : data?.message || "Request failed");
  }
  return data;
}

export const api = {
  register: (name, email, password) =>
    request("/api/User/register", { method: "POST", body: JSON.stringify({ name, email, password }) }),

  login: (email, password) =>
    request("/api/User/login", { method: "POST", body: JSON.stringify({ email, password }) }),

  getProducts: () => request("/api/Product/all"),

  createOrder: (items) =>
    request("/api/Order/create", { method: "POST", body: JSON.stringify({ items }) }),

  checkout: (orderId, provider) =>
    request(`/api/Payment/checkout/${orderId}`, { method: "POST", body: JSON.stringify({ provider }) }),

  confirmPayment: (provider, transactionId) =>
    request(`/api/Payment/confirm?provider=${provider}&transactionId=${transactionId}`, { method: "POST" }),
};